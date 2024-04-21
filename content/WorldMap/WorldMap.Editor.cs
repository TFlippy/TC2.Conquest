
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public enum EditorMode: uint
		{
			None = 0,

			Governorate,
			Prefecture,
			Location,
			Doodad,
			Roads,
			Junctions,
			Route,

			Max
		}

#if CLIENT

		public static Road.Junction.Branch? edit_branch_a;
		public static Road.Junction.Branch? edit_branch_b;

		public static IRoute.Target? edit_route_target;

		public static void DrawBranch(ref Road.Junction.Branch branch, float size = 0.125f, Color32BGRA? color = null, int? index = null)
		{
			if (branch.IsNull() || branch.sign == 0) return;

			var junc = road_junctions[branch.junction_index];
			var seg_a = junc.segments[branch.index];
			var seg_b = seg_a;
			seg_b.index = (byte)(seg_b.index + branch.sign);


			ref var region = ref World.GetGlobalRegion();
			region.DrawDebugCircle(junc.pos, size, color ?? Color32BGRA.Magenta, filled: true);
			ref var pos = ref seg_b.GetPosition();
			if (pos.IsNotNull())
			{
				//region.DrawDebugLine(junc.pos, seg_a.GetPosition(), Color32BGRA.Cyan, 2.00f);
				//region.DrawDebugLine(junc.pos, seg_b.GetPosition(), Color32BGRA.Cyan, 2.00f);
				region.DrawDebugLine(junc.pos, pos, Color32BGRA.Yellow, 2.00f);
				region.DrawDebugText(junc.pos - new Vector2(0.00f, 0.25f), $"[{index}] #{branch.junction_index}; {seg_a.index} to {seg_b.index} (sign: {branch.sign})", Color32BGRA.White);
			}
		}

		private static void DrawEditor(ref AABB rect, ref IScenario.Data scenario_data, IAsset2<IScenario, IScenario.Data>.Definition scenario_asset, ref Mouse.Data mouse, ref Keyboard.Data kb, float zoom, ref Matrix3x2 mat_l2c, ref Vector2 mouse_local, bool hovered)
		{
			switch (editor_mode)
			{
				case EditorMode.Governorate:
				{
					if (!hovered && edit_asset == null) break;

					var governorate_handle = default(IGovernorate.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					var ts = Timestamp.Now();
					foreach (var asset in IGovernorate.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								governorate_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref governorate_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							var point = asset_data.points[index];
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (!edit_points_index.HasValue)
							{
								if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
								{
									if (mouse.GetKeyDown(Mouse.Key.Right))
									{
										if (kb.GetKey(Keyboard.Key.LeftShift))
										{
											//d_prefecture.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
											asset_data.points = asset_data.points.Insert(index, asset_data.points[index]);
											//asset.Save();
											edit_asset = asset;
											hs_pending_asset_saves.Add(asset);
										}
										else
										{
											edit_points_index = index;
											edit_points_s16 = asset_data.points;
											edit_asset = asset;

											//GUI.FocusAsset(asset.GetHandle());
											hs_pending_asset_saves.Add(asset);
											Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
										}
									}
									else if (kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.points = asset_data.points.Remove(index);
										//asset.Save();
										hs_pending_asset_saves.Add(asset);
									}
								}

								GUI.FocusableAsset(asset.GetHandle(), rect: AABB.Centered(point_t, new(1.00f * zoom)));
							}
						}
					}
				}
				break;

				case EditorMode.Prefecture:
				{
					if (!hovered && edit_asset == null) break;

					var prefecture_handle = default(IPrefecture.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					foreach (var pair in pos_hash_to_prefecture)
					{
						ref var prefecture_data = ref pair.Value.GetData();
						if (prefecture_data.IsNotNull())
						{
							var point = Unsafe.BitCast<int, short2>(pair.Key);
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = prefecture_data.color_fill;
							GUI.DrawRectFilled(AABB.Simple(point_t, new Vector2(zoom, -zoom)), color: color.WithAlphaMult(0.25f), layer: GUI.Layer.Window);
						}
					}

					var ts = Timestamp.Now();
					foreach (var asset in IPrefecture.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								prefecture_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref prefecture_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							var point = asset_data.points[index];
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (!edit_points_index.HasValue)
							{
								if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
								{
									if (mouse.GetKeyDown(Mouse.Key.Right))
									{
										if (kb.GetKey(Keyboard.Key.LeftShift))
										{
											//d_prefecture.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
											asset_data.points = asset_data.points.Insert(index, asset_data.points[index]);
											//asset.Save();
											edit_asset = asset;
											hs_pending_asset_saves.Add(asset);
										}
										else
										{
											edit_points_index = index;
											edit_points_s16 = asset_data.points;
											edit_asset = asset;

											//GUI.FocusAsset(asset.GetHandle());
											hs_pending_asset_saves.Add(asset);
											Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
										}
									}
									else if (kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.points = asset_data.points.Remove(index);
										//asset.Save();
										hs_pending_asset_saves.Add(asset);
									}
								}

								GUI.FocusableAsset(asset.GetHandle(), rect: AABB.Centered(point_t, new(1.00f * zoom)));
							}
						}
					}
				}
				break;

				case EditorMode.Location:
				{
					if (!hovered && edit_asset == null) break;

					var distance_sq = 0.00f;
					var h_location = (edit_asset as ILocation.Definition)?.GetHandle() ?? GetNearestLocation(mouse_local, out distance_sq);
					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref h_location.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							ref var point = ref asset_data.point;
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color, 0.50f);
							//GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (h_selected_location == h_location)
							{
								edit_asset = asset;

								var point_tmp = (Vector2)point;
								var scale = Vector2.One;
								var rotation = 0.00f;

								if (GUI.DrawGizmo(ref gizmo, zoom * 0.25f, ref point_tmp, ref scale, ref rotation, mat_l2c))
								{
									hs_pending_asset_saves.Add(asset);

									asset_data.point = new short2((short)MathF.Round(point_tmp.X), (short)MathF.Round(point_tmp.Y));
								}
							}
						}
					}
				}
				break;

				case EditorMode.Doodad:
				{
					if (!hovered && !edit_doodad_index.HasValue) break;

					var ts = Timestamp.Now();
					//if (hovered && edit_doodad_index.HasValue && GUI.IsMouseDoubleClicked())
					//{
					//	edit_doodad_index = null;
					//}

					ref var asset_data = ref h_scenario.GetData(out var asset);
					if (asset_data.IsNotNull())
					{
						var index = edit_doodad_index ?? -1;
						var distance_sq = 0.00f;

						var doodads = asset_data.doodads.AsSpan();

						ref var doodad = ref doodads.GetRefAtIndexOrNull(index);
						if (doodad.IsNull())
						{
							doodad = ref GetNearestDoodad(mouse_local, doodads, out index, out distance_sq);
						}
						var ts_elapsed = ts.GetMilliseconds();

						if (doodad.IsNotNull() && (edit_doodad_index.HasValue || distance_sq <= 1.00f.Pow2()))
						{
							{
								ref var point = ref doodad.position;
								var point_t = point.Transform(in mat_l2c);

								var selected = edit_doodad_index == index;
								var color = Color32BGRA.White.LumaBlend(doodad.color, 0.50f);

								//GUI.DrawCircle(point_t, 0.50f * zoom, color: color, segments: 16, layer: GUI.Layer.Foreground);
								GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

								if (hovered && !kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl | Keyboard.Key.LeftShift))
								{
									if (mouse.GetKeyDown(Mouse.Key.Left))
									{
										edit_doodad_index = index;
										hs_pending_asset_saves.Add(asset);
									}
								}

								if (selected)
								{
									edit_asset = asset;
									GUI.DrawGizmo(ref gizmo, zoom * 0.25f, ref doodad.position, ref doodad.scale, ref doodad.rotation, mat_l2c);
								}

								if (kb.GetKey(Keyboard.Key.LeftControl))
								{
									if (kb.GetKeyDown(Keyboard.Key.C))
									{
										clipboard_doodad = doodad;
										Notification.Push("Copied doodad to clipboard.", color: Color32BGRA.White, sound: "ui.copy", volume: 0.40f);
									}
								}
								else
								{
									if (hovered && kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.doodads = asset_data.doodads.Remove(index);

										hs_pending_asset_saves.Add(asset);
										edit_doodad_index = null;

										App.ScheduleGC(1);
										Notification.Push($"Deleted doodad at [{point.X:0.00}, {point.Y:0.00}].", color: Color32BGRA.Orange, sound: "ui.misc.02", volume: 0.70f, pitch: 1.00f);
									}
								}
							}
						}
					}
				}
				break;

				case EditorMode.Roads:
				{
					if (!hovered && edit_asset == null) break;

					var prefecture_handle = default(IPrefecture.Handle);
					//var distance_sq = float.MaxValue;
					//var index = int.MaxValue;

					var road_points_distance_sq = float.MaxValue;
					var road_point_index = int.MaxValue;
					var road_index = int.MaxValue;

					var ts = Timestamp.Now();
					//if (!edit_points_index.HasValue)
					{
						foreach (var asset in IPrefecture.Database.GetAssets())
						{
							if (asset.id == 0) continue;
							ref var asset_data = ref asset.GetData();

							var span_roads = asset_data.roads.AsSpan();
							for (var i = 0; i < span_roads.Length; i++)
							{
								ref var road = ref span_roads[i];
								var bb = road.bb.Grow(0.25f);

								//if (bb.ContainsPoint(mouse_local))
								{
									var points = road.points.AsSpan();
									if (!points.IsEmpty)
									{
										points.GetNearestIndex(mouse_local, out var road_nearest_index_tmp, out var road_nearest_distance_sq_tmp);

										if (road_nearest_distance_sq_tmp < road_points_distance_sq)
										{
											road_points_distance_sq = road_nearest_distance_sq_tmp;
											road_point_index = road_nearest_index_tmp;
											road_index = i;
											prefecture_handle = asset;
										}
									}
								}

								//road.UpdateBB();
								//GUI.DrawRectFilled(bb.Transform(in mat_l2c), Color32BGRA.Red.WithAlphaMult(0.05f), layer: GUI.Layer.Window);
								//GUI.DrawRect(bb.Transform(in mat_l2c), Color32BGRA.Red.WithAlphaMult(0.25f), layer: GUI.Layer.Window);
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", GUI.CanvasSize * 0.50f, layer: GUI.Layer.Foreground);

					if (road_points_distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref prefecture_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							//var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							//GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							var span_roads = asset_data.roads.AsSpan();
							if ((uint)road_index < span_roads.Length)
							{
								ref var road = ref span_roads[road_index];

								var color = Color32BGRA.White.LumaBlend(road.color_border, 0.50f);

								if (!edit_points_index.HasValue)
								{
									if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
									{
										if (mouse.GetKeyDown(Mouse.Key.Right))
										{
											GUI.FocusAsset(prefecture_handle);

											if (kb.GetKey(Keyboard.Key.LeftShift))
											{
												//d_prefecture.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
												road.points = road.points.Insert(road_point_index, road.points[road_point_index]);
												//asset.Save();
												edit_asset = asset;
												hs_pending_asset_saves.Add(asset);
											}
											else
											{
												edit_points_index = road_point_index;
												edit_points_f32 = road.points;
												edit_asset = asset;

												//GUI.FocusAsset(asset.GetHandle());
												hs_pending_asset_saves.Add(asset);
												Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
											}
										}
										else if (mouse.GetKeyDown(Mouse.Key.Left))
										{
											edit_road = new Road.Chain(prefecture_handle, (byte)road_index);
										}
										else if (kb.GetKeyDown(Keyboard.Key.Delete))
										{
											if (road.points.Length <= 1)
											{
												asset_data.roads = asset_data.roads.Remove(road_index);
											}
											else
											{
												road.points = road.points.Remove(road_point_index);
												road_point_index = Maths.Wrap(road_point_index, 0, road.points.Length);
											}
											//asset.Save();
											hs_pending_asset_saves.Add(asset);
										}
									}
								}
								else
								{
									if (mouse.GetKeyDown(Mouse.Key.Left))
									{
										var road_new = road;
										road_new.points = new Vector2[]
										{
											mouse_local + (road.points[Maths.Wrap(road_point_index - 1, 0, road.points.Length)] - mouse_local).GetNormalized().GetPerpendicular(true),
											mouse_local,
										};

										asset_data.roads = asset_data.roads.Add(road_new);
										hs_pending_asset_saves.Add(asset);
										Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);

										App.WriteLine("lmb");
									}
								}

								GUI.DrawCircleFilled(Vector2.Transform(road.points[road_point_index], mat_l2c), 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
								GUI.DrawTextCentered($"{asset_data.name}\n{road.type} [{road_index}]\n{ts_elapsed:0.0000} ms", Vector2.Transform(road.points[road_point_index], mat_l2c), layer: GUI.Layer.Foreground);
							}
						}
					}
				}
				break;

				case EditorMode.Junctions:
				{
					//var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
					//junctions_span.GetNearestIndex(mouse_local, out var junction_index, out var junction_dist_sq, (ref Road.Junction junction) => ref junction.pos);

					//if (junction_dist_sq < 1.00f.Pow2())
					//{
					//	ref var junction = ref junctions_span[junction_index];

					//	junctions_queue.Clear();
					//	segments_visited.Clear();

					//	//hs_visited.Add(junction_index);

					//	var type = junction.segments[0].GetRoad().type;

					//	var iter_max = 10;

					//	junctions_queue.Enqueue(junction_index);
					//	for (var i = 0; i < iter_max; i++)
					//	{
					//		var count = junctions_queue.Count;
					//		for (var j = 0; j < count; j++)
					//		{
					//			var junction_index_new = junctions_queue.Dequeue();
					//			DrawJunction(segments_visited, junctions_queue, junctions_span, junction_index_new, ref mat_l2c, zoom, i, iter_max, type);
					//		}
					//	}

					//	//DrawJunction(hs_visited, queue, junctions_span, ref junction.segments[0], ref mat_l2c, zoom, 0, iter_max);
					//	static void DrawJunction(HashSet<int> hs_visited, Queue<int> queue, Span<Road.Junction> junctions_span, int junction_index, ref Matrix3x2 mat_l2c, float zoom, int depth, int iter_max, Road.Type type)
					//	{
					//		//if (depth >= iter_max) return;

					//		//var junction_index = road_segment_to_junction_index[segment_from];



					//		var color = Color32BGRA.FromHSV((1.00f - ((float)depth / (float)iter_max)) * 2.00f, 1.00f, 1.00f);

					//		ref var junction = ref junctions_span[junction_index];
					//		GUI.DrawCircle(Vector2.Transform(junction.pos, mat_l2c), 0.125f * zoom, color: color, segments: 12, layer: GUI.Layer.Window);

					//		var segments_span = junction.segments.Slice(junction.segments_count);
					//		foreach (ref var segment_base in segments_span)
					//		{
					//			//if (hs_visited.Contains(segment_base.GetHashCode())) continue;
					//			//hs_visited.Add(segment_base.GetHashCode());

					//			//if (segment_base == segment_from) continue;

					//			//if (hs_visited.Contains(segment_base.GetHashCode())) continue;
					//			//hs_visited.Add(segment_base.GetHashCode());


					//			ref var road = ref segment_base.GetRoad();
					//			if (road.type != type) continue;

					//			var pos = segment_base.GetPosition();
					//			var road_points_span = road.points.AsSpan();

					//			GUI.DrawCircleFilled(Vector2.Transform(pos, mat_l2c), 0.125f * zoom * 0.50f, color: color, segments: 4, layer: GUI.Layer.Window);

					//			var depth_offset = Vector2.Zero;
					//			//var depth_offset = new Vector2(0, -4 * depth);

					//			{
					//				var pos_last = pos;
					//				for (var i = segment_base.index + 1; i < road_points_span.Length; i++)
					//				{
					//					var segment = new Road.Segment(segment_base.chain, (byte)i);

					//					if (hs_visited.Contains(segment.GetHashCode())) break;
					//					hs_visited.Add(segment.GetHashCode());

					//					GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, layer: GUI.Layer.Window);
					//					pos_last = road_points_span[i];

					//					if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
					//					{
					//						queue.Enqueue(junction_index_new);
					//						break;
					//						//DrawJunction(hs_visited, junctions_span, ref segment, ref mat_l2c, zoom, depth + 1, iter_max);
					//					}
					//				}
					//			}

					//			{
					//				var pos_last = pos;
					//				for (var i = segment_base.index - 1; i >= 0; i--)
					//				{
					//					var segment = new Road.Segment(segment_base.chain, (byte)i);

					//					if (hs_visited.Contains(segment.GetHashCode())) break;
					//					hs_visited.Add(segment.GetHashCode());

					//					GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, layer: GUI.Layer.Window);
					//					pos_last = road_points_span[i];

					//					if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
					//					{
					//						queue.Enqueue(junction_index_new);
					//						break;
					//						//DrawJunction(hs_visited, junctions_span, ref segment, ref mat_l2c, zoom, depth + 1, iter_max);
					//					}
					//				}
					//			}

					//			if (hs_visited.Contains(segment_base.GetHashCode())) continue;
					//			hs_visited.Add(segment_base.GetHashCode());

					//		}
					//	}
					//}
				}
				break;

				case EditorMode.Route:
				{
					if (!hovered && edit_asset == null) break;

					if (edit_asset is IRoute.Definition route_asset)
					{
						ref var route_data = ref route_asset.GetData();
						if (route_data.IsNotNull() && hovered)
						{
							var pos = mouse_local;

							ref var region = ref World.GetGlobalRegion();

							//var branches = route_data.branches.AsSpan();

							var ignore_limits = false;
							var dot_min = 0.40f;
							var dot_max = 1.00f;

							////var nearest_junction = road_junctions.MinBy(x => Vector2.DistanceSquared(x.pos, pos));

							ref var target = ref edit_route_target.GetRefOrNull();
							if (target.IsNotNull())
							{
								var nearest_junction = road_junctions.MinBy(x => Vector2.DistanceSquared(x.pos, pos));
								if (Vector2.DistanceSquared(nearest_junction.pos, pos) < 1.00f)
								{
									var junction_index = road_junctions.IndexOf(nearest_junction);
									var dir_ab = (pos - nearest_junction.pos).GetNormalizedFast();

									GUI.DrawCircle(Vector2.Transform(nearest_junction.pos, mat_l2c), 0.20f * zoom, color: Color32BGRA.White, segments: 8, layer: GUI.Layer.Foreground);

									//ref var current_route = ref route_

									var segment_index = -1;

									var c_alt = default(Road.Segment);
									var c_alt_sign = 0;
									var c_alt_dot = -1.00f;

									var segments = nearest_junction.segments.Slice(nearest_junction.segments_count);
									//for (var i = 0; i < segments.Length; i++)
									{
										for (var j = 0; j < segments.Length; j++)
										{
											ref var j_segment = ref segments[j];

											ref var j_road = ref j_segment.GetRoad();
											//if (j_road.IsNull() || j_road.type != type) continue;
											if (j_road.IsNull()) continue;

											var j_points = j_road.points.AsSpan();
											var j_pos = j_points[j_segment.index];

											if (j_segment.index < j_points.Length - 1)
											{
												var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
												var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

												var draw = false;
												if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
												{
													c_alt = new(j_segment.chain, (byte)(j_segment.index + 1));
													c_alt_dot = dot_tmp;
													c_alt_sign = 1;

													segment_index = (byte)(j);

													draw = true;
													region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
												}
												//if (draw || (j == branches[0].index && branches[0].sign == 1) || (j == branches[1].index && branches[1].sign == 1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == branches[0].index && branches[0].sign == 1) || (j == branches[1].index && branches[1].sign == 1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);
												//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
											}

											if (j_segment.index > 0)
											{
												var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
												var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

												var draw = false;
												if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
												{
													c_alt = new(j_segment.chain, (byte)(j_segment.index - 1));
													c_alt_dot = dot_tmp;
													c_alt_sign = -1;

													segment_index = (byte)(j);
													draw = true;
													region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
												}
												//if (draw || (j == branches[0].index && branches[0].sign == -1) || (j == branches[1].index && branches[1].sign == -1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == branches[0].index && branches[0].sign == -1) || (j == branches[1].index && branches[1].sign == -1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);
												//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
											}
										}
										//GUI.DrawLine(Vector2.Transform(nearest_junction.pos, mat_l2c), Vector2.Transform(nearest_junction.pos + ((nearest_junction.pos - segment.GetPosition()).GetNormalized() * 0.25f), mat_l2c), color: Color32BGRA.Magenta, 1.00F, layer: GUI.Layer.Foreground);
									}

									if (hovered && segment_index != -1)
									{
										if (mouse.GetKeyDown(Mouse.Key.Left))
										{
											ref var branch = ref target.branch_entry;
											branch = new Road.Junction.Branch((ushort)junction_index, (byte)segment_index, (sbyte)c_alt_sign);
										}

										//if (mouse.GetKeyDown(Mouse.Key.Left))
										//{
										//	ref var route = ref edit_branch_a.GetRefOrDefault();
										//	route = new Road.Junction.Branch((ushort)junction_index, (byte)segment_index, (sbyte)c_alt_sign);
										//}
										//else if (mouse.GetKeyDown(Mouse.Key.Right))
										//{
										//	ref var route = ref edit_branch_b.GetRefOrDefault();
										//	route = new Road.Junction.Branch((ushort)junction_index, (byte)segment_index, (sbyte)c_alt_sign);
										//}
									}
								}

								DrawBranch(ref target.branch_entry);
							}

							//DrawBranch(ref edit_branch_a.GetRefOrNull());
							//DrawBranch(ref edit_branch_b.GetRefOrNull());

							//if (false && branches != null)
							//{
							//	foreach (ref var branch in branches)
							//	{
							//		DrawBranch(ref branch);
							//	}
							//}

							//if (false)
							//{
							//	var junction_index_tmp = (int)branches[1].junction_index;
							//	var sign_tmp = branches[1].sign;
							//	var road_segment_tmp_b = road_junctions[junction_index_tmp].segments[branches[1].index];
							//	var road_segment_tmp_c = road_junctions[junction_index_tmp].segments[branches[1].index];
							//	road_segment_tmp_c.index = (byte)(road_segment_tmp_c.index + sign_tmp);

							//	GUI.DrawLine(Vector2.Transform(road_segment_tmp_b.GetPosition(), mat_l2c), Vector2.Transform(road_segment_tmp_c.GetPosition(), mat_l2c), Color32BGRA.Green, layer: GUI.Layer.Foreground, thickness: 2.00f);

							//	var random = XorRandom.New(true);

							//	for (var i = 0; i < 8; i++)
							//	{
							//		//ref var route = ref 
							//		if (TryGetNextJunction(road_segment_tmp_b, (int)sign_tmp, out junction_index_tmp, out road_segment_tmp_b, out road_segment_tmp_c))
							//		{
							//			if (junction_index_tmp != -1)
							//			{
							//				var jun = road_junctions[junction_index_tmp];
							//				var dir = (road_segment_tmp_c.GetPosition() - road_segment_tmp_b.GetPosition()).GetNormalizedFast();

							//				Span<ResolvedJunction> resolved_junctions = stackalloc ResolvedJunction[8];

							//				//ResolveJunction(dir, ref jun, ignore_limits, dot_min, dot_max, out var sign_new, out var seg_index, out var dot);
							//				Train.ResolveJunction2(dir, ref jun, ignore_limits, dot_min, dot_max, ref resolved_junctions);
							//				//var seg = jun.segments[seg_index];


							//				//ref var res = ref resolved_junctions[resolved_junctions.Length - 1];
							//				ref var res = ref resolved_junctions[0];
							//				var seg = jun.segments[res.segment_index];


							//				//var sign_new = Train.GetSign(dir, ref seg, ignore_limits, dot_min: dot_min, dot_max: dot_max);

							//				//GUI.DrawLine(Vector2.Transform(road_segment_tmp_b.GetPosition(), mat_l2c), Vector2.Transform(road_segment_tmp_c.GetPosition(), mat_l2c), Color32BGRA.Orange, layer: GUI.Layer.Foreground, thickness: 2.00f * (1 + i));
							//				//GUI.DrawLine(Vector2.Transform(jun.pos, mat_l2c), Vector2.Transform(jun.pos + (dir * 0.50f), mat_l2c), Color32BGRA.Green, layer: GUI.Layer.Foreground, thickness: 2.00f * (1 + i));
							//				GUI.DrawTextCentered($"[{i}]: {sign_tmp}; {road_segment_tmp_b.index}; {road_segment_tmp_c.index}; {jun.segments_count}", Vector2.Transform(jun.pos, mat_l2c) + new Vector2(0, 32), size: 32, layer: GUI.Layer.Foreground);

							//				road_segment_tmp_b = seg;
							//				road_segment_tmp_c = seg;
							//				road_segment_tmp_c.index = (byte)(road_segment_tmp_c.index + res.sign);

							//				var route = new Road.Junction.Branch((ushort)junction_index_tmp, res.segment_index, (sbyte)res.sign);
							//				DrawBranch(ref route);

							//				sign_tmp = (sbyte)res.sign;
							//			}
							//		}
							//		else
							//		{
							//			GUI.DrawTextCentered("fail", Vector2.Transform(road_junctions[branches[1].junction_index].pos, mat_l2c), size: 64, layer: GUI.Layer.Foreground);
							//		}
							//	}
							//}

						}
					}
				}
				break;
			}

			if (edit_points_index.TryGetValue(out var v_edit_points_index))
			{
				if (edit_points_s16 != null) edit_points_s16[v_edit_points_index] = new short2((short)MathF.Round(mouse_local.X), (short)MathF.Round(mouse_local.Y));
				if (edit_points_f32 != null) edit_points_f32[v_edit_points_index] = mouse_local;

				if (mouse.GetKeyUp(Mouse.Key.Right))
				{
					//edit_asset.Save();

					edit_points_index = null;
					edit_points_s16 = null;
					edit_points_f32 = null;
					//edit_asset = null;

					Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 0.80f);
				}
			}

			if (kb.GetKey(Keyboard.Key.LeftControl))
			{
				if (kb.GetKeyDown(Keyboard.Key.MoveDown))
				{
					if (hs_pending_asset_saves.Count > 0)
					{
						foreach (var asset in hs_pending_asset_saves)
						{
							if (asset != null)
							{
								asset.Save();
							}
						}
						hs_pending_asset_saves.Clear();
					}
				}
				//else if (kb.GetKeyDown(Keyboard.Key.C) && scenario_data.IsNotNull() && edit_doodad_index.HasValue)
				//{
				//	ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(edit_doodad_index.Value);
				//	if (doodad.IsNotNull())
				//	{
				//		clipboard_doodad = doodad;
				//		Notification.Push("Copied doodad to clipboard.", color: Color32BGRA.Yellow, sound: "ui.copy", volume: 0.40f);
				//	}
				//}
				else if (kb.GetKeyDown(Keyboard.Key.V) && scenario_data.IsNotNull() && clipboard_doodad.HasValue)
				{
					ref var doodad = ref clipboard_doodad.GetRefOrNull();
					if (doodad.IsNotNull())
					{
						var doodad_tmp = doodad;
						doodad_tmp.position = mouse_local;

						scenario_data.doodads = scenario_data.doodads.Add(doodad_tmp);
						hs_pending_asset_saves.Add(scenario_asset);

						App.ScheduleGC(1);

						Notification.Push("Pasted doodad from clipboard.", color: Color32BGRA.Green, sound: "ui.copy", volume: 0.30f, pitch: 0.70f);
					}
				}
			}

			{
				if (hs_pending_asset_saves.Count > 0)
				{
					var text_offset = 16;
					GUI.DrawTextCentered("Pending Saves:"u8, rect.GetPosition(new(0.50f, 0.00f)) + new Vector2(0, text_offset), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
					text_offset += 22;

					foreach (var asset in hs_pending_asset_saves)
					{
						if (asset != null)
						{
							GUI.DrawTextCentered(asset.Identifier, rect.GetPosition(new(0.50f, 0.00f)) + new Vector2(0, text_offset), font: GUI.Font.Superstar, size: 16, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
							text_offset += 16;
						}
					}
				}
			}

			if (editor_mode != EditorMode.None) // App.debug_mode_gui)
			{
				GUI.DrawTextCentered($"Editor: {editor_mode}", rect.GetPosition(new(0.50f, 1.00f)) - new Vector2(0, 16), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
			}
		}

		private static void DrawDebugWindow(ref AABB rect)
		{
			if (editor_mode != EditorMode.None) // App.debug_mode_gui)
			{
				using (var window = GUI.Window.Standalone("worldmap.debug"u8, position: rect.GetPosition(0, 1, new(8, -8)), size: new(300, 400), pivot: new(0.00f, 1.00f), padding: new(8), force_position: false))
				{
					if (window.show)
					{
						ref var region = ref World.GetGlobalRegion();
						var mat_l2c = region.GetWorldToCanvasMatrix();

						GUI.DrawWindowBackground();

						GUI.Title("Worldmap Debug"u8);

						GUI.SeparatorThick();

						GUI.Checkbox("Renderer", ref enable_renderer, new(32, 32), show_text: false, show_tooltip: true);

						GUI.Checkbox("Show Governorates", ref show_governorates, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Prefectures", ref show_prefectures, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Regions", ref show_regions, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Locations", ref show_locations, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Roads", ref show_roads, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Rails", ref show_rails, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Fill", ref show_fill, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Borders", ref show_borders, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Doodads", ref show_doodads, new(32, 32), show_text: false, show_tooltip: true);

						if (GUI.DrawButton("Recalculate Roads"u8, size: new Vector2(160, 40)))
						{
							RecalculateRoads();
						}

						//if (road_segments.Count > 0)
						//{
						//	foreach (var road in road_segments)
						//	{
						//		GUI.DrawCircleFilled(Vector2.Transform(road.Value.GetPosition(), )
						//	}
						//}

						//if (GUI.DrawButton("Rescale", size: new Vector2(100, 40)))
						//{
						//	Rescale();
						//}

						//GUI.EnumInputMasked("worldmap.filter.roads", ref filter_roads, new Vector2(GUI.RmX, 32), filter_roads_mask, show_none: true, show_all: true, close_on_select: false);
						//GUI.EnumInput("worldmap.filter.roads", ref filter_roads, new Vector2(GUI.RmX, 32), filter_roads_mask, show_none: true, show_all: true, close_on_select: false);

						GUI.SeparatorThick();

						using (var scrollbox = GUI.Scrollbox.New("worldmap.scroll.editor"u8, size: GUI.Rm))
						{
							switch (editor_mode)
							{
								case EditorMode.Roads:
								{
									if (edit_road.h_prefecture != 0)
									{
										ref var road = ref edit_road.GetRoad();
										if (road.IsNotNull())
										{
											ref var prefecture_data = ref edit_road.h_prefecture.GetData();
											if (prefecture_data.IsNotNull())
											{
												var changed = GUI.DrawStyledEditorForType(ref road, new Vector2(GUI.RmX, 32));
												if (changed)
												{
													hs_pending_asset_saves.Add(edit_road.h_prefecture.GetDefinition());
												}

												var road_points_span = road.points.AsSpan();
												GUI.DrawLines(road_points_span, in mat_l2c, prefecture_data.color_border, layer: GUI.Layer.Foreground, draw_points: true, draw_indices: true);

												//var pos_last = Vector2.Zero;

												//for (var i = 0; i < road_points_span.Length; i++)
												//{
												//	var pos = Vector2.Transform(road_points_span[i], mat_l2c);

												//	if (i > 0) GUI.DrawLine(pos_last, pos, prefecture_data.color_border, layer: GUI.Layer.Foreground);
												//	GUI.DrawCircleFilled(pos, 0.125f * zoom * 0.75f, road.color_border.WithAlphaMult(0.75f), 3, GUI.Layer.Foreground);
												//	GUI.DrawTextCentered($"[{i}]", pos, layer: GUI.Layer.Foreground);

												//	pos_last = pos;
												//}
											}
										}
									}
									//if ()
								}
								break;

								case EditorMode.Junctions:
								{
									//var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
									//junctions_span.GetNearestIndex
								}
								break;

								case EditorMode.Prefecture:
								{
									var mouse_pos_world = region.CanvasToWorld(mouse_pos_new);
									var road_nearest = GetNearestRoad(Road.Type.Road, mouse_pos_world, out var dist_sq);
									if (road_nearest.IsValid())
									{
										var hovered_prefecture = WorldMap.GetPrefectureAtPosition(mouse_pos_world);


										GUI.DrawCircleFilled(region.WorldToCanvas(road_nearest.GetPosition()), 10.00f, Color32BGRA.Magenta, layer: GUI.Layer.Foreground);
										GUI.DrawTextCentered($"{hovered_prefecture}\n{road_nearest.index}", mouse_pos_new, layer: GUI.Layer.Foreground);
									}
									//GUI.DrawTextCentered($"{road_nearest.index}", mouse_pos_new + new Vector2(0, -16), layer: GUI.Layer.Foreground);
								}
								break;

								case EditorMode.Doodad:
								{
									if (edit_doodad_index.TryGetValue(out var index_doodad))
									{
										ref var scenario_data = ref h_scenario.GetData(out var scenario_asset);
										if (scenario_data.IsNotNull())
										{
											ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(index_doodad);
											if (doodad.IsNotNull())
											{
												var changed = GUI.DrawStyledEditorForType(ref doodad, new Vector2(GUI.RmX, 32), show_label: true);
												if (changed)
												{
													hs_pending_asset_saves.Add(scenario_asset);
												}

											}
										}
									}
								}
								break;

								case EditorMode.Route:
								{
									var h_route = (edit_asset as IRoute.Definition)?.GetHandle() ?? default;
									if (GUI.AssetInput("editor.route", ref h_route, new(200, 32)))
									{
										edit_asset = h_route.GetDefinition();
									}
									GUI.FocusableAsset(h_route);

									if (edit_asset is IRoute.Definition route)
									{
										ref var route_data = ref route.GetData();
										if (route_data.IsNotNull())
										{
											//App.WriteLine($"{edit_route_target.HasValue}");

											if (GUI.DrawButton("Generate Path", size: new Vector2(128, 40)))
											{
												Span<Road.Junction.Branch> branches = stackalloc Road.Junction.Branch[32];

												if (RoadNav.Astar.TryFindPath(edit_branch_a.Value, edit_branch_b.Value, ref branches))
												{
													App.WriteLine($"result: {branches.Length}");

													//route_data.branches = branches.ToArray();
												}
											}

											var changed = false;
											if (changed = GUI.DrawStyledEditorForType(ref route_data, new Vector2(GUI.GetRemainingWidth(), 32), false))
											{

											}

											if (h_selected_location.IsValid())
											{
												var targets = route_data.targets.AsSpan();
												for (var i = 0; i < targets.Length; i++)
												{
													ref var target = ref targets[i];
													if (target.h_location == h_selected_location)
													{
														ref var target_current = ref edit_route_target.GetRefOrNull();
														if (target_current.IsNotNull() && target_current.h_location == h_selected_location)
														{
															if (!changed) target = target_current;
														}
														edit_route_target = target;
													}
												}
											}

											//ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(index_doodad);
											//if (doodad.IsNotNull())
											//{
											//	var changed = GUI.DrawStyledEditorForType(ref doodad, new Vector2(GUI.RmX, 32));
											//	if (changed)
											//	{
											//		hs_pending_asset_saves.Add(route);
											//	}
											//}
										}
									}
								}
								break;

								default:
								{
									GUI.SliderFloat("DEV: Scale A", ref IScenario.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32), snap: 0.001f);
									GUI.SliderFloat("DEV: Scale B", ref IScenario.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32), snap: 0.001f);

									GUI.SliderFloat("DEV: Size.X", ref worldmap_window_size.X, 1.00f, 1920.00f, new(GUI.RmX * 0.50f, 32), snap: 8);
									GUI.SameLine();
									GUI.SliderFloat("DEV: Size.Y", ref worldmap_window_size.Y, 1.00f, 1920.00f, new(GUI.RmX, 32), snap: 8);

									GUI.SliderFloat("DEV: Offset.X", ref worldmap_window_offset.X, -512.00f, 512.00f, new(GUI.RmX * 0.50f, 32), snap: 1);
									GUI.SameLine();
									GUI.SliderFloat("DEV: Offset.Y", ref worldmap_window_offset.Y, -512.00f, 512.00f, new(GUI.RmX, 32), snap: 1);
								}
								break;
							}
						}
					}
				}
			}
		}
#endif
	}
}

