
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class Train
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			public Road.Segment segment_a;
			public Road.Segment segment_b;
			public Road.Segment segment_c;

			public Vector2 direction_old;
			public Vector2 direction;

			public float dot_current;
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
			//if (train.segment_a.chain.h_district == 0 || train.segment_a.index == 255 || train.segment_a.chain.index == 255)
			//{
			//	train.road_distance_current = 0.00f;
			//	train.road_distance_target = 0.00f;
			//	train.segment_a = WorldMap.GetNearestRoad("trphajzel", Road.Type.Rail, transform.position, out _);
			//	train.segment_b = new Road.Segment(train.segment_a.chain, (byte)(train.segment_a.index));

			//	return;
			//}

			if (!train.segment_a.IsValid()) return;
			if (!train.segment_b.IsValid()) return;

#if SERVER
return;
#endif

#if CLIENT
			region.DrawDebugCircle(train.segment_a.GetPosition(), 0.125f, Color32BGRA.Yellow, filled: true);
			region.DrawDebugCircle(train.segment_b.GetPosition(), 0.125f, Color32BGRA.Green, filled: true);

			region.DrawDebugDir(train.segment_a.GetPosition(), train.direction * Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition()), Color32BGRA.Yellow);
			//region.DrawDebugDir(train.segment_b.GetPosition(), train.direction, Color32BGRA.Magenta);
