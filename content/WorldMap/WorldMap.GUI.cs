using TC2.Base.Components;
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
			////App.WriteLine($"{(App.CurrentFrame - WorldMap.last_open_frame) <= 1}");
			//App.WriteLine($"{WorldMap.IsOpen}");
			if (WorldMap.IsOpen)
			{
				IScenario.WorldMap.Renderer.Submit();
				Doodad.Renderer.Submit();
			}

			//App.WriteLine("OnPostRender");
		}

		private static ulong last_open_frame;
		public static bool IsOpen => (App.CurrentFrame - WorldMap.last_open_frame) <= 1;

		//public static Timestamp ts_last_draw;

		public static void Draw()
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();
			ref var region = ref world.GetGlobalRegion();
			if (region.IsNull()) return;

			last_open_frame = App.CurrentFrame;
			//App.WriteLine($"{App.CurrentFrame - WorldMap.last_open_frame}");


			//ts_last_draw = Timestamp.Now();

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

			using (var group_canvas = GUI.Group.New(GUI.GetRemainingSpace()))
			{
				var rect = group_canvas.GetInnerRect();

				using (GUI.ID.Push("worldmap"u8))
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

					if (editor_mode != EditorMode.None && editor_mode != EditorMode.Route && !gizmo.IsHovered() && GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false) && edit_asset != null && GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
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
									if (show_prefectures) GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 0.75f * zoom * asset_data.size, color: asset_data.color_fill.WithColorMult(0.35f).WithAlphaMult(0.50f), layer: GUI.Layer.Window);
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
										if (region_info.IsNotNull())
										{
											ref var map_info = ref region_info.map_info.GetRefOrNull();
											if (map_info.IsNotNull())
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
						}
						#endregion

						#region Markers
						foreach (ref var row in region.IterateQuery<WorldMap.Marker.GetAllMarkersQuery>())
						{
							row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
							in WorldMap.Marker.Data marker,
							in Transform.Data transform,
							ref Nameable.Data nameable,
							bool has_parent) =>
							{
								if (has_parent || marker.flags.HasAny(Marker.Data.Flags.Hidden)) return;

								var pos = transform.GetInterpolatedPosition();
								var scale = 0.500f;
								var asset_scale = Maths.Clamp(marker.scale, 0.50f, 1.00f);

								//var rect_text = AABB.Centered(Vector2.Transform(pos + asset_data.text_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));
								var rect_icon = AABB.Centered(Vector2.Transform(pos + marker.icon_offset, mat_l2c), ((Vector2)marker.icon.size) * region.GetWorldToCanvasScale() * 0.125f);
								var rect_button = AABB.Circle(Vector2.Transform(pos + marker.icon_offset, mat_l2c), marker.radius * region.GetWorldToCanvasScale());

								//GUI.DrawRect(rect_icon, layer: GUI.Layer.Foreground);
								//GUI.DrawRect(rect_text, layer: GUI.Layer.Foreground);

								var is_selected = WorldMap.selected_entity == entity;
								var is_pressed = GUI.ButtonBehavior(entity, rect_button, out var is_hovered, out var is_held);

								var color = (is_selected || is_hovered) ? Color32BGRA.White : marker.color;

								if (is_hovered)
								{
									GUI.SetCursor(App.CursorType.Hand, 1000);

									var location_asset = default(ILocation.Definition);

									if (entity.TryGetAsset(out var asset))
									{
										GUI.FocusableAsset(asset, rect: rect_button);

										location_asset = asset as ILocation.Definition; // TODO: shithack

										if (show_locations && location_asset != null)
										{
											if ((is_selected || is_hovered) && editor_mode == EditorMode.Roads)
											{
												//var ts = Timestamp.Now();
												//var nearest_road = GetNearestRoad(asset_data.h_prefecture, Road.Type.Road, (Vector2)asset_data.point, out var nearest_road_dist_sq);
												//var nearest_rail = GetNearestRoad(asset_data.h_prefecture, Road.Type.Rail, (Vector2)asset_data.point, out var nearest_rail_dist_sq);
												//var ts_elapsed = ts.GetMilliseconds();

												//if (nearest_road_dist_sq <= 1.50f.Pow2())

												if (location_to_road.TryGetValue(location_asset, out var nearest_road))
												{
													GUI.DrawCircleFilled(nearest_road.GetPosition().Transform(in mat_l2c), 0.125f * zoom * 0.50f, Color32BGRA.Yellow, 8, GUI.Layer.Window);
													if (Maths.IsInDistance(mouse_local, nearest_road.GetPosition(), 0.25f))
													{
														DrawConnectedRoads(nearest_road, ref mat_l2c, zoom, iter_max: 50, budget: 30.00f);
													}
												}

												//if (nearest_rail_dist_sq <= 1.00f.Pow2())

												if (location_to_rail.TryGetValue(location_asset, out var nearest_rail))
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
									}

									if (is_pressed)
									{
										selected_region_id = 0;

										if (is_selected)
										{
											WorldMap.selected_entity = default;
											GUI.selected_entity = default;
											if (location_asset != null) WorldMap.h_selected_location = default;
											Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
										}
										else
										{
											WorldMap.selected_entity = entity;
											GUI.selected_entity = entity;
											if (location_asset != null) WorldMap.h_selected_location = location_asset;
											Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
										}
										// Client.RequestSetActiveRegion((byte)i);
									}
								}

								if (show_locations)
								{
									var sprite = marker.icon;
									if (marker.flags.HasAny(Marker.Data.Flags.Directional))
									{
										var rot = marker.rotation;
										var rot_snapped = Maths.Snap(rot, MathF.PI * 0.250f);
										var rot_rem = Maths.DeltaAngle(rot, rot_snapped);

										var rot_invlerp = Maths.InvLerp01(-MathF.PI, MathF.PI, rot_snapped);
										sprite.frame.X = (uint)(rot_invlerp * 8);

										// the sprites aren't 45°
										switch (sprite.frame.X)
										{
											case 1:
											{
												rot_rem += MathF.PI * 0.100f;
											}
											break;

											case 3:
											{
												rot_rem -= MathF.PI * 0.100f;
											}
											break;

											case 5:
											{
												rot_rem += MathF.PI * 0.050f;
											}
											break;

											case 7:
											{
												rot_rem -= MathF.PI * 0.050f;
											}
											break;
										}

										GUI.DrawSprite2(sprite, rect_icon.Scale(asset_scale), layer: GUI.Layer.Window, color: color, rotation: rot_rem);
									}
									else
									{
										GUI.DrawSpriteCentered(sprite, rect_icon, layer: GUI.Layer.Window, 0.125f * Maths.Max(scale * zoom * asset_scale, 16), color: color);
									}

									if (nameable.IsNotNull())
									{
										GUI.DrawTextCentered(nameable.name, Vector2.Transform(transform.position + marker.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.50f * Maths.Max(marker.scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);
									}
								}

								//if (entity.TryGetAsset(out var asset))
								//{
								//	if (asset is ILocation.Definition asset_location)
								//	{
								//		WorldMap.sele
								//	}

								//	GUI.FocusableAsset(asset, rect: rect_icon);
								//}
							});
						}
						#endregion

						if (false)
						{
							#region Locations
							foreach (var asset in ILocation.Database.GetAssets())
							{
								if (asset.id == 0) continue;

								ref var asset_data = ref asset.GetData();
								if (asset_data.flags.HasAny(ILocation.Flags.Hidden) || asset_data.h_location_parent.id != 0) continue;

								var pos = (Vector2)asset_data.point;
								var scale = 0.500f;
								var asset_scale = Maths.Clamp(asset_data.size, 0.50f, 1.00f);
								var icon = asset_data.icon;

								//var rect_text = AABB.Centered(Vector2.Transform(pos + asset_data.text_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));
								var rect_icon = AABB.Centered(Vector2.Transform(pos + asset_data.icon_offset, mat_l2c), ((Vector2)icon.size) * region.GetWorldToCanvasScale() * 0.125f * 0.625f);

								//GUI.DrawRect(rect_icon, layer: GUI.Layer.Foreground);
								//GUI.DrawRect(rect_text, layer: GUI.Layer.Foreground);

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

								//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.625f * asset_scale) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * Maths.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

								if (show_locations)
								{
									GUI.DrawSpriteCentered(asset_data.icon, rect_icon, layer: GUI.Layer.Window, 0.125f * Maths.Max(scale * zoom * asset_scale, 16), color: color);
									GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.50f * Maths.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

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
						}

						#region Overlays
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Window, color: Color32BGRA.White.WithAlphaMult(0.30f));
						//GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 72, 72, 0, 1), rect: AABB.Centered(Vector2.Transform(mouse_local_snapped, mat_l2c), new Vector2(0.25f)), layer: GUI.Layer.Window, scale: 0.125f * 0.50f * zoom, color: Color32BGRA.Black.WithAlphaMult(1));
						//GUI.DrawTextCentered($"Zoom: {zoom:0.00}x\ndelta: [{snap_delta.X:0.000000}, {snap_delta.Y:0.000000}]\ndelta.c: [{snap_delta_canvas.X:0.000000}, {snap_delta_canvas.Y:0.000000}]\ncam: [{worldmap_offset_target.X:0.000000}, {worldmap_offset_target.Y:0.000000}]\ncam.s: [{worldmap_offset_current_snapped.X:0.000000}, {worldmap_offset_current_snapped.Y:0.000000}]\nmouse.l: [{mouse_local.X:0.0000}, {mouse_local.Y:0.0000}]\nmouse: [{mouse_pos.X:0.00}, {mouse_pos.Y:0.00}]", position: rect.GetPosition(new(1, 1)), new(1, 1), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground);
						#endregion

						if (editor_mode != EditorMode.None)
						{
							if (road_junctions.Length > 0)
							{
								var ts = Timestamp.Now();
								foreach (var junction in road_junctions)
								{
									var pos_c = Vector2.Transform(junction.pos, mat_l2c);

									//for (var i = 0; i < junction.segments_count; i++)
									//{
									//	var dir = junction.segments[i].GetPosition() - junction.pos;
									//	GUI.DrawLine(pos_c, Vector2.Transform(junction.pos + (dir * 24), mat_l2c), layer: GUI.Layer.Window);
									//}

									GUI.DrawCircleFilled(pos_c, 0.125f * zoom, junction.segments_count > 1 ? Color32BGRA.Green : Color32BGRA.Yellow.WithAlphaMult(0.250f), 4, GUI.Layer.Window);
									GUI.DrawTextCentered($"{junction.segments_count}", pos_c, layer: GUI.Layer.Window);
								}
								var ts_elapsed = ts.GetMilliseconds();
								GUI.DrawTextCentered($"{ts_elapsed:0.000} ms", GUI.CanvasSize * 0.75f, layer: GUI.Layer.Foreground);
							}
						}

						var hovered = is_worldmap_hovered = GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false);

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
							if (mouse.GetKeyDown(Mouse.Key.Middle))
							{
								//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;

								mouse_pos_old = mouse_pos;
								mouse_pos_new = mouse_pos;

								dragging = true;
							}

							if (GUI.IsMouseDoubleClicked())
							{
								WorldMap.selected_entity = default;
							}
						}

						mouse_delta = mouse_pos_old - mouse_pos_new;

						if (mouse.GetKeyUp(Mouse.Key.Middle))
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
								worldmap_zoom_target = Maths.Clamp(worldmap_zoom_target, 5.85f, 7.50f);
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
							GUI.SetHoveredID("worldmap"u8);
						}
					}
				}

				#region Interactions
				DrawInteractionWindow();
				#endregion

				#region Left
				DrawLeftWindow(ref rect);
				#endregion

				#region Right
				DrawRightWindow(is_loading, ref rect);
				#endregion

				#region Debug			
				DrawDebugWindow(ref rect);
				#endregion
			}
		}

		internal static bool is_worldmap_hovered;
		public static bool IsHovered() => is_worldmap_hovered;

		public static void FocusLocation(ILocation.Handle h_location)
		{
			GUI.RegionMenu.ToggleWidget(true);
			WorldMap.h_selected_location = h_location;

			ref var location_data = ref h_location.GetData(out var location_asset);
			if (location_data.IsNotNull())
			{
				ref var location_parent_data = ref location_data.h_location_parent.GetData(out var location_parent_asset);
				if (location_parent_data.IsNotNull())
				{
					WorldMap.h_selected_location = location_parent_asset;
					WorldMap.selected_entity = location_parent_asset.entity;
					WorldMap.worldmap_offset_target = (Vector2)location_parent_data.point;
				}
				else
				{
					WorldMap.h_selected_location = location_asset;
					WorldMap.selected_entity = location_asset.entity;
					WorldMap.worldmap_offset_target = (Vector2)location_data.point;
				}
			}
		}

		public static void FocusPosition(Vector2 pos)
		{
			GUI.RegionMenu.ToggleWidget(true);
			WorldMap.worldmap_offset_target = pos;
		}

		public static void FocusEntity(Entity entity)
		{
			if (entity.IsValid() && entity.GetRegionID() == 0)
			{
				GUI.RegionMenu.ToggleWidget(true);

				WorldMap.selected_entity = entity;
				ref var transform = ref entity.GetComponent<Transform.Data>();
				if (transform.IsNotNull())
				{
					WorldMap.worldmap_offset_target = transform.position;
				}
			}
		}

		public static void DrawInteractionWindow()
		{
			if (WorldMap.selected_entity.id != 0)
			{
				WorldMap.selected_entity_cached = WorldMap.selected_entity;
			}

			if (WorldMap.selected_entity_cached.IsAlive() && WorldMap.IsOpen)
			{
				ref var interactable = ref WorldMap.selected_entity_cached.GetComponent<Interactable.Data>();
				if (interactable.IsNotNull())
				{
					using (var dock = GUI.Dock.New((uint)WorldMap.selected_entity_cached.id))
					{
						var sub_size = interactable.window_size;
						//using (var window_sub = window.BeginChildWindow("worldmap.side.right.sub", GUI.AlignX.Left, GUI.AlignY.Top, pivot: new(1.00f, 0.00f), size: sub_size + new Vector2(16, 16), padding: new(8, 8), open: WorldMap.selected_entity.IsValid(), tex_bg: GUI.tex_window_popup_b))
						using (var window = GUI.Window.Standalone("worldmap.interact"u8, pivot: new(0.50f, 0.00f), position: new(GUI.CanvasSize.X * 0.50f, 32), force_position: false, size: sub_size + new Vector2(16, 16), size_min: interactable.window_size_min, padding: new(8, 8), flags: GUI.Window.Flags.Resizable))
						{
							if (window.appearing)
							{
								//App.WriteLine("appearing");
								//Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f);
							}

							if (window.show)
							{
								GUI.DrawWindowBackground(GUI.tex_window_popup_b, padding: new(4), color: GUI.col_default);

								using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
								{
									var count = dock.GetTabCount();
									for (var i = 0u; i < count; i++)
									{
										if (i > 0) GUI.SameLine();
										dock.DrawTab(i, new(0, group_row.size.Y));
									}

									using (var group_close = group_row.Split(size: new Vector2(group_row.size.Y), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
									{
										//group_close.DrawBackground(GUI.tex_window_sidebar_b);

										if (GUI.DrawSpriteButton("close", new("ui_icons_window", 16, 16, 0, 0), size: GUI.Rm, color: GUI.font_color_red_b.WithColorMult(0.75f), color_hover: GUI.font_color_red_b.WithColorMult(1.00f), play_sound: false))
										{
											//window.Close();
											WorldMap.selected_entity = default;
										}
										GUI.DrawHoverTooltip("Close");
									}
								}

								GUI.SeparatorThick();

								dock.SetSpace(GUI.Rm);

								if (GUI.GetKeyboard().GetKeyDown(Keyboard.Key.Escape) && window.Close())
								{
									WorldMap.selected_entity = default;
									//Sound.PlayGUI(GUI.sound_window_open, volume: 0.40f);

								}

								if (WorldMap.selected_entity_cached != 0 && WorldMap.selected_entity == 0)
								{
									//App.WriteLine("close");
									//Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
								}
							}
						}
					}
				}
			}
			WorldMap.selected_entity_cached = WorldMap.selected_entity;


		}

		private static void DrawLeftWindow(ref AABB rect)
		{
			using (var window = GUI.Window.Standalone("worldmap.side.left"u8, position: new Vector2(rect.a.X, rect.a.Y) + new Vector2(6, 12), size: new(284, Maths.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
			{
				if (window.show)
				{
					window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

					var mod_context = App.GetModContext();

					ref var world_info = ref Client.GetWorldInfo();
					ref var region = ref World.GetGlobalRegion();

					using (GUI.Group.New(size: GUI.Rm))
					{
						var h_character = Client.GetCharacterHandle();
						ref var character_data = ref h_character.GetData(out var character_asset);
						if (character_data.IsNotNull())
						{
							var ent_character = h_character.AsGlobalEntity();
							//var ent_inside = character_data.ent_inside;

							var is_ent_character_alive = ent_character.IsAlive();

							var ent_inside = is_ent_character_alive ? ent_character.GetParent(Relation.Type.Stored) : default;

							var is_ent_inside_alive = ent_inside.IsAlive();



							var h_location_inside = default(ILocation.Handle);
							if (is_ent_inside_alive && ent_inside.TryGetAssetHandle(out h_location_inside))
							{

							}

							scoped ref var transform = ref Unsafe.NullRef<Transform.Data>();
							if (is_ent_character_alive)
							{
								transform = ref ent_character.GetComponent<Transform.Data>();
							}

							using (GUI.Group.New(size: new(GUI.RmX, 48)))
							{
								GUI.DrawCharacterHead(h_character, frame_size: new Vector2(GUI.RmY), scale: 3.00f);

								GUI.SameLine();
								GUI.TitleCentered(character_data.name, size: 24, pivot: new(0.00f, 0.00f), offset: new(4, 4));
								if (GUI.Selectable3("character"u8, GUI.GetLastItemRect(), selected: is_ent_character_alive && WorldMap.selected_entity == ent_character))
								{
									if (is_ent_character_alive)
									{
										WorldMap.selected_entity.Toggle(ent_character);
										if (WorldMap.selected_entity == ent_character) WorldMap.FocusEntity(ent_character);
									}
								}
								GUI.FocusableAsset(h_character);

								GUI.TitleCentered(character_data.origin.GetName(), size: 16, pivot: new(1.00f, 0.00f), offset: new(-4, 8));
								GUI.FocusableAsset(character_data.origin);


								//GUI.TitleCentered(character_data.origin.GetName(), size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4));


								if (is_ent_character_alive && !is_ent_inside_alive && transform.IsNotNull())
								{
									//var h_location_nearest = WorldMap.GetNearestLocation(transform.position, out var distance_sq);
									var ent_enterable = WorldMap.Enterable.GetNearest(transform.position, out var distance_sq);
									ref var enterable = ref ent_enterable.GetComponent<Enterable.Data>();
									if (enterable.IsNotNull())
									{
										var can_enter = distance_sq <= enterable.radius.Pow2();

										ent_enterable.TryGetAssetHandle(out ILocation.Handle h_location_enterable);

										GUI.TitleCentered(ent_enterable.GetName(), size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4), color: GUI.font_color_disabled);
										//if (GUI.Selectable3(ent_enterable, GUI.GetLastItemRect(), selected: WorldMap.h_selected_location == h_location_nearest))
										if (GUI.Selectable3("enterable.current"u8, GUI.GetLastItemRect(), selected: WorldMap.selected_entity == ent_enterable))
										{
											//WorldMap.h_selected_location.Toggle(h_location_nearest);
											//if (WorldMap.h_selected_location != default) WorldMap.FocusLocation(h_location_nearest);

											WorldMap.selected_entity.Toggle(ent_enterable);
											if (h_location_enterable.IsValid()) WorldMap.h_selected_location.Toggle(h_location_enterable);
											if (WorldMap.selected_entity == ent_enterable) WorldMap.FocusEntity(ent_enterable);
										}
										//GUI.FocusableAsset(h_location_nearest);

										if (can_enter)
										{
											GUI.TitleCentered("[Enter]"u8, size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: Color32BGRA.Green);
											if (GUI.Selectable3("enter"u8, GUI.GetLastItemRect(), selected: false))
											{
												var rpc = new Enterable.EnterRPC()
												{
													h_character = h_character,
													//h_location = h_location_nearest
												};
												rpc.Send(ent_enterable);
											}
										}
										else
										{
											GUI.TitleCentered($"{distance_sq.Sqrt() * WorldMap.km_per_unit:0.00} km", size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: GUI.font_color_disabled);
											//GUI.TitleCentered("Wilderness", size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4), color: GUI.font_color_green_b.WithAlphaMult(0.50f));
										}
									}
								}
								else
								{
									//GUI.TitleCentered(character_data.h_location_current.GetName(), size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4));
									//if (GUI.Selectable3(character_data.h_location_current.id, GUI.GetLastItemRect(), selected: WorldMap.h_selected_location == character_data.h_location_current))
									//{
									//	WorldMap.h_selected_location.Toggle(character_data.h_location_current);
									//	if (WorldMap.h_selected_location != default) WorldMap.FocusLocation(character_data.h_location_current);
									//}
									//GUI.FocusableAsset(character_data.h_location_current);

									GUI.TitleCentered(ent_inside.GetName(), size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4));
									//if (GUI.Selectable3((uint)ent_inside.id, GUI.GetLastItemRect(), selected: WorldMap.h_selected_location == character_data.h_location_current))
									if (GUI.Selectable3((uint)ent_inside.id, GUI.GetLastItemRect(), selected: WorldMap.selected_entity == ent_inside))
									{
										WorldMap.selected_entity.Toggle(ent_inside);
										if (h_location_inside.IsValid()) WorldMap.h_selected_location.Toggle(h_location_inside);
										if (WorldMap.selected_entity == ent_inside) WorldMap.FocusEntity(ent_inside);

										//WorldMap.h_selected_location.Toggle(character_data.h_location_current);
										//if (WorldMap.h_selected_location != default) WorldMap.FocusLocation(character_data.h_location_current);
									}
									//GUI.FocusableAsset(character_data.h_location_current);

									GUI.TitleCentered("[Exit]"u8, size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: Color32BGRA.Red);
									if (GUI.Selectable3("exit"u8, GUI.GetLastItemRect(), selected: false))
									{
										var rpc = new Enterable.ExitRPC()
										{
											h_character = h_character,
										};
										rpc.Send(ent_inside);
									}
								}
							}
							GUI.SeparatorThick();

							using (GUI.Group.New(size: new(GUI.RmX, 0)))
							{
								GUI.DrawMoney(character_data.money, new(48, 48));


								if (is_ent_character_alive)
								{

									//if (transform.IsNotNull())
									//{

									//}

									//if (GUI.DrawButton("Select", size: new(64, 40)))
									//{
									//	WorldMap.selected_entity.Toggle(ent_character);
									//}
								}
								else
								{
									//if (GUI.DrawButton("Exit", size: new(64, 40)))
									//{
									//	var rpc = new Unit.TestRPC()
									//	{
									//		h_character = h_character
									//	};
									//	rpc.Send();
									//}
								}
							}
						}

						//GUI.Checkbox("DEV: Renderer", ref use_renderer, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale A", ref IScenario.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale B", ref IScenario.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32));

						//GUI.SeparatorThick();

						//using (var scrollbox = GUI.Scrollbox.New("sb.worldmap.left", GUI.Rm))
						//{
						//	using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
						//	{
						//		using (GUI.ID.Push("regions"))
						//		{
						//			using (var collapsible = GUI.Collapsible2.New("regions", new Vector2(GUI.RmX, 32), default_open: true))
						//			{
						//				GUI.TitleCentered("Regions", size: 24, pivot: new(0.00f, 0.50f));

						//				if (collapsible.Inner(padding: new Vector4(6, 0, 0, 0)))
						//				{
						//					var region_list_span = World.GetRegionList().AsSpan();
						//					for (var i = 1; i < region_list_span.Length; i++)
						//					{
						//						ref var region_info = ref region_list_span[i];
						//						ref var map_info = ref region_info.map_info.GetRefOrNull();

						//						if (region_info.IsValid() && map_info.IsNotNull())
						//						{
						//							ref var location_data = ref map_info.h_location.GetData();
						//							if (location_data.IsNotNull())
						//							{
						//								using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
						//								{
						//									if (group_row.IsVisible())
						//									{
						//										GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmY));

						//										group_row.DrawBackground(GUI.tex_panel);

						//										GUI.SameLine();

						//										using (GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
						//										{
						//											GUI.TitleCentered(map_info.name, size: 18, pivot: new(0.00f, 0.00f));
						//										}

						//										var selected = selected_region_id == i;
						//										if (GUI.Selectable3((uint)i, group_row.GetOuterRect(), selected))
						//										{
						//											if (selected)
						//											{
						//												selected_region_id = 0;
						//												h_selected_location = default;
						//											}
						//											else
						//											{
						//												selected_region_id = (byte)i;
						//												h_selected_location = map_info.h_location;
						//												worldmap_offset_target = (Vector2)location_data.point;
						//											}
						//										}
						//									}
						//								}
						//							}
						//						}
						//					}
						//				}
						//			}
						//		}
						//	}

						//	using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
						//	{
						//		using (GUI.ID.Push("locations"))
						//		{
						//			using (var collapsible = GUI.Collapsible2.New("locations", new Vector2(GUI.RmX, 32), default_open: true))
						//			{
						//				GUI.TitleCentered("Locations", size: 24, pivot: new(0.00f, 0.50f));

						//				if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
						//				{
						//					foreach (var asset in ILocation.Database.GetAssets())
						//					{
						//						if (asset.id == 0) continue;

						//						ref var asset_data = ref asset.GetData();

						//						using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 4)))
						//						{
						//							if (group_row.IsVisible())
						//							{
						//								group_row.DrawBackground(GUI.tex_panel);

						//								GUI.TitleCentered(asset_data.name, size: 20, pivot: new(0.00f, 0.50f));
						//								GUI.TextShadedCentered(asset_data.type.GetEnumName(), size: 14, pivot: new(1.00f, 0.50f), color: GUI.font_color_desc);

						//								var selected = asset == h_selected_location; // selected_region_id == i;
						//								if (GUI.Selectable3((uint)asset.id, group_row.GetOuterRect(), selected))
						//								{
						//									selected_region_id = 0;

						//									if (selected)
						//									{
						//										h_selected_location = default;
						//									}
						//									else
						//									{
						//										h_selected_location = asset;
						//										worldmap_offset_target = (Vector2)asset_data.point;
						//									}
						//								}
						//							}
						//						}
						//					}
						//				}
						//			}
						//		}
						//	}
						//}

					}
				}
			}
		}

		private static void DrawRightWindow(bool is_loading, ref AABB rect)
		{
			if (selected_region_id != 0 || h_selected_location != 0)
			{
				//var draw_external = true;

				using (var window = GUI.Window.Standalone("worldmap.side.right"u8, position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(322, Maths.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
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
									if (region_info.IsNotNull())
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
									GUI.FocusableAsset(location_asset.GetHandle());

									GUI.SeparatorThick();

									var map_asset = default(MapAsset);

									ref var region_info = ref World.GetRegionInfo(selected_region_id);
									if (region_info.IsNotNull())
									{
										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
										{
											map_asset = App.GetModContext().GetMap(region_info.map);
											//if (map_asset != null)
											//{
											//	using (var group_map = GUI.Group.New(size: new(GUI.RmX, (GUI.RmX * 0.50f) + 40 + 4), padding: new(4, 4)))
											//	{
											//		using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY)))
											//		{
											//			GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmX));

											//			if (true)
											//			{
											//				var color = GUI.col_button_ok;
											//				var alpha = 1.00f;

											//				if (Client.GetRegionID() != selected_region_id)
											//				{
											//					if (GUI.DrawButton("Enter"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
											//					{
											//						Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

											//						window.Close();
											//						GUI.RegionMenu.ToggleWidget(false);

											//						//Client.TODO_LoadRegion(region_id);
											//					}
											//				}
											//				else
											//				{
											//					color = GUI.col_button_error;
											//					if (GUI.DrawButton("Exit"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
											//					{
											//						Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
											//					}
											//				}
											//			}
											//		}

											//		GUI.SameLine();

											//		using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(6, 2)))
											//		{
											//			using (GUI.Wrap.Push(GUI.RmX))
											//			{
											//				GUI.TextShaded(map_asset.Description);
											//			}
											//		}
											//	}
											//}

										}

										//GUI.SeparatorThick();
									}

									using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
									{
										using (var group_left = GUI.Group.New(size: new(128 + 12, 0)))
										{
											using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmX)))
											{
												if (map_asset != null)
												{
													GUI.DrawMapThumbnail(map_asset, size: GUI.Rm, show_frame: false);
												}
												else
												{
													//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
													GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 0.50f);
												}

												GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
											}

											if (map_asset != null)
											{
												var color = GUI.col_button_ok;
												var alpha = 1.00f;

												if (Client.GetRegionID() != selected_region_id)
												{
													if (GUI.DrawButton("Enter"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
													{
														Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

														window.Close();
														GUI.RegionMenu.ToggleWidget(false);

														//Client.TODO_LoadRegion(region_id);
													}
												}
												else
												{
													color = GUI.col_button_error;
													if (GUI.DrawButton("Exit"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
													{
														Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
													}
												}
											}
											else
											{
												if (GUI.DrawButton("Button"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: false, color: GUI.col_button, text_color: GUI.font_color_button_text))
												{

												}
											}
										}

										GUI.SameLine();

										using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
										{
											group_desc.DrawBackground(GUI.tex_panel, inner: true);

											using (GUI.Wrap.Push(GUI.RmX))
											{
												if (map_asset != null)
												{
													GUI.TextShaded(map_asset.Description);
												}
												else
												{
													GUI.TextShaded(location_data.desc);
												}
											}
										}
									}

									GUI.SeparatorThick();


									//using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
									//{
									//	using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8, 8)))
									//	{
									//		using (GUI.Wrap.Push(GUI.RmX))
									//		{
									//			GUI.TextShaded(location_data.desc);
									//		}
									//	}
									//}

									//GUI.SeparatorThick();

									//using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
									using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(2, 2)))
									{
										//using (var group_info_wide = GUI.Group.New2(size: new(GUI.RmX, 0), padding: new(2, 2, 6, 2)))
										//{
										//	//using (GUI.Wrap.Push(GUI.RmX))
										//	//{
										//	//	GUI.LabelShaded("Categories:", location_data.categories, font_a: GUI.Font.Superstar, size_a: 16);
										//	//}
										//}

										//using (var group_info_left = GUI.Group.New2(size: new(GUI.RmX - 64, GUI.RmY), padding: new(8, 4, 8, 8)))
										//{
										//	//group_info_left.DrawBackground(GUI.tex_frame_white, color: GUI.col_button.WithAlphaMult(0.50f));
										//	group_info_left.DrawBackground(GUI.tex_panel);

										//	using (GUI.Wrap.Push(GUI.RmX))
										//	{
										//		GUI.LabelShaded("Type:"u8, location_data.type.GetEnumName(), font_a: GUI.Font.Superstar, size_a: 20, font_b: GUI.Font.Superstar, size_b: 20);
										//	}
										//}

										//GUI.SameLine();

										using (var group_bottom = GUI.Group.New2(size: GUI.Rm))
										{
											using (var scrollbox = GUI.Scrollbox.New("scroll.bottom", size: GUI.Rm))
											{
												Span<Entity> children_span = stackalloc Entity[16];
												ent_asset.GetAllChildren(ref children_span, false);

												foreach (var ent_child in children_span)
												{
													if (ILocation.TryGetAsset(ent_child, out var h_location_child))
													{
														ref var location_data_child = ref h_location_child.GetData(out var location_asset_child);
														if (location_data_child.IsNotNull())
														{
															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 64)))
															{
																var selected = WorldMap.selected_entity == ent_child;
																var color = location_data_child.color with { a = 255 };

																if (selected) color = GUI.col_white;

																using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
																{
																	group_icon.DrawBackground(GUI.tex_slot);

																	GUI.DrawSpriteCentered(location_data_child.icon, group_icon.GetInnerRect(), GUI.Layer.Window, scale: 3.00f, color: color);

																	if (GUI.Selectable3(ent_child.GetShortID(), group_icon.GetInnerRect(), selected: selected))
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

																		using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new(4)))
																		{
																			using (GUI.Wrap.Push(GUI.RmX))
																			{
																				GUI.Text(location_data_child.desc);
																			}
																		}
																	}
																}
																GUI.FocusableAsset(h_location_child);

																GUI.SameLine();

																using (var group_right = GUI.Group.New(size: GUI.Rm))
																{
																	//group_right.DrawBackground(GUI.tex_window_popup);
																}
															}

															GUI.SeparatorThick();
														}
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

									//using (var group_misc = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
									//{
									//	//Span<Entity> children_span = stackalloc Entity[8];
									//	//ent_asset.GetAllChildren(ref children_span, false);

									//	//foreach (var ent_child in children_span)
									//	//{
									//	//	using (var group_child = GUI.Group.New(size: new(96, 96)))
									//	//	{
									//	//		group_child.DrawBackground(GUI.tex_frame);

									//	//		if (GUI.Selectable3(ent_child.GetShortID(), group_child.GetInnerRect(), false))
									//	//		{
									//	//			WorldMap.selected_entity = ent_child;
									//	//		}
									//	//	}
									//	//}
									//}

									//if (GUI.DrawButton("Test", size: new(100, 32)))
									//{
									//	var rpc = new Location.DEV_TestRPC()
									//	{
									//		val = 1337
									//	};
									//	rpc.Send(ent_asset);
									//}

									//GUI.SameLine();

									//GUI.Text(ent_asset.GetIdentifier());

								}

								//ref var interactable = ref ent_asset.GetComponent<Interactable.Data>();
								//if (interactable.IsNotNull())

								//if (WorldMap.selected_entity.IsValid())
								//{
								//	ref var interactable = ref WorldMap.selected_entity.GetComponent<Interactable.Data>();
								//	if (interactable.IsNotNull())
								//	{
								//		var sub_size = interactable.window_size;
								//		//using (var window_sub = window.BeginChildWindow("worldmap.side.right.sub", GUI.AlignX.Left, GUI.AlignY.Top, pivot: new(1.00f, 0.00f), size: sub_size + new Vector2(16, 16), padding: new(8, 8), open: WorldMap.selected_entity.IsValid(), tex_bg: GUI.tex_window_popup_b))
								//		using (var window_sub = GUI.Window.Standalone("worldmap.side.right.sub", pivot: new(0.50f, 0.00f), position: new(GUI.CanvasSize.X * 0.50f, 32), force_position: false, size: sub_size + new Vector2(16, 16), padding: new(8, 8)))
								//		{
								//			if (window_sub.show)
								//			{
								//				GUI.DrawWindowBackground(GUI.tex_window_popup_b, padding: new(4), color: GUI.col_default);

								//				//if (GUI.DrawButton("A", size: new(100, 40)))
								//				//{
								//				//	dock.SetTab(0);
								//				//}

								//				//GUI.SameLine();

								//				//if (GUI.DrawButton("B", size: new(100, 40)))
								//				//{
								//				//	dock.SetTab(1);
								//				//}

								//				using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
								//				{
								//					var count = dock.GetTabCount();
								//					for (var i = 0u; i < count; i++)
								//					{
								//						if (i > 0) GUI.SameLine();
								//						dock.DrawTab(i, new(0, group_row.size.Y));
								//					}
								//				}

								//				GUI.SeparatorThick();

								//				//GUI.SameLine();

								//				//GUI.Text($"{dock.GetTab()}");



								//				dock.SetSpace(GUI.Rm);

								//			}
								//		}
								//	}
								//}
							}

							if (GUI.GetKeyboard().GetKeyDown(Keyboard.Key.Escape) && window.Close())
							{
								WorldMap.selected_region_id = 0;
								WorldMap.h_selected_location = 0;
							}

							if (WorldMap.h_selected_location == 0 && WorldMap.selected_region_id == 0)
							{
								Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
							}
						}
					}
				}
			}
		}


		public partial struct LocationGUI: IGUICommand
		{
			public Entity ent_location;
			public Location.Data location;

			public static int selected_tab;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Location###location.gui"u8, this.ent_location))
				{
					this.StoreCurrentWindowTypeID(order: -150);
					if (window.show)
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref this.ent_location.GetRegionCommon();
						//ref var map_info = ref region.GetMapInfo();
						ref var location_data = ref this.location.h_location.GetData(out var s_location);

						using (GUI.Group.New(size: GUI.Rm, padding: new(0)))
						{
							if (location_data.IsNotNull())
							{
								using (GUI.Group.New(new(GUI.RmX, 40), new(0, 0)))
								{
									using (var group_header = GUI.Group.New(new(GUI.RmX, 40), new(8, 0)))
									{

									}
								}

								GUI.SeparatorThick(margin: new(4, 4));

								using (var group_main = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(4)))
								{
									group_main.DrawBackground(GUI.tex_panel);

									var materials_filtered = IMaterial.Database.GetAssets().Where(x => x.data.commodity?.flags.HasAny(IMaterial.Commodity.Flags.Marketable) ?? false).ToArray();
									var materials_filtered_span = materials_filtered.AsSpan();

									Span<(float buy, float sell, float produce)> weights_span = stackalloc (float buy, float sell, float produce)[materials_filtered_span.Length];

									using (var scrollbox = GUI.Scrollbox.New("scroll.economy"u8, size: GUI.Rm))
									{
										// BUY
										using (var group_buy = GUI.Group.New(size: new Vector2(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
										{
											for (var i = 0; i < materials_filtered_span.Length; i++)
											{
												var material_asset = materials_filtered_span[i];
												weights_span[i].buy = Market.CalculateBuyScore(material_asset, ref location_data);
												weights_span[i].sell = Market.CalculateSellScore(material_asset, ref location_data);
												weights_span[i].produce = Market.CalculateProductionScore(material_asset, ref location_data);
											}
											weights_span.Sort(materials_filtered_span, (x, y) => y.produce.CompareTo(x.produce));
											//weights_span = weights_span.OrderByDescending(x => x.produce);

											for (var i = 0; i < materials_filtered_span.Length; i++)
											{
												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
												{
													if (group_row.IsVisible())
													{
														group_row.DrawBackground(GUI.tex_panel);

														var material_asset = materials_filtered_span[i];
														ref var material_data = ref material_asset.GetData();

														GUI.DrawMaterialSmall(material_asset, new(GUI.RmY));

														GUI.SameLine(8);

														GUI.TitleCentered(material_data.name, pivot: new(0.00f, 0.50f));
														GUI.TextCentered($"{weights_span[i].buy:0.00}", pivot: new(1.00f, 0.50f), offset: new(-96, 0));
														GUI.DrawHoverTooltip("Buy"u8);

														GUI.TextCentered($"{weights_span[i].sell:0.00}", pivot: new(1.00f, 0.50f), offset: new(-48, 0));
														GUI.DrawHoverTooltip("Sell"u8);

														GUI.TextCentered($"{weights_span[i].produce:0.00}", pivot: new(1.00f, 0.50f), offset: new(0, 0));
														GUI.DrawHoverTooltip("Produce"u8);

														//App.WriteLine($"BUY [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
													}
												}

												GUI.NewLine(4);
											}
										}

										//GUI.SameLine();

										//using (var group_sell = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(4)))
										//{
										//	for (var i = 0; i < materials_filtered_span.Length; i++)
										//	{
										//		var material_asset = materials_filtered_span[i];
										//		weights_span[i] = Market.CalculateSellWeights(material_asset, ref location_data);
										//		//weights_span[i] = Market.CalculateProduceWeights(material_asset, ref location_data);
										//	}
										//	weights_span.Sort(materials_filtered_span);

										//	for (var i = materials_filtered_span.Length - 1; i >= 0; i--)
										//	{
										//		using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
										//		{
										//			if (group_row.IsVisible())
										//			{
										//				group_row.DrawBackground(GUI.tex_panel);

										//				var material_asset = materials_filtered_span[i];
										//				ref var material_data = ref material_asset.GetData();

										//				GUI.DrawMaterialSmall(material_asset, new(GUI.RmY));

										//				GUI.SameLine(8);

										//				GUI.TitleCentered(material_data.name, pivot: new(0.00f, 0.50f));
										//				GUI.TextCentered($"{weights_span[i]:0.00}", pivot: new(1.00f, 0.50f));
										//				//App.WriteLine($"BUY [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
										//			}
										//		}

										//		GUI.NewLine(4);
										//	}
										//}

									}
								}
							}
						}
					}
				}
			}
		}


		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Location.Data location,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.show)
			{
				var gui = new LocationGUI()
				{
					ent_location = entity,
					location = location,
				};
				gui.Submit();
			}
		}
#endif
	}
}

