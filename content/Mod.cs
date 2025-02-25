
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
			WorldMap.Init();

			//#if CLIENT
			//			//GUI.worldmenu_widget_size_override 
			//			GUI.func_worldmenu_override = static () =>
			//			{
			//				WorldMap.Draw(GUI.GetRemainingSpace());
			//			};
			//#endif
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
			var has_region = Client.HasRegion() || Client.IsLoadingRegion();
			GUI.worldmenu_widget_size_override = has_region ? new Vector2(Maths.Min(1380, GUI.CanvasSize.X - 400), Maths.Min(860, GUI.CanvasSize.Y - 200)) : new Vector2(1680, 980);
			GUI.RegionMenu.enabled = false;

			//if (!has_region) GUI.GameMenu.widget_toggle_open = true;

			if (Client.IsLoadingRegion())
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 1.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);
			}
			else
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 0.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);
				GUI.Menu.enable_background = true;
			}

			if (overlay_alpha > 0.01f)
			{
				using (var window = GUI.Window.HUD("worldmap.loading"u8, size: GUI.CanvasSize, position: GUI.CanvasSize * 0.50f))
				{
					if (window.show)
					{
						var rect = window.group.GetOuterRect();

						GUI.DrawTexture(GUI.tex_white, rect, layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(overlay_alpha));
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.50f * overlay_alpha));
						GUI.DrawTextCentered("Loading..."u8, position: rect.GetPosition(), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground, color: GUI.font_color_title.WithAlphaMult(overlay_alpha));
					}
				}
			}
		}

		[Ugly]
		[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global, order: -50)]
		public static void UpdateWorldmap(ISystem.Info.Global info)
		{
			var has_region = Client.HasRegion() || Client.IsLoadingRegion();
			var flags = Sidebar.Widget.Flags.No_Front_On_Focus;
			var override_pos = default(Vector2?);
			var override_size = GUI.worldmenu_widget_size_override;

			var rect_canvas = GUI.GetCanvasRect();
			var rect_window = rect_canvas;

			if (!has_region)
			{
				flags |= Sidebar.Widget.Flags.Force_Open | Sidebar.Widget.Flags.Hide_Icon | Sidebar.Widget.Flags.Hide_Title | Sidebar.Widget.Flags.No_Background;
				//rect_window = rect_canvas.Pad(128, 32, 24, 24 + 32).Constrain(rect_window); //.Pad(120, 40, 120, 120);
				//rect_window = rect_canvas.Pad(32, 32, 24, 24 + 32).Constrain(rect_window); //.Pad(120, 40, 120, 120);
				//rect_window = rect_canvas.Pad(24, -32, 24, 24 + 32).Constrain(rect_window.Scale(2)); //.Pad(120, 40, 120, 120);
				rect_window = rect_canvas.Pad(0, -32, 0, 32).Constrain(rect_window.Scale(2)); //.Pad(120, 40, 120, 120);

				override_size = rect_window.GetSize();
				override_pos = rect_window.GetPosition(new(0.50f, 0.00f));
			}
			else
			{
				//flags |= Sidebar.Widget.Flags.Resizable;
			}

			using (var widget = Sidebar.Widget.New("menu.worldmap", "World Map", new Sprite(GUI.tex_icons_widget, 16, 16, 9, 1), size: override_size ?? new Vector2(600, 700), override_pos: override_pos, order: -10.00f, flags: flags))
			{
				ref readonly var kb = ref Control.GetKeyboard();
				if (kb.GetKeyDown(Keyboard.Key.M) || GUI.GameMenu.widget_toggle_open.HasValue)
				{
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && (!GUI.GameMenu.widget_toggle_open ?? true))
					{
						if (widget.IsActive()) Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
						widget.SetActive(false);
					}
					else if (GUI.GameMenu.widget_toggle_open ?? true)
					{
						if (!widget.IsActive()) Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f);
						widget.SetActive(true);
					}

					GUI.GameMenu.widget_toggle_open = null;
				}

				if (!has_region)
				{
					widget.SetActive(true);
				}

				if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && !Client.IsLoadingRegion())
				{
					using (var group = GUI.Group.New(GUI.Av))
					{
						WorldMap.Draw();
					}
				}
			}
		}

		protected override void OnDrawRegionMenu()
		{
			//ref var world = ref Client.GetWorld();
			//if (world.IsNull()) return;

			////return;
			//var is_loading = Client.IsLoadingRegion();

			////GUI.RegionMenu.enabled = false;
			////GUI.Menu.enable_background = true;

			//if (overlay_alpha < 0.98f)
			//{
			//	//var viewport_size = new Vector2(1200, 800);
			//	var viewport_size = GUI.CanvasSize - new Vector2(64, 64);
			//	//var viewport_size = WorldMap.worldmap_window_size; // GUI.CanvasSize - new Vector2(64, 64);
			//	//var window_pos = new Vector2(GUI.CanvasSize.X * 0.50f, 48);
			//	//var window_pos = (GUI.CanvasSize * 0.50f) + WorldMap.worldmap_window_offset;
			//	var window_pos = (GUI.CanvasSize * 0.50f);
			//	//var pivot = new Vector2(0.50f, 0.00f);
			//	var pivot = new Vector2(0.50f, 0.50f);
			//	//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
			//	//using (var window = GUI.Window.Standalone("region_menu", position: window_pos, size: viewport_size, pivot: pivot, padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
			//	//{
			//	//	if (window.show)
			//	//	{
			//	//		GUI.DrawWindowBackground(GUI.tex_window_character);
			//	//		WorldMap.Draw(GUI.GetRemainingSpace());
			//	//	}
			//	//}
			//}
			////else
			////{
			////	using (var window = GUI.Window.HUD("worldmap.loading", size: GUI.CanvasSize, position: GUI.CanvasSize * 0.50f))
			////	{
			////		if (window.show)
			////		{
			////			var rect = window.group.GetOuterRect();

			////			GUI.DrawTexture("white", GUI.GetCanvasRect(), layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.80f));
			////			GUI.DrawTexture("vignette", GUI.GetCanvasRect(), layer: GUI.Layer.Foreground, color: Color32BGRA.Black.WithAlphaMult(0.50f));
			////			GUI.DrawTextCentered("Loading...", position: rect.GetPosition(), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground);
			////		}
			////	}
			////}
		}
#endif
	}
}

