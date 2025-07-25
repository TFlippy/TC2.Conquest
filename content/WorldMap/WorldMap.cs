﻿
using TC2.Base;
using TC2.Base.Components;
using System.Collections.Frozen;

namespace TC2.Conquest
{
	public static partial class WorldMap
	{
		//public enum Context: uint
		//{
		//	Undefined,


		//}

		public static FrozenDictionary<Road.Segment, int> road_segment_to_junction_index;
		public static Road.Junction[] road_junctions;

		public static float road_junction_threshold = 0.1250f;
		public const float km_per_unit = 2.00f;
		public const float km_per_unit_inv = 1.00f / km_per_unit;

		public static (Entity current, Entity pending) hovered_entity;

		public static Entity interacted_entity;
		public static Entity interacted_entity_cached;

		public static Entity ent_context_current;

		public static readonly Dictionary<ILocation.Handle, Road.Segment> location_to_road = new();
		public static readonly Dictionary<ILocation.Handle, Road.Segment> location_to_rail = new();

		public static readonly Dictionary<Road.Segment, ILocation.Handle> road_to_location = new();
		public static readonly Dictionary<Road.Segment, ILocation.Handle> rail_to_location = new();

		public static readonly Dictionary<int, IPrefecture.Handle> pos_hash_to_prefecture = new();

		public static bool TryGetRoad(this ILocation.Handle h_location, out Road.Segment road)
		{
			return location_to_road.TryGetValue(h_location, out road);
		}

		public static bool TryGetRail(this ILocation.Handle h_location, out Road.Segment rail)
		{
			return location_to_rail.TryGetValue(h_location, out rail);
		}

		public static IPrefecture.Handle GetPrefectureAtPosition(Vector2 pos)
		{
			var pos_grid = new Vec2i16((short)pos.X, (short)pos.Y);
			var pos_key = Maths.ToInt32BitCast(pos_grid);

			pos_hash_to_prefecture.TryGetValue(pos_key, out var h_prefecture);
			return h_prefecture;
		}

		public static IPrefecture.Handle GetPrefectureAtPoint(Vec2i16 pos)
		{
			var pos_key = Maths.ToInt32BitCast(pos);

			pos_hash_to_prefecture.TryGetValue(pos_key, out var h_prefecture);
			return h_prefecture;
		}

		public static partial class Marker
		{
			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global, sync_table_capacity: 128)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					Hidden = 1u << 0,
					Directional = 1u << 1,
					Use_Worldmap_Renderer = 1u << 2,
					Hide_If_Parented = 1u << 3,
					No_Interact = 1u << 4,
					No_Select = 1u << 5,
				}

				public WorldMap.Marker.Data.Flags flags;

				[Editor.Picker.Position(relative: true, mark_modified: true)]
				public Vector2 relative_offset;

				public float radius = 1.00f;
				public float scale = 1.00f;
				public float rotation;

				public Color32BGRA color;
				public Color32BGRA color_override;

				public Vector2 text_offset;

