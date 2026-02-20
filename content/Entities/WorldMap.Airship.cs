
using System.Runtime.InteropServices;
using TC2.Base.Components;

namespace TC2.Conquest
{
	[Shitcode]
	public static partial class WorldMap
	{
		public static class Trader
		{
			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public enum State: byte
				{
					Undefined = 0,
				}

				public ICatalogue.Handle h_catalogue;

				public WorldMap.Trader.Data.Flags flags;
				public WorldMap.Trader.Data.State state;

				[Asset.Ignore] public ILocation.Handle h_location_current;
				[Asset.Ignore] public FixedArray8<ILocation.Handle> dock_request_locations;
			}
		}

		public static class Airship
		{
			// TODO: split this into a Trader component
			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					Active = 1u << 0,
					Docked = 1u << 1,

					In_Region = 1u << 2,
				}

				public enum State: byte
				{
					Undefined = 0,


					Moving,
					Docking,
					Docked,


				}

				public IRoute.Handle h_route;
				public ITransport.Handle h_transport;
				public Prefab.Handle h_prefab_region;

				public WorldMap.Airship.Data.Flags flags;
				public WorldMap.Airship.Data.State state;

				[Asset.Ignore] public int current_route_index;

				[Asset.Ignore] public ILocation.Handle h_location_target;
				[Asset.Ignore] public ILocation.Handle h_location_docked;
				[Asset.Ignore] public IEntrance.Handle h_entrance_docked;
				[Asset.Ignore] public IAddress.Handle h_address_docked;

				public Filter.Mask<ILocation.Buildings> mask_stop_buildings;
				public Filter.Mask<ILocation.Categories> mask_stop_categories;

				public float duration_docked = 30.00f;
				public float duration_resupply = 60.00f;

				public float dock_radius_max = 1.00f;

				[Asset.Ignore] public FixedArray8<ILocation.Handle> dock_request_locations; 

				[Asset.Ignore] public float t_stop_departing;
			}

			[Shitcode]
			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnUpdate(ISystem.Info.Global info, ref Region.Data.Global region, Entity ent_airship,
			[Source.Owned] ref WorldMap.Airship.Data airship, [Source.Owned] ref Transform.Data transform, 
			[Source.Owned] ref WorldMap.Unit.Data unit, [Source.Owned] ref WorldMap.Marker.Data marker)
			{
				ref var route_data = ref airship.h_route.GetData();
				if (route_data.IsNotNull())
				{
#if SERVER
					var sync = false;
#endif

					//ref var route_target_current = ref route_data.targets.GetRefAtIndexOrNull(route.current_route_index);
					//if (route_target_current.IsNotNull())
					//{
					//	//route.h_location_target = route_target_current.h_location;
					//}

					var h_location_target = airship.h_location_target;

					ref var location_data = ref h_location_target.GetData();
					if (location_data.IsNotNull())
					{
						var pos_target = (Vector2)location_data.point;
						var dir = (pos_target - transform.position).GetNormalized(out var dist);

						unit.pos_target = pos_target;
						unit.ent_target = h_location_target.GetGlobalEntity();
						unit.action = WorldMap.Unit.Action.Move;

#if SERVER
						if (Vec2f.IsInDistanceSq(transform.position, pos_target, 0.0000125f))
						{
							airship.current_route_index = (airship.current_route_index + 1) % route_data.targets.Length;
							//route.current_route_index.ne
						}
#endif

					}


#if SERVER
					ref var route_target_current = ref route_data.targets.GetRefAtIndexOrNull(airship.current_route_index);
					if (route_target_current.IsNotNull())
					{
						sync |= airship.h_location_target.TryChange(route_target_current.h_location);

						//unit.pos_target = 
					}

					//if (!route.h_location_target)
					//{
					//	route.h_lo
					//}
#endif

#if SERVER
					if (sync)
					{
						airship.Sync(ent_airship);
					}
#endif
				}
			}

