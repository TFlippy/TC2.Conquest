﻿
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	[Asset.Hjson(prefix: "route.", capacity_world: 128, capacity_region: 0, capacity_local: 0)]
	public interface IRoute: IAsset2<IRoute, IRoute.Data>
	{
		public struct Target
		{
			[Save.Force]
			public ILocation.Handle h_location;
			[Save.Force]
			public Road.Junction.Branch branch_entry;
			[Save.Force]
			public float stop_time;
		}

		static void IAsset2<IRoute, IRoute.Data>.GenerateSample(out IRoute.Data data)
		{
			data = new();
			data.name = "Sample";
			//data.desc = "TODO: Desc";
		}

		public struct Data
		{
			[Save.Force]
			public string name;

			//[Save.Force, Save.NewLine]
			//public Road.Junction.Branch[] branches;

			[Save.Force, Save.NewLine]
			public IRoute.Target[] targets;

			public Data()
			{
			}
		}
	}

	public struct ResolvedJunction: IComparable<ResolvedJunction>, IEquatable<ResolvedJunction>
	{
		public ushort unused;
		public byte segment_index;
		public sbyte sign;

		public float dot;

		public ResolvedJunction(byte segment_index, sbyte sign, float dot)
		{
			this.unused = 0;
			this.segment_index = segment_index;
			this.sign = sign;
			this.dot = dot;
		}

		public int CompareTo(ResolvedJunction other)
		{
			return other.dot.CompareTo(this.dot);
		}

		public bool Equals(ResolvedJunction other)
		{
			return Unsafe.As<ResolvedJunction, uint>(ref this) == Unsafe.As<ResolvedJunction, uint>(ref other);
		}

		public override int GetHashCode()
		{
			return Unsafe.As<ResolvedJunction, int>(ref this);
		}

		public static bool operator <(ResolvedJunction left, ResolvedJunction right) => left.CompareTo(right) < 0;
		public static bool operator >(ResolvedJunction left, ResolvedJunction right) => left.CompareTo(right) > 0;
		public static bool operator <=(ResolvedJunction left, ResolvedJunction right) => left.CompareTo(right) <= 0;
		public static bool operator >=(ResolvedJunction left, ResolvedJunction right) => left.CompareTo(right) >= 0;
	}

	public static class RoadNav
	{
		public struct JunctionNode: IEquatable<JunctionNode>
		{
			//public short junction_index;
			//public short junction_index_parent;

			public Road.Junction.Branch branch;

			public int parent_hash;
			public float weight;
			public float distance;
			public float cost;

			public JunctionNode(ushort junction_index, byte index, sbyte sign, float weight, float distance)
			{
				this.branch = new(junction_index, index, sign);
				this.weight = weight;
				this.distance = distance;
			}

			//public JunctionNode(short junction_index, short junction_index_parent, float weight, float distance)
			//{
			//	this.junction_index = junction_index;
			//	this.junction_index_parent = junction_index_parent;
			//	this.weight = weight;
			//	this.distance = distance;
			//}

			//public float GetDW()
			//{
			//	return this.distance + this.weight;
			//}

			public bool Equals(JunctionNode other)
			{
				return Unsafe.As<JunctionNode, uint>(ref this) == Unsafe.As<JunctionNode, uint>(ref other);
			}

			public override int GetHashCode()
			{
				return Unsafe.As<JunctionNode, int>(ref this);
			}

			public static bool operator ==(JunctionNode left, JunctionNode right) => left.Equals(right);
			public static bool operator !=(JunctionNode left, JunctionNode right) => !(left == right);

			public override bool Equals(object obj) => obj is JunctionNode node && this.Equals(node);
		}

		public static class Astar
		{
			//List<List<Node>> Grid;
			//int GridRows
			//{
			//	get
			//	{
			//		return Grid[0].Count;
			//	}
			//}
			//int GridCols
			//{
			//	get
			//	{
			//		return Grid.Count;
			//	}
			//}

			//public Astar(List<List<Node>> grid)
			//{
			//	Grid = grid;
			//}

			[ThreadStatic]
			public static PriorityQueue<JunctionNode, float> tls_open_list;
			[ThreadStatic]
			public static Dictionary<int, JunctionNode> tls_closed_list;
			//public static Stack<Road.Junction.Branch> Path = new();

			public static bool TryFindPath(Road.Junction.Branch a, Road.Junction.Branch b, ref Span<Road.Junction.Branch> out_results, bool ignore_limits = false, float dot_min = 0.50f, float dot_max = 1.00f)
			{
				if (a.GetHashCode() == b.GetHashCode()) return false;

				ref var open_list = ref tls_open_list;
				ref var closed_list = ref tls_closed_list;

				open_list ??= new(64);
				closed_list ??= new(64);

				var ts = Timestamp.Now();
				var junctions_span = WorldMap.road_junctions.AsSpan();

				//Path.Clear();
				open_list.Clear();
				closed_list.Clear();

				var current = new JunctionNode(a.junction_index, a.index, a.sign, -1.00f, 0.00f);

				open_list.Enqueue(current, -1.00f);

				//var ignore_limits = true;
				//var dot_min = 0.50f;
				//var dot_max = 1.00f;

				var hash_end = new JunctionNode(b.junction_index, b.index, b.sign, -1.00f, 0.00f).GetHashCode();
				Span<ResolvedJunction> resolved_junctions_buffer = stackalloc ResolvedJunction[8];

				var pos_a = junctions_span[a.junction_index].pos;
				var pos_b = junctions_span[b.junction_index].pos;

				while (open_list.Count != 0 && !closed_list.ContainsKey(hash_end))
				{
					current = open_list.Dequeue();
					closed_list.Add(current.GetHashCode(), current);

					//if (current.branch.junction_index >= junctions_span.Length) continue;
					//var junc = junctions_span[current.branch.junction_index];

					//if (current.branch.index >= junc.segments.Length) continue;
					//var seg_a = junc.segments[current.branch.index % junc.segments.Length];
					//var seg_a = junc.segments[current.branch.index];

					var seg_a = junctions_span[current.branch.junction_index].segments[current.branch.index];
					if (WorldMap.TryGetNextJunction(seg_a, current.branch.sign, out var junction_a, out var segment_b, out var segment_c))
					{
						var seg_a_pos = seg_a.GetPosition();

						ref var junction = ref junctions_span[junction_a];
						try
						{
							var dir = (segment_c.GetPosition() - segment_b.GetPosition()).GetNormalizedFast();
							//App.WriteLine($"{junction.pos} {segment_b.index}; {segment_c.index}; {dir}");

							var resolved_junctions = resolved_junctions_buffer;
							Train.ResolveJunction2(dir, ref junction, ignore_limits, dot_min, dot_max, ref resolved_junctions);

							for (var i = 0; i < resolved_junctions.Length; i++)
							{
								ref var res = ref resolved_junctions[i];
								var seg = junction.segments[res.segment_index];
								var branch = new Road.Junction.Branch((ushort)junction_a, res.segment_index, (sbyte)res.sign);

								ref var road = ref seg.GetRoad();

								var n = new JunctionNode(branch.junction_index, branch.index, branch.sign, current.weight, current.distance);
								if (!closed_list.ContainsKey(n.GetHashCode()))
								{
									var is_found = false;
									foreach (var oLNode in open_list.UnorderedItems)
									{
										if (oLNode.Element == n)
										{
											is_found = true;
										}
									}
									if (!is_found)
									{
										n.parent_hash = current.GetHashCode();
										n.distance = MathF.Abs(junction.pos.X - pos_b.X) + MathF.Abs(junction.pos.Y - pos_b.Y); //  Vector2.Distance(junction.pos, junctions_span[b.junction_index].pos); //  DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) + Math.Abs(n.Position.Y - end.Position.Y);
										n.weight = (Vector2.Distance(seg_a_pos, junction.pos) / (road.speed_mult * road.integrity)) * 0.50f;
										//n.weight = road.speed_mult * road.integrity;
										n.cost = (n.weight + current.cost);
										open_list.Enqueue(n, n.distance + n.cost);
									}
								}
							}
						}
						catch (Exception e)
						{
							App.WriteException(e);
						}
					}
				}

				if (!closed_list.ContainsKey(hash_end))
				{
					//App.WriteLine(ClosedList.Count);
					return false;
				}

				var results_count = 0;
				var temp = closed_list[current.GetHashCode()];
				do
				{
					out_results[results_count++] = temp.branch;
					//Path.Push(temp.route);
					temp = closed_list[temp.parent_hash];
				}
				while (temp.branch.junction_index != a.junction_index && results_count < out_results.Length - 1);

				out_results[results_count++] = a;

				//foreach (var route in Path)
				//{
				//	out_results[results_count++] = route;
				//}
				//out_results[results_count++] = a;
				out_results = out_results.Slice(0, results_count);
				out_results.Reverse();

				//Path.

				var ts_elapsed = ts.GetMilliseconds();
				//App.WriteLine($"Calculated a path in {ts_elapsed:0.0000} ms. ({out_results.Length}/{closed_list.Count})", App.Color.Green);
				return true;
			}
		}
	}

	public static partial class Train
	{
		public static void ResolveJunction(Vector2 dir_ab, ref Road.Junction nearest_junction, bool ignore_limits, float dot_min, float dot_max, out int c_alt_sign, out int segment_index, out float c_alt_dot)
		{
			ref var region = ref World.GetGlobalRegion();
			segment_index = -1;

			var c_alt = default(Road.Segment);
			c_alt_sign = 0;
			c_alt_dot = -1.00f;

			var segments = nearest_junction.segments.Slice(nearest_junction.segments_count);
			for (var j = 0; j < segments.Length; j++)
			{
				ref var j_segment = ref segments[j];

				ref var j_road = ref j_segment.GetRoad();
				//if (j_road.IsNull() || j_road.type != type) continue;
				if (j_road.IsNull()) continue;

				var j_points = j_road.points.AsSpan();
				var j_pos = j_points[j_segment.index];

				if (j_segment.index < j_points.Length - 1)
				{
					var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

					//var draw = false;
					if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
					{
						c_alt = new(j_segment.chain, (byte)(j_segment.index + 1));
						c_alt_dot = dot_tmp;
						c_alt_sign = 1;

						segment_index = (byte)(j);

						//draw = true;
						//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
					}
					//if (draw || (j == routes[0].index && routes[0].sign == 1) || (j == routes[1].index && routes[1].sign == 1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == routes[0].index && routes[0].sign == 1) || (j == routes[1].index && routes[1].sign == 1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);

					//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
				}

				if (j_segment.index > 0)
				{
					var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

					//var draw = false;
					if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
					{
						c_alt = new(j_segment.chain, (byte)(j_segment.index - 1));
						c_alt_dot = dot_tmp;
						c_alt_sign = -1;

						segment_index = (byte)(j);
						//draw = true;
						//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
					}
					//if (draw || (j == routes[0].index && routes[0].sign == -1) || (j == routes[1].index && routes[1].sign == -1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == routes[0].index && routes[0].sign == -1) || (j == routes[1].index && routes[1].sign == -1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);

					//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
				}
			}
		}

		public static void ResolveJunction2(Vector2 dir_ab, ref Road.Junction nearest_junction, bool ignore_limits, float dot_min, float dot_max, ref Span<ResolvedJunction> out_indices)
		{
			//ref var region = ref World.GetGlobalRegion();

			var indices_count = 0;

			var segments = nearest_junction.segments.Slice(nearest_junction.segments_count);
			for (var j = 0; j < segments.Length; j++)
			{
				ref var j_segment = ref segments[j];

				ref var j_road = ref j_segment.GetRoad();
				//if (j_road.IsNull() || j_road.type != type) continue;
				if (j_road.IsNull()) continue;

				var j_points = j_road.points.AsSpan();
				var j_pos = j_points[j_segment.index];

				if (j_segment.index < j_points.Length - 1)
				{
					var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

					if ((ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
					{
						out_indices[indices_count++] = new((byte)j, 1, dot_tmp);
						//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
					}
					//if (draw || (j == routes[0].index && routes[0].sign == 1) || (j == routes[1].index && routes[1].sign == 1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == routes[0].index && routes[0].sign == 1) || (j == routes[1].index && routes[1].sign == 1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);

					//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
				}

				if (j_segment.index > 0)
				{
					var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

					if ((ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
					{
						out_indices[indices_count++] = new((byte)j, -1, dot_tmp);
						//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Green);
					}
					//if (draw || (j == routes[0].index && routes[0].sign == -1) || (j == routes[1].index && routes[1].sign == -1)) region.DrawDebugDir(j_pos, dir_tmp * 0.50f, (j == routes[0].index && routes[0].sign == -1) || (j == routes[1].index && routes[1].sign == -1) ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 3.00f);

					//region.DrawDebugDir(j_pos, dir_tmp * 0.65f, Color32BGRA.Red);
				}
			}

			out_indices = out_indices.Slice(0, indices_count);
			out_indices.Sort();
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Active = 1u << 0,
				[Asset.Ignore] Stuck = 1u << 1,
				[Asset.Ignore] Docked = 1u << 2,
				[Asset.Ignore] Pathing = 1u << 3,
				[Asset.Ignore] Wants_Repath = 1u << 4,
			}

			public Train.Data.Flags flags;

			public IRoute.Handle h_route;
			[Asset.Ignore] public int current_branch_index;
			[Asset.Ignore] public int current_route_index;
			[Asset.Ignore] public Road.Junction.Branch last_branch;
			[Asset.Ignore] public FixedArray64<Road.Junction.Branch> branches;
			[Asset.Ignore] public int branches_count;

			[Asset.Ignore] public ILocation.Handle h_location_target;
			public ITransport.Handle h_transport;

			public Filter.Mask<ILocation.Buildings> mask_stop_buildings;
			public Filter.Mask<ILocation.Categories> mask_stop_categories;

			[Asset.Ignore] public Road.Segment segment_a;
			[Asset.Ignore] public Road.Segment segment_b;
			[Asset.Ignore] public Road.Segment segment_c;
			[Asset.Ignore] public Road.Segment segment_stop;


			[Asset.Ignore] public Vector2 direction_old;
			[Asset.Ignore] public Vector2 direction;

			[Asset.Ignore] public Vector2 dir_ab;
			[Asset.Ignore] public Vector2 dir_bc;

			//public float dot_current;
			public float dot_min = 0.70f;
			public float dot_max = 1.00f;


			//public Road.Chain current_road;
			//public int current_road_index;
			[Asset.Ignore] public float road_distance_current;
			[Asset.Ignore] public float road_distance_target;

			public float speed = 0.50f;
			[Asset.Ignore] public float speed_current = 0.50f;
			public float speed_max = 1.50f;
			public float brake = 0.50f;
			public float acceleration = 0.50f;

			[Asset.Ignore] public float t_stop_departing;
			[Asset.Ignore] public int sign;

			public Data()
			{
			}

			public void Repath()
			{
				var branch_src = this.last_branch;

				ref var route_data = ref this.h_route.GetData();
				if (route_data.IsNotNull())
				{
					var targets = route_data.targets.AsSpan();
					if (targets.Length > 0)
					{
						var branches_span = this.branches.AsSpan();
						branches_span.Clear();

						ref var target = ref targets[0];
						var branch_dst = target.branch_entry;

						if (RoadNav.Astar.TryFindPath(branch_src, branch_dst, ref branches_span, dot_min: this.dot_min, dot_max: this.dot_max))
						{
#if CLIENT
							foreach (ref var branch in branches_span)
							{
								if (branch.sign != 0)
								{
									WorldMap.DrawBranch(ref branch);
								}
							}
#endif

							this.branches_count = branches_span.Length;
							this.current_branch_index = 0;
							//this.flags |= Flags.Active;
						}
						else
						{
							this.branches_count = 0;
							this.current_branch_index = 0;
						}
					}
				}

				//RoadNav.Astar.TryFindPath(last_branch, )

				//WorldMap.TryGetNextJunction(this.segment_b, this.sign, out var junction_index, out var road_b, out var road_c);

				//RoadNav.Astar.TryFindPath(new(junction_index, )

			}
		}

#if SERVER
		[ChatCommand.Global("train", "", creative: true)]
		public static void TrainCommand(ref ChatCommand.Context context, string location)
		{
			ref var region = ref context.GetRegionGlobal();
			var h_location = new ILocation.Handle(location);

			ref var location_data = ref h_location.GetData(out var location_asset);
			if (location_data.IsNotNull())
			{
				if (WorldMap.location_to_rail.TryGetValue(h_location, out var rail))
				{
					region.SpawnPrefab("train", rail.GetPosition()).ContinueWith(ent_train =>
					{
						ref var train = ref ent_train.GetComponent<Train.Data>();
						if (train.IsNotNull())
						{
							train.sign = 1;

							train.segment_a = rail with { index = (byte)(rail.index - 1) };
							train.segment_b = rail with { index = rail.index };
							train.segment_c = rail with { index = (byte)(rail.index + 1) };

							train.Sync(ent_train, true);


						}
					});
				}
			}
		}
#endif

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnUpdate(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Train.Data train, [Source.Owned] ref Transform.Data transform)
		{
			if (!train.segment_a.IsValid()) return;
			if (!train.segment_b.IsValid()) return;
			if (!train.segment_c.IsValid()) return;

			const bool show_debug = false;

#if SERVER
			return;
#endif

			//			if (show_debug)
			//			{
			//#if CLIENT
			//				//region.DrawDebugRect(AABB.Centered(transform.position, new Vector2(0.125f)), Color32BGRA.Cyan);

			//				region.DrawDebugCircle(train.segment_a.GetPosition(), 0.125f, Color32BGRA.Blue, filled: true);
			//				region.DrawDebugCircle(train.segment_b.GetPosition(), 0.125f, Color32BGRA.Yellow, filled: true);
			//				region.DrawDebugCircle(train.segment_c.GetPosition(), 0.125f, Color32BGRA.Red, filled: true);

			//				region.DrawDebugDir(train.segment_a.GetPosition(), train.direction * Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition()), Color32BGRA.Yellow);
			//				//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction * Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition()), Color32BGRA.Yellow);
			//				//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction, Color32BGRA.Magenta);
			//#endif
			//			}

#if SERVER
			//region.DrawDebugCircle(transform.position, 0.175f, Color32BGRA.Cyan, filled: true);
#endif

			if (train.flags.TrySetFlag(Data.Flags.Wants_Repath, false))
			{
				App.WriteLine("wants repath");
				train.Repath();
			}

#if CLIENT
			//region.DrawDebugDir(train.segment_a.GetPosition(), train.dir_ab, Color32BGRA.Yellow);
			//region.DrawDebugDir(train.segment_b.GetPosition(), train.dir_bc, Color32BGRA.Yellow);
			//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction, Color32BGRA.Magenta);

			//WorldMap.DrawBranch(ref train.last_branch, size: 0.25f, color: Color32BGRA.Green);

			//if (train.flags.HasAny(Data.Flags.Pathing))

			//if (train.branches_count > 0)
			//{
			//	var branches_span = train.branches.Slice(train.branches_count);
			//	for (var i = train.current_branch_index; i < train.branches_count; i++)
			//	{
			//		ref var branch = ref branches_span[i];
			//		if (branch.sign != 0)
			//		{
			//			WorldMap.DrawBranch(ref branch, index: i);
			//		}
			//	}
			//}
#endif

			//{
			//	if (WorldMap.TryGetNextJunction(train.segment_c, train.sign, out var j_index, out var segment_b, out var segment_c))
			//	{
			//		var route = new Road.Junction.Branch(j_index, 0, (sbyte)train.sign);
			//	}
			//}

			if (train.segment_b == train.segment_c) train.flags |= Data.Flags.Stuck;
			if (train.flags.HasAny(Data.Flags.Stuck))
			{
				train.road_distance_current = 0.00f;
				return;
			}

			if (train.flags.HasAny(Data.Flags.Docked))
			{
				if (info.WorldTime >= train.t_stop_departing)
				{
					train.segment_stop = default;
					train.flags.SetFlag(Data.Flags.Docked, false);
				}
			}
			else
			{
				if (train.segment_a == train.segment_stop)
				{
					train.speed = 0.00f;

					if (train.speed_current <= 0.01f)
					{
						train.flags.SetFlag(Data.Flags.Docked, true);
						train.t_stop_departing = info.WorldTime + 5.00f;
					}
				}
				else
				{
					train.speed = train.speed_max;
				}

				train.speed_current = Maths.MoveTowards(train.speed_current, train.speed, (train.speed_current < train.speed ? train.acceleration : train.brake) * App.fixed_update_interval_s);

				if (train.road_distance_current >= train.road_distance_target)
				{
					var segment_a_tmp = train.segment_a;
					var segment_b_tmp = train.segment_b;
					var segment_c_tmp = train.segment_c;

					train.segment_a = train.segment_b;
					train.segment_b = train.segment_c;

					var ok = false;

					ref var route_data = ref train.h_route.GetData();
					if (route_data.IsNotNull() && train.branches_count > 0 && train.current_branch_index < train.branches_count)
					{
						var branches = train.branches.Slice(train.branches_count);

						//ref var branch = ref train.branches[train.current_branch_index];
						//var junction = WorldMap.road_junctions[branch.junction_index];

						if (WorldMap.TryAdvance(train.segment_a, train.segment_b, out train.segment_c, ref train.sign, out var junction_index, false))
						{
							ok = true;
						}

						//#if CLIENT
						//						region.DrawDebugCircle(junction.pos, 0.125f, Color32BGRA.Yellow, filled: true);
						//#endif

						if (junction_index != -1)
						{
							App.WriteLine($"junc {junction_index}");
						}

						ref var branch = ref branches[train.current_branch_index];
						if (junction_index == branch.junction_index)
						{
							var junction = WorldMap.road_junctions[branch.junction_index];
							train.sign = branch.sign;

							train.segment_b = junction.segments[branch.index];
							train.segment_c = junction.segments[branch.index] with { index = (byte)(train.segment_b.index + train.sign) };
							train.last_branch = branch;

							//train.segment_c = train.segment_b;
							//train.segment_c.index = (byte)(train.segment_c.index + train.sign);

							train.current_branch_index++;
							if (train.current_branch_index >= branches.Length)
							{
								train.flags.SetFlag(Data.Flags.Pathing, false);
							}
							else
							{
								train.flags.SetFlag(Data.Flags.Pathing, true);
							}
							//train.current_branch_index %= train.branches.Length;

							//if (train.current_route_index > route_data.routes.Length)
							//{
							//	train.current_route_index =
							//}

							ok = true;
						}
						else if (junction_index != -1)
						{
							//App.WriteLine($"junction {junction_index} ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index}to {segment_c_new.chain.h_prefecture}:{segment_c_new.chain.index}:{segment_c_new.index})");

							if (WorldMap.TryAdvanceJunction(train.segment_a, train.segment_b, train.segment_c, junction_index, out var c_branch, out var c_alt_segment, out var c_alt_sign, out var c_alt_dot, dot_min: train.dot_min, dot_max: train.dot_max, ignore_limits: !ok))
							{
								//App.WriteLine($"advanced junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}) ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index})");

								train.last_branch = c_branch;
								if (train.flags.HasAny(Data.Flags.Pathing))
								{
									train.segment_c = c_alt_segment;
									train.sign = c_alt_sign;
									ok = true;
								}
								//train.dot_current = c_alt_dot;



								//return;
							}
							else
							{
								//App.WriteLine($"skip junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}; ({c_alt_segment.chain.h_prefecture}:{c_alt_segment.chain.index}:{c_alt_segment.index})");
							}
						}
						else
						{
							//App.WriteLine($"no junction {junction_index}");
						}

						if (!ok)
						{
							(train.segment_a, train.segment_b, train.segment_c) = (segment_b_tmp, segment_c_tmp, segment_b_tmp);
							train.sign = -train.sign;
						}
					}
					else
					{

						if (WorldMap.TryAdvance(train.segment_a, train.segment_b, out train.segment_c, ref train.sign, out var junction_index, false))
						{
							ok = true;
							//App.WriteLine("advanced road");
						}
						else
						{
							//App.WriteLine("failed road");
						}



						if (junction_index != -1)
						{
							//App.WriteLine($"junction {junction_index} ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index}to {segment_c_new.chain.h_prefecture}:{segment_c_new.chain.index}:{segment_c_new.index})");

							if (WorldMap.TryAdvanceJunction(train.segment_a, train.segment_b, train.segment_c, junction_index, out var c_branch, out var c_alt_segment, out var c_alt_sign, out var c_alt_dot, dot_min: train.dot_min, dot_max: train.dot_max, ignore_limits: !ok))
							{
								//App.WriteLine($"advanced junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}) ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index})");

								train.last_branch = c_branch;
								train.segment_c = c_alt_segment;
								train.sign = c_alt_sign;
								//train.dot_current = c_alt_dot;

								ok = true;

								//return;
							}
							else
							{
								//App.WriteLine($"skip junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}; ({c_alt_segment.chain.h_prefecture}:{c_alt_segment.chain.index}:{c_alt_segment.index})");
							}
						}
						else
						{
							//App.WriteLine($"no junction {junction_index}");
						}

						if (!ok)
						{
							(train.segment_a, train.segment_b, train.segment_c) = (segment_b_tmp, segment_c_tmp, segment_b_tmp);
							train.sign = -train.sign;
						}
					}

					train.direction_old = train.direction;
					train.road_distance_current -= train.road_distance_target;
					train.direction = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalized(out train.road_distance_target);

					train.dir_ab = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalizedFast();
					train.dir_bc = (train.segment_c.GetPosition() - train.segment_b.GetPosition()).GetNormalizedFast();

					if (WorldMap.rail_to_location.TryGetValue(train.segment_c, out var h_location))
					{
						ref var location_data = ref h_location.GetData();
						if (location_data.IsNotNull())
						{
							if (train.mask_stop_buildings.Evaluate(location_data.buildings) && train.mask_stop_categories.Evaluate(location_data.categories))
							{

								train.segment_stop = train.segment_c;
								//App.WriteLine($"passed {h_location}");
							}
						}
					}
				}

				if (train.segment_a.IsValid())
				{
					train.road_distance_current += train.speed_current * App.fixed_update_interval_s;

					//transform.SetRotation(transform.rotation, Maths.LerpAngle(train.dir_ab.GetAngleRadians(), train.direction.GetAngleRadians(), Maths.NormalizeClamp(train.road_distance_current, 0.350f)));

					//train.direction = Vector2.Lerp(train.dir_ab, train.dir_bc, (Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) + Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.50f);
					//train.direction = train.dir_ab; // Vector2.Lerp(train.dir_ab, train.dir_bc, Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)).GetNormalizedFast();

					//transform.SetRotation(Vector2.Lerp(train.dir_ab, train.dir_bc, ((Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) * 0.75f) + (Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.25f)).GetAngleRadians());
					transform.position = train.segment_a.GetPosition() + (train.direction * train.road_distance_current);
					//transform.SetRotation(Vector2.Lerp(train.dir_ab, train.dir_bc, ((Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) * 0.75f) + (Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.25f)).GetAngleRadians());

					transform.SetRotation(transform.rotation, Maths.LerpAngle(train.direction_old.GetAngleRadiansFast(), train.direction.GetAngleRadiansFast(), Maths.Normalize01Fast(train.road_distance_current, 0.350f)));

				}
			}
		}

#if CLIENT
		public partial struct TrainGUI: IGUICommand
		{
			public Entity ent_train;
			public Train.Data train;
			public Transform.Data transform;

			public static Sprite sprite_train = new("gui.icon.train.00", 24, 24);

			public void Draw()
			{
				ref var region = ref this.ent_train.GetRegionCommon();
				var rect = region.WorldToCanvas(AABB.Circle(this.transform.position, 0.25f));

				using (var window = GUI.Window.Standalone($"train.{this.ent_train}", rect.GetPosition(), size: rect.GetSize(), flags: GUI.Window.Flags.None, force_position: true))
				{
					using (GUI.ID.Push("train"u8))
					{
						//GUI.DrawCircle(region.WorldToCanvas(this.transform.GetInterpolatedPosition()),, Color32BGRA.Magenta, segments: 3, layer: GUI.Layer.Foreground);
						var is_pressed = GUI.ButtonBehavior("train"u8, rect, out var is_hovered, out var is_held);

						var sprite = sprite_train;

						var rot = this.transform.GetInterpolatedRotation();
						var rot_snapped = Maths.Snap(rot, MathF.PI * 0.250f);
						var rot_rem = Maths.DeltaAngle(rot, rot_snapped);

						var rot_invlerp = Maths.InvLerp01(-MathF.PI, MathF.PI, rot_snapped);
						sprite.frame.x = (uint)(rot_invlerp * 8);

						// the sprites aren't 45°
						switch (sprite.frame.x)
						{
							case 1:
							{
								rot_rem += MathF.PI * 0.100f;
							}
							break;

							case 3:
							{
								rot_rem -= MathF.PI * 0.100f;
							}
							break;

							case 5:
							{
								rot_rem += MathF.PI * 0.100f;
							}
							break;

							case 7:
							{
								rot_rem -= MathF.PI * 0.100f;
							}
							break;
						}

						var use_shader = true;
						if (use_shader)
						{
							Doodad.Renderer.Add(new Doodad.Renderer.Data()
							{
								sprite = sprite,
								position = Maths.Snap(this.transform.position, 1.00f / 32.00f),
								rotation = -rot_rem,
								z = 0.75f,
								color = Color32BGRA.White,
								scale = new Vector2(0.375f)
							});
						}
						else
						{
							GUI.DrawSpriteCentered2(sprite, rect, GUI.Layer.Window, scale: (region.GetWorldToCanvasScale() / 32) * 1.25f, rotation: rot_rem, pivot: new(0.00f, 0.50f));
						}

						//GUI.DrawRect(rect, Color32BGRA.Yellow, layer: GUI.Layer.Window);
						//GUI.DrawTextCentered($"{rot:0.000}\n{rot_rem:0.000}", rect.GetPosition(), layer: GUI.Layer.Window);


						if (is_pressed)
						{
							WorldMap.interacted_entity.Toggle(this.ent_train);

							App.WriteLine("press");

							GUI.SetDebugEntity(this.ent_train);
						}

						if (is_hovered)
						{
							GUI.SetCursor(App.CursorType.Hand, 1000);
							//App.WriteLine("hover");
						}
					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, 
		[Source.Owned] ref Train.Data train, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Train.TrainGUI()
				{
					ent_train = entity,
					train = train,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

