
namespace TC2.Conquest
{
	public static partial class Unit
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			public enum Type: uint
			{
				Undefined = 0,

				Character,
				Convoy,
			}

			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public Unit.Data.Flags flags;

			public ILocation.Handle h_location;
			public Vector2 pos_target;
			public float speed = 1.00f;

			public Data()
			{
			}
		}

		public struct TestRPC: Net.IGRPC<World.Global>
		{
			public ICharacter.Handle h_character;

#if SERVER
			public void Invoke(ref NetConnection connection, ref World.Global data)
			{
				ref var character_data = ref this.h_character.GetData(out var character_asset);
				Assert.NotNull(ref character_data);

				var h_location = character_data.h_location_current;
				
				ref var location_data = ref h_location.GetData(out var location_asset);
				Assert.NotNull(ref location_data);

				ref var region = ref World.GetGlobalRegion();
				var ent_asset = this.h_character.AsEntity(0);

				var random = XorRandom.New(true);

				region.SpawnPrefab("unit.guy", position: new Vector2(location_data.point.X, location_data.point.Y) + random.NextUnitVector2Range(0.25f, 0.50f), faction_id: character_data.faction, entity: ent_asset).ContinueWith((ent) =>
				{
					ref var unit = ref ent.GetComponent<Unit.Data>();
					if (unit.IsNotNull())
					{
						unit.h_location = default;
					}

					ref var nameable = ref ent.GetComponent<Nameable.Data>();
					if (nameable.IsNotNull())
					{
						nameable.name = character_asset.GetName();
						//nameable.Sync(ent, true);
					}
				});
			}
#endif
		}

		public struct MoveRPC: Net.IRPC<Unit.Data>
		{
			public Vector2 pos_target;

#if SERVER
			public void Invoke(ref NetConnection connection, Entity entity, ref Unit.Data data)
			{
				data.pos_target = this.pos_target;

				data.Sync(entity, true);
			}
#endif
		}

		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
		{

		}

		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnUpdateGlobal(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
		{
			var dir = (unit.pos_target - transform.position).GetNormalized(out var dist);
			transform.position += (dir * Maths.Min(dist, unit.speed * info.DeltaTime));
		}

#if CLIENT
		public partial struct UnitGUI: IGUICommand
		{
			public Entity ent_unit;
			public Unit.Data unit;
			public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_unit.GetRegionCommon();

				using (var window = GUI.Window.Interaction("unit", this.ent_unit))
				{
					if (window.show)
					{
						var mouse = GUI.GetMouse();

						var scale = region.GetWorldToCanvasScale();

						var pos = GUI.GetMousePosition(); // mouse.GetInterpolatedPosition();
						var pos_w = region.CanvasToWorld(pos);
						pos_w.Snap(0.125f, out var pos_w_snapped);

						var pos_c = region.WorldToCanvas(pos_w_snapped);

						var pos_c_current = region.WorldToCanvas(this.transform.GetInterpolatedPosition());
						var pos_c_target = region.WorldToCanvas(this.unit.pos_target);

						//GUI.Text($"{ent_unit}; {pos:0.00} {pos_w:0.00}; {pos_c:0.00}");

						GUI.DrawCircle(pos_c, 0.250f * region.GetWorldToCanvasScale(), Color32BGRA.Green, segments: 4, layer: GUI.Layer.Foreground);
						GUI.DrawCircle(pos_c_current, 0.750f * region.GetWorldToCanvasScale(), Color32BGRA.Yellow, segments: 16, layer: GUI.Layer.Foreground);
						GUI.DrawCircleFilled(pos_c_target, 0.1250f * region.GetWorldToCanvasScale(), Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);

						if (mouse.GetKeyDown(Mouse.Key.Right))
						{
							var rpc = new Unit.MoveRPC();
							rpc.pos_target = pos_w_snapped;
							rpc.Send(this.ent_unit);
						}
					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Unit.UnitGUI()
				{
					ent_unit = entity,
					unit = unit,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

