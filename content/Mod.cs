
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
		protected override void OnPreprocessMap(ref Map.Data map_data, ref Map.Info map_info)
		{
			if (!Constants.World.edit_mode)
			{
				var random = XorRandom.New(true);

				var pixels_span = map_data.GetPixels();
				var w = map_data.width;
				var h = map_data.height;

				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "stone", 0.80f, h_offset: -0.50f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 2.00f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "stone", 0.50f, h_offset: 0.00f, scale_0: 0.50f, scale_1: 0.80f, scale_2: 1.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("kruskite"), "silver.ore", 0.50f, h_offset: 0.10f, scale_0: 2.00f, scale_1: 0.50f, scale_2: 1.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("silver.ore"), "gold.ore", 1.20f, h_offset: -0.70f, scale_0: 1.50f, scale_1: 2.00f, scale_2: 1.75f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "iron.ore", 0.60f, h_offset: 0.10f, scale_0: 1.20f, scale_1: 0.50f, scale_2: 3.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("iron.ore"), "pjerdelite", 0.50f, h_offset: -0.20f, scale_0: 0.80f, scale_1: 0.50f, scale_2: 6.50f);
				Map.GenerateOres(ref random, pixels_span, w, h, IBlock.GetColor("pjerdelite"), "smirgl.ore", 1.00f, h_offset: -0.80f, scale_0: 0.50f, scale_1: 0.50f, scale_2: 4.50f);
			}
		}
#endif

#if CLIENT
		protected override void OnDrawRegionMenu()
		{
			//GUI.RegionMenu.enabled = false;
			//GUI.Menu.enable_background = false;

			//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.50f, size: new Vector2(600, 400), padding: new(8), force_position: false))
			//{
			//	if (window.show)
			//	{
			//		var aspect = GUI.CanvasSize.GetNormalized();
			//		var scale = 3.00f;

			//		GUI.DrawTexture("ui_worldmap_grid", new AABB(Vector2.Zero, GUI.CanvasSize), uv_0: Vector2.Zero, uv_1: Vector2.One * aspect * scale, clip: false);

			//		//GUI.DrawBackground(GUI.tex_window_menu, window.group.GetOuterRect(), new(4));

			//		GUI.Text("derp");
			//	}
			//}
		}
#endif
	}
}

