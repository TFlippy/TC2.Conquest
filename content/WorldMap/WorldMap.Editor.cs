﻿
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public enum EditorMode: uint
		{
			None = 0,

			Province,
			District,
			Location,
			Doodad,
			Roads,
			Junctions,

			Max
		}

#if CLIENT
		private static void DrawEditor(ref AABB rect, ref IScenario.Data scenario_data, IAsset2<IScenario, IScenario.Data>.Definition scenario_asset, ref Mouse.Data mouse, ref Keyboard.Data kb, float zoom, ref Matrix3x2 mat_l2c, ref Vector2 mouse_local, bool hovered)
		{
			switch (editor_mode)
			{
				case EditorMode.Province:
				{
					if (!hovered && edit_asset == null) break;

					var province_handle = default(IProvince.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					var ts = Timestamp.Now();
					foreach (var asset in IProvince.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								province_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref province_handle.GetData(out var asset);
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
											//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
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

				case EditorMode.District:
				{
					if (!hovered && edit_asset == null) break;

					var district_handle = default(IDistrict.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					var ts = Timestamp.Now();
					foreach (var asset in IDistrict.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								district_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref district_handle.GetData(out var asset);
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
											//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
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

					ref var asset_data = ref h_world.GetData(out var asset);
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

					var district_handle = default(IDistrict.Handle);
					//var distance_sq = float.MaxValue;
					//var index = int.MaxValue;

					var road_points_distance_sq = float.MaxValue;
					var road_point_index = int.MaxValue;
					var road_index = int.MaxValue;

					var ts = Timestamp.Now();
					//if (!edit_points_index.HasValue)
					{
						foreach (var asset in IDistrict.Database.GetAssets())
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
											district_handle = asset;
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
						ref var asset_data = ref district_handle.GetData(out var asset);
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
											GUI.FocusAsset(district_handle);

											if (kb.GetKey(Keyboard.Key.LeftShift))
											{
												//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
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
											edit_road = new Road.Chain(district_handle, (byte)road_index);
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
					GUI.DrawTextCentered("Pending Saves:", rect.GetPosition(new(0.50f, 0.00f)) + new Vector2(0, text_offset), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
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

		private static void DrawDebugWindow(ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			if (editor_mode != EditorMode.None) // App.debug_mode_gui)
			{
				using (var window = GUI.Window.Standalone("worldmap.debug", position: rect.GetPosition(0, 1, new(8, -8)), size: new(300, 400), pivot: new(0.00f, 1.00f), padding: new(8), force_position: false))
				{
					if (window.show)
					{
						GUI.DrawWindowBackground();

						GUI.Title("Worldmap Debug");

						GUI.SeparatorThick();

						GUI.Checkbox("Renderer", ref enable_renderer, new(32, 32), show_text: false, show_tooltip: true);

						GUI.Checkbox("Show Provinces", ref show_provinces, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Districts", ref show_districts, new(32, 32), show_text: false, show_tooltip: true);
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

						if (GUI.DrawButton("Recalculate Roads", size: new Vector2(160, 40)))
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

						using (var scrollbox = GUI.Scrollbox.New("worldmap.scroll.editor", size: GUI.Rm))
						{
							switch (editor_mode)
							{
								case EditorMode.Roads:
								{
									if (edit_road.h_district != 0)
									{
										ref var road = ref edit_road.GetRoad();
										if (road.IsNotNull())
										{
											ref var district_data = ref edit_road.h_district.GetData();
											if (district_data.IsNotNull())
											{
												var changed = GUI.DrawStyledEditorForType(ref road, new Vector2(GUI.RmX, 32));
												if (changed)
												{
													hs_pending_asset_saves.Add(edit_road.h_district.GetDefinition());
												}

												var road_points_span = road.points.AsSpan();
												GUI.DrawLines(road_points_span, in mat_l2c, district_data.color_border, layer: GUI.Layer.Foreground, draw_points: true, draw_indices: true);

												//var pos_last = Vector2.Zero;

												//for (var i = 0; i < road_points_span.Length; i++)
												//{
												//	var pos = Vector2.Transform(road_points_span[i], mat_l2c);

												//	if (i > 0) GUI.DrawLine(pos_last, pos, district_data.color_border, layer: GUI.Layer.Foreground);
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

								case EditorMode.Doodad:
								{
									if (edit_doodad_index.TryGetValue(out var index_doodad))
									{
										ref var scenario_data = ref h_world.GetData(out var scenario_asset);
										if (scenario_data.IsNotNull())
										{
											ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(index_doodad);
											if (doodad.IsNotNull())
											{
												var changed = GUI.DrawStyledEditorForType(ref doodad, new Vector2(GUI.RmX, 32));
												if (changed)
												{
													hs_pending_asset_saves.Add(scenario_asset);
												}

											}
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