#if CLIENT
			[Shitcode]
			public partial struct WindowGUI: IGUICommand
			{
				public Entity ent_airship;
				public WorldMap.Airship.Data airship;
				public Transform.Data transform;

				public static Sprite sprite_zeppelin = new("gui.icon.zeppelin.00", 24, 24);

				public void Draw()
				{
					ref var region = ref this.ent_airship.GetRegionCommon();

					var pos_world = transform.GetInterpolatedPosition();

					var h_location_nearest = WorldMap.GetNearestLocation(pos_world, out var nearest_location_dist_sq);
					var pos_location_nearest = h_location_nearest.GetPosition();
					var dist_location_nearest = nearest_location_dist_sq.Sqrt();

					var is_nearest_in_range = (dist_location_nearest <= this.airship.dock_radius_max);

					using (var window = GUI.Window.Interaction("Zeppelin"u8, this.ent_airship))
					{
						this.StoreCurrentWindowTypeID(order: -200);
						if (window.show)
						{
							using (var group = GUI.Group.New(size: new(GUI.RmX, 224), padding: new(6)))
							{
								group.DrawBackground(GUI.tex_window);

								using (var group_location = GUI.Group.New(size: new(224, 24)))
								{
									ref var location_target_data = ref this.airship.h_location_target.GetData();
									if (location_target_data.IsNotNull())
									{
										GUI.TitleCentered(location_target_data.name, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0));
									}
								}

								GUI.Title(is_nearest_in_range ? h_location_nearest.GetName() : "N/A");
								GUI.LabelShaded("Distance:"u8, dist_location_nearest, format: "0.00' km'");
							}
						}
					}

					WorldMap.worldmap_offset_target = pos_world;

					if (is_nearest_in_range)
					{
						GUI.DrawLine(region.WorldToCanvas(pos_world), region.WorldToCanvas(pos_location_nearest), 
							color: GUI.font_color_orange.WithAlpha(200), thickness: 2.00f, layer: GUI.Layer.Foreground);
					}

					GUI.DrawCircle(region.WorldToCanvas(pos_world), radius: this.airship.dock_radius_max * region.GetWorldToCanvasScale() * WorldMap.km_per_unit, 
						color: GUI.font_color_orange, layer: GUI.Layer.Foreground);

					ref var route_data = ref airship.h_route.GetData();
					if (route_data.IsNotNull())
					{
						Vector2 pos_prev_tmp = pos_world;

						var targets = route_data.targets.AsSpan();
						for (var i = 0; i < targets.Length; i++)
						{
							ref var target = ref targets[i];
							ref var location_data = ref target.h_location.GetData();
							if (location_data.IsNotNull())
							{
								var pos_tmp = (Vector2)location_data.point;
								//if (i > 0) GUI.DrawLine(region.WorldToCanvas(pos_prev), region.WorldToCanvas(pos), color: GUI.font_color_yellow.WithAlpha(200), thickness: 4.00f, layer: GUI.Layer.Foreground);
								if (i > 0) GUI.DrawLine(region.WorldToCanvas(pos_prev_tmp), region.WorldToCanvas(pos_tmp), color: GUI.col_default.WithAlpha(200), thickness: 1.00f, layer: GUI.Layer.Foreground);
								pos_prev_tmp = pos_tmp;
							}
						}
					}
				}
			}

			[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity ent_airship,
			[Source.Owned] ref WorldMap.Airship.Data airship, [Source.Owned] ref Transform.Data transform)
			{
				if (WorldMap.IsOpen && WorldMap.interacted_entity_cached == ent_airship)
				{
					var gui = new WorldMap.Airship.WindowGUI()
					{
						ent_airship = ent_airship,
						airship = airship,
						transform = transform
					};
					gui.Submit();
				}

				//App.WriteLine("GUI");
			}
#endif

#if SERVER
		[ChatCommand.Global("zeppelin", "", creative: true)]
		public static void ZeppelinCommand(ref ChatCommand.Context context, string route)
		{
			ref var region = ref context.GetRegionGlobal();
			var h_route = new IRoute.Handle(route);

			//ref var location_data = ref h_location.GetData(out var location_asset);
			//if (location_data.IsNotNull())
			//{
			//	if (WorldMap.location_to_rail.TryGetValue(h_location, out var rail))
			//	{
			//		region.SpawnPrefab("train", rail.GetPosition()).ContinueWith(ent_train =>
			//		{
			//			ref var train = ref ent_train.GetComponent<Train.Data>();
			//			if (train.IsNotNull())
			//			{
			//				train.sign = 1;

			//				train.segment_a = rail with { index = (byte)(rail.index - 1) };
			//				train.segment_b = rail with { index = rail.index };
			//				train.segment_c = rail with { index = (byte)(rail.index + 1) };

			//				train.Sync(ent_train, true);


			//			}
			//		});
			//	}
			//}
		}
#endif
		}
	}
}

