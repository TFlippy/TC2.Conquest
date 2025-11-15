using TC2.Base.Components;
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

		public static AABB camera_rect_canvas;
		public static AABB camera_rect_world;

		//public static Timestamp ts_last_draw;


		// TODO: clean up the rest of mess
		[Ugly, Shitcode]
		public static void Draw()
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();
			ref var region = ref world.GetGlobalRegion();
			if (region.IsNull()) return;

			var has_region = Client.HasRegion() || is_loading;

			last_open_frame = App.CurrentFrame;

			//var derp = WorldMap.hovered_entity;
			//App.WriteLine(WorldMap.hovered_entity);
			//var pop = WorldMap.hovered_entity.Pop(); // WorldMap.interacted_entity);
			//App.WriteLine(WorldMap.hovered_entity.Item1);
			//App.WriteLine(WorldMap.hovered_entity.Item2);
			//App.WriteLine(pop);

			//App.WriteLine(WorldMap.hovered_entity);

			Keg.Extensions.TupleExtensions.Pop(ref WorldMap.hovered_entity); // WorldMap.hovered_entity.Pop();

			//App.WriteLine($"{WorldMap.hovered_entity.current}; {WorldMap.hovered_entity.pending}");
			//App.WriteLine($"{WorldMap.hovered_entity.Pop()}");
			//App.WriteLine($"{WorldMap.hovered_entity.current}; {WorldMap.hovered_entity.pending}");


			//WorldMap.hovered_entity.Item1 = WorldMap.hovered_entity.Item2; //.Push(); //.pending = default;
			//WorldMap.hovered_entity.Item2 = default; //.Push(); //.pending = default;


			//WorldMap.hovered_entity.current = WorldMap.hovered_entity.pending; //.Push(); //.pending = default;
			//WorldMap.hovered_entity.pending = default; //.Push(); //.pending = default;


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
					//GUI.DrawWindowBackground(GUI.tex_window_character);
					//sb.Clear();

					ref var scenario_data = ref h_scenario.GetData(out var scenario_asset);
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
					//GUI.DrawRect(rect, color: GUI.col_button_yellow, layer: GUI.Layer.Foreground);

					using (GUI.Clip.Push(rect))
					{
						var scale_b = IScenario.WorldMap.scale_b;
						var snap_delta = (worldmap_offset_current_snapped - worldmap_offset_current);

						mat_l2c = Maths.TRS3x2((worldmap_offset_current * -zoom) + rect.GetPosition(new(0.50f)) - snap_delta, rotation, new Vector2(zoom));
						Matrix3x2.Invert(mat_l2c, out mat_c2l);

						var mat_l2c2 = Maths.TRS3x2(rect.GetPosition(new Vector2(0.50f)), rotation, new Vector2(1));
						Matrix3x2.Invert(mat_l2c2, out var mat_c2l2);

						region.SetWorldToCanvasMatrix(mat_l2c);
						region.SetCanvasToWorldMatrix(mat_c2l);

						region.SetWorldToCanvasScale(zoom * 0.50f);
						region.SetCanvasToWorldScale(1.00f / region.GetWorldToCanvasScale());

						var snap_delta_canvas = snap_delta * zoom;

						var tex_scale = enable_renderer ? (IScenario.WorldMap.worldmap_size.x / scale_b) * zoom : 16.00f;
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
						var mouse_local = region.CanvasToWorld(mouse_pos);
						var mouse_local_snapped = mouse_local;
						mouse_local_snapped.Snap(1.00f / scale_b, out mouse_local_snapped);

						WorldMap.worldmap_mouse_position = mouse_local;
						WorldMap.worldmap_mouse_position_snapped = mouse_local_snapped;

						var tex_line_prefecture = h_texture_line_00;
						var tex_line_governorate = h_texture_line_01;

						WorldMap.camera_rect_canvas = rect;
						WorldMap.camera_rect_world = region.CanvasToWorld(rect);

						//GUI.DrawRect(WorldMap.camera_rect_canvas, layer: GUI.Layer.Foreground);
						//GUI.DrawRect(region.WorldToCanvas(WorldMap.camera_rect_world), layer: GUI.Layer.Foreground);

						//region.DrawDebugRect(WorldMap.camera_rect_world, Color32BGRA.Yellow);

						#region Prefectures
						DrawPrefectures(ref region);
						static void DrawPrefectures(ref Region.Data.Global region)
						{
							foreach (var asset in IPrefecture.Database.GetAssetsSpan())
							{
								//if (asset.id == 0) continue;
								if (asset.data.bb.OverlapsRect(WorldMap.camera_rect_world))
								{
									ref var asset_data = ref asset.GetData();

									var points = asset_data.points.AsSpan();
									if (!points.IsEmpty)
									{
										Inner(ref region, ref asset_data, points);

										[MethodImpl(MethodImplOptions.NoInlining)] // Stackalloc in loop
										static void Inner(ref Region.Data.Global region, ref IPrefecture.Data asset_data, Span<Vec2i16> points)
										{
											var pos_center = Vector2.Zero;

											Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
											for (var i = 0; i < points.Length; i++)
											{
												var point = (Vector2)points[i];
												pos_center += point;
												var point_t = region.WorldToCanvas(point); //.Transform(in mat_l2c);

												points_t_span[i] = point_t;
											}
											pos_center /= points.Length;
											pos_center += asset_data.offset;

											if (show_prefectures && show_fill) GUI.DrawPolygon(points_t_span, asset_data.color_fill.WithAlpha(50), GUI.Layer.Window);

											//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
											if (enable_renderer)
											{
												if (show_prefectures && show_borders) DrawOutline(ref region, points, asset_data.color_border, asset_data.border_scale, 2.00f, asset_data.h_texture_border);

												//if (show_roads)
												{
													var roads_span = asset_data.roads.AsSpan();
													foreach (ref var road in roads_span)
													{
														if (road.type == Road.Type.Road && !show_roads) continue;
														if (road.type == Road.Type.Rail && !show_rails) continue;


														//region.DrawDebugRect(road.bb, road.bb.OverlapsRect(WorldMap.camera_rect_world) ? Color32BGRA.Green : Color32BGRA.Red);

														//region.DrawDebugText(road.bb.GetPosition(), $"{road.bb.a} {road.bb.b}", Color32BGRA.Magenta);

														//if (road.bb.OverlapsRect(WorldMap.camera_rect_world))
														//{
														//	//GUI.DrawRect(region.WorldToCanvas(road.bb), layer: GUI.Layer.Foreground);
														//}

														if (road.bb.OverlapsRect(WorldMap.camera_rect_world))
														{
															DrawOutlineShader(road.points, road.color_border, road.scale, road.h_texture, loop: false);
														}
													}
												}
											}
											else
											{
												if (show_prefectures && show_borders) DrawOutline(ref region, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, asset_data.h_texture_border);
											}

											//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 1.00f * zoom, color: GUI.font_color_title.WithAlphaMult(1.00f), layer: GUI.Layer.Window);
											if (show_prefectures) GUI.DrawTextCentered(asset_data.name_short, region.WorldToCanvas(pos_center), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: region.GetWorldToCanvasScale() * asset_data.size, color: asset_data.color_fill.WithColorMult(0.30f).WithAlpha(150), layer: GUI.Layer.Window);
										}
									}
								}
							}
						}
						#endregion

						#region Governorates
						DrawGovernorates(ref region);
						static void DrawGovernorates(ref Region.Data.Global region)
						{
							foreach (var asset in IGovernorate.Database.GetAssetsSpan())
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
										if (show_governorates && show_borders) DrawOutline(ref region, points, asset_data.color_border, 0.125f, 0.25f, asset_data.h_texture_border);
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
						}
						#endregion

						#region Regions
						DrawRegions(ref region);
						// TODO: old stuff, rewrite this
						static void DrawRegions(ref Region.Data.Global region)
						{
							ref var world_info = ref Client.GetWorldInfo();
							if (world_info.IsNotNull())
							{
								//if (world_info.regions != null)
								{
									var mod_context = App.GetModContext();

									for (var i = 1; i < Region.max_count; i++)
									{
										using (GUI.ID<WorldMap.Marker.Data, Region.Data>.Push(i))
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
													//var text_offset = Vector2.Zero;
													var icon_offset = Vector2.Zero;

													ref var location_data = ref map_info.h_location.GetData();
													if (location_data.IsNotNull())
													{
														map_pos = (Vector2)location_data.point;
														//text_offset = location_data.text_offset;
														icon_offset = location_data.icon_offset;
													}

													//var rect_text = AABB.Centered(Vector2.Transform(map_pos + text_offset, mat_l2c), new Vector2(icon_size * zoom * 0.50f));
													var rect_icon = AABB.Centered(region.WorldToCanvas(map_pos + icon_offset + new Vector2(0, -0.375f)), new Vector2(icon_size * region.GetWorldToCanvasScale() * 2.00f));
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
						}
						#endregion

						#region Markers
						DrawMarkers(ref region);
						static void DrawMarkers(ref Region.Data.Global region)
						{
							foreach (ref var row in region.IterateQuery<WorldMap.Marker.GetAllMarkersQuery>())
							{
								row.Run(static (ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
								in WorldMap.Marker.Data marker,
								in Transform.Data transform,
								ref Nameable.Data nameable,
								bool has_parent) =>
								{
									if (marker.flags.HasAny(Marker.Data.Flags.Hidden)) return;
									if (has_parent && marker.flags.HasAny(Marker.Data.Flags.Hide_If_Parented)) return;

									var pos = transform.GetInterpolatedPosition();
									var scale = 1.000f;
									var asset_scale = Maths.Clamp(marker.scale, 0.250f, 1.00f);

									//var rect_text = AABB.Centered(Vector2.Transform(pos + asset_data.text_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));
									//var rect_icon = AABB.Centered(Vector2.Transform(pos + marker.icon_offset, mat_l2c), ((Vector2)marker.icon.size) * region.GetWorldToCanvasScale() * 0.125f);
									//var rect_button = AABB.Circle(Vector2.Transform(pos + marker.icon_offset, mat_l2c), marker.radius * region.GetWorldToCanvasScale());

									var rect_icon = AABB.Centered(region.WorldToCanvas(pos + marker.icon_offset), ((Vector2)marker.icon.size) * region.GetWorldToCanvasScale() * 0.125f);
									if (WorldMap.camera_rect_canvas.OverlapsRect(rect_icon))
									{
										var rect_button = AABB.Circle(region.WorldToCanvas(pos + marker.icon_offset), marker.radius * region.GetWorldToCanvasScale());

										//GUI.DrawRect(rect_icon, layer: GUI.Layer.Foreground);
										//GUI.DrawRect(rect_text, layer: GUI.Layer.Foreground);

										var is_selected = WorldMap.interacted_entity_cached == entity;
										var is_pressed = GUI.ButtonBehavior(entity, rect_button, out var is_hovered, out var is_held);
										var is_interactable = marker.flags.HasNone(Marker.Data.Flags.No_Interact);
										var is_selectable = marker.flags.HasNone(Marker.Data.Flags.No_Select);

										//if (is_selected)
										//{
										//	App.WriteLine(WorldMap.hovered_entity);
										//}

										if (is_selectable)
										{
											if (is_hovered) WorldMap.hovered_entity.pending = entity;
											is_hovered |= WorldMap.hovered_entity.current == entity;
										}
										else
										{
											is_hovered = false;
										}
											//is_hovered |= WorldMap.hovered_entity.current == entity;
										//is_hovered &= is_selectable;
										

										ref var unit = ref entity.GetComponent<WorldMap.Unit.Data>();
										if (unit.IsNotNull())
										{
											is_interactable &= unit.CanPlayerControlUnit(entity, Client.GetPlayerHandle());
										}

										//var color = (is_selected || is_hovered) ? Color32BGRA.White : (marker.color_override.a > 0 ? marker.color_override : marker.color);
										var color = (marker.color_override.a > 0 ? marker.color_override : marker.color);
										var alpha_mult = is_interactable ? 1.00f : 0.75f;

										if ((is_selected | is_hovered) || WorldMap.hs_selected_entities.Contains(entity))
										{
											if (is_interactable) color = color.WithColorMult(1.20f).WithAlpha(255);
											asset_scale *= 1.10f;
											//rect_icon = rect_icon.Grow(10.00f);
										}

										var ent_parent = default(Entity);
										if (entity.TryGetParent(Relation.Type.Child, out ent_parent))
										{
											ref var transform_parent = ref ent_parent.GetComponent<Transform.Data>();
											if (transform_parent.IsNotNull())
											{
												var pos_parent = transform_parent.GetInterpolatedPosition();
												var dir = (pos_parent - pos).GetNormalized(out var parent_dist);
												//GUI.DrawLineTextured(region.WorldToCanvas(pos), region.WorldToCanvas(transform_parent.GetInterpolatedPosition()), GUI.tex_separator_b, color: GUI.col_white, thickness: 0.200f * region.GetWorldToCanvasScale(), overshoot: 0.25f, layer: GUI.Layer.Window);
												GUI.DrawLine(region.WorldToCanvas(pos), region.WorldToCanvas(pos_parent - (dir * 0.125f)), color: Color32BGRA.Black.WithAlpha(140), thickness: 0.125f * region.GetWorldToCanvasScale(), layer: GUI.Layer.Window);
											}
										}

										if (is_hovered)
										{
											////WorldMap.hovered_entity.pending = entity;
											if (is_interactable) GUI.SetCursor(App.CursorType.Hand, 100);

											if (entity.TryGetAsset(out ILocation.Definition location_asset) || ent_parent.TryGetAsset(out location_asset))
											{
												GUI.FocusableAsset(location_asset, rect: rect_button);

												//location_asset = asset as ILocation.Definition; // TODO: shithack

												if (show_locations)
												{
													if ((is_selected | is_hovered) && editor_mode == EditorMode.Roads)
													{
														//var ts = Timestamp.Now();
														//var nearest_road = GetNearestRoad(asset_data.h_prefecture, Road.Type.Road, (Vector2)asset_data.point, out var nearest_road_dist_sq);
														//var nearest_rail = GetNearestRoad(asset_data.h_prefecture, Road.Type.Rail, (Vector2)asset_data.point, out var nearest_rail_dist_sq);
														//var ts_elapsed = ts.GetMilliseconds();

														//if (nearest_road_dist_sq <= 1.50f.Pow2())

														if (location_to_road.TryGetValue(location_asset, out var nearest_road))
														{
															GUI.DrawCircleFilled(region.WorldToCanvas(nearest_road.GetPosition()), 0.125f * region.GetWorldToCanvasScale(), Color32BGRA.Yellow, 8, GUI.Layer.Window);
															if (Maths.IsInDistance(WorldMap.worldmap_mouse_position, nearest_road.GetPosition(), 0.25f))
															{
																DrawConnectedRoads(ref region, nearest_road, iter_max: 50, budget: 30.00f);
															}
														}

														//if (nearest_rail_dist_sq <= 1.00f.Pow2())

														if (location_to_rail.TryGetValue(location_asset, out var nearest_rail))
														{
															GUI.DrawCircleFilled(region.WorldToCanvas(nearest_rail.GetPosition()), 0.125f * region.GetWorldToCanvasScale(), Color32BGRA.Orange, 8, GUI.Layer.Window);
															if (Maths.IsInDistance(WorldMap.worldmap_mouse_position, nearest_rail.GetPosition(), 0.25f))
															{
																DrawConnectedRoads(ref region, nearest_rail, iter_max: 50, budget: 100.00f);
															}
														}
														//GUI.Text($"nearest in {ts_elapsed:0.0000} ms");
													}
												}
											}

											if (is_pressed & is_selectable & is_interactable)
											{
												selected_region_id = 0;

												if (is_selected)
												{
													WorldMap.interacted_entity = default;
													if (unit.IsNotNull())
													{
														var select_results = WorldMap.SelectUnitBehavior(entity, SelectUnitMode.Single, SelectUnitFlags.Multiselect | SelectUnitFlags.Hold_Shift | SelectUnitFlags.Toggle | SelectUnitFlags.Include_Children, selected: is_selected);

														//if (GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift))
														//{
														//	//WorldMap.hs_selected_entities.Remove(entity);
														//	if (WorldMap.hs_selected_entities.Count > 0)
														//	{
														//		WorldMap.hs_selected_entities.Remove(entity);
														//	}
														//	else
														//	{
														//		WorldMap.hs_selected_entities.Add(entity);
														//	}
														//}
														//else
														//{
														//	if (WorldMap.hs_selected_entities.Count > 1)
														//	{
														//		WorldMap.hs_selected_entities.Clear();
														//		WorldMap.hs_selected_entities.Add(entity);
														//	}
														//	else
														//	{
														//		WorldMap.hs_selected_entities.Clear();
														//	}
														//}
													}

													GUI.selected_entity = default;

													if (location_asset != null) WorldMap.h_selected_location = default;
													else WorldMap.h_selected_location = default;

													Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
												}
												else
												{
													if (unit.IsNotNull())
													{
														var select_results = WorldMap.SelectUnitBehavior(entity, SelectUnitMode.Single, SelectUnitFlags.Multiselect | SelectUnitFlags.Hold_Shift | SelectUnitFlags.Toggle | SelectUnitFlags.Include_Children, selected: is_selected);
														//if (GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift))
														//{
														//	WorldMap.hs_selected_entities.Toggle(entity, !is_selected);
														//}
														//else
														//{
														//	WorldMap.selected_entity = entity;
														//	GUI.selected_entity = entity;
														//	if (location_asset != null) WorldMap.h_selected_location = location_asset;

														//	WorldMap.hs_selected_entities.Clear();
														//	WorldMap.hs_selected_entities.Add(entity);
														//}
													}

													if (is_interactable)
													{
														WorldMap.interacted_entity = entity;
														GUI.selected_entity = entity;

														if (location_asset != null) WorldMap.h_selected_location = location_asset;
														else WorldMap.h_selected_location = default;

														Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
													}
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
												sprite.frame.x = (uint)(rot_invlerp * 8);

												// the sprites aren't 45°
												switch (sprite.frame.x)
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

												if (marker.flags.HasAny(Marker.Data.Flags.Use_Worldmap_Renderer))
												{
													Doodad.Renderer.Add(new Doodad.Renderer.Data()
													{
														sprite = sprite,
														position = Maths.Snap(pos + marker.icon_offset, 1.00f / 32.00f),
														rotation = -rot_rem,
														z = 0.75f,
														color = Color32BGRA.White,
														scale = new Vector2(marker.scale)
													});
												}
												else
												{
													GUI.DrawSprite2(sprite, rect_icon.Scale(asset_scale), layer: GUI.Layer.Window, color: color.WithAlphaMult(alpha_mult), rotation: rot_rem);
												}
											}
											else
											{
												if (marker.flags.HasAny(Marker.Data.Flags.Use_Worldmap_Renderer))
												{
													Doodad.Renderer.Add(new Doodad.Renderer.Data()
													{
														sprite = sprite,
														position = Maths.Snap(pos + marker.icon_offset, 1.00f / 32.00f),
														rotation = transform.GetInterpolatedRotation(),
														z = 0.75f,
														color = Color32BGRA.White,
														scale = new Vector2(marker.scale)
													});
												}
												else
												{
													GUI.DrawSpriteCentered(sprite, rect_icon, layer: GUI.Layer.Window, 0.125f * Maths.Max(scale * region.GetWorldToCanvasScale() * asset_scale, 16), color: color.WithAlphaMult(alpha_mult));
												}
											}

											if (nameable.IsNotNull())
											{
												GUI.DrawTextCentered(nameable.name, region.WorldToCanvas(transform.position + marker.text_offset), pivot: new(0.50f, 0.50f), color: GUI.font_color_default.WithAlphaMult(alpha_mult), font: GUI.Font.Superstar, size: Maths.Clamp(region.GetWorldToCanvasScale() * asset_scale * 0.50f, 12, 32), layer: GUI.Layer.Window, box_shadow: true);
											}
										}

										// TODO: big shitcode
										ref var enterable = ref entity.GetComponent<WorldMap.Enterable.Data>();
										if (enterable.IsNotNull())
										{
											var children_span = FixedArray.CreateSpan8NoInit<Entity>(out var children_buffer);
											entity.GetChildren(ref children_span, Relation.Type.Stored);

											var child_i = 0;
											foreach (var ent_child in children_span)
											{
												ref var child_marker = ref ent_child.GetComponent<WorldMap.Marker.Data>();
												if (child_marker.IsNotNull())
												{
													//var child_icon = ent_child.GetIcon();
													var child_icon = child_marker.icon;
													if (child_icon.texture.id != 0)
													{
														var rect_child = region.WorldToCanvas(AABB.Simple(transform.position + marker.text_offset + new Vector2(-0.25f + (child_i * 0.1750f), 0.125f * 0.25f), new Vector2(1, 1) * 0.20f));
														//var rect_child = AABB.Simple(new Vector2(rect_button.a.X, rect_button.b.Y), new Vector2(16, 16));

														//GUI.DrawRectFilled(rect_child, color: GUI.col_black.WithAlpha(150), layer: GUI.Layer.Window);

														var child_color = color.WithRGB(child_marker.color_override.IsVisible() ? child_marker.color_override : child_marker.color);

														GUI.DrawSpriteCentered(
															child_icon, rect_child,
															layer: GUI.Layer.Window, scale: 0.125f * Maths.Max(region.GetWorldToCanvasScale() * child_marker.scale * 0.75f, 16),
															color: child_color.WithAlphaMult(alpha_mult), pivot: new Vector2(0.50f, 0.50f));

														child_i++;
													}
												}
											}
										}
									}
								});
							}
						}
						#endregion

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
						if (hovered)
						{
							GUI.SetHoveredID("worldmap"u8);
						}

						var hovered_any = GUI.IsHoveringRect(rect, allow_blocked: true, allow_overlapped: true, root_window: true, child_windows: true, any_window: true);
						if (hovered_any)
						{
							Chat.target_region_id = 0;
							GUI.DisablePlayerMovement();
						}

						#region Editor
						DrawEditor(ref rect, ref scenario_data, scenario_asset, ref mouse, ref kb, zoom, ref mat_l2c, ref mouse_local, hovered);
						#endregion

						#region Camera
						UpdateCamera(ref mouse, ref kb, zoom_inv, scale_b, ref mouse_pos, hovered, hovered_any);
						static void UpdateCamera(ref Mouse.Data mouse, ref Keyboard.Data kb, float zoom_inv, float scale_b, ref Vector2 mouse_pos, bool hovered, bool hovered_any)
						{
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
									WorldMap.interacted_entity = default;
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

								ref var world_data = ref h_scenario.GetData();
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
										if (World.GetConfig().enable_mod_editing)
										{
											var scroll = mouse.GetScroll();
											if (scroll != 0.00f)
											{
												Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
												editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) - (int)(scroll), 0, (int)EditorMode.Max);
												edit_asset = null;
												edit_doodad_index = null;
											}
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

								var move_speed = (1.00f / MathF.Sqrt(worldmap_zoom_current)) * 500.00f * (float)App.FrameDeltaTime;

								if (hovered_any && !kb.GetKey(Keyboard.Key.LeftControl))
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
						}
						#endregion

						if (dragging && GUI.IsHovered)
						{
							GUI.SetHoveredID("worldmap"u8);
						}
					}

					if (has_region)
					{
						GUI.DrawBackground(GUI.tex_frame, rect: rect, padding: new(6));
					}
				}



				#region Interactions
				DrawInteractionWindow();
				#endregion

				#region Left
				DrawLeftWindow(ref rect);
				#endregion

				//#region Right
				//DrawRightWindow(is_loading, ref rect);
				//#endregion

				#region Bottom
				DrawBottomWindow(is_loading, ref rect);
				#endregion

				#region Debug
				if (World.GetConfig().enable_mod_editing)
				{
					DrawDebugWindow(ref rect);
				}
				#endregion
			}
		}

		internal static bool is_worldmap_hovered;
		public static bool IsHovered() => is_worldmap_hovered;

		public static void FocusLocation(ILocation.Handle h_location, bool interact = true, bool open_widget = true)
		{
			if (open_widget) GUI.RegionMenu.ToggleWidget(true);
			WorldMap.h_selected_location = h_location;

			ref var location_data = ref h_location.GetData(out var location_asset);
			if (location_data.IsNotNull())
			{
				ref var location_parent_data = ref location_data.h_location_parent.GetData(out var location_parent_asset);
				if (location_parent_data.IsNotNull())
				{
					WorldMap.h_selected_location = location_parent_asset;
					if (interact) WorldMap.interacted_entity = location_parent_asset.GetGlobalEntity();
					WorldMap.worldmap_offset_target = (Vector2)location_parent_data.point;
				}
				else
				{
					WorldMap.h_selected_location = location_asset;
					if (interact) WorldMap.interacted_entity = location_asset.GetGlobalEntity();
					WorldMap.worldmap_offset_target = (Vector2)location_data.point;
				}
			}
		}

		public static void FocusPosition(Vector2 pos)
		{
			GUI.RegionMenu.ToggleWidget(true);
			WorldMap.worldmap_offset_target = pos;
		}

		public static void SelectEntity(Entity entity, bool focus = true, bool interact = true, bool open_widget = true)
		{
			if (entity.id != 0 && entity.GetRegionID() == 0 && entity.IsAlive())
			{
				if (open_widget) GUI.RegionMenu.ToggleWidget(true);

				if (interact)
				{
					WorldMap.interacted_entity = entity;
				}

				hs_selected_entities.Add(entity);

				if (focus)
				{
					ref var transform = ref entity.GetComponent<Transform.Data>();
					if (transform.IsNotNull())
					{
						WorldMap.worldmap_offset_target = transform.position;
					}
				}
			}
			else
			{
				WorldMap.selected_region_id = 0;
				if (interact)
				{
					WorldMap.interacted_entity = default;
					WorldMap.h_selected_location = default;
				}
			}
		}

		public static void FocusEntity(Entity entity, bool interact = true, bool open_widget = true)
		{
			if (entity.id != 0 && entity.GetRegionID() == 0 && entity.IsAlive())
			{
				if (open_widget) GUI.RegionMenu.ToggleWidget(true);

				if (interact)
				{
					WorldMap.interacted_entity = entity;
				}

				ref var transform = ref entity.GetComponent<Transform.Data>();
				if (transform.IsNotNull())
				{
					WorldMap.worldmap_offset_target = transform.position;
				}
			}
			else
			{
				WorldMap.selected_region_id = 0;
				WorldMap.interacted_entity = default;
				WorldMap.h_selected_location = default;
			}
		}

		public static void DrawInteractionWindow()
		{
			//if (WorldMap.interacted_entity.id != 0)
			//{
			//	WorldMap.interacted_entity_cached = WorldMap.interacted_entity;
			//}
			WorldMap.interacted_entity_cached = WorldMap.interacted_entity;


			if (WorldMap.IsOpen && WorldMap.interacted_entity_cached.IsAlive())
			{
				ref var interactable = ref WorldMap.interacted_entity_cached.GetComponent<Interactable.Data>();
				if (interactable.IsNotNull())
				{
					using (var dock = GUI.Dock.New((uint)WorldMap.interacted_entity_cached.id))
					{
						var sub_size = interactable.window_size;
						//using (var window_sub = window.BeginChildWindow("worldmap.side.right.sub", GUI.AlignX.Left, GUI.AlignY.Top, pivot: new(1.00f, 0.00f), size: sub_size + new Vector2(16, 16), padding: new(8, 8), open: WorldMap.selected_entity.IsValid(), tex_bg: GUI.tex_window_popup_b))
						using (var window = GUI.Window.Standalone(identifier: "worldmap.interact"u8,
						pivot: new(0.50f, 0.00f),
						position: new(GUI.CanvasSize.X * 0.50f, 32),
						force_position: false,
						size: sub_size + new Vector2(16, 16),
						size_min: interactable.window_size_min,
						padding: new(4, 4)))
						{
							//var interacted_entity_cached = WorldMap.interacted_entity;

							if (window.appearing)
							{
								//App.WriteLine("appearing");
								//Interactable.Close();
								//Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f);
							}

							Interactable.Hide();


							if (window.show)
							{

								//GUI.DrawWindowBackground(GUI.tex_window_popup_b, padding: new(4), color: GUI.col_default);
								GUI.DrawWindowBackground(GUI.tex_window, padding: new(4), color: null); //, color: GUI.col_default);

								using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
								{
									using (var group_tabs = GUI.Group.New(size: new(GUI.RmX - GUI.RmY, GUI.RmY)))
									using (GUI.Clip.Push(group_tabs.GetOuterRect()))
									{
										var count = dock.GetTabCount();
										for (var i = 0u; i < count; i++)
										{
											if (i > 0) GUI.SameLine();
											dock.DrawTab(i, new(0, group_row.size.Y));
										}
									}

									GUI.SameLine();

									using (var group_close = group_row.Split(size: new Vector2(group_row.size.Y), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
									{
										//group_close.DrawBackground(GUI.tex_window_sidebar_b);

										if (GUI.DrawSpriteButton("close"u8, new("ui_icons_window", 16, 16, 0, 0), size: GUI.Rm, color: GUI.font_color_red_b.WithColorMult(0.75f), color_hover: GUI.font_color_red_b.WithColorMult(1.00f), play_sound: false))
										{
											//window.Close();
											WorldMap.interacted_entity = default;
										}
										GUI.DrawHoverTooltip("Close"u8);
									}
								}

								GUI.SeparatorThick();

								dock.SetSpace(GUI.Rm);

								if (GUI.GetKeyboard().GetKeyDown(Keyboard.Key.Escape | Keyboard.Key.E) && window.Close())
								{
									WorldMap.interacted_entity = default;
									//Sound.PlayGUI(GUI.sound_window_open, volume: 0.40f);

								}

								if (WorldMap.interacted_entity_cached != 0 && WorldMap.interacted_entity == 0)
								{
									//App.WriteLine("close");
									//Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
								}

								if (interactable.window_size_misc.X > 0)
								{
									using (var window_side = window.BeginChildWindow(identifier: "worldmap.interact.sub"u8, anchor_x: GUI.AlignX.Left, anchor_y: GUI.AlignY.Center, size: new(interactable.window_size_misc.X + 8, (interactable.window_size_misc.Y <= 0 ? interactable.window_size.Y : interactable.window_size_misc.Y) + 8), padding: new(4), offset: new(6, 0), open: true))
									{
										if (window_side.show)
										{
											//using (GUI.Group.New(GUI.Rm))
											//using (var scrollbox = GUI.Scrollbox.New("worldmap.interact.sub.scroll"u8, size: GUI.Rm))
											using (var dock_misc = GUI.Dock.New("Misc"u8))
											{
												//GUI.DrawInventoryDock(Inventory.Type.Essence, new(48, 48));

												dock_misc.SetSpace(size: GUI.Rm);
											}

											//GUI.Text("hi");
											//GUI.DrawInventoryDock(this.vehicle.inventory_type, GUI.Rm);
										}
									}
								}

								if (true) //false && interactable.flags.HasNone(Interactable.Flags.No_Tab))
								{
									//using (var window_side = window.BeginChildWindow("worldmap.interact.children"u8, GUI.AlignX.Center, GUI.AlignY.Bottom, size: new(interactable.window_size.X, 32), pivot: new(0.50f, 0), offset: new(0, -8), open: true))
									using (var window_side = window.BeginChildWindow(identifier: "worldmap.interact.children"u8,
									anchor_x: GUI.AlignX.Center,
									anchor_y: GUI.AlignY.Top,
									size: new(interactable.window_size.X, 32),
									pivot: new(0.50f, 0),
									offset: new(0, -32),
									open: true,
									tex_bg: default(Texture.Handle)))
									{
										if (window_side.show)
										{
											var random = XorRandom.New(true);
											//var window_rect = GUI.GetCurrentWindowRect();

											var ent_root = WorldMap.interacted_entity_cached.GetRoot(Relation.Type.Child);
											var is_root = ent_root == WorldMap.interacted_entity_cached;

											var ents_list = FixedArray.CreateSpan16NoInit<Entity>(out var ents_buffer).AsSpanList();
											ents_list.Add(ent_root);
											ent_root.GetChildren(ref ents_list, Relation.Type.Child);
											var ents_span = ents_list.GetSpan();

											//var ent_parent = is_root ? Entity.Default : ent_interacted.GetParent(relation: Relation.Type.Child);

											var window_rect = window.GetWindowRect();
											//GUI.DrawRect(window_rect, layer: GUI.Layer.Foreground);

											if (WorldMap.IsHovered() || GUI.IsHoveringRect(window_rect, window_only: false, allow_overlapped: true, clip: false))
											{
												if (GUI.GetMouse().GetKeyDown(Mouse.Key.Forward))
												{
													App.WriteLine($"forward");
													if (ents_span.TryGetNext(ref WorldMap.interacted_entity))
													{
														Sound.PlayGUI(GUI.sound_button, volume: 0.40f, pitch: random.NextFloatExtra(1.00f, 0.02f));
													}
												}
												else if (GUI.GetMouse().GetKeyDown(Mouse.Key.Back))
												{
													App.WriteLine($"back");
													if (ents_span.TryGetPrev(ref WorldMap.interacted_entity))
													{
														Sound.PlayGUI(GUI.sound_button, volume: 0.40f, pitch: random.NextFloatExtra(0.95f, 0.02f));
													}
												}
											}

											//if (ent_root.IsAlive())
											//{
											//	var color = ent_root.GetComponent<WorldMap.Marker.Data>().OrDefault().color;

											//	GUI.DrawTab3(ent_root.GetName(), size: new(64, 32), index: ent_root, selected_index: ref interacted_entity, inner: true, color: color);
											//	if (GUI.IsItemHovered())
											//	{
											//		GUI.DrawEntityMarker(ent_root, cross_size: 0.125f, layer: GUI.Layer.Foreground);
											//		WorldMap.hovered_entity.pending = ent_root;
											//	}

											//	GUI.SameLine();
											//}

											//if (ent_parent != ent_root)
											//{
											//	if (ent_parent.IsAlive())
											//	{
											//		GUI.DrawTab3(ent_parent.GetName(), size: new(64, 32), index: ent_parent, selected_index: ref interacted_entity, inner: false);
											//		if (GUI.IsItemHovered())
											//		{
											//			GUI.DrawEntityMarker(ent_parent, cross_size: 0.125f, layer: GUI.Layer.Foreground);
											//			WorldMap.hovered_entity.pending = ent_parent;
											//		}

											//		GUI.SameLine();
											//	}
											//}

											for (var i = 0; i < ents_span.Length; i++)
											{
												var ent = ents_span[i];
												var name = ent.GetName();

												if (i == 0)
												{
													var color = ent.GetComponent<WorldMap.Marker.Data>().OrDefault().color;

													if (GUI.DrawTab3(name, size: new(64, 32), index: ent, selected_index: ref WorldMap.interacted_entity, inner: true, color: color))
													{
														App.WriteLine("press root tab");
													}

													//if (GUI.IsItemHovered())
													//{
													//	GUI.DrawEntityMarker(ent_root, cross_size: 0.125f, layer: GUI.Layer.Foreground);
													//	WorldMap.hovered_entity.pending = ent_root;
													//}
												}
												else
												{
													GUI.SameLine();

													if (GUI.DrawTab3(name, size: new(64, 32), index: ent, selected_index: ref WorldMap.interacted_entity, inner: false, font_size: 20, padding: 20.00f))
													{
														App.WriteLine("press child tab");
													}
													//if (GUI.IsItemHovered())
													//{
													//	GUI.DrawEntityMarker(ent_child, cross_size: 0.125f, layer: GUI.Layer.Foreground);
													//	WorldMap.hovered_entity.pending = ent_child;
													//}
												}

												if (GUI.IsItemHovered())
												{
													GUI.DrawEntityMarker(ent, cross_size: 0.125f, layer: GUI.Layer.Foreground);
													WorldMap.hovered_entity.pending = ent;
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
			//WorldMap.interacted_entity_cached = WorldMap.interacted_entity;
		}

		//public struct UnitInfo
		//{
		//	public Entity entity;


		//	public List<UnitInfo> units_children;
		//}

		//public static readonly List<UnitInfo> unit_infos_cached = new(32);


		[Flags]
		public enum UnitSearchFilter: uint
		{
			None = 0u,

			Units = 1u << 0,
			Regions = 1u << 1,
			Settlements = 1u << 2,
		}

		internal static UnitSearchFilter edit_unit_search_filter = UnitSearchFilter.Units | UnitSearchFilter.Regions | UnitSearchFilter.Settlements;
		internal static string edit_units_search = string.Empty;

		[Shitcode]
		private static void DrawLeftWindow(ref AABB rect)
		{
			using (var window = GUI.Window.Standalone("worldmap.side.left"u8, position: rect.a + new Vector2(6, 80), size: new(284, Maths.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
			{
				if (window.show)
				{
					window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

					var mod_context = App.GetModContext();

					ref var world_info = ref Client.GetWorldInfo();
					ref var region = ref World.GetGlobalRegion();

					//using (GUI.Group.New(size: GUI.Rm))
					//{
					using (var group_list = GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
					{
						//if (GUI.TextInput("worldmap.left.search"u8, "<search>"u8, ref edit_units_search, size: new(GUI.RmX * 0.50f, 32), max_length: 32))
						if (GUI.TextInput("worldmap.left.search"u8, "<search>"u8, ref edit_units_search, size: new(GUI.RmX, 32), max_length: 32))
						{

						}
						GUI.FocusOnCtrlF();

						//GUI.SameLine();

						//GUI.EnumInput("worldmap.left.filter"u8, ref edit_unit_search_filter, size: new(GUI.RmX, 32), show_label: false);
						GUI.EnumInput("worldmap.left.filter"u8, ref edit_unit_search_filter, size: new(GUI.RmX, 32), show_label: false);

						var is_filtering = !edit_units_search.IsNullOrEmpty();

						using (var scroll = GUI.Scrollbox.New("units.scroll"u8, size: new(GUI.RmX, GUI.RmY - 48)))
						{
							using (var collapsible = GUI.Collapsible2.New("units.regions.collapsible"u8, new Vector2(GUI.RmX, 32), default_open: true))
							{
								GUI.TitleCentered("Regions"u8, size: 24, pivot: new(0.00f, 0.50f));

								if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
								{
									for (var i = 1u; i < Region.max_count; i++)
									{
										ref var map_info = ref World.GetMapInfo((byte)i);
										if (map_info.IsNotNull())
										{
											using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
											{
												if (group_row.IsVisible())
												{
													var h_location = map_info.h_location;
													ref var location_data = ref h_location.GetData();

													var ent_location = h_location.GetGlobalEntity();
													var map_identifier = world_info.regions[i];

													var map_asset = mod_context.GetMap(map_identifier);
													//if (map_asset != null)
													//{
													//	var tex_thumbnail = map_asset.GetThumbnail();
													//	if (tex_thumbnail != null)
													//	{
													//		GUI.DrawTexture(tex_thumbnail.Identifier, rect_icon, GUI.Layer.Window, color: color_thumbnail.WithAlphaMult(alpha));
													//	}

													//	GUI.DrawBackground(GUI.tex_frame_white, rect_icon, padding: new(2), color: color_frame.WithAlphaMult(alpha));
													//}

													using (GUI.ID<Region.Info, IMap.Info>.Push(i))
													{
														var is_selected = WorldMap.interacted_entity_cached == ent_location;
														var contains = WorldMap.hs_selected_entities.Contains(ent_location);
														var is_selectable = WorldMap.CanPlayerControlUnit(ent_location, Client.GetPlayerHandle());

														using (GUI.Alpha.Push(GUI.GetEnabledAlpha(is_selectable)))
														//using (GUI.Disabled.Push(true, !is_selectable))
														{
															group_row.DrawBackground(GUI.tex_panel);

															//if (location_data.IsNotNull())
															//{
															//	icon = location_data.thumbnail;
															//}

															//var map_asset = default(MapAsset);
															using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmY)))
															{
																//ref var location_data = ref ent_location.GetAssetData<ILocation.Data>();
																if (map_asset != null)
																{
																	GUI.DrawMapThumbnail(map_asset, size: GUI.Rm, show_frame: false);
																}
																//else if (location_data.IsNotNull())
																//{
																//	GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 0.25f);
																//}
																//else
																//{
																//	//var icon = marker.icon;
																//	//GUI.DrawSpriteCentered(icon, group_row.GetInnerRect(), layer: GUI.Layer.Window, pivot: new(1.00f, 0.50f), scale: 2.00f, color: marker.color_override.IsVisible() ? marker.color_override : marker.color);
																//	//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
																//}

																GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
															}

															GUI.SameLine();

															GUI.TitleCentered(map_info.name, size: 20, pivot: new(0.00f, 0.00f), offset: new(2, 2));
															GUI.TextShadedCentered(location_data.h_prefecture.GetShortName(), pivot: new(0.00f, 1.00f), offset: new(2, -2));

															//var ent_parent = entity.GetParent(Relation.Type.Child);
															//if (ent_parent.IsValid() && ent_parent.TryGetAssetName(out var name_parent))
															//{
															//	GUI.TextShadedCentered(name_parent, size: 14, pivot: new(0.00f, 1.00f), color: GUI.font_color_desc);
															//}
															//else
															//{
															//	//GUI.TextShadedCentered(entity.GetFaction().GetName(), size: 14, pivot: new(0.00f, 1.00f), color: GUI.font_color_desc);
															//}

															if (GUI.Selectable3(ent_location.GetShortID(), group_row.GetOuterRect(), is_selected || contains, is_readonly: !is_selectable))
															{
																var result = WorldMap.SelectUnitBehavior(ent_location, SelectUnitMode.Single, SelectUnitFlags.Multiselect | SelectUnitFlags.Hold_Shift | SelectUnitFlags.Toggle, selected: is_selected);
																if (result.HasAny(SelectUnitResults.Changed))
																{
																	if (result.HasAny(SelectUnitResults.Removed))
																	{
																		WorldMap.interacted_entity = default;
																	}
																	else
																	{
																		WorldMap.interacted_entity.Toggle(ent_location, true);
																		WorldMap.FocusEntity(ent_location, interact: false);
																	}
																}

																if (WorldMap.interacted_entity_cached == ent_location && (ent_location.TryGetAsset(out ILocation.Definition location_asset))) // || ent_parent.TryGetAsset(out location_asset)))
																{
																	WorldMap.h_selected_location = location_asset;
																}
																else
																{
																	WorldMap.h_selected_location = default;
																}
															}
														}

														if (GUI.IsItemHovered())
														{
															if (GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
															{
																WorldMap.FocusEntity(ent_location, interact: false);
															}
															GUI.DrawEntityMarker(ent_location, cross_size: 0.125f, layer: GUI.Layer.Foreground);
														}
													}
												}
											}
										}
									}
								}
							}

							//// TODO: this is awful
							//using (var collapsible = GUI.Collapsible2.New("units.collapsible"u8, new Vector2(GUI.RmX, 32), default_open: true))
							//{
							//	GUI.TitleCentered("Units"u8, size: 24, pivot: new(0.00f, 0.50f));

							//	if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
							//	{
							//		var rows = region.IterateQuery<WorldMap.Marker.GetAllMarkersQuery>().HasParent(Relation.Type.Child, false);
							//		if (edit_unit_search_filter.HasNone(UnitSearchFilter.Units)) rows = rows.HasComponent<Unit.Data>(false);
							//		if (edit_unit_search_filter.HasNone(UnitSearchFilter.Regions)) rows = rows.HasComponent<Zone.Data>(false);
							//		if (edit_unit_search_filter.HasNone(UnitSearchFilter.Settlements)) rows = rows.HasComponent<Settlement.Data>(false);

							//		foreach (ref var row in rows) //.HasComponent<Location.Data>(false))
							//		{
							//			row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
							//			in WorldMap.Marker.Data marker,
							//			in Transform.Data transform,
							//			ref Nameable.Data nameable,
							//			bool has_parent) =>
							//			{
							//				//return;
							//				if (has_parent) return;
							//				if (marker.flags.HasAny(Marker.Data.Flags.Hidden)) return;

							//				//var pos = transform.GetInterpolatedPosition();
							//				//var asset_scale = Maths.Clamp(marker.scale, 0.250f, 1.00f);

							//				////if ((has_parent && marker.flags.HasAny(Marker.Data.Flags.Hide_If_Parented)))
							//				//if (has_parent)
							//				//{
							//				//	return;
							//				//}
							//				//else
							//				//{

							//				//}

							//				if (is_filtering && !nameable.name.ToString().Contains(edit_units_search, StringComparison.OrdinalIgnoreCase)) return;

							//				using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(4, 4)))
							//				{
							//					if (group_row.IsVisible())
							//					{
							//						using (GUI.ID.Push(entity))
							//						{
							//							var is_selected = WorldMap.interacted_entity_cached == entity;
							//							var contains = WorldMap.hs_selected_entities.Contains(entity);
							//							var is_selectable = WorldMap.CanPlayerControlUnit(entity, Client.GetPlayerHandle());

							//							using (GUI.Alpha.Push(GUI.GetEnabledAlpha(is_selectable)))
							//							//using (GUI.Disabled.Push(true, !is_selectable))
							//							{
							//								group_row.DrawBackground(GUI.tex_panel);

							//								//if (location_data.IsNotNull())
							//								//{
							//								//	icon = location_data.thumbnail;
							//								//}

							//								var map_asset = default(MapAsset);
							//								using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmY)))
							//								{
							//									ref var location_data = ref entity.GetAssetData<ILocation.Data>();
							//									if (map_asset != null)
							//									{
							//										GUI.DrawMapThumbnail(map_asset, size: GUI.Rm, show_frame: false);
							//									}
							//									else if (location_data.IsNotNull())
							//									{
							//										GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 0.25f);
							//									}
							//									else
							//									{
							//										var icon = marker.icon;
							//										GUI.DrawSpriteCentered(icon, group_row.GetInnerRect(), layer: GUI.Layer.Window, pivot: new(1.00f, 0.50f), scale: 2.00f, color: marker.color_override.IsVisible() ? marker.color_override : marker.color);
							//										//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
							//									}

							//									GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
							//								}

							//								GUI.SameLine();

							//								GUI.TitleCentered(nameable.name, size: 16, pivot: new(0.00f, 0.00f), offset: new(0, 0));

							//								//var ent_parent = entity.GetParent(Relation.Type.Child);
							//								//if (ent_parent.IsValid() && ent_parent.TryGetAssetName(out var name_parent))
							//								//{
							//								//	GUI.TextShadedCentered(name_parent, size: 14, pivot: new(0.00f, 1.00f), color: GUI.font_color_desc);
							//								//}
							//								//else
							//								//{
							//								//	//GUI.TextShadedCentered(entity.GetFaction().GetName(), size: 14, pivot: new(0.00f, 1.00f), color: GUI.font_color_desc);
							//								//}

							//								if (GUI.Selectable3(entity.GetShortID(), group_row.GetOuterRect(), is_selected || contains, is_readonly: !is_selectable))
							//								{
							//									var result = WorldMap.SelectUnitBehavior(entity, SelectUnitMode.Single, SelectUnitFlags.Multiselect | SelectUnitFlags.Hold_Shift | SelectUnitFlags.Toggle, selected: is_selected);
							//									if (result.HasAny(SelectUnitResults.Changed))
							//									{
							//										if (result.HasAny(SelectUnitResults.Removed))
							//										{
							//											WorldMap.interacted_entity = default;
							//										}
							//										else
							//										{
							//											WorldMap.interacted_entity.Toggle(entity, true);
							//										}
							//									}

							//									if (WorldMap.interacted_entity_cached == entity && (entity.TryGetAsset(out ILocation.Definition location_asset))) // || ent_parent.TryGetAsset(out location_asset)))
							//									{
							//										WorldMap.h_selected_location = location_asset;
							//									}
							//									else
							//									{
							//										WorldMap.h_selected_location = default;
							//									}
							//								}
							//							}

							//							if (GUI.IsItemHovered())
							//							{
							//								if (GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
							//								{
							//									WorldMap.FocusEntity(entity, interact: false);
							//								}
							//								GUI.DrawEntityMarker(entity, cross_size: 0.125f, layer: GUI.Layer.Foreground);
							//							}
							//						}
							//					}
							//				}

							//				//ref var enterable = ref entity.GetComponent<Enterable.Data>();
							//				//if (enterable.IsNotNull())
							//				{
							//					var children_list = FixedArray.CreateSpan8NoInit<Entity>(out var children_buffer).AsSpanList();
							//					entity.GetChildren(ref children_list, Relation.Type.Child);
							//					entity.GetChildren(ref children_list, Relation.Type.Stored);
							//					var children = children_list.GetSpan();

							//					if (!children.IsEmpty)
							//					{
							//						foreach (var ent_child in children)
							//						{
							//							GUI.NewLine(0);
							//							GUI.OffsetLine(32);

							//							using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 4)))
							//							{
							//								if (group_row.IsVisible())
							//								{
							//									using (GUI.ID.Push(ent_child))
							//									{
							//										var is_selected = WorldMap.interacted_entity_cached == ent_child; // || WorldMap.hs_selected_entities.Contains(ent_child);
							//										var contains = WorldMap.hs_selected_entities.Contains(ent_child);
							//										var is_selectable = WorldMap.CanPlayerControlUnit(ent_child, Client.GetPlayerHandle());

							//										using (GUI.Alpha.Push(GUI.GetEnabledAlpha(is_selectable)))
							//										{
							//											group_row.DrawBackground(GUI.tex_panel);

							//											ref var marker_child = ref ent_child.GetComponent<Marker.Data>();
							//											if (marker_child.IsNotNull())
							//											{
							//												GUI.DrawSpriteCentered(marker_child.icon, group_row.GetInnerRect(), layer: GUI.Layer.Window, pivot: new(1.00f, 0.50f), scale: 2.00f);
							//											}

							//											GUI.TitleCentered(ent_child.GetName(), size: 16, pivot: new(0.00f, 0.00f), offset: new(0, 0));
							//											//GUI.TextShadedCentered("Test", size: 14, pivot: new(0.00f, 1.00f), color: GUI.font_color_desc);

							//											//var selected = asset == h_selected_location; // selected_region_id == i;

							//											if (GUI.Selectable3(ent_child.GetShortID(), group_row.GetOuterRect(), contains || is_selected, is_readonly: !is_selectable))
							//											{
							//												var result = WorldMap.SelectUnitBehavior(ent_child, SelectUnitMode.Single, SelectUnitFlags.Multiselect | SelectUnitFlags.Hold_Shift | SelectUnitFlags.Toggle);
							//												if (result.HasAny(SelectUnitResults.Changed))
							//												{
							//													if (result.HasAny(SelectUnitResults.Removed)) WorldMap.interacted_entity = default;
							//													else
							//													{
							//														WorldMap.interacted_entity.Toggle(ent_child, true);
							//													}
							//												}
							//												//if (WorldMap.hs_selected_entities.Count <= 1 || GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift))
							//												//{
							//												//	if (ent_child.HasComponent<WorldMap.Unit.Data>())
							//												//	{
							//												//		WorldMap.hs_selected_entities.Toggle(ent_child, !is_selected);
							//												//	}
							//												//}
							//												//else
							//												//{
							//												//	WorldMap.selected_entity.Toggle(ent_child, !is_selected);
							//												//}


							//												var ent_parent = entity.GetParent(Relation.Type.Child);
							//												if (WorldMap.interacted_entity_cached == ent_child && (ent_child.TryGetAsset(out ILocation.Definition location_asset) || entity.TryGetAsset(out location_asset) || ent_parent.TryGetAsset(out location_asset)))
							//												{
							//													WorldMap.h_selected_location = location_asset;
							//												}
							//												else
							//												{
							//													WorldMap.h_selected_location = default;
							//												}
							//											}
							//										}

							//										if (GUI.IsItemHovered())
							//										{
							//											if (GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
							//											{
							//												WorldMap.FocusEntity(ent_child, interact: false);
							//											}
							//											GUI.DrawEntityMarker(ent_child, cross_size: 0.125f, layer: GUI.Layer.Foreground);
							//											//hs_selected_entities.Add(ent_child);
							//										}
							//									}
							//								}
							//							}
							//						}
							//					}
							//				}
							//			});

							//		}
							//		//var ts_elapsed = ts.GetMilliseconds();
							//		//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", GUI.CanvasSize * 0.50f, layer: GUI.Layer.Foreground);
							//	}
							//}

						
						}
					}
					//}
				}
			}
		}

		private static void DrawRightWindow(bool is_loading, ref AABB rect)
		{
			if (selected_region_id != 0 || h_selected_location != 0)
			{
				//var draw_external = true;

				using (var window = GUI.Window.Standalone("worldmap.side.right"u8, position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(348, Maths.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
				{
					if (window.show)
					{
						if (false)
						{
							window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

							var h_player = Client.GetPlayerHandle();
							ref var player_data = ref h_player.GetData();

							var h_character_main = player_data.IsNotNull() ? player_data.h_character_main : default;
							ref var character_main_data = ref h_character_main.GetData();

							var h_character_current = Client.GetCharacterHandle();
							ref var character_current_data = ref h_character_current.GetData();

							var h_company = Client.GetCompanyHandle();
							ref var company_data = ref h_company.GetData();

							using (GUI.Group.New(size: GUI.Rm))
							{
								using (var group_title = GUI.Group.New(size: new(GUI.RmX, 64), padding: new(0, 0)))
								{
									var button_size = new Vec2f(GUI.RmY);

									using (var button = GUI.CustomButton.New(GUI.Hash<ICharacter.Handle>.New(1), size: button_size, sound: GUI.sound_button))
									{
										Dormitory.DrawCharacterHead(h_character_main, new(GUI.RmY));
										if (button.pressed)
										{
											ent_context_current = h_character_main.GetGlobalEntity();
											GUI.selected_entity = ent_context_current;
										}
									}

									GUI.SameLine();

									using (var button = GUI.CustomButton.New(GUI.Hash<ICharacter.Handle>.New(2), size: button_size, sound: GUI.sound_button))
									{
										Dormitory.DrawCharacterHead(h_character_current, new(GUI.RmY));
										if (button.pressed)
										{
											ent_context_current = Client.GetControlledEntity();
											GUI.selected_entity = ent_context_current;
										}
									}

									GUI.SameLine();

									using (var button = GUI.CustomButton.New(GUI.Hash<ICompany.Handle>.New(1), size: button_size, sound: GUI.sound_button))
									{
										if (button.pressed)
										{
											ent_context_current = default;
											GUI.selected_entity = ent_context_current;
										}
									}
									//GUI.TitleCentered(location_data.name, size: 32, pivot: new(0.00f, 0.50f));
								}
								//GUI.FocusableAsset(location_asset.GetHandle());

								GUI.SeparatorThick();

								//var map_asset = default(MapAsset);

								//ref var region_info = ref World.GetRegionInfo(selected_region_id);
								//if (region_info.IsNotNull())
								//{
								//	ref var map_info = ref region_info.map_info.GetRefOrNull();
								//	if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
								//	{
								//		map_asset = App.GetModContext().GetMap(region_info.map);
								//	}
								//}

								using (var group_top = GUI.Group.New(size: new(GUI.RmX, 200), padding: new(4, 4)))
								{
									using (var group_desc = GUI.Group.New(size: GUI.Rm - new Vec2f(48 * 2, 0), padding: new(4, 4)))
									{
										group_desc.DrawBackground(GUI.tex_panel, inner: false);

										using (GUI.Wrap.Push(GUI.RmX))
										{
											//GUI.LabelShaded("Name:"u8, ent_context_current.GetName(), font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);
											//GUI.TitleCentered(ent_context_current.GetName(), size: 24, pivot: new(0.00f, 0.00f));
											GUI.Title(ent_context_current.GetName(), size: 24);

											GUI.SeparatorThick();

											GUI.NewLine(6);

											//ref var origin_data = ref character_data_selected.origin.GetData();
											//ref var species_data = ref character_data_selected.species.GetData();
											//ref var faction_data = ref character_data_selected.faction.GetData();
											////ref var company_data = ref character_data.h_company.GetData();

											//if (species_data.IsNotNull())
											//{
											//	GUI.LabelShaded("Species:"u8, species_data.name, font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);
											//}

											//GUI.LabelShaded("Date of Birth:"u8, current_year - character_data_selected.age, format: "0' S.D.'", font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);

											//GUI.NewLine(6);

											////if (origin_data.IsNotNull())
											////{
											//GUI.LabelShaded("Occupation:"u8, character_data_selected.origin.GetName().OrDefault("N/A"), font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);
											////}

											//GUI.LabelShaded("Company:"u8, character_data_selected.h_company.GetName().OrDefault("N/A"), font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);

											////if (company_data.IsNotNull())
											////{
											////}

											//GUI.NewLine(6);

											//GUI.LabelShaded("Faction:"u8, character_data_selected.faction.GetName().OrDefault("N/A"), font_a: GUI.Font.Superstar, font_b: GUI.Font.Monaco, size_a: 16, size_b: 14);

										}
									}

									GUI.SameLine();

									using (var group_inventories = GUI.Group.New(size: new(48 * 2, GUI.RmY)))
									{
										if (ent_context_current.IsAlive())
										{
											var inventories = ent_context_current.GetInventories();
											foreach (var h_inventory in inventories)
											{
												if (h_inventory.IsValid() && h_inventory.Flags.HasNone(Inventory.Flags.Hidden))
												{
													using (GUI.Group.New(size: h_inventory.GetFrameSize(0, 2)))
													{
														GUI.DrawInventory(h_inventory, is_readonly: false);
													}
												}
											}
										}
									}

									//using (var group_left = GUI.Group.New(size: new(128 + 12, 0)))
									//{
									//	//using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmX)))
									//	//{
									//	//	//if (map_asset != null)
									//	//	//{
									//	//	//	GUI.DrawMapThumbnail(map_asset, size: GUI.Rm, show_frame: false);
									//	//	//}
									//	//	//else
									//	//	//{
									//	//	//	//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
									//	//	//	//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
									//	//	//}

									//	//	GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
									//	//}

									//	//if (map_asset != null)
									//	//{
									//	//	var color = GUI.col_button_ok;
									//	//	var alpha = 1.00f;

									//	//	if (GUI.DrawIconButton("info"u8, new(GUI.tex_icons_widget, 16, 16, 6, 1), size: new(48, 48)))
									//	//	{

									//	//	}

									//	//	GUI.SameLine();

									//	//	if (Client.GetRegionID() != selected_region_id)
									//	//	{
									//	//		if (GUI.DrawButton("Join"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
									//	//		{
									//	//			Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

									//	//			window.Close();
									//	//			GUI.RegionMenu.ToggleWidget(false);

									//	//			//Client.TODO_LoadRegion(region_id);
									//	//		}
									//	//	}
									//	//	else
									//	//	{
									//	//		color = GUI.col_button_error;
									//	//		if (GUI.DrawButton("Leave"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
									//	//		{
									//	//			Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
									//	//		}
									//	//	}
									//	//}
									//	//else
									//	//{
									//	//	//if (GUI.DrawButton("Button"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: false, color: GUI.col_button, text_color: GUI.font_color_button_text))
									//	//	//{

									//	//	//}
									//	//}
									//}

									//GUI.SameLine();

									//using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
									//{
									//	group_desc.DrawBackground(GUI.tex_panel, inner: true);

									//	using (GUI.Wrap.Push(GUI.RmX))
									//	{
									//		//if (map_asset != null)
									//		//{
									//		//	GUI.TextShaded(map_asset.Description);
									//		//}
									//		//else
									//		//{
									//		//	GUI.TextShaded(location_data.desc);
									//		//}
									//	}
									//}
								}

								GUI.SeparatorThick();

								using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(2, 2)))
								{
									using (var group_bottom = GUI.Group.New2(size: GUI.Rm))
									{
										using (var scrollbox = GUI.Scrollbox.New("scroll.bottom"u8, size: GUI.Rm))
										{
											//Span<Entity> children_span = FixedArray.CreateSpan32NoInit<Entity>(out var buffer_children);
											//ent_asset.GetAllChildren(ref children_span, false);
											//ent_asset.GetChildren(ref children_span, Relation.Type.Child);

											//foreach (var ent_child in children_span)
											//{
											//	//if (ILocation.TryGetAsset(ent_child, out var h_location_child))
											//	{
											//		if (ent_child.TryGetAsset(out ILocation.Definition asset_location) || ent_child.TryGetAsset(out IEntrance.Definition asset_entrance))
											//		{
											//			using (GUI.ID.Push(ent_child))
											//			using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
											//			{
											//				using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
											//				{
											//					var color_frame = Color32BGRA.GUI;

											//					ref var faction_data = ref ent_child.GetFactionHandle().GetData();
											//					if (faction_data.IsNotNull())
											//					{
											//						color_frame = faction_data.color_a;
											//					}

											//					group_icon.DrawBackground(GUI.tex_slot_white, color: color_frame);
											//				}

											//				GUI.SameLine();

											//				using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
											//				{
											//					GUI.Title(ent_child.GetName(), size: 24);
											//					//group_right.DrawBackground(GUI.tex_window_popup);
											//				}

											//				var is_selected = WorldMap.interacted_entity == ent_child;
											//				if (GUI.Selectable3(ent_child.GetShortID(), group_row.GetInnerRect(), selected: is_selected))
											//				{
											//					WorldMap.interacted_entity.Toggle(ent_child, !is_selected);
											//					GUI.SetDebugEntity(ent_child);
											//				}
											//			}

											//			GUI.SeparatorThick();
											//		}
											//	}
											//}
										}
									}
								}
							}
						}
						else
						{
							window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

							if (h_selected_location != 0)
							{
								ref var location_data = ref h_selected_location.GetData(out var location_asset);
								if (location_data.IsNotNull())
								{
									var ent_asset = location_asset.GetGlobalEntity();
									selected_region_id = h_selected_location.GetRegionID();

									//for (var i = 0; i < Region.max_count; i++)
									//{
									//	ref var region_info = ref World.GetRegionInfo((byte)i);
									//	if (region_info.IsNotNull())
									//	{
									//		ref var map_info = ref region_info.map_info.GetRefOrNull();
									//		if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
									//		{
									//			selected_region_id = (byte)i;
									//			break;
									//		}
									//	}
									//}

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
											}
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
														GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
													}

													GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
												}

												if (map_asset != null)
												{
													var color = GUI.col_button_ok;
													var alpha = 1.00f;

													//if (GUI.DrawIconButton("info"u8, new(GUI.tex_icons_widget, 16, 16, 6, 1), size: new(48, 48)))
													//{

													//}

													//GUI.SameLine();

													//if (Client.GetRegionID() != selected_region_id)
													//{
													//	if (GUI.DrawButton("Join"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
													//	{
													//		Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

													//		window.Close();
													//		GUI.RegionMenu.ToggleWidget(false);

													//		//Client.TODO_LoadRegion(region_id);
													//	}
													//}
													//else
													//{
													//	color = GUI.col_button_error;
													//	if (GUI.DrawButton("Leave"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
													//	{
													//		Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
													//	}
													//}
												}
												else
												{
													//if (GUI.DrawButton("Button"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: false, color: GUI.col_button, text_color: GUI.font_color_button_text))
													//{

													//}
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

										using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(2, 2)))
										{
											using (var group_bottom = GUI.Group.New2(size: GUI.Rm))
											{
												using (var scrollbox = GUI.Scrollbox.New("scroll.bottom"u8, size: GUI.Rm))
												{
													Span<Entity> children_span = FixedArray.CreateSpan32NoInit<Entity>(out var buffer_children);
													//ent_asset.GetAllChildren(ref children_span, false);
													ent_asset.GetChildren(ref children_span, Relation.Type.Child);

													foreach (var ent_child in children_span)
													{
														//if (ILocation.TryGetAsset(ent_child, out var h_location_child))
														{
															if (ent_child.TryGetAsset(out ILocation.Definition asset_location) || ent_child.TryGetAsset(out IEntrance.Definition asset_entrance))
															{
																using (GUI.ID.Push(ent_child))
																using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
																{
																	using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
																	{
																		var color_frame = Color32BGRA.GUI;

																		ref var faction_data = ref ent_child.GetFactionHandle().GetData();
																		if (faction_data.IsNotNull())
																		{
																			color_frame = faction_data.color_a;
																		}

																		group_icon.DrawBackground(GUI.tex_slot_white, color: color_frame);

																		//ref var marker = ref ent_child.GetComponent<Marker.Data>();
																		//if (marker.IsNotNull())
																		//{
																		//	GUI.DrawSpriteCentered(marker.icon, group_icon.GetInnerRect(), layer: GUI.Layer.Window, scale: 2.00f);
																		//}

																		//if (GUI.Selectable3(ent_child.GetShortID(), group_icon.GetInnerRect(), selected: selected))
																		//{
																		//	WorldMap.selected_entity = selected ? default : ent_child;
																		//	GUI.SetDebugEntity(ent_child);
																		//}
																	}
																	//if (GUI.IsItemHovered())
																	//{
																	//	using (GUI.Tooltip.New(size: new(128, 0)))
																	//	{
																	//		using (GUI.Wrap.Push(GUI.RmX))
																	//		{
																	//			//GUI.Title(location_data_child.name_short, size: 20);
																	//			GUI.Title(ent_child.GetName(), size: 20);
																	//		}

																	//		GUI.SeparatorThick(new(-4, -4));

																	//		using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new(4)))
																	//		{
																	//			using (GUI.Wrap.Push(GUI.RmX))
																	//			{
																	//				//GUI.Text(location_data_child.desc);
																	//			}
																	//		}
																	//	}
																	//}
																	//GUI.FocusableAsset(h_location_child);

																	GUI.SameLine();

																	using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
																	{
																		GUI.TitleCentered(ent_child.GetName(), size: 24, pivot: new(0.00f, 0.50f));
																		//group_right.DrawBackground(GUI.tex_window_popup);

																		//if (GUI.DrawIconButton("info"u8, new(GUI.tex_icons_widget, 16, 16, 6, 1), size: new(48, 48)))
																		//{

																		//}

																		//GUI.SameLine();

																		using (var group_button = group_right.Split(size: new(80, GUI.RmY), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
																		{
																			var alpha = 1.00f;
																			if (Client.GetRegionID() != selected_region_id)
																			{
																				var color = GUI.col_button_ok;
																				if (GUI.DrawButton("Join"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
																				{
																					Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

																					window.Close();
																					GUI.RegionMenu.ToggleWidget(false);

																					//Client.TODO_LoadRegion(region_id);
																				}
																			}
																			else
																			{
																				var color = GUI.col_button_error;
																				if (GUI.DrawButton("Leave"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
																				{
																					Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
																				}
																			}
																		}
																	}

																	var is_selected = WorldMap.interacted_entity_cached == ent_child;
																	if (GUI.Selectable3(ent_child.GetShortID(), group_row.GetInnerRect(), selected: is_selected))
																	{
																		WorldMap.interacted_entity.Toggle(ent_child, !is_selected);
																		GUI.SetDebugEntity(ent_child);
																	}
																}

																GUI.SeparatorThick();
															}
														}
													}
												}
											}
										}
									}
								}

								if (GUI.GetKeyboard().GetKeyDown(Keyboard.Key.Escape | Keyboard.Key.E) && window.Close())
								{
									WorldMap.selected_region_id = 0;
									WorldMap.h_selected_location = 0;
									if (location_asset.GetGlobalEntity() == WorldMap.interacted_entity_cached) WorldMap.interacted_entity = default;
								}

								if (WorldMap.h_selected_location == 0 && WorldMap.selected_region_id == 0)
								{
									Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
								}
							}
							else
							{
								selected_region_id = 0;
							}
						}
					}
				}
			}
		}

		[Flags]
		public enum SelectUnitFlags: uint
		{
			None = 0u,

			Multiselect = 1u << 0,
			Hold_Shift = 1u << 1,
			Toggle = 1u << 2,
			Include_Children = 1u << 3
			//Toggle = 1u << 1
		}

		[Flags]
		public enum SelectUnitResults: uint
		{
			None = 0u,

			Changed = 1u << 0,
			Added = 1u << 1,
			Removed = 1u << 2,

		}

		public enum SelectUnitMode: uint
		{
			Undefined = 0,

			Single,
			Multiple
		}

		[Ugly]
		public static SelectUnitResults SelectUnitBehavior(Entity ent_unit, SelectUnitMode mode, SelectUnitFlags flags, bool? selected = null)
		{
			if (ent_unit.id == 0) return SelectUnitResults.None;
			var is_unit = ent_unit.HasComponent<WorldMap.Unit.Data>();
			//if (!is_unit) return SelectUnitResults.None;

			var hs = WorldMap.hs_selected_entities;
			var selected_count = hs.Count;
			var contains = hs.Contains(ent_unit);
			var is_selected = selected ?? contains;
			var results = SelectUnitResults.None;

			if (flags.HasAny(SelectUnitFlags.Multiselect) && (flags.HasNone(SelectUnitFlags.Hold_Shift) || GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift)))
			{
				if (is_selected || contains)
				{
					if (hs.Remove(ent_unit) || is_selected) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
				}
				else
				{
					if (hs.Add(ent_unit)) results |= SelectUnitResults.Added | SelectUnitResults.Changed;
				}
			}
			else
			{
				if (flags.HasAny(SelectUnitFlags.Toggle))
				{
					if (is_selected)
					{
						switch (mode)
						{
							case SelectUnitMode.Single:
							{
								hs.Clear();

								results |= SelectUnitResults.Changed;
								if (selected_count > 1)
								{
									//if (hs.Add(ent_unit) && !contains) results |= SelectUnitResults.Added;
									if (!contains && hs.Add(ent_unit)) results |= SelectUnitResults.Added;
									else results |= SelectUnitResults.Removed;
								}
								else
								{
									//if (contains) results |= SelectUnitResults.Removed;
									if (contains || is_selected) results |= SelectUnitResults.Removed;
								}
							}
							break;

							case SelectUnitMode.Multiple:
							{
								if (hs.Remove(ent_unit) || is_selected) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
							}
							break;
						}
					}
					else
					{
						switch (mode)
						{
							case SelectUnitMode.Single:
							{
								hs.Clear();

								results |= SelectUnitResults.Changed;
								if (hs.Add(ent_unit) && !contains) results |= SelectUnitResults.Added;
							}
							break;

							case SelectUnitMode.Multiple:
							{
								if (hs.Remove(ent_unit) || is_selected) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
							}
							break;
						}

						//hs.Clear();

						//results |= SelectUnitResults.Changed;
						//if (!contains && hs.Add(ent_unit)) results |= SelectUnitResults.Added;
					}
				}
				else
				{
					if (is_selected)
					{
						if (hs.Remove(ent_unit) || is_selected) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
					}
					else
					{
						hs.Clear();

						results |= SelectUnitResults.Changed;
						if (!contains && hs.Add(ent_unit)) results |= SelectUnitResults.Added;
					}
				}
			}

			// TODO: shithack
			if (!is_unit)
			{
				hs.Remove(ent_unit);
			}

			if (flags.HasAny(SelectUnitFlags.Include_Children) && results.HasAny(SelectUnitResults.Added | SelectUnitResults.Removed))
			{
				var h_player = Client.GetPlayerHandle();

				var children_span = FixedArray.CreateSpan16NoInit<Entity>(out var buffer);
				ent_unit.GetChildren(ref children_span, Relation.Type.Stored);

				if (results.HasAny(SelectUnitResults.Added))
				{
					foreach (var ent_child in children_span)
					{
						if (WorldMap.CanPlayerControlUnit(ent_child, h_player))
						{
							hs.Add(ent_child);
						}
					}
				}
				else if (results.HasAny(SelectUnitResults.Removed))
				{
					foreach (var ent_child in children_span)
					{
						hs.Remove(ent_child);
					}
				}
			}

			//}
			//else
			//{
			//	if (flags.HasAny(SelectUnitFlags.Multiselect) && (flags.HasNone(SelectUnitFlags.Hold_Shift) || GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift)))
			//	{
			//		if (is_selected)
			//		{
			//			if (hs.Remove(ent_unit)) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
			//		}
			//		else
			//		{
			//			if (hs.Add(ent_unit)) results |= SelectUnitResults.Added | SelectUnitResults.Changed;
			//		}
			//	}
			//	else
			//	{
			//		if (flags.HasAny(SelectUnitFlags.Toggle))
			//		{
			//			if (is_selected)
			//			{
			//				if (hs.Remove(ent_unit)) results |= SelectUnitResults.Removed | SelectUnitResults.Changed;
			//			}
			//			else
			//			{
			//				hs.Clear();

			//				results |= SelectUnitResults.Changed;
			//				if (hs.Add(ent_unit)) results |= SelectUnitResults.Added;
			//			}
			//		}
			//		else
			//		{

			//		}
			//	}
			//}

			return results;
		}

		//public static void SelectUnitBehavior(Entity ent_unit, SelectUnitFlags flags_empty = SelectUnitFlags.None, SelectUnitFlags flags_single = SelectUnitFlags.None, SelectUnitFlags flags_multiple = SelectUnitFlags.None)
		//{
		//	var selected_count = WorldMap.hs_selected_entities.Count;
		//	if (GUI.GetKeyboard().GetKeyNow(Keyboard.Key.LeftShift))
		//	{
		//		if (selected_count > 0)
		//		{
		//			WorldMap.hs_selected_entities.Remove(ent_unit);
		//		}
		//		else
		//		{
		//			WorldMap.hs_selected_entities.Add(ent_unit);
		//		}
		//	}
		//	else
		//	{
		//		if (selected_count > 1)
		//		{
		//			if (flags_multiple.HasAny(SelectUnitFlags.Clear)) WorldMap.hs_selected_entities.Clear();
		//			WorldMap.hs_selected_entities.Add(ent_unit);
		//		}
		//		else
		//		{
		//			if (flags_single.HasAny(SelectUnitFlags.Clear)) WorldMap.hs_selected_entities.Clear();
		//		}
		//	}
		//}

		public static AABB drag_rect_cached;
		public static AABB drag_rect_cached_world;
		public static readonly HashSet<Entity> hs_selected_entities = new();
		public static Entity? ent_hovered_unit_override;

		private static void DrawBottomWindow(bool is_loading, ref AABB rect)
		{
			//if (selected_region_id != 0 || h_selected_location != 0)
			{
				//var draw_external = true;

				const float button_h = 32;
				using (var window = GUI.Window.Standalone("worldmap.side.bottom"u8, position: rect.GetPosition(new Vector2(0.50f, 1.00f), new Vector2(0.00f, -8.00f)), size: new(64 * 8, 64 + 16 + button_h), pivot: new(0.50f, 1.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
				{
					if (window.show)
					{
						ref var region = ref World.GetGlobalRegion();
						var mouse = GUI.GetMouse();

						GUI.DrawBackground(GUI.tex_window_chat, rect: window.group.GetOuterRect().Pad(u: button_h - 4), padding: new(4), color: GUI.col_default);

						//var drag_rect = default(AABB);

						var drag_active = GUI.TryGetMouseDragRect(ref drag_rect_cached, out var drag_pressed, out var drag_released, enabled: WorldMap.IsHovered() && WorldMap.editor_mode == EditorMode.None);
						if (drag_active)
						{
							drag_rect_cached_world = region.CanvasToWorld(drag_rect_cached);

							hs_selected_entities.Clear();
							//var selected_entities_count = 0;

							foreach (ref var row in region.IterateQuery<WorldMap.Unit.GetAllQuery>())
							{
								row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Unit.Data unit, [Source.Owned] in Transform.Data transform, [Source.Owned, Keg.Engine.Game.Optional(false)] in Faction.Data faction) =>
								{
									if (drag_rect_cached_world.ContainsPoint(transform.position) && !entity.HasParent(Relation.Type.Child) && unit.CanPlayerControlUnit(entity, Client.GetPlayerHandle()))
									{
										//GUI.DrawCircle(region.WorldToCanvas(transform.position), 0.50f * region.GetWorldToCanvasScale(), color: Color32BGRA.Green.WithAlpha(200), layer: GUI.Layer.Foreground);
										hs_selected_entities.Add(entity);
									}
								});
							}
						}

						var drag_rect_color = Color32BGRA.Yellow;

						//if (drag_pressed) drag_rect_color = Color32BGRA.Green;
						//else if (drag_released) drag_rect_color = Color32BGRA.Red;

						if (drag_pressed)
						{
							drag_rect_color = Color32BGRA.Green;
							//App.WriteLine("begin drag");
						}

						if (drag_released)
						{
							drag_rect_color = Color32BGRA.Red;
							//App.WriteLine("end drag");
						}

						if (!drag_active) drag_rect_color.a = 100;

						var wpos_mouse_snapped = WorldMap.worldmap_mouse_position_snapped;
						var canvas_scale = region.GetWorldToCanvasScale();

						var random = XorRandom.New(true);

						var unit_index = 0;

						//// TODO: temporary shithack
						//if (!selected_entities.Contains(WorldMap.selected_entity)) selected_entities[^1] = WorldMap.selected_entity;

						var slot_h = GUI.RmY;
						var slot_w = slot_h - button_h;

						foreach (var ent_unit in hs_selected_entities)
						{
							if (ent_unit.IsAlive())
							{
								var ent_parent = ent_unit.GetParent(Relation.Type.Stored);
								var has_parent = ent_parent.IsAlive();

								ref var transform = ref ent_unit.GetComponent<Transform.Data>();
								if (transform.IsNotNull())
								{
									GUI.DrawCircle(region.WorldToCanvas(transform.position), 0.50f * region.GetWorldToCanvasScale(), color: Color32BGRA.Green.WithAlpha(200), layer: GUI.Layer.Foreground);

									using (GUI.ID.Push(ent_unit))
									using (var group = GUI.Group.New(size: new(slot_w, slot_h)))
									{
										ref var unit = ref ent_unit.GetComponent<Unit.Data>();

										using (var group_button = GUI.Group.New(size: new(GUI.RmX, button_h)))
										{
											if (unit.IsNotNull())
											{
												if (has_parent)
												{
													var can_exit = true;
													if (ent_parent.TryGetAsset(out ILocation.Definition location_asset) && location_asset.data.flags.HasAny(ILocation.Flags.Region))
													{
														if (GUI.DrawButton("Region"u8, size: GUI.Rm, color: GUI.col_button, enabled: true))
														{
															WorldMap.FocusLocation(location_asset);
															//var rpc = new Unit.ActionRPC();
															//rpc.action = Unit.Action.Exit;
															//rpc.ent_target = ent_parent;
															//rpc.pos_target = wpos_mouse_snapped + ((transform.position - wpos_mouse_snapped).GetNormalized(out var dist) * Maths.Min((unit_index++) * 0.30f, dist * 0.50f));
															//rpc.Send(ent_unit);
														}
													}
													else
													{
														if (GUI.DrawButton("Exit"u8, size: GUI.Rm, color: GUI.col_remove, enabled: can_exit))
														{
															var rpc = new Unit.ActionRPC();
															rpc.action = Unit.Action.Exit;
															rpc.ent_target = ent_parent;
															rpc.pos_target = wpos_mouse_snapped + ((transform.position - wpos_mouse_snapped).GetNormalized(out var dist) * Maths.Min((unit_index++) * 0.30f, dist * 0.50f));
															rpc.Send(ent_unit);
														}
													}
												}
												else
												{
													//if (GUI.DrawButton("Enter", size: GUI.Rm, color: GUI.col_add))
													//{

													//}
												}
											}
										}

										if (unit.IsNotNull())
										{
											if (!has_parent)
											{
												GUI.DrawLine(region.WorldToCanvas(transform.position), region.WorldToCanvas(unit.pos_next), Color32BGRA.Green.WithAlpha(80), thickness: 0.125f * canvas_scale * 0.25f, GUI.Layer.Foreground);

												GUI.DrawCircleFilled(region.WorldToCanvas(unit.pos_next), 0.1250f * canvas_scale * 0.50f, Color32BGRA.Green.WithAlpha(140), segments: 4, layer: GUI.Layer.Foreground);
												GUI.DrawCircleFilled(region.WorldToCanvas(unit.pos_target), 0.1250f * canvas_scale * 0.50f, Color32BGRA.Green.WithAlpha(140), segments: 4, layer: GUI.Layer.Foreground);

												if (WorldMap.IsHovered())
												{
													GUI.DrawLine(region.WorldToCanvas(transform.position), region.WorldToCanvas(wpos_mouse_snapped), Color32BGRA.Yellow.WithAlpha(80), thickness: 0.125f * canvas_scale * 0.25f, GUI.Layer.Foreground);
													GUI.DrawRectFilled(region.WorldToCanvas(AABB.Centered(wpos_mouse_snapped, new Vector2(0.125f * 0.50f))), Color32BGRA.Yellow.WithAlpha(140), GUI.Layer.Foreground);
												}
											}
										}

										var is_selected = WorldMap.interacted_entity == ent_unit;

										using (var group_icon = GUI.Group.New(size: new(GUI.RmX)))
										{
											group_icon.DrawBackground(GUI.tex_frame);

											ref var marker = ref ent_unit.GetComponent<Marker.Data>();
											if (marker.IsNotNull())
											{
												GUI.DrawSpriteCentered(marker.icon, group_icon.GetInnerRect(), layer: GUI.Layer.Window, scale: 3.00f);
											}

											if (GUI.Selectable3(ent_unit.GetShortID(), group_icon.GetOuterRect(), selected: is_selected))
											{
												//App.WriteLine("click");
												//WorldMap.FocusEntity(ent_unit);

												WorldMap.interacted_entity.Toggle(ent_unit, !is_selected);
												if (!is_selected) WorldMap.FocusEntity(ent_unit);
											}
										}

										if (WorldMap.IsHovered() || WorldMap.ent_hovered_unit_override.HasValue)
										{
											if (unit.IsNotNull() && !ent_parent.IsAsset<ILocation.Handle>())
											{
												//App.WriteLine("test");

												var rpc = new Unit.ActionRPC();
												rpc.action = Unit.Action.Move;

												var ent_hovered = WorldMap.ent_hovered_unit_override ?? WorldMap.hovered_entity.current;
												if (ent_hovered != ent_unit)
												{
													if (ent_hovered.IsAlive())
													{
														ref var enterable = ref ent_hovered.GetComponent<Enterable.Data>();
														if (enterable.IsNotNull() && enterable.mask_units.Has(unit.type))
														{
															if (has_parent && ent_hovered == ent_parent)
															{
																if (WorldMap.ent_hovered_unit_override.HasValue)
																{
																	// TODO
																}
																else
																{
																	GUI.SetCursor(App.CursorType.Remove, 200);
																	rpc.action = Unit.Action.Exit;
																	rpc.ent_target = ent_hovered;
																}
															}
															else
															{
																GUI.SetCursor(App.CursorType.Add, 200);
																rpc.action = Unit.Action.Enter;
																rpc.ent_target = ent_hovered;
															}
														}
													}
													else
													{
														if (has_parent && hs_selected_entities.Count == 1)
														{
															ref var enterable = ref ent_parent.GetComponent<Enterable.Data>();
															if (enterable.IsNotNull() && enterable.mask_units.Has(unit.type) && transform.position.IsInRadius(wpos_mouse_snapped, enterable.radius * 2))
															{
																GUI.SetCursor(App.CursorType.Remove, 200);
																rpc.action = Unit.Action.Exit;
																rpc.ent_target = ent_parent;
																rpc.pos_target = wpos_mouse_snapped;
															}
														}
													}
												}

												if (mouse.GetKeyDown(Mouse.Key.Right) && unit.CanPlayerControlUnit(ent_unit, Client.GetPlayerHandle()))
												{
													if (rpc.pos_target == Vector2.Zero) rpc.pos_target = wpos_mouse_snapped + ((transform.position - wpos_mouse_snapped).GetNormalized(out var dist) * Maths.Min((unit_index++) * 0.30f, dist * 0.50f));
													rpc.Send(ent_unit);
												}
											}
										}
									}

									GUI.SameLine();
								}
							}
						}

						//GUI.DrawRect(drag_rect_cached, color: drag_rect_color, layer: GUI.Layer.Foreground);
						if (drag_active)
						{
							GUI.DrawRect(region.WorldToCanvas(drag_rect_cached_world), color: drag_rect_color, layer: GUI.Layer.Foreground);
						}

						if (GUI.IsMouseDoubleClicked() && WorldMap.IsHovered())
						{
							drag_rect_cached_world = default;
							WorldMap.FocusEntity(default);

							hs_selected_entities.Clear();
						}

						WorldMap.ent_hovered_unit_override.Unset();
					}
				}
			}
		}

		//public partial struct LocationGUI: IGUICommand
		//{
		//	public Entity ent_location;
		//	public Location.Data location;

		//	public static int selected_tab;

		//	public void Draw()
		//	{
		//		using (var window = GUI.Window.Interaction("Location###location.gui"u8, this.ent_location))
		//		{
		//			this.StoreCurrentWindowTypeID(order: -150);
		//			if (window.show)
		//			{
		//				ref var player = ref Client.GetPlayer();
		//				ref var region = ref this.ent_location.GetRegionCommon();
		//				//ref var map_info = ref region.GetMapInfo();
		//				ref var location_data = ref this.location.h_location.GetData(out var s_location);

		//				using (GUI.Group.New(size: GUI.Rm, padding: new(0)))
		//				{
		//					if (location_data.IsNotNull())
		//					{
		//						using (GUI.Group.New(new(GUI.RmX, 40), new(0, 0)))
		//						{
		//							using (var group_header = GUI.Group.New(new(GUI.RmX, 40), new(8, 0)))
		//							{

		//							}
		//						}

		//						GUI.SeparatorThick(margin: new(4, 4));

		//						using (var group_main = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(4)))
		//						{
		//							group_main.DrawBackground(GUI.tex_panel);

		//							//var materials_filtered = IMaterial.Database.GetAssets().Where(x => x.data.commodity?.flags.HasAny(IMaterial.Commodity.Flags.Marketable) ?? false).ToArray();
		//							//var materials_filtered_span = materials_filtered.AsSpan();

		//							//Span<(float buy, float sell, float produce)> weights_span = stackalloc (float buy, float sell, float produce)[materials_filtered_span.Length];

		//							//using (var scrollbox = GUI.Scrollbox.New("scroll.economy"u8, size: GUI.Rm))
		//							//{
		//							//	// BUY
		//							//	using (var group_buy = GUI.Group.New(size: new Vector2(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
		//							//	{
		//							//		for (var i = 0; i < materials_filtered_span.Length; i++)
		//							//		{
		//							//			var material_asset = materials_filtered_span[i];
		//							//			weights_span[i].buy = Market.CalculateBuyScore(material_asset, ref location_data);
		//							//			weights_span[i].sell = Market.CalculateSellScore(material_asset, ref location_data);
		//							//			weights_span[i].produce = Market.CalculateProductionScore(material_asset, ref location_data);
		//							//		}
		//							//		weights_span.Sort(materials_filtered_span, (x, y) => y.produce.CompareTo(x.produce));
		//							//		//weights_span = weights_span.OrderByDescending(x => x.produce);

		//							//		for (var i = 0; i < materials_filtered_span.Length; i++)
		//							//		{
		//							//			using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
		//							//			{
		//							//				if (group_row.IsVisible())
		//							//				{
		//							//					group_row.DrawBackground(GUI.tex_panel);

		//							//					var material_asset = materials_filtered_span[i];
		//							//					ref var material_data = ref material_asset.GetData();

		//							//					GUI.DrawMaterialSmall(material_asset, new(GUI.RmY));

		//							//					GUI.SameLine(8);

		//							//					GUI.TitleCentered(material_data.name, pivot: new(0.00f, 0.50f));
		//							//					GUI.TextCentered($"{weights_span[i].buy:0.00}", pivot: new(1.00f, 0.50f), offset: new(-96, 0));
		//							//					GUI.DrawHoverTooltip("Buy"u8);

		//							//					GUI.TextCentered($"{weights_span[i].sell:0.00}", pivot: new(1.00f, 0.50f), offset: new(-48, 0));
		//							//					GUI.DrawHoverTooltip("Sell"u8);

		//							//					GUI.TextCentered($"{weights_span[i].produce:0.00}", pivot: new(1.00f, 0.50f), offset: new(0, 0));
		//							//					GUI.DrawHoverTooltip("Produce"u8);

		//							//					//App.WriteLine($"BUY [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
		//							//				}
		//							//			}

		//							//			GUI.NewLine(4);
		//							//		}
		//							//	}

		//							//	//GUI.SameLine();

		//							//	//using (var group_sell = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(4)))
		//							//	//{
		//							//	//	for (var i = 0; i < materials_filtered_span.Length; i++)
		//							//	//	{
		//							//	//		var material_asset = materials_filtered_span[i];
		//							//	//		weights_span[i] = Market.CalculateSellWeights(material_asset, ref location_data);
		//							//	//		//weights_span[i] = Market.CalculateProduceWeights(material_asset, ref location_data);
		//							//	//	}
		//							//	//	weights_span.Sort(materials_filtered_span);

		//							//	//	for (var i = materials_filtered_span.Length - 1; i >= 0; i--)
		//							//	//	{
		//							//	//		using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
		//							//	//		{
		//							//	//			if (group_row.IsVisible())
		//							//	//			{
		//							//	//				group_row.DrawBackground(GUI.tex_panel);

		//							//	//				var material_asset = materials_filtered_span[i];
		//							//	//				ref var material_data = ref material_asset.GetData();

		//							//	//				GUI.DrawMaterialSmall(material_asset, new(GUI.RmY));

		//							//	//				GUI.SameLine(8);

		//							//	//				GUI.TitleCentered(material_data.name, pivot: new(0.00f, 0.50f));
		//							//	//				GUI.TextCentered($"{weights_span[i]:0.00}", pivot: new(1.00f, 0.50f));
		//							//	//				//App.WriteLine($"BUY [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
		//							//	//			}
		//							//	//		}

		//							//	//		GUI.NewLine(4);
		//							//	//	}
		//							//	//}

		//							//}


		//						}
		//					}
		//				}
		//			}
		//		}
		//	}
		//}


		//[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnGUI(Entity entity,
		//[Source.Owned] in Location.Data location,
		//[Source.Owned] in Interactable.Data interactable)
		//{
		//	if (interactable.IsActive())
		//	{
		//		var gui = new LocationGUI()
		//		{
		//			ent_location = entity,
		//			location = location,
		//		};
		//		gui.Submit();
		//	}
		//}
#endif
	}
}

