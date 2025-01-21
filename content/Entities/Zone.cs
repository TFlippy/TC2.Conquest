
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	public static partial class Zone
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public Zone.Data.Flags flags;

		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Zone.Data zone, [Source.Owned] ref Transform.Data transform)
		{

		}

#if CLIENT
		public partial struct ZoneGUI: IGUICommand
		{
			public Entity ent_zone;
			public Zone.Data zone;
			public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_zone.GetRegionCommon();

				using (var window = GUI.Window.Interaction("Zone"u8, this.ent_zone))
				{
					if (window.show)
					{

					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Zone.Data zone, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Zone.ZoneGUI()
				{
					ent_zone = entity,
					zone = zone,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

