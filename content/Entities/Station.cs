
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	public static partial class Station
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public Station.Data.Flags flags;
			public Road.Type road_type;

			public Data()
			{
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Station.Data station, [Source.Owned] ref Transform.Data transform)
		{

		}

#if CLIENT
		public partial struct StationGUI: IGUICommand
		{
			public Entity ent_station;
			public Station.Data station;
			public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_station.GetRegionCommon();

				using (var window = GUI.Window.Interaction("station", this.ent_station))
				{
					if (window.show)
					{

					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Station.Data station, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Station.StationGUI()
				{
					ent_station = entity,
					station = station,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

