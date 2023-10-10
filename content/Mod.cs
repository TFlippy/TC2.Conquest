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

			var has_region = Client.GetWorld().IsNotNull() && Client.GetRegion().IsNotNull();
			GUI.worldmenu_widget_size_override = has_region ? new Vector2(1200, 800) : new Vector2(1680, 980);
			GUI.RegionMenu.enabled = false;

			//if (!has_region) GUI.GameMenu.widget_toggle_open = true;

			if (Client.IsLoadingRegion())
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 1.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);
			}
			else
			{
				overlay_alpha = Maths.Lerp(overlay_alpha, 0.00f, 0.10f); // App.fixed_update_interval_s * 2.00f);

				//GUI.RegionMenu.enabled = false;
				GUI.Menu.enable_background = true;

				// TODO: move this elsewhere
				//using (var widget = Sidebar.Widget.New("menu.worldmap", "World Map", new Sprite(GUI.tex_icons_widget, 16, 16, 9, 1), size: GUI.worldmenu_widget_size_override ?? new Vector2(600, 700), order: -10.00f))
				//{
				//	ref readonly var kb = ref Control.GetKeyboard();
				//	if (kb.GetKeyDown(Keyboard.Key.M) || GUI.GameMenu.widget_toggle_open.HasValue)
				//	{
				//		if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && (!GUI.GameMenu.widget_toggle_open ?? true))
				//		{
				//			Sound.PlayGUI(GUI.sound_window_close, volume: 0.40f);
				//			widget.SetActive(false);
				//		}
				//		else if (GUI.GameMenu.widget_toggle_open ?? true)
				//		{
				//			Sound.PlayGUI(GUI.sound_window_open, volume: 0.40f);
				//			widget.SetActive(true);
				//		}

				//		GUI.GameMenu.widget_toggle_open = null;
				//	}

				//	//App.WriteLine("hi");

				//	if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && !Client.IsLoadingRegion())
				//	{
				//		//App.WriteLine("hi");

				//		using (var group = GUI.Group.New(GUI.GetAvailableSize()))
				//		{
				//			group.DrawRect(Color32BGRA.Magenta);

				//			//group.DrawBackground(GUI.tex_panel);
				//			//GUI.Text("yes");

				//			//WorldMap.Draw();
				//		}
				//	}
				//}

				//using (var window = GUI.Window.Standalone("test", size: new(100, 200)))
				//{
				//	window.group.DrawBackground(GUI.tex_panel);
				//}

				//{
				//	ref var player = ref Client.GetPlayer();
				//	if (player.IsNotNull())
				//	{
				//		ref var region = ref Client.GetRegion();
				//		if (region.IsNotNull())
				//		{

				//			var pos_tmp = GUI.GetMouse().GetInterpolatedPosition();
				//			GUI.DrawCircleFilled(region.WorldToCanvas(pos_tmp), 16.00f, color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground);
				//			GUI.DrawLine(GUI.CanvasSize * 0.50f, region.WorldToCanvas(pos_tmp), color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground);

				//			//GUI.DrawTextCentered($"{region.GetWorldToCanvasMatrix():0.0000}\n{region.WorldToCanvas(pos_tmp)}\n{GUI.WorldToCanvas(pos_tmp)}\n{pos_tmp}\n{Vulkan.v_lerp}", GUI.CanvasSize * 0.50f);
				//		}
				//	}
				//}
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

		[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global, order: -50)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void UpdateWorldmap(ISystem.Info.Global info)
		{
			using (var widget = Sidebar.Widget.New("menu.worldmap", "World Map", new Sprite(GUI.tex_icons_widget, 16, 16, 9, 1), size: GUI.worldmenu_widget_size_override ?? new Vector2(600, 700), order: -10.00f, flags: Sidebar.Widget.Flags.No_Front_On_Focus))
			{
				ref readonly var kb = ref Control.GetKeyboard();
				if (kb.GetKeyDown(Keyboard.Key.M) || GUI.GameMenu.widget_toggle_open.HasValue)
				{
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && (!GUI.GameMenu.widget_toggle_open ?? true))
					{
						Sound.PlayGUI(GUI.sound_window_close, volume: 0.40f);
						widget.SetActive(false);
					}
					else if (GUI.GameMenu.widget_toggle_open ?? true)
					{
						Sound.PlayGUI(GUI.sound_window_open, volume: 0.40f);
						widget.SetActive(true);
					}

					GUI.GameMenu.widget_toggle_open = null;
				}

				//App.WriteLine("hi");

				if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show) && !Client.IsLoadingRegion())
				{
					//App.WriteLine("hi");

					using (var group = GUI.Group.New(GUI.Av))
					{
						//group.DrawRect(Color32BGRA.Magenta);

						//group.DrawBackground(GUI.tex_panel);
						//GUI.Text("yes");

						WorldMap.Draw();
					}
				}
			}

			//var gui = new SidebarHUD()
			//{

			//};
			//gui.Submit();
		}

		protected override void OnDrawRegionMenu()
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();

			//GUI.RegionMenu.enabled = false;
			//GUI.Menu.enable_background = true;

			if (overlay_alpha < 0.98f)
			{
				//var viewport_size = new Vector2(1200, 800);
				var viewport_size = GUI.CanvasSize - new Vector2(64, 64);
				//var viewport_size = WorldMap.worldmap_window_size; // GUI.CanvasSize - new Vector2(64, 64);
				//var window_pos = new Vector2(GUI.CanvasSize.X * 0.50f, 48);
				//var window_pos = (GUI.CanvasSize * 0.50f) + WorldMap.worldmap_window_offset;
				var window_pos = (GUI.CanvasSize * 0.50f);
				//var pivot = new Vector2(0.50f, 0.00f);
				var pivot = new Vector2(0.50f, 0.50f);
				//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.00f, size: new Vector2(600, 400), pivot: new(0, 0), padding: new(8), force_position: true))
				//using (var window = GUI.Window.Standalone("region_menu", position: window_pos, size: viewport_size, pivot: pivot, padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus))
				//{
				//	if (window.show)
				//	{
				//		GUI.DrawWindowBackground(GUI.tex_window_character);
				//		WorldMap.Draw(GUI.GetRemainingSpace());
				//	}
				//}
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

