
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public static ulong GetRoadPairKey(Road.Segment a, Road.Segment b)
		{
			var ret = (ulong)Unsafe.BitCast<Road.Segment, uint>(a);
			ret <<= 32;
			ret |= Unsafe.BitCast<Road.Segment, uint>(b);

			return ret;
		}

		public static void RecalculateRoads()
		{
			road_segments.Clear();
			road_junctions.Clear();
			road_segments_overlapped.Clear();
			road_segment_to_junction_index.Clear();

			location_to_road.Clear();
			location_to_rail.Clear();

			road_to_location.Clear();
			rail_to_location.Clear();

			{
				var ts = Timestamp.Now();
				foreach (var asset in IDistrict.Database.GetAssets())
				{
					if (asset.id == 0) continue;

					var h_district = asset.GetHandle();

					var roads_span = asset.data.roads.AsSpan();
					for (var road_index = 0; road_index < roads_span.Length; road_index++)
					{
						ref var road = ref roads_span[road_index];

						road.UpdateBB();

						var points_span = road.points.AsSpan();
						for (var point_index = 0; point_index < points_span.Length; point_index++)
						{
							ref var point = ref points_span[point_index];

							var pos_grid = new short2((short)point.X, (short)point.Y);
							var pos_key = Unsafe.BitCast<short2, int>(pos_grid);

							var segment = new Road.Segment(h_district, (byte)road_index, (byte)point_index);

							//road_segments_overlapped

							if (!road_segments.TryAdd(pos_key, segment))
							{
								var segment_other = road_segments[pos_key];
								//if (segment_other.chain == segment.chain) continue;

								if (!road_segments_overlapped.TryGetValue(pos_key, out var segments_list))
								{
									segments_list = road_segments_overlapped[pos_key] = new(4);
									segments_list.Add(road_segments[pos_key]);
								}

								segments_list.Add(segment);
							}
						}
					}
				}

				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Rasterized road lines in {ts_elapsed:0.0000} ms.");
			}

			{
				var ts = Timestamp.Now();
				if (road_segments_overlapped.Count > 0)
				{
					var road_junction_threshold_sq = road_junction_threshold * road_junction_threshold;

					foreach (var pair in road_segments_overlapped)
					{
						var road_list = pair.Value;

						//var road_list_count = road_list.Count;
						Span<Road.Segment> road_list_span = stackalloc Road.Segment[road_list.Count];
						road_list.CopyTo(road_list_span);

						//Span<int> indices = stackalloc int[road_list.Count];

						while (road_list_span.Length > 0)
						{
							var junction = new Road.Junction();
							junction.pos = road_list_span[0].GetPosition();
							junction.segments[junction.segments_count++] = road_list_span[0];

							road_list_span.RemoveAtSwapback(0, resize: true);

							for (var i = 0; i < road_list_span.Length;)
							{
								ref var road = ref road_list_span[i];
								var pos = road.GetPosition();

								if (Vector2.DistanceSquared(junction.pos, pos) < road_junction_threshold_sq)
								{
									junction.segments[junction.segments_count++] = road;
									junction.pos = Vector2.Lerp(junction.pos, pos, 0.50f);
									road_list_span.RemoveAtSwapback(i, resize: true);
								}
								else
								{
									i++;
								}
							}

							if (junction.segments_count > 1)
							{
								var junction_index = road_junctions.Count;
								road_junctions.Add(junction);

								for (var j = 0; j < junction.segments_count; j++)
								{
									road_segment_to_junction_index[junction.segments[j]] = junction_index;
								}
							}
						}
					}
				}
				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Calculated road junctions in {ts_elapsed:0.0000} ms.");
			}

			{
				foreach (var asset in ILocation.Database.GetAssets())
				{
					if (asset.id == 0) continue;
					ref var asset_data = ref asset.GetData();


					{
						var nearest_segment = GetNearestRoad(asset_data.h_district, Road.Type.Road, (Vector2)asset_data.point, out var dist_sq);
						if (dist_sq <= 1.50f.Pow2())
						{
							location_to_road[asset] = nearest_segment;
							road_to_location[nearest_segment] = asset;

							if (road_segment_to_junction_index.ContainsKey(nearest_segment))
							{
								App.WriteLine($"Location \"{asset.identifier}\"'s nearest road is a junction - this may cause issues!", App.Color.Yellow);
							}
						}
					}

					{
						var nearest_segment = GetNearestRoad(asset_data.h_district, Road.Type.Rail, (Vector2)asset_data.point, out var dist_sq);
						if (dist_sq <= 1.00f.Pow2())
						{
							location_to_rail[asset] = nearest_segment;
							rail_to_location[nearest_segment] = asset;

							if (road_segment_to_junction_index.ContainsKey(nearest_segment))
							{
								App.WriteLine($"Location \"{asset.identifier}\"'s nearest rail is a junction - this may cause issues!", App.Color.Yellow);
							}
						}
					}
				}
			}
		}

		// TODO: implement a faster lookup
		public static ILocation.Handle GetNearestLocation(Vector2 position, out float distance_sq)
		{
			var nearest_handle = default(ILocation.Handle);
			var nearest_distance_sq = float.MaxValue;

			foreach (var asset in ILocation.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();

				var distance_sq_tmp = Vector2.DistanceSquared((Vector2)asset_data.point, position);
				if (distance_sq_tmp < nearest_distance_sq)
				{
					nearest_handle = asset;
					nearest_distance_sq = distance_sq_tmp;
				}
			}

			distance_sq = nearest_distance_sq;
			return nearest_handle;
		}

		// TODO: implement a faster lookup, especially for this
		public static ref Doodad.Renderer.Data GetNearestDoodad(Vector2 position, Span<Doodad.Renderer.Data> span, out int index, out float distance_sq)
		{
			var nearest_index = int.MaxValue;
			var nearest_distance_sq = float.MaxValue;

			for (var i = 0; i < span.Length; i++)
			{
				ref var doodad = ref span[i];

				var distance_sq_tmp = Vector2.DistanceSquared(doodad.position, position);
				if (distance_sq_tmp < nearest_distance_sq)
				{
					nearest_index = i;
					nearest_distance_sq = distance_sq_tmp;
				}
			}

			index = nearest_index;
			distance_sq = nearest_distance_sq;

			if ((uint)index < span.Length) return ref span[nearest_index];
			else return ref Unsafe.NullRef<Doodad.Renderer.Data>();
		}

		// TODO: implement a faster lookup
		public static Road.Segment GetNearestRoad(IDistrict.Handle h_district, Road.Type type, Vector2 position, out float distance_sq)
		{
			var road_points_distance_sq = float.MaxValue;
			var road_point_index = int.MaxValue;
			var road_index = int.MaxValue;

			ref var district_data = ref h_district.GetData();
			if (district_data.IsNotNull())
			{
				var span_roads = district_data.roads.AsSpan();
				for (var i = 0; i < span_roads.Length; i++)
				{
					ref var road = ref span_roads[i];
					if (road.type != type) continue;
					if (road.bb.Grow(1.00f).ContainsPoint(position))
					{
						var points = road.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(position, out var road_nearest_index_tmp, out var road_nearest_distance_sq_tmp);

							if (road_nearest_distance_sq_tmp < road_points_distance_sq)
							{
								road_points_distance_sq = road_nearest_distance_sq_tmp;
								road_point_index = road_nearest_index_tmp;
								road_index = i;
							}
						}
					}
				}
			}

			distance_sq = road_points_distance_sq;
			return new Road.Segment(h_district, (byte)road_index, (byte)road_point_index);
		}

