
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
			if (!Constants.World.edit_mode)
			{
				var random = XorRandom.New(true);

				var pixels_span = bitmap.GetPixels();
				var w = bitmap.width;
				var h = bitmap.height;

				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "stone", 0.80f, h_offset: -0.50f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 2.00f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "stone", 0.50f, h_offset: 0.00f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 1.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "silver.ore", 0.50f, h_offset: 0.10f, scale_0: 2.00f, scale_1: 0.50f, scale_2: 1.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("silver.ore"), "gold.ore", 1.20f, h_offset: -0.70f, scale_0: 1.50f, scale_1: 2.00f, scale_2: 1.75f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "iron.ore", 0.60f, h_offset: 0.10f, scale_0: 1.20f, scale_1: 0.50f, scale_2: 3.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("iron.ore"), "pjerdelite", 0.50f, h_offset: -0.20f, scale_0: 0.80f, scale_1: 0.50f, scale_2: 6.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "smirglum.ore", 1.00f, h_offset: -0.80f, scale_0: 0.50f, scale_1: 0.50f, scale_2: 4.50f);
			}
		}
#endif

#if CLIENT
		public static Vector2 worldmap_offset;
		public static float worldmap_zoom = 1.00f;

		protected override void OnDrawRegionMenu()
		{
			return;

			GUI.RegionMenu.enabled = false;
			GUI.Menu.enable_background = false;

			using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
			{
				if (window.show)
				{
					var aspect = GUI.CanvasSize.GetNormalized();

					var mouse = GUI.GetMouse();
					var kb = GUI.GetKeyboard();

					worldmap_zoom -= mouse.GetScroll(0.25f);
					worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 4.00f);

					var zoom = MathF.Pow(2.00f, worldmap_zoom);

					if (mouse.GetKey(Mouse.Key.Left))
					{
						worldmap_offset += mouse.GetDelta() * GUI.GetWorldToCanvasScale() / zoom;
					}

					var uv_offset = worldmap_offset / GUI.GetWorldToCanvasScale();
					GUI.DrawTexture("ui_worldmap_grid", new AABB(Vector2.Zero, GUI.CanvasSize), GUI.Layer.Background, uv_0: Vector2.Zero - uv_offset, uv_1: (Vector2.One * aspect / (zoom * 0.125f * 0.125f)) - uv_offset, clip: false, color: Color32BGRA.White.WithAlphaMult(1.00f));


					//if (kb.GetKey(Keyboard.Key.MoveRight)) worldmap_offset.X += speed;
					//if (kb.GetKey(Keyboard.Key.MoveLeft)) worldmap_offset.X -= speed;
					//if (kb.GetKey(Keyboard.Key.MoveUp)) worldmap_offset.Y -= speed;
					//if (kb.GetKey(Keyboard.Key.MoveDown)) worldmap_offset.Y += speed;

					var h_location = new ILocation.Handle("province.krumpel_island");
					ref var location_data = ref h_location.GetData();
					if (location_data.IsNotNull())
					{
						var points = location_data.points;
						if (points != null)
						{
							var count = points.Length;

							var last_vert = default(int2);
							for (var i = 0; i < (count + 1); i++)
							{
								ref var vert = ref points[i % count];
								if (i > 0)
								{
									var a = last_vert;
									var b = vert;

									GUI.DrawLine((((Vector2)a) + worldmap_offset) * zoom, (((Vector2)b) + worldmap_offset) * zoom, Color32BGRA.Black, thickness: 4.00f, layer: GUI.Layer.Foreground);

									//DebugDrawFatSegment(a, b, radius, outlineColor, fillColor, data);
								}
								last_vert = vert;
							}
						}
					}

					//GUI.DrawBackground(GUI.tex_window_menu, window.group.GetOuterRect(), new(4));

					GUI.Text($"derp {zoom}; {mouse.position}");
				}
			}
		}
#endif
	}
}

