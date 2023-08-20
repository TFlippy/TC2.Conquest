
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

		}
#endif

#if CLIENT
		protected override void OnDrawRegionMenu()
		{
			WorldMap.Draw();
		}
#endif
	}
}

