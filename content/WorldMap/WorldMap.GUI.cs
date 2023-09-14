
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{

#if CLIENT
		//[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnGUIEarly(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		//{
		//	App.WriteLine("GUI World Begin");
		//}

		//[ISystem.VeryLateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnGUILate(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		//{
		//	App.WriteLine("GUI World End");
		//}

		//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnTickEarly(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		//{
		//	App.WriteLine("Tick World Begin");
		//}

		//[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnTickLate(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		//{
		//	App.WriteLine("Tick World End");
		//}

		[ISystem.PreRender(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnPreRender(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			IScenario.WorldMap.Renderer.Clear();
			Doodad.Renderer.Clear();

			//App.WriteLine("OnPreRender");
		}

		[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnRender(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			//App.WriteLine("OnRender");
		}

		[ISystem.PostRender(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnPostRender(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			IScenario.WorldMap.Renderer.Submit();
			Doodad.Renderer.Submit();

			//App.WriteLine("OnPostRender");
		}

		public static Timestamp ts_last_draw;

		public static void Draw(Vector2 size)
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();
			ref var region = ref world.GetGlobalRegion();
			if (region.IsNull()) return;

			ts_last_draw = Timestamp.Now();

			//var use_renderer = true;

			//if (!Client.IsLoadingRegion())

			//if (!is_loading)

			//if (enable_renderer)
			//{
			//	IScenario.WorldMap.Renderer.Clear();
			//	IScenario.Doodad.Renderer.Clear();
			//}

			var mouse = GUI.GetMouse();
			var kb = GUI.GetKeyboard();

			var zoom = MathF.Pow(2.00f, worldmap_zoom_current);
			var zoom_inv = 1.00f / zoom;

			var mat_l2c = Matrix3x2.Identity;
			var mat_c2l = Matrix3x2.Identity;

			using (var group_canvas = GUI.Group.New(size))
			{
				var rect = group_canvas.GetInnerRect();

				using (GUI.ID.Push("worldmap"))
				{
					GUI.DrawWindowBackground(GUI.tex_window_character);
					//sb.Clear();

					ref var scenario_data = ref h_world.GetData(out var scenario_asset);
					var disable_input = false;

					if (is_loading)
					{
						mouse = default;
						kb = default;
					}

					disable_input |= gizmo.IsActive() || gizmo.IsHovered();
					if (disable_input)
					{
						//mouse.ClearKeys();
						//kb.Clear();

						dragging = false;
						mouse.SetKeyState(~(Mouse.Key.ScrollUp | Mouse.Key.ScrollDown), false);
						kb.SetKeyState(~(Keyboard.Key.MoveDown | Keyboard.Key.MoveLeft | Keyboard.Key.MoveRight | Keyboard.Key.MoveUp | Keyboard.Key.LeftControl | Keyboard.Key.C | Keyboard.Key.V), false);
					}

					if (editor_mode != EditorMode.None && !gizmo.IsHovered() && GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false) && edit_asset != null && GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
					{
						edit_asset = null;
						h_selected_location = default;
						edit_doodad_index = null;
					}

					var rect_center = rect.GetPosition();

					using (GUI.Clip.Push(rect))
					{
						var scale_b = IScenario.WorldMap.scale_b;
						var snap_delta = (worldmap_offset_current_snapped - worldmap_offset_current) * 1.0f;

						mat_l2c = Maths.TRS3x2((worldmap_offset_current * -zoom) + rect.GetPosition(new(0.50f)) - snap_delta, rotation, new Vector2(zoom));
						Matrix3x2.Invert(mat_l2c, out mat_c2l);

						var mat_l2c2 = Maths.TRS3x2(rect.GetPosition(new Vector2(0.50f)), rotation, new Vector2(1));
						Matrix3x2.Invert(mat_l2c2, out var mat_c2l2);

						region.GetWorldToCanvasMatrix() = mat_l2c;
						region.GetCanvasToWorldMatrix() = mat_c2l;

						region.GetWorldToCanvasScale() = zoom * 0.50f;
						region.GetCanvasToWorldScale() = 1.00f / region.GetWorldToCanvasScale();

						var snap_delta_canvas = snap_delta * zoom;

						var tex_scale = enable_renderer ? (IScenario.WorldMap.worldmap_size.X / scale_b) * zoom : 16.00f;
						var tex_scale_inv = 1.00f / tex_scale;

						var color_grid = new Color32BGRA(0xff4eabb5);
						//GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), clip: false);

						if (enable_renderer)
						{
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), clip: false);
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);

							GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) / 16, uv_1: Vector2.Transform(rect.b, mat_c2l) / 16, clip: false, color: color_grid.WithAlphaMult(0.25f));

							GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);
						}
						else
						{
							GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						}

						var mouse_pos = GUI.GetMousePosition();
						var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
						var mouse_local_snapped = mouse_local;
						mouse_local_snapped.Snap(1.00f / scale_b, out mouse_local_snapped);

						var tex_line_prefecture = h_texture_line_00;
						var tex_line_governorate = h_texture_line_01;

						#region Prefectures
						//if (editor_mode != EditorMode.Doodad)
						{
							foreach (var asset in IPrefecture.Database.GetAssets())
							{
								if (asset.id == 0) continue;
								ref var asset_data = ref asset.GetData();

								var points = asset_data.points.AsSpan();
								if (!points.IsEmpty)
								{
									var pos_center = Vector2.Zero;

									Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
									for (var i = 0; i < points.Length; i++)
									{
										var point = (Vector2)points[i];
										pos_center += point;
										var point_t = point.Transform(in mat_l2c);

										points_t_span[i] = point_t;
									}
									pos_center /= points.Length;
									pos_center += asset_data.offset;

									if (show_prefectures && show_fill) GUI.DrawPolygon(points_t_span, asset_data.color_fill with { a = 50 }, GUI.Layer.Window);

									//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
									if (enable_renderer)
									{
										if (show_prefectures && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, asset_data.border_scale * 0.50f, 2.00f, asset_data.h_texture_border);

										//if (show_roads)
										{
											var roads_span = asset_data.roads.AsSpan();
											foreach (ref var road in roads_span)
											{
												if (road.type == Road.Type.Road && !show_roads) continue;
												if (road.type == Road.Type.Rail && !show_rails) continue;

												DrawOutlineShader(road.points, road.color_border, road.scale, road.h_texture, loop: false);
											}
										}
									}
									else
									{
										if (show_prefectures && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, asset_data.h_texture_border);
									}

									//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 1.00f * zoom, color: GUI.font_color_title.WithAlphaMult(1.00f), layer: GUI.Layer.Window);
									if (show_prefectures) GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 0.75f * zoom * asset_data.size, color: asset_data.color_fill.WithColorMult(0.32f).WithAlphaMult(0.30f), layer: GUI.Layer.Window);
								}
							}
						}
						#endregion

						#region Governorates
						foreach (var asset in IGovernorate.Database.GetAssets())
						{
							if (asset.id == 0) continue;
							ref var asset_data = ref asset.GetData();

							var points = asset_data.points.AsSpan();
							if (!points.IsEmpty)
							{
								if (enable_renderer)
								{
									if (show_governorates && show_borders) DrawOutlineShader(points, asset_data.color_border, asset_data.border_scale, asset_data.h_texture_border);
								}
								else
								{
									if (show_governorates && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f, 0.25f, asset_data.h_texture_border);
								}

								var pos_center = Vector2.Zero;
								var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);

								for (var i = 0; i < points.Length; i++)
								{
									var point = (Vector2)points[i];
									pos_center += point;
								}
								pos_center /= points.Length;
							}
						}
						#endregion

						#region Regions
						ref var world_info = ref Client.GetWorldInfo();
						if (world_info.IsNotNull())
						{
							//if (world_info.regions != null)
							{
								var mod_context = App.GetModContext();

								for (var i = 1; i < Region.max_count; i++)
								{
									using (GUI.ID.Push(i + 10000))
									{
										//ref var map_info = ref world_info.[i];

										ref var region_info = ref World.GetRegionInfo((byte)i);

										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (region_info.IsValid() && map_info.IsNotNull())
										{
											var icon_size = 0.75f; // * (zoom_inv * 128);
											var color = GUI.font_color_title;
											var color_frame = GUI.col_frame;
											var color_thumbnail = GUI.col_white;

											var map_pos = Vector2.Zero;
											ref var location_data = ref map_info.h_location.GetData();
											if (location_data.IsNotNull())
											{
												map_pos = (Vector2)location_data.point;
											}

											var rect_text = AABB.Centered(Vector2.Transform(map_pos + map_info.text_offset, mat_l2c), new Vector2(icon_size * zoom * 0.50f));
											var rect_icon = AABB.Centered(Vector2.Transform(map_pos + map_info.icon_offset, mat_l2c), new Vector2(icon_size * zoom * 1.00f));
											var is_selected = selected_region_id == i || (location_data.IsNotNull() && h_selected_location == map_info.h_location);

											var alpha = 0.50f;
											if (is_selected)
											{
												alpha = 1.00f;
												color_frame = GUI.col_white;
												selected_region_id = (byte)i;
											}

											if (show_regions)
											{
												var map_asset = mod_context.GetMap(region_info.map);
												if (map_asset != null)
												{
													var tex_thumbnail = map_asset.GetThumbnail();
													if (tex_thumbnail != null)
													{
														GUI.DrawTexture(tex_thumbnail.Identifier, rect_icon, GUI.Layer.Window, color: color_thumbnail.WithAlphaMult(alpha));
													}

													GUI.DrawBackground(GUI.tex_frame_white, rect_icon, padding: new(2), color: color_frame.WithAlphaMult(alpha));
												}
											}
										}
									}
								}
							}
						}
						#endregion

						#region Locations
						foreach (var asset in ILocation.Database.GetAssets())
						{
							if (asset.id == 0) continue;

							ref var asset_data = ref asset.GetData();
							if (asset_data.flags.HasAny(ILocation.Flags.Hidden) || asset_data.h_location_parent.id != 0) continue;

							var pos = (Vector2)asset_data.point;
							var scale = 0.500f;
							var asset_scale = Maths.Clamp(asset_data.size, 0.50f, 1.00f);

							var rect_text = AABB.Centered(Vector2.Transform(pos + asset_data.text_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));
							var rect_icon = AABB.Centered(Vector2.Transform(pos + asset_data.icon_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));

							var is_selected = h_selected_location == asset;
							var is_pressed = GUI.ButtonBehavior(asset_data.name, rect_icon, out var is_hovered, out var is_held);

							var color = (is_selected || is_hovered) ? Color32BGRA.White : asset_data.color;

							if (is_hovered)
							{
								GUI.SetCursor(App.CursorType.Hand, 1000);

								if (is_pressed)
								{
									selected_region_id = 0;

									if (is_selected)
									{
										WorldMap.selected_entity = default;
										h_selected_location = default;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
									}
									else
									{
										WorldMap.selected_entity = asset.GetEntity();
										h_selected_location = asset;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
									}
									// Client.RequestSetActiveRegion((byte)i);
								}
							}

							//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.625f * asset_scale) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

							if (show_locations)
							{
								GUI.DrawSpriteCentered(asset_data.icon, rect_icon, layer: GUI.Layer.Window, 0.125f * MathF.Max(scale * zoom * asset_scale, 16), color: color);
								GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.50f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

								if ((is_selected || is_hovered) && editor_mode == EditorMode.Roads)
								{
									//var ts = Timestamp.Now();
									//var nearest_road = GetNearestRoad(asset_data.h_prefecture, Road.Type.Road, (Vector2)asset_data.point, out var nearest_road_dist_sq);
									//var nearest_rail = GetNearestRoad(asset_data.h_prefecture, Road.Type.Rail, (Vector2)asset_data.point, out var nearest_rail_dist_sq);
									//var ts_elapsed = ts.GetMilliseconds();

									//if (nearest_road_dist_sq <= 1.50f.Pow2())

									if (location_to_road.TryGetValue(asset, out var nearest_road))
									{
										GUI.DrawCircleFilled(nearest_road.GetPosition().Transform(in mat_l2c), 0.125f * zoom * 0.50f, Color32BGRA.Yellow, 8, GUI.Layer.Window);
										if (Maths.IsInDistance(mouse_local, nearest_road.GetPosition(), 0.25f))
										{
											DrawConnectedRoads(nearest_road, ref mat_l2c, zoom, iter_max: 50, budget: 30.00f);
										}
									}

									//if (nearest_rail_dist_sq <= 1.00f.Pow2())

									if (location_to_rail.TryGetValue(asset, out var nearest_rail))
									{
										GUI.DrawCircleFilled(nearest_rail.GetPosition().Transform(in mat_l2c), 0.125f * zoom * 0.50f, Color32BGRA.Orange, 8, GUI.Layer.Window);
										if (Maths.IsInDistance(mouse_local, nearest_rail.GetPosition(), 0.25f))
										{
											DrawConnectedRoads(nearest_rail, ref mat_l2c, zoom, iter_max: 50, budget: 100.00f);
										}
									}
									//GUI.Text($"nearest in {ts_elapsed:0.0000} ms");
								}
							}

							GUI.FocusableAsset(asset.GetHandle(), rect: rect_icon);
						}
						#endregion

						#region Overlays
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Window, color: Color32BGRA.White.WithAlphaMult(0.30f));
						//GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 72, 72, 0, 1), rect: AABB.Centered(Vector2.Transform(mouse_local_snapped, mat_l2c), new Vector2(0.25f)), layer: GUI.Layer.Window, scale: 0.125f * 0.50f * zoom, color: Color32BGRA.Black.WithAlphaMult(1));
						//GUI.DrawTextCentered($"Zoom: {zoom:0.00}x\ndelta: [{snap_delta.X:0.000000}, {snap_delta.Y:0.000000}]\ndelta.c: [{snap_delta_canvas.X:0.000000}, {snap_delta_canvas.Y:0.000000}]\ncam: [{worldmap_offset_target.X:0.000000}, {worldmap_offset_target.Y:0.000000}]\ncam.s: [{worldmap_offset_current_snapped.X:0.000000}, {worldmap_offset_current_snapped.Y:0.000000}]\nmouse.l: [{mouse_local.X:0.0000}, {mouse_local.Y:0.0000}]\nmouse: [{mouse_pos.X:0.00}, {mouse_pos.Y:0.00}]", position: rect.GetPosition(new(1, 1)), new(1, 1), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground);
						#endregion

						if (editor_mode != EditorMode.None)
						{
							if (road_junctions.Count > 0)
							{
								var ts = Timestamp.Now();
								foreach (var junction in road_junctions)
								{
									var pos_c = Vector2.Transform(junction.pos, mat_l2c);

									GUI.DrawCircleFilled(pos_c, 0.125f * zoom, junction.segments_count > 1 ? Color32BGRA.Green : Color32BGRA.Yellow.WithAlphaMult(0.250f), 4, GUI.Layer.Window);
									GUI.DrawTextCentered($"{junction.segments_count}", pos_c, layer: GUI.Layer.Window);
								}
								var ts_elapsed = ts.GetMilliseconds();
								GUI.DrawTextCentered($"{ts_elapsed:0.000} ms", GUI.CanvasSize * 0.75f, layer: GUI.Layer.Foreground);
							}
						}

						var hovered = GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false);

						#region Editor
						DrawEditor(ref rect, ref scenario_data, scenario_asset, ref mouse, ref kb, zoom, ref mat_l2c, ref mouse_local, hovered);
						#endregion

						#region Camera
						var mouse_delta = Vector2.Zero;
						mouse_pos_old = mouse_pos_new;
						mouse_pos_new = mouse_pos;

						if (hovered)
						{
							//if (mouse.GetKey(Mouse.Key.Left) || mouse.GetKeyDown(Mouse.Key.Left))
							if (mouse.GetKeyDown(Mouse.Key.Left))
							{
								//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;

								mouse_pos_old = mouse_pos;
								mouse_pos_new = mouse_pos;

								dragging = true;
							}
						}

						mouse_delta = mouse_pos_old - mouse_pos_new;

						if (mouse.GetKeyUp(Mouse.Key.Left))
						{
							dragging = false;
						}

						//if (!disable_input)
						{
							if (dragging)
							{
								//worldmap_offset += mouse_delta * zoom_inv;
								//momentum = mouse_delta; // Vector2.Lerp(momentum, mouse_delta, 0.80f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
								if (enable_momentum) momentum = Vector2.Lerp(momentum, mouse_delta, 0.50f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
								worldmap_offset_target += (mouse_delta * zoom_inv); // + (momentum * zoom_inv * 0.10f);
								worldmap_offset_current = Vector2.Lerp(worldmap_offset_current, worldmap_offset_target, 0.25f);

								//worldmap_offset += momentum * zoom_inv * 0.10f;
							}
							else
							{
								if (enable_momentum) momentum = Vector2.Lerp(momentum, Vector2.Zero, 0.10f);
								worldmap_offset_target += momentum * zoom_inv;
								worldmap_offset_current = Vector2.Lerp(worldmap_offset_current, worldmap_offset_target, 0.25f);
								if (!enable_momentum) momentum = default;
							}

							if (hovered && !kb.GetKey(Keyboard.Key.LeftShift))
							{
								worldmap_zoom_target -= mouse.GetScroll(0.25f);
								//worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 8.00f);
								worldmap_zoom_target = Maths.Clamp(worldmap_zoom_target, 1.00f, 7.00f);
							}
						}

						worldmap_zoom_current = Maths.Lerp(worldmap_zoom_current, worldmap_zoom_target, 0.20f);

						if (enable_renderer)
						{
							worldmap_offset_current_snapped = Maths.SnapFloor(worldmap_offset_current, 1 / scale_b);
							IScenario.WorldMap.Renderer.UpdateCamera(worldmap_offset_current_snapped, 0.00f, new Vector2(1));

							ref var world_data = ref h_world.GetData();
							if (world_data.IsNotNull())
							{
								if (world_data.doodads != null && show_doodads)
								{
									Doodad.Renderer.Add(world_data.doodads.AsSpan());
								}
							}

							//IScenario.WorldMap.Renderer.Submit();
							//IScenario.Doodad.Renderer.Submit();
						}

						//if (!disable_input)
						{
							if (hovered && !gizmo.IsHovered())
							{
								if (kb.GetKey(Keyboard.Key.LeftShift))
								{
									var scroll = mouse.GetScroll();
									if (scroll != 0.00f)
									{
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
										editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) - (int)(scroll), 0, (int)EditorMode.Max);
										edit_asset = null;
										edit_doodad_index = null;
									}
									//else
									//{
									//	worldmap_offset_current = default;
									//	worldmap_offset_current_snapped = default;
									//	worldmap_offset_target = default;
									//	momentum = default;
									//	rotation = default;
									//}
								}

								//if (kb.GetKeyDown(Keyboard.Key.Tab))
								//{
								//	editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) + 1, 0, (int)EditorMode.Max);
								//	edit_asset = null;
								//}
							}

							var move_speed = (1.00f / MathF.Sqrt(worldmap_zoom_current)) * 10.00f;

							if (!kb.GetKey(Keyboard.Key.LeftControl))
							{
								if (kb.GetKey(Keyboard.Key.MoveLeft))
								{
									//worldmap_offset.X += move_speed;
									momentum.X -= move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveRight))
								{
									//worldmap_offset.X -= move_speed;
									momentum.X += move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveUp))
								{
									//worldmap_offset.Y += move_speed;
									momentum.Y -= move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveDown))
								{
									//worldmap_offset.Y -= move_speed;
									momentum.Y += move_speed;
								}
							}
						}
						#endregion

						if (dragging && GUI.IsHovered)
						{
							GUI.SetHoveredID("worldmap");
						}
					}
				}

				#region Left
				DrawLeftWindow(is_loading, ref rect, zoom, ref mat_l2c);
				#endregion

				#region Right
				DrawRightWindow(is_loading, ref rect, zoom, ref mat_l2c);
				#endregion

				#region Debug			
				DrawDebugWindow(ref rect, zoom, ref mat_l2c);
				#endregion
			}
		}

		private static void DrawLeftWindow(bool loading, ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			using (var window = GUI.Window.Standalone("worldmap.side.left", position: new Vector2(rect.a.X, rect.a.Y) + new Vector2(6, 12), size: new(284, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
			{
				if (window.show)
				{
					window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

					var mod_context = App.GetModContext();

					ref var world_info = ref Client.GetWorldInfo();

					using (GUI.Group.New(size: GUI.Rm))
					{
						//GUI.Checkbox("DEV: Renderer", ref use_renderer, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale A", ref IScenario.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale B", ref IScenario.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32));

						GUI.SeparatorThick();

						using (var scrollbox = GUI.Scrollbox.New("sb.worldmap.left", GUI.Rm))
						{
							using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
							{
								using (GUI.ID.Push("regions"))
								{
									using (var collapsible = GUI.Collapsible2.New("regions", new Vector2(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Regions", size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner(padding: new Vector4(6, 0, 0, 0)))
										{
											var region_list_span = World.GetRegionList().AsSpan();
											for (var i = 1; i < region_list_span.Length; i++)
											{
												ref var region_info = ref region_list_span[i];
												ref var map_info = ref region_info.map_info.GetRefOrNull();

												if (region_info.IsValid() && map_info.IsNotNull())
												{
													ref var location_data = ref map_info.h_location.GetData();
													if (location_data.IsNotNull())
													{
														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
														{
															if (group_row.IsVisible())
															{
																GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmY));

																group_row.DrawBackground(GUI.tex_panel);

																GUI.SameLine();

																using (GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
																{
																	GUI.TitleCentered(map_info.name, size: 18, pivot: new(0.00f, 0.00f));
																}

																var selected = selected_region_id == i;
																if (GUI.Selectable3((uint)i, group_row.GetOuterRect(), selected))
																{
																	if (selected)
																	{
																		selected_region_id = 0;
																		h_selected_location = default;
																	}
																	else
																	{
																		selected_region_id = (byte)i;
																		h_selected_location = map_info.h_location;
																		worldmap_offset_target = (Vector2)location_data.point;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}

							using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
							{
								using (GUI.ID.Push("locations"))
								{
									using (var collapsible = GUI.Collapsible2.New("locations", new Vector2(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Locations", size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
										{
											foreach (var asset in ILocation.Database.GetAssets())
											{
												if (asset.id == 0) continue;

												ref var asset_data = ref asset.GetData();

												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 4)))
												{
													if (group_row.IsVisible())
													{
														group_row.DrawBackground(GUI.tex_panel);

														GUI.TitleCentered(asset_data.name, size: 20, pivot: new(0.00f, 0.50f));
														GUI.TextShadedCentered(asset_data.type.GetEnumName(), size: 14, pivot: new(1.00f, 0.50f), color: GUI.font_color_desc);

														var selected = asset == h_selected_location; // selected_region_id == i;
														if (GUI.Selectable3((uint)asset.id, group_row.GetOuterRect(), selected))
														{
															selected_region_id = 0;

															if (selected)
															{
																h_selected_location = default;
															}
															else
															{
																h_selected_location = asset;
																worldmap_offset_target = (Vector2)asset_data.point;
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static void DrawRightWindow(bool is_loading, ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			if (selected_region_id != 0 || h_selected_location != 0)
			{
				//var draw_external = true;

				using (var window = GUI.Window.Standalone("worldmap.side.right", position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(322, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
				{
					using (var dock = GUI.Dock.New((uint)WorldMap.selected_entity.id))
					{
						if (window.show)
						{
							window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

							if (h_selected_location != 0)
							{
								ref var location_data = ref h_selected_location.GetData(out var location_asset);
								if (location_data.IsNotNull())
								{
									var ent_asset = location_asset.GetEntity();

									for (var i = 0; i < Region.max_count; i++)
									{
										ref var region_info = ref World.GetRegionInfo((byte)i);
										if (region_info.IsNotNull() && region_info.map_info.HasValue)
										{
											ref var map_info = ref region_info.map_info.GetRefOrNull();
											if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
											{
												selected_region_id = (byte)i;
												break;
											}
										}
									}

									using (GUI.Group.New(size: GUI.Rm))
									{
										using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
										{
											GUI.TitleCentered(location_data.name, size: 32, pivot: new(0.00f, 0.50f));
										}

										GUI.SeparatorThick();

										ref var region_info = ref World.GetRegionInfo(selected_region_id);
										if (region_info.IsNotNull() && region_info.IsValid())
										{
											ref var map_info = ref region_info.map_info.GetRefOrNull();
											if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
											{
												var map_asset = App.GetModContext().GetMap(region_info.map);
												if (map_asset != null)
												{
													using (var group_map = GUI.Group.New(size: new(GUI.RmX, (GUI.RmX * 0.50f) + 40 + 4), padding: new(4, 4)))
													{
														using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY)))
														{
															GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmX));

															if (true)
															{
																var color = GUI.col_button_ok;
																var alpha = 1.00f;

																if (GUI.DrawButton("Enter", size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
																{
																	Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

																	window.Close();
																	GUI.RegionMenu.ToggleWidget(false);

																	//Client.TODO_LoadRegion(region_id);
																}
															}
														}

														GUI.SameLine();

														using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(6, 2)))
														{
															using (GUI.Wrap.Push(GUI.RmX))
															{
																GUI.TextShaded(map_asset.Description);
															}
														}
													}
												}
											}

											GUI.SeparatorThick();
										}

										using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
										{
											using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8, 8)))
											{
												using (GUI.Wrap.Push(GUI.RmX))
												{
													GUI.TextShaded(location_data.desc);
												}
											}
										}

										GUI.SeparatorThick();

										//using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
										using (var group_info = GUI.Group.New(size: new(GUI.RmX, 144), padding: new(2, 2)))
										{
											using (var group_info_wide = GUI.Group.New2(size: new(GUI.RmX, 0), padding: new(2, 2, 6, 2)))
											{
												//using (GUI.Wrap.Push(GUI.RmX))
												//{
												//	GUI.LabelShaded("Categories:", location_data.categories, font_a: GUI.Font.Superstar, size_a: 16);
												//}
											}

											using (var group_info_left = GUI.Group.New2(size: new(GUI.RmX - 64, GUI.RmY), padding: new(8, 4, 8, 8)))
											{
												//group_info_left.DrawBackground(GUI.tex_frame_white, color: GUI.col_button.WithAlphaMult(0.50f));
												group_info_left.DrawBackground(GUI.tex_panel);

												using (GUI.Wrap.Push(GUI.RmX))
												{
													GUI.LabelShaded("Type:", location_data.type, font_a: GUI.Font.Superstar, size_a: 20, font_b: GUI.Font.Superstar, size_b: 20);
												}
											}

											GUI.SameLine();

											using (var group_info_right = GUI.Group.New2(size: new(GUI.RmX, GUI.RmY), padding: new(0, 0, 0, 0)))
											{
												Span<Entity> children_span = stackalloc Entity[8];
												ent_asset.GetAllChildren(ref children_span, false);

												foreach (var ent_child in children_span)
												{
													if (ILocation.TryGetAsset(ent_child, out var h_location_child))
													{
														ref var location_data_child = ref h_location_child.GetData(out var location_asset_child);
														if (location_data_child.IsNotNull())
														{
															using (var group_child = GUI.Group.New(size: new(GUI.RmX)))
															{
																var selected = WorldMap.selected_entity == ent_child;
																var color = location_data_child.color with { a = 255 };

																if (selected) color = GUI.col_white;

																group_child.DrawBackground(GUI.tex_slot);

																GUI.DrawSpriteCentered(location_data_child.icon, group_child.GetInnerRect(), GUI.Layer.Window, scale: 3.00f, color: color);

																if (GUI.Selectable3(ent_child.GetShortID(), group_child.GetInnerRect(), selected: selected))
																{
																	WorldMap.selected_entity = selected ? default : ent_child;
																	GUI.SetDebugEntity(ent_child);
																}
															}
															if (GUI.IsItemHovered())
															{
																using (GUI.Tooltip.New(size: new(128, 0)))
																{
																	using (GUI.Wrap.Push(GUI.RmX))
																	{
																		GUI.Title(location_data_child.name_short, size: 20);


																	}

																	GUI.SeparatorThick(new(-4, -4));

																	using (GUI.Group.New(padding: new(2)))
																	{
																		using (GUI.Wrap.Push(GUI.RmX))
																		{
																			GUI.Text(location_data_child.desc);
																		}
																	}
																}
															}

															GUI.FocusableAsset(h_location_child);
														}
													}
												}
											}


											//using (var group_info_left = GUI.Group.New2(size: new(72, GUI.RmY), padding: new(0, 0, 0, 0)))
											//{
											//	Span<Entity> children_span = stackalloc Entity[8];
											//	ent_asset.GetAllChildren(ref children_span, false);

											//	foreach (var ent_child in children_span)
											//	{
											//		if (ILocation.TryGetAsset(ent_child, out var h_location_child))
											//		{
											//			ref var location_data_child = ref h_location_child.GetData(out var location_asset_child);
											//			if (location_data_child.IsNotNull())
											//			{
											//				using (var group_child = GUI.Group.New(size: new(GUI.RmX)))
											//				{
											//					group_child.DrawBackground(GUI.tex_slot);

											//					GUI.DrawSpriteCentered(location_data_child.icon, group_child.GetInnerRect(), GUI.Layer.Window, scale: 3.00f);

											//					if (GUI.Selectable3(ent_child.GetShortID(), group_child.GetInnerRect(), false))
											//					{
											//						WorldMap.selected_entity = ent_child;
											//					}
											//				}
											//				GUI.FocusableAsset(h_location_child);
											//			}
											//		}
											//	}
											//}

											//GUI.SameLine();

											////using (var group_info_right = GUI.Group.New2(size: new(GUI.RmX, GUI.RmY), padding: new(0, 0, 0, 0)))
											//using (var group_info_right = GUI.Group.New2(size: new(GUI.RmX, GUI.RmY), padding: new(8, 4, 8, 4)))
											//{

											//	//group_info_left.DrawBackground(GUI.tex_frame_white, color: GUI.col_button.WithAlphaMult(0.50f));
											//	group_info_right.DrawBackground(GUI.tex_panel);

											//	using (GUI.Wrap.Push(GUI.RmX))
											//	{
											//		GUI.LabelShaded("Type:", location_data.type, font_a: GUI.Font.Superstar, size_a: 16);
											//	}
											//}



											//GUI.TextShaded("- some info here");
										}

										using (var group_misc = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
										{
											//Span<Entity> children_span = stackalloc Entity[8];
											//ent_asset.GetAllChildren(ref children_span, false);

											//foreach (var ent_child in children_span)
											//{
											//	using (var group_child = GUI.Group.New(size: new(96, 96)))
											//	{
											//		group_child.DrawBackground(GUI.tex_frame);

											//		if (GUI.Selectable3(ent_child.GetShortID(), group_child.GetInnerRect(), false))
											//		{
											//			WorldMap.selected_entity = ent_child;
											//		}
											//	}
											//}
										}

										if (GUI.DrawButton("Test", size: new(100, 32)))
										{
											var rpc = new Location.DEV_TestRPC()
											{
												val = 1337
											};
											rpc.Send(ent_asset);
										}

										GUI.SameLine();

										GUI.Text(ent_asset.GetIdentifier());

									}

									//ref var interactable = ref ent_asset.GetComponent<Interactable.Data>();
									//if (interactable.IsNotNull())

									if (WorldMap.selected_entity.IsValid())
									{
										ref var interactable = ref WorldMap.selected_entity.GetComponent<Interactable.Data>();
										if (interactable.IsNotNull())
										{
											var sub_size = interactable.window_size;
											//using (var window_sub = window.BeginChildWindow("worldmap.side.right.sub", GUI.AlignX.Left, GUI.AlignY.Top, pivot: new(1.00f, 0.00f), size: sub_size + new Vector2(16, 16), padding: new(8, 8), open: WorldMap.selected_entity.IsValid(), tex_bg: GUI.tex_window_popup_b))
											using (var window_sub = GUI.Window.Standalone("worldmap.side.right.sub", pivot: new(0.50f, 0.00f), position: new(GUI.CanvasSize.X * 0.50f, 32), force_position: false, size: sub_size + new Vector2(16, 16), padding: new(8, 8)))
											{
												if (window_sub.show)
												{
													GUI.DrawWindowBackground(GUI.tex_window_popup_b, padding: new(4), color: GUI.col_default);

													//if (GUI.DrawButton("A", size: new(100, 40)))
													//{
													//	dock.SetTab(0);
													//}

													//GUI.SameLine();

													//if (GUI.DrawButton("B", size: new(100, 40)))
													//{
													//	dock.SetTab(1);
													//}

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
													{
														var count = dock.GetTabCount();
														for (var i = 0u; i < count; i++)
														{
															if (i > 0) GUI.SameLine();
															dock.DrawTab(i, new(0, group_row.size.Y));
														}
													}

													GUI.SeparatorThick();

													//GUI.SameLine();

													//GUI.Text($"{dock.GetTab()}");



													dock.SetSpace(GUI.Rm);

												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
#endif
	}
}