#endif

			//return;

			if (train.road_distance_current >= train.road_distance_target)
			{
				var direction_cached = train.direction;

				if (WorldMap.TryAdvance(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, out var junction_index, false))
				{
					//App.WriteLine("advanced road");

					if (junction_index != -1)
					{
						App.WriteLine($"optional junction {junction_index} ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

						if (WorldMap.TryAdvanceJunction(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, junction_index, Vector2.Dot(direction_cached, train.direction), out float dot_match, dot_min: train.dot_min, dot_max: train.dot_max))
						{
							App.WriteLine($"advanced optional junction ({dot_match} >= {train.dot_min} => {train.dot_current} <= {train.dot_max}) ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

							//return;
						}
						else
						{
							App.WriteLine($"skip optional junction ({dot_match} >= {train.dot_min} => {train.dot_current} <= {train.dot_max})");
						}
					}
					else
					{

					}
				}
				else
				{
					if (junction_index == -1)
					{
						App.WriteLine($"reversed direction a ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

						//train.sign = -train.sign;
						//train.road_distance_current = 0.00f;
						//train.direction *= -1.00f;

						return;

						//if (WorldMap.TryAdvance(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, out junction_index, false))
						//{

						//}

						//(train.segment_a, train.segment_b) = (train.segment_b, train.segment_a);
					}
					else
					{
						//App.WriteLine("reversed direction b");

						if (WorldMap.TryAdvanceJunction(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, junction_index, Vector2.Dot(direction_cached, train.direction), out float dot_match, dot_min: train.dot_min, dot_max: train.dot_max))
						{
							App.WriteLine($"advanced junction ({dot_match} >= {train.dot_min} => {train.dot_current} <= {train.dot_max}) ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

							//train.road_distance_current = 0.00f;
							//train.road_distance_target = Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition());
							//train.dot_current = dot_match;

							//train.road_distance_current = 0.00f;
							//train.road_distance_target = Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition());

							//return;

							//train.road_distance_current = 0.00f;
							//train.road_distance_target = Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition());

							//train.direction_old = direction_cached;
							//train.dot_current = Vector2.Dot(train.direction_old, train.direction);
						}
						else
						{
							App.WriteLine($"reversed direction b ({train.segment_a.chain.h_district}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_district}:{train.segment_b.chain.index}:{train.segment_b.index})");

							//train.sign = -train.sign;
							//train.road_distance_current = 0.00f;
							//train.direction *= -1.00f;

							return;


							//if (WorldMap.TryAdvance(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, out junction_index, false))
							//{

							//}

							//if (WorldMap.TryAdvance(ref train.segment_a, ref train.segment_b, ref train.direction, ref train.sign, out _, false))
							//{
							//	train.road_distance_current = 0.00f;
							//	train.road_distance_target = Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition());


							//	App.WriteLine("reversed direction");
							//}
							//else
							//{
							//	App.WriteLine("failed to advance junction");
							//}
						}
					}
				}


				train.road_distance_current = 0.00f;
				train.road_distance_target = Vector2.Distance(train.segment_a.GetPosition(), train.segment_b.GetPosition());

				train.direction_old = direction_cached;
				train.dot_current = Vector2.Dot(train.direction_old, train.direction);
			}

			if (train.segment_a != default && train.segment_b != default)
			{
				//var dir = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalized(out var dist);
				//train.road_distance_target = dist;

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

		public static bool TryAdvance(ref Road.Segment a, ref Road.Segment b, ref Vector2 dir_cached, ref int dir_sign, out int junction_index, bool skip_inner_junctions = false)
		{
			junction_index = -1;
			var is_at_end = false;
			var is_at_junction = false;

			if (a.chain == b.chain)
			{
				ref var road = ref b.GetRoad();
				var points = road.points.AsSpan();

				dir_sign = Maths.Sign(b.index - a.index);
				if (dir_sign == 0) dir_sign = b.index > 0 ? -1 : 1;

				is_at_end = dir_sign == -1 ? b.index == 0 : b.index >= points.Length - 1;

				if ((is_at_end || !skip_inner_junctions) && road_segment_to_junction_index.TryGetValue(b, out var junction_index_tmp))
				{
					junction_index = junction_index_tmp;
					is_at_junction = true;
					//is_at_end = true;
					//var junction = road_junctions[junction_index_v];
				}
				else
				{

				}


				//var pos_a = a.GetPosition();
				//var pos_b = b.GetPosition();

				a = b;
				b = new Road.Segment(b.chain, (byte)Maths.Clamp(b.index + dir_sign, 0, points.Length - 1));


				if (!is_at_end)
				{
					dir_cached = (points[b.index] - points[a.index]).GetNormalizedFast();
				}
			}
			else
			{
				ref var road = ref b.GetRoad();
				var points = road.points.AsSpan();


				is_at_end = dir_sign == -1 ? b.index == 0 : b.index >= points.Length - 1;


				a = b;
				b = new Road.Segment(b.chain, (byte)Maths.Clamp(b.index + dir_sign, 0, points.Length - 1));


				if (!is_at_end)
				{

					dir_cached = (points[b.index] - points[a.index]).GetNormalizedFast();
				}

				//var dot_max = float.MaxValue;

				//if (b.index > 0)
			}

			return !is_at_end; // && !is_at_junction;
		}

		public static bool TryAdvanceJunction(ref Road.Segment a, ref Road.Segment b, ref Vector2 dir_cached, ref int dir_sign, int junction_index, float dot_current, out float match_dot, float dot_min = 0.40f, float dot_max = 1.00f)
		{
			var ok = false;
			match_dot = -1.00f;

			if ((uint)junction_index < road_junctions.Count)
			{
				var junction = road_junctions[junction_index];
				var junction_segments = junction.segments.Slice(junction.segments_count);

				ref var road_a = ref a.GetRoad();
				ref var road_b = ref b.GetRoad();

				var points_a = road_a.points.AsSpan();
				var points_b = road_b.points.AsSpan();

				var type = road_a.type;

				//var match_dot = dot_current;
				var match_segment_a = default(Road.Segment);
				var match_segment = default(Road.Segment);

				var dir_sign_tmp = dir_sign;
				var dir_tmp = dir_cached;


				var match_dot_max = -1.00f;

				////ref var road_a = ref a.GetRoad();
				//ref var road = ref b.GetRoad();

				////var points_a = road_a.points.AsSpan();
				//var points = road.points.AsSpan();

				//var type = road.type;

				//var match_dot = dot_threshold;
				//var match_segment = default(Road.Segment);

				//var dir_sign_tmp = dir_sign;
				//var dir_tmp = dir_cached;

				for (var i = 0; i < junction_segments.Length; i++)
				{
					ref var j_segment = ref junction_segments[i];
					if (j_segment == a) continue;

					ref var j_road = ref j_segment.GetRoad();
					if (j_road.IsNull() || j_road.type != type) continue;

					var j_points = j_road.points.AsSpan();

					if (j_segment.index < j_points.Length - 1)
					{
						var j_dir = (j_points[j_segment.index + 1] - points_a[a.index]).GetNormalizedFast();
						var j_dot = Vector2.Dot(dir_cached, j_dir);

						match_dot_max = MathF.Max(j_dot, match_dot_max);

						if (j_dot >= dot_min && j_dot > match_dot && j_dot <= dot_max)
						{
							match_dot = j_dot;
							match_segment = new(j_segment.chain, (byte)(j_segment.index + 1));
							match_segment_a = j_segment;
							dir_tmp = j_dir;
							dir_sign_tmp = 1;
						}
					}

					if (j_segment.index > 0)
					{
						var j_dir = (j_points[j_segment.index - 1] - points_a[a.index]).GetNormalizedFast();
						var j_dot = Vector2.Dot(dir_cached, j_dir);

						match_dot_max = MathF.Max(j_dot, match_dot_max);

						if (j_dot >= dot_min && j_dot > match_dot && j_dot <= dot_max)
						{
							match_dot = j_dot;
							match_segment = new(j_segment.chain, (byte)(j_segment.index - 1));
							match_segment_a = j_segment;
							dir_tmp = j_dir;
							dir_sign_tmp = -1;
						}
					}
				}

				if (match_segment != default)
				{
					//a = b;
					a = match_segment_a;
					b = match_segment;
					dir_cached = (b.GetPosition() - a.GetPosition()).GetNormalizedFast();
					dir_sign = dir_sign_tmp;

					ok = true;
				}
				else
				{
					match_dot = match_dot_max;
				}




				//a = b;
				//b = new Road.Segment(b.chain, (byte)Maths.Clamp(b.index + dir_sign, 0, points.Length - 1));

				//var dot_max = float.MaxValue;

				//if (b.index > 0)
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

