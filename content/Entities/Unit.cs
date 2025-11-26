
using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		//public static ILocation.Handle GetCurrentLocation(this ICharacter.Handle h_character)
		//{


		//	//if (h_character.TryGetDefinition(out var character_asset))
		//	//{
		//	//	if (character_asset.RegionID == 0)
		//	//	{
		//	//		var ent_character_global = character_asset.GetGlobalEntity();
		//	//		if (ent_character_global.IsAlive())
		//	//		{
		//	//			var ent_location_owner = ent_character_global.GetComponentOwner<Location.Data>(Relation.Type.Child);
		//	//			if (ent_location_owner.TryGetAssetHandle(out ILocation.Handle h_location))
		//	//			{
		//	//				//h_location.TryGetRegionID
		//	//				return h_location;
		//	//			}
		//	//		}
		//	//	}
		//	//	else
		//	//	{
		//	//	}
		//	//}

		//	//return ILocation.Handle.None;
		//}

		public static bool CanPlayerControlUnit(this Entity ent_unit, IPlayer.Handle h_player)
		{
			if (ent_unit.IsAlive())
			{
				ref var unit = ref ent_unit.GetComponent<Unit.Data>();
				return unit.CanPlayerControlUnit(ent_unit: ent_unit, h_player: h_player);
			}

			return false;
		}

		public static bool CanPlayerControlUnit(ref readonly this Unit.Data unit, Entity ent_unit, IPlayer.Handle h_player)
		{
			if (Unsafe.IsNullRef(in unit))
			{
				return true;
			}
			else
			{
				if (unit.flags.HasAny(Unit.Flags.Requires_Driver)) return h_player.CanControlCharacter(unit.h_character_driver);
				else if (unit.type == Unit.Type.Character)
				{
					return ent_unit.TryGetAssetHandle(out ICharacter.Handle h_character) && h_player.CanControlCharacter(h_character);
				}
			}

			return false;
		}

		public static partial class Enterable
		{
			[Query(ISystem.Scope.Global)]
			public delegate void GetAllQuery(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
				[Source.Owned] in Enterable.Data enterable, [Source.Owned] in Transform.Data transform,
				[Source.Owned, Optional(false)] in Faction.Data faction, [HasRelation(Source.Modifier.Owned, Relation.Type.Child, true)] bool has_parent);

			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
			public partial struct Data(): IComponent
			{
				public enum Type: uint
				{
					Undefined = 0,

					Vehicle,
					Location,
					Entrance
				}

				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					Hide_If_Parented = 1u << 0
				}

				public Enterable.Data.Type type;
				public Enterable.Data.Flags flags;

				public BitField<Unit.Type> mask_units;

				public float radius = 0.50f;
			}

			//[MethodImpl(MethodImplOptions.NoInlining)]
			public static Entity GetNearest(Vector2 pos, out float dist_sq, IFaction.Handle h_faction = default, Entity ent_exclude = default, Enterable.Data.Type type = Data.Type.Undefined)
			{
				ref var region = ref World.GetGlobalRegion();
				var dist_sq_current = float.MaxValue;
				var ent_nearest = Entity.None;

				foreach (ref var row in region.IterateQuery<WorldMap.Enterable.GetAllQuery>())
				{
					var index = row.Index;
					var count = row.Count;

					row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Enterable.Data enterable, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction, [HasRelation(Source.Modifier.Owned, Relation.Type.Child, true)] bool has_parent) =>
					{
						//try
						//{
#if CLIENT
						//region.DrawDebugText(transform.position, $"{entity} != {ent_exclude}; {index}/{info.Count}/{count}; {info.Offset}; {info.TableCount}", Color32BGRA.White);
#endif

						if ((enterable.flags.HasNone(Data.Flags.Hide_If_Parented) ? has_parent : true) && entity != ent_exclude && (!h_faction || h_faction == faction.id) && (type == Enterable.Data.Type.Undefined || enterable.type == type))
						{
							var dist_sq_tmp = Vector2.DistanceSquared(pos, transform.position); // - enterable.radius.Pow2();
							if (dist_sq_tmp < dist_sq_current)
							{
								dist_sq_current = dist_sq_tmp;
								ent_nearest = entity;
							}
						}
						//}
						//catch (Exception e)
						//{
						//	App.WriteException(e);
						//}
					});
				}

				dist_sq = dist_sq_current;
				return ent_nearest;
			}
		}

		public static partial class Unit
		{
			// TODO: add validation
			public struct EnterRPC: Net.IRPC<WorldMap.Unit.Data>
			{
				//public ICharacter.Handle h_character;
				public Entity ent_enterable;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref WorldMap.Unit.Data data)
				{
					Unit.TryEnter(rpc.entity, this.ent_enterable);

					//Assert.Check(this.ent_enterable != entity);
					//Assert.Check(this.ent_enterable.IsAlive());
					//Assert.Check(this.ent_enterable.GetRegionID() == entity.GetRegionID());
					////Assert.Check(!this.ent_enterable.TryGetParent(Relation.Type.Child, out var ent_enterable_parent));

					//ref var enterable = ref this.ent_enterable.GetComponent<WorldMap.Enterable.Data>();
					//Assert.IsNotNull(ref enterable);

					//Assert.Check(enterable.flags.HasNone(Enterable.Data.Flags.Hide_If_Parented) || !this.ent_enterable.GetParent(Relation.Type.Child).IsValid());

					//entity.AddRelation(this.ent_enterable, Relation.Type.Child, true);
				}
#endif
			}

			// TODO: add validation
			public struct ExitRPC: Net.IRPC<WorldMap.Unit.Data>
			{
				//public ICharacter.Handle h_character;
				//public Entity ent_unit;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref WorldMap.Unit.Data data)
				{
					Unit.TryExit(rpc.entity);
					//var ent_enterable = entity.GetParent(Relation.Type.Child);

					//Assert.Check(ent_enterable != entity);
					//Assert.Check(ent_enterable.IsAlive());
					//Assert.Check(ent_enterable.GetRegionID() == entity.GetRegionID());

					//ref var enterable = ref ent_enterable.GetComponent<WorldMap.Enterable.Data>();
					//Assert.IsNotNull(ref enterable);

					//ref var transform = ref ent_enterable.GetComponent<Transform.Data>();
					//Assert.IsNotNull(ref transform);

					////var road = WorldMap.GetNearestRoad(location_data.h_prefecture, Road.Type.Road, (Vector2)location_data.point, out var dist_sq);
					////var pos = road.GetPosition().GetRefValueOrDefault();

					////var random = XorRandom.New(true);
					////if (pos == default) pos = (Vector2)location_data.point + random.NextUnitVector2Range(0.25f, 0.50f);

					//var pos = transform.position;

					//var road = WorldMap.GetNearestRoad(Road.Type.Road, pos, out var dist_sq);
					//var pos_tmp = road.GetNearestPosition(pos, out dist_sq);

					//if (dist_sq <= enterable.radius.Pow2())
					//{
					//	pos = pos_tmp;
					//}

					//ref var region = ref World.GetGlobalRegion();

					////character_data.ent_inside = default;
					//if (entity.IsAlive())
					//{
					//	entity.RemoveRelation(ent_enterable, Relation.Type.Child);

					//	ref var transform_unit = ref entity.GetComponent<Transform.Data>();
					//	if (transform_unit.IsNotNull())
					//	{
					//		transform_unit.SetPosition(pos);
					//		transform_unit.Sync(entity);
					//	}
					//}


					//else
					//{
					//	region.SpawnPrefab("unit.guy", position: pos, entity: entity).ContinueWith((ent) =>
					//	{
					//		ref var unit = ref ent.GetComponent<Unit.Data>();
					//		if (unit.IsNotNull())
					//		{
					//			unit.pos_target = pos;
					//			unit.h_location = default;
					//			unit.Sync(ent);
					//		}

					//		ref var nameable = ref ent.GetComponent<Nameable.Data>();
					//		if (nameable.IsNotNull())
					//		{
					//			nameable.name = character_asset.GetName();
					//		}
					//	});
					//}



					//var h_location = default(ILocation.Handle);
					//if (entity.TryGetAssetHandle(out h_location) && WorldMap.location_to_road.TryGetValue(h_location, out var road))
					//{
					//	pos = road.GetPosition();
					//	App.WriteLine(pos);

					//	//ref var location_data = ref h_location.GetData();
					//	//if (location_data.IsNotNull())
					//	//{
					//	//	WorldMap.
					//	//}
					//}



					//character_asset.Sync();

					//ref var region = ref World.GetGlobalRegion();

					////character_data.ent_inside = default;
					//if (ent_character.IsAlive())
					//{
					//	ent_character.RemoveRelation(Entity.Wildcard, Relation.Type.Child);

					//	ref var transform_character = ref ent_character.GetComponent<Transform.Data>();
					//	if (transform_character.IsNotNull())
					//	{
					//		transform_character.SetPosition(pos);
					//		transform_character.Sync(ent_character);
					//	}
					//}
					////else
					//{
					//	region.SpawnPrefab("unit.guy", position: pos, faction_id: character_data.faction, entity: ent_character).ContinueWith((ent) =>
					//	{
					//		ref var unit = ref ent.GetComponent<Unit.Data>();
					//		if (unit.IsNotNull())
					//		{
					//			unit.pos_target = pos;
					//			unit.h_location = default;
					//			unit.Sync(ent);
					//		}

					//		ref var nameable = ref ent.GetComponent<Nameable.Data>();
					//		if (nameable.IsNotNull())
					//		{
					//			nameable.name = character_asset.GetName();
					//		}
					//	});
					//}
				}
#endif
			}

			[Query(ISystem.Scope.Global)]
			public delegate void GetAllQuery(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, 
				[Source.Owned] in Unit.Data unit, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction);

			public enum Type: byte
			{
				Undefined = 0,

				Character,
				Vehicle
			}

			public enum Action: byte
			{
				None = 0,

				Enter,
				Exit,
				Move,
				Follow,
				Attack,
				Investigate,
				Load,
				Unload
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				[Asset.Ignore] Wants_Repath = 1 << 0,
				Requires_Driver = 1 << 1,
			}


			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
			public partial struct Data(): IComponent
			{
				public Unit.Flags flags;
				public Unit.Type type;
				[Asset.Ignore] public Unit.Action action;

				[Shitcode]
				[Asset.Ignore] public ICharacter.Handle h_character_driver;

				public Road.Type road_type;

				[Asset.Ignore] public Vector2 pos_next;
				[Asset.Ignore] public Vector2 pos_target;
				[Asset.Ignore] public Vector2 dir_last;
				[Asset.Ignore] public Entity ent_target;
				[Asset.Ignore] public float target_interact_distance;

				// speed in km/h
				public float speed = 10.00f;
				[Asset.Ignore] public float speed_current;
				public float acc = 3.00f;

				public float speed_mult_road = 1.00f;
				public float speed_mult_offroad = 0.30f;
				public float speed_mult_forest = 0.30f;
				public float speed_mult_rugged = 0.30f;
				public float speed_mult_water = 0.30f;

				//[Net.Ignore, Save.Ignore] public float target_dist;
				[Net.Ignore, Save.Ignore] public Road.Segment next_segment;
				[Net.Ignore, Save.Ignore] public Road.Segment end_segment;
				[Net.Ignore, Save.Ignore] public int current_branch_index;
				[Net.Ignore, Save.Ignore] public FixedArray32<Road.Junction.Branch> branches;
				[Net.Ignore, Save.Ignore] public int branches_count;
				[Net.Ignore, Save.Ignore] public float t_next_action;
				[Net.Ignore, Save.Ignore] public EntRef<Transform.Data> ref_target_transform;
			}

			public static Entity GetNearest(Vector2 pos, out float dist_sq, IFaction.Handle h_faction = default)
			{
				ref var region = ref World.GetGlobalRegion();
				var dist_sq_current = float.MaxValue;
				var ent_nearest = Entity.None;

				foreach (ref var row in region.IterateQuery<WorldMap.Unit.GetAllQuery>())
				{
					row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Unit.Data unit, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction) =>
					{
						if (!h_faction || h_faction == faction.id)
						{
							var dist_sq_tmp = Vector2.DistanceSquared(pos, transform.position);
							if (dist_sq_tmp < dist_sq_current)
							{
								dist_sq_current = dist_sq_tmp;
								ent_nearest = entity;
							}
						}
					});
				}

				dist_sq = dist_sq_current;
				return ent_nearest;
			}



			public struct ActionRPC: Net.IRPC<Unit.Data>
			{
				public Unit.Action action;
				public Entity ent_target;
				public Vector2 pos_target;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Unit.Data data)
				{
					Assert.Check(data.CanPlayerControlUnit(rpc.entity, rpc.connection.GetPlayerHandle()));

					//App.WriteLine(this.action);

					var ok = true;
					switch (this.action)
					{
						case Action.Move:
						{
							data.pos_target = this.pos_target;
							data.ent_target = default;
							data.flags.AddFlag(Unit.Flags.Wants_Repath);
						}
						break;

						case Action.Enter:
						{
							Assert.Check(this.ent_target.IsAlive());

							ref var enterable = ref this.ent_target.GetComponent<Enterable.Data>();
							Assert.IsNotNull(ref enterable);

							Assert.Check(enterable.mask_units.Has(data.type));

							data.pos_target = this.pos_target;
							data.ent_target = this.ent_target;
							data.target_interact_distance = enterable.radius;
							data.flags.AddFlag(Unit.Flags.Wants_Repath);
						}
						break;

						case Action.Exit:
						{
							data.pos_target = this.pos_target;
							data.ent_target = default;
							data.flags.RemoveFlag(Unit.Flags.Wants_Repath);
						}
						break;

						default:
						{
							ok = false;
						}
						break;
					}

					if (ok)
					{
						data.action = this.action;
						data.t_next_action = 0.00f;
						data.Sync(rpc.entity, true);
					}

					//Sound.PlayGUI(ref connection, "ui.misc.03", volume: 0.35f, pitch: 1.30f);

					//var random = XorRandom.New(true);
					//Sound.PlayGUI(ref connection, random.NextBool(0.50f) ? "phone.in.00" : "phone.out.00", volume: 0.25f, pitch: 1.00f);
				}
