﻿
using System.Text;

namespace TC2.Conquest
{
	public sealed partial class ModInstance: Mod
	{
		protected override void OnRegister(ModContext context)
		{

		}

		protected override void OnInitialize(ModContext context)
		{

		}

#if SERVER
		protected override void OnPreprocessMap(Bitmap bitmap, ref IMap.Info map_info)
		{
			//if (!Constants.World.edit_mode)
			//{
			//	var random = XorRandom.New(true);

			//	var pixels_span = bitmap.GetPixels();
			//	var w = bitmap.width;
			//	var h = bitmap.height;

			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "stone", 0.80f, h_offset: -0.50f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 2.00f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "stone", 0.50f, h_offset: 0.00f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 1.50f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "silver.ore", 0.50f, h_offset: 0.10f, scale_0: 2.00f, scale_1: 0.50f, scale_2: 1.50f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("silver.ore"), "gold.ore", 1.20f, h_offset: -0.70f, scale_0: 1.50f, scale_1: 2.00f, scale_2: 1.75f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "iron.ore", 0.60f, h_offset: 0.10f, scale_0: 1.20f, scale_1: 0.50f, scale_2: 3.50f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("iron.ore"), "pjerdelite", 0.50f, h_offset: -0.20f, scale_0: 0.80f, scale_1: 0.50f, scale_2: 6.50f);
			//	Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "smirglum.ore", 1.00f, h_offset: -0.80f, scale_0: 0.50f, scale_1: 0.50f, scale_2: 4.50f);
			//}
		}
#endif

#if CLIENT
		public static Vector2 worldmap_offset;
		public static float worldmap_zoom = 1.00f;
		public static float worldmap_zoom_lerp = 1.00f;

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

		public static StringBuilder sb = new StringBuilder();

		protected override void OnDrawRegionMenu()
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			return;

			GUI.RegionMenu.enabled = false;
			GUI.Menu.enable_background = false;

			var viewport_size = new Vector2(1200, 800);
			var window_pos = new Vector2(GUI.CanvasSize.X * 0.50f, GUI.CanvasSize.Y * 0.50f);
			var pivot = new Vector2(0.50f, 0.50f);

