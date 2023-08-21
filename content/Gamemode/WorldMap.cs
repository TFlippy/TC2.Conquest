
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
#if CLIENT
		public static Vector2 worldmap_offset;
		public static Vector2 worldmap_offset_target;
		public static Vector2 momentum;
		public static float worldmap_zoom = 6.00f;
		public static float worldmap_zoom_lerp = 6.00f;

		public static Matrix3x2 mat_proj;
		public static Matrix3x2 mat_view;

		public static float rotation;

		public static int? edit_points_index;
		public static int2[] edit_points;
		public static IAsset.IDefinition edit_asset;

		public static Texture.Handle h_texture_bg_00 = "ui_worldmap.bg.00";
		public static Texture.Handle h_texture_bg_01 = "ui_worldmap.bg.01";
		public static Texture.Handle h_texture_icons = "ui_worldmap.icons";
		public static Texture.Handle h_texture_line_00 = "ui_worldmap.line.00";
		public static Texture.Handle h_texture_line_01 = "ui_worldmap.line.01";
		public static Texture.Handle h_texture_line_02 = "ui_worldmap.line.02";

		public static Vector2 mouse_pos_old;
		public static Vector2 mouse_pos_new;

		public static ILocation.Handle h_selected_location;
		public static byte selected_region_id;

		public static bool dragging;
		public static bool enable_editor;

		//public static StringBuilder sb = new StringBuilder();

		public static void Draw(Vector2 size)
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();


			//if (!Client.IsLoadingRegion())
			{
				//GUI.RegionMenu.enabled = false;
				//GUI.Menu.enable_background = true;

				//if (!is_loading)
				{
					//var viewport_size = new Vector2(1200, 800);
					//var viewport_size = GUI.CanvasSize - new Vector2(64, 64);
					//var window_pos = new Vector2(GUI.CanvasSize.X * 0.50f, 48);
					//var window_pos = GUI.CanvasSize * 0.50f;
					//var pivot = new Vector2(0.50f, 0.00f);
					//var pivot = new Vector2(0.50f, 0.50f);
					//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
					//using (var window = GUI.Window.Standalone("region_menu", position: window_pos, size: viewport_size, pivot: pivot, padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
					//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.50f, size: viewport_size, pivot: new(0.50f, 0.50f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))


					using (var group_canvas = GUI.Group.New(size))
					{
						var rect = group_canvas.GetInnerRect();

						//if (window.show)
						//using (var button = GUI.CustomButton.New(id: "worldmap.button", size: GUI.Rm, set_cursor: false))
						//GUI.ButtonBehavior("worldmap.button", rect: rect, out var hovered, out _);

						using (GUI.ID.Push(1337))
						{
							GUI.DrawWindowBackground(GUI.tex_window_character);
							//sb.Clear();

							//var hovered = true;
							var scale_canvas = GUI.GetWorldToCanvasScale();
							//var hovered = button.hovered;

							var mouse = GUI.GetMouse();
							var kb = GUI.GetKeyboard();

							if (is_loading)
							{
								mouse = default;
								kb = default;
							}

							var zoom = MathF.Pow(2.00f, worldmap_zoom_lerp);
							var zoom_inv = 1.00f / zoom;

							//var mouse_pos = mouse.position;
							//var mouse_pos = GUI.GetMousePosition(); // mouse.GetInterpolatedPosition() * scale_canvas;
							//var mouse_delta = -(mouse.GetDelta() * scale_canvas);
							var rect_center = rect.GetPosition();

							//var is_dragging = false;

							//var hovered = GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false);
							//if (hovered)
							//{
							//	mouse_delta = mouse_pos_old - mouse_pos_new;

							//	if (mouse.GetKey(Mouse.Key.Left) || mouse.GetKeyDown(Mouse.Key.Left))
							//	{
							//		//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;
							//		is_dragging = true;
							//	}
							//	else if (mouse.GetKeyDown(Mouse.Key.Left))
							//	{
							//		momentum = mouse_delta;
							//	}
							//}

							//if (is_dragging && hovered)
							//{
							//	//worldmap_offset += mouse_delta * zoom_inv;
							//	momentum = Vector2.Lerp(momentum, mouse_delta, 0.50f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
							//	worldmap_offset_target += (mouse_delta * zoom_inv); // + (momentum * zoom_inv * 0.10f);
							//	worldmap_offset = Vector2.Lerp(worldmap_offset, worldmap_offset_target, 0.40f);

							//	//worldmap_offset += momentum * zoom_inv * 0.10f;
							//}
							//else
							//{
							//	momentum = Vector2.Lerp(momentum, Vector2.Zero, 0.10f);
							//	worldmap_offset_target += momentum * zoom_inv;
							//	worldmap_offset = Vector2.Lerp(worldmap_offset, worldmap_offset_target, 0.25f);
							//}



							if (mouse.GetKeyUp(Mouse.Key.Left))
							{
								//momentum = mouse_delta;
							}

							if (enable_editor)
							{
								GUI.DrawTextCentered("Edit Mode", rect.GetPosition(new(0.50f, 1.00f)) + new Vector2(0, 32), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground);
							}

							using (GUI.Clip.Push(rect))
							{
								//mat_proj = Matrix3x2.Identity;
								//mat_proj.M11 = 2.00f / size.X;
								//mat_proj.M22 = 2.00f / size.Y;
								//mat_view = Maths.TRS3x2(worldmap_offset, rotation, new Vector2(zoom));

								//var mat_vp = Matrix3x2.Multiply(mat_view, mat_proj);
								//Matrix3x2.Invert(mat_vp, out var mat_vp_inv);

								var mat_l2c = Maths.TRS3x2((worldmap_offset * -zoom) + rect.GetPosition(new(0.50f)), rotation, new Vector2(zoom));
								Matrix3x2.Invert(mat_l2c, out var mat_c2l);
								//mat_l2c.Translation += rect.GetPosition(new(0.50f));

								var uv_offset = worldmap_offset / scale_canvas;

								var tex_scale = 16.00f;
								var tex_scale_inv = 1.00f / tex_scale;

								var color_grid = new Color32BGRA(0xff4eabb5);
								GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv,
									clip: false,
									color: color_grid.WithAlphaMult(0.10f));

								//var mouse_pos = GUI.GetMousePosition();
								//var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
								var mouse_pos = GUI.GetMousePosition(); // Vector2.Transform(GUI.GetMousePosition(), mat_c2l);
								var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
								var mouse_local_snapped = mouse_local;
								mouse_local_snapped.Snap(1);

								var tex_line_district = h_texture_line_00;
								var tex_line_province = h_texture_line_01;

								static void DrawOutline(Matrix3x2 mat_l2c, float zoom, Span<int2> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
								{
									var count = points.Length;

									var last_vert = default(int2);
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

								foreach (var asset in IDistrict.Database.GetAssets())
								{
									if (asset.id == 0) continue;
									ref var asset_data = ref asset.GetData();

									var points = asset_data.points;
									if (points != null && points.Length > 0)
									{
										var pos_center = Vector2.Zero;

										Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
										for (var i = 0; i < points.Length; i++)
										{
											var point = (Vector2)points[i];
											pos_center += point;
											var point_t = Vector2.Transform(point, mat_l2c);

											points_t_span[i] = point_t;

											if (enable_editor)
											{
												if (rect.ContainsPoint(point_t) && ((Vector2.DistanceSquared(point, mouse_local) <= 0.75f.Pow2()) || (edit_asset == asset && edit_points_index == i)))
												{
													GUI.DrawCircleFilled(point_t, 0.25f * zoom, color: Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f), segments: 4, layer: GUI.Layer.Foreground);

													if (!edit_points_index.HasValue)
													{
														if (mouse.GetKeyDown(Mouse.Key.Right))
														{
															if (kb.GetKey(Keyboard.Key.LeftShift))
															{
																//d_district.points = points.Insert(i, (int2)(points[i] + points[(i + 1) % points.Length]) / 2);
																asset_data.points = points.Insert(i, points[i]);
															}
															else
															{
																edit_points_index = i;
																edit_points = points;
																edit_asset = asset;

																GUI.FocusAsset(asset.GetHandle());
															}
														}
														else if (kb.GetKeyDown(Keyboard.Key.Delete))
														{
															asset_data.points = points.Remove(i);
														}
													}
												}
											}
										}
										pos_center /= points.Length;
										pos_center += asset_data.offset;

										GUI.DrawPolygon(points_t_span, asset_data.color_fill with { a = 100 }, GUI.Layer.Window);

										//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
										DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, tex_line_district);

										//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 1.00f * zoom, color: GUI.font_color_title.WithAlphaMult(1.00f), layer: GUI.Layer.Window);
										GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 0.75f * zoom * asset_data.size, color: asset_data.color_fill.WithColorMult(0.32f).WithAlphaMult(0.30f), layer: GUI.Layer.Window);
									}
								}

								foreach (var asset in IProvince.Database.GetAssets())
								{
									if (asset.id == 0) continue;
									ref var asset_data = ref asset.GetData();

									var points = asset_data.points;
									if (points != null)
									{
										DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f, 0.25f, tex_line_province);

										var pos_center = Vector2.Zero;
										var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);

										Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
										for (var i = 0; i < points.Length; i++)
										{
											var point = (Vector2)points[i];
											pos_center += point;
											var point_t = Vector2.Transform(point, mat_l2c);

											points_t_span[i] = point_t;

											if (enable_editor)
											{
												if (rect.ContainsPoint(point_t) && ((Vector2.DistanceSquared(point, mouse_local) <= 0.25f.Pow2()) || (edit_asset == asset && edit_points_index == i)))
												{
													GUI.DrawCircleFilled(point_t, 0.375f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);

													if (!edit_points_index.HasValue)
													{
														if (mouse.GetKeyDown(Mouse.Key.Right))
														{
															if (kb.GetKey(Keyboard.Key.LeftShift))
															{
																//d_district.points = points.Insert(i, (int2)(points[i] + points[(i + 1) % points.Length]) / 2);
																asset_data.points = points.Insert(i, points[i]);
															}
															else
															{
																edit_points_index = i;
																edit_points = points;
																edit_asset = asset;

																GUI.FocusAsset(asset.GetHandle());
															}
														}
														else if (kb.GetKeyDown(Keyboard.Key.Delete))
														{
															asset_data.points = points.Remove(i);
														}
													}
												}
											}
										}
										pos_center /= points.Length;
									}
								}

								foreach (var asset in ILocation.Database.GetAssets())
								{
									if (asset.id == 0) continue;

									ref var asset_data = ref asset.GetData();

									var scale = 0.500f;
									var asset_scale = Maths.Clamp(asset_data.size, 0.50f, 1.00f);
									var rect_location = AABB.Centered(Vector2.Transform((Vector2)asset_data.point, mat_l2c), new Vector2(scale * zoom * 0.50f));

									GUI.DrawSpriteCentered(asset_data.icon, rect_location, layer: GUI.Layer.Window, 0.125f * MathF.Max(scale * zoom * asset_scale, 16), color: asset_data.color);
									GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.625f * asset_scale), mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);
								}

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

													//map_info.

													var rect_map = AABB.Centered(Vector2.Transform((Vector2)map_info.point, mat_l2c), new Vector2(icon_size * zoom * 0.50f));
													var rect_map_lg = AABB.Centered(Vector2.Transform(((Vector2)map_info.point) + new Vector2(0.00f, -0.875f), mat_l2c), new Vector2(icon_size * zoom * 1.50f));

													var is_pressed = GUI.ButtonBehavior(map_info.name, rect_map_lg, out var is_hovered, out var is_held);
													//var is_hovered = GUI.IsHoveringRect(rect_map_lg);
													var is_selected = selected_region_id == i;

													var scale = 1.00f;
													//if (is_selected) scale *= 1.125f;
													//GUI.DrawRect(rect_map_lg, Color32BGRA.Yellow, layer: GUI.Layer.Foreground);


													var map_asset = mod_context.GetMap(region_info.map);
													if (map_asset != null)
													{
														var tex_thumbnail = map_asset.GetThumbnail();
														if (tex_thumbnail != null)
														{
															GUI.DrawTexture(tex_thumbnail.Identifier, rect_map_lg, GUI.Layer.Window);
														}

														//GUI.DrawBackground(is_hovered ? GUI.tex_frame_white : GUI.tex_frame, rect_map_lg, padding: new(4 * zoom_inv));
														GUI.DrawBackground((is_hovered || is_selected) ? GUI.tex_frame_white : GUI.tex_frame, rect_map_lg, padding: new(4));
														//GUI.DrawTexture(is_hovered ? GUI.tex_frame_white : GUI.tex_frame, rect_map_lg, layer: GUI.Layer.Window);

														//if (tex_thumbnail != null && GUI.IsItemHovered())
														//{
														//	using (GUI.Tooltip.New())
														//	{
														//		using (var group_preview = GUI.Group.New(size: tex_thumbnail.size))
														//		{
														//			GUI.DrawTexture(tex_thumbnail.handle, tex_thumbnail.size);
														//			GUI.DrawBackground(GUI.tex_frame, group_preview.GetInnerRect(), new(8));
														//		}
														//	}
														//}
													}

													if (is_hovered)
													{
														GUI.SetCursor(App.CursorType.Hand, 1000);

														if (is_pressed)
														{
															if (is_selected)
															{
																selected_region_id = 0;
																Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
															}
															else
															{
																selected_region_id = (byte)i;
																Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
															}
															// Client.RequestSetActiveRegion((byte)i);
														}
													}

													GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 24, 24, 3, 0), rect_map, layer: GUI.Layer.Window, color: (is_selected) ? GUI.col_white : GUI.col_button, scale: zoom * 0.0625f * scale);
													//GUI.DrawRectFilled(rect_map, color, layer: GUI.Layer.Window);
													GUI.DrawTextCentered(map_info.name, Vector2.Transform(((Vector2)map_info.point) + new Vector2(0.00f, -0.25f - 0.10f), mat_l2c), pivot: new Vector2(0.50f, 0.50f), color: color, font: GUI.Font.Superstar, size: 0.37f * MathF.Max(icon_size * zoom * scale, 32), layer: GUI.Layer.Window, box_shadow: true);
												}
											}
										}
									}
								}

								GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Window, color: Color32BGRA.White.WithAlphaMult(0.30f));

								if (edit_points_index.TryGetValue(out var v_edit_points_index))
								{
									edit_points[v_edit_points_index] = new int2((int)MathF.Round(mouse_local.X), (int)MathF.Round(mouse_local.Y));

									if (mouse.GetKeyUp(Mouse.Key.Right))
									{
										edit_asset.Save();

										edit_points_index = null;
										edit_points = null;
										edit_asset = null;
									}
								}

								GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 72, 72, 0, 1), rect: AABB.Centered(Vector2.Transform(mouse_local_snapped, mat_l2c), new Vector2(0.25f * zoom * 0.50f)), layer: GUI.Layer.Window, scale: 0.125f * 0.250f * zoom, color: Color32BGRA.Black.WithAlphaMult(0.20f));

								GUI.DrawTextCentered($"Zoom: {worldmap_zoom:0.00}x\n[{mouse_local_snapped.X:0}, {mouse_local_snapped.Y:0}]\n[{worldmap_offset_target.X:0.00}, {worldmap_offset_target.Y:0.00}]\n[{mouse_local.X:0.00}, {mouse_local.Y:0.00}]\n[{mouse_pos.X:0.00}, {mouse_pos.Y:0.00}]", position: rect.GetPosition(new(1, 1)), new(1, 1), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground);


								var mouse_delta = Vector2.Zero;
								mouse_pos_old = mouse_pos_new;
								mouse_pos_new = mouse_pos;

								var hovered = GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false);
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

									mouse_delta = mouse_pos_old - mouse_pos_new;

									//else if (mouse.GetKeyDown(Mouse.Key.Left))
									//{
									//	momentum = mouse_delta;
									//}
								}

								if (mouse.GetKeyUp(Mouse.Key.Left))
								{
									dragging = false;
								}


								if (dragging && hovered)
								{
									//worldmap_offset += mouse_delta * zoom_inv;
									//momentum = mouse_delta; // Vector2.Lerp(momentum, mouse_delta, 0.80f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
									momentum = Vector2.Lerp(momentum, mouse_delta, 0.50f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
									worldmap_offset_target += (mouse_delta * zoom_inv); // + (momentum * zoom_inv * 0.10f);
									worldmap_offset = Vector2.Lerp(worldmap_offset, worldmap_offset_target, 0.25f);

									//worldmap_offset += momentum * zoom_inv * 0.10f;
								}
								else
								{
									momentum = Vector2.Lerp(momentum, Vector2.Zero, 0.10f);
									worldmap_offset_target += momentum * zoom_inv;
									worldmap_offset = Vector2.Lerp(worldmap_offset, worldmap_offset_target, 0.25f);
								}

								if (hovered)
								{
									worldmap_zoom -= mouse.GetScroll(0.25f);
									//worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 8.00f);
									worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 6.50f);
								}

								worldmap_zoom_lerp = Maths.Lerp(worldmap_zoom_lerp, worldmap_zoom, 0.20f);

								if (hovered)
								{
									if (kb.GetKeyDown(Keyboard.Key.Reload))
									{
										worldmap_offset_target = default;
										rotation = default;
									}

									if (kb.GetKeyDown(Keyboard.Key.Tab))
									{
										enable_editor = !enable_editor;
									}

									//if (kb.GetKey(Keyboard.Key.Q))
									//{
									//	rotation -= MathF.PI * 0.025f;
									//}

									//if (kb.GetKey(Keyboard.Key.E))
									//{
									//	rotation += MathF.PI * 0.025f;
									//}

									//mouse_delta = mouse_delta.RotateByRad(0);

									//if (mouse.GetKey(Mouse.Key.Left))
									//{
									//	//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;
									//	worldmap_offset += mouse_delta * zoom_inv;
									//}
								}

								var move_speed = (1.00f / MathF.Sqrt(worldmap_zoom_lerp)) * 10.00f;

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

								if (dragging && GUI.IsHovered)
								{
									GUI.SetHoveredID(1337);
								}
							}
						}

						using (var window = GUI.Window.Standalone("region_menu.list", position: new Vector2(rect.a.X, rect.a.Y) + new Vector2(6, 12), size: new(200, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
						{
							if (window.show)
							{
								window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

								var mod_context = App.GetModContext();

								ref var world_info = ref Client.GetWorldInfo();
								if (world_info.IsNotNull())
								{

								}

								using (GUI.Group.New(size: GUI.Rm))
								{
									using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
									{
										GUI.TitleCentered("Regions", size: 32, pivot: new(0.00f, 0.50f));
									}

									GUI.SeparatorThick();

									using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
									{
										if (world_info.IsNotNull())
										{
											var region_list_span = World.GetRegionList().AsSpan();
											for (var i = 1; i < region_list_span.Length; i++)
											{
												ref var region_info = ref region_list_span[i];
												ref var map_info = ref region_info.map_info.GetRefOrNull();

												if (region_info.IsValid() && map_info.IsNotNull())
												{

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 4)))
													{
														group_row.DrawBackground(GUI.tex_panel);

														var selected = selected_region_id == i;
														if (GUI.Selectable3((uint)i, group_row.GetOuterRect(), selected))
														{
															if (selected)
															{
																selected_region_id = 0;
															}
															else
															{
																selected_region_id = (byte)i;
																worldmap_offset_target = (Vector2)map_info.point;
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

						if (selected_region_id != 0)
						{
							//GUI.SetFocusNext();

							//using (var window = GUI.Window.Standalone("region_menu.region", position: new Vector2(GUI.CanvasSize.X, 0) + new Vector2(-48, 48), size: new(300, 550), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true))
							using (var window = GUI.Window.Standalone("region_menu.region", position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(300, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
							{
								if (window.show)
								{
									window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

									ref var region_info = ref World.GetRegionInfo(selected_region_id);
									if (region_info.IsNotNull() && region_info.IsValid())
									{
										ref var map_info = ref region_info.map_info.GetRefOrNull();
										var map_asset = App.GetModContext().GetMap(region_info.map);

										using (GUI.Group.New(size: GUI.Rm))
										{
											using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
											{
												GUI.TitleCentered(map_info.name, size: 32, pivot: new(0.00f, 0.50f));
											}

											GUI.SeparatorThick();

											using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
											{
												using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmX * 0.50f)))
												{
													var tex_thumbnail = map_asset.GetThumbnail();
													if (tex_thumbnail != null)
													{
														GUI.DrawTexture(tex_thumbnail.Identifier, group_thumbnail.GetInnerRect(), GUI.Layer.Window);
													}
													GUI.DrawBackground(GUI.tex_frame, group_thumbnail.GetOuterRect(), padding: new(4));
												}

												GUI.SameLine();

												using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8, 8)))
												{
													using (GUI.Wrap.Push(GUI.RmX))
													{
														GUI.TextShaded(map_asset.Description);
													}
												}
											}

											GUI.SeparatorThick();

											using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
											{
												GUI.TextShaded("- some info here");
											}

											if (true)
											{
												var color = GUI.col_button_ok;
												var alpha = 1.00f;

												if (GUI.DrawButton("Enter", size: new(GUI.RmX * 0.50f, 40), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
												{
													Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

													//Client.TODO_LoadRegion(region_id);
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