#endif
			}

			//[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
			//public static void Update(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			//{

			//}


			[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
			public static void UpdateParented([Source.Owned] ref Transform.Data transform_child, [Source.Parent] in Transform.Data transform_parent, 
			[Source.Owned] in Marker.Data marker)
			{
				transform_child.SetPosition(transform_parent.position + marker.relative_offset);
			}

			[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
			public static void UpdateStored([Source.Owned] ref Transform.Data transform_child, [Source.Stored] in Transform.Data transform_parent, 
			[Source.Owned] in Marker.Data marker)
			{
				transform_child.SetPosition(transform_parent.position + marker.relative_offset);
			}

#if CLIENT
			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region), HasRelation(Source.Modifier.Owned, Relation.Type.Child, false), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, false)]
			public static void UpdateMarker([Source.Owned] in Unit.Data unit, [Source.Owned] ref Marker.Data marker)
			{
				marker.rotation = unit.dir_last.GetAngleRadiansFast();
			}
#endif

#if SERVER
			// TODO: kinda shithack
			[HasRelation(Source.Modifier.Owned, Relation.Type.Child, false), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, false)]
			[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Global, interval: 1.1374f), HasTag("initialized", true, Source.Modifier.Owned)]
			public static void OnUpdateDespawn(ISystem.Info.Global info, Entity entity, ref XorRandom random,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref Despawn.Data despawn, [Source.Owned] ref WorldMap.Unit.Data unit)
			{
				if (info.WorldTime >= despawn.next_update)
				{
					despawn.next_update = info.WorldTime + random.NextFloatExtra(despawn.interval, despawn.interval_extra);

					if (Vec2f.IsInDistance(transform.position, despawn.last_pos, 0.125f) && !unit.h_character_driver) // despawn.last_pos ) //despawn.state_flags.HasAny(Despawn.StateFlags.Idle)) 
					{
						if (despawn.sleep_count >= despawn.sleep_count_max)
						{
							if (Despawn.debug_log) App.WriteLine($"{entity.GetPrefabName()}: despawn delete");

							var ev = new Despawn.DespawnEvent()
							{
								position = transform.position
							};
							ev.Trigger(entity);

							entity.Delete();
						}
						else
						{
							if (Despawn.debug_log) App.WriteLine($"{entity.GetPrefabName()}: despawn progress {despawn.sleep_count}/{despawn.sleep_count_max}");
							despawn.sleep_count++;
							despawn.Sync(entity);
						}
					}
					else
					{
						despawn.last_pos = transform.position;

						if (despawn.sleep_count > 0)
						{
							if (Despawn.debug_log) App.WriteLine($"{entity.GetPrefabName()}: despawn reset");
							despawn.sleep_count = 0;
							despawn.Sync(entity);
						}
					}
				}
			}
