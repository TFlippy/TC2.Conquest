using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		public struct RespawnGUI: IGUICommand
		{
			public static readonly Texture.Handle tex_icons_minimap = "ui_icons_minimap";
			public static Entity? ent_selected_spawn_new;

			public Entity ent_respawn;
			public IFaction.Handle faction_id;
			public Respawn.Data respawn;

			//public static bool[] selected_items = new bool[32];
			//public static int? selected_character_index;

			//public static Entity ent_selected_spawn;
			//public static ulong selected_character_id;
			public static float current_cost;

			public const int max_item_count = 8;

			internal const float lh = 48;

			public void Draw()
			{
				//var rem_height = MathF.Max(GUI.CanvasSize.Y - total_height, 0.00f);

				//var rem_height = GUI.CanvasSize.Y - RespawnGUI.window_offset.Y

				//RespawnGUI.window_size.Y = Maths.Clamp(RespawnGUI.window_size.Y, 0, GUI.CanvasSize.Y - RespawnGUI.window_offset.Y - 40);

				var max_height = GUI.CanvasSize.Y - Spawn.RespawnGUI.window_offset.Y - 12;

				Spawn.RespawnGUI.window_size.Y = Maths.Clamp(Spawn.RespawnGUI.window_size.Y, 0, max_height);

				Spawn.RespawnGUI.ent_selected_spawn = this.respawn.ent_selected_spawn;

				using (var window = GUI.Window.Standalone("Respawn", position: new Vector2(GUI.CanvasSize.X * 0.50f, 0) + Spawn.RespawnGUI.window_offset, pivot: Spawn.RespawnGUI.window_pivot, size: Spawn.RespawnGUI.window_size))
				{
					Spawn.RespawnGUI.window_size = new Vector2(500, 700);

					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						ref var region = ref Client.GetRegion();
						ref var player = ref Client.GetPlayer();

						GUI.DrawWindowBackground(GUI.tex_window_menu, padding: new Vector4(8, 8, 8, 8));

						using (GUI.Group.New(size: new Vector2(GUI.GetRemainingWidth(), GUI.GetRemainingHeight()), padding: new(8, 8)))
						{
							if (!Spawn.RespawnGUI.ent_selected_spawn.IsAlive())
							{
								region.Query<Region.GetSpawnsQuery>(Func).Execute(ref this);
								static void Func(ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction)
								{
									ref var player = ref Client.GetPlayer();
									if (faction.id == 0 || (faction.id == player.faction_id))
									{
										var random = XorRandom.New();

										if (Spawn.RespawnGUI.ent_selected_spawn.id == 0 || random.NextBool(0.30f)) ent_selected_spawn_new = entity;
									}
								}
							}

							{
								ref var info = ref region.GetMapInfo();

								using (GUI.Wrap.Push(GUI.GetRemainingWidth()))
								{
									using (GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
									{
										using (GUI.Wrap.Push(GUI.GetRemainingWidth()))
										{
											//if (!info.name.IsEmpty()) GUI.Title(info.name, size: 32);
											//if (!info.desc.IsEmpty()) GUI.Text(info.desc);
										}
									}

									//var ts = Timestamp.Now();
									using (var group = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
									{
										//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
										//if (minimap != null)
										//{
										//	var map_frame_size = minimap.GetFrameSize(2);
										//	map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.GetRemainingWidth(), 80));

										//	Minimap.DrawMap(ref region, minimap, map_frame_size, map_scale: 1.00f);
										//}

										ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
										if (minimap != null)
										{
											var map_frame_size = minimap.GetFrameSize(2);
											map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.GetRemainingWidth(), 80));

											using (var map = GUI.Map.New(ref region, minimap, size: map_frame_size, map_scale: 1.00f, draw_markers: false))
											{
												foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
												{
													var selected = row.Entity == Spawn.RespawnGUI.ent_selected_spawn;

													var transform_copy = default(Transform.Data);
													var nameable_copy = default(Nameable.Data);
													var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);

													row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
													{
														transform_copy = transform;
														nameable_copy = nameable;

														if (faction.id.TryGetData(out var ref_faction))
														{
															color = ref_faction.value.color_a;
															//sprite.frame.X = 1;
														}
													});

													using (var node = map.DrawNode(new Sprite(tex_icons_minimap, 16, 16, 3, 0), transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: color, color_hovered: Color32BGRA.White))
													{
														//GUI.DrawTextCentered(nameable_copy.name, node.rect.GetPosition() + new Vector2(16, 0), piv layer: GUI.Layer.Window, font: GUI.Font.Superstar, size: 16);

														if (node.is_hovered)
														{
															GUI.SetCursor(App.CursorType.Hand, 100);

															if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
															{
																//App.WriteLine("press");
																ent_selected_spawn_new = row.Entity;
															}

															using (GUI.Tooltip.New())
															{
																GUI.Title(nameable_copy.name, font: GUI.Font.Superstar, size: 16);
															}
														}
													}
												}
											}
										}
									}
									//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");

									//GUI.NewLine(4);
									//GUI.Separator();
									//GUI.NewLine(4);

									//using (GUI.Group.New(size: new(GUI.GetRemainingWidth() * 0.50f, 0), padding: new(4)))
									//{
									//	GUI.LabelShaded("Urbanization:", info.urbanization, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");

									//	GUI.LabelShaded("Industrialization:", info.industrialization, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");

									//	GUI.LabelShaded("Education:", info.education, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");

									//	GUI.LabelShaded("Wealth:", info.wealth, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");

									//	GUI.LabelShaded("Wilderness:", info.wilderness, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");
									//}

									//GUI.SameLine();

									//using (GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
									//{
									//	GUI.LabelShaded("Devastation:", info.devastation, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("Destruction of the environment through\ndisasters, warfare and industry.");

									//	GUI.LabelShaded("Savagery:", info.savagery, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("Hostility of wildlife, flora\nand other inhabitants.");

									//	GUI.LabelShaded("Anarchy:", info.anarchy, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("Political stability of the region.");

									//	GUI.LabelShaded("Elevation:", info.elevation, format: "{0:0.00} m", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");

									//	GUI.LabelShaded("Population:", info.population, format: "~{0}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
									//	GUI.DrawHoverTooltip("TODO: desc");
									//}
								}
							}

							GUI.SeparatorThick();

							this.DrawSpawns(ref region, size: new(GUI.GetRemainingWidth(), (24 * 4.50f) + 8));

							GUI.SeparatorThick();

							//var h_character = default(ICharacter.Handle);

							var spawn_info = default(Respawn.SpawnInfo);

							var span_keys = this.respawn.spawns_keys.AsSpan();
							var span_values = this.respawn.spawns_values.AsSpan();

							var index = span_keys.IndexOf(Spawn.RespawnGUI.ent_selected_spawn);
							if (index >= 0)
							{
								spawn_info = span_values[index];

								//ref var info = ref span_values[index];
								//h_character = info.character;
							}

							using (var group_row = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 80)))
							{
								RespawnExt.DrawCharacter(ref spawn_info);
							}

							using (var scrollbox = GUI.Scrollbox.New("levels", size: new(GUI.GetRemainingWidth(), GUI.GetRemainingHeight() - 48), padding: new(0)))
							{
								//GUI.Title("Experience", size: 20);

								var h_character = spawn_info.character;
								ref var character = ref h_character.GetData();
								if (character.IsNotNull())
								{
									Experience.DrawTableSmall2(ref character.experience);
								}
							}

							GUI.SeparatorThick();


							//ref var character_data = ref h_character.GetData();
							//if (character_data.IsNotNull())
							//{
							//	ref var origin_data = ref character_data.origin.GetData();
							//	if (origin_data.IsNotNull())
							//	{
							//		Experience.DrawTableSmall(ref origin_data.experience);
							//	}
							//}

							if (Spawn.RespawnGUI.ent_selected_spawn.IsAlive())
							{
								ref var faction = ref Spawn.RespawnGUI.ent_selected_spawn.GetComponent<Faction.Data>();
								if (faction.IsNull() || faction.id == player.faction_id)
								{
									ref var spawn = ref Spawn.RespawnGUI.ent_selected_spawn.GetComponent<Spawn.Data>();
									if (!spawn.IsNull())
									{
										if (GUI.DrawButton((this.respawn.cooldown > 0.00f ? $"Respawn ({MathF.Floor(this.respawn.cooldown):0}s)" : "Respawn"), new Vector2(168, 48), enabled: this.respawn.cooldown <= float.Epsilon && current_cost <= this.respawn.tokens && spawn_info.character.id != 0, font_size: 24, color: GUI.font_color_green_b))
										{
											var rpc = new RespawnExt.SpawnRPC
											{
												ent_spawn = Spawn.RespawnGUI.ent_selected_spawn
											};
											rpc.Send(player.ent_player);
										}
										if (GUI.IsItemHovered())
										{
											using (GUI.Tooltip.New())
											{
												GUI.Text("Respawn as this character at the selected spawn point.");
											}
										}
									}
									else
									{

									}
								}
								else
								{
									Spawn.RespawnGUI.ent_selected_spawn = default;
								}
							}

							if (ent_selected_spawn_new.HasValue && ent_selected_spawn_new != Spawn.RespawnGUI.ent_selected_spawn)
							{
								var rpc = new RespawnExt.SetSpawnRPC()
								{
									ent_spawn = ent_selected_spawn_new.Value
								};
								rpc.Send(player.ent_player);
								ent_selected_spawn_new = default;
							}
						}
					}
				}
			}

			private void DrawSpawns(ref Region.Data region, Vector2 size)
			{
				//GUI.Title("Spawns", size: 32);
				//GUI.SeparatorThick();

				using (var scrollable = GUI.Scrollbox.New("Spawns", size: size, padding: new(4, 4), force_scrollbar: true))
				{
					using (var table = GUI.Table.New("Spawns.Table", 2, new(GUI.GetRemainingWidth(), 0)))
					{
						if (table.show)
						{
							table.SetupColumnFixed(24);
							table.SetupColumnFlex(1);
							//table.SetupColumnFixed(48);


							static bool DrawSpawnsRow(Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Faction.Data faction)
							{
								var pressed = false;

								using (var row = GUI.Table.Row.New(new Vector2(GUI.GetRemainingWidth(), 24)))
								{
									using (GUI.ID.Push(entity))
									{
										var spawn_name = nameable.name;
										if (spawn_name.IsEmpty()) spawn_name = "Unknown";

										var color = GUI.font_color_default;
										if (faction.id.TryGetData(out var ref_faction))
										{
											color = ref_faction.value.color_a;
										}

										//var text = ZString.Format("{0} {1}", spawn_name, (faction.ent_faction != 0 ? $"({faction.name})" : ""));

										using (row.Column(0, padding: new(4, 0)))
										{
											GUI.TitleCentered($"{spawn.respawn_counter}", size: 16, color: Color32BGRA.Lerp(GUI.font_color_default_dark, GUI.font_color_default, Maths.Clamp01(spawn.respawn_counter * 0.01f)), pivot: new(0.50f, 0.50f));
										}
										GUI.DrawHoverTooltip("Popularity");

										using (row.Column(1, padding: new(4, 0)))
										{
											//if (faction.id != 0)
											//{
											//	GUI.TitleCentered($"[{faction.tag}]", color: faction.color_a, pivot: new(0.00f, 0.50f));
											//	GUI.ResetLine(32);
											//}
											GUI.TitleCentered(spawn_name, size: 24, color: faction.id != 0 ? color : GUI.font_color_default, pivot: new(0.00f, 0.50f));

											if (entity.TryGetPrefab(out var prefab))
											{
												var prefab_name = (Utf8String)prefab.GetName();

												//var text_size = default(Vector2);
												//ImGuiNative.igCalcTextSize2(prefab_name, null, 1, 0, 16, GUI.Font.Superstar.ptr, &text_size);

												//GUI.OffsetLine(GUI.GetRemainingWidth() - text_size.X);
												GUI.TitleCentered(prefab_name, color: faction.id != 0 ? color.WithColorMult(0.50f) : GUI.font_color_default.WithColorMult(0.50f), pivot: new(1.00f, 0.50f));
												////GUI.TextShadedCentered(prefab_name, pivot: new(1.00f, 0.50f), size: 16);
											}
										}

										//using (row.Column(1))
										//{

										//}

										GUI.SameLine();
										if (GUI.Selectable("", Spawn.RespawnGUI.ent_selected_spawn.id == entity.id, size: GUI.GetRemainingSpace(), same_line: false))
										{
											RespawnGUI.ent_selected_spawn_new = entity;
											pressed = true;
										}
									}
								}

								return pressed;
							}

							//App.WriteLine(faction.id);
							if (this.faction_id != 0)
							{
								region.Query<Region.GetSpawnsQuery>(FuncA).Execute(ref this);
								static void FuncA(ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction)
								{
									ref var data = ref info.GetParameter<RespawnGUI>();
									if (!data.IsNull())
									{
										if (faction.id != 0 && faction.id == data.faction_id)
										{
											//GUI.DrawBackground(GUI.tex_panel_white, GUI.GetRemainingRect(), new(4), faction.color_a);

											DrawSpawnsRow(entity, in spawn, in nameable, in faction);
										}
									}
								}

								//GUI.NewLine(4);
								//GUI.Separator(faction.color_a.WithAlphaMult(0.50f));
								//GUI.NewLine(4);
							}

							region.Query<Region.GetSpawnsQuery>(FuncB).Execute(ref this);
							static void FuncB(ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction)
							{
								ref var data = ref info.GetParameter<RespawnGUI>();
								if (!data.IsNull())
								{
									if (faction.id == 0)
									{
										var pressed = DrawSpawnsRow(entity, in spawn, in nameable, in faction);
										if (pressed)
										{
											//var rpc = new RespawnExt.SetSpawnRPC()
											//{
											//	ent_spawn = entity
											//};
											//rpc.Send(data.ent_respawn);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIRespawn(Entity entity, [Source.Owned] in Player.Data player, [Source.Owned] in Respawn.Data respawn)
		{
			Spawn.RespawnGUI.enabled = false;

			if (player.IsLocal())
			{
				if (!player.flags.HasAll(Player.Flags.Alive) && !(player.flags.HasAll(Player.Flags.Editor) && !Editor.show_respawn_menu))
				{
					var gui = new RespawnGUI
					{
						ent_respawn = entity
					};

					//App.WriteLine(faction.id);
					gui.respawn = respawn;
					gui.faction_id = player.faction_id;

					gui.Submit();
				}
			}
		}
#endif
	}
}

