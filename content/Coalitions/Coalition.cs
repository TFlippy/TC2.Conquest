using TC2.Base.Components;
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	[Asset.Hjson(prefix: "coalition.", capacity_world: 8, capacity_region: 0, capacity_local: 4, 
	flags_global: Asset.Flags.Auto_Entity, flags_world: Asset.Flags.Auto_Entity)]
	public interface ICoalition: IAsset2<ICoalition, ICoalition.Data>
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,

		}

		[Flags]
		public enum Tags: ulong
		{
			None = 0,

		}


		static void IAsset2<ICoalition, ICoalition.Data>.OnRefresh(ICoalition.Definition definition)
		{
			ref var data = ref definition.GetData();

			var identifier = definition.identifier;
			var h_coalition = definition.GetHandle();
			var index = (nuint)h_coalition.id;


		}

		public struct Data(): IName, IDescription, IIcon
		{
			[Save.Force] public required string name;
			[Save.Force] public required string name_short;
			[Save.Force] public required string name_imperial;

			[Save.NewLine]
			[Save.Force, Save.MultiLine] public string desc;
			[Save.Force, Save.MultiLine] public string goal;

			[Save.NewLine]
			[Save.MultiLine] public string lore;

			[Save.NewLine]
			[Save.Force] public required ICoalition.Flags flags;
			[Save.Force] public required ICoalition.Tags tags;
			[Save.Force] public required NPC.Connotations connotations;
			[Save.Force] public required IMap.Services services;
			[Save.Force] public required IMap.Industry industry;
			[Save.Force] public required ICompany.Authority authority;

			[Save.NewLine]
			public Ideology ideology;

			[Save.NewLine]
			[Save.Force] public required ICompany.Handle h_company;
			[Save.Force] public required IFaction.Handle h_faction;
			[Save.Force] public required ICatalogue.Handle h_catalogue;
			[Save.Force] public required ILocation.Handle h_location_headquarters;

			[Save.NewLine]
			[Save.Force] public required Color32BGRA color_a;
			[Save.Force] public required Color32BGRA color_b;

			[Save.NewLine]
			[Save.Force] public required Sprite icon;

			readonly ReadOnlySpan<char> IName.GetName() => this.name;
			readonly ReadOnlySpan<char> IName.GetShortName() => this.name_short;
			readonly ReadOnlySpan<char> IDescription.GetDescription() => this.desc;
			readonly Sprite IIcon.GetIcon() => this.icon;
		}
	}

	public static partial class Coalition
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,
			}

			public ICoalition.Handle h_coalition;
			public Coalition.Data.Flags flags;

			public uint unused_00;
			public uint unused_01;
			public uint unused_02;
		}

		//[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		//public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		//[Source.Owned] ref Coalition.Data coalition)
		//{

		//}

#if CLIENT
		public partial struct CoalitionGUI: IGUICommand
		{
			public Entity ent_coalition;
			public Coalition.Data coalition;
	
			public void Draw()
			{
				//var rect_canvas = GUI.GetCanvasRect();

				//using (var window = GUI.Window.Standalone(identifier: "hud.coalition"u8,
				//position: rect_canvas.GetPosition(pivot: new(0.50f, 1.00f), offset: new(0.00f, -(96.00f + 12.00f))),
				//pivot: new(0.50f, 1.00f),
				//size: new(0.00f, 64.00f),
				//flags: GUI.Window.Flags.No_Appear_Focus))
				//{

				//}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Common info, ref Region.Data.Common region, Entity ent_coalition,
		[Source.Owned] in Coalition.Data coalition)
		{
			//var gui = new Coalition.CoalitionGUI()
			//{
			//	ent_coalition = ent_coalition,
			//	coalition = coalition
			//};
			//gui.Submit();
		}
#endif
	}
}

