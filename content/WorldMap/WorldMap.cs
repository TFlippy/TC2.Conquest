
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public static Dictionary<Road.Segment, int> road_segment_to_junction_index = new(256);
		public static List<Road.Junction> road_junctions = new(128);

		public static float road_junction_threshold = 0.1250f;
		public const float km_per_unit = 2.00f;

		public static Entity selected_entity;

		public static Dictionary<ILocation.Handle, Road.Segment> location_to_road = new();
		public static Dictionary<ILocation.Handle, Road.Segment> location_to_rail = new();

		public static Dictionary<Road.Segment, ILocation.Handle> road_to_location = new();
		public static Dictionary<Road.Segment, ILocation.Handle> rail_to_location = new();

		public static partial class Marker
		{
			[IComponent.Data(Net.SendType.Reliable)]
			public partial struct Data: IComponent
			{
				public short2 point;
				public float radius;
				public float scale;

				public Sprite icon;
			}
		}

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

		public static bool TryGetNextJunction(Road.Segment a, int dir_sign, out int junction_index, out Road.Segment b, out Road.Segment c, bool skip_inner_junctions = false)
		{
			junction_index = -1;
			b = a;
			c = b;

			ref var road = ref a.GetRoad();
			if (road.IsNull()) return false;

			var points = road.points.AsSpan();

			var index = (int)a.index; // + dir_sign;
			if (dir_sign == -1)
			{
				while (--index >= 0)
				{
					b = c;
					c = b with { index = (byte)Maths.Clamp(index, 0, points.Length - 1) };

					//World.GetGlobalRegion().DrawDebugCircle(c.GetPosition(), 0.25f, Color32BGRA.Cyan.WithAlphaMult(0.25f), filled: true);

					if (!skip_inner_junctions && road_segment_to_junction_index.TryGetValue(c, out junction_index))
					{
						return true;
					}
				}

				if (road_segment_to_junction_index.TryGetValue(c, out junction_index))
				{
					return true;
				}
			}
			else if (dir_sign == 1)
			{
				while (++index <= points.Length)
				{
					b = c;
					c = b with { index = (byte)Maths.Clamp(index, 0, points.Length - 1) };

					//World.GetGlobalRegion().DrawDebugCircle(c.GetPosition(), 0.125f, Color32BGRA.Magenta.WithAlphaMult(0.25f), filled: true);

					if (!skip_inner_junctions && road_segment_to_junction_index.TryGetValue(c, out junction_index))
					{
						return true;
					}
				}
				
				if (road_segment_to_junction_index.TryGetValue(c, out junction_index))
				{
					return true;
				}
			}

			return false;
		}


		public static bool TryAdvanceJunction(Road.Segment a, Road.Segment b, Road.Segment c, int junction_index, out Road.Junction.Branch c_branch, out Road.Segment c_alt, out int c_alt_sign, out float c_alt_dot, float dot_min = 0.40f, float dot_max = 1.00f, bool ignore_limits = false)
		{
			var ok = false;
			c_alt = default;
			c_alt_dot = -1.00f;
			c_alt_sign = default;
			c_branch = default;

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

						//World.GetGlobalRegion().DrawDebugDir(j_pos, dir_tmp, Color32BGRA.Orange);

						if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
						{
							c_alt = new(j_segment.chain, (byte)(j_segment.index + 1));
							c_alt_dot = dot_tmp;
							c_alt_sign = 1;
							c_branch = new((ushort)junction_index, (byte)i, (sbyte)c_alt_sign);
						}
					}

					if (j_segment.index > 0)
					{
						var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
						var dot_tmp = Vector2.Dot(dir_ab, dir_tmp);

						//World.GetGlobalRegion().DrawDebugDir(j_pos, dir_tmp, Color32BGRA.Orange);

						if (dot_tmp > c_alt_dot && (ignore_limits || (dot_tmp >= dot_min && dot_tmp <= dot_max)))
						{
							c_alt = new(j_segment.chain, (byte)(j_segment.index - 1));
							c_alt_dot = dot_tmp;
							c_alt_sign = -1;
							c_branch = new((ushort)junction_index, (byte)i, (sbyte)c_alt_sign);
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

		public static Doodad.Renderer.Data? clipboard_doodad;

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

		public static bool show_governorates = true;
		public static bool show_prefectures = true;
		public static bool show_regions = true;
		public static bool show_locations = true;
		public static bool show_roads = true;
		public static bool show_rails = true;
		public static bool show_borders = true;
		public static bool show_fill = true;
		public static bool show_doodads = true;
		//public static BitField<Road.Type> filter_roads = new BitField<Road.Type>(Road.Type.Road, Road.Type.Rail, Road.Type.Marine, Road.Type.Air);
		//public static BitField<Road.Type> filter_roads_mask = new BitField<Road.Type>(Road.Type.Road, Road.Type.Rail, Road.Type.Marine, Road.Type.Air);

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnPreUpdate(Entity entity, [Source.Owned] ref Interactable.Data interactable) //, [Source.Owned] ref Body.Data body)
		{
			if (WorldMap.IsOpen)
			{
				interactable.show = WorldMap.selected_entity == entity
				//&& WorldMap.ts_last_draw.GetMilliseconds() <= 50 // TODO: shithack
				&& !interactable.flags.HasAll(Interactable.Flags.No_Window)
				&& interactable.window_size != default;
			}
		}
#endif
	}
}

