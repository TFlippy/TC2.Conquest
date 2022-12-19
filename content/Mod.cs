
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

#if CLIENT
        protected override void OnDrawRegionMenu()
		{
			//GUI.RegionMenu.enabled = false;
			//GUI.Menu.enable_background = false;

			//using (var window = GUI.Window.Standalone("region_menu", position: GUI.CanvasSize * 0.50f, size: new Vector2(600, 400), padding: new(8), force_position: false))
			//{
			//	if (window.show)
			//	{
			//		GUI.DrawBackground(GUI.tex_window_menu, window.group.GetOuterRect(), new(4));

			//		GUI.Text("derp");
			//	}
			//}
		}
#endif
    }
}

