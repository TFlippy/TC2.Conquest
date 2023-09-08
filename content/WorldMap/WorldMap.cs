
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class Train
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Active = 1u << 0,
				Stuck = 1u << 1,
			}

			public Train.Data.Flags flags;

			public Road.Segment segment_a;
			public Road.Segment segment_b;
			public Road.Segment segment_c;

			public Vector2 direction_old;
			public Vector2 direction;

			//public float dot_current;
			public float dot_min = 0.70f;
			public float dot_max = 1.00f;

			//public Road.Chain current_road;
			//public int current_road_index;
			public float road_distance_current;
			public float road_distance_target;

			public float speed = 0.50f;
			public float speed_current = 0.50f;
			public float brake = 0.50f;
			public float acceleration = 0.50f;


			public int sign;

			public Data()
			{
			}
		}

		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnUpdate(ISystem.Global.Info info, ref Region.Global.Data region, Entity entity, [Source.Owned] ref Train.Data train, [Source.Owned] ref Transform.Data transform)
		{
			if (!train.segment_a.IsValid()) return;
			if (!train.segment_b.IsValid()) return;
			if (!train.segment_c.IsValid()) return;

			var show_debug = false;

#if SERVER
			return;
#endif

			if (show_debug)
			{
#if CLIENT
			//region.DrawDebugRect(AABB.Centered(transform.position, new Vector2(0.125f)), Color32BGRA.Cyan);

			region.DrawDebugCircle(train.segment_a.GetPosition(), 0.125f, Color32BGRA.Blue, filled: true);
			region.DrawDebugCircle(train.segment_b.GetPosition(), 0.125f, Color32BGRA.Yellow, filled: true);
			region.DrawDebugCircle(train.segment_c.GetPosition(), 0.125f, Color32BGRA.Red, filled: true);

			region.DrawDebugDir(train.segment_a.GetPosition(), train.direction * Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition()), Color32BGRA.Yellow);
			//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction * Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition()), Color32BGRA.Yellow);
			//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction, Color32BGRA.Magenta);
#endif
			}

#if CLIENT
			region.DrawDebugCircle(transform.position, 0.175f, Color32BGRA.Cyan, filled: true);
