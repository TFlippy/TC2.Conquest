using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		public struct IntroGUI: IGUICommand
		{
			public static bool hide = false;

			public void Draw()
			{
				var size = new Vector2(24 * 33, 48 * 12);

				//using (var window = GUI.Window.Standalone("Intro"u8, size: size,
				//position: new(GUI.CanvasSize.X * 0.50f, 96), pivot: new(0.50f, 0.00f), force_position: false))
				//{
				//	this.StoreCurrentWindowTypeID();
				//	if (window.show)
				//	{

				using (var widget = Sidebar.Widget.New(identifier: "intro", name: "Overview",
				icon: new Sprite(GUI.tex_icons_widget, 16, 16, 13, 3), size: size,
				enabled: true, lockable: false, order: 1000.00f, flags: Sidebar.Widget.Flags.Open_Centered | Sidebar.Widget.Flags.No_Collapse | Sidebar.Widget.Flags.Starts_Open))
				{
					ref readonly var kb = ref Control.GetKeyboard();
					if (kb.GetKeyDown(Keyboard.Key.Tab))
					{
						if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
						{
							if (widget.IsActive()) Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
							widget.SetActive(false);
						}
						else
						{
							Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f);
							widget.SetActive(true);
						}
					}

					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						using (var group_main = GUI.Group.New(size: GUI.Av))
						{
							//GUI.DrawWindowBackground(GUI.tex_window_character, new Vector4(16, 16, 16, 16));

							ref var g_region = ref World.GetGlobalRegion();
							ref var game_info = ref Client.GetGameInfo();
							ref var scenario_data = ref WorldMap.h_scenario.GetData();
							ref var world_global = ref g_region.GetGlobalComponent<World.Global>();
							ref var conquest = ref g_region.GetGlobalComponent<Conquest.Gamemode>();

							using (var group_header = GUI.Group.New(size: new(512, 64)))
							{
								group_header.DrawBackground(GUI.tex_window);

								GUI.TitleCentered("Welcome to Territory Control 2"u8, size: 32, pivot: new(0.50f, 0.50f), offset: new(0, -8));

								//GUI.TitleCentered(game_info.name, size: 20, pivot: new(0.50f, 0.50f), offset: new(0, 14));
								GUI.TitleCentered("(still work in progress)"u8, size: 20, pivot: new(0.50f, 0.50f), offset: new(0, 14), color: GUI.font_color_desc);

							}

							GUI.SameLine();

							using (var group_header = GUI.Group.New(size: new(GUI.RmX, 64), padding: new(8, 4)))
							{
								group_header.DrawBackground(GUI.tex_window);

								if (GUI.DrawDiscordButton("btn.discord"u8, "SdRrPGbp2G", size: new(64, GUI.RmY), scale: 2))
								{

								}

								//if (GUI.DrawButton("DEV: Hide"u8, size: new(96, 40)))
								//{
								//	hide = true;
								//}
								//group_header.DrawBackground(GUI.tex_panel);

							}

							GUI.SeparatorThick();

							using (var group_body = GUI.Group.New2(size: GUI.Rm, padding: new(8, 10, 8, 8)))
							{
								group_body.DrawBackground(GUI.tex_scoreboard_bg);

								//GUI.DrawSprite("ui_logo_title");

								using (var group_l = GUI.Group.New(size: new(384, GUI.RmY)))
								{
									using (var group_a = GUI.Group.New(size: new(GUI.RmX, 48 * 7), padding: new(4)))
									{
										group_a.DrawBackground(GUI.tex_window_widget);

										if (false) // lol exactly same intro message as in TC1
										{
											GUI.Title("Territory Control 2's world is a cruel place."u8);
											GUI.NewLine(4);
											GUI.Title("You may end up getting shot, slaved, stabbed,\nblown up, turned into a monster and such."u8);
											GUI.NewLine(8);
											GUI.Title("If you're a beginner, consider joining an existing faction by asking one of its members to join."u8);
										}
										else
										{
											if (true)
											{
												using (GUI.Wrap.Push(GUI.RmX))
												{
													GUI.Title("Introduction"u8, size: 24);
													//GUI.TextShaded("- <TODO: >"u8);

													GUI.Title("Getting Started"u8, size: 16);
													GUI.TextShaded("- Consider joining a faction, and feel free to ask others for help!"u8);
													GUI.TextShaded("- You can enter a region immediately and play as a random character."u8);
													GUI.TextShaded("- Create a character by pressing \"New Main Character\" in the top menu."u8);
													GUI.TextShaded("- Move your character to one of a region's checkpoint icons to enter the region."u8);

													GUI.NewLine(4);

													GUI.Title("Regions"u8, size: 16);
													GUI.TextShaded("- All regions contain different resources and challenges."u8);
													GUI.TextShaded("- Start new factions, mass-produce stupidly powerful weapons, build a mining and processing company, expand your territory!"u8);

													GUI.NewLine(4);

													GUI.Title("Basic Gameplay"u8, size: 16);
													GUI.TextShaded("- To build, equip a wrench for access to the build menu."u8);
													GUI.TextShaded("- Press E on buildings to interact with them."u8);
													GUI.TextShaded("- Beware of NPCs and players, for death is quick and violence is plenty."u8);

													//if (true) // if multiplayer
													//{
													//	GUI.TextShaded("- "u8);
													//}
													//else
													//{
													//	GUI.TextShaded("- "u8);
													//}

													//GUI.Separator(spacing: 8);

													//GUI.TextShaded("- Select your starting region"u8);
													//GUI.DrawHoverTooltip("Either in the menu on the left side of your screen, or directly on the World Map (orange symbol)."u8);
												}
											}
											else if (scenario_data.IsNotNull())
											{
												GUI.Title(scenario_data.name, size: 24);
												GUI.TextShaded(scenario_data.desc);
											}
										}
									}
								
									using (var group_airships = GUI.Group.New(size: GUI.Rm, padding: new(4)))
									{
										group_airships.DrawBackground(GUI.tex_frame);

										//g_region.RunSystem(WorldMap.Airship.System_Fetch);
									}
								}

								GUI.SameLine();

								using (var group_r = GUI.Group.New(size: GUI.Rm))
								{
									//group_r.DrawBackground(GUI.tex_window_widget);

									using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32)))
									{
										group_title.DrawBackground(GUI.tex_slot_filled);

										//GUI.Title(map_info.name, size: 24);
										GUI.TitleCentered("Region List"u8, size: 28, pivot: new(0.00f, 0.50f), offset: new(8, 0));
										if (world_global.IsNotNull())
										{
											GUI.TitleCentered($"{world_global.date.ToDateString()} S.D.", size: 20, color: GUI.font_color_default, 
											pivot: new(1.00f, 0.50f), offset: new(-6, -1));
											GUI.DrawHoverTooltip("Current in-game calendar date."u8);
										}
										//GUI.TextShadedCentered("derp"u8, pivot: new(1.00f, 0.50f));
									}

									GUI.SeparatorThick();

									using (var group_a = GUI.Scrollbox.New("sb.regions"u8, size: GUI.Rm, padding: new(6)))
									{
										group_a.group_frame.DrawBackground(GUI.tex_window, inner: true);

										const bool enable_quick_start = false;
										if (enable_quick_start)
										{
											using (var group_top = GUI.Group.New(size: new(GUI.RmX, 32)))
											{
												GUI.TitleCentered("Quick Start"u8, size: 32, pivot: new(0.00f, 0.50f), offset: new(4, 0));
											}

											GUI.SeparatorThick();

											using (GUI.ID<IntroGUI>.Push("ps.industrialized"u8))
											using (var group_playstyle = GUI.Group.New(size: new(GUI.RmX, 80), padding: new(4)))
											{
												group_playstyle.DrawBackground(GUI.tex_window);

												if (GUI.DrawIconButton("btn.test"u8, sprite: new("specialization_icons", 32, 32, 0, 0), size: new(GUI.RmY)))
												{

												}

												GUI.SameLine();

												using (var group_text = GUI.Group.New(size: GUI.Rm.SubY(24), padding: new(2)))
												{
													GUI.Title("Industrialized"u8, size: 24);
													GUI.TextShaded("- <TODO>"u8);
													GUI.TextShaded("- Fewer natural resources and wildlife"u8);
												}
												//GUI.TitleCentered("Builder"u8, size: 24, pivot: new(0.00f, 0.00f), offset: new(2));
											}

											using (GUI.ID<IntroGUI>.Push("ps.frontier"u8))
											using (var group_playstyle = GUI.Group.New(size: new(GUI.RmX, 80), padding: new(4)))
											{
												group_playstyle.DrawBackground(GUI.tex_window);

												if (GUI.DrawIconButton("btn.test"u8, sprite: new("specialization_icons", 32, 32, 0, 0), size: new(GUI.RmY)))
												{
												}

												GUI.SameLine();

												using (var group_text = GUI.Group.New(size: GUI.Rm.SubY(24), padding: new(2)))
												{
													GUI.Title("Frontier"u8, size: 24);
													GUI.TextShaded("- <TODO>"u8);
												}
												//GUI.TitleCentered("Builder"u8, size: 24, pivot: new(0.00f, 0.00f), offset: new(2));
											}
										}

										if (true)
										{
											ref var mod_context = ref App.GetModContext();
											ref var world_info = ref Client.GetWorldInfo();

											var is_loading = Client.IsLoadingRegion();
											var has_region = is_loading || Client.HasRegion();

											for (var region_id = 1u; region_id < Region.max_count; region_id++)
											{
												ref var map_info = ref World.GetMapInfo((byte)region_id);
												if (map_info.IsNotNull())
												{
													var h_location = map_info.h_location;
													ref var location_data = ref h_location.GetData();
													if (location_data.IsNotNull())
													{
														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 64)))
														{
															if (group_row.IsVisible())
															{
																//var h_location = map_info.h_location;
																//ref var location_data = ref h_location.GetData();

																var ent_location = h_location.GetGlobalEntity();
																var map_identifier = world_info.regions[region_id];

																var player_count = (byte)0;
																var is_locked = false;

																ref var site = ref ent_location.GetComponent<Site.Data>();
																if (site.IsNotNull())
																{
																	player_count = site.player_count;
																	is_locked = site.flags.HasAny(Site.Data.Flags.Locked);
																}

																//var map_asset = mod_context.GetMap(map_identifier);
																//if (map_asset != null)
																//{
																//	var tex_thumbnail = map_asset.GetThumbnail();
																//	if (tex_thumbnail != null)
																//	{
																//		GUI.DrawTexture(tex_thumbnail.Identifier, rect_icon, GUI.Layer.Window, color: color_thumbnail.WithAlphaMult(alpha));
																//	}

																//	GUI.DrawBackground(GUI.tex_frame_white, rect_icon, padding: new(2), color: color_frame.WithAlphaMult(alpha));
																//}

																using (GUI.ID<Region.Info, IMap.Info>.Push(region_id))
																{
																	var is_selected = WorldMap.interacted_entity_cached == ent_location;
																	var contains = WorldMap.hs_selected_entities.Contains(ent_location);
																	var is_selectable = true; // !is_locked; // WorldMap.CanPlayerControlUnit(ent_location, Client.GetPlayerHandle());
																	var is_current_region = region_id == Client.GetRegionID();
																	//var is_current_region = 
																	//using (GUI.Disabled.Push(!is_selectable)) //, GUI.GetEnabledAlpha(is_selectable)))
																	//using (GUI.Disabled.Push(true, !is_selectable))
																	{
																		group_row.DrawBackground(GUI.tex_panel);

																		using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmY)))
																		{
																			GUI.DrawMapThumbnail(h_location, size: GUI.Rm, show_frame: false);
																			GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
																		}

																		GUI.SameLine();

																		using (var group_info = GUI.Group.New(size: GUI.Rm, padding: new(4, 2)))
																		{
																			//group_info.DrawBackground(GUI.tex_slot_simple);

																			//GUI.TitleCentered(map_info.name, size: 24, pivot: new(0.00f, 0.00f), offset: new(4, 2));

																			using (var group_title = GUI.Group.New(size: new(GUI.RmX, 24)))
																			{
																				//GUI.Title(map_info.name, size: 24);
																				GUI.TitleCentered(map_info.name, size: 24, pivot: new(0.00f, 0.50f));
																				//GUI.TextShadedCentered("derp"u8, pivot: new(1.00f, 0.50f));
																				//GUI.TextShadedCentered(player_count, format: "'Players: '0", font: GUI.Font.Superstar, size: 16, pivot: new(1.00f, 0.50f));
																				GUI.TextShadedCentered(player_count, format: "'Players: '0", font: GUI.Font.Superstar, size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_default.WithAlphaMult(GUI.GetEnabledAlpha(!is_locked)));

																			}

																			GUI.SeparatorThick();

																			//GUI.TextShadedCentered(location_data.h_prefecture.GetShortName(), pivot: new(0.00f, 1.00f), offset: new(2, -2));
																			//GUI.NewLine();

																			using (var group_bottom = GUI.Group.New(size: GUI.Rm))
																			{
																				if (is_current_region)
																				{
																					if (GUI.DrawButton("Exit"u8, size: new(64, GUI.RmY), error: is_loading, color: GUI.col_button_error))
																					{
																						Client.RequestSetActiveRegion(0, delay_seconds: 0.50f);
																						//GUI.RegionMenu.ToggleWidget(false);
																					}
																				}
																				else if (is_locked)
																				{
																					if (GUI.DrawButton("Locked"u8, size: new(72, GUI.RmY), error: true, color: GUI.col_button))
																					{
																						
																					}
																				}
																				else
																				{
																					//widget.SetActive(false);

																					if (GUI.DrawButton("Join"u8, size: new(64, GUI.RmY), error: is_loading, color: GUI.col_button_ok))
																					{
																						Client.RequestSetActiveRegion((byte)region_id, delay_seconds: 0.50f).ContinueWith(async (x) =>
																						{
																							await App.WaitRender();
																							GUI.RegionMenu.ToggleWidget(false);
																							widget.SetActive(false);
																						});
																					}
																				}

																				//GUI.SameLine();

																				//if (GUI.DrawButton("View"u8, size: new(64, GUI.RmY), error: is_loading, color: GUI.col_button))
																				//{

																				//}

																				GUI.SameLine();

																				using (var group_left = GUI.Group.New(size: GUI.Rm, padding: new(3)))
																				{
																					if (site.IsNotNull())
																					{
																						var date_unlock = conquest.region_unlock_dates[site.region_id];
																						if (site.flags.HasAny(Site.Data.Flags.Locked))
																						{
																							//GUI.TitleCentered($"CLOSED UNTIL {date_unlock.ToDateString()} S.D.", size: 12, font: GUI.Font.Editia, color: GUI.font_color_red_b, pivot: new(1.00f, 0.50f), offset: new(0, 0));
																							GUI.TitleCentered($"CLOSED UNTIL {date_unlock.ToDateString()} S.D.", size: 12, font: GUI.Font.Editia, color: GUI.font_color_red_b, pivot: new(1.00f, 0.50f), offset: new(0, 0));
																							GUI.DrawHoverTooltip(arg: (world_global.date, date_unlock, world_global.speed), draw: static (x) =>
																							{
																								var date_delta = (x.arg.date_unlock - x.arg.date);
																								var time_delta_irl_s = ((double)(date_delta.ticks / App.tickrate) / (double)x.arg.speed);
																								var timespan_irl = TimeSpan.FromSeconds(time_delta_irl_s);

																								var (days, ticks_rem) = Math.DivRem((int)date_delta.ticks, (int)ImperialDateTime.ticks_per_day);
																								var hours = ticks_rem / ImperialDateTime.ticks_per_hour;

																								GUI.Title("This map is currently locked."u8);
																								//GUI.LabelShaded(text: "Unlocks in:"u8, value: days, format: "0 'hours'", width: 192);
																								GUI.TextShaded($"Unlocks in {days} days and {hours} hours, S.D.");
																								//GUI.TextShaded($"({timespan_irl:%d'.'hh':'mm':'ss} irl)");
																								//GUI.TextShaded($"{timespan_irl.TotalDays:0} days, {timespan_irl:hh':'mm':'ss}");
																								GUI.TextShaded($"({timespan_irl.TotalHours:0}h {timespan_irl.Minutes}min irl)");
																								//GUI.TextShaded($"({TimeSpan.FromSeconds(time_delta_irl_s).TotalSeconds:0} s irl)");
																							});
																						}
																						else
																						{
																							GUI.TitleCentered("UNLOCKED"u8, size: 12, font: GUI.Font.Editia, color: GUI.font_color_green_b.WithAlpha(224), pivot: new(1.00f, 0.50f), offset: new(0, 0));
																						}
																					}

																					//GUI.LabelShaded("Players:"u8, player_count, font_a: GUI.Font.Superstar, font_b: GUI.Font.Superstar, size_a: 16, size_b: 16, width: 80);
																					//GUI.LabelShaded("Players:"u8, player_count * 16, width: 72);
																					//GUI.LabelShaded("Hazard:"u8, player_count, width: 72);
																				}
																			}
																		}

																		if (GUI.Selectable3(ent_location.GetShortID(), group_row.GetOuterRect(), is_selected || contains, is_readonly: !is_selectable))
																		{
																			var result = WorldMap.SelectUnitBehavior(ent_location, WorldMap.SelectUnitMode.Single, WorldMap.SelectUnitFlags.Multiselect | WorldMap.SelectUnitFlags.Hold_Shift | WorldMap.SelectUnitFlags.Toggle, selected: is_selected);
																			if (result.HasAny(WorldMap.SelectUnitResults.Changed))
																			{
																				if (result.HasAny(WorldMap.SelectUnitResults.Removed))
																				{
																					WorldMap.interacted_entity = default;
																				}
																				else
																				{
																					WorldMap.interacted_entity.Toggle(ent_location, true);
																					WorldMap.FocusEntity(ent_location, interact: false);
																				}
																			}

																			if (WorldMap.interacted_entity_cached == ent_location && (ent_location.TryGetAsset(out ILocation.Definition location_asset))) // || ent_parent.TryGetAsset(out location_asset)))
																			{
																				WorldMap.h_selected_location = location_asset;
																			}
																			else
																			{
																				WorldMap.h_selected_location = default;
																			}
																		}
																	}

																	//if (GUI.IsItemHovered())
																	//{
																	//	if (GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
																	//	{
																	//		WorldMap.FocusEntity(ent_location, interact: false);
																	//	}
																	//	GUI.DrawEntityMarker(ent_location, cross_size: 0.125f, layer: GUI.Layer.Foreground);
																	//}
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
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnIntroGUI(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			//return;

			//ref var player_data = ref Client.GetPlayerData(out var player_asset);
			//if (player_data.IsNotNull())
			{
				//var h_character = player_data.h_character_main;
				//var h_character_current = Client.GetCharacterHandle();

				//Conquest.IntroGUI.hide = false;
				//if (!Conquest.IntroGUI.hide)
				{
					var gui = new Conquest.IntroGUI();
					gui.Submit();
				}
			}
		}
#endif

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void ManualSystemTest(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [ISystem.Parameter] ref SpawnInfo arg,
		[Source.Owned] ref Storage.Data storage, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction)
		{
			App.WriteLine($"ManualSystemTest: {entity}; storage: {storage.flags}; pos: {transform.position}; faction: {faction.id}", App.Color.Green);
			if (arg.IsNotNull())
			{
				App.WriteLine($"- faction: {arg.h_faction}; pos: {arg.pos}", color: App.Color.DarkGreen);
			}
		}

		public ref struct FetchNearestSpawnArgs
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,
			}

			public enum SearchType: byte
			{
				Nearest = 0,

				Left,
				Right
			}

			public Entity ent_selected_spawn;

			public Vec2f pos_pivot;
			public float nearest_dist_sq;
			public float unused;

			public IFaction.Handle h_faction;
			public ICharacter.Handle h_character;

			public FetchNearestSpawnArgs.SearchType search_type;
			public FetchNearestSpawnArgs.Flags flags;

			public SpawnInfo spawn_info;

			public FetchNearestSpawnArgs(Entity ent_selected_spawn, Vec2f pos_pivot, IFaction.Handle h_faction, ICharacter.Handle h_character, SearchType search_type, Flags flags)
			{
				this.ent_selected_spawn = ent_selected_spawn;
				this.pos_pivot = pos_pivot;
				this.nearest_dist_sq = float.MaxValue;
				this.h_faction = h_faction;
				this.h_character = h_character;
				this.search_type = search_type;
				this.flags = flags;
			}
		}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void FetchNearestSpawn(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		[ISystem.Parameter] ref FetchNearestSpawnArgs args,
		[Source.Owned] in Spawn.Data spawn, [Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional(true)] ref Dormitory.Data dormitory, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (args.IsNotNull())
			{
				//App.WriteLine($"{entity} vs {args.ent_selected_spawn}; {args.nearest_dist_sq}; {args.spawn_info.ent_spawn}");
				if (entity != args.ent_selected_spawn && spawn.IsVisibleToFaction(h_faction: args.h_faction, h_faction_spawn: faction.id))
				{
					var pos = (Vec2f)transform.position;
					//App.WriteLine($"- {pos} vs {args.pos_pivot}");

					float dist;
					switch (args.search_type)
					{
						default:
						case FetchNearestSpawnArgs.SearchType.Nearest:
						{
							//dist = Maths.GetDistanceSq(pos, args.pos_pivot);
						}
						break;

						case FetchNearestSpawnArgs.SearchType.Left:
						{
							if (pos.x > args.pos_pivot.x) return;
							//dist = Maths.GetDistanceSq(pos, args.pos_pivot);
						}
						break;

						case FetchNearestSpawnArgs.SearchType.Right:
						{
							if (pos.x < args.pos_pivot.x) return;
							//dist = Maths.GetDistanceSq(pos, args.pos_pivot);
						}
						break;
					}

					//App.WriteLine($"- {pos} vs {args.pos_pivot}");


					dist = Maths.GetDistanceSq(pos, args.pos_pivot);
					if (dist < args.nearest_dist_sq)
					{
						//App.WriteLine(entity.GetFullName());

						args.nearest_dist_sq = dist;
						args.spawn_info = new SpawnInfo()
						{
							ent_spawn = entity,
							pos = pos,

							flags = SpawnInfo.Flags.None,
							h_faction = faction.id,
						};
					}
				}
			}
			else
			{
				//info.SetInterrupted(entity);
			}
		}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void FetchSpawns(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity, [ISystem.Parameter] ref SpanList<SpawnInfo> arg,
		[Source.Owned] in Spawn.Data spawn, [Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional(true)] ref Dormitory.Data dormitory, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (arg.IsNotNull())
			{

			}
			else
			{
				info.SetInterrupted(entity);
			}
		}

		public struct SpawnInfo()
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Is_Visible = 1 << 0,


			}


			public Entity ent_spawn;
			public Vec2f pos;

			public SpawnInfo.Flags flags;

			public IFaction.Handle h_faction;

		}


