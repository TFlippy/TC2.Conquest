
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

			// speed in km/h
			public float speed = 6.00f;

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
				var pos = (Vector2)location_data.point + random.NextUnitVector2Range(0.25f, 0.50f);

				region.SpawnPrefab("unit.car", position: pos, faction_id: character_data.faction, entity: ent_asset).ContinueWith((ent) =>
				{
					ref var unit = ref ent.GetComponent<Unit.Data>();
					if (unit.IsNotNull())
					{
						unit.pos_target = pos;
						unit.h_location = default;
					}

					ref var nameable = ref ent.GetComponent<Nameable.Data>();
					if (nameable.IsNotNull())
					{
						nameable.name = character_asset.GetName();
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
			const float s_to_h = 1.00f / 60.00f;

			ref var world_global = ref region.GetGlobalComponent<World.Global>();
			var time_scale = world_global.speed;

			var dir = (unit.pos_target - transform.position).GetNormalized(out var dist);
			transform.position += (dir * Maths.Min(dist, ((unit.speed * info.DeltaTime * s_to_h) * time_scale)) / WorldMap.km_per_unit);
		}

#if CLIENT
		public partial struct UnitGUI: IGUICommand
		{
			public Entity ent_unit;
			public Unit.Data unit;
			public Transform.Data transform;

			public static Vector2? mouse_drag_a;
			public static Vector2? mouse_drag_b;
			public static AABB? mouse_drag_rect;
			public static bool is_mouse_dragging;

			public void Draw()
			{
				ref var region = ref this.ent_unit.GetRegionCommon();

				using (var window = GUI.Window.Interaction("unit", this.ent_unit))
				{
					if (window.show)
					{
						using (GUI.Group.New(GUI.Rm, padding: new(4)))
						{

							var mouse = GUI.GetMouse();

							ref var world_global = ref region.GetGlobalComponent<World.Global>();
							var time_scale = world_global.speed;
							var irl_seconds_to_hour = 60.00f / time_scale;
							var speed_km_h = this.unit.speed;

							GUI.LabelShaded("Speed:", $"{speed_km_h:0.00} km/h");
							//GUI.Text($"{time_scale:0.00}");

							var scale = region.GetWorldToCanvasScale();

							var pos = GUI.GetMousePosition(); // mouse.GetInterpolatedPosition();
							var pos_w = region.CanvasToWorld(pos);
							pos_w.Snap(1.00f / 32.00f, out var pos_w_snapped);

							var pos_c = region.WorldToCanvas(pos_w_snapped);

							var pos_c_current = region.WorldToCanvas(this.transform.GetInterpolatedPosition());
							var pos_c_target = region.WorldToCanvas(this.unit.pos_target);

							//GUI.Text($"{ent_unit}; {pos:0.00} {pos_w:0.00}; {pos_c:0.00}");

							GUI.DrawCircle(pos_c_current, 0.750f * region.GetWorldToCanvasScale(), Color32BGRA.Yellow, segments: 16, layer: GUI.Layer.Foreground);
							GUI.DrawCircleFilled(pos_c_target, 0.1250f * region.GetWorldToCanvasScale(), Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);

							var is_mouse_dragging_cached = is_mouse_dragging;
							is_mouse_dragging = mouse.GetKeyNow(Mouse.Key.Left);

							if (is_mouse_dragging)
							{
								mouse_drag_b = pos_w_snapped;					
							}
							else if (is_mouse_dragging_cached)
							{
								mouse_drag_a = null;
								mouse_drag_b = null;
								mouse_drag_rect = null;
							}

							if (mouse_drag_a.HasValue && mouse_drag_b.HasValue)
							{
								mouse_drag_rect = new AABB(mouse_drag_a.Value, mouse_drag_b.Value);
							}

							if (mouse_drag_rect.TryGetValue(out var rect))
							{
								var rect_c = region.WorldToCanvas(rect);

								GUI.DrawRectFilled(rect_c, color: Color32BGRA.Yellow.WithAlphaMult(0.10f), layer: GUI.Layer.Foreground);
								GUI.DrawRect(rect_c, color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground);
							}

							if (WorldMap.IsHovered())
							{
								if (mouse.GetKeyDown(Mouse.Key.Left))
								{
									is_mouse_dragging = true;

									mouse_drag_a = pos_w_snapped;		
								}

								GUI.DrawCircle(pos_c, 0.250f * region.GetWorldToCanvasScale(), Color32BGRA.Green, segments: 4, layer: GUI.Layer.Foreground);


								//var nearest_location_a = WorldMap.GetNearestLocation(transform.position, out var distance_sq_a);
								//var nearest_location_b = WorldMap.GetNearestLocation(pos_w_snapped, out var distance_sq_b);

								//ref var nearest_location_a_data = ref nearest_location_a.GetData();
								//ref var nearest_location_b_data = ref nearest_location_b.GetData();

								//if (nearest_location_a_data.IsNotNull() && nearest_location_b_data.IsNotNull())
								//{
								//	Span<Road.Junction.Branch> branches_span = stackalloc Road.Junction.Branch[32];

								//	//var road = WorldMap.location_to_road[nearest_location];
								//	var road_a = WorldMap.GetNearestRoad(nearest_location_a_data.h_prefecture, Road.Type.Road, transform.position, out var dist_road_a_sq);
								//	var road_b = WorldMap.GetNearestRoad(nearest_location_b_data.h_prefecture, Road.Type.Road, pos_w_snapped, out var dist_road_b_sq);

								//	GUI.DrawCircle(region.WorldToCanvas(road_a.GetPosition()), 0.250f * region.GetWorldToCanvasScale(), Color32BGRA.Magenta, segments: 4, layer: GUI.Layer.Foreground);
								//	GUI.DrawCircle(region.WorldToCanvas(road_b.GetPosition()), 0.250f * region.GetWorldToCanvasScale(), Color32BGRA.Magenta, segments: 4, layer: GUI.Layer.Foreground);


								//	//								WorldMap.TryGetNextJunction(road_a, -1, out var junction_index_a, out var segment_a_b, out var segment_a_c);
								//	//								WorldMap.TryGetNextJunction(road_b, 1, out var junction_index_b, out var segment_b_b, out var segment_b_c);

								//	//								var target = pos_w_snapped;
								//	//								var branch_src = new Road.Junction.Branch((ushort)junction_index_a, segment_a_b.index, -1);
								//	//								var branch_dst = new Road.Junction.Branch((ushort)junction_index_b, segment_b_b.index, 1);

								//	//								if (RoadNav.Astar.TryFindPath(branch_src, branch_dst, ref branches_span))
								//	//								{
								//	//#if CLIENT
								//	//									foreach (ref var branch in branches_span)
								//	//									{
								//	//										if (branch.sign != 0)
								//	//										{
								//	//											WorldMap.DrawBranch(ref branch);
								//	//										}
								//	//									}
								//	//#endif
								//	//								}
								//}


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