				public Vector2 icon_offset;
				public Sprite icon;
			}

			[Query(ISystem.Scope.Global)]
			public delegate void GetAllMarkersQuery(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
				[Source.Owned] in WorldMap.Marker.Data marker,
				[Source.Owned] in Transform.Data transform,
				[Source.Owned, Optional(true)] ref Nameable.Data nameable,
				[HasRelation(Source.Modifier.Owned, Relation.Type.Stored, true)] bool has_parent);

			[Query(ISystem.Scope.Global, Query.Flags.None)]
			public delegate void GetUnitsQuery(ISystem.Info.Global info, ref Region.Data.Global region,
				Entity ent_marker, Entity ent_transform,
				[Source.Owned] in WorldMap.Marker.Data marker,
				[Source.Owned] in Transform.Data transform);

			//#if CLIENT
			// TODO: temporary workaround, make it update when the asset is modified
			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Global, interval: 1.00f, order: 1000)]
			public static void UpdateMarkerFactionColor(ISystem.Info.Common info, Entity entity,
			[Source.Owned] in Faction.Data faction, [Source.Owned] ref WorldMap.Marker.Data marker, [Source.Owned] ref Faction.Colorable colorable)
			{
				//if (colorable.h_faction_cached != faction.id || renderer.color_mask_r.bgra != colorable.color_a_cached.bgra || renderer.color_mask_g.bgra != colorable.color_b_cached.bgra)
				{
					var color_a = default(Color32BGRA);
					if (faction.id.TryGetData(out var ref_faction))
					{
						color_a = ref_faction.value.color_a;
					}

					marker.color_override = Color32BGRA.LerpRGB(marker.color, color_a, color_a.IsVisible() ? Maths.Max(colorable.intensity_a, color_a.GetLuma()) : 0.00f);
					colorable.h_faction_cached = faction.id;
				}
			}

			// TODO: temporary workaround, make it update when the asset is modified
			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Global, interval: 1.00f)]
			[ISystem.Event<IEntrance.RefreshEvent>(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void UpdateMarkerEntrance(ISystem.Info.Common info, Entity entity, ref IEntrance.RefreshEvent ev,
			[Source.Owned] ref WorldMap.Marker.Data marker, [Source.Owned] in Entrance.Data entrance)
			{
				marker.relative_offset = ev.data.relative_offset;
				marker.icon = ev.data.icon;
			}

			// TODO: temporary workaround, make it update when the asset is modified
			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Global, interval: 1.00f)]
			[ISystem.Event<ILocation.RefreshEvent>(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void UpdateMarkerLocation(ISystem.Info.Common info, Entity entity, ref ILocation.RefreshEvent ev,
			[Source.Owned] ref WorldMap.Marker.Data marker, [Source.Owned] in Location.Data location)
			{
				marker.icon = ev.data.icon;
				marker.icon_offset = ev.data.icon_offset;
				marker.text_offset = ev.data.text_offset;
				marker.color = ev.data.color;
			}

			// TODO: temporary workaround, make it update when the asset is modified
			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Global, interval: 1.00f), HasTag("asset", true, Source.Modifier.Owned)]
			public static void UpdateMarkerIcon(ISystem.Info.Common info, Entity entity,
			[Source.Owned] ref WorldMap.Marker.Data marker)
			{
				entity.TryGetAssetIcon(ref marker.icon);
			}

			//[ISystem.Event<ILocation.UpdateEvent>(ISystem.Mode.Single, ISystem.Scope.Global)]
			//public static void OnLocationModified(ISystem.Info.Common info, Entity entity, ref XorRandom random, ref ILocation.UpdateEvent ev,
			//[Source.Owned] ref Transform.Data transform)
			//{
			//	App.WriteLine($"OnLocationModified(): {entity.GetName()}; {ev.h_asset}; {ev.data}");
			//}
			//#endif

#if SERVER
			//[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Global)]
			//[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Global)]
			[ISystem.Event<ILocation.RefreshEvent>(ISystem.Mode.Single, ISystem.Scope.Global)]
			public static void OnLocationRefreshEvent(ISystem.Info.Common info, Entity entity, ref ILocation.RefreshEvent ev,
			[Source.Owned] ref Location.Data location, [Source.Owned] ref WorldMap.Marker.Data marker)
			{
				marker.icon = ev.data.icon;
				marker.icon_offset = ev.data.icon_offset;
				marker.text_offset = ev.data.text_offset;
				marker.color = ev.data.color;
				marker.scale = ev.data.size;

				marker.flags.SetFlag(Data.Flags.Hidden, ev.data.flags.HasAny(ILocation.Flags.Hidden));
				marker.Sync(entity, true);
			}
#endif

#if CLIENT




			//[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
			//public static void OnGUI(Entity entity,
			//[Source.Owned] in Location.Data location,
			//[Source.Owned] in Interactable.Data interactable)
			//{
			//	if (interactable.IsActive())
			//	{
			//		var gui = new LocationGUI()
			//		{
			//			ent_location = entity,
			//			location = location,
			//		};
			//		gui.Submit();
			//	}
			//}

			//public partial struct MarkerGUI: IGUICommand
			//{
			//	public Entity ent_location;
			//	public Location.Data location;

			//	public static int selected_tab;

			//	public void Draw()
			//	{
			//		using (var window = GUI.Window.Standalone("Location###location.gui"))
			//		{
			//			this.StoreCurrentWindowTypeID(order: -150);
			//			if (window.show)
			//			{

			//			}
			//		}
			//	}
			//}
