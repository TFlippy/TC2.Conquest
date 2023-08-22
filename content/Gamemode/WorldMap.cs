
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
#if CLIENT
		public static Vector2 worldmap_offset_current_snapped;
		public static Vector2 worldmap_offset_current;
		public static Vector2 worldmap_offset_target;
		public static Vector2 momentum;

		public static float worldmap_zoom_current = 6.00f;
		public static float worldmap_zoom_target = 6.00f;

		public static Vector2 worldmap_window_size = new Vector2(1024, 1024);
		public static Vector2 worldmap_window_offset = new Vector2(0, 0);


		//public static Matrix3x2 mat_proj;
		//public static Matrix3x2 mat_view;

		public static float rotation;

		public static int? edit_points_index;
		public static int2[] edit_points;

		public static IAsset.IDefinition edit_asset;
		public static IWorld.Handle h_world = "krumpel";
		public static ILocation.Handle h_selected_location;

		public static Texture.Handle h_texture_bg_00 = "ui_worldmap.bg.00";
		public static Texture.Handle h_texture_bg_01 = "ui_worldmap.bg.01";
		public static Texture.Handle h_texture_icons = "ui_worldmap.icons";
		public static Texture.Handle h_texture_line_00 = "ui_worldmap.line.00";
		public static Texture.Handle h_texture_line_01 = "ui_worldmap.line.01";
		public static Texture.Handle h_texture_line_02 = "ui_worldmap.line.02";

		public static Texture.Handle h_texture_terrain_beach_00 = "worldmap.terrain.beach.00";

		public static Vector2 mouse_pos_old;
		public static Vector2 mouse_pos_new;

		public static EditorMode editor_mode;

		public static byte selected_region_id;

		public static bool dragging;
		//public static bool enable_editor;
		public static bool use_renderer;

		public enum EditorMode: uint
		{
			None = 0,

			Province,
			District,
			Location,
			Doodad,

			Max
		}

		public static void DrawOutlineShader(Matrix3x2 mat_l2c, float zoom, Span<int2> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
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

					IWorld.WorldMap.Renderer.Add(new()
					{
						a = a,
						b = b,
						color = Color32BGRA.White,
						h_texture = h_texture,
						thickness = thickness,
						uv_scale = Vector2.Distance(a, b) * 0.5f
					});

					//GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void DrawOutline(Matrix3x2 mat_l2c, float zoom, Span<int2> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
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

		//public static StringBuilder sb = new StringBuilder();

		public static void Draw(Vector2 size)
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();
			//var use_renderer = true;

			//if (!Client.IsLoadingRegion())

			//if (!is_loading)

			if (use_renderer)
			{
				IWorld.WorldMap.Renderer.Clear();
				IWorld.Doodad.Renderer.Clear();
			}

			using (var group_canvas = GUI.Group.New(size))
			{
				var rect = group_canvas.GetInnerRect();

				var enable_momentum = true;
				var enable_snapping = true;
				var snap_camera = true;

				//if (window.show)
				//using (var button = GUI.CustomButton.New(id: "worldmap.button", size: GUI.Rm, set_cursor: false))
				//GUI.ButtonBehavior("worldmap.button", rect: rect, out var hovered, out _);

				using (GUI.ID.Push("worldmap"))
				{
					GUI.DrawWindowBackground(GUI.tex_window_character);
					//sb.Clear();

					//var hovered = true;
					//var scale_canvas = GUI.GetWorldToCanvasScale();
					//var hovered = button.hovered;

					var mouse = GUI.GetMouse();
					var kb = GUI.GetKeyboard();

					if (is_loading)
					{
						mouse = default;
						kb = default;
					}

					var zoom = MathF.Pow(2.00f, worldmap_zoom_current);
					var zoom_inv = 1.00f / zoom;

					var rect_center = rect.GetPosition();

					if (editor_mode != EditorMode.None)
					{
						GUI.DrawTextCentered($"Editor: {editor_mode}", rect.GetPosition(new(0.50f, 1.00f)) - new Vector2(0, 16), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
					}

					using (GUI.Clip.Push(rect))
					{
						var scale_b = IWorld.WorldMap.scale_b;
						var snap_delta = (worldmap_offset_current_snapped - worldmap_offset_current) * 1.0f;
						//snap_delta = default;

						//mat_proj = Matrix3x2.Identity;
						//mat_proj.M11 = 2.00f / size.X;
						//mat_proj.M22 = 2.00f / size.Y;
						//mat_view = Maths.TRS3x2(worldmap_offset, rotation, new Vector2(zoom));

						//var mat_vp = Matrix3x2.Multiply(mat_view, mat_proj);
						//Matrix3x2.Invert(mat_vp, out var mat_vp_inv);

						var mat_l2c = Maths.TRS3x2((worldmap_offset_current * -zoom) + rect.GetPosition(new(0.50f)) - snap_delta, rotation, new Vector2(zoom));
						Matrix3x2.Invert(mat_l2c, out var mat_c2l);

						var mat_l2c2 = Maths.TRS3x2(rect.GetPosition(new Vector2(0.50f)), rotation, new Vector2(1));
						Matrix3x2.Invert(mat_l2c2, out var mat_c2l2);

						//var snap_delta_canvas = Vector2.Transform(snap_delta, mat_l2c);
						var snap_delta_canvas = snap_delta * zoom;

						//mat_l2c.Translation += rect.GetPosition(new(0.50f));

						//var uv_offset = worldmap_offset_current_snapped / scale_canvas;

						var tex_scale = use_renderer ? (IWorld.WorldMap.worldmap_size.X / scale_b) * zoom : 16.00f;
						var tex_scale_inv = 1.00f / tex_scale;

						var color_grid = new Color32BGRA(0xff4eabb5);
						//GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), clip: false);

						if (use_renderer)
						{
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), clip: false);
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);
							GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);
						}
						else
						{
							GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						}

						//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false);

						//var mouse_pos = GUI.GetMousePosition();
						//var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
						var mouse_pos = GUI.GetMousePosition(); // Vector2.Transform(GUI.GetMousePosition(), mat_c2l);
						var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
						var mouse_local_snapped = mouse_local;
						mouse_local_snapped.Snap(1.00f / scale_b, out mouse_local_snapped);

						var tex_line_district = h_texture_line_00;
						var tex_line_province = h_texture_line_01;

						#region Districts
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

									if (editor_mode == EditorMode.District)
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
														asset.Save();
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
													asset.Save();
												}
											}
										}
									}
								}
								pos_center /= points.Length;
								pos_center += asset_data.offset;

								GUI.DrawPolygon(points_t_span, asset_data.color_fill with { a = 100 }, GUI.Layer.Window);

								//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
								if (use_renderer)
								{
									DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, tex_line_district);
								}
								else
								{
									DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, tex_line_district);
								}

								//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 1.00f * zoom, color: GUI.font_color_title.WithAlphaMult(1.00f), layer: GUI.Layer.Window);
								GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 0.75f * zoom * asset_data.size, color: asset_data.color_fill.WithColorMult(0.32f).WithAlphaMult(0.30f), layer: GUI.Layer.Window);
							}
						}
						#endregion

						#region Provinces
						foreach (var asset in IProvince.Database.GetAssets())
						{
							if (asset.id == 0) continue;
							ref var asset_data = ref asset.GetData();

							var points = asset_data.points;
							if (points != null)
							{
								if (use_renderer)
								{
									DrawOutlineShader(mat_l2c, zoom, points, asset_data.color_border, 1.00f, 0.25f, h_texture_terrain_beach_00);
								}
								else
								{
									DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f, 0.25f, tex_line_province);
								}

								var pos_center = Vector2.Zero;
								var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);

								Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
								for (var i = 0; i < points.Length; i++)
								{
									var point = (Vector2)points[i];
									pos_center += point;
									var point_t = Vector2.Transform(point, mat_l2c);

									points_t_span[i] = point_t;

									if (editor_mode == EditorMode.Province)
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
														asset.Save();
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
													asset.Save();
												}
											}
										}
									}
								}
								pos_center /= points.Length;
							}
						}
						#endregion

						#region Locations
						foreach (var asset in ILocation.Database.GetAssets())
						{
							if (asset.id == 0) continue;

							ref var asset_data = ref asset.GetData();

							var scale = 0.500f;
							var asset_scale = Maths.Clamp(asset_data.size, 0.50f, 1.00f);
							var rect_location = AABB.Centered(Vector2.Transform((Vector2)asset_data.point, mat_l2c), new Vector2(MathF.Max(scale * zoom * asset_scale * 1.50f, 16)));

							//GUI.DrawRect(rect_location, color, layer: GUI.Layer.Foreground);

							var is_selected = h_selected_location == asset;
							var is_pressed = GUI.ButtonBehavior(asset_data.name, rect_location, out var is_hovered, out var is_held);

							var color = (is_selected || is_hovered) ? Color32BGRA.White : asset_data.color;

							if (is_hovered)
							{
								GUI.SetCursor(App.CursorType.Hand, 1000);

								if (is_pressed)
								{
									selected_region_id = 0;

									if (is_selected)
									{
										h_selected_location = default;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
									}
									else
									{
										h_selected_location = asset;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
									}
									// Client.RequestSetActiveRegion((byte)i);
								}
							}

							GUI.DrawSpriteCentered(asset_data.icon, rect_location, layer: GUI.Layer.Window, 0.125f * MathF.Max(scale * zoom * asset_scale, 16), color: color);
							GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.625f * asset_scale), mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);
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
						#endregion

						#region Overlays
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Window, color: Color32BGRA.White.WithAlphaMult(0.30f));
						GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 72, 72, 0, 1), rect: AABB.Centered(Vector2.Transform(mouse_local_snapped, mat_l2c), new Vector2(0.25f)), layer: GUI.Layer.Window, scale: 0.125f * 0.50f * zoom, color: Color32BGRA.Black.WithAlphaMult(1));
						GUI.DrawTextCentered($"Zoom: {zoom:0.00}x\ndelta: [{snap_delta.X:0.000000}, {snap_delta.Y:0.000000}]\ndelta.c: [{snap_delta_canvas.X:0.000000}, {snap_delta_canvas.Y:0.000000}]\ncam: [{worldmap_offset_target.X:0.000000}, {worldmap_offset_target.Y:0.000000}]\ncam.s: [{worldmap_offset_current_snapped.X:0.000000}, {worldmap_offset_current_snapped.Y:0.000000}]\nmouse.l: [{mouse_local.X:0.0000}, {mouse_local.Y:0.0000}]\nmouse: [{mouse_pos.X:0.00}, {mouse_pos.Y:0.00}]", position: rect.GetPosition(new(1, 1)), new(1, 1), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground);
						#endregion

						#region Editor
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
						#endregion

						#region Camera
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
						}

						mouse_delta = mouse_pos_old - mouse_pos_new;

						if (mouse.GetKeyUp(Mouse.Key.Left))
						{
							dragging = false;
						}

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

						if (hovered)
						{
							worldmap_zoom_target -= mouse.GetScroll(0.25f);
							//worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 8.00f);
							worldmap_zoom_target = Maths.Clamp(worldmap_zoom_target, 1.00f, 7.00f);
						}

						worldmap_zoom_current = Maths.Lerp(worldmap_zoom_current, worldmap_zoom_target, 0.20f);

						if (use_renderer)
						{
							worldmap_offset_current_snapped = Maths.SnapFloor(worldmap_offset_current, 1 / scale_b);
							IWorld.WorldMap.Renderer.UpdateCamera(worldmap_offset_current_snapped, 0.00f, new Vector2(1));

							ref var world_data = ref h_world.GetData();
							if (world_data.IsNotNull())
							{
								if (world_data.doodads != null)
								{
									IWorld.Doodad.Renderer.Add(world_data.doodads.AsSpan());
								}
							}

							IWorld.WorldMap.Renderer.Submit();
							IWorld.Doodad.Renderer.Submit();
						}

						if (hovered)
						{
							if (kb.GetKeyDown(Keyboard.Key.Reload))
							{
								worldmap_offset_current = default;
								worldmap_offset_current_snapped = default;
								worldmap_offset_target = default;
								momentum = default;
								rotation = default;
							}

							if (kb.GetKeyDown(Keyboard.Key.Tab))
							{
								editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) + 1, 0, (int)EditorMode.Max);
								//enable_editor = !enable_editor;
							}
						}

						var move_speed = (1.00f / MathF.Sqrt(worldmap_zoom_current)) * 10.00f;

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
						#endregion

						if (dragging && GUI.IsHovered)
						{
							GUI.SetHoveredID("worldmap");
						}
					}
				}

				#region Left
				using (var window = GUI.Window.Standalone("worldmap.side.left", position: new Vector2(rect.a.X, rect.a.Y) + new Vector2(6, 12), size: new(244, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
				{
					if (window.show)
					{
						window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

						var mod_context = App.GetModContext();

						ref var world_info = ref Client.GetWorldInfo();

						using (GUI.Group.New(size: GUI.Rm))
						{
							GUI.Checkbox("DEV: Renderer", ref use_renderer, new(GUI.RmX, 32));
							GUI.SliderFloat("DEV: Scale A", ref IWorld.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32));
							GUI.SliderFloat("DEV: Scale B", ref IWorld.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32));

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

											if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
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

															GUI.TitleCentered(map_info.name, size: 18, pivot: new(0.00f, 0.50f));

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
				#endregion

				#region Right
				if (selected_region_id != 0 || h_selected_location != 0)
				{
					using (var window = GUI.Window.Standalone("worldmap.side.right", position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(300, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
					{
						if (window.show)
						{
							window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

							if (selected_region_id != 0)
							{
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
							else if (h_selected_location != 0)
							{
								ref var location_data = ref h_selected_location.GetData();
								if (location_data.IsNotNull())
								{

									using (GUI.Group.New(size: GUI.Rm))
									{
										using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
										{
											GUI.TitleCentered(location_data.name, size: 32, pivot: new(0.00f, 0.50f));
										}

										GUI.SeparatorThick();

										using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
										{
											//using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmX * 0.50f)))
											//{
											//	var tex_thumbnail = map_asset.GetThumbnail();
											//	if (tex_thumbnail != null)
											//	{
											//		GUI.DrawTexture(tex_thumbnail.Identifier, group_thumbnail.GetInnerRect(), GUI.Layer.Window);
											//	}
											//	GUI.DrawBackground(GUI.tex_frame, group_thumbnail.GetOuterRect(), padding: new(4));
											//}

											//GUI.SameLine();

											using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8, 8)))
											{
												using (GUI.Wrap.Push(GUI.RmX))
												{
													GUI.TextShaded(location_data.desc);
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

										}
									}
								}
							}
						}
					}
				}
				#endregion
			}

			#region Debug
			{
				if (App.debug_mode_gui)
				{
					using (var window = GUI.Window.Standalone("worldmap.debug", position: GUI.CanvasSize * 0.50f, size: new(244, 400), pivot: new(0.50f, 0.50f), padding: new(8), force_position: false))
					{
						if (window.show)
						{
							GUI.DrawWindowBackground();

							GUI.Title("Worldmap Debug");

							GUI.SeparatorThick();

							GUI.SliderFloat("DEV: Size.X", ref worldmap_window_size.X, 1.00f, 1920.00f, new(GUI.RmX * 0.50f, 32), snap: 8);
							GUI.SameLine();
							GUI.SliderFloat("DEV: Size.Y", ref worldmap_window_size.Y, 1.00f, 1920.00f, new(GUI.RmX, 32), snap: 8);

							GUI.SliderFloat("DEV: Offset.X", ref worldmap_window_offset.X, -512.00f, 512.00f, new(GUI.RmX * 0.50f, 32), snap: 1);
							GUI.SameLine();
							GUI.SliderFloat("DEV: Offset.Y", ref worldmap_window_offset.Y, -512.00f, 512.00f, new(GUI.RmX, 32), snap: 1);
						}
					}
				}
			}
			#endregion

		}
#endif
	}
}

