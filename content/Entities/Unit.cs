
namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public static partial class Enterable
		{
			[Query(ISystem.Scope.Global)]
			public delegate void GetAllQuery(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Enterable.Data enterable, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction);

			[IComponent.Data(Net.SendType.Reliable)]
			public partial struct Data: IComponent
			{
				public enum Type: uint
				{
					Undefined = 0,

					Vehicle,
					Location,
				}

				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public Enterable.Data.Type type;
				public Enterable.Data.Flags flags;

				public BitField<Unit.Data.Type> mask_units;

				public float radius = 0.50f;

				public Data()
				{
				}
			}

			public static Entity GetNearest(Vector2 pos, out float dist_sq, IFaction.Handle h_faction = default)
			{
				ref var region = ref World.GetGlobalRegion();
				var dist_sq_current = float.MaxValue;
				var ent_nearest = Entity.None;

				foreach (ref var row in region.IterateQuery<WorldMap.Enterable.GetAllQuery>())
				{
					row.Run((ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Enterable.Data enterable, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction) =>
					{
						if (h_faction.id == 0 || h_faction == faction.id)
						{
							var dist_sq_tmp = Vector2.DistanceSquared(pos, transform.position); // - enterable.radius.Pow2();
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

			// TODO: add validation
			public struct EnterRPC: Net.IRPC<Enterable.Data>
			{
				public ICharacter.Handle h_character;

#if SERVER
				public void Invoke(ref NetConnection connection, Entity entity, ref Enterable.Data data)
				{
					ref var character_data = ref this.h_character.GetData(out var character_asset);
					Assert.NotNull(ref character_data);

					//ref var location_data = ref this.h_location.GetData(out var location_asset);
					//Assert.NotNull(ref location_data);

					ref var region = ref World.GetGlobalRegion();
					var ent_asset = this.h_character.AsEntity(0);

					if (ent_asset.IsAlive())
					{
						ent_asset.Delete();
					}

					character_data.ent_inside = entity;
					character_asset.Sync();


				}
#endif
			}

			// TODO: add validation
			public struct ExitRPC: Net.IRPC<Enterable.Data>
			{
				public ICharacter.Handle h_character;

#if SERVER
				public void Invoke(ref NetConnection connection, Entity entity, ref Enterable.Data data)
				{
					ref var character_data = ref this.h_character.GetData(out var character_asset);
					Assert.NotNull(ref character_data);

					//var h_location = character_data.h_location_current;

					//ref var location_data = ref h_location.GetData(out var location_asset);
					//Assert.NotNull(ref location_data);

					ref var region = ref World.GetGlobalRegion();
					var ent_asset = this.h_character.AsEntity(0);

					ref var transform = ref entity.GetComponent<Transform.Data>();
					Assert.NotNull(ref transform);

					//var road = WorldMap.GetNearestRoad(location_data.h_prefecture, Road.Type.Road, (Vector2)location_data.point, out var dist_sq);
					//var pos = road.GetPosition().GetRefValueOrDefault();

					//var random = XorRandom.New(true);
					//if (pos == default) pos = (Vector2)location_data.point + random.NextUnitVector2Range(0.25f, 0.50f);

					var pos = transform.position;

					var h_location = default(ILocation.Handle);
					if (entity.TryGetAssetHandle(out h_location) && WorldMap.location_to_road.TryGetValue(h_location, out var road))
					{
						pos = road.GetPosition();

						//ref var location_data = ref h_location.GetData();
						//if (location_data.IsNotNull())
						//{
						//	WorldMap.
						//}
					}

					character_data.ent_inside = default;
					character_asset.Sync();

					region.SpawnPrefab("unit.guy", position: pos, faction_id: character_data.faction, entity: ent_asset).ContinueWith((ent) =>
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
		}

		public static partial class Unit
		{
			[Query(ISystem.Scope.Global)]
			public delegate void GetAllQuery(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] in Unit.Data unit, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional(false)] in Faction.Data faction);

			[IComponent.Data(Net.SendType.Reliable)]
			public partial struct Data: IComponent
			{
				public enum Type: uint
				{
					Undefined = 0,

					Character,
					Vehicle
				}

				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					Wants_Repath = 1u << 0
				}

				public Unit.Data.Flags flags;

				public ILocation.Handle h_location;
				public Vector2 pos_next;
				public Vector2 pos_target;

				// speed in km/h
				public float speed = 6.00f;
				public float speed_current;
				public float acc = 3.00f;

				[Net.Ignore, Save.Ignore] public Road.Segment next_segment;
				[Net.Ignore, Save.Ignore] public Road.Segment end_segment;
				[Net.Ignore, Save.Ignore] public int current_branch_index;
				[Net.Ignore, Save.Ignore] public FixedArray32<Road.Junction.Branch> branches;
				[Net.Ignore, Save.Ignore] public int branches_count;

				public Data()
				{
				}
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
						if (h_faction.id == 0 || h_faction == faction.id)
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

			public struct MoveRPC: Net.IRPC<Unit.Data>
			{
				public Vector2 pos_target;

#if SERVER
				public void Invoke(ref NetConnection connection, Entity entity, ref Unit.Data data)
				{
					data.pos_target = this.pos_target;
					data.flags.SetFlag(Data.Flags.Wants_Repath, true);

					data.Sync(entity, true);
				}
#endif
			}

			[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
			public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			{

			}

			[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnUpdateGlobal(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Global] ref World.Global world_global, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			{
				if (unit.flags.TrySetFlag(Data.Flags.Wants_Repath, false))
				{
					unit.current_branch_index = 0;
					unit.branches_count = 0;

					var branches_span = unit.branches.AsSpan();
					if (Repath(transform.position, unit.pos_target, ref unit.next_segment, ref unit.end_segment, ref branches_span))
					{
						var branch = branches_span[0];

						unit.branches_count = branches_span.Length;
						unit.pos_next = unit.next_segment.GetPosition();
					}
					else
					{
						unit.pos_next = unit.pos_target;
					}
				}

				const float s_to_h = 1.00f / 60.00f;
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
						unit.pos_next = unit.next_segment.GetPosition();
					}
				}
				else
				{
					unit.pos_next = unit.pos_target;
				}

				var pos_target = unit.pos_next;
				var dir = (pos_target - transform.position).GetNormalized(out var dist);
				unit.speed_current = unit.speed;


				//if (dist > unit.acc * info.DeltaTime * 0.50f)
				//{
				//	unit.speed_current.MoveTowards(unit.speed, unit.acc * info.DeltaTime);
				//}
				//else
				//{
				//	unit.speed_current.MoveTowards(Maths.Min(dist * 50 * 5, unit.speed), unit.acc * info.DeltaTime);
				//}

				transform.position += (dir * Maths.Min(dist, ((unit.speed_current * info.DeltaTime * s_to_h) * time_scale)) / WorldMap.km_per_unit);
			}


			public static void GetNextSegment(Vector2 pos_current, ref Road.Segment segment, ref Road.Segment end_segment, ref int current_branch_index, Span<Road.Junction.Branch> branches)
			{
				//var dist_sq = Vector2.DistanceSquared(pos_current, segment.GetPosition());
				if (Maths.IsInDistance(pos_current, segment.GetPosition(), 0.10f))
				{
					if (segment == end_segment && end_segment.IsValid())
					{
						segment = default;
						App.WriteLine("done");
					}
					else if (current_branch_index < branches.Length)
					{
						//if (current_branch_index < branches.Length - 1 && WorldMap.road_segment_to_junction_index.TryGetValue(segment, out var junction_index) && junction_index == branches[current_branch_index + 1].junction_index)
						if (current_branch_index < branches.Length - 1 && Maths.IsInDistance(pos_current, WorldMap.road_junctions[branches[current_branch_index + 1].junction_index].pos, 0.25f))
						{
							current_branch_index++;
							segment = branches[current_branch_index].GetSegment();
							//segment.index = (byte)(segment.index + branches[current_branch_index].sign);
							App.WriteLine(branches[current_branch_index].sign);
						}
						else
						{
							segment.index = (byte)(segment.index + branches[current_branch_index].sign);
						}
					}
				}
			}

			public static bool Repath(Vector2 pos_a, Vector2 pos_b, ref Road.Segment segment_start, ref Road.Segment segment_end, ref Span<Road.Junction.Branch> branches_span)
			{
				var dir = (pos_b - pos_a).GetNormalizedFast();

				segment_start = default;
				segment_end = default;

				var road_a = WorldMap.GetNearestRoad(Road.Type.Road, pos_a, out var road_a_dist_sq);
				var road_b = WorldMap.GetNearestRoad(Road.Type.Road, pos_b, out var road_b_dist_sq);

				if (road_a.IsValid() && road_b.IsValid() && road_a != road_b && road_a_dist_sq < 2.00f.Pow2() && road_b_dist_sq < 2.00f.Pow2())
				{
					var sign_a = road_a.GetSign(dir, true, 0.00f, 1.00f);
					var sign_b = road_b.GetSign(-dir, true, 0.00f, 1.00f);

					var ok_a = road_a.TryGetNearestJunction(out var junction_index_a, out _);
					var ok_b = road_b.TryGetNearestJunction(out var junction_index_b, out _);

					if (ok_a && ok_b)
					{
						var junction_a = WorldMap.road_junctions[junction_index_a];
						var junction_b = WorldMap.road_junctions[junction_index_b];

						junction_a.TryResolveBranch((pos_b - junction_a.pos).GetNormalizedFast(), out var branch_src);
						junction_b.TryResolveBranch(dir, out var branch_dst);

						if (RoadNav.Astar.TryFindPath(branch_src, branch_dst, ref branches_span, ignore_limits: true, dot_min: 0.00f, dot_max: 1.00f))
						{
							//segment_start = branch_src.GetSegment();
							segment_start = branches_span[0].GetSegment();
							segment_start = segment_start.chain.GetNearestSegment(pos_a);

							//segment_end = road_b;

							//segment_start = road_a;
							segment_end = road_b;

							//segment_end = branch_dst.GetSegment();
							//segment_end = branches_span[branches_span.Length - 1].GetSegment();
							//segment_end = segment_end.chain.GetNearestSegment(pos_b);

							return true;
						}
					}
				}

				return false;
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
							//if (window.appearing)
							//{
							//	is_mouse_dragging = false;
							//	mouse_drag_rect = null;
							//	mouse_drag_a = null;
							//	mouse_drag_b = null;
							//}

							using (GUI.Group.New(GUI.Rm, padding: new(4)))
							{
								var mouse = GUI.GetMouse();

								ref var world_global = ref region.GetGlobalComponent<World.Global>();
								var time_scale = world_global.speed;
								var speed_km_h = this.unit.speed;

								GUI.LabelShaded("Speed:", $"{speed_km_h:0.00} km/h");
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

								GUI.DrawLine(pos_c_current, pos_c_next, Color32BGRA.Green.WithAlphaMult(0.25f), thickness: 0.125f * scale * 0.25f, GUI.Layer.Foreground);

								GUI.DrawCircle(pos_c_current, 0.750f * scale, Color32BGRA.Green, segments: 16, layer: GUI.Layer.Foreground);
								GUI.DrawCircleFilled(pos_c_next, 0.1250f * scale, Color32BGRA.Green.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);
								GUI.DrawCircleFilled(pos_c_target, 0.1250f * scale, Color32BGRA.Green.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);



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

								if (is_mouse_dragging && mouse_drag_a.HasValue && mouse_drag_b.HasValue)
								{
									mouse_drag_rect = new AABB(mouse_drag_a.Value, mouse_drag_b.Value);

									if (mouse_drag_rect.TryGetValue(out var rect))
									{
										var rect_c = region.WorldToCanvas(rect);

										GUI.DrawRectFilled(rect_c, color: Color32BGRA.Yellow.WithAlphaMult(0.10f), layer: GUI.Layer.Foreground);
										GUI.DrawRect(rect_c, color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground);
									}
								}

								ref var current_branch = ref unit.branches[unit.current_branch_index];
								if (current_branch.sign != 0)
								{
									GUI.DrawCircleFilled(region.WorldToCanvas(WorldMap.road_junctions[current_branch.junction_index].pos), 0.5f * scale * 0.50f, Color32BGRA.Orange.WithAlphaMult(0.50f), segments: 16, layer: GUI.Layer.Foreground);
								}

								GUI.Text($"{unit.current_branch_index}/{unit.branches_count}");
								if (unit.next_segment.IsValid())
								{
									GUI.DrawCircleFilled(region.WorldToCanvas(unit.next_segment.GetPosition()), 0.5f * scale * 0.50f, Color32BGRA.Cyan.WithAlphaMult(0.50f), segments: 16, layer: GUI.Layer.Foreground);
									GUI.Text($"{unit.next_segment.index}; {unit.next_segment.GetPosition()}");
								}

								WorldMap.DrawBranch(ref current_branch);

								if (WorldMap.IsHovered())
								{
									if (mouse.GetKeyDown(Mouse.Key.Left))
									{
										is_mouse_dragging = true;

										mouse_drag_a = pos_w_snapped;
									}

									GUI.DrawLine(pos_c_current, pos_c_current + (dir * 100), layer: GUI.Layer.Foreground);
									GUI.DrawLine(pos_c_current, pos_c_hover, Color32BGRA.Yellow.WithAlphaMult(0.25f), thickness: 0.125f * scale * 0.25f, GUI.Layer.Foreground);

									//GUI.DrawTextCentered($"{dist * WorldMap.km_per_unit:0.00} km", (pos_c_current + pos_c_hover) * 0.50f, layer: GUI.Layer.Foreground, box_shadow: true);

									GUI.DrawCircleFilled(pos_c_hover, 0.125f * scale * 0.50f, Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4, layer: GUI.Layer.Foreground);

									GUI.DrawTextCentered($"{dist * WorldMap.km_per_unit:0.00} km", pos_c_hover - ((pos_c_hover - pos_c_current).GetNormalized() * 0.50f * scale), layer: GUI.Layer.Foreground, box_shadow: true);

									var road_a = WorldMap.GetNearestRoad(Road.Type.Road, transform.position, out var road_a_dist_sq);
									var road_b = WorldMap.GetNearestRoad(Road.Type.Road, pos_w_snapped, out var road_b_dist_sq);

									if (road_a.IsValid() && road_b.IsValid())
									{
										var sign_a = road_a.GetSign(dir, true, 0.00f, 1.00f);
										var sign_b = road_b.GetSign(-dir, true, 0.00f, 1.00f);

										var ok_a = road_a.TryGetNearestJunction(out var junction_index_a, out _);
										var ok_b = road_b.TryGetNearestJunction(out var junction_index_b, out _);

										if (ok_a && ok_b)
										{
											Span<Road.Junction.Branch> branches_span = stackalloc Road.Junction.Branch[32];

											var junction_a = WorldMap.road_junctions[junction_index_a];
											var junction_b = WorldMap.road_junctions[junction_index_b];

											TryResolveBranch(junction_a, (pos_w_snapped - junction_a.pos).GetNormalizedFast(), out var branch_src);
											TryResolveBranch(junction_b, dir, out var branch_dst);

											//WorldMap.DrawBranch(ref branch_src);
											//WorldMap.DrawBranch(ref branch_dst);

											if (RoadNav.Astar.TryFindPath(branch_src, branch_dst, ref branches_span, ignore_limits: true, dot_min: 0.00f, dot_max: 1.00f))
											{
												foreach (ref var branch in branches_span)
												{
													//if (branch.sign != 0)
													{
														WorldMap.DrawBranch(ref branch);
													}
												}

												GUI.TextShaded("ok");
											}
										}
									}

									if (mouse.GetKeyDown(Mouse.Key.Right))
									{
										var rpc = new Unit.MoveRPC();
										rpc.pos_target = pos_w_snapped;
										rpc.Send(this.ent_unit);
									}
								}
							}
						}
						else
						{
							is_mouse_dragging = false;
							mouse_drag_rect = null;
							mouse_drag_a = null;
							mouse_drag_b = null;
						}
					}
				}
			}

			[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Unit.Data unit, [Source.Owned] ref Transform.Data transform)
			{
				if (WorldMap.IsOpen && WorldMap.selected_entity == entity)
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
}

