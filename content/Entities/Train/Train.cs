
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
				Docked = 1u << 2,
			}

			public ITransport.Handle h_transport;
			public Train.Data.Flags flags;

			public Limit.Mask<ILocation.Buildings> mask_stop_buildings;
			public Limit.Mask<ILocation.Categories> mask_stop_categories;

			public Road.Segment segment_a;
			public Road.Segment segment_b;
			public Road.Segment segment_c;
			public Road.Segment segment_stop;

			public Vector2 direction_old;
			public Vector2 direction;

			public Vector2 dir_ab;
			public Vector2 dir_bc;

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

			public float t_stop_departing;

			public int sign;

			public Data()
			{
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

		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnUpdate(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Train.Data train, [Source.Owned] ref Transform.Data transform)
		{
			if (!train.segment_a.IsValid()) return;
			if (!train.segment_b.IsValid()) return;
			if (!train.segment_c.IsValid()) return;

			var show_debug = false;

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

			//#if CLIENT
			//			region.DrawDebugDir(train.segment_a.GetPosition(), train.dir_ab, Color32BGRA.Yellow);
			//			region.DrawDebugDir(train.segment_b.GetPosition(), train.dir_bc, Color32BGRA.Yellow);
			//			region.DrawDebugDir(train.segment_b.GetPosition(), train.direction, Color32BGRA.Magenta);
			//#endif


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
						train.t_stop_departing = info.WorldTime + 15.00f;
					}
				}
				else
				{
					train.speed = 1.00f;
				}

				train.speed_current = Maths.MoveTowards(train.speed_current, train.speed, (train.speed_current < train.speed ? train.acceleration : train.brake) * info.DeltaTime);

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
						//App.WriteLine($"junction {junction_index} ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index}to {segment_c_new.chain.h_prefecture}:{segment_c_new.chain.index}:{segment_c_new.index})");

						if (WorldMap.TryAdvanceJunction(train.segment_a, train.segment_b, train.segment_c, junction_index, out var c_alt_segment, out var c_alt_sign, out var c_alt_dot, dot_min: train.dot_min, dot_max: train.dot_max, ignore_limits: !ok))
						{
							//App.WriteLine($"advanced junction ({c_alt_dot} >= {train.dot_min} <= {train.dot_max}) ({train.segment_a.chain.h_prefecture}:{train.segment_a.chain.index}:{train.segment_a.index} to {train.segment_b.chain.h_prefecture}:{train.segment_b.chain.index}:{train.segment_b.index})");

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

					train.direction_old = train.direction;
					train.road_distance_current -= train.road_distance_target;
					train.direction = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalized(out train.road_distance_target);

					train.dir_ab = (train.segment_b.GetPosition() - train.segment_a.GetPosition()).GetNormalized();
					train.dir_bc = (train.segment_c.GetPosition() - train.segment_b.GetPosition()).GetNormalized();

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
					train.road_distance_current += train.speed_current * info.DeltaTime;

					//transform.SetRotation(transform.rotation, Maths.LerpAngle(train.dir_ab.GetAngleRadians(), train.direction.GetAngleRadians(), Maths.NormalizeClamp(train.road_distance_current, 0.350f)));

					//train.direction = Vector2.Lerp(train.dir_ab, train.dir_bc, (Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) + Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.50f);
					//train.direction = train.dir_ab; // Vector2.Lerp(train.dir_ab, train.dir_bc, Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)).GetNormalizedFast();

					//transform.SetRotation(Vector2.Lerp(train.dir_ab, train.dir_bc, ((Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) * 0.75f) + (Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.25f)).GetAngleRadians());
					transform.position = train.segment_a.GetPosition() + (train.direction * train.road_distance_current);
					//transform.SetRotation(Vector2.Lerp(train.dir_ab, train.dir_bc, ((Maths.InvLerp01(0.00f, 0.50f, train.road_distance_current) * 0.75f) + (Maths.InvLerp01(train.road_distance_target - 0.50f, train.road_distance_target, train.road_distance_current)) * 0.25f)).GetAngleRadians());

					transform.SetRotation(transform.rotation, Maths.LerpAngle(train.direction_old.GetAngleRadians(), train.direction.GetAngleRadians(), Maths.NormalizeClamp(train.road_distance_current, 0.350f)));

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

				using (var window = GUI.Window.Standalone($"train.{ent_train}", rect.GetPosition(), size: rect.GetSize(), flags: GUI.Window.Flags.None, force_position: true))
				{
					using (GUI.ID.Push("train"))
					{
						//GUI.DrawCircle(region.WorldToCanvas(this.transform.GetInterpolatedPosition()),, Color32BGRA.Magenta, segments: 3, layer: GUI.Layer.Foreground);
						var is_pressed = GUI.ButtonBehavior("train", rect, out var is_hovered, out var is_held);

						var sprite = sprite_train;

						var rot = transform.GetInterpolatedRotation();
						var rot_snapped = Maths.Snap(rot, MathF.PI * 0.250f);
						var rot_rem = Maths.DeltaAngle(rot, rot_snapped);

						var rot_invlerp = Maths.InvLerp01(-MathF.PI, MathF.PI, rot_snapped);
						sprite.frame.X = (uint)(rot_invlerp * 8);

						// the sprites aren't 45°
						switch (sprite.frame.X)
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
								position = Maths.Snap(transform.position, 1.00f / 32.00f),
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
							if (WorldMap.selected_entity == ent_train) WorldMap.selected_entity = default;
							else WorldMap.selected_entity = ent_train;

							App.WriteLine("press");

							GUI.SetDebugEntity(ent_train);
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

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity, [Source.Owned] ref Train.Data train, [Source.Owned] ref Transform.Data transform)
		{
			var gui = new Train.TrainGUI()
			{
				ent_train = entity,
				train = train,
				transform = transform
			};
			gui.Submit();


			//App.WriteLine("GUI");
		}
#endif
	}
}