#if CLIENT
		[Shitcode]
		public struct RespawnGUI: IGUICommand
		{
			public static readonly Texture.Handle tex_icons_minimap = "ui_icons_minimap";
			//public static Entity? ent_selected_spawn_new;

			public Entity ent_respawn;
			public IFaction.Handle h_faction;
			public Respawn.Data respawn;

			//public static bool[] selected_items = new bool[32];
			//public static int? selected_character_index;

			//public static Entity ent_selected_spawn;
			//public static ulong selected_character_id;
			public static float current_cost;

			public const int max_item_count = 8;

			internal const float lh = 48;

			public static ICharacter.Handle h_selected_character;



			[FixedAddressValueType, Region.Local] public static SpawnInfo selected_spawn_info_cached;
			[FixedAddressValueType, Region.Local] public static FixedArray32<SpawnInfo> buffer_spawn_infos;

			//var rpc = new RespawnExt.SetSpawnRPC()
			//{
			//	ent_spawn = ent_selected_spawn_new.Value
			//};
			//rpc.Send(Client.GetEntity());

			//public static PinnedList<SpawnInfo> list_spawn_infos = new PinnedList<SpawnInfo>(32);


			public void Draw()
			{
				var h_faction = this.h_faction;
				Vec2f map_frame_size_raw;

				ref var minimap = ref Minimap.MinimapHUD.minimaps[this.ent_respawn.region_id];
				if (minimap != null)
				{
					map_frame_size_raw = minimap.GetFrameSize(2);
				}
				else
				{
					map_frame_size_raw = default;
				}

				var pos = GUI.GetCanvasRect().GetPosition(pivot: new(0.50f, 0.00f), offset: new(0.00f, 64.00f + 8.00f));

				//using (var window = GUI.Window.Standalone("Respawn"u8, position: new Vector2(GUI.CanvasSize.X * 0.40f, 0) + Spawn.RespawnGUI.window_offset, pivot: Spawn.RespawnGUI.window_pivot, size: Spawn.RespawnGUI.window_size, flags: GUI.Window.Flags.No_Appear_Focus))
				//using (var window = GUI.Window.InteractionAnchored("Respawn"u8, size: new(600, 144), anchor: GUI.Anchor.Top, align: 0.00f)) //, flags: GUI.Window.Flags.No_Appear_Focus))
				using (var window = GUI.Window.Standalone("Respawn"u8, size: new(600, 144), position: pos, pivot: new(0.50f, 0.00f))) // anchor: GUI.Anchor.Top, align: 0.00f)) //, flags: GUI.Window.Flags.No_Appear_Focus))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						var pos_interaction = pos + new Vector2(0.00f, 144.00f);
						Interactable.SetCanvasPosition(pos: pos_interaction, pivot: new(0.50f, 0.00f));

						ref var region = ref this.ent_respawn.GetRegion();
						ref var player = ref Client.GetPlayerData(out var player_asset);

						var h_character_current = Client.GetCharacterHandle();

						//var random = XorRandom.New(true);
						var pos_camera = (Vec2f)Camera.position;

						var spawn_info_current = selected_spawn_info_cached;
						if (spawn_info_current.ent_spawn.TrySet(this.respawn.ent_selected_spawn))
						{
							//Interactable.Open(spawn_info_current.ent_spawn);
							//App.WriteLine("open");
							//spawn_info_current.ent_spawn = this.respawn.ent_selected_spawn;
						}

						if (spawn_info_current.ent_spawn.IsAlive())
						{

						}

						if (spawn_info_current.ent_spawn == 0)
						{
							spawn_info_current.ent_spawn = this.respawn.ent_selected_spawn;
							spawn_info_current.pos = pos_camera;
							//Interactable.Open(spawn_info_current.ent_spawn);
						}

						GUI.DrawWindowBackground(GUI.tex_window_menu, padding: new Vector4(8, 8, 8, 8));

						using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
						{


							{
								using (GUI.Wrap.Push(GUI.RmX))
								using (var group = GUI.Group.New(size: new(GUI.RmX, 80), padding: new(4)))
								{
									//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
									//if (minimap != null)
									//{
									//	var map_frame_size = minimap.GetFrameSize(2);
									//	map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.RmX, 80));

									//	Minimap.DrawMap(ref region, minimap, map_frame_size, map_scale: 1.00f);
									//}

									//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
									if (minimap != null)
									{
										//var map_frame_size = minimap.GetFrameSize(2);
										var map_frame_size = map_frame_size_raw.vect.ScaleToSize(GUI.Rm);

										using (group.Split(size: map_frame_size, GUI.AlignX.Center, GUI.AlignY.Center))
										{
											using (var map = GUI.Map.New(ref region, minimap, size: map_frame_size, map_scale: 1.00f, draw_markers: false))
											{
												//ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable

												foreach (ref var row in region.IterateQuery<Minimap.GetMarkersQuery>())
												{
													var selected = row.Entity == spawn_info_current.ent_spawn;
													var is_selectable = false;
													var is_visible = false;
													var has_characters = false;

													var transform_copy = default(Transform.Data);
													//var nameable_copy = default(Nameable.Data);
													var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);
													var marker_copy = default(Minimap.Marker.Data);
													//var alpha = 1.00f;

													var faction_id_tmp = this.h_faction;

													//var ok = false;

													row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable) =>
													{
														transform_copy = transform;
														//nameable_copy = nameable;
														marker_copy = marker;

														ref var spawn = ref entity.GetComponent<Spawn.Data>();
														if (spawn.IsNotNull())
														{
															if (is_visible = spawn.IsVisibleToFaction(faction_id_tmp, faction.id)) // (faction.id == faction_id_tmp.id || spawn.flags.HasAny(Spawn.Flags.Public)) || (faction_id_tmp.id == 0 && spawn.flags.HasAny(Spawn.Flags.Neutral_Only))) //((faction.id == 0 && spawn.flags.HasAny(Spawn.Flags.Public)) || faction.id == faction_id_tmp || (faction_id_tmp == 0 && spawn.flags.HasAny(Spawn.Flags.Neutral_Only))) && marker.flags.HasAll(Minimap.Marker.Flags.Spawner))
															{
																if (faction.id.TryGetData(out var ref_faction))
																{
																	color = color.LumaBlend(ref_faction.value.color_a); // = Color32BGRA.Lerp(color, ref_faction.value.color_a, ref_faction.value.color_a.GetLuma());
																}

																//sprite.frame.X = 1;
																//ok = true;

																ref var dormitory = ref entity.GetComponent<Dormitory.Data>();
																if (dormitory.IsNotNull())
																{
																	has_characters = dormitory.HasSpawnableCharacters(h_faction: faction_id_tmp, h_faction_spawn: faction.id, h_player: player_asset, spawn_flags: spawn.flags);

																	//var dormitory_characters = dormitory.GetCharacterSpan(); //.characters.Slice(dormitory.characters_capacity);

																	//var is_empty = dormitory_characters.GetFilledCount() == 0;
																	//if (is_empty)
																	//{
																	//	alpha = 0.65f;
																	//}
																}

																is_selectable = !h_character_current && spawn.IsSelectableByFaction(faction_id_tmp, faction.id, has_characters, is_visible); // has_characters || faction.id == faction_id_tmp || (faction.id == 0 && spawn.flags.HasAny(Spawn.Flags.Public));
															}
															//else if (spawn.flags.HasAny(Spawn.Flags.Public))
															//{
															//	ok = true;
															//	alpha = 0.65f;
															//}
														}
													});

													if (is_visible)
													{
														var alpha = has_characters & is_selectable ? 1.00f : 0.50f;
														using (var node = map.DrawNode(marker_copy.sprite, transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: (selected ? Color32BGRA.White : color).WithAlphaMult(alpha), color_hovered: selected ? Color32BGRA.White : Color32BGRA.Lerp(color, Color32BGRA.White, 0.50f).WithAlphaMult(alpha)))
														{
															//GUI.DrawTextCentered(nameable_copy.name, node.rect.GetPosition() + new Vector2(16, 0), piv layer: GUI.Layer.Window, font: GUI.Font.Superstar, size: 16);

															if (node.is_hovered && !selected)
															{
																if (is_selectable)
																{
																	GUI.SetCursor(App.CursorType.Hand, 100);

																	if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
																	{
																		var rpc = new RespawnExt.SetSpawnRPC()
																		{
																			ent_spawn = row.Entity
																		};
																		rpc.Send(this.ent_respawn);

																		//App.WriteLine("press");
																		//ent_selected_spawn_new = row.Entity;
																	}
																}

																using (GUI.Tooltip.New())
																{
																	//GUI.Title(nameable_copy.name, font: GUI.Font.Superstar, size: 16);
																	GUI.Title(row.Entity.GetFullName(), font: GUI.Font.Superstar, size: 16);
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

							GUI.SeparatorThick();


							using (var group_top = GUI.Group.New(size: new(GUI.RmX, 48)))
							{
								var rm = new Vec2f(GUI.Rm);
								var button_size = new Vec2f(32, rm.y);

								if (GUI.DrawIconButton(identifier: "spawn.prev"u8, enabled: !h_character_current, sprite: GUI.tex_icons_widget.GetSprite(8, 16, 4, 8), size: button_size, color_icon: GUI.col_button_yellow))
								{
									scoped var args = new FetchNearestSpawnArgs(ent_selected_spawn: this.respawn.ent_selected_spawn, pos_pivot: pos_camera,
									h_faction: h_faction, h_character: h_character_current,
									search_type: FetchNearestSpawnArgs.SearchType.Left,
									flags: FetchNearestSpawnArgs.Flags.None);

									var ret = region.RunSystem(Conquest.FetchNearestSpawn, ref args);
									if (args.spawn_info.ent_spawn != 0)
									{
										selected_spawn_info_cached = args.spawn_info;

										var rpc = new RespawnExt.SetSpawnRPC()
										{
											ent_spawn = args.spawn_info.ent_spawn
										};
										rpc.Send(this.ent_respawn);
									}
								}

								GUI.SameLine();

								//GUI.DropdownInput(GUI.Dropdown.Begin()

								using (var group_title = GUI.Group.New(size: rm - new Vec2f(button_size.x.x2(), 0.00f), padding: new(4, 0)))
								{
									group_title.DrawBackground(GUI.tex_window);

									Utf8String title_text = (spawn_info_current.ent_spawn.IsValid() ? spawn_info_current.ent_spawn.GetFullName() : "Select a spawnpoint"u8);
									GUI.TitleCentered(title_text, rect: group_title.GetInnerRect(), size: 32, pivot: new(0.50f, 0.50f));
								}

								GUI.SameLine();

								if (GUI.DrawIconButton(identifier: "spawn.next"u8, enabled: !h_character_current, sprite: GUI.tex_icons_widget.GetSprite(8, 16, 5, 8), size: button_size, color_icon: GUI.col_button_yellow))
								{
									scoped var args = new FetchNearestSpawnArgs(ent_selected_spawn: this.respawn.ent_selected_spawn, pos_pivot: pos_camera,
									h_faction: h_faction, h_character: h_character_current,
									search_type: FetchNearestSpawnArgs.SearchType.Right,
									flags: FetchNearestSpawnArgs.Flags.None);

									var ret = region.RunSystem(Conquest.FetchNearestSpawn, ref args);
									if (args.spawn_info.ent_spawn != 0)
									{
										selected_spawn_info_cached = args.spawn_info;

										var rpc = new RespawnExt.SetSpawnRPC()
										{
											ent_spawn = args.spawn_info.ent_spawn
										};
										rpc.Send(this.ent_respawn);
									}
								}
							}



							//if (spawn_info_current.ent_spawn.IsAlive())
							//{
							//	var context = GUI.ItemContext.Begin(is_readonly: true);
							//	var available_items = Span<Shipment.Item>.Empty;

							//	ref var faction = ref spawn_info_current.ent_spawn.GetComponent<Faction.Data>();
							//	ref var spawn = ref spawn_info_current.ent_spawn.GetComponent<Spawn.Data>();

							//	var oc_shipment = spawn_info_current.ent_spawn.GetComponentWithOwner<Shipment.Data>(Relation.Type.Instance);
							//	if (oc_shipment.IsValid() && oc_shipment.data.flags.HasAnyExcept(Shipment.Flags.Allow_Withdraw, Shipment.Flags.No_GUI | Shipment.Flags.Staging | Shipment.Flags.Locked))
							//	{
							//		available_items = oc_shipment.data.items.AsSpan();
							//	}

							//	var h_inventory = default(Inventory.Handle);

							//	var h_selected_character_tmp = h_selected_character;

							//	ref var dormitory = ref spawn_info_current.ent_spawn.GetComponent<Dormitory.Data>();
							//	if (dormitory.IsNotNull())
							//	{
							//		var characters = dormitory.GetCharacterSpan();
							//		if (h_selected_character_tmp && !characters.Contains(h_selected_character_tmp))
							//		{
							//			h_selected_character_tmp = default;
							//		}
							//	}

							//	ref var armory = ref spawn_info_current.ent_spawn.GetComponent<Armory.Data>();
							//	if (armory.IsNotNull())
							//	{
							//		armory.inv_storage.TryGetHandle(out h_inventory);
							//	}

							//	var has_storage = h_inventory.IsValid() || !available_items.IsEmpty;
							//	var is_empty = true;

							//	//if (dormitory.IsNotNull())
							//	//{
							//	//	var characters = dormitory.characters.Slice(dormitory.characters_capacity);
							//	//	is_empty = characters.GetFilledCount() == 0;
							//	//}

							//	//Crafting.Context.New(ref region, ent_selected_spawn, ent_selected_spawn, out var crafting_context, inventory: h_inventory, shipment: oc_shipment, search_radius: 0.00f, h_faction: this.faction_id);
							//	Crafting.Context.NewFromSelf(ref region.AsCommon(), spawn_info_current.ent_spawn, out var crafting_context, search_radius: 0.00f);

							//	//var context = GUI.ItemContext.Begin();

							//	//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(4, 0)))
							//	//{
							//	//	var spawn_name = ent_selected_spawn.GetFullName();
							//	//	GUI.TitleCentered(spawn_name, size: 32, pivot: new(0.00f, 0.50f));

							//	//	//if (is_empty)
							//	//	//{
							//	//	//	GUI.TitleCentered("This spawnpoint is empty.", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, -8));
							//	//	//	GUI.TitleCentered("Select another one on the map!", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, 8));
							//	//	//}
							//	//}

							//	//GUI.SeparatorThick();

							//	//using (GUI.Group.New(size: GUI.Rm with { X = 304 }, padding: new(0, 0)))
							//	//{
							//	//	//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 0)))
							//	//	//{
							//	//	//	var spawn_name = ent_selected_spawn.GetFullName();
							//	//	//	GUI.TitleCentered(spawn_name, size: 24, pivot: new(0.00f, 0.50f));
							//	//	//}

							//	//	//GUI.SeparatorThick();

							//	//	using (var scrollable = GUI.Scrollbox.New("characters"u8, size: GUI.GetRemainingSpace(y: (has_storage ? -96 - 8 - 8 : 0)), padding: new(4, 4), force_scrollbar: true))
							//	//	{
							//	//		if (dormitory.IsNotNull())
							//	//		{
							//	//			var characters = dormitory.GetCharacterSpan();

							//	//			//var characters_count_max = Math.Min(characters.Length, dormitory.characters_capacity);
							//	//			for (var i = 0; i < characters.Length; i++)
							//	//			{
							//	//				//DrawCharacter(characters[i].GetHandle());

							//	//				var h_character = characters[i];
							//	//				using (GUI.ID<Conquest.RespawnGUI, Character.Data>.Push(h_character))
							//	//				using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
							//	//				{
							//	//					ref var character_data = ref h_character.GetData();

							//	//					if (h_character && !h_selected_character_tmp)
							//	//					{
							//	//						h_selected_character_tmp = h_character;
							//	//						h_selected_character = h_character;
							//	//					}

							//	//					is_empty &= !h_character;
							//	//					//var selectable = h_character.CanSpawnAsCharacter(faction_id, faction.id, spawn.flags); //  character_data.IsNotNull() && character_data.faction == faction_id;

							//	//					Dormitory.DrawCharacterSmall(h_character);

							//	//					var selected = h_selected_character_tmp && h_character == h_selected_character_tmp; // selected_index;
							//	//					if (GUI.Selectable3("selectable"u8, group_row.GetOuterRect(), selected))
							//	//					{
							//	//						h_selected_character = h_character;
							//	//					}

							//	//					GUI.FocusableAsset(h_character);
							//	//				}
							//	//			}
							//	//		}
							//	//	}

							//	//	if (has_storage)
							//	//	{
							//	//		using (var group_storage = GUI.Group.New(size: GUI.Rm, padding: new(8, 8)))
							//	//		{
							//	//			GUI.DrawBackground(GUI.tex_frame, group_storage.GetOuterRect(), new(8, 8, 8, 8));

							//	//			if (h_inventory.IsValid())
							//	//			{
							//	//				using (GUI.Group.New(size: h_inventory.GetFrameSize(2, 0)))
							//	//				{
							//	//					GUI.DrawInventory(h_inventory, is_readonly: true);
							//	//				}
							//	//			}

							//	//			GUI.SameLine();

							//	//			if (oc_shipment.data.IsNotNull())
							//	//			{
							//	//				using (GUI.Group.New(size: GUI.Rm))
							//	//				{
							//	//					GUI.DrawShipment(ref context, spawn_info_current.ent_spawn, ref oc_shipment.data, slot_size: new(96, 48));
							//	//				}
							//	//			}
							//	//		}
							//	//	}
							//	//}

							//	//GUI.SameLine();

							//	//using (var group_character = GUI.Group.New(size: GUI.Rm))
							//	//{
							//	//	var selected_items = Spawn.RespawnGUI.character_id_to_selected_items.GetOrAdd(h_selected_character_tmp);

							//	//	ref var character_data = ref h_selected_character_tmp.GetData();
							//	//	using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(2, 4)))
							//	//	{
							//	//		GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

							//	//		//if (character_data.IsNotNull())
							//	//		//{
							//	//		//	if (selected_items != null)
							//	//		//	{
							//	//		//		foreach (var h_kit in character_data.kits)
							//	//		//		{
							//	//		//			selected_items.Add(h_kit);
							//	//		//		}
							//	//		//	}
							//	//		//}

							//	//		//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(8, 8)))
							//	//		//{
							//	//		//	if (character_data.IsNotNull())
							//	//		//	{
							//	//		//		GUI.TitleCentered(character_data.name, size: 24, pivot: new(0.00f, 0.00f));
							//	//		//	}
							//	//		//	else
							//	//		//	{
							//	//		//		GUI.TitleCentered("<no character selected>"u8, size: 24, pivot: new(0.00f, 0.00f));
							//	//		//	}
							//	//		//}

							//	//		if (dormitory.flags.HasNone(Dormitory.Flags.Hide_XP))
							//	//		{
							//	//			using (var group_xp = GUI.Group.New(size: GUI.GetRemainingSpace(y: -128)))
							//	//			{
							//	//				using (var scrollbox = GUI.Scrollbox.New("scrollbox_xp"u8, size: GUI.Rm))
							//	//				{
							//	//					GUI.DrawBackground(GUI.tex_panel, scrollbox.group_frame.GetOuterRect(), new(8, 8, 8, 8));

							//	//					if (character_data.IsNotNull())
							//	//					{
							//	//						Experience.DrawTableSmall2(ref character_data.experience);
							//	//					}

							//	//					//if (origin_data.IsNotNull())
							//	//					//{
							//	//					//	Experience.DrawTableSmall(ref origin_data.experience);
							//	//					//}
							//	//				}
							//	//			}

							//	//			GUI.SeparatorThick();
							//	//		}

							//	//		if (dormitory.flags.HasNone(Dormitory.Flags.Hide_Kits))
							//	//		{
							//	//			//GUI.SeparatorThick();

							//	//			//using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(2, 4)))
							//	//			//{
							//	//			//	GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

							//	//			using (var scrollable = GUI.Scrollbox.New("kits"u8, size: GUI.Rm, padding: new(4, 4), force_scrollbar: true))
							//	//			{
							//	//				Dormitory.DrawKits(ref dormitory, ref crafting_context, ref character_data, h_inventory, has_storage, available_items, selected_items);
							//	//			}
							//	//			//}

							//	//			//GUI.SeparatorThick();

							//	//		}
							//	//	}

							//	//	//if (spawn.IsNotNull() && (faction.IsNull() || faction.id == 0 || faction.id == player.faction_id || (player.faction_id == 0 && spawn.flags.HasAny(Spawn.Flags.Neutral_Only))))
							//	//	//{
							//	//	if (is_empty)
							//	//	{
							//	//		if (GUI.DrawButton("No characters available."u8, size: new Vector2(GUI.RmX, 48), font_size: 24, color: GUI.col_button_error, error: true))
							//	//		{

							//	//		}
							//	//		GUI.DrawHoverTooltip("This spawnpoint doesn't have any more characters left.\n\nSelect another spawnpoint on the map."u8);
							//	//	}
							//	//	else
							//	//	{
							//	//		var is_selected_character_spawnable = h_selected_character_tmp.CanSpawnAsCharacter(h_faction: h_faction, h_faction_spawn: faction.id, h_player: player_asset, spawn_flags: spawn.flags);
							//	//		if (is_selected_character_spawnable)
							//	//		{
							//	//			if (GUI.DrawButton("Spawn"u8, size: new Vector2(GUI.RmX, 48), font_size: 24, color: GUI.col_button_ok))
							//	//			{
							//	//				var rpc = new Spawn.SpawnRPC()
							//	//				{
							//	//					h_character = h_selected_character_tmp,
							//	//					h_component = IComponent.Handle.FromComponent<Dormitory.Data>(),
							//	//					control = true
							//	//				};

							//	//				foreach (var h_kit in selected_items)
							//	//				{
							//	//					rpc.kits.TryAdd(in h_kit);
							//	//				}

							//	//				//rpc.Send(ent_selected_spawn);
							//	//				rpc.Send(spawn_info_current.ent_spawn);
							//	//				//AsTask(ent_selected_spawn).ContinueWith((result) =>
							//	//				//{
							//	//				//	//App.WriteLine(result.out_ent_spawned);
							//	//				//});

							//	//				h_selected_character = default;
							//	//			}
							//	//		}
							//	//		else
							//	//		{
							//	//			if (GUI.DrawButton("Cannot spawn as this character."u8, size: new Vector2(GUI.RmX, 48), font_size: 16, color: GUI.col_button_error, error: true))
							//	//			{

							//	//			}
							//	//			GUI.DrawHoverTooltip("Character belongs to another faction."u8);
							//	//		}
							//	//	}

							//	//}
							//}
							//else
							//{
							//	//using (var group_top = GUI.Group.New(size: new(GUI.RmX, 48)))
							//	//{
							//	//	var rm = new Vec2f(GUI.Rm);
							//	//	var button_size = new Vec2f(32, rm.y);

							//	//	if (GUI.DrawIconButton("spawn.prev"u8, GUI.tex_icons_widget.GetSprite(8, 16, 4, 8), size: button_size))
							//	//	{
							//	//		//var arg = (this.h_faction, player.name);

							//	//		var arg = (pos_pivot: (Vec2f)pos_camera, nearest_dist_sq: float.MaxValue, h_faction: h_faction, spawn_info: default(Conquest.SpawnInfo));
							//	//		var ret = region.TriggerSystem(Conquest.FetchNearestSpawn, ref arg);
							//	//		App.WriteValue(ret);
							//	//		App.WriteValue(arg.nearest_dist_sq);
							//	//	}

							//	//	GUI.SameLine();

							//	//	//GUI.DropdownInput(GUI.Dropdown.Begin()

							//	//	using (var group_title = GUI.Group.New(size: rm - new Vec2f(button_size.x.x2(), 0.00f), padding: new(4, 0)))
							//	//	{
							//	//		group_title.DrawBackground(GUI.tex_window);
							//	//		GUI.TitleCentered("Select a spawnpoint"u8, rect: group_title.GetInnerRect(), size: 32, pivot: new(0.50f, 0.50f));

							//	//		//if (is_empty)
							//	//		//{
							//	//		//	GUI.TitleCentered("This spawnpoint is empty.", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, -8));
							//	//		//	GUI.TitleCentered("Select another one on the map!", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, 8));
							//	//		//}
							//	//	}

							//	//	GUI.SameLine();

							//	//	if (GUI.DrawIconButton("spawn.next"u8, GUI.tex_icons_widget.GetSprite(8, 16, 5, 8), size: button_size))
							//	//	{

							//	//	}
							//	//}

							//	//using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
							//	//{
							//	//	GUI.SeparatorThick();

							//	//	ref var faction_data = ref this.h_faction.GetData();
							//	//	if (faction_data.IsNotNull())
							//	//	{
							//	//		using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
							//	//		{
							//	//			//GUI.Title("Your faction has no established presence in this region."u8, size: 20);

							//	//			//if (GUI.DrawButton($"Deploy a Scout ({faction_data.scout_count} available)", size: new(300, 40), font_size: 20, color: GUI.col_button_yellow, enabled: faction_data.scout_count > 0))
							//	//			//{
							//	//			//	var rpc = new Conquest.DeployInfiltratorRPC()
							//	//			//	{
							//	//			//		h_character = 0
							//	//			//	};
							//	//			//	rpc.Send();
							//	//			//}
							//	//		}
							//	//	}
							//	//}
							//}

							//if (ent_selected_spawn_new.HasValue && ent_selected_spawn_new.Value != ent_selected_spawn)
							//{
							//	var rpc = new RespawnExt.SetSpawnRPC()
							//	{
							//		ent_spawn = ent_selected_spawn_new.Value
							//	};
							//	rpc.Send(Client.GetEntity());

							//	//ent_selected_spawn = ent_selected_spawn_new.Value;
							//	ent_selected_spawn_new = default;
							//}
						}
					}
				}
			}
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnAddWorld(Entity entity, [Source.Owned] ref World.Global world)
		{
			App.WriteValue(nameof(OnAddWorld), entity, color: App.Color.Green);
			//ref var scenario = ref world

			// TODO: hack
			ref var scenario_data = ref WorldMap.h_scenario.GetData();
			if (scenario_data.IsNotNull())
			{
				WorldMap.FocusPosition(scenario_data.worldmap_position);
				App.WriteValue(nameof(scenario_data.worldmap_position), scenario_data.worldmap_position, App.Color.Magenta);
			}
		}

		[Shitcode]
		[HasTag("local", true, Source.Modifier.Owned)]
		[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked, order: -55)]
		public static void UpdateRespawn([Source.Owned] in Respawn.Data respawn, [Source.Owned] in Player.Data player)
		{
			if (!WorldMap.IsOpen && !player.h_character && (player.flags.HasNone(Player.Flags.Editor) | Editor.show_respawn_menu) && respawn.ent_selected_spawn.IsAlive())
			{
				ref var interactable = ref respawn.ent_selected_spawn.GetComponent<Interactable.Data>();
				if (interactable.IsNotNull())
				{
					interactable.SetActive(true);
					Interactor.local_target = respawn.ent_selected_spawn;
				}
			}
		}

		[ISystem.Event<Interactable.DrawWindowEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnDrawInteractionEvent(ISystem.Info info, ref Region.Data region, Entity ent_character, ref Interactable.DrawWindowEvent ev,
		[Source.Owned] ref Character.Data character)
		{
			using (var window_child = ev.window.BeginChildWindow(identifier: "interaction.bottom"u8,
			anchor_x: GUI.AlignX.Left,
			anchor_y: GUI.AlignY.Bottom,
			size: new((48 * 4) + 12, (24 * 4) + 12),
			pivot: new(0.00f, 1.00f),
			offset: new(0.00f, -2),
			padding: new(6),
			tex_bg: GUI.tex_window_sidebar_c,
			color_bg: Color32BGRA.White,
			open: true))
			{
				if (window_child.show)
				{
					var h_character = ent_character.GetAssetHandle<ICharacter.Handle>();

					//using (GUI.Group.New(size: new(GUI.RmX, 48)))
					//{
					//	Dormitory.DrawCharacterHead(h_character, new(48));
					//}

					//GUI.SameLine();

					using (GUI.Group.New(size: GUI.Rm))
					{
						var h_inventory = character.GetInventory();
						if (h_inventory)
						{
							GUI.DrawInventory(h_inventory);
						}
					}
					//GUI.DrawWindowBackground();
				}
			}
			//App.WriteLine("yeah");
			//GUI.Text("yeah");
		}

		[Shitcode]
		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIRespawn(Entity entity, [Source.Owned] in Player.Data player, [Source.Owned] in Respawn.Data respawn)
		{
			//Spawn.RespawnGUI.enabled = false;

			//if (player.IsLocal())
			{
				//if (!WorldMap.IsOpen && player.flags.HasNone(Player.Flags.Alive) && !(player.flags.HasAny(Player.Flags.Editor) && !Editor.show_respawn_menu))
				if ((player.flags.HasNone(Player.Flags.Editor) | Editor.show_respawn_menu) && !WorldMap.IsOpen && !player.ent_controlled.IsAlive())
				{
					var gui = new RespawnGUI
					{
						ent_respawn = entity
					};

					//App.WriteLine(faction.id);
					gui.respawn = respawn;
					gui.h_faction = player.faction_id;

					gui.Submit();
				}
			}
		}
#endif
	}
}

