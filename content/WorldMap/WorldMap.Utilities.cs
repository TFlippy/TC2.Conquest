
using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		private static Dictionary<int, Road.Segment> road_segments_tmp = new(256);
		private static Dictionary<int, List<Road.Segment>> road_segments_overlapped_tmp = new(256);

		//public static void GetSegment(ref this Road.Junction.Branch branch, out Road.Segment segment)
		//{
		//	segment = new Road.Segment()
		//}

		public static sbyte GetSign(this Road.Segment j_segment, Vector2 dir, bool ignore_limits, float dot_min, float dot_max)
		{
			ref var j_road = ref j_segment.GetRoad();
			if (j_road.IsNull()) return 0;

			var j_points = j_road.points.AsSpan();
			var j_pos = j_points[j_segment.index];

			var c_alt_sign = 0;
			var c_alt_dot = -1.00f;

			if (j_segment.index < j_points.Length - 1)
			{
				var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
				var dot_tmp = Vector2.Dot(dir, dir_tmp);

				if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
				{
					c_alt_dot = dot_tmp;
					c_alt_sign = 1;
				}
			}

			if (j_segment.index > 0)
			{
				var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
				var dot_tmp = Vector2.Dot(dir, dir_tmp);

				if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
				{
					c_alt_dot = dot_tmp;
					c_alt_sign = -1;
				}
			}

			return (sbyte)c_alt_sign;
		}

		//public static ulong GetRoadPairKey(Road.Segment a, Road.Segment b)
		//{
		//	var ret = (ulong)Unsafe.BitCast<Road.Segment, uint>(a);
		//	ret <<= 32;
		//	ret |= Unsafe.BitCast<Road.Segment, uint>(b);

		//	return ret;
		//}

		public static void RecalculateRoads()
		{
			road_segments_tmp.Clear();
			road_segments_overlapped_tmp.Clear();

			var road_junctions_tmp = new List<Road.Junction>(64);
			var road_segment_to_junction_index_tmp = new Dictionary<Road.Segment, int>(64);

			pos_hash_to_prefecture.Clear();

			location_to_road.Clear();
			location_to_rail.Clear();

			road_to_location.Clear();
			rail_to_location.Clear();

			{
				var ts = Timestamp.Now();
				foreach (var asset in IPrefecture.Database.GetAssetsSpan())
				{
					if (asset.id == 0) continue;

					var h_prefecture = asset.GetHandle();

					var roads_span = asset.data.roads.AsSpan();
					for (var road_index = 0; road_index < roads_span.Length; road_index++)
					{
						ref var road = ref roads_span[road_index];

						road.UpdateBB();

						var points_span = road.points.AsSpan();
						for (var point_index = 0; point_index < points_span.Length; point_index++)
						{
							ref var point = ref points_span[point_index];

							var pos_grid = new Vec2i16((short)point.X, (short)point.Y);
							var pos_key = Maths.ToInt32BitCast(pos_grid);

							pos_hash_to_prefecture[pos_key] = h_prefecture;

							var segment = new Road.Segment(h_prefecture, (byte)road_index, (byte)point_index);

							//road_segments_overlapped

							if (!road_segments_tmp.TryAdd(pos_key, segment))
							{
								//var segment_other = road_segments_tmp[pos_key];
								//if (segment_other.chain == segment.chain) continue;

								if (!road_segments_overlapped_tmp.TryGetValue(pos_key, out var segments_list))
								{
									segments_list = road_segments_overlapped_tmp[pos_key] = new(4);
									segments_list.Add(road_segments_tmp[pos_key]);
								}

								segments_list.Add(segment);
							}
							else if (point_index == 0 || point_index == points_span.Length - 1)
							{
								if (!road_segments_overlapped_tmp.TryGetValue(pos_key, out var segments_list))
								{
									segments_list = road_segments_overlapped_tmp[pos_key] = new(4);
									segments_list.Add(road_segments_tmp[pos_key]);
								}
							}
						}
					}
				}

				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Calculated road segments in {ts_elapsed:0.0000} ms.");
			}

			{
				var ts = Timestamp.Now();
				if (road_segments_overlapped_tmp.Count > 0)
				{
					var road_junction_threshold_sq = road_junction_threshold * road_junction_threshold;

					foreach (var pair in road_segments_overlapped_tmp)
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

							if (junction.segments_count > 1 || (junction.segments_count == 1 && (junction.segments[0].index == 0 || junction.segments[0].index == junction.segments[0].chain.GetSpan().Length - 1)))
							{
								var junction_index = road_junctions_tmp.Count;
								junction.index = (ushort)junction_index;
								road_junctions_tmp.Add(junction);

								for (var j = 0; j < junction.segments_count; j++)
								{
									road_segment_to_junction_index_tmp[junction.segments[j]] = junction_index;
								}
							}
						}
					}
				}
				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Calculated road junctions in {ts_elapsed:0.0000} ms.");
			}

			{
				foreach (var asset in ILocation.Database.GetAssetsSpan())
				{
					if (asset.id == 0) continue;
					ref var asset_data = ref asset.GetData();
					if (asset_data.flags.HasAny(ILocation.Flags.Hidden) || asset_data.h_location_parent) continue;

					var pos_grid = asset_data.point;
					var pos_key = Maths.ToInt32BitCast(pos_grid);

					pos_hash_to_prefecture[pos_key] = asset_data.h_prefecture;

					//if (asset_data.buildings.HasAny(ILocation.Buildings.Checkpoint))
					{
						var nearest_segment = GetNearestRoad(asset_data.h_prefecture, Road.Type.Road, (Vector2)asset_data.point, out var dist_sq);
						if (dist_sq <= 1.50f.Pow2())
						{
							location_to_road[asset] = nearest_segment;
							road_to_location[nearest_segment] = asset;

							if (road_segment_to_junction_index_tmp.ContainsKey(nearest_segment))
							{
								if (App.log_filter.HasAny(App.LogFlags.Debug)) App.WriteLine($"- Location \"{asset.identifier}\"'s nearest road is a junction - this may cause issues!", App.Color.DarkYellow);
							}
						}
					}

					if (asset_data.buildings.HasAny(ILocation.Buildings.Trainyard))
					{
						var nearest_segment = GetNearestRoad(asset_data.h_prefecture, Road.Type.Rail, (Vector2)asset_data.point, out var dist_sq);
						if (dist_sq <= 1.00f.Pow2())
						{
							location_to_rail[asset] = nearest_segment;
							rail_to_location[nearest_segment] = asset;

							if (road_segment_to_junction_index_tmp.ContainsKey(nearest_segment))
							{
								if (App.log_filter.HasAny(App.LogFlags.Debug)) App.WriteLine($"- Location \"{asset.identifier}\"'s nearest rail is a junction - this may cause issues!", App.Color.DarkYellow);
							}
						}
					}
				}
			}

			{
				foreach (var asset in IPrefecture.Database.GetAssetsSpan())
				{
					asset.UpdateBB();
				}
			}

			road_junctions = road_junctions_tmp.ToArray();
			road_segment_to_junction_index = road_segment_to_junction_index_tmp.ToFrozenDictionary();
		}

		// TODO: implement a faster lookup
		public static ILocation.Handle GetNearestLocation(Vector2 position, out float distance_sq)
		{
			var nearest_handle = default(ILocation.Handle);
			var nearest_distance_sq = float.MaxValue;

			foreach (var asset in ILocation.Database.GetAssetsSpan())
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

		public static ref Road.Junction GetJunction(int junction_index)
		{
			if ((uint)junction_index < road_junctions.Length) return ref road_junctions[junction_index];
			else return ref Unsafe.NullRef<Road.Junction>();
		}

		public static Span<Road.Segment> GetSegments(ref this Road.Junction junction)
		{
			return junction.segments.Slice(junction.segments_count);
		}

		public static bool TryGetSegmentIndex(this Road.Junction junction, Road.Segment segment, out int index)
		{
			index = -1;
			var segments_span = junction.GetSegments();
			return segments_span.TryGetIndexOf(in segment, ref index);
		}

		public static Road.Segment GetSegment(this Road.Junction.Branch branch)
		{
			return road_junctions[branch.junction_index].segments[branch.index];
		}

		public static Road.Segment GetSegment(this Road.Chain road_chain, int index)
		{
			return new Road.Segment(road_chain, (byte)Maths.Clamp(index, 0, road_chain.GetSpan().Length - 1));
		}

		public static Road.Segment GetNearestSegment(this Road.Chain road_chain, Vector2 pos)
		{
			var span = road_chain.GetSpan();
			span.GetNearestIndex(pos, out var index, out var dist_sq);
			return new Road.Segment(road_chain, (byte)index);
		}

		public static Road.Segment GetNearestSegment(this Road.Junction.Branch branch, Vector2 pos, out Vector2 pos_segment)
		{
			var branch_segment = branch.GetSegment();
			var span = branch_segment.chain.GetSpan();
			pos_segment = pos;

			var index = (int)branch_segment.index;
			var dist_sq_nearest = float.MaxValue;

			var pos_prev = branch_segment.GetPosition();
			if (branch.sign.IsNegative())
			{
				for (var i = index; i >= 0; i--)
				{
					var pos_segment_tmp = Maths.ClosestPointOnLine(pos_prev, span[i], pos);
					var dist_sq_tmp = Vector2.DistanceSquared(pos_segment_tmp, pos);
					if (dist_sq_tmp < dist_sq_nearest)
					{
						dist_sq_nearest = dist_sq_tmp;
						pos_segment = pos_segment_tmp;						
						index = i;
					}
					pos_prev = span[i];
				}
			}
			else
			{
				for (var i = index; i < span.Length; i++)
				{
					var pos_segment_tmp = Maths.ClosestPointOnLine(pos_prev, span[i], pos);
					var dist_sq_tmp = Vector2.DistanceSquared(pos_segment_tmp, pos);
					if (dist_sq_tmp < dist_sq_nearest)
					{
						dist_sq_nearest = dist_sq_tmp;
						pos_segment = pos_segment_tmp;
						index = i;
					}
					pos_prev = span[i];
				}
			}

			return new Road.Segment(branch_segment.chain, (byte)index);
		}

		public static Vector2 GetNearestPosition(this Road.Segment road_segment, Vector2 pos, out float dist_sq)
		{
			ref var road = ref road_segment.GetRoad();
			var points = road.points.AsSpan();

			var road_index = road_segment.index;
			var dist_closest_sq = float.MaxValue;
			var pos_closest = pos;

			if (road_segment.index < points.Length - 1)
			{
				var line = new Line(points[road_index], points[road_index + 1]);
				var pos_closest_tmp = line.GetClosestPoint(pos);
			
				if (Maths.TrySetMin(ref dist_closest_sq, Vector2.DistanceSquared(pos_closest, pos_closest_tmp)))
				{
					pos_closest = pos_closest_tmp;
				}
			}

			if (road_segment.index > 0)
			{
				var line = new Line(points[road_index], points[road_index - 1]);
				var pos_closest_tmp = line.GetClosestPoint(pos);

				if (Maths.TrySetMin(ref dist_closest_sq, Vector2.DistanceSquared(pos_closest, pos_closest_tmp)))
				{
					pos_closest = pos_closest_tmp;
				}
			}

			dist_sq = dist_closest_sq;
			return pos_closest;
		}

		// TODO: this is dumb
		public static Road.Segment GetNearestRoad(Road.Type type, Vector2 position, out float distance_sq)
		{
			var h_prefecture = WorldMap.GetPrefectureAtPosition(position);
			return GetNearestRoad(h_prefecture: h_prefecture, type: type, position: position, distance_sq: out distance_sq);
		}

		// TODO: implement a faster lookup
		public static Road.Segment GetNearestRoad(this IPrefecture.Handle h_prefecture, Road.Type type, Vector2 position, out float distance_sq)
		{
			var road_points_distance_sq = float.MaxValue;
			var road_point_index = int.MaxValue;
			var road_index = int.MaxValue;

			ref var prefecture_data = ref h_prefecture.GetData();
			if (prefecture_data.IsNotNull())
			{
				var span_roads = prefecture_data.roads.AsSpan();
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
			return new Road.Segment(h_prefecture, (byte)road_index, (byte)road_point_index);
		}

#if CLIENT
		public static void DrawConnectedRoads(ref Region.Data.Global region, Road.Segment road_segment, int iter_max = 10, float budget = 1000.00f)
		{
			//var thickness = road_segment.GetRoad().scale * 0.50f;
			var alpha = 0.25f;

			var junctions_span = road_junctions.AsSpan();
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
					DrawSegment(ref region, segments_visited, junctions_queue, new(0, 0.00f, budget), ref road_segment, type, Color32BGRA.Green.WithAlphaMult(alpha), alpha, budget);
				}

				for (var i = 0; i < iter_max; i++)
				{
					var count = junctions_queue.Count;
					for (var j = 0; j < count; j++)
					{
						var connection = junctions_queue.Dequeue();
						DrawJunction(ref region, segments_visited, junctions_queue, junctions_span, in connection, i, iter_max, type, alpha, budget);
					}
				}

				//DrawJunction(hs_visited, queue, junctions_span, ref junction.segments[0], ref mat_l2c, zoom, 0, iter_max);
				static void DrawJunction(ref Region.Data.Global region, HashSet<ulong> hs_visited, Queue<RoadConnection> queue, Span<Road.Junction> junctions_span, in RoadConnection connection, int depth, int iter_max, Road.Type type, float alpha, float budget)
				{
					var color = Color32BGRA.FromHSV((1.00f - ((float)depth / (float)iter_max)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha);

					ref var junction = ref junctions_span[connection.junction_index];
					//GUI.DrawCircle(Vector2.Transform(junction.pos, mat_l2c), 0.125f * zoom, color: color, segments: 12, layer: GUI.Layer.Window);

					var segments_span = junction.segments.Slice(junction.segments_count);
					foreach (ref var segment_base in segments_span)
					{
						DrawSegment(ref region, hs_visited, queue, in connection, ref segment_base, type, color, alpha, budget);
					}
				}

				static void DrawSegment(ref Region.Data.Global region, HashSet<ulong> hs_visited, Queue<RoadConnection> queue, in RoadConnection connection, ref Road.Segment segment_base, Road.Type type, Color32BGRA color, float alpha, float budget)
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
							var key = Road.GetRoadPairKey(segment, new(segment_base.chain, (byte)(i - 1)));

							if (hs_visited.Contains(key)) break;
							hs_visited.Add(key);

							var distance = Vector2.Distance(pos_last, road_points_span[i]);
							connection_copy.distance += distance;
							connection_copy.budget -= (distance / (road.speed_mult * road.integrity));

							GUI.DrawLine(region.WorldToCanvas(pos_last) - depth_offset, region.WorldToCanvas(road_points_span[i]) - depth_offset, Color32BGRA.FromHSV((Maths.NormalizeClamp(connection_copy.budget, budget)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha), thickness: thickness * region.GetWorldToCanvasScale(), layer: GUI.Layer.Window);

							if (connection_copy.budget <= 0.00f) break;

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								GUI.DrawTextCentered($"{(connection_copy.distance * km_per_unit):0.0}km; {connection_copy.budget:0}/{budget:0}", region.WorldToCanvas(pos_last), layer: GUI.Layer.Window, box_shadow: false);

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
							var key = Road.GetRoadPairKey(new(segment_base.chain, (byte)(i + 1)), segment);

							if (hs_visited.Contains(key)) break;
							hs_visited.Add(key);

							var distance = Vector2.Distance(pos_last, road_points_span[i]);
							connection_copy.distance += distance;
							connection_copy.budget -= (distance / (road.speed_mult * road.integrity));

							GUI.DrawLine(region.WorldToCanvas(pos_last) - depth_offset, region.WorldToCanvas(road_points_span[i]) - depth_offset, Color32BGRA.FromHSV((Maths.NormalizeClamp(connection_copy.budget, budget)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha), thickness: thickness * region.GetWorldToCanvasScale(), layer: GUI.Layer.Window);

							if (connection_copy.budget <= 0.00f) break;

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								GUI.DrawTextCentered($"{(connection_copy.distance * km_per_unit):0.0}km; {connection_copy.budget:0}/{budget:0}", region.WorldToCanvas(pos_last), layer: GUI.Layer.Window, box_shadow: false);

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

		public static void DrawOutlineShader(Span<Vec2i16> points, Color32BGRA color, float thickness, Texture.Handle h_texture, bool loop = true)
		{
			var count = points.Length;

			var last_vert = default(Vec2i16);
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

		public static void DrawOutline(ref Region.Data.Global region, Span<Vec2i16> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
		{
			var count = points.Length;

			var last_vert = default(Vec2i16);
			for (var i = 0; i < (count + 1); i++)
			{
				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = (Vector2)last_vert;
					var b = (Vector2)vert;

					var ta = region.WorldToCanvas(a); // Vector2.Transform(a, mat_l2c);
					var tb = region.WorldToCanvas(b); // Vector2.Transform(b, mat_l2c);

					GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * region.GetWorldToCanvasScale(), cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void Rescale()
		{
			foreach (var asset in ILocation.Database.GetAssetsSpan())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				asset_data.point *= 2;

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IGovernorate.Database.GetAssetsSpan())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				foreach (ref var point in asset_data.points.AsSpan())
				{
					point *= 2;
				}

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IPrefecture.Database.GetAssetsSpan())
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

			foreach (var asset in IScenario.Database.GetAssetsSpan())
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