#endif

			//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Global)]
			//public static void UpdateTest([Source.Owned] ref Faction.Data faction, [Source.Owned] ref Marker.Data marker)
			//{
			//	ref var faction_data = ref faction.id.GetData();

			//	marker.derp = Asset.GetPointerTest(ref faction_data);				
			//}

			//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Global), HasRelation(Source.Modifier.Owned, Relation.Type.Child, false)]
			//public static void UpdateTest(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Global] ref World.Global world_global, [Source.Owned] ref WorldMap.Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			//{
			//	ref readonly var comp = ref ECS.GetInfo<Gun.Data>();
			//	if (comp.alignment >= 4)
			//	{
			//		unit.speed_current = Maths.Max(10, unit.speed_current); 
			//	}
			//}



			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnUpdateActions(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, 
			[Source.Global] ref World.Global world_global, [Source.Owned] ref WorldMap.Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			{
				var time = info.WorldTime;
				switch (unit.action)
				{
					case Unit.Action.None:
					{
						unit.pos_target = transform.position;
					}
					break;

					case Unit.Action.Move:
					{
						// TODO: Move UpdateMovement() here?
					}
					break;

					case Unit.Action.Enter:
					{
						if (time >= unit.t_next_action)
						{
							ref var transform_target = ref unit.ref_target_transform.GetValueOrNullRef(unit.ent_target, out var target_changed);
							if (transform_target.IsNotNull())
							{
								unit.pos_target = transform_target.position;

#if SERVER
								if (unit.pos_target.IsInRadius(transform.position, unit.target_interact_distance))
								{
									if (Unit.TryEnter(entity, unit.ent_target))
									{
										unit.action = default;
										unit.ent_target = default;
										unit.target_interact_distance = 0.00f;
										unit.Sync(entity);
									}
								}
#endif

								unit.t_next_action = time + 1.00f;
							}
						}
					}
					break;

					case Unit.Action.Exit:
					{
						if (time >= unit.t_next_action)
						{
#if SERVER
							if (Unit.TryExit(entity, unit.pos_target, true))
							{

							}

							unit.action = default;
							unit.ent_target = default;
							unit.target_interact_distance = 0.00f;
							unit.Sync(entity);
#endif

							unit.t_next_action = time + 1.00f;
						}
					}
					break;
				}

#if SERVER
				//region.DrawDebugCircle(transform.position, 0.125f, Color32BGRA.Magenta);
#endif
			}

#if SERVER
			public static bool TryEnter(Entity ent_unit, Entity ent_enterable)
			{
				try
				{
					Assert.Check(ent_enterable != ent_unit);
					Assert.Check(ent_enterable.region_id == ent_unit.region_id);

					Assert.Check(ent_unit.IsAlive());
					Assert.Check(ent_enterable.IsAlive());

					//Assert.Check(!this.ent_enterable.TryGetParent(Relation.Type.Child, out var ent_enterable_parent));

					ref var enterable = ref ent_enterable.GetComponent<WorldMap.Enterable.Data>();
					Assert.IsNotNull(ref enterable);

					Assert.Check(enterable.flags.HasNone(Enterable.Data.Flags.Hide_If_Parented) || !ent_enterable.GetParent(Relation.Type.Child).IsValid());

					ent_unit.ReplaceRelation(ent_enterable, Relation.Type.Stored, true);

					return true;
				}
				catch (Exception e)
				{
					App.WriteException(e);
					return false;
				}
			}

			public static bool TryExit(Entity ent_unit, Vector2? pos_target = null, bool clamp_pos_target = true)
			{
				try
				{
					Assert.Check(ent_unit.IsAlive());

					var ent_enterable = ent_unit.GetParent(Relation.Type.Stored);

					Assert.Check(ent_enterable != ent_unit);
					Assert.Check(ent_enterable.region_id == ent_unit.region_id);

					Assert.Check(ent_enterable.IsAlive());

					ref var enterable = ref ent_enterable.GetComponent<WorldMap.Enterable.Data>();
					Assert.IsNotNull(ref enterable);

					ref var transform = ref ent_enterable.GetComponent<Transform.Data>();
					Assert.IsNotNull(ref transform);

					//var road = WorldMap.GetNearestRoad(location_data.h_prefecture, Road.Type.Road, (Vector2)location_data.point, out var dist_sq);
					//var pos = road.GetPosition().GetRefValueOrDefault();

					//var random = XorRandom.New(true);
					//if (pos == default) pos = (Vector2)location_data.point + random.NextUnitVector2Range(0.25f, 0.50f);

					var pos = transform.position;
					if (pos_target.TryGetValue(out var pos_target_v))
					{
						//App.WriteLine(pos_target_v);
						if (clamp_pos_target)
						{
							pos_target_v = Maths.ClampRadius(pos_target_v, transform.position, enterable.radius);
						}

						pos = pos_target_v;
					}
					else
					{
						var road = WorldMap.GetNearestRoad(Road.Type.Road, pos, out var dist_sq);
						if (road.IsValid())
						{
							var pos_tmp = road.GetNearestPosition(pos, out dist_sq);

							if (dist_sq <= enterable.radius.Pow2())
							{
								pos = pos_tmp;
							}
						}
					}

					ref var region = ref World.GetGlobalRegion();

					//character_data.ent_inside = default;
					//if (ent_unit.IsAlive())
					{
						ent_unit.RemoveRelation(ent_enterable, Relation.Type.Stored);
						//ref var transform_unit = ref ent_unit.GetComponent<Transform.Data>();
						//if (transform_unit.IsNotNull())
						//{
						//	transform_unit.SetPosition(pos);
						//	transform_unit.Sync(ent_unit);
						//}

						region.Schedule((ref region) =>
						{
							ref var transform_unit = ref ent_unit.GetComponent<Transform.Data>();
							if (transform_unit.IsNotNull())
							{
								transform_unit.SetPosition(pos);
								transform_unit.Sync(ent_unit);
							}
						});
					}
					return true;
				}
				catch (Exception e)
				{
					App.WriteException(e);
					return false;
				}
			}
#endif

			// TODO: doesn't trigger when jumping between two enterables at the same time (e.g. as character from vehicle to region entrance)
			[Shitcode]
			[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnUnitEnter(ISystem.Info.Global info, ref Region.Data.Global region,
			Entity ent_unit_parent, Entity ent_unit_child, Entity ent_enterable,
			[Source.Stored] ref WorldMap.Enterable.Data enterable,
			[Source.Stored, Optional(true)] ref WorldMap.Unit.Data unit_parent, [Source.Owned] ref WorldMap.Unit.Data unit_child)
			{
				App.WriteLine($"OnUnitEnter() {info.EventType}");
				//App.WriteValue(ent_unit_parent);
				//App.WriteValue(ent_unit_child);
				//App.WriteValue(ent_enterable);

				if (unit_child.type == WorldMap.Unit.Type.Character && ent_unit_child.TryGetAsset(out ICharacter.Definition character_asset))
				{
					switch (info.EventType)
					{
						case ISystem.EventType.Add:
						{
							if (unit_parent.IsNotNull())
							{
#if SERVER
								unit_parent.h_character_driver = character_asset;
								unit_parent.Sync(ent_unit_parent, true);
#endif

#if CLIENT
								// TODO: hack
								if (ent_unit_child == WorldMap.interacted_entity_cached || (WorldMap.hs_selected_entities.Count == 1 && WorldMap.hs_selected_entities.Contains(ent_unit_child)))
								{
									WorldMap.hs_selected_entities.Add(ent_unit_parent);
									if (!WorldMap.interacted_entity_cached.IsAlive()) WorldMap.SelectEntity(ent_unit_parent, open_widget: false);
								}
#endif
							}
							else
							{
#if CLIENT
								// TODO: hack
								if (ent_enterable == WorldMap.interacted_entity_cached || (ent_unit_child == WorldMap.interacted_entity_cached || (WorldMap.hs_selected_entities.Count == 1 && WorldMap.hs_selected_entities.Contains(ent_unit_child))))
								{
									WorldMap.FocusEntity(ent_enterable, interact: true, open_widget: false);
								}
#endif
							}
						}
						break;

						case ISystem.EventType.Remove:
						{
							if (unit_parent.IsNotNull())
							{
#if SERVER
								unit_parent.h_character_driver = default;
								unit_parent.Sync(ent_unit_parent, true);
#endif

#if CLIENT
								// TODO: hack
								if (ent_unit_child == WorldMap.interacted_entity_cached || (WorldMap.hs_selected_entities.Contains(ent_unit_child) && (WorldMap.hs_selected_entities.Count == 2 ? WorldMap.hs_selected_entities.Contains(ent_unit_parent) : WorldMap.hs_selected_entities.Count == 1)))
								{
									WorldMap.hs_selected_entities.Remove(ent_unit_parent);
									//if (WorldMap.selected_entity_cached == ent_unit_parent) WorldMap.SelectEntity(default);
								}
#endif
							}
							else
							{
#if CLIENT
								// TODO: hack
								if (ent_enterable == WorldMap.interacted_entity_cached || (WorldMap.hs_selected_entities.Contains(ent_unit_child) && WorldMap.hs_selected_entities.Count == 1))
								{
									WorldMap.FocusEntity(ent_unit_child, interact: true, open_widget: false);
								}
#endif
							}
						}
						break;
					}
				}
			}

			// TODO: probably make this serverside + skip if not moving
			[Shitcode]
			[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Global), HasRelation(Source.Modifier.Owned, Relation.Type.Child, false), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, false)]
			public static void OnUpdateMovement(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, 
			[Source.Global] ref World.Global world_global, [Source.Owned] ref WorldMap.Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			{
				var dt = App.fixed_update_interval_s;

				//App.WriteLine("unit");

				if (unit.flags.TrySetFlag(Unit.Flags.Wants_Repath, false))
				{
					unit.current_branch_index = 0;
					unit.branches_count = 0;

					var branches_span = unit.branches.AsSpan();
					if (Repath(unit.road_type, transform.position, unit.pos_target, out var pos_end, ref unit.next_segment, ref unit.end_segment, ref branches_span))
					{
						//var branch = branches_span[0];

						unit.branches_count = branches_span.Length;
						unit.pos_next = unit.next_segment.GetPosition();
					}
					else
					{
						unit.pos_next = unit.pos_target;
					}
				}

				var speed_mult = 1.00f;

				const float s_to_h = 1.00f / 60.00f / 60.00f;
				var time_scale = world_global.speed;

				if (unit.next_segment.IsValid())
				{
					GetNextSegment(transform.position, ref unit.next_segment, ref unit.end_segment, ref unit.current_branch_index, unit.branches.Slice(unit.branches_count));

					if (!unit.next_segment.IsValid())
					{
						unit.pos_next = unit.pos_target;
					}
					else
					{
						ref var road = ref unit.next_segment.GetRoad();
						if (road.IsNotNull())
						{
							speed_mult *= road.speed_mult * road.integrity;
						}

						unit.pos_next = unit.next_segment.GetPosition();
					}
				}
				else
				{
					unit.pos_next = unit.pos_target;
				}

				var pos_target = unit.pos_next;
				var dir = (pos_target - transform.position).GetNormalized(out var dist);

				speed_mult *= Maths.Clamp(dist * 4.50f, 0.20f, 1.00f);

				unit.speed_current.MoveTowards(Maths.Min(unit.speed, unit.speed * 0.90f * speed_mult), unit.acc * dt * time_scale);
				//unit.target_dist = dist;
				if (dist > 0.01f) unit.dir_last = dir;

				transform.position += (dir * Maths.Min(dist, ((unit.speed_current * dt * s_to_h) * time_scale)) * WorldMap.km_per_unit_inv);
			}

			public static void GetNextSegment(Vector2 pos_current, ref Road.Segment segment, ref Road.Segment end_segment, ref int current_branch_index, Span<Road.Junction.Branch> branches)
			{
				//var dist_sq = Vector2.DistanceSquared(pos_current, segment.GetPosition());
				if (Maths.IsInDistance(pos_current, segment.GetPosition(), 0.10f))
				{
					if (segment == end_segment && end_segment.IsValid())
					{
						segment = default;
						//App.WriteLine("done");
					}
					else if (current_branch_index < branches.Length)
					{
						//if (current_branch_index < branches.Length - 1 && WorldMap.road_segment_to_junction_index.TryGetValue(segment, out var junction_index) && junction_index == branches[current_branch_index + 1].junction_index)
						if (current_branch_index < branches.Length - 1 && Maths.IsInDistance(pos_current, WorldMap.road_junctions[branches[current_branch_index + 1].junction_index].pos, 0.25f))
						{
							current_branch_index++;
							segment = branches[current_branch_index].GetSegment();
							//segment.index = (byte)(segment.index + branches[current_branch_index].sign);
							//App.WriteLine(branches[current_branch_index].sign);
						}
						else
						{
							segment.index = (byte)(segment.index + branches[current_branch_index].sign);
						}
					}
				}
			}

			[Shitcode]
			public static bool Repath(Road.Type road_type, Vector2 pos_a, Vector2 pos_b, out Vector2 pos_end, ref Road.Segment segment_start, ref Road.Segment segment_end, ref Span<Road.Junction.Branch> branches_span)
			{
				if (!enable_pathfinding)
				{
					pos_end = pos_b;
					return false;
				}

				//var dir = (pos_b - pos_a).GetNormalizedFast();
				pos_end = pos_b;
				if (road_type == Road.Type.Undefined) return false;

				segment_start = default;
				segment_end = default;

				var road_a = WorldMap.GetNearestRoad(road_type, pos_a, out var road_a_dist_sq);
				var road_b = WorldMap.GetNearestRoad(road_type, pos_b, out var road_b_dist_sq);

				if (road_a.IsValid() && road_b.IsValid() && road_a != road_b && road_a_dist_sq < 0.50f.Pow2() && road_b_dist_sq < 1.00f.Pow2())
				{

					//#if CLIENT
					//					if (road_a.IsValid()) World.GetGlobalRegion().DrawDebugCircle(road_a.GetPosition(), 0.125f, Color32BGRA.Magenta, filled: true);
					//					if (road_b.IsValid()) World.GetGlobalRegion().DrawDebugCircle(road_b.GetPosition(), 0.125f, Color32BGRA.Magenta, filled: true);
					//#endif


					//var sign_a = road_a.GetSign(dir, true, 0.00f, 1.00f);
					//var sign_b = road_b.GetSign(-dir, true, 0.00f, 1.00f);

					//var ok_a = road_a.chain.GetNearestSegment(pos_b).TryGetNearestJunction(out var junction_index_a, out _, out var sign_a);
					var ok_a = road_a.TryGetNearestJunction(out var junction_index_a, out _, out var sign_a);
					var ok_b = road_b.TryGetNearestJunction(out var junction_index_b, out _, out var sign_b);



					if (ok_a && ok_b)
					{
						var junction_a = WorldMap.road_junctions[junction_index_a];
						var junction_b = WorldMap.road_junctions[junction_index_b];

						//#if CLIENT
						//						World.GetGlobalRegion().DrawDebugCircle(junction_a.pos, 0.125f, Color32BGRA.Yellow, filled: true);
						//						World.GetGlobalRegion().DrawDebugCircle(junction_b.pos, 0.125f, Color32BGRA.Yellow, filled: true);
						//#endif


						//junction_a.TryResolveBranch((pos_b - junction_a.pos).GetNormalizedFast(), out var branch_src);
						//junction_b.TryResolveBranch(dir, out var branch_dst);



						var dir = (pos_b - pos_a).GetNormalizedFast();
						var dir_a = (pos_a - junction_a.pos).GetNormalizedFast();
						//var dir_a = (junction_a.pos - pos_a).GetNormalizedFast();
						var dir_a2 = (junction_a.pos - pos_b).GetNormalizedFast();
						var dir_ab = (junction_a.pos - junction_b.pos).GetNormalizedFast();
						//var dir_b = dir; // (pos_b - junction_b.pos).GetNormalizedFast();
						//var dir_b2 = (junction_b.pos - junction_a.pos).GetNormalizedFast();
						var dir_b = (pos_b - junction_b.pos).GetNormalizedFast();
						var dir_b2 = (pos_b - junction_b.pos).GetNormalizedFast();


						//#if CLIENT
						//						ref var region = ref World.GetGlobalRegion();
						//						region.DrawDebugCircle(junction_a.pos, 0.50f, Color32BGRA.Red.WithAlphaMult(0.250f), filled: true);
						//						region.DrawDebugCircle(junction_b.pos, 0.50f, Color32BGRA.Green.WithAlphaMult(0.250f), filled: true);

						//						region.DrawDebugDir(junction_a.pos, dir_a * 4, thickness: 10, color: Color32BGRA.Red.WithAlphaMult(0.250f));
						//						region.DrawDebugDir(junction_b.pos, dir_b * 4, thickness: 10, color: Color32BGRA.Green.WithAlphaMult(0.250f));
						//#endif

						//if ((junction_a.TryResolveBranch(dir_a, out var branch_src)) && (road_b.TryGetNearestBranch(out var branch_dst) || junction_b.TryResolveBranch((pos_b - junction_b.pos).GetNormalizedFast(), out branch_dst)))
						////if ((road_a.TryGetNearestBranch(out var branch_src) || junction_a.TryResolveBranch(dir_a, out branch_src)) && (road_b.TryGetNearestBranch(out var branch_dst) || junction_b.TryResolveBranch((pos_b - junction_b.pos).GetNormalizedFast(), out branch_dst)))
						//if ((road_a.TryGetEntryBranch(dir_a, out var branch_src)) 
						//	&& (road_b.TryGetEntryBranch(dir_b, out var branch_dst) || road_b.TryGetExitBranch(dir_b, out branch_dst)))




						if ((junction_a.TryResolveBranch(dir, out var branch_src)
							|| road_a.TryGetEntryBranch(dir_a, out branch_src, dot_min: 0.01f, dot_max: 0.99f)
							|| road_a.TryGetExitBranch(dir_a, out branch_src, dot_min: 0.01f, dot_max: 0.99f))
							&& (junction_b.TryResolveBranch(dir_b, out var branch_dst)
							|| road_b.TryGetEntryBranch(dir_b, out branch_dst, dot_min: 0.01f, dot_max: 0.99f)
							|| road_b.TryGetExitBranch(dir_b, out branch_dst, dot_min: 0.01f, dot_max: 0.99f)
							|| junction_b.TryResolveBranch(-dir, out branch_dst)))




						// || road_b.TryGetEntryBranch(-dir, out branch_dst, dot_min: 0.00f))) // || junction_b.TryResolveBranch(dir, out branch_dst)))																																																																		   //if ((road_a.TryGetEntryBranch(dir, out var branch_src, dot_min: 0.00f, dot_max: 1.00f)) && (road_b.TryGetEntryBranch(dir, out var branch_dst, dot_min: 0.00f, dot_max: 1.00f) || junction_b.TryResolveBranch(dir_b, out branch_dst)))
						//																																																														   //if ((road_a.TryGetEntryBranch(dir, out var branch_src, dot_min: 0.00f) || junction_a.TryResolveBranch(dir_a, out branch_src)) && (road_b.TryGetExitBranch(dir_b, out var branch_dst, dot_min: 0.00f) || junction_b.TryResolveBranch(dir, out branch_dst))) // || road_b.TryGetEntryBranch(-dir, out branch_dst, dot_min: 0.00f))) // || junction_b.TryResolveBranch(dir, out branch_dst)))																																																																		   //if ((road_a.TryGetEntryBranch(dir, out var branch_src, dot_min: 0.00f, dot_max: 1.00f)) && (road_b.TryGetEntryBranch(dir, out var branch_dst, dot_min: 0.00f, dot_max: 1.00f) || junction_b.TryResolveBranch(dir_b, out branch_dst)))
						//if (road_a.TryGetNearestBranch(out var branch_src) && road_b.TryGetNearestBranch(out var branch_dst))
						{
							//App.WriteLine(WorldMap.GetJunction(branch_src.junction_index).segments_count);

							//TryResolveBranch(junction_b, dir_b, out var branch_dst);

							if (RoadNav.Astar.TryFindPath(branch_src, branch_dst, ref branches_span, ignore_limits: true, dot_min: -0.90f, dot_max: 1.00f))
							{
								//segment_start = branch_src.GetSegment();
								segment_start = branches_span[0].GetSegment();
								segment_start = segment_start.chain.GetNearestSegment(pos_a);

								//segment_end = road_b;

								//segment_start = road_a;
								//segment_end = road_b;

								//segment_end = branch_dst.GetSegment();
								//segment_end = branches_span[branches_span.Length - 1].GetSegment();
								segment_end = branches_span[branches_span.Length - 1].GetNearestSegment(pos_b, out pos_end);
								//segment_end = segment_end.chain.GetNearestSegment(pos_b);

								//segment_end.chain.GetNearestSegment

								//pos_end = Maths.ClosestPointOnLine(segment_end.GetPosition(), )

								return true;
							}
						}
					}
				}

				return false;
			}

			public static bool enable_pathfinding = false;

#if CLIENT
			public partial struct UnitGUI: IGUICommand
			{
				public Entity ent_unit;
				public Unit.Data unit;
				public Transform.Data transform;
				public bool has_parent;

				//public static Vector2? mouse_drag_a;
				//public static Vector2? mouse_drag_b;
				//public static AABB? mouse_drag_rect;
				//public static bool is_mouse_dragging;

				[Shitcode]
				public void Draw()
				{
					//using (var window = GUI.Window.InteractionMisc("unit"u8, this.ent_unit, size: new(0, 0)))
					//using (var window = GUI.Window.Interaction("unit"u8, this.ent_unit))
					{
						//this.StoreCurrentWindowTypeID();
						//if (window.show)
						{
							ref var region = ref this.ent_unit.GetRegionCommon();
							static void DrawPath(ref Region.Data.Common region, Road.Segment segment_start, Road.Segment segment_end, Vector2 pos_end, Span<Road.Junction.Branch> branches_span, out float distance, Color32BGRA color = default, float thickness = 0.250f)
							{
								distance = 0.00f;

								var pos_current = segment_start.GetPosition();
								var segment_current = segment_start;
								var current_branch_index = 0;

								var scale = region.GetWorldToCanvasScale();
								if (color == 0) color = Color32BGRA.Yellow.WithAlphaMult(0.250f);

								var is_valid = segment_current.IsValid();
								for (var i = 0; i < 200; i++)
								{
									var pos_a = pos_current;
									var pos_b = pos_current = segment_current.GetPosition();

									distance += Vector2.Distance(pos_a, pos_b);

									GetNextSegment(pos_current, ref segment_current, ref segment_end, ref current_branch_index, branches_span);
									//if (current_branch_index == branches_span.Length - 1)
									//{
									//GUI.DrawCircleFilled(region.WorldToCanvas(pos_b), 0.125f * scale, color: Color32BGRA.Magenta.WithAlpha(100), segments: 4, layer: GUI.Layer.Foreground);
									//GUI.DrawTextCentered($"[{i}]", region.WorldToCanvas(pos_b), color: Color32BGRA.White, layer: GUI.Layer.Foreground);
									//}

									is_valid = segment_current.IsValid();



									if (is_valid)
									{
										GUI.DrawLine(region.WorldToCanvas(pos_a), region.WorldToCanvas(pos_b), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);

										//GUI.DrawLine(region.WorldToCanvas(pos_a), region.WorldToCanvas(pos_b), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);
									}
									else
									{
										//var pos_end_line = Maths.ClosestPointOnLine(pos_a, pos_b, pos_end);

										////Maths.ClosestPointOnLine(pos_a, pos_b, pos_end);

										////GUI.DrawLine(region.WorldToCanvas(pos_a), region.WorldToCanvas(pos_b), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);
										//GUI.DrawLine(region.WorldToCanvas(pos_a), region.WorldToCanvas(pos_end_line), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);
										//GUI.DrawLine(region.WorldToCanvas(pos_end_line), region.WorldToCanvas(pos_end), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);
										GUI.DrawLine(region.WorldToCanvas(pos_a), region.WorldToCanvas(pos_end), color: color, thickness: thickness * scale, layer: GUI.Layer.Foreground);

										//GUI.DrawCircleFilled(region.WorldToCanvas(pos_end_line), 0.125f * scale, color: Color32BGRA.Magenta.WithAlpha(100), segments: 4, layer: GUI.Layer.Foreground);
										break;
									}
								}
							}

							//using (GUI.Group.New(new(GUI.AvX, 0), padding: new(4)))
							{
								var mouse = GUI.GetMouse();

								ref var world_global = ref region.GetGlobalComponent<World.Global>();
								//var time_scale = world_global.speed;
								var speed_km_h = this.unit.speed_current;

								//GUI.LabelShaded("Speed:", $"{speed_km_h:0.00} km/h");
								//GUI.Text($"{time_scale:0.00}");

								//App.WriteLine("h");
								//if (false)
								//{
								//	var distance_sq = 0.00f;
								//	var ent_enterable = this.has_parent ? this.ent_unit.GetParent(Relation.Type.Child) : WorldMap.Enterable.GetNearest(this.transform.position, out distance_sq, ent_exclude: this.ent_unit);

								//	//GUI.Text($"{ent_enterable}");

								//	if (ent_enterable.IsAlive() && ent_enterable != this.ent_unit) // && !ent_enterable.TryGetParent(Relation.Type.Child, out var ent_enterable_parent))
								//	{
								//		ref var enterable = ref ent_enterable.GetComponent<Enterable.Data>();
								//		if (enterable.IsNotNull())
								//		{
								//			//var can_enter = distance_sq <= enterable.radius.Pow2();

								//			ent_enterable.TryGetAssetHandle(out ILocation.Handle h_location_enterable);

								//			GUI.TitleCentered(ent_enterable.GetName(), size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4), color: GUI.font_color_disabled);
								//			//if (GUI.Selectable3(ent_enterable, GUI.GetLastItemRect(), selected: WorldMap.h_selected_location == h_location_nearest))
								//			if (GUI.Selectable3("unit.enterable.current"u8, GUI.GetLastItemRect(), selected: WorldMap.selected_entity == ent_enterable))
								//			{
								//				//WorldMap.h_selected_location.Toggle(h_location_nearest);
								//				//if (WorldMap.h_selected_location != default) WorldMap.FocusLocation(h_location_nearest);

								//				WorldMap.selected_entity.Toggle(ent_enterable);
								//				if (h_location_enterable.IsValid()) WorldMap.h_selected_location.Toggle(h_location_enterable);
								//				if (WorldMap.selected_entity == ent_enterable) WorldMap.FocusEntity(ent_enterable);
								//			}
								//			//GUI.FocusableAsset(h_location_nearest);

								//			if (this.has_parent)
								//			{
								//				GUI.TitleCentered("[Exit]"u8, size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: Color32BGRA.Red);
								//				if (GUI.Selectable3("exit"u8, GUI.GetLastItemRect(), selected: false))
								//				{
								//					var rpc = new WorldMap.Unit.ExitRPC()
								//					{
								//						//h_character = h_character,
								//					};
								//					rpc.Send(this.ent_unit);
								//				}
								//			}
								//			else
								//			{
								//				var can_enter = distance_sq <= enterable.radius.Pow2();
								//				if (can_enter)
								//				{
								//					GUI.TitleCentered("[Enter]"u8, size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: Color32BGRA.Green);
								//					if (GUI.Selectable3("unit.enter"u8, GUI.GetLastItemRect(), selected: false))
								//					{
								//						var rpc = new WorldMap.Unit.EnterRPC()
								//						{
								//							ent_enterable = ent_enterable
								//							//h_character = h_character,
								//							//h_location = h_location_nearest
								//						};
								//						rpc.Send(this.ent_unit);
								//					}
								//				}
								//				else
								//				{
								//					GUI.TitleCentered($"{distance_sq.Sqrt() * WorldMap.km_per_unit:0.00} km", size: 16, pivot: new(1.00f, 1.00f), offset: new(-4, -4), color: GUI.font_color_disabled);
								//					//GUI.TitleCentered("Wilderness", size: 16, pivot: new(0.00f, 1.00f), offset: new(4, -4), color: GUI.font_color_green_b.WithAlphaMult(0.50f));
								//				}
								//			}
								//		}
								//	}
								//}

								//if (GUI.DrawButton("Enter"))

								//GUI.LabelShaded("Speed:", $"{speed_km_h:0.00} km/h");
								//GUI.Text($"{time_scale:0.00}");

								var scale = region.GetWorldToCanvasScale();

								var pos = GUI.GetMousePosition(); // mouse.GetInterpolatedPosition();
								var pos_w = region.CanvasToWorld(pos);
								pos_w.Snap(1.00f / 32.00f, out var pos_w_snapped);

								//var road = WorldMap.GetNearestRoad(Road.Type.Road, pos_w, out var road_dist_sq);

								var pos_c_hover = region.WorldToCanvas(pos_w_snapped);

								var pos_c_current = region.WorldToCanvas(this.transform.GetInterpolatedPosition());
								var pos_c_target = region.WorldToCanvas(this.unit.pos_target);
								var pos_c_next = region.WorldToCanvas(this.unit.pos_next);

								var dir = (pos_w - this.transform.GetInterpolatedPosition()).GetNormalized(out var dist);

								//GUI.Text($"{ent_unit}; {pos:0.00} {pos_w:0.00}; {pos_c:0.00}");

								if (!this.has_parent)
								{
									GUI.DrawLine(pos_c_current, pos_c_next, Color32BGRA.Green.WithAlphaMult(0.25f), thickness: 0.125f * scale * 0.25f, GUI.Layer.Foreground);
								}

								//GUI.DrawCircle(pos_c_current, 0.750f * scale, Color32BGRA.Green, segments: 16, layer: GUI.Layer.Foreground);

								if (!this.has_parent)
								{
									GUI.DrawCircleFilled(pos_c_next, 0.1250f * scale * 0.50f, Color32BGRA.Green.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);
									GUI.DrawCircleFilled(pos_c_target, 0.1250f * scale * 0.50f, Color32BGRA.Green.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);
								}

								//var is_mouse_dragging_cached = is_mouse_dragging;
								//is_mouse_dragging = mouse.GetKeyNow(Mouse.Key.Left);

								//if (is_mouse_dragging)
								//{
								//	mouse_drag_b = pos_w_snapped;
								//}
								//else if (is_mouse_dragging_cached)
								//{
								//	mouse_drag_a = null;
								//	mouse_drag_b = null;
								//	mouse_drag_rect = null;
								//}

								//if (is_mouse_dragging && mouse_drag_a.HasValue && mouse_drag_b.HasValue)
								//{
								//	mouse_drag_rect = new AABB(mouse_drag_a.Value, mouse_drag_b.Value);

								//	if (mouse_drag_rect.TryGetValue(out var rect))
								//	{
								//		var rect_c = region.WorldToCanvas(rect);

								//		GUI.DrawRectFilled(rect_c, color: Color32BGRA.Yellow.WithAlphaMult(0.10f), layer: GUI.Layer.Foreground);
								//		GUI.DrawRect(rect_c, color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground);
								//	}
								//}

								//ref var current_branch = ref unit.branches[unit.current_branch_index];
								//if (current_branch.sign != 0)
								//{
								//	GUI.DrawCircleFilled(region.WorldToCanvas(WorldMap.road_junctions[current_branch.junction_index].pos), 0.5f * scale * 0.50f, Color32BGRA.Orange.WithAlphaMult(0.50f), segments: 16, layer: GUI.Layer.Foreground);
								//}

								//GUI.Text($"{unit.current_branch_index}/{unit.branches_count}");
								//if (unit.next_segment.IsValid())
								//{
								//	GUI.DrawCircleFilled(region.WorldToCanvas(unit.next_segment.GetPosition()), 0.5f * scale * 0.50f, Color32BGRA.Cyan.WithAlphaMult(0.50f), segments: 16, layer: GUI.Layer.Foreground);
								//	GUI.Text($"{unit.next_segment.index}; {unit.next_segment.GetPosition()}");
								//}

								var path_distance = dist;

								if (this.unit.branches_count > 0 && this.unit.next_segment.IsValid() && this.unit.end_segment.IsValid())
								{
									var road_a = this.unit.next_segment; // branches[0].GetSegment();
									var road_b = this.unit.end_segment;

									DrawPath(ref region, road_a, road_b, this.unit.pos_target, this.unit.branches.AsSpan().Slice(this.unit.current_branch_index, this.unit.branches_count - this.unit.current_branch_index), color: GUI.font_color_green.WithAlphaMult(0.40f), thickness: 0.125f, distance: out path_distance);
								}

								//WorldMap.DrawBranch(ref current_branch);

								if (WorldMap.IsHovered())
								{
									//if (mouse.GetKeyDown(Mouse.Key.Left))
									//{
									//	is_mouse_dragging = true;

									//	mouse_drag_a = pos_w_snapped;
									//}

									if (!this.has_parent)
									{
										if (this.unit.road_type != Road.Type.Undefined)
										{
											var road_a = default(Road.Segment);
											var road_b = default(Road.Segment);

											var ts = Timestamp.Now();
											Span<Road.Junction.Branch> branches_span = stackalloc Road.Junction.Branch[32];
											if (Repath(this.unit.road_type, this.transform.position, pos_w_snapped, out var pos_end, ref road_a, ref road_b, ref branches_span))
											{
												var ts_elapsed = ts.GetMilliseconds();
												//GUI.Text($"{ts_elapsed:0.0000} ms");

												DrawPath(ref region, road_a, road_b, pos_end, branches_span, color: GUI.col_button_yellow.WithAlphaMult(0.20f), thickness: 0.125f, distance: out path_distance);

												//foreach (ref var branch in branches_span)
												//{
												//	WorldMap.DrawBranch(ref branch);
												//}
											}
										}

										GUI.DrawLine(pos_c_current, pos_c_current + (dir * 100), layer: GUI.Layer.Foreground);
										GUI.DrawLine(pos_c_current, pos_c_hover, Color32BGRA.Yellow.WithAlphaMult(0.25f), thickness: 0.125f * scale * 0.25f, GUI.Layer.Foreground);

										//GUI.DrawTextCentered($"{dist * WorldMap.km_per_unit:0.00} km", (pos_c_current + pos_c_hover) * 0.50f, layer: GUI.Layer.Foreground, box_shadow: true);

										GUI.DrawCircleFilled(pos_c_hover, 0.125f * scale * 0.50f, Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);

										GUI.DrawTextCentered($"{path_distance * WorldMap.km_per_unit:0.00} km", pos_c_hover - ((pos_c_hover - pos_c_current).GetNormalized() * 0.50f * scale), layer: GUI.Layer.Foreground, box_shadow: true);
									}

									//if (!this.has_parent)
									//{
									//	if (mouse.GetKeyDown(Mouse.Key.Right))
									//	{
									//		var rpc = new Unit.MoveRPC();
									//		rpc.pos_target = pos_w_snapped;
									//		rpc.Send(this.ent_unit);
									//	}
									//}
								}
							}
						}
						//else
						//{
						//	is_mouse_dragging = false;
						//	mouse_drag_rect = null;
						//	mouse_drag_a = null;
						//	mouse_drag_b = null;
						//}
					}
				}
			}

			[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform,
			[HasRelation(Source.Modifier.Owned, Relation.Type.Stored, true)] bool has_parent)
			{
				//return;

				if (WorldMap.IsOpen && WorldMap.interacted_entity_cached == entity && unit.CanPlayerControlUnit(entity, Client.GetPlayerHandle()))
				{
					var gui = new Unit.UnitGUI()
					{
						ent_unit = entity,
						unit = unit,
						transform = transform,
						has_parent = has_parent
					};
					gui.Submit();
				}
			}
#endif
		}
	}
}

