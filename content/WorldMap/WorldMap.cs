
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		public static Dictionary<int, Road.Segment> road_segments = new(256);
		public static Dictionary<int, List<Road.Segment>> road_segments_overlapped = new(256);
		public static Dictionary<Road.Segment, int> road_segment_to_junction_index = new(256);
		public static List<Road.Junction> road_junctions = new(128);

		public static float road_junction_threshold = 0.250f;
		public const float km_per_unit = 2.00f;

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

