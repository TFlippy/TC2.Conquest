
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
#if CLIENT
			//GUI.worldmenu_widget_size_override 
			GUI.func_worldmenu_override = static () =>
			{
				WorldMap.Draw(GUI.GetRemainingSpace());
			};
#endif
		}

		protected override void OnWorldTick(ref World.Data world)
		{

		}

#if SERVER
		protected override void OnPreprocessMap(Bitmap bitmap, ref IMap.Info map_info)
		{

		}
#endif

#if CLIENT
		public static float overlay_alpha = 0.00f;

		protected override void OnGUI()
		{
			GUI.worldmenu_widget_size_override = new Vector2(1000, 600);

			if (Client.IsLoadingRegion())
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 1.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);
			}
			else
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 0.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);
			}

			if (overlay_alpha > 0.01f)
			{
				using (var window = GUI.Window.HUD("worldmap.loading", size: GUI.CanvasSize, position: GUI.CanvasSize * 0.50f))
				{
					if (window.show)
					{
						var rect = window.group.GetOuterRect();

						GUI.DrawTexture(GUI.tex_white, rect, layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(1.00f * overlay_alpha));
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.50f * overlay_alpha));
						GUI.DrawTextCentered("Loading...", position: rect.GetPosition(), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground, color: GUI.font_color_title.WithAlphaMult(overlay_alpha));
					}
				}
			}
		}

		protected override void OnDrawRegionMenu()
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();

			GUI.RegionMenu.enabled = false;
			GUI.Menu.enable_background = true;

			if (overlay_alpha < 0.98f)
			{
				//var viewport_size = new Vector2(1200, 800);
				var viewport_size = GUI.CanvasSize - new Vector2(64, 64);
				//var window_pos = new Vector2(GUI.CanvasSize.X * 0.50f, 48);
				var window_pos = GUI.CanvasSize * 0.50f;
				//var pivot = new Vector2(0.50f, 0.00f);
				var pivot = new Vector2(0.50f, 0.50f);
				//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
				using (var window = GUI.Window.Standalone("region_menu", position: window_pos, size: viewport_size, pivot: pivot, padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
				//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.50f, size: viewport_size, pivot: new(0.50f, 0.50f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
				{
					if (window.show)
					{
						GUI.DrawWindowBackground(GUI.tex_window_character);
						WorldMap.Draw(GUI.GetRemainingSpace());
					}
				}
			}
			//else
			//{
			//	using (var window = GUI.Window.HUD("worldmap.loading", size: GUI.CanvasSize, position: GUI.CanvasSize * 0.50f))
			//	{
			//		if (window.show)
			//		{
			//			var rect = window.group.GetOuterRect();

			//			GUI.DrawTexture("white", GUI.GetCanvasRect(), layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.80f));
			//			GUI.DrawTexture("vignette", GUI.GetCanvasRect(), layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.50f));
			//			GUI.DrawTextCentered("Loading...", position: rect.GetPosition(), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground);
			//		}
			//	}
			//}
		}
#endif
	}
}

