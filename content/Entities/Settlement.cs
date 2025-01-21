
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	public static partial class Settlement
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public enum Type: byte
			{
				Undefined = 0,
			}

			public Settlement.Data.Flags flags;
			public Settlement.Data.Type type;
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Settlement.Data settlement, [Source.Owned] ref Transform.Data transform)
		{

		}

#if CLIENT
		public partial struct SettlementGUI: IGUICommand
		{
			public Entity ent_settlement;
			public Settlement.Data settlement;
			public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_settlement.GetRegionCommon();

				using (var window = GUI.Window.Interaction("Settlement"u8, this.ent_settlement))
				{
					if (window.show)
					{

					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Settlement.Data settlement, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Settlement.SettlementGUI()
				{
					ent_settlement = entity,
					settlement = settlement,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

