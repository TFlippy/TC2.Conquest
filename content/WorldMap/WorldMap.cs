
using System.Runtime.InteropServices;
using System.Text;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
#if CLIENT
		public static Vector2 worldmap_offset_current_snapped;
		public static Vector2 worldmap_offset_current;
		public static Vector2 worldmap_offset_target;
		public static Vector2 momentum;

		public static float worldmap_zoom_current = 6.00f;
		public static float worldmap_zoom_target = 6.00f;

		public static Vector2 worldmap_window_size = new Vector2(1024, 1024);
		public static Vector2 worldmap_window_offset = new Vector2(0, 0);

		public static Dictionary<int, Road.Segment> road_segments = new(256);
		public static Dictionary<int, List<Road.Segment>> road_segments_overlapped = new(256);
		public static List<Road.Junction> road_junctions = new(64);
		public static Dictionary<Road.Segment, int> road_segment_to_junction_index = new(256);
		public static float road_junction_threshold = 0.250f;

		public static HashSet<int> junctions_visited = new HashSet<int>();
		public static Queue<int> junctions_queue = new Queue<int>();

		public static float rotation;

		public static int? edit_points_index;
		public static short2[] edit_points_s32;
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

		public static void DrawConnectedRoads(Road.Segment road_segment, ref Matrix3x2 mat_l2c, float zoom, int iter_max = 10)
		{
			//var thickness = road_segment.GetRoad().scale * 0.50f;
			var alpha = 0.25f;

			var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
			//junctions_span.GetNearestIndex(mouse, out var junction_index, out var junction_dist_sq, (ref Road.Junction junction) => ref junction.pos);

			//if (junction_dist_sq < 1.00f.Pow2())
			{
				//ref var junction = ref junctions_span[junction_index];

				junctions_queue.Clear();
				junctions_visited.Clear();

				//hs_visited.Add(junction_index);

				//var type = junction.segments[0].GetRoad().type;

				//var iter_max = 10;

				//junctions_queue.Enqueue(junction_index);

				ref var road = ref road_segment.GetRoad();
				var type = road.type;

				if (road_segment_to_junction_index.TryGetValue(road_segment, out var junction_index))
				{
					junctions_queue.Enqueue(junction_index);
				}
				else
				{
					DrawSegment(junctions_visited, junctions_queue, ref road_segment, ref mat_l2c, zoom, type, Color32BGRA.Green.WithAlphaMult(alpha));
				}

				for (var i = 0; i < iter_max; i++)
				{
					var count = junctions_queue.Count;
					for (var j = 0; j < count; j++)
					{
						var junction_index_new = junctions_queue.Dequeue();
						DrawJunction(junctions_visited, junctions_queue, junctions_span, junction_index_new, ref mat_l2c, zoom, i, iter_max, type, alpha);
					}
				}

				//DrawJunction(hs_visited, queue, junctions_span, ref junction.segments[0], ref mat_l2c, zoom, 0, iter_max);
				static void DrawJunction(HashSet<int> hs_visited, Queue<int> queue, Span<Road.Junction> junctions_span, int junction_index, ref Matrix3x2 mat_l2c, float zoom, int depth, int iter_max, Road.Type type, float alpha)
				{
					var color = Color32BGRA.FromHSV((1.00f - ((float)depth / (float)iter_max)) * 2.00f, 1.00f, 1.00f).WithAlphaMult(alpha);

					ref var junction = ref junctions_span[junction_index];
					//GUI.DrawCircle(Vector2.Transform(junction.pos, mat_l2c), 0.125f * zoom, color: color, segments: 12, layer: GUI.Layer.Window);

					var segments_span = junction.segments.Slice(junction.segments_count);
					foreach (ref var segment_base in segments_span)
					{
						DrawSegment(junctions_visited, junctions_queue, ref segment_base, ref mat_l2c, zoom, type, color);
					}
				}

				static void DrawSegment(HashSet<int> hs_visited, Queue<int> queue, ref Road.Segment segment_base, ref Matrix3x2 mat_l2c, float zoom, Road.Type type, Color32BGRA color)
				{
					ref var road = ref segment_base.GetRoad();
					if (road.type != type) return;

					var thickness = road.scale * 0.50f;

					var pos = segment_base.GetPosition();
					var road_points_span = road.points.AsSpan();

					//GUI.DrawCircleFilled(Vector2.Transform(pos, mat_l2c), 0.125f * zoom * 0.50f, color: color, segments: 4, layer: GUI.Layer.Window);
					var depth_offset = Vector2.Zero;

					{
						var pos_last = pos;
						for (var i = segment_base.index + 1; i < road_points_span.Length; i++)
						{
							var segment = new Road.Segment(segment_base.chain, (byte)i);

							if (hs_visited.Contains(segment.GetHashCode())) break;
							hs_visited.Add(segment.GetHashCode());

							GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, thickness: thickness * zoom, layer: GUI.Layer.Window);
							pos_last = road_points_span[i];

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								queue.Enqueue(junction_index_new);
								break;
							}
						}
					}

					{
						var pos_last = pos;
						for (var i = segment_base.index - 1; i >= 0; i--)
						{
							var segment = new Road.Segment(segment_base.chain, (byte)i);

							if (hs_visited.Contains(segment.GetHashCode())) break;
							hs_visited.Add(segment.GetHashCode());

							GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, thickness: thickness * zoom, layer: GUI.Layer.Window);
							pos_last = road_points_span[i];

							if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
							{
								queue.Enqueue(junction_index_new);
								break;
							}
						}
					}

					if (hs_visited.Contains(segment_base.GetHashCode())) return;
					hs_visited.Add(segment_base.GetHashCode());
				}
			}
		}

		public static void RecalculateRoads()
		{
			road_segments.Clear();
			road_segments_overlapped.Clear();
			road_junctions.Clear();
			road_segment_to_junction_index.Clear();

			{
				var ts = Timestamp.Now();
				foreach (var asset in IDistrict.Database.GetAssets())
				{
					if (asset.id == 0) continue;

					var h_district = asset.GetHandle();

					var roads_span = asset.data.roads.AsSpan();
					for (var road_index = 0; road_index < roads_span.Length; road_index++)
					{
						ref var road = ref roads_span[road_index];

						var points_span = road.points.AsSpan();
						for (var point_index = 0; point_index < points_span.Length; point_index++)
						{
							ref var point = ref points_span[point_index];

							var pos_grid = new short2((short)point.X, (short)point.Y);
							var pos_key = Unsafe.BitCast<short2, int>(pos_grid);

							var segment = new Road.Segment(h_district, (byte)road_index, (byte)point_index);

							//road_segments_overlapped

							if (!road_segments.TryAdd(pos_key, segment))
							{
								var segment_other = road_segments[pos_key];
								//if (segment_other.chain == segment.chain) continue;

								if (!road_segments_overlapped.TryGetValue(pos_key, out var segments_list))
								{
									segments_list = road_segments_overlapped[pos_key] = new(4);
									segments_list.Add(road_segments[pos_key]);
								}

								segments_list.Add(segment);
							}
						}
					}
				}

				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Rasterized road lines in {ts_elapsed:0.0000} ms.");
			}

			{
				var ts = Timestamp.Now();
				if (road_segments_overlapped.Count > 0)
				{
					var road_junction_threshold_sq = road_junction_threshold * road_junction_threshold;

					foreach (var pair in road_segments_overlapped)
					{
						var road_list = pair.Value;

						//var road_list_count = road_list.Count;
						Span<Road.Segment> road_list_span = stackalloc Road.Segment[road_list.Count];
						road_list.CopyTo(road_list_span);

						//Span<int> indices = stackalloc int[road_list.Count];

						while (road_list_span.Length > 0)
						{
							var junction = new Road.Junction();
							junction.pos = road_list_span[0].GetPosition();
							junction.segments[junction.segments_count++] = road_list_span[0];

							road_list_span.RemoveAtSwapback(0, resize: true);

							for (var i = 0; i < road_list_span.Length;)
							{
								ref var road = ref road_list_span[i];
								var pos = road.GetPosition();

								if (Vector2.DistanceSquared(junction.pos, pos) < road_junction_threshold_sq)
								{
									junction.segments[junction.segments_count++] = road;
									junction.pos = Vector2.Lerp(junction.pos, pos, 0.50f);
									road_list_span.RemoveAtSwapback(i, resize: true);
								}
								else
								{
									i++;
								}
							}

							if (junction.segments_count > 1)
							{
								var junction_index = road_junctions.Count;
								road_junctions.Add(junction);

								for (var j = 0; j < junction.segments_count; j++)
								{
									road_segment_to_junction_index[junction.segments[j]] = junction_index;
								}
							}
						}
					}
				}
				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"Calculated road junctions in {ts_elapsed:0.0000} ms.");
			}
		}



		public enum EditorMode: uint
		{
			None = 0,

			Province,
			District,
			Location,
			Doodad,
			Roads,
			Junctions,

			Max
		}

		// TODO: implement a faster lookup
		public static ILocation.Handle GetNearestLocation(Vector2 position, out float distance_sq)
		{
			var nearest_handle = default(ILocation.Handle);
			var nearest_distance_sq = float.MaxValue;

			foreach (var asset in ILocation.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();

				var distance_sq_tmp = Vector2.DistanceSquared((Vector2)asset_data.point, position);
				if (distance_sq_tmp < nearest_distance_sq)
				{
					nearest_handle = asset;
					nearest_distance_sq = distance_sq_tmp;
				}
			}

			distance_sq = nearest_distance_sq;
			return nearest_handle;
		}

		// TODO: implement a faster lookup, especially for this
		public static ref IScenario.Doodad GetNearestDoodad(Vector2 position, Span<IScenario.Doodad> span, out int index, out float distance_sq)
		{
			var nearest_index = int.MaxValue;
			var nearest_distance_sq = float.MaxValue;

			for (var i = 0; i < span.Length; i++)
			{
				ref var doodad = ref span[i];

				var distance_sq_tmp = Vector2.DistanceSquared(doodad.position, position);
				if (distance_sq_tmp < nearest_distance_sq)
				{
					nearest_index = i;
					nearest_distance_sq = distance_sq_tmp;
				}
			}

			index = nearest_index;
			distance_sq = nearest_distance_sq;

			if ((uint)index < span.Length) return ref span[nearest_index];
			else return ref Unsafe.NullRef<IScenario.Doodad>();
		}

		// TODO: implement a faster lookup
		public static Road.Segment GetNearestRoad(IDistrict.Handle h_district, Road.Type type, Vector2 position, out float distance_sq)
		{
			var road_points_distance_sq = float.MaxValue;
			var road_point_index = int.MaxValue;
			var road_index = int.MaxValue;

			ref var district_data = ref h_district.GetData();
			if (district_data.IsNotNull())
			{
				var span_roads = district_data.roads.AsSpan();
				for (var i = 0; i < span_roads.Length; i++)
				{
					ref var road = ref span_roads[i];
					if (road.type != type) continue;

					var points = road.points.AsSpan();
					if (!points.IsEmpty)
					{
						points.GetNearestIndex(position, out var road_nearest_index_tmp, out var road_nearest_distance_sq_tmp);

						if (road_nearest_distance_sq_tmp < road_points_distance_sq)
						{
							road_points_distance_sq = road_nearest_distance_sq_tmp;
							road_point_index = road_nearest_index_tmp;
							road_index = i;
						}
					}
				}
			}

			distance_sq = road_points_distance_sq;
			return new Road.Segment(h_district, (byte)road_index, (byte)road_point_index);
		}

		//// TODO: implement a faster lookup, especially for this
		//public static void GetNearestIndex(Vector2 position, Span<short2> span, out int index, out float distance_sq)
		//{
		//	var nearest_index = int.MaxValue;
		//	var nearest_distance_sq = float.MaxValue;

		//	for (var i = 0; i < span.Length; i++)
		//	{
		//		var pos_tmp = (Vector2)span[i];

		//		var distance_sq_tmp = Vector2.DistanceSquared(pos_tmp, position);
		//		if (distance_sq_tmp < nearest_distance_sq)
		//		{
		//			nearest_index = i;
		//			nearest_distance_sq = distance_sq_tmp;
		//		}
		//	}

		//	index = nearest_index;
		//	distance_sq = nearest_distance_sq;
		//}

		//// TODO: implement a faster lookup, especially for this
		//public static void GetNearestIndex(Vector2 position, Span<Vector2> span, out int index, out float distance_sq)
		//{
		//	var nearest_index = int.MaxValue;
		//	var nearest_distance_sq = float.MaxValue;

		//	for (var i = 0; i < span.Length; i++)
		//	{
		//		var pos_tmp = span[i];

		//		var distance_sq_tmp = Vector2.DistanceSquared(pos_tmp, position);
		//		if (distance_sq_tmp < nearest_distance_sq)
		//		{
		//			nearest_index = i;
		//			nearest_distance_sq = distance_sq_tmp;
		//		}
		//	}

		//	index = nearest_index;
		//	distance_sq = nearest_distance_sq;
		//}

		public static void DrawOutlineShader(Span<short2> points, Color32BGRA color, float thickness, Texture.Handle h_texture, bool loop = true)
		{
			var count = points.Length;

			var last_vert = default(short2);
			for (var i = 0; i < (count + 1); i++)
			{
				if (!loop && i >= count) break;

				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = (Vector2)last_vert;
					var b = (Vector2)vert;

					IScenario.WorldMap.Renderer.Add(new()
					{
						a = a,
						b = b,
						color = color,
						h_texture = h_texture,
						thickness = thickness,
						uv_scale = Vector2.Distance(a, b) * 0.5f
					});

					//GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void DrawOutlineShader(Span<Vector2> points, Color32BGRA color, float thickness, Texture.Handle h_texture, bool loop = true)
		{
			var count = points.Length;

			var last_vert = default(Vector2);
			for (var i = 0; i < (count + 1); i++)
			{
				if (!loop && i >= count) break;

				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = last_vert;
					var b = vert;

					IScenario.WorldMap.Renderer.Add(new()
					{
						a = a,
						b = b,
						color = color,
						h_texture = h_texture,
						thickness = thickness,
						uv_scale = Vector2.Distance(a, b) * 0.5f
					});

					//GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void DrawOutline(Matrix3x2 mat_l2c, float zoom, Span<short2> points, Color32BGRA color, float thickness, float cap_size, Texture.Handle h_texture)
		{
			var count = points.Length;

			var last_vert = default(short2);
			for (var i = 0; i < (count + 1); i++)
			{
				var index = i % count;
				ref var vert = ref points[index];
				if (i > 0)
				{
					var a = (Vector2)last_vert;
					var b = (Vector2)vert;

					var ta = Vector2.Transform(a, mat_l2c);
					var tb = Vector2.Transform(b, mat_l2c);

					GUI.DrawLineTexturedCapped(ta, tb, h_texture, color: color, thickness: thickness * zoom * 2, cap_size: cap_size, layer: GUI.Layer.Window);
				}
				last_vert = vert;
			}
		}

		public static void Rescale()
		{
			foreach (var asset in ILocation.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				asset_data.point *= 2;

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IProvince.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				foreach (ref var point in asset_data.points.AsSpan())
				{
					point *= 2;
				}

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IDistrict.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();
				foreach (ref var point in asset_data.points.AsSpan())
				{
					point *= 2;
				}

				foreach (ref var road in asset_data.roads.AsSpan())
				{
					foreach (ref var point in road.points.AsSpan())
					{
						point *= 2;
					}
				}

				hs_pending_asset_saves.Add(asset);
			}

			foreach (var asset in IScenario.Database.GetAssets())
			{
				if (asset.id == 0) continue;

				ref var asset_data = ref asset.GetData();

				foreach (ref var doodad in asset_data.doodads.AsSpan())
				{
					doodad.position *= 2;
				}

				hs_pending_asset_saves.Add(asset);
			}
		}

		public static void Draw(Vector2 size)
		{
			ref var world = ref Client.GetWorld();
			if (world.IsNull()) return;

			//return;
			var is_loading = Client.IsLoadingRegion();
			//var use_renderer = true;

			//if (!Client.IsLoadingRegion())

			//if (!is_loading)

			if (enable_renderer)
			{
				IScenario.WorldMap.Renderer.Clear();
				IScenario.Doodad.Renderer.Clear();
			}

			var mouse = GUI.GetMouse();
			var kb = GUI.GetKeyboard();

			var zoom = MathF.Pow(2.00f, worldmap_zoom_current);
			var zoom_inv = 1.00f / zoom;

			var mat_l2c = Matrix3x2.Identity;
			var mat_c2l = Matrix3x2.Identity;

			using (var group_canvas = GUI.Group.New(size))
			{
				var rect = group_canvas.GetInnerRect();

				using (GUI.ID.Push("worldmap"))
				{
					GUI.DrawWindowBackground(GUI.tex_window_character);
					//sb.Clear();

					ref var scenario_data = ref h_world.GetData(out var scenario_asset);
					var disable_input = false;

					if (is_loading)
					{
						mouse = default;
						kb = default;
					}

					disable_input |= gizmo.IsActive() || gizmo.IsHovered();
					if (disable_input)
					{
						//mouse.ClearKeys();
						//kb.Clear();

						dragging = false;
						mouse.SetKeyState(~(Mouse.Key.ScrollUp | Mouse.Key.ScrollDown), false);
						kb.SetKeyState(~(Keyboard.Key.MoveDown | Keyboard.Key.MoveLeft | Keyboard.Key.MoveRight | Keyboard.Key.MoveUp | Keyboard.Key.LeftControl | Keyboard.Key.C | Keyboard.Key.V), false);
					}

					if (editor_mode != EditorMode.None && !gizmo.IsHovered() && GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false) && edit_asset != null && GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
					{
						edit_asset = null;
						h_selected_location = default;
						edit_doodad_index = null;
					}

					var rect_center = rect.GetPosition();

					using (GUI.Clip.Push(rect))
					{
						var scale_b = IScenario.WorldMap.scale_b;
						var snap_delta = (worldmap_offset_current_snapped - worldmap_offset_current) * 1.0f;

						mat_l2c = Maths.TRS3x2((worldmap_offset_current * -zoom) + rect.GetPosition(new(0.50f)) - snap_delta, rotation, new Vector2(zoom));
						Matrix3x2.Invert(mat_l2c, out mat_c2l);

						var mat_l2c2 = Maths.TRS3x2(rect.GetPosition(new Vector2(0.50f)), rotation, new Vector2(1));
						Matrix3x2.Invert(mat_l2c2, out var mat_c2l2);

						var snap_delta_canvas = snap_delta * zoom;

						var tex_scale = enable_renderer ? (IScenario.WorldMap.worldmap_size.X / scale_b) * zoom : 16.00f;
						var tex_scale_inv = 1.00f / tex_scale;

						var color_grid = new Color32BGRA(0xff4eabb5);
						//GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv) + new Vector2(0.50f), clip: false);

						if (enable_renderer)
						{
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) - new Vector2(0.50f), clip: false);
							//GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);

							GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) / 16, uv_1: Vector2.Transform(rect.b, mat_c2l) / 16, clip: false, color: color_grid.WithAlphaMult(0.25f));

							GUI.DrawTexture("_worldmap", rect, GUI.Layer.Window, uv_0: (Vector2.Transform(rect.a - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), uv_1: (Vector2.Transform(rect.b - snap_delta_canvas, mat_c2l2) * tex_scale_inv) + new Vector2(0.50f), clip: false);
						}
						else
						{
							GUI.DrawTexture(h_texture_bg_00, rect, GUI.Layer.Window, uv_0: Vector2.Transform(rect.a, mat_c2l) * tex_scale_inv, uv_1: Vector2.Transform(rect.b, mat_c2l) * tex_scale_inv, clip: false, color: color_grid.WithAlphaMult(0.10f));
						}

						var mouse_pos = GUI.GetMousePosition();
						var mouse_local = Vector2.Transform(mouse_pos, mat_c2l);
						var mouse_local_snapped = mouse_local;
						mouse_local_snapped.Snap(1.00f / scale_b, out mouse_local_snapped);

						var tex_line_district = h_texture_line_00;
						var tex_line_province = h_texture_line_01;

						#region Districts
						//if (editor_mode != EditorMode.Doodad)
						{
							foreach (var asset in IDistrict.Database.GetAssets())
							{
								if (asset.id == 0) continue;
								ref var asset_data = ref asset.GetData();

								var points = asset_data.points.AsSpan();
								if (!points.IsEmpty)
								{
									var pos_center = Vector2.Zero;

									Span<Vector2> points_t_span = stackalloc Vector2[points.Length];
									for (var i = 0; i < points.Length; i++)
									{
										var point = (Vector2)points[i];
										pos_center += point;
										var point_t = Vector2.Transform(point, mat_l2c);

										points_t_span[i] = point_t;
									}
									pos_center /= points.Length;
									pos_center += asset_data.offset;

									if (show_districts && show_fill) GUI.DrawPolygon(points_t_span, asset_data.color_fill with { a = 50 }, GUI.Layer.Window);

									//DrawOutline(points, asset_data.color_border.WithAlphaMult(0.50f), 0.100f);
									if (enable_renderer)
									{
										if (show_districts && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, asset_data.border_scale * 0.50f, 2.00f, asset_data.h_texture_border);

										//if (show_roads)
										{
											var roads_span = asset_data.roads.AsSpan();
											foreach (ref var road in roads_span)
											{
												if (road.type == Road.Type.Road && !show_roads) continue;
												if (road.type == Road.Type.Rail && !show_rails) continue;

												DrawOutlineShader(road.points, road.color_border, road.scale, road.h_texture, loop: false);
											}
										}
									}
									else
									{
										if (show_districts && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f * 0.75f, 4.00f, asset_data.h_texture_border);
									}

									//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 1.00f * zoom, color: GUI.font_color_title.WithAlphaMult(1.00f), layer: GUI.Layer.Window);
									if (show_districts) GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(pos_center, mat_l2c), pivot: new(0.50f, 0.50f), font: GUI.Font.Superstar, size: 0.75f * zoom * asset_data.size, color: asset_data.color_fill.WithColorMult(0.32f).WithAlphaMult(0.30f), layer: GUI.Layer.Window);
								}
							}
						}
						#endregion

						#region Provinces
						foreach (var asset in IProvince.Database.GetAssets())
						{
							if (asset.id == 0) continue;
							ref var asset_data = ref asset.GetData();

							var points = asset_data.points.AsSpan();
							if (!points.IsEmpty)
							{
								if (enable_renderer)
								{
									if (show_provinces && show_borders) DrawOutlineShader(points, asset_data.color_border, asset_data.border_scale, asset_data.h_texture_border);
								}
								else
								{
									if (show_provinces && show_borders) DrawOutline(mat_l2c, zoom, points, asset_data.color_border, 0.125f, 0.25f, asset_data.h_texture_border);
								}

								var pos_center = Vector2.Zero;
								var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);

								for (var i = 0; i < points.Length; i++)
								{
									var point = (Vector2)points[i];
									pos_center += point;
								}
								pos_center /= points.Length;
							}
						}
						#endregion

						#region Regions
						ref var world_info = ref Client.GetWorldInfo();
						if (world_info.IsNotNull())
						{
							//if (world_info.regions != null)
							{
								var mod_context = App.GetModContext();

								for (var i = 1; i < Region.max_count; i++)
								{
									using (GUI.ID.Push(i + 10000))
									{
										//ref var map_info = ref world_info.[i];

										ref var region_info = ref World.GetRegionInfo((byte)i);

										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (region_info.IsValid() && map_info.IsNotNull())
										{
											var icon_size = 0.75f; // * (zoom_inv * 128);
											var color = GUI.font_color_title;
											var color_frame = GUI.col_frame;
											var color_thumbnail = GUI.col_white;

											var map_pos = (Vector2)map_info.point;
											ref var location_data = ref map_info.h_location.GetData();
											if (location_data.IsNotNull())
											{
												map_pos = (Vector2)location_data.point;
											}

											var rect_text = AABB.Centered(Vector2.Transform(map_pos + map_info.text_offset, mat_l2c), new Vector2(icon_size * zoom * 0.50f));
											var rect_icon = AABB.Centered(Vector2.Transform(map_pos + map_info.icon_offset, mat_l2c), new Vector2(icon_size * zoom * 1.00f));
											var is_selected = selected_region_id == i || (location_data.IsNotNull() && h_selected_location == map_info.h_location);

											var alpha = 0.50f;
											if (is_selected)
											{
												alpha = 1.00f;
												color_frame = GUI.col_white;
												selected_region_id = (byte)i;
											}

											if (show_regions)
											{
												var map_asset = mod_context.GetMap(region_info.map);
												if (map_asset != null)
												{
													var tex_thumbnail = map_asset.GetThumbnail();
													if (tex_thumbnail != null)
													{
														GUI.DrawTexture(tex_thumbnail.Identifier, rect_icon, GUI.Layer.Window, color: color_thumbnail.WithAlphaMult(alpha));
													}

													GUI.DrawBackground(GUI.tex_frame_white, rect_icon, padding: new(2), color: color_frame.WithAlphaMult(alpha));
												}
											}
										}
									}
								}
							}
						}
						#endregion

						#region Locations
						foreach (var asset in ILocation.Database.GetAssets())
						{
							if (asset.id == 0) continue;

							ref var asset_data = ref asset.GetData();

							var pos = (Vector2)asset_data.point;
							var scale = 0.500f;
							var asset_scale = Maths.Clamp(asset_data.size, 0.50f, 1.00f);

							var rect_text = AABB.Centered(Vector2.Transform(pos + asset_data.text_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));
							var rect_icon = AABB.Centered(Vector2.Transform(pos + asset_data.icon_offset, mat_l2c), new Vector2(scale * zoom * asset_scale * 1.50f));

							var is_selected = h_selected_location == asset;
							var is_pressed = GUI.ButtonBehavior(asset_data.name, rect_icon, out var is_hovered, out var is_held);

							var color = (is_selected || is_hovered) ? Color32BGRA.White : asset_data.color;

							if (is_hovered)
							{
								GUI.SetCursor(App.CursorType.Hand, 1000);

								if (is_pressed)
								{
									selected_region_id = 0;

									if (is_selected)
									{
										h_selected_location = default;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f, pitch: 0.80f);
									}
									else
									{
										h_selected_location = asset;
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
									}
									// Client.RequestSetActiveRegion((byte)i);
								}
							}

							//GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + new Vector2(0.00f, -0.625f * asset_scale) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.75f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

							if (show_locations)
							{
								GUI.DrawSpriteCentered(asset_data.icon, rect_icon, layer: GUI.Layer.Window, 0.125f * MathF.Max(scale * zoom * asset_scale, 16), color: color);
								GUI.DrawTextCentered(asset_data.name_short, Vector2.Transform(((Vector2)asset_data.point) + asset_data.text_offset, mat_l2c), pivot: new(0.50f, 0.50f), color: GUI.font_color_title, font: GUI.Font.Superstar, size: 0.50f * MathF.Max(asset_scale * zoom * scale, 16), layer: GUI.Layer.Window, box_shadow: true);

								if (is_selected || is_hovered)
								{
									var ts = Timestamp.Now();
									var nearest_road = GetNearestRoad(asset_data.h_district, Road.Type.Road, (Vector2)asset_data.point, out var nearest_road_dist_sq);
									var nearest_rail = GetNearestRoad(asset_data.h_district, Road.Type.Rail, (Vector2)asset_data.point, out var nearest_rail_dist_sq);
									var ts_elapsed = ts.GetMilliseconds();

									if (nearest_road_dist_sq <= 2.00f.Pow2())
									{
										GUI.DrawCircleFilled(Vector2.Transform(nearest_road.GetPosition(), mat_l2c), 0.125f * zoom * 0.50f, Color32BGRA.Yellow, 8, GUI.Layer.Window);
										if (Maths.IsInDistance(mouse_local, nearest_road.GetPosition(), 0.25f))
										{
											DrawConnectedRoads(nearest_road, ref mat_l2c, zoom, iter_max: 6);
										}
									}

									if (nearest_rail_dist_sq <= 2.00f.Pow2())
									{
										GUI.DrawCircleFilled(Vector2.Transform(nearest_rail.GetPosition(), mat_l2c), 0.125f * zoom * 0.50f, Color32BGRA.Orange, 8, GUI.Layer.Window);
										if (Maths.IsInDistance(mouse_local, nearest_rail.GetPosition(), 0.25f))
										{
											DrawConnectedRoads(nearest_rail, ref mat_l2c, zoom, iter_max: 20);
										}
									}
									GUI.Text($"nearest in {ts_elapsed:0.0000} ms");
								}
							}

							GUI.FocusableAsset(asset.GetHandle(), rect: rect_icon);
						}
						#endregion

						#region Overlays
						GUI.DrawTexture(GUI.tex_vignette, rect, layer: GUI.Layer.Window, color: Color32BGRA.White.WithAlphaMult(0.30f));
						//GUI.DrawSpriteCentered(new Sprite(h_texture_icons, 72, 72, 0, 1), rect: AABB.Centered(Vector2.Transform(mouse_local_snapped, mat_l2c), new Vector2(0.25f)), layer: GUI.Layer.Window, scale: 0.125f * 0.50f * zoom, color: Color32BGRA.Black.WithAlphaMult(1));
						//GUI.DrawTextCentered($"Zoom: {zoom:0.00}x\ndelta: [{snap_delta.X:0.000000}, {snap_delta.Y:0.000000}]\ndelta.c: [{snap_delta_canvas.X:0.000000}, {snap_delta_canvas.Y:0.000000}]\ncam: [{worldmap_offset_target.X:0.000000}, {worldmap_offset_target.Y:0.000000}]\ncam.s: [{worldmap_offset_current_snapped.X:0.000000}, {worldmap_offset_current_snapped.Y:0.000000}]\nmouse.l: [{mouse_local.X:0.0000}, {mouse_local.Y:0.0000}]\nmouse: [{mouse_pos.X:0.00}, {mouse_pos.Y:0.00}]", position: rect.GetPosition(new(1, 1)), new(1, 1), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground);
						#endregion

						if (editor_mode != EditorMode.None)
						{
							if (road_junctions.Count > 0)
							{
								var ts = Timestamp.Now();
								foreach (var junction in road_junctions)
								{
									var pos_c = Vector2.Transform(junction.pos, mat_l2c);

									GUI.DrawCircleFilled(pos_c, 0.125f * zoom, junction.segments_count > 1 ? Color32BGRA.Green : Color32BGRA.Yellow.WithAlphaMult(0.250f), 4, GUI.Layer.Window);
									GUI.DrawTextCentered($"{junction.segments_count}", pos_c, layer: GUI.Layer.Window);
								}
								var ts_elapsed = ts.GetMilliseconds();
								GUI.DrawTextCentered($"{ts_elapsed:0.000} ms", GUI.CanvasSize * 0.75f, layer: GUI.Layer.Foreground);
							}
						}

						var hovered = GUI.IsHoveringRect(rect, allow_blocked: false, allow_overlapped: false, root_window: false, child_windows: false);

						#region Editor
						DrawEditor(ref rect, ref scenario_data, scenario_asset, ref mouse, ref kb, zoom, ref mat_l2c, ref mouse_local, hovered);
						#endregion

						#region Camera
						var mouse_delta = Vector2.Zero;
						mouse_pos_old = mouse_pos_new;
						mouse_pos_new = mouse_pos;

						if (hovered)
						{
							//if (mouse.GetKey(Mouse.Key.Left) || mouse.GetKeyDown(Mouse.Key.Left))
							if (mouse.GetKeyDown(Mouse.Key.Left))
							{
								//worldmap_offset += mouse.GetDelta() * scale_canvas / zoom;

								mouse_pos_old = mouse_pos;
								mouse_pos_new = mouse_pos;

								dragging = true;
							}
						}

						mouse_delta = mouse_pos_old - mouse_pos_new;

						if (mouse.GetKeyUp(Mouse.Key.Left))
						{
							dragging = false;
						}

						//if (!disable_input)
						{
							if (dragging)
							{
								//worldmap_offset += mouse_delta * zoom_inv;
								//momentum = mouse_delta; // Vector2.Lerp(momentum, mouse_delta, 0.80f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
								if (enable_momentum) momentum = Vector2.Lerp(momentum, mouse_delta, 0.50f); // mouse_delta.LengthSquared() > 0.50f || mouse.GetKeyUp(Mouse.Key.Left) ? Vector2.Lerp(momentum, mouse_delta, 0.50f) : Vector2.Zero;
								worldmap_offset_target += (mouse_delta * zoom_inv); // + (momentum * zoom_inv * 0.10f);
								worldmap_offset_current = Vector2.Lerp(worldmap_offset_current, worldmap_offset_target, 0.25f);

								//worldmap_offset += momentum * zoom_inv * 0.10f;
							}
							else
							{
								if (enable_momentum) momentum = Vector2.Lerp(momentum, Vector2.Zero, 0.10f);
								worldmap_offset_target += momentum * zoom_inv;
								worldmap_offset_current = Vector2.Lerp(worldmap_offset_current, worldmap_offset_target, 0.25f);
								if (!enable_momentum) momentum = default;
							}

							if (hovered && !kb.GetKey(Keyboard.Key.LeftShift))
							{
								worldmap_zoom_target -= mouse.GetScroll(0.25f);
								//worldmap_zoom = Maths.Clamp(worldmap_zoom, 1.00f, 8.00f);
								worldmap_zoom_target = Maths.Clamp(worldmap_zoom_target, 1.00f, 7.00f);
							}
						}

						worldmap_zoom_current = Maths.Lerp(worldmap_zoom_current, worldmap_zoom_target, 0.20f);

						if (enable_renderer)
						{
							worldmap_offset_current_snapped = Maths.SnapFloor(worldmap_offset_current, 1 / scale_b);
							IScenario.WorldMap.Renderer.UpdateCamera(worldmap_offset_current_snapped, 0.00f, new Vector2(1));

							ref var world_data = ref h_world.GetData();
							if (world_data.IsNotNull())
							{
								if (world_data.doodads != null && show_doodads)
								{
									IScenario.Doodad.Renderer.Add(world_data.doodads.AsSpan());
								}
							}

							IScenario.WorldMap.Renderer.Submit();
							IScenario.Doodad.Renderer.Submit();
						}

						//if (!disable_input)
						{
							if (hovered && !gizmo.IsHovered())
							{
								if (kb.GetKey(Keyboard.Key.LeftShift))
								{
									var scroll = mouse.GetScroll();
									if (scroll != 0.00f)
									{
										Sound.PlayGUI(GUI.sound_select, volume: 0.09f);
										editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) - (int)(scroll), 0, (int)EditorMode.Max);
										edit_asset = null;
										edit_doodad_index = null;
									}
									//else
									//{
									//	worldmap_offset_current = default;
									//	worldmap_offset_current_snapped = default;
									//	worldmap_offset_target = default;
									//	momentum = default;
									//	rotation = default;
									//}
								}

								//if (kb.GetKeyDown(Keyboard.Key.Tab))
								//{
								//	editor_mode = (EditorMode)Maths.Wrap(((int)editor_mode) + 1, 0, (int)EditorMode.Max);
								//	edit_asset = null;
								//}
							}

							var move_speed = (1.00f / MathF.Sqrt(worldmap_zoom_current)) * 10.00f;

							if (!kb.GetKey(Keyboard.Key.LeftControl))
							{
								if (kb.GetKey(Keyboard.Key.MoveLeft))
								{
									//worldmap_offset.X += move_speed;
									momentum.X -= move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveRight))
								{
									//worldmap_offset.X -= move_speed;
									momentum.X += move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveUp))
								{
									//worldmap_offset.Y += move_speed;
									momentum.Y -= move_speed;
								}

								if (kb.GetKey(Keyboard.Key.MoveDown))
								{
									//worldmap_offset.Y -= move_speed;
									momentum.Y += move_speed;
								}
							}
						}
						#endregion

						if (dragging && GUI.IsHovered)
						{
							GUI.SetHoveredID("worldmap");
						}
					}
				}

				#region Left
				DrawLeftWindow(ref rect, zoom, ref mat_l2c);
				#endregion

				#region Right
				DrawRightWindow(is_loading, ref rect, zoom, ref mat_l2c);
				#endregion

				#region Debug			
				DrawDebugWindow(ref rect, zoom, ref mat_l2c);
				#endregion
			}
		}

		private static void DrawEditor(ref AABB rect, ref IScenario.Data scenario_data, IAsset2<IScenario, IScenario.Data>.Definition scenario_asset, ref Mouse.Data mouse, ref Keyboard.Data kb, float zoom, ref Matrix3x2 mat_l2c, ref Vector2 mouse_local, bool hovered)
		{
			switch (editor_mode)
			{
				case EditorMode.Province:
				{
					if (!hovered && edit_asset == null) break;

					var province_handle = default(IProvince.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					var ts = Timestamp.Now();
					foreach (var asset in IProvince.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								province_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref province_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							var point = asset_data.points[index];
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (!edit_points_index.HasValue)
							{
								if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
								{
									if (mouse.GetKeyDown(Mouse.Key.Right))
									{
										if (kb.GetKey(Keyboard.Key.LeftShift))
										{
											//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
											asset_data.points = asset_data.points.Insert(index, asset_data.points[index]);
											//asset.Save();
											edit_asset = asset;
											hs_pending_asset_saves.Add(asset);
										}
										else
										{
											edit_points_index = index;
											edit_points_s32 = asset_data.points;
											edit_asset = asset;

											//GUI.FocusAsset(asset.GetHandle());
											hs_pending_asset_saves.Add(asset);
											Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
										}
									}
									else if (kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.points = asset_data.points.Remove(index);
										//asset.Save();
										hs_pending_asset_saves.Add(asset);
									}
								}

								GUI.FocusableAsset(asset.GetHandle(), rect: AABB.Centered(point_t, new(1.00f * zoom)));
							}
						}
					}
				}
				break;

				case EditorMode.District:
				{
					if (!hovered && edit_asset == null) break;

					var district_handle = default(IDistrict.Handle);
					var distance_sq = float.MaxValue;
					var index = int.MaxValue;

					var ts = Timestamp.Now();
					foreach (var asset in IDistrict.Database.GetAssets())
					{
						if (asset.id == 0) continue;
						ref var asset_data = ref asset.GetData();

						var points = asset_data.points.AsSpan();
						if (!points.IsEmpty)
						{
							points.GetNearestIndex(mouse_local, out var nearest_index_tmp, out var nearest_distance_sq_tmp);

							if (nearest_distance_sq_tmp < distance_sq)
							{
								district_handle = asset;
								distance_sq = nearest_distance_sq_tmp;
								index = nearest_index_tmp;
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref district_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							var point = asset_data.points[index];
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (!edit_points_index.HasValue)
							{
								if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
								{
									if (mouse.GetKeyDown(Mouse.Key.Right))
									{
										if (kb.GetKey(Keyboard.Key.LeftShift))
										{
											//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
											asset_data.points = asset_data.points.Insert(index, asset_data.points[index]);
											//asset.Save();
											edit_asset = asset;
											hs_pending_asset_saves.Add(asset);
										}
										else
										{
											edit_points_index = index;
											edit_points_s32 = asset_data.points;
											edit_asset = asset;

											//GUI.FocusAsset(asset.GetHandle());
											hs_pending_asset_saves.Add(asset);
											Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
										}
									}
									else if (kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.points = asset_data.points.Remove(index);
										//asset.Save();
										hs_pending_asset_saves.Add(asset);
									}
								}

								GUI.FocusableAsset(asset.GetHandle(), rect: AABB.Centered(point_t, new(1.00f * zoom)));
							}
						}
					}
				}
				break;

				case EditorMode.Location:
				{
					if (!hovered && edit_asset == null) break;

					var distance_sq = 0.00f;
					var h_location = (edit_asset as ILocation.Definition)?.GetHandle() ?? GetNearestLocation(mouse_local, out distance_sq);
					if (distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref h_location.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							ref var point = ref asset_data.point;
							var point_t = Vector2.Transform((Vector2)point, mat_l2c);

							var color = Color32BGRA.White.LumaBlend(asset_data.color, 0.50f);
							//GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							if (h_selected_location == h_location)
							{
								edit_asset = asset;

								var point_tmp = (Vector2)point;
								var scale = Vector2.One;
								var rotation = 0.00f;

								if (GUI.DrawGizmo(ref gizmo, zoom * 0.25f, ref point_tmp, ref scale, ref rotation, mat_l2c))
								{
									hs_pending_asset_saves.Add(asset);

									asset_data.point = new short2((short)MathF.Round(point_tmp.X), (short)MathF.Round(point_tmp.Y));
								}
							}
						}
					}
				}
				break;

				case EditorMode.Doodad:
				{
					if (!hovered && !edit_doodad_index.HasValue) break;

					var ts = Timestamp.Now();
					//if (hovered && edit_doodad_index.HasValue && GUI.IsMouseDoubleClicked())
					//{
					//	edit_doodad_index = null;
					//}

					ref var asset_data = ref h_world.GetData(out var asset);
					if (asset_data.IsNotNull())
					{
						var index = edit_doodad_index ?? -1;
						var distance_sq = 0.00f;

						var doodads = asset_data.doodads.AsSpan();

						ref var doodad = ref doodads.GetRefAtIndexOrNull(index);
						if (doodad.IsNull())
						{
							doodad = ref GetNearestDoodad(mouse_local, doodads, out index, out distance_sq);
						}
						var ts_elapsed = ts.GetMilliseconds();

						if (doodad.IsNotNull() && (edit_doodad_index.HasValue || distance_sq <= 1.00f.Pow2()))
						{
							{
								ref var point = ref doodad.position;
								var point_t = Vector2.Transform(point, mat_l2c);

								var selected = edit_doodad_index == index;
								var color = Color32BGRA.White.LumaBlend(doodad.color, 0.50f);

								//GUI.DrawCircle(point_t, 0.50f * zoom, color: color, segments: 16, layer: GUI.Layer.Foreground);
								GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

								if (hovered && !kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl | Keyboard.Key.LeftShift))
								{
									if (mouse.GetKeyDown(Mouse.Key.Left))
									{
										edit_doodad_index = index;
										hs_pending_asset_saves.Add(asset);
									}
								}

								if (selected)
								{
									edit_asset = asset;
									GUI.DrawGizmo(ref gizmo, zoom * 0.25f, ref doodad.position, ref doodad.scale, ref doodad.rotation, mat_l2c);
								}

								if (kb.GetKey(Keyboard.Key.LeftControl))
								{
									if (kb.GetKeyDown(Keyboard.Key.C))
									{
										clipboard_doodad = doodad;
										Notification.Push("Copied doodad to clipboard.", color: Color32BGRA.White, sound: "ui.copy", volume: 0.40f);
									}
								}
								else
								{
									if (hovered && kb.GetKeyDown(Keyboard.Key.Delete))
									{
										asset_data.doodads = asset_data.doodads.Remove(index);

										hs_pending_asset_saves.Add(asset);
										edit_doodad_index = null;

										App.ScheduleGC(1);
										Notification.Push($"Deleted doodad at [{point.X:0.00}, {point.Y:0.00}].", color: Color32BGRA.Orange, sound: "ui.misc.02", volume: 0.70f, pitch: 1.00f);
									}
								}
							}
						}
					}
				}
				break;

				case EditorMode.Roads:
				{
					if (!hovered && edit_asset == null) break;

					var district_handle = default(IDistrict.Handle);
					//var distance_sq = float.MaxValue;
					//var index = int.MaxValue;

					var road_points_distance_sq = float.MaxValue;
					var road_point_index = int.MaxValue;
					var road_index = int.MaxValue;

					var ts = Timestamp.Now();
					//if (!edit_points_index.HasValue)
					{
						foreach (var asset in IDistrict.Database.GetAssets())
						{
							if (asset.id == 0) continue;
							ref var asset_data = ref asset.GetData();

							var span_roads = asset_data.roads.AsSpan();
							for (var i = 0; i < span_roads.Length; i++)
							{
								ref var road = ref span_roads[i];
								var points = road.points.AsSpan();
								if (!points.IsEmpty)
								{
									points.GetNearestIndex(mouse_local, out var road_nearest_index_tmp, out var road_nearest_distance_sq_tmp);

									if (road_nearest_distance_sq_tmp < road_points_distance_sq)
									{
										road_points_distance_sq = road_nearest_distance_sq_tmp;
										road_point_index = road_nearest_index_tmp;
										road_index = i;
										district_handle = asset;
									}
								}
							}
						}
					}
					var ts_elapsed = ts.GetMilliseconds();

					if (road_points_distance_sq <= 1.00f.Pow2())
					{
						ref var asset_data = ref district_handle.GetData(out var asset);
						if (asset_data.IsNotNull())
						{
							//var color = Color32BGRA.White.LumaBlend(asset_data.color_border, 0.50f);
							//GUI.DrawCircleFilled(point_t, 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
							//GUI.DrawTextCentered($"{ts_elapsed:0.0000} ms", point_t, layer: GUI.Layer.Foreground);

							var span_roads = asset_data.roads.AsSpan();
							if ((uint)road_index < span_roads.Length)
							{
								ref var road = ref span_roads[road_index];

								var color = Color32BGRA.White.LumaBlend(road.color_border, 0.50f);

								if (!edit_points_index.HasValue)
								{
									if (!kb.GetKey(Keyboard.Key.LeftAlt | Keyboard.Key.LeftControl))
									{
										if (mouse.GetKeyDown(Mouse.Key.Right))
										{
											GUI.FocusAsset(district_handle);

											if (kb.GetKey(Keyboard.Key.LeftShift))
											{
												//d_district.points = points.Insert(i, (short2)(points[i] + points[(i + 1) % points.Length]) / 2);
												road.points = road.points.Insert(road_point_index, road.points[road_point_index]);
												//asset.Save();
												edit_asset = asset;
												hs_pending_asset_saves.Add(asset);
											}
											else
											{
												edit_points_index = road_point_index;
												edit_points_f32 = road.points;
												edit_asset = asset;

												//GUI.FocusAsset(asset.GetHandle());
												hs_pending_asset_saves.Add(asset);
												Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);
											}
										}
										else if (kb.GetKeyDown(Keyboard.Key.Delete))
										{
											if (road.points.Length <= 1)
											{
												asset_data.roads = asset_data.roads.Remove(road_index);
											}
											else
											{
												road.points = road.points.Remove(road_point_index);
												road_point_index = Maths.Wrap(road_point_index, 0, road.points.Length);
											}
											//asset.Save();
											hs_pending_asset_saves.Add(asset);
										}
									}
								}
								else
								{
									if (mouse.GetKeyDown(Mouse.Key.Left))
									{
										var road_new = road;
										road_new.points = new Vector2[]
										{
											mouse_local + (road.points[Maths.Wrap(road_point_index - 1, 0, road.points.Length)] - mouse_local).GetNormalized().GetPerpendicular(true),
											mouse_local,
										};

										asset_data.roads = asset_data.roads.Add(road_new);
										hs_pending_asset_saves.Add(asset);
										Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 1.00f);

										App.WriteLine("lmb");
									}
								}

								GUI.DrawCircleFilled(Vector2.Transform(road.points[road_point_index], mat_l2c), 0.125f * zoom, color: color, segments: 4, layer: GUI.Layer.Foreground);
								GUI.DrawTextCentered($"{asset_data.name}\n{road.type} [{road_index}]\n{ts_elapsed:0.0000} ms", Vector2.Transform(road.points[road_point_index], mat_l2c), layer: GUI.Layer.Foreground);
							}
						}
					}
				}
				break;

				case EditorMode.Junctions:
				{
					var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
					junctions_span.GetNearestIndex(mouse_local, out var junction_index, out var junction_dist_sq, (ref Road.Junction junction) => ref junction.pos);

					if (junction_dist_sq < 1.00f.Pow2())
					{
						ref var junction = ref junctions_span[junction_index];

						junctions_queue.Clear();
						junctions_visited.Clear();

						//hs_visited.Add(junction_index);

						var type = junction.segments[0].GetRoad().type;

						var iter_max = 10;

						junctions_queue.Enqueue(junction_index);
						for (var i = 0; i < iter_max; i++)
						{
							var count = junctions_queue.Count;
							for (var j = 0; j < count; j++)
							{
								var junction_index_new = junctions_queue.Dequeue();
								DrawJunction(junctions_visited, junctions_queue, junctions_span, junction_index_new, ref mat_l2c, zoom, i, iter_max, type);
							}
						}

						//DrawJunction(hs_visited, queue, junctions_span, ref junction.segments[0], ref mat_l2c, zoom, 0, iter_max);
						static void DrawJunction(HashSet<int> hs_visited, Queue<int> queue, Span<Road.Junction> junctions_span, int junction_index, ref Matrix3x2 mat_l2c, float zoom, int depth, int iter_max, Road.Type type)
						{
							//if (depth >= iter_max) return;

							//var junction_index = road_segment_to_junction_index[segment_from];



							var color = Color32BGRA.FromHSV((1.00f - ((float)depth / (float)iter_max)) * 2.00f, 1.00f, 1.00f);

							ref var junction = ref junctions_span[junction_index];
							GUI.DrawCircle(Vector2.Transform(junction.pos, mat_l2c), 0.125f * zoom, color: color, segments: 12, layer: GUI.Layer.Window);

							var segments_span = junction.segments.Slice(junction.segments_count);
							foreach (ref var segment_base in segments_span)
							{
								//if (hs_visited.Contains(segment_base.GetHashCode())) continue;
								//hs_visited.Add(segment_base.GetHashCode());

								//if (segment_base == segment_from) continue;

								//if (hs_visited.Contains(segment_base.GetHashCode())) continue;
								//hs_visited.Add(segment_base.GetHashCode());


								ref var road = ref segment_base.GetRoad();
								if (road.type != type) continue;

								var pos = segment_base.GetPosition();
								var road_points_span = road.points.AsSpan();

								GUI.DrawCircleFilled(Vector2.Transform(pos, mat_l2c), 0.125f * zoom * 0.50f, color: color, segments: 4, layer: GUI.Layer.Window);

								var depth_offset = Vector2.Zero;
								//var depth_offset = new Vector2(0, -4 * depth);

								{
									var pos_last = pos;
									for (var i = segment_base.index + 1; i < road_points_span.Length; i++)
									{
										var segment = new Road.Segment(segment_base.chain, (byte)i);

										if (hs_visited.Contains(segment.GetHashCode())) break;
										hs_visited.Add(segment.GetHashCode());

										GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, layer: GUI.Layer.Window);
										pos_last = road_points_span[i];

										if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
										{
											queue.Enqueue(junction_index_new);
											break;
											//DrawJunction(hs_visited, junctions_span, ref segment, ref mat_l2c, zoom, depth + 1, iter_max);
										}
									}
								}

								{
									var pos_last = pos;
									for (var i = segment_base.index - 1; i >= 0; i--)
									{
										var segment = new Road.Segment(segment_base.chain, (byte)i);

										if (hs_visited.Contains(segment.GetHashCode())) break;
										hs_visited.Add(segment.GetHashCode());

										GUI.DrawLine(Vector2.Transform(pos_last, mat_l2c) - depth_offset, Vector2.Transform(road_points_span[i], mat_l2c) - depth_offset, color, layer: GUI.Layer.Window);
										pos_last = road_points_span[i];

										if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_new))
										{
											queue.Enqueue(junction_index_new);
											break;
											//DrawJunction(hs_visited, junctions_span, ref segment, ref mat_l2c, zoom, depth + 1, iter_max);
										}
									}
								}

								if (hs_visited.Contains(segment_base.GetHashCode())) continue;
								hs_visited.Add(segment_base.GetHashCode());

							}
						}
					}
				}
				break;
			}

			if (edit_points_index.TryGetValue(out var v_edit_points_index))
			{
				if (edit_points_s32 != null) edit_points_s32[v_edit_points_index] = new short2((short)MathF.Round(mouse_local.X), (short)MathF.Round(mouse_local.Y));
				if (edit_points_f32 != null) edit_points_f32[v_edit_points_index] = mouse_local;

				if (mouse.GetKeyUp(Mouse.Key.Right))
				{
					//edit_asset.Save();

					edit_points_index = null;
					edit_points_s32 = null;
					edit_points_f32 = null;
					//edit_asset = null;

					Sound.PlayGUI(GUI.sound_pop, 0.07f, pitch: 0.80f);
				}
			}

			if (kb.GetKey(Keyboard.Key.LeftControl))
			{
				if (kb.GetKeyDown(Keyboard.Key.MoveDown))
				{
					if (hs_pending_asset_saves.Count > 0)
					{
						foreach (var asset in hs_pending_asset_saves)
						{
							if (asset != null)
							{
								asset.Save();
							}
						}
						hs_pending_asset_saves.Clear();
					}
				}
				//else if (kb.GetKeyDown(Keyboard.Key.C) && scenario_data.IsNotNull() && edit_doodad_index.HasValue)
				//{
				//	ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(edit_doodad_index.Value);
				//	if (doodad.IsNotNull())
				//	{
				//		clipboard_doodad = doodad;
				//		Notification.Push("Copied doodad to clipboard.", color: Color32BGRA.Yellow, sound: "ui.copy", volume: 0.40f);
				//	}
				//}
				else if (kb.GetKeyDown(Keyboard.Key.V) && scenario_data.IsNotNull() && clipboard_doodad.HasValue)
				{
					ref var doodad = ref clipboard_doodad.GetRefOrNull();
					if (doodad.IsNotNull())
					{
						var doodad_tmp = doodad;
						doodad_tmp.position = mouse_local;

						scenario_data.doodads = scenario_data.doodads.Add(doodad_tmp);
						hs_pending_asset_saves.Add(scenario_asset);

						App.ScheduleGC(1);

						Notification.Push("Pasted doodad from clipboard.", color: Color32BGRA.Green, sound: "ui.copy", volume: 0.30f, pitch: 0.70f);
					}
				}
			}

			{
				if (hs_pending_asset_saves.Count > 0)
				{
					var text_offset = 16;
					GUI.DrawTextCentered("Pending Saves:", rect.GetPosition(new(0.50f, 0.00f)) + new Vector2(0, text_offset), font: GUI.Font.Superstar, size: 24, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
					text_offset += 22;

					foreach (var asset in hs_pending_asset_saves)
					{
						if (asset != null)
						{
							GUI.DrawTextCentered(asset.Identifier, rect.GetPosition(new(0.50f, 0.00f)) + new Vector2(0, text_offset), font: GUI.Font.Superstar, size: 16, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
							text_offset += 16;
						}
					}
				}
			}

			if (editor_mode != EditorMode.None) // App.debug_mode_gui)
			{
				GUI.DrawTextCentered($"Editor: {editor_mode}", rect.GetPosition(new(0.50f, 1.00f)) - new Vector2(0, 16), font: GUI.Font.Superstar, size: 32, layer: GUI.Layer.Foreground, color: GUI.font_color_yellow);
			}
		}

		private static void DrawLeftWindow(ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			using (var window = GUI.Window.Standalone("worldmap.side.left", position: new Vector2(rect.a.X, rect.a.Y) + new Vector2(6, 12), size: new(284, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(0.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
			{
				if (window.show)
				{
					window.group.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_default);

					var mod_context = App.GetModContext();

					ref var world_info = ref Client.GetWorldInfo();

					using (GUI.Group.New(size: GUI.Rm))
					{
						//GUI.Checkbox("DEV: Renderer", ref use_renderer, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale A", ref IScenario.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32));
						//GUI.SliderFloat("DEV: Scale B", ref IScenario.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32));

						GUI.SeparatorThick();

						using (var scrollbox = GUI.Scrollbox.New("sb.worldmap.left", GUI.Rm))
						{
							using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
							{
								using (GUI.ID.Push("regions"))
								{
									using (var collapsible = GUI.Collapsible2.New("regions", new Vector2(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Regions", size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner(padding: new Vector4(6, 0, 0, 0)))
										{
											var region_list_span = World.GetRegionList().AsSpan();
											for (var i = 1; i < region_list_span.Length; i++)
											{
												ref var region_info = ref region_list_span[i];
												ref var map_info = ref region_info.map_info.GetRefOrNull();

												if (region_info.IsValid() && map_info.IsNotNull())
												{
													ref var location_data = ref map_info.h_location.GetData();
													if (location_data.IsNotNull())
													{
														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
														{
															if (group_row.IsVisible())
															{
																GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmY));

																group_row.DrawBackground(GUI.tex_panel);

																GUI.SameLine();

																using (GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
																{
																	GUI.TitleCentered(map_info.name, size: 18, pivot: new(0.00f, 0.00f));
																}

																var selected = selected_region_id == i;
																if (GUI.Selectable3((uint)i, group_row.GetOuterRect(), selected))
																{
																	if (selected)
																	{
																		selected_region_id = 0;
																		h_selected_location = default;
																	}
																	else
																	{
																		selected_region_id = (byte)i;
																		h_selected_location = map_info.h_location;
																		worldmap_offset_target = (Vector2)location_data.point;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}

							using (var group_list = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
							{
								using (GUI.ID.Push("locations"))
								{
									using (var collapsible = GUI.Collapsible2.New("locations", new Vector2(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Locations", size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
										{
											foreach (var asset in ILocation.Database.GetAssets())
											{
												if (asset.id == 0) continue;

												ref var asset_data = ref asset.GetData();

												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 4)))
												{
													if (group_row.IsVisible())
													{
														group_row.DrawBackground(GUI.tex_panel);

														GUI.TitleCentered(asset_data.name, size: 20, pivot: new(0.00f, 0.50f));
														GUI.TextShadedCentered(asset_data.type.GetEnumName(), size: 14, pivot: new(1.00f, 0.50f), color: GUI.font_color_desc);

														var selected = asset == h_selected_location; // selected_region_id == i;
														if (GUI.Selectable3((uint)asset.id, group_row.GetOuterRect(), selected))
														{
															selected_region_id = 0;

															if (selected)
															{
																h_selected_location = default;
															}
															else
															{
																h_selected_location = asset;
																worldmap_offset_target = (Vector2)asset_data.point;
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static void DrawRightWindow(bool is_loading, ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			if (selected_region_id != 0 || h_selected_location != 0)
			{
				using (var window = GUI.Window.Standalone("worldmap.side.right", position: new Vector2(rect.b.X, rect.a.Y) + new Vector2(-6, 12), size: new(322, MathF.Min(rect.GetHeight() - 8, 550)), pivot: new(1.00f, 0.00f), padding: new(8), force_position: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.Child))
				{
					if (window.show)
					{
						window.group.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_default);

						if (h_selected_location != 0)
						{
							ref var location_data = ref h_selected_location.GetData();
							if (location_data.IsNotNull())
							{
								for (var i = 0; i < Region.max_count; i++)
								{
									ref var region_info = ref World.GetRegionInfo((byte)i);
									if (region_info.IsNotNull() && region_info.map_info.HasValue)
									{
										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
										{
											selected_region_id = (byte)i;
											break;
										}
									}
								}

								using (GUI.Group.New(size: GUI.Rm))
								{
									using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
									{
										GUI.TitleCentered(location_data.name, size: 32, pivot: new(0.00f, 0.50f));
									}

									GUI.SeparatorThick();

									ref var region_info = ref World.GetRegionInfo(selected_region_id);
									if (region_info.IsNotNull() && region_info.IsValid())
									{
										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
										{
											var map_asset = App.GetModContext().GetMap(region_info.map);
											if (map_asset != null)
											{
												using (var group_map = GUI.Group.New(size: new(GUI.RmX, (GUI.RmX * 0.50f) + 40 + 4), padding: new(4, 4)))
												{
													using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY)))
													{
														GUI.DrawMapThumbnail(region_info.map, size: new(GUI.RmX));

														if (true)
														{
															var color = GUI.col_button_ok;
															var alpha = 1.00f;

															if (GUI.DrawButton("Enter", size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
															{
																Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

																//Client.TODO_LoadRegion(region_id);
															}
														}
													}

													GUI.SameLine();

													using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(6, 2)))
													{
														using (GUI.Wrap.Push(GUI.RmX))
														{
															GUI.TextShaded(map_asset.Description);
														}
													}
												}
											}
										}

										GUI.SeparatorThick();
									}

									using (var group_top = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
									{
										using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8, 8)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												GUI.TextShaded(location_data.desc);
											}
										}
									}

									GUI.SeparatorThick();

									using (var group_info = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40), padding: new(8, 8)))
									{
										using (var group_info_wide = GUI.Group.New2(size: new(GUI.RmX, 0), padding: new(2, 2, 12, 2)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												GUI.LabelShaded("Categories:", location_data.categories, font_a: GUI.Font.Superstar, size_a: 16);
											}
										}

										using (var group_info_left = GUI.Group.New2(size: new(GUI.RmX * 0.50f, GUI.RmY), padding: new(2, 2, 12, 2)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												GUI.LabelShaded("Type:", location_data.type, font_a: GUI.Font.Superstar, size_a: 16);
											}
										}

										GUI.SameLine();

										using (var group_info_right = GUI.Group.New2(size: new(GUI.RmX, GUI.RmY), padding: new(12, 2, 2, 2)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												//var ts = Timestamp.Now();
												//var nearest_road = GetNearestRoad(location_data.h_district, Road.Type.Road, (Vector2)location_data.point, out var nearest_road_dist_sq);
												//var nearest_rail = GetNearestRoad(location_data.h_district, Road.Type.Rail, (Vector2)location_data.point, out var nearest_rail_dist_sq);
												//var ts_elapsed = ts.GetMilliseconds();

												//if (nearest_road_dist_sq <= 4.00f.Pow2())
												//{
												//	GUI.DrawCircleFilled(Vector2.Transform(nearest_road.GetPosition(), mat_l2c), 0.125f * zoom, Color32BGRA.Yellow, 4, GUI.Layer.Foreground);
												//}

												//if (nearest_rail_dist_sq <= 4.00f.Pow2())
												//{
												//	GUI.DrawCircleFilled(Vector2.Transform(nearest_rail.GetPosition(), mat_l2c), 0.125f * zoom, Color32BGRA.Orange, 4, GUI.Layer.Foreground);
												//}
												//GUI.Text($"nearest in {ts_elapsed:0.0000} ms");
											}
										}

										//GUI.TextShaded("- some info here");
									}
								}
							}
						}
					}
				}
			}
		}

		private static void DrawDebugWindow(ref AABB rect, float zoom, ref Matrix3x2 mat_l2c)
		{
			if (editor_mode != EditorMode.None) // App.debug_mode_gui)
			{
				using (var window = GUI.Window.Standalone("worldmap.debug", position: rect.GetPosition(0, 1, new(8, -8)), size: new(300, 400), pivot: new(0.00f, 1.00f), padding: new(8), force_position: false))
				{
					if (window.show)
					{
						GUI.DrawWindowBackground();

						GUI.Title("Worldmap Debug");

						GUI.SeparatorThick();

						GUI.Checkbox("Renderer", ref enable_renderer, new(32, 32), show_text: false, show_tooltip: true);

						GUI.Checkbox("Show Provinces", ref show_provinces, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Districts", ref show_districts, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Regions", ref show_regions, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Locations", ref show_locations, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Roads", ref show_roads, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Rails", ref show_rails, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Fill", ref show_fill, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Borders", ref show_borders, new(32, 32), show_text: false, show_tooltip: true);
						GUI.SameLine();
						GUI.Checkbox("Show Doodads", ref show_doodads, new(32, 32), show_text: false, show_tooltip: true);

						if (GUI.DrawButton("Recalculate Roads", size: new Vector2(160, 40)))
						{
							RecalculateRoads();
						}

						//if (road_segments.Count > 0)
						//{
						//	foreach (var road in road_segments)
						//	{
						//		GUI.DrawCircleFilled(Vector2.Transform(road.Value.GetPosition(), )
						//	}
						//}

						//if (GUI.DrawButton("Rescale", size: new Vector2(100, 40)))
						//{
						//	Rescale();
						//}

						//GUI.EnumInputMasked("worldmap.filter.roads", ref filter_roads, new Vector2(GUI.RmX, 32), filter_roads_mask, show_none: true, show_all: true, close_on_select: false);
						//GUI.EnumInput("worldmap.filter.roads", ref filter_roads, new Vector2(GUI.RmX, 32), filter_roads_mask, show_none: true, show_all: true, close_on_select: false);

						GUI.SeparatorThick();

						using (var scrollbox = GUI.Scrollbox.New("worldmap.scroll.editor", size: GUI.Rm))
						{
							switch (editor_mode)
							{
								case EditorMode.Roads:
								{
									//if ()
								}
								break;

								case EditorMode.Junctions:
								{
									//var junctions_span = CollectionsMarshal.AsSpan(road_junctions);
									//junctions_span.GetNearestIndex
								}
								break;

								case EditorMode.Doodad:
								{
									if (edit_doodad_index.TryGetValue(out var index_doodad))
									{
										ref var scenario_data = ref h_world.GetData(out var scenario_asset);
										if (scenario_data.IsNotNull())
										{
											ref var doodad = ref scenario_data.doodads.AsSpan().GetRefAtIndexOrNull(index_doodad);
											if (doodad.IsNotNull())
											{
												GUI.DrawStyledEditorForType(ref doodad, new Vector2(GUI.RmX, 32));
											}
										}
									}
								}
								break;

								default:
								{
									GUI.SliderFloat("DEV: Scale A", ref IScenario.WorldMap.scale, 1.00f, 256.00f, new(GUI.RmX, 32), snap: 0.001f);
									GUI.SliderFloat("DEV: Scale B", ref IScenario.WorldMap.scale_b, 1.00f, 256.00f, new(GUI.RmX, 32), snap: 0.001f);

									GUI.SliderFloat("DEV: Size.X", ref worldmap_window_size.X, 1.00f, 1920.00f, new(GUI.RmX * 0.50f, 32), snap: 8);
									GUI.SameLine();
									GUI.SliderFloat("DEV: Size.Y", ref worldmap_window_size.Y, 1.00f, 1920.00f, new(GUI.RmX, 32), snap: 8);

									GUI.SliderFloat("DEV: Offset.X", ref worldmap_window_offset.X, -512.00f, 512.00f, new(GUI.RmX * 0.50f, 32), snap: 1);
									GUI.SameLine();
									GUI.SliderFloat("DEV: Offset.Y", ref worldmap_window_offset.Y, -512.00f, 512.00f, new(GUI.RmX, 32), snap: 1);
								}
								break;
							}
						}
					}
				}
			}
		}
#endif
	}
}