#if CLIENT
		public static void DrawConnectedRoads(Road.Segment road_segment, ref Matrix3x2 mat_l2c, float zoom, int iter_max = 10, float budget = 1000.00f)
		{
			//var thickness = road_segment.GetRoad().scale * 0.50f;
			var alpha = 0.25f;

			var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
			//junctions_span.GetNearestIndex(mouse, out var junction_index, out var junction_dist_sq, (ref Road.Junction junction) => ref junction.pos);

			//if (junction_dist_sq < 1.00f.Pow2())
			{
				//ref var junction = ref junctions_span[junction_index];

				junctions_queue.Clear();
				segments_visited.Clear();

				//hs_visited.Add(junction_index);

				//var type = junction.segments[0].GetRoad().type;

				//var iter_max = 10;

				//junctions_queue.Enqueue(junction_index);

				ref var road = ref road_segment.GetRoad();
				var type = road.type;

				if (road_segment_to_junction_index.TryGetValue(road_segment, out var junction_index))
				{
					junctions_queue.Enqueue(new(junction_index, 0.00f, budget));
				}
				else
				{
					DrawSegment(segments_visited, junctions_queue, new(0, 0.00f, budget), ref road_segment, ref mat_l2c, zoom, type, Color32BGRA.Green.WithAlphaMult(alpha), alpha, budget);
				}

				for (var i = 0; i < iter_max; i++)
				{
					var count = junctions_queue.Count;
					for (var j = 0; j < count; j++)
					{
						var connection = junctions_queue.Dequeue();
						DrawJunction(segments_visited, junctions_queue, junctions_span, in connection, ref mat_l2c, zoom, i, iter_max, type, alpha, budget);
					}
				}

				//DrawJunction(hs_visited, queue, junctions_span, ref junction.segments[0], ref mat_l2c, zoom, 0, iter_max);
				static void DrawJunction(HashSet<ulong> hs_visited, Queue<RoadConnection> queue, Span<Road.Junction> junctions_span, in RoadConnection connection, ref Matrix3x2 mat_l2c, float zoom, int depth, int iter_max, Road.Type type, float alpha, float budget)
				{
					var color = Color32BGRA.FromHSV((1.00f - ((float)depth / (float)iter_max)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha);

					ref var junction = ref junctions_span[connection.junction_index];
					//GUI.DrawCircle(Vector2.Transform(junction.pos, mat_l2c), 0.125f * zoom, color: color, segments: 12, layer: GUI.Layer.Window);

					var segments_span = junction.segments.Slice(junction.segments_count);
					foreach (ref var segment_base in segments_span)
					{
						DrawSegment(hs_visited, queue, in connection, ref segment_base, ref mat_l2c, zoom, type, color, alpha, budget);
					}
				}

				static void DrawSegment(HashSet<ulong> hs_visited, Queue<RoadConnection> queue, in RoadConnection connection, ref Road.Segment segment_base, ref Matrix3x2 mat_l2c, float zoom, Road.Type type, Color32BGRA color, float alpha, float budget)
				{
					ref var road = ref segment_base.GetRoad();
					if (road.type != type) return;

					var thickness = road.scale * 0.50f;

					var pos = segment_base.GetPosition();
					var road_points_span = road.points.AsSpan();

					//GUI.DrawCircleFilled(Vector2.Transform(pos, mat_l2c), 0.125f * zoom * 0.50f, color: color, segments: 4, layer: GUI.Layer.Window);
					var depth_offset = Vector2.Zero;

					{
						var connection_copy = connection;

						var pos_last = pos;
						for (var i = segment_base.index + 1; i < road_points_span.Length; i++)
						{
							var segment = new Road.Segment(segment_base.chain, (byte)i);
							var key = GetRoadPairKey(segment, new(segment_base.chain, (byte)(i - 1)));

							if (hs_visited.Contains(key)) break;
							hs_visited.Add(key);

							var distance = Vector2.Distance(pos_last, road_points_span[i]);
							connection_copy.distance += distance;
							connection_copy.budget -= (distance / (road.speed_mult * road.integrity));

							GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, Color32BGRA.FromHSV((Maths.NormalizeClamp(connection_copy.budget, budget)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha), thickness: thickness * zoom, layer: GUI.Layer.Window);

							if (connection_copy.budget <= 0.00f) break;

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								GUI.DrawTextCentered($"{(connection_copy.distance * km_per_unit):0.0}km; {connection_copy.budget:0}/{budget:0}", pos_last.Transform(in mat_l2c), layer: GUI.Layer.Window, box_shadow: false);

								connection_copy.junction_index = junction_index_new;
								queue.Enqueue(connection_copy);
								break;
							}

							pos_last = road_points_span[i];
						}
					}

					{
						var connection_copy = connection;

						var pos_last = pos;
						for (var i = segment_base.index - 1; i >= 0; i--)
						{
							var segment = new Road.Segment(segment_base.chain, (byte)i);
							var key = GetRoadPairKey(new(segment_base.chain, (byte)(i + 1)), segment);

							if (hs_visited.Contains(key)) break;
							hs_visited.Add(key);

							var distance = Vector2.Distance(pos_last, road_points_span[i]);
							connection_copy.distance += distance;
							connection_copy.budget -= (distance / (road.speed_mult * road.integrity));

							GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, Color32BGRA.FromHSV((Maths.NormalizeClamp(connection_copy.budget, budget)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha), thickness: thickness * zoom, layer: GUI.Layer.Window);

							if (connection_copy.budget <= 0.00f) break;

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								GUI.DrawTextCentered($"{(connection_copy.distance * km_per_unit):0.0}km; {connection_copy.budget:0}/{budget:0}", pos_last.Transform(in mat_l2c), layer: GUI.Layer.Window, box_shadow: false);

								connection_copy.junction_index = junction_index_new;
								queue.Enqueue(connection_copy);
								break;
							}

							pos_last = road_points_span[i];
						}
					}

					//var key_base = GetRoadPairKey(segment, new(segment_base.chain, (byte)(i - 1)));

					//if (hs_visited.Contains(segment_base.GetHashCode())) return;
					//hs_visited.Add(segment_base.GetHashCode());
				}
			}
		}

		public static void DrawOutlineShader(Span<short2> points, Color32BGRA color, float thickness, Texture.Handle h_texture, bool loop = true)
		{
			var count = points.Length;

			var last_vert = default(short2);
			for (var i = 0; i < (count + 1); i++)
			{
				if (!loop && i >= count) break;

				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = (Vector2)last_vert;
					var b = (Vector2)vert;

					IScenario.WorldMap.Renderer.Add(new()
					{
						a = a,
						b = b,
						color = color,
						h_texture = h_texture,
						thickness = thickness,
						uv_scale = Vector2.Distance(a, b) * 0.5f
					});

					//GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void DrawOutlineShader(Span<Vector2> points, Color32BGRA color, float thickness, Texture.Handle h_texture, bool loop = true)
		{
			var count = points.Length;

			var last_vert = default(Vector2);
			for (var i = 0; i < (count + 1); i++)
			{
				if (!loop && i >= count) break;

				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = last_vert;
					var b = vert;

					IScenario.WorldMap.Renderer.Add(new()
					{
						a = a,
						b = b,
						color = color,
						h_texture = h_texture,
						thickness = thickness,
						uv_scale = Vector2.Distance(a, b) * 0.5f
					});

					//GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void DrawOutline(Matrix3x2 mat_l2c, float zoom, Span<short2> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
		{
			var count = points.Length;

			var last_vert = default(short2);
			for (var i = 0; i < (count + 1); i++)
			{
				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = (Vector2)last_vert;
					var b = (Vector2)vert;

					var ta = Vector2.Transform(a, mat_l2c);
					var tb = Vector2.Transform(b, mat_l2c);

					GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void Rescale()
		{
			foreach (var asset in ILocation.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				asset_data.point *= 2;

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IProvince.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				foreach (ref var point in asset_data.points.AsSpan())
				{
					point *= 2;
				}

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IDistrict.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				foreach (ref var point in asset_data.points.AsSpan())
				{
					point *= 2;
				}

				foreach (ref var road in asset_data.roads.AsSpan())
				{
					foreach (ref var point in road.points.AsSpan())
					{
						point *= 2;
					}
				}

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IScenario.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();

				foreach (ref var doodad in asset_data.doodads.AsSpan())
				{
					doodad.position *= 2;
				}

				hs_pending_asset_saves.Add(asset);
			}
		}
#endif
	}
}