#endif
		}

		public static bool TryAdvance(this Road.Segment a, Road.Segment b, out Road.Segment c, ref int dir_sign, out int junction_index, bool skip_inner_junctions = false)
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

		//public static bool TryResolveJunction(Road.Segment a, Vector2 dir, out int junction_index, out Road.Segment b, out Road.Segment c, bool skip_inner_junctions = false)
		//{

		//}

		public static bool TryGetNearestJunction(this Road.Segment segment, out int junction_index, out float dist_sq, out int sign)
		{
			junction_index = -1;
			sign = 0;
			dist_sq = float.MaxValue;

			ref var road = ref segment.GetRoad();
			if (road.IsNull()) return false;

			if (road_segment_to_junction_index.TryGetValue(segment, ref junction_index))
			{
				dist_sq = 0.00f;
				return true;
			}

			var points = road.points.AsSpan();

			var index = (int)segment.index;
			var pos = points[index];

			var junction_index_a = -1;
			var junction_index_b = -1;

			var a = segment;
			//while (--a.index >= 0)
			while (--a.index < points.Length) // overflows to 255, not -1
			{
				if (road_segment_to_junction_index.TryGetValue(a, ref junction_index_a))
				{
					break;
				}
			}

			var b = segment;
			while (++b.index < points.Length)
			{
				if (road_segment_to_junction_index.TryGetValue(b, ref junction_index_b))
				{
					break;
				}
			}

			if ((uint)junction_index_a < road_junctions.Length && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_a].pos)))
			{
				junction_index = junction_index_a;
				sign = 1;
			}

			if ((uint)junction_index_b < road_junctions.Length && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_b].pos)))
			{
				junction_index = junction_index_b;
				sign = -1;
			}

			return junction_index != -1;
		}

		public static bool TryGetExitBranch(this Road.Segment segment, Vector2 dir, out Road.Junction.Branch branch, float dot_min = 0.10f, float dot_max = 1.00f)
		{
			branch = default;
			var dist_sq = 0.00f;

			ref var road = ref segment.GetRoad();
			if (road.IsNull()) return false;

			//{
			//	if (road_segment_to_junction_index.TryGetValue(segment, out var junction_index_tmp))
			//	{
			//		branch = new((ushort)junction_index_tmp, (byte)segment.index, 1);
			//		return false;
			//	}
			//}

			var junction_index = -1;
			var sign = 0;
			var points = road.points.AsSpan();

			var index = (int)segment.index;
			var pos = points[index];

			var a_index = byte.MaxValue;
			var b_index = byte.MaxValue;

			var junction_index_a = -1;
			var junction_index_b = -1;

			//var dir = Vector2.Zero;
			var dir_a = Vector2.Zero;
			var dir_b = Vector2.Zero;

			//var dot = -1.00f;
			var dot_a = -10.00f;
			var dot_b = -10.00f;

			var a = segment;
			//while (--a.index >= 0)
			while (--a.index < points.Length) // overflows to 255, not -1
			{
				if (road_segment_to_junction_index.TryGetValue(a, out var junction_index_tmp))
				{
					var dir_tmp = (pos - points[a.index]).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir, dir_tmp);

					if (dot_tmp > dot_a && dot_tmp >= dot_min && dot_tmp <= dot_max)
					{
						dot_a = dot_tmp;
						dir_a = dir_tmp;
						a_index = a.index;

						junction_index_a = junction_index_tmp;

						break;
					}
				}
			}

			var b = segment;
			while (++b.index < points.Length)
			{
				if (road_segment_to_junction_index.TryGetValue(b, out var junction_index_tmp))
				{
					var dir_tmp = (pos - points[b.index]).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir, dir_tmp);

					if (dot_tmp > dot_b && dot_tmp >= dot_min && dot_tmp <= dot_max)
					{
						dot_b = dot_tmp;
						dir_b = dir_tmp;
						b_index = b.index;

						junction_index_b = junction_index_tmp;

						break;
					}
				}
			}

			if ((uint)junction_index_a < road_junctions.Length && dot_a > dot_b && Maths.TrySetMax(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_a].pos)))
			{
				junction_index = junction_index_a;
				segment.index = a_index;
				sign = 1;
			}

			if ((uint)junction_index_b < road_junctions.Length && dot_b > dot_a && Maths.TrySetMax(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_b].pos)))
			{
				junction_index = junction_index_b;
				segment.index = b_index;
				sign = -1;
			}

			ref var junction = ref WorldMap.GetJunction(junction_index);
			if (junction.IsNotNull() && junction.TryGetSegmentIndex(segment, out var branch_segment_index))
			{
				branch = new((ushort)junction_index, (byte)branch_segment_index, (sbyte)sign);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool TryGetEntryBranch(this Road.Segment segment, Vector2 dir, out Road.Junction.Branch branch, float dot_min = 0.10f, float dot_max = 1.00f)
		{
			branch = default;
			var dist_sq = float.MaxValue;

			ref var road = ref segment.GetRoad();
			if (road.IsNull()) return false;

			var junction_index = -1;
			var sign = 0;
			var points = road.points.AsSpan();

			var index = (int)(uint)segment.index;
			var pos = points[index];

			var a_index = byte.MaxValue;
			var b_index = byte.MaxValue;

			var junction_index_a = -1;
			var junction_index_b = -1;

			//var dir = Vector2.Zero;
			var dir_a = Vector2.Zero;
			var dir_b = Vector2.Zero;

			//var dot = -1.00f;
			var dot_a = -1.00f;
			var dot_b = -1.00f;

			var a = segment;
			//while (--a.index >= 0)
			while (--a.index < points.Length) // overflows to 255, not -1
			{
				if (road_segment_to_junction_index.TryGetValue(a, out var junction_index_tmp))
				{
					var dir_tmp = (pos - points[a.index]).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir, dir_tmp);

					if (dot_tmp > dot_a && dot_tmp >= dot_min && dot_tmp <= dot_max)
					{
						dot_a = dot_tmp;
						dir_a = dir_tmp;
						a_index = a.index;

						junction_index_a = junction_index_tmp;

						break;
					}
				}
			}

			var b = segment;
			while (++b.index < points.Length)
			{
				if (road_segment_to_junction_index.TryGetValue(b, out var junction_index_tmp))
				{
					var dir_tmp = (pos - points[b.index]).GetNormalizedFast();
					var dot_tmp = Vector2.Dot(dir, dir_tmp);

					if (dot_tmp > dot_b && dot_tmp >= dot_min && dot_tmp <= dot_max)
					{
						dot_b = dot_tmp;
						dir_b = dir_tmp;
						b_index = b.index;

						junction_index_b = junction_index_tmp;

						break;
					}
				}
			}

			if ((uint)junction_index_a < road_junctions.Length && dot_a > dot_b && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_a].pos)))
			{
				junction_index = junction_index_a;
				segment.index = a_index;
				sign = 1;
			}

			if ((uint)junction_index_b < road_junctions.Length && dot_b > dot_a && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_b].pos)))
			{
				junction_index = junction_index_b;
				segment.index = b_index;
				sign = -1;
			}

			ref var junction = ref WorldMap.GetJunction(junction_index);
			if (junction.IsNotNull() && junction.segments_count > 1 && junction.TryGetSegmentIndex(segment, out var branch_segment_index))
			{
				branch = new((ushort)junction_index, (byte)branch_segment_index, (sbyte)sign);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool TryGetNearestBranch(this Road.Segment segment, out Road.Junction.Branch branch)
		{
			branch = default;
			var dist_sq = float.MaxValue;

			ref var road = ref segment.GetRoad();
			if (road.IsNull()) return false;

			var junction_index = -1;
			var sign = 0;
			var points = road.points.AsSpan();

			var index = (int)(uint)segment.index;
			var pos = points[index];

			var junction_index_a = -1;
			var junction_index_b = -1;

			var a = segment;
			//while (--a.index >= 0)
			while (--a.index < points.Length) // overflows to 255, not -1
			{
				if (road_segment_to_junction_index.TryGetValue(a, ref junction_index_a))
				{
					break;
				}
			}

			var b = segment;
			while (++b.index < points.Length)
			{
				if (road_segment_to_junction_index.TryGetValue(b, ref junction_index_b))
				{
					break;
				}
			}

			if ((uint)junction_index_a < road_junctions.Length && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_a].pos)))
			{
				junction_index = junction_index_a;
				segment.index = a.index;
				sign = 1;
			}

			if ((uint)junction_index_b < road_junctions.Length && Maths.TrySetMin(ref dist_sq, Vector2.DistanceSquared(pos, road_junctions[junction_index_b].pos)))
			{
				junction_index = junction_index_b;
				segment.index = b.index;
				sign = -1;
			}

			ref var junction = ref WorldMap.GetJunction(junction_index);
			if (junction.IsNotNull() && junction.TryGetSegmentIndex(segment, out var branch_segment_index))
			{
				branch = new((ushort)junction_index, (byte)branch_segment_index, (sbyte)sign);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool TryGetNextJunction(this Road.Segment a, int dir_sign, out int junction_index, out Road.Segment b, out Road.Segment c, bool skip_inner_junctions = false)
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

					if (!skip_inner_junctions && road_segment_to_junction_index.TryGetValue(c, ref junction_index))
					{
						return true;
					}
				}

				if (road_segment_to_junction_index.TryGetValue(c, ref junction_index))
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

					if (!skip_inner_junctions && road_segment_to_junction_index.TryGetValue(c, ref junction_index))
					{
						return true;
					}
				}

				if (road_segment_to_junction_index.TryGetValue(c, ref junction_index))
				{
					return true;
				}
			}

			return false;
		}

		public static bool TryResolveBranch(this Road.Junction junction, Vector2 dir, out Road.Junction.Branch branch)
		{
			branch = default;

			if (junction.IsValid())
			{
				var junction_segments = junction.segments.Slice(junction.segments_count);
				var dot = -1.00f;

				for (var i = 0; i < junction_segments.Length; i++)
				{
					ref var j_segment = ref junction_segments[i];
					ref var j_road = ref j_segment.GetRoad();
					//if (j_road.IsNull() || j_road.type != type) continue;

					var j_points = j_road.points.AsSpan();
					//if (j_segment.index >= j_points.Length)
					//{
					//	App.WriteLine($"{j_segment.index}/{j_points.Length}");
					//}

					var j_pos = j_points[j_segment.index];

					if (j_segment.index < j_points.Length - 1)
					{
						var dir_tmp = (j_points[j_segment.index + 1] - j_pos).GetNormalizedFast();
						var dot_tmp = Vector2.Dot(dir, dir_tmp);
						if (dot_tmp > dot)
						{
							dot = dot_tmp;
							branch = new(junction.index, (byte)i, 1);
						}
					}

					if (j_segment.index > 0)
					{
						var dir_tmp = (j_points[j_segment.index - 1] - j_pos).GetNormalizedFast();
						var dot_tmp = Vector2.Dot(dir, dir_tmp);
						if (dot_tmp > dot)
						{
							dot = dot_tmp;
							branch = new(junction.index, (byte)i, -1);
						}
					}
				}
			}

			return branch.sign != 0;
		}

		public static bool TryAdvanceJunction(this Road.Segment a, Road.Segment b, Road.Segment c, int junction_index, out Road.Junction.Branch c_branch, out Road.Segment c_alt, out int c_alt_sign, out float c_alt_dot, float dot_min = 0.40f, float dot_max = 1.00f, bool ignore_limits = false)
		{
			var ok = false;
			c_alt = default;
			c_alt_dot = -1.00f;
			c_alt_sign = default;
			c_branch = default;

			if ((uint)junction_index < road_junctions.Length)
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

				var dot = Maths.Min(dot_max, Vector2.Dot(dir_ab, dir_bc));

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

		public static readonly HashSet<ulong> segments_visited = new(128);
		public static readonly Queue<RoadConnection> junctions_queue = new(64);

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
		public static Vector2 worldmap_window_offset;

		public static Vector2 worldmap_mouse_position;
		public static Vector2 worldmap_mouse_position_snapped;

		public static float rotation;

		public static Road.Chain edit_road;

		public static int? edit_points_index;
		public static Vec2i16[] edit_points_s16;
		public static Vector2[] edit_points_f32;

		public static Doodad.Renderer.Data? clipboard_doodad;

		public static IAsset.IDefinition edit_asset;
		public static IScenario.Handle h_scenario = "krumpel";

		// TODO: remove this
		public static ILocation.Handle h_selected_location;

		//public static Road.Handle road_ch;
		public static int? edit_doodad_index;

		public static Texture.Handle h_texture_bg_00 = "worldmap.bg.00";
		public static Texture.Handle h_texture_icons = "worldmap.icons.00";
		public static Texture.Handle h_texture_line_00 = "worldmap.border.00";
		public static Texture.Handle h_texture_line_01 = "worldmap.border.01";
		public static Texture.Handle h_texture_line_02 = "worldmap.border.02";

		public static Vector2 mouse_pos_old;
		public static Vector2 mouse_pos_new;

		public static readonly HashSet<IAsset.IDefinition> hs_pending_asset_saves = new(64);

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
			//if (WorldMap.IsOpen)
			{
				interactable.show = WorldMap.interacted_entity_cached == entity
				&& WorldMap.IsOpen
				//&& WorldMap.ts_last_draw.GetMilliseconds() <= 50 // TODO: shithack
				&& interactable.flags.HasNone(Interactable.Flags.No_Window)
				&& interactable.window_size.IsNotNil();
				//&& entity.CanPlayerControlUnit(Client.GetPlayerHandle());
			}
		}
#endif
	}
}