			//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
			using (var window = GUI.Window.Standalone("region_menu", position: window_pos, size: viewport_size, pivot: pivot, padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
			//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.50f, size: viewport_size, pivot: new(0.50f, 0.50f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
			{
				if (window.show)
				{
					GUI.DrawWindowBackground(GUI.tex_frame);
					sb.Clear();


					var aspect = GUI.CanvasSize.GetNormalized();
					var scale_canvas = GUI.GetWorldToCanvasScale();
					var hovered = GUI.IsHoveringRect(window.group.GetOuterRect());

					var mouse = GUI.GetMouse();
					var kb = GUI.GetKeyboard();

					if (hovered)
					{
						worldmap_zoom -= mouse.GetScroll(0.25f);
						worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 8.00f);
					}

					worldmap_zoom_lerp = Maths.Lerp(worldmap_zoom_lerp, worldmap_zoom, 0.20f);

					var zoom = MathF.Pow(2.00f, worldmap_zoom_lerp);
					var zoom_inv = 1.00f / zoom;

					//var mouse_pos = GUI.WorldToCanvas(mouse.GetInterpolatedPosition());  // * scale_canvas;
					var mouse_pos = mouse.GetInterpolatedPosition() * scale_canvas;
					var mouse_delta = (mouse.GetDelta() * scale_canvas);


					//mouse_delta *= zoom;

					if (hovered)
					{
						if (kb.GetKeyDown(Keyboard.Key.Reload))
						{
							worldmap_offset = default;
							rotation = default;
						}

						if (kb.GetKeyDown(Keyboard.Key.Tab))
						{
						}

						if (kb.GetKey(Keyboard.Key.Q))
						{
							rotation -= MathF.PI * 0.025f;
						}

						if (kb.GetKey(Keyboard.Key.E))
						{
							rotation += MathF.PI * 0.025f;
						}

						//mouse_delta = mouse_delta.RotateByRad(0);

						if (mouse.GetKey(Mouse.Key.Left))
						{
							//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;
							worldmap_offset += mouse_delta * zoom_inv;
						}
					}

					var move_speed = zoom_inv * 20;

					if (kb.GetKey(Keyboard.Key.MoveLeft))
					{
						worldmap_offset.X += move_speed;
					}

					if (kb.GetKey(Keyboard.Key.MoveRight))
					{
						worldmap_offset.X -= move_speed;
					}

					if (kb.GetKey(Keyboard.Key.MoveUp))
					{
						worldmap_offset.Y += move_speed;
					}

					if (kb.GetKey(Keyboard.Key.MoveDown))
					{
						worldmap_offset.Y -= move_speed;
					}

					//var rotation = MathF.PI * 0.00f;
					//var mat = Maths.TRS3x2(worldmap_offset, rotation, new Vector2(zoom_inv));
					//Matrix3x2.Invert(mat, out mat);

					//var mat2 = Maths.TRS3x2(default, 0, new Vector2(1.00f));
					//mat.Translation += window.group.a + (window.group.size * 0.50f);
					//mat2.Translation += window.group.a + (window.group.size * 0.50f);


					mat_proj = Matrix3x2.Identity;
					mat_proj.M11 = 2.00f / viewport_size.X;
					mat_proj.M22 = 2.00f / viewport_size.Y;

					mat_view = Maths.TRS3x2(worldmap_offset, rotation, new Vector2(zoom));

					var mat_vp = Matrix3x2.Multiply(mat_view, mat_proj);
					Matrix3x2.Invert(mat_vp, out mat_vp);

					var mat_l2c = Maths.TRS3x2(worldmap_offset * zoom, rotation, new Vector2(zoom));
					//var mat_l2c = Matrix3x2.Multiply(mat_view, mat_proj);
					//mat_l2c.Translation += window.group.a + (window.group.size * 0.50f);
					Matrix3x2.Invert(mat_l2c, out var mat_c2l);
					mat_l2c.Translation += new Vector2(window.group.a.X, window.group.a.Y) + (window.group.size * 0.50f);
					//mat_c2l.Translation -= new Vector2(window.group.a.X, window.group.a.Y) + (window.group.size * 0.50f);

					//mat_c2l.Translation += (GUI.CanvasSize * 0.50f) - (window_pos + (viewport_size * pivot));
					//mat_c2l.Translation = (((GUI.CanvasSize * 0.50f) - (window_pos + (viewport_size * pivot))) / scale_canvas);

					//GUI.DrawCircleFilled(Vector2.Transform(new Vector2(0, 0), mat_l2c), 0.125f * zoom, Color32BGRA.Magenta, 8, layer: GUI.Layer.Foreground);

					//mat_proj = Matrix3x2.CreateScale(Vulkan.p_ortho.)

					var uv_offset = worldmap_offset / scale_canvas;
					var rect = window.group.GetOuterRect();

					var tex_scale = 16.00f;
					var tex_scale_inv = 1.00f / tex_scale;

					//GUI.DrawTexture("ui_worldmap_grid", new AABB(Vector2.Zero, GUI.CanvasSize), GUI.Layer.Background, uv_0: Vector2.Zero - uv_offset, uv_1: (Vector2.One * aspect / (zoom * 0.125f * 0.125f)) - uv_offset, clip: false, color: Color32BGRA.White.WithAlphaMult(1.00f));
					GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Background, uv_0: Vector2.Transform(rect.a - (GUI.CanvasSize * 0.50f), mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b - (GUI.CanvasSize * 0.50f), mat_c2l) * tex_scale_inv, clip: false, color: Color32BGRA.White.WithAlphaMult(0.20f));


					//if (kb.GetKey(Keyboard.Key.MoveRight)) worldmap_offset.X += speed;
					//if (kb.GetKey(Keyboard.Key.MoveLeft)) worldmap_offset.X -= speed;
					//if (kb.GetKey(Keyboard.Key.MoveUp)) worldmap_offset.Y -= speed;
					//if (kb.GetKey(Keyboard.Key.MoveDown)) worldmap_offset.Y += speed;

					var tm = Vector2.Transform(mouse_pos, mat_c2l);
					//var tm2 = Vector2.Transform(tm, mat_l2c);
					//GUI.DrawCircleFilled(tm2, 0.125f * zoom, Color32BGRA.White, 8, layer: GUI.Layer.Foreground);


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

								//GUI.DrawLine((((Vector2)a) + worldmap_offset) * zoom, (((Vector2)b) + worldmap_offset) * zoom, Color32BGRA.Black, thickness: 4.00f, layer: GUI.Layer.Foreground);
								//GUI.DrawLine(ta, tb, color, thickness: thickness * zoom, layer: GUI.Layer.Window);
								GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
								sb.AppendLine($"{ta}, {tb}");

								//DebugDrawFatSegment(a, b, radius, outlineColor, fillColor, data);
							}
							last_vert = vert;
						}
					}

					//App.WriteLine(GUI.GetMouse().GetKeyUp(Mouse.Key.Right));

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

								if ((Vector2.DistanceSquared(point, tm) <= 0.75f.Pow2()) || (edit_asset == asset && edit_points_index == i))
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
							pos_center /= points.Length;
							pos_center += asset_data.offset;

							GUI.DrawPolygon(points_t_span, asset_data.color_fill with { a = 100 }, GUI.Layer.Window);

							//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
							DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f, 4.00f, tex_line_district);

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

								if ((Vector2.DistanceSquared(point, tm) <= 0.25f.Pow2()) || (edit_asset == asset && edit_points_index == i))
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
							pos_center /= points.Length;
						}
					}		

					ref var world_info = ref Client.GetWorldInfo();
					if (world_info.IsNotNull())
					{
						if (world_info.maps != null)
						{
							var mod_context = App.GetModContext();

							for (var i = 0u; i < world_info.maps.Length; i++)
							{
								ref var map_info = ref world_info.maps[i];
								if (map_info.IsNotNull())
								{
									var size = 0.75f;
									var color = GUI.font_color_title;

									//map_info.

									var rect_map = AABB.Centered(Vector2.Transform((Vector2)map_info.point, mat_l2c), new Vector2(size * zoom * 0.50f));
									var rect_map_lg = AABB.Centered(Vector2.Transform(((Vector2)map_info.point) + new Vector2(0.00f, -0.875f), mat_l2c), new Vector2(size * zoom * 1.50f));

									var is_hovered = GUI.IsHoveringRect(rect_map_lg);
									var scale = 1.00f;
									//if (is_hovered) scale *= 1.50f;

									var map_asset = mod_context.GetMap(map_info.identifier);
									if (map_asset != null)
									{
										var tex_thumbnail = map_asset.GetThumbnail();
										if (tex_thumbnail != null)
										{
											GUI.DrawTexture(tex_thumbnail.Identifier, rect_map_lg, GUI.Layer.Window);
										}

										GUI.DrawBackground(is_hovered ? GUI.tex_frame_white : GUI.tex_frame, rect_map_lg, padding: new(4 * zoom_inv));
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
									}

									GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 16, 16, 0, 0), rect_map, layer: GUI.Layer.Window, color: is_hovered ? GUI.col_white : GUI.col_button, scale: zoom * 0.0625f * scale);
									//GUI.DrawRectFilled(rect_map, color, layer: GUI.Layer.Window);
									GUI.DrawTextCentered(map_info.name, Vector2.Transform(((Vector2)map_info.point) + new Vector2(0.00f, -0.25f - 0.10f), mat_l2c), pivot: new Vector2(0.50f, 0.50f), color: color, font: GUI.Font.Superstar, size: 0.37f * size * zoom * scale, layer: GUI.Layer.Window, box_shadow: true);
								}
							}
						}
					}

					foreach (var asset in ILocation.Database.GetAssets())
					{
						if (asset.id == 0) continue;

						ref var asset_data = ref asset.GetData();

						var scale = 0.500f;
						var rect_location = AABB.Centered(Vector2.Transform((Vector2)asset_data.point, mat_l2c), new Vector2(scale * zoom * 0.50f));

						//GUI.DrawCircleFilled(Vector2.Transform((Vector2)asset_data.point, mat_l2c), 0.175f * asset_data.size * zoom * scale, asset_data.color, segments: 4, layer: GUI.Layer.Window);

						GUI.DrawSpriteCentered(asset_data.icon, rect_location, layer: GUI.Layer.Window, 0.125f * scale * zoom * asset_data.size, color: asset_data.color);
						//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.50f * asset_data.size), mat_l2c), pivot: new(0.50f, 0.50f), color: asset_data.color, font: GUI.Font.Superstar, size: 0.75f * asset_data.size * zoom * scale, layer: GUI.Layer.Window);
						GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.50f * asset_data.size), mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * asset_data.size * zoom * scale, layer: GUI.Layer.Window, box_shadow: true);
					}

					if (edit_points_index.TryGetValue(out var v_edit_points_index))
					{
						edit_points[v_edit_points_index] = new int2((int)MathF.Round(tm.X), (int)MathF.Round(tm.Y));

						if (mouse.GetKeyUp(Mouse.Key.Right))
						{
							edit_asset.Save();

							edit_points_index = null;
							edit_points = null;
							edit_asset = null;
						}
					}



					//GUI.DrawBackground(GUI.tex_window_menu, window.group.GetOuterRect(), new(4));

					//GUI.TitleCentered($"{zoom}\n{worldmap_offset}\n{mouse.GetInterpolatedPosition()}\n{tm}\n{mat_l2c}\n{sb.ToString()}", pivot: new(0.00f, 0.00f));
					//GUI.TitleCentered($"{zoom}\n{worldmap_offset}\n{mouse.GetInterpolatedPosition()}\n{tm}\n{mat_proj}\n{mat_view}\n{mat_l2c}\n{mat_c2l}\n{mat_vp}", pivot: new(0.00f, 0.00f));
					//GUI.TitleCentered($"{tm}\n{window.group.a}\n{window.group.size}\n{window_pos}\n{mouse_pos}\n{tm}\n{mat_c2l}", pivot: new(0.00f, 0.00f));
				}
			}
		}
#endif
	}
}