#endif


			if (train.segment_b == train.segment_c) train.flags |= Data.Flags.Stuck;
			if (train.flags.HasAny(Data.Flags.Stuck))
			{
				train.road_distance_current = 0.00f;
				return;
			}

			if (train.road_distance_current >= train.road_distance_target)
			{
				var segment_a_tmp = train.segment_a;
				var segment_b_tmp = train.segment_b;
				var segment_c_tmp = train.segment_c;

				train.segment_a = train.segment_b;
				train.segment_b = train.segment_c;

				var ok = false;

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
					//App.WriteLine($"junction {junction_index} ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index}to {segment_c_new.chain.h_district}:{segment_c_new.chain.index}:{segment_c_new.index})");

					if (WorldMap.TryAdvanceJunction(train.segment_a, train.segment_b, train.segment_c, junction_index, out var c_alt_segment, out var c_alt_sign, out var c_alt_dot, dot_min: train.dot_min, dot_max: train.dot_max, ignore_limits: !ok))
					{
						//App.WriteLine($"advanced junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}) ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

						train.segment_c = c_alt_segment;
						train.sign = c_alt_sign;
						//train.dot_current = c_alt_dot;

						ok = true;

						//return;
					}
					else
					{
						//App.WriteLine($"skip junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}; ({c_alt_segment.chain.h_district}:{c_alt_segment.chain.index}:{c_alt_segment.index})");
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

				train.direction_old = train.direction;
				train.road_distance_current -= train.road_distance_target;
				train.direction = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalized(out train.road_distance_target);
			}

			if (train.segment_a.IsValid())
			{
				train.road_distance_current += info.DeltaTime * train.speed;
				transform.position = train.segment_a.GetPosition() + (train.direction * train.road_distance_current);
			}
		}
	}

	public static partial class Location
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			public ILocation.Handle h_location;
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnAdd(ISystem.Global.Info info, ref Region.Global.Data region, Entity entity, [Source.Owned] ref Location.Data location, [Source.Owned] ref Transform.Data transform)
		{
			if (ILocation.TryGetAsset(entity, out var h_location))
			{
				ref var location_data = ref h_location.GetData();
				if (location_data.IsNotNull())
				{
					location.h_location = h_location;
					transform.SetPosition(new(location_data.point.X, location_data.point.Y));
				}
			}
			else
			{

			}

			//App.WriteLine($"OnAdd: {h_location}; {entity}");
		}

		public struct DEV_TestRPC: Net.IRPC<Location.Data>
		{
			public uint val;

#if SERVER
			public void Invoke(ref NetConnection connection, Entity entity, ref Location.Data data)
			{
				App.WriteLine($"hello {this.val}; {entity}; {data.h_location}");
			}
#endif
		}
	}

	public static partial class WorldMap
	{
		public static Dictionary<int, Road.Segment> road_segments = new(256);
		public static Dictionary<int, List<Road.Segment>> road_segments_overlapped = new(256);
		public static Dictionary<Road.Segment, int> road_segment_to_junction_index = new(256);
		public static List<Road.Junction> road_junctions = new(128);

		public static float road_junction_threshold = 0.250f;
		public const float km_per_unit = 2.00f;

		public static Dictionary<ILocation.Handle, Road.Segment> location_to_road = new();
		public static Dictionary<ILocation.Handle, Road.Segment> location_to_rail = new();

		public static bool TryAdvance(Road.Segment a, Road.Segment b, out Road.Segment c, ref int dir_sign, out int junction_index, bool skip_inner_junctions = false)
		{
			junction_index = -1;
			var is_at_end = false;

			ref var road = ref b.GetRoad();
			var points = road.points.AsSpan();

			is_at_end = dir_sign == -1 ? b.index == 0 : b.index >= points.Length - 1;
			c = new Road.Segment(b.chain, (byte)Maths.Clamp(b.index + dir_sign, 0, points.Length - 1));

			if ((is_at_end || !skip_inner_junctions) && road_segment_to_junction_index.TryGetValue(b, out var junction_index_tmp))
			{
				junction_index = junction_index_tmp;
			}

			is_at_end = c == b;
			return !is_at_end; // && !is_at_junction;
		}

		public static bool TryAdvanceJunction(Road.Segment a, Road.Segment b, Road.Segment c, int junction_index, out Road.Segment c_alt, out int c_alt_sign, out float c_alt_dot, float dot_min = 0.40f, float dot_max = 1.00f, bool ignore_limits = false)
		{
			var ok = false;
			c_alt = default;
			c_alt_dot = -1.00f;
			c_alt_sign = default;

			if ((uint)junction_index < road_junctions.Count)
			{
				var junction = road_junctions[junction_index];
				var junction_segments = junction.segments.Slice(junction.segments_count);

				//ignore_limits &= junction.segments_count <= 2;

				ref var road_a = ref a.GetRoad();
				ref var road_b = ref b.GetRoad();
				ref var road_c = ref b.GetRoad();

				var points_a = road_a.points.AsSpan();
				var points_b = road_b.points.AsSpan();
				var points_c = road_c.points.AsSpan();

				var type = road_a.type;

				var dir_ab = (points_b[b.index] - points_a[a.index]).GetNormalizedFast();
				var dir_bc = (points_c[c.index] - points_b[b.index]).GetNormalizedFast();

				var dot = MathF.Min(dot_max, Vector2.Dot(dir_ab, dir_bc));

				for (var i = 0; i < junction_segments.Length; i++)
				{
					ref var j_segment = ref junction_segments[i];
					if (j_segment == a) continue;
					if (j_segment == b) continue;
					if (j_segment == c) continue;

					ref var j_road = ref j_segment.GetRoad();
					if (j_road.IsNull() || j_road.type != type) continue;

					var j_points = j_road.points.AsSpan();
					var j_pos = j_points[j_segment.index];

					if (j_segment.index < j_points.Length - 1)
					{
						var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
						var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

						World.GetGlobalRegion().DrawDebugDir(j_pos, dir_tmp, Color32BGRA.Orange);

						if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
						{
							c_alt = new(j_segment.chain, (byte)(j_segment.index + 1));
							c_alt_dot = dot_tmp;
							c_alt_sign = 1;
						}
					}

					if (j_segment.index > 0)
					{
						var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
						var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

						World.GetGlobalRegion().DrawDebugDir(j_pos, dir_tmp, Color32BGRA.Orange);

						if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
						{
							c_alt = new(j_segment.chain, (byte)(j_segment.index - 1));
							c_alt_dot = dot_tmp;
							c_alt_sign = -1;
						}
					}
				}

				ok = c_alt.IsValid(); // && (ignore_limits || c_alt_dot >= dot);
			}

			return ok;
		}

		public struct RoadConnection
		{
			public int junction_index;
			public float distance;
			public float budget;

			public RoadConnection(int junction_index, float distance, float score)
			{
				this.junction_index = junction_index;
				this.distance = distance;
				this.budget = score;
			}
		}

		public static HashSet<ulong> segments_visited = new(128);
		public static Queue<RoadConnection> junctions_queue = new(64);

		public static void Init()
		{
			WorldMap.RecalculateRoads();
		}

#if CLIENT
		public static Vector2 worldmap_offset_current_snapped;
		public static Vector2 worldmap_offset_current;
		public static Vector2 worldmap_offset_target;
		public static Vector2 momentum;

		public static float worldmap_zoom_current = 6.00f;
		public static float worldmap_zoom_target = 6.00f;

		public static Vector2 worldmap_window_size = new Vector2(1024, 1024);
		public static Vector2 worldmap_window_offset = new Vector2(0, 0);

		public static float rotation;

		public static Road.Chain edit_road;

		public static int? edit_points_index;
		public static short2[] edit_points_s16;
		public static Vector2[] edit_points_f32;

		public static IScenario.Doodad? clipboard_doodad;

		public static IAsset.IDefinition edit_asset;
		public static IScenario.Handle h_world = "krumpel";
		public static ILocation.Handle h_selected_location;
		//public static Road.Handle road_ch;
		public static int? edit_doodad_index;
		public static Vector2 edit_doodad_offset;

		public static Texture.Handle h_texture_bg_00 = "worldmap.bg.00";
		public static Texture.Handle h_texture_icons = "worldmap.icons.00";
		public static Texture.Handle h_texture_line_00 = "worldmap.border.00";
		public static Texture.Handle h_texture_line_01 = "worldmap.border.01";
		public static Texture.Handle h_texture_line_02 = "worldmap.border.02";

		public static Texture.Handle h_texture_terrain_beach_00 = "worldmap.terrain.beach.00";

		public static Vector2 mouse_pos_old;
		public static Vector2 mouse_pos_new;

		public static HashSet<IAsset.IDefinition> hs_pending_asset_saves = new HashSet<IAsset.IDefinition>(64);

		public static byte selected_region_id;

		public static GUI.GizmoState gizmo;

		//public static bool enable_editor;

		public static EditorMode editor_mode;
		public static bool enable_renderer = true;
		public static bool enable_momentum = true;
		public static bool enable_snapping = true;
		public static bool snap_camera = true;
		public static bool dragging;

		public static bool show_provinces = true;
		public static bool show_districts = true;
		public static bool show_regions = true;
		public static bool show_locations = true;
		public static bool show_roads = true;
		public static bool show_rails = true;
		public static bool show_borders = true;
		public static bool show_fill = true;
		public static bool show_doodads = true;
		//public static BitField<Road.Type> filter_roads = new BitField<Road.Type>(Road.Type.Road, Road.Type.Rail, Road.Type.Marine, Road.Type.Air);
		//public static BitField<Road.Type> filter_roads_mask = new BitField<Road.Type>(Road.Type.Road, Road.Type.Rail, Road.Type.Marine, Road.Type.Air);
#endif
	}
}

