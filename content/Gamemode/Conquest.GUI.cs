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

			public static ICharacter.Handle h_selected_character;

			public void Draw()
			{
				//var rem_height = MathF.Max(GUI.CanvasSize.Y - total_height, 0.00f);

				//var rem_height = GUI.CanvasSize.Y - RespawnGUI.window_offset.Y

				//RespawnGUI.window_size.Y = Maths.Clamp(RespawnGUI.window_size.Y, 0, GUI.CanvasSize.Y - RespawnGUI.window_offset.Y - 40);

				var max_height = GUI.CanvasSize.Y - Spawn.RespawnGUI.window_offset.Y - 12;

				Spawn.RespawnGUI.window_size.Y = Maths.Clamp(Spawn.RespawnGUI.window_size.Y, 0, max_height);

				//Spawn.RespawnGUI.ent_selected_spawn = this.respawn.ent_selected_spawn;
				ref var ent_selected_spawn = ref Spawn.RespawnGUI.ent_selected_spawn;
				ent_selected_spawn = this.respawn.ent_selected_spawn;

				using (var window = GUI.Window.Standalone("Respawn", position: new Vector2(GUI.CanvasSize.X * 0.50f, 0) + Spawn.RespawnGUI.window_offset, pivot: Spawn.RespawnGUI.window_pivot, size: Spawn.RespawnGUI.window_size))
				{
					Spawn.RespawnGUI.window_size = new Vector2(632, 700);

					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						ref var region = ref Client.GetRegion();
						ref var player = ref Client.GetPlayer();
						var random = XorRandom.New(true);

						GUI.DrawWindowBackground(GUI.tex_window_menu, padding: new Vector4(8, 8, 8, 8));

						using (GUI.Group.New(size: new Vector2(GUI.GetRemainingWidth(), GUI.GetRemainingHeight()), padding: new(8, 8)))
						{
							if (!Spawn.RespawnGUI.ent_selected_spawn.IsAlive())
							{
								var player_faction_id = player.faction_id;

								foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
								{
									row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
									{
										if (faction.id == 0 || (faction.id == player_faction_id))
										{
											if (Spawn.RespawnGUI.ent_selected_spawn.id == 0 || random.NextBool(0.30f)) ent_selected_spawn_new = entity;
										}
									});
								}
							}

							//{
							//	ref var info = ref region.GetMapInfo();

							//	using (GUI.Wrap.Push(GUI.GetRemainingWidth()))
							//	{
							//		using (GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
							//		{
							//			using (GUI.Wrap.Push(GUI.GetRemainingWidth()))
							//			{
							//				//if (!info.name.IsEmpty()) GUI.Title(info.name, size: 32);
							//				//if (!info.desc.IsEmpty()) GUI.Text(info.desc);
							//			}
							//		}

							//		//var ts = Timestamp.Now();
							//		using (var group = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
							//		{
							//			//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
							//			//if (minimap != null)
							//			//{
							//			//	var map_frame_size = minimap.GetFrameSize(2);
							//			//	map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.GetRemainingWidth(), 80));

							//			//	Minimap.DrawMap(ref region, minimap, map_frame_size, map_scale: 1.00f);
							//			//}

							//			ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
							//			if (minimap != null)
							//			{
							//				var map_frame_size = minimap.GetFrameSize(2);
							//				map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.GetRemainingWidth(), 80));

							//				using (var map = GUI.Map.New(ref region, minimap, size: map_frame_size, map_scale: 1.00f, draw_markers: false))
							//				{
							//					foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
							//					{
							//						var selected = row.Entity == Spawn.RespawnGUI.ent_selected_spawn;

							//						var transform_copy = default(Transform.Data);
							//						var nameable_copy = default(Nameable.Data);
							//						var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);

							//						row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
							//						{
							//							transform_copy = transform;
							//							nameable_copy = nameable;

							//							if (faction.id.TryGetData(out var ref_faction))
							//							{
							//								color = ref_faction.value.color_a;
							//								//sprite.frame.X = 1;
							//							}
							//						});

							//						using (var node = map.DrawNode(new Sprite(tex_icons_minimap, 16, 16, 3, 0), transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: color, color_hovered: Color32BGRA.White))
							//						{
							//							//GUI.DrawTextCentered(nameable_copy.name, node.rect.GetPosition() + new Vector2(16, 0), piv layer: GUI.Layer.Window, font: GUI.Font.Superstar, size: 16);

							//							if (node.is_hovered)
							//							{
							//								GUI.SetCursor(App.CursorType.Hand, 100);

							//								if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
							//								{
							//									//App.WriteLine("press");
							//									ent_selected_spawn_new = row.Entity;
							//								}

							//								using (GUI.Tooltip.New())
							//								{
							//									GUI.Title(nameable_copy.name, font: GUI.Font.Superstar, size: 16);
							//								}
							//							}
							//						}
							//					}
							//				}
							//			}
							//		}
							//		//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");

							//		//GUI.NewLine(4);
							//		//GUI.Separator();
							//		//GUI.NewLine(4);

							//		//using (GUI.Group.New(size: new(GUI.GetRemainingWidth() * 0.50f, 0), padding: new(4)))
							//		//{
							//		//	GUI.LabelShaded("Urbanization:", info.urbanization, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");

							//		//	GUI.LabelShaded("Industrialization:", info.industrialization, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");

							//		//	GUI.LabelShaded("Education:", info.education, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");

							//		//	GUI.LabelShaded("Wealth:", info.wealth, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");

							//		//	GUI.LabelShaded("Wilderness:", info.wilderness, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");
							//		//}

							//		//GUI.SameLine();

							//		//using (GUI.Group.New(size: new(GUI.GetRemainingWidth(), 0), padding: new(4)))
							//		//{
							//		//	GUI.LabelShaded("Devastation:", info.devastation, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("Destruction of the environment through\ndisasters, warfare and industry.");

							//		//	GUI.LabelShaded("Savagery:", info.savagery, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("Hostility of wildlife, flora\nand other inhabitants.");

							//		//	GUI.LabelShaded("Anarchy:", info.anarchy, format: "{0:P2}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("Political stability of the region.");

							//		//	GUI.LabelShaded("Elevation:", info.elevation, format: "{0:0.00} m", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");

							//		//	GUI.LabelShaded("Population:", info.population, format: "~{0}", color_a: GUI.font_color_default, color_b: GUI.font_color_desc);
							//		//	GUI.DrawHoverTooltip("TODO: desc");
							//		//}
							//	}
							//}

							{
								ref var info = ref region.GetMapInfo();

								using (GUI.Wrap.Push(GUI.GetRemainingWidth()))
								{
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
												//ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable

												foreach (ref var row in region.IterateQuery<Minimap.GetMarkersQuery>())
												{
													var selected = row.Entity == ent_selected_spawn;

													var transform_copy = default(Transform.Data);
													//var nameable_copy = default(Nameable.Data);
													var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);
													var faction_id_tmp = this.faction_id;

													var ok = false;

													row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable) =>
													{
														transform_copy = transform;
														//nameable_copy = nameable;

														if ((faction.id == 0 || faction.id == faction_id_tmp) && marker.flags.HasAll(Minimap.Marker.Flags.Spawner))
														{
															if (faction.id.TryGetData(out var ref_faction))
															{
																color = ref_faction.value.color_a;
															}
															//sprite.frame.X = 1;
															ok = true;
														}
													});

													if (ok)
													{
														using (var node = map.DrawNode(new Sprite(tex_icons_minimap, 16, 16, 3, 0), transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: selected ? Color32BGRA.White : color, color_hovered: selected ? Color32BGRA.White : Color32BGRA.Lerp(color, Color32BGRA.White, 0.50f)))
														{
															//GUI.DrawTextCentered(nameable_copy.name, node.rect.GetPosition() + new Vector2(16, 0), piv layer: GUI.Layer.Window, font: GUI.Font.Superstar, size: 16);

															if (node.is_hovered && !selected)
															{
																GUI.SetCursor(App.CursorType.Hand, 100);

																if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
																{
																	//App.WriteLine("press");
																	ent_selected_spawn_new = row.Entity;
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

							if (ent_selected_spawn.IsAlive())
							{
								var context = GUI.ItemContext.Begin(is_readonly: true);

								ref var dormitory = ref ent_selected_spawn.GetComponent<Dormitory.Data>();
								ref var armory = ref ent_selected_spawn.GetComponent<Armory.Data>();
								ref var shipment = ref ent_selected_spawn.GetComponent<Shipment.Data>();
								var h_inventory = default(Inventory.Handle);

								var h_selected_character_tmp = h_selected_character;

								if (dormitory.IsNotNull())
								{
									if (h_selected_character_tmp.id != 0 && !dormitory.characters.Contains(h_selected_character_tmp))
									{
										h_selected_character_tmp = default;
									}
								}

								if (armory.IsNotNull())
								{
									armory.inv_storage.TryGetHandle(out h_inventory);
								}

								Crafting.Context.New(ref region, ent_selected_spawn, ent_selected_spawn, out var crafting_context, inventory: h_inventory, shipment: new(ref shipment, ent_selected_spawn), search_radius: 0.00f);

								var selected_items = Spawn.RespawnGUI.character_id_to_selected_items.GetOrAdd(h_selected_character_tmp);

								//var context = GUI.ItemContext.Begin();

								using (GUI.Group.New(size: GUI.GetRemainingSpace() with { X = 304 }, padding: new(0, 0)))
								{
									using (var scrollable = GUI.Scrollbox.New("characters", size: GUI.GetRemainingSpace(y: -96 - 8 - 8), padding: new(4, 4), force_scrollbar: true))
									{
										if (dormitory.IsNotNull())
										{
											var characters = dormitory.characters.AsSpan();

											var characters_count_max = Math.Min(characters.Length, dormitory.characters_capacity);
											for (var i = 0; i < characters_count_max; i++)
											{
												//DrawCharacter(characters[i].GetHandle());

												using (GUI.ID.Push(i - 100))
												{
													using (var group_row = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 40)))
													{
														var h_character = characters[i];

														if (h_character.id != 0 && h_selected_character_tmp.id == 0)
														{
															h_selected_character_tmp = h_character;
															h_selected_character = h_character;
														}

														Dormitory.DormitoryGUI.DrawCharacterSmall(h_character);

														var selected = h_selected_character_tmp.id != 0 && h_character == h_selected_character_tmp; // selected_index;
														if (GUI.Selectable3("selectable", group_row.GetOuterRect(), selected))
														{
															h_selected_character = h_character;
														}
													}
												}
											}
										}
									}

									using (var group_storage = GUI.Group.New(size: GUI.GetRemainingSpace(), padding: new(8, 8)))
									{
										GUI.DrawBackground(GUI.tex_frame, group_storage.GetOuterRect(), new(8, 8, 8, 8));

										if (h_inventory.IsValid())
										{
											using (GUI.Group.New(size: h_inventory.GetPreferedFrameSize()))
											{
												GUI.DrawInventory(h_inventory, is_readonly: true);
											}
										}

										GUI.SameLine();

										if (shipment.IsNotNull())
										{
											using (GUI.Group.New(size: GUI.GetRemainingSpace()))
											{
												GUI.DrawShipment(ref context, ent_selected_spawn, ref shipment, slot_size: new(96, 48));
											}
										}
									}

								}

								GUI.SameLine();

								using (var group_character = GUI.Group.New(size: GUI.GetRemainingSpace()))
								{
									ref var character_data = ref h_selected_character_tmp.GetData();

									using (var group_title = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 24), padding: new(8, 8)))
									{
										if (character_data.IsNotNull())
										{
											GUI.TitleCentered(character_data.name, size: 24, pivot: new(0.00f, 0.00f));
										}
										else
										{
											GUI.TitleCentered("<no character selected>", size: 24, pivot: new(0.00f, 0.00f));
										}
									}

									GUI.SeparatorThick();

									using (var group_title = GUI.Group.New(size: GUI.GetRemainingSpace(y: -350), padding: new(8, 8)))
									{

									}

									GUI.SeparatorThick();

									using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(8, 8)))
									{
										GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

										using (var scrollable = GUI.Scrollbox.New("kits", size: GUI.GetRemainingSpace(), padding: new(4, 4), force_scrollbar: true))
										{
											if (character_data.IsNotNull() && armory.IsNotNull() && shipment.IsNotNull() && h_inventory.IsValid())
											{
												var shipment_armory_span = shipment.items.AsSpan();

												var kits_unavailable_count = 0;
												Span<IKit.Handle> kits_unavailable = stackalloc IKit.Handle[32];

												foreach (var asset in IKit.Database.GetAssets())
												{
													if (asset.id == 0) continue;
													ref var kit_data = ref asset.GetData();
													var h_kit = asset.GetHandle();

													if (kit_data.character_flags.Evaluate(character_data.flags) < 0.50f) continue;

													var valid = false;

													if (h_kit.Evaluate(ref character_data))
													{
														Span<Crafting.Requirement> requirements = stackalloc Crafting.Requirement[8];

														foreach (ref var item in kit_data.shipment.items)
														{
															if (!item.IsValid()) continue;
															if (item.flags.HasAny(Shipment.Item.Flags.No_Consume)) continue;

															requirements.Add(item.ToRequirement());
														}

														if (Crafting.Evaluate2(ref crafting_context, requirements, Crafting.EvaluateFlags.None))
														{
															valid = true;
														}
													}

													//GUI.Text($"{valid}");

													if (valid)
													{
														Dormitory.DrawKit(ref h_kit, ref kit_data, ref h_inventory, ref shipment_armory_span, true, selected_items);
													}
													else
													{
														kits_unavailable.Add(h_kit, ref kits_unavailable_count);
													}
												}

												for (var i = 0; i < kits_unavailable_count; i++)
												{
													var h_kit_unavailable = kits_unavailable[i];
													ref var kit_unavailable_data = ref h_kit_unavailable.GetData();

													if (kit_unavailable_data.IsNotNull())
													{
														Dormitory.DrawKit(ref h_kit_unavailable, ref kit_unavailable_data, ref h_inventory, ref shipment_armory_span, false, selected_items);
													}
												}
											}
										}
									}

									if (ent_selected_spawn.IsAlive())
									{
										ref var faction = ref ent_selected_spawn.GetComponent<Faction.Data>();
										if (faction.IsNull() || faction.id == player.faction_id)
										{
											if (GUI.DrawButton("Respawn", size: new Vector2(GUI.GetRemainingWidth(), 48), color: GUI.col_button_ok, enabled: h_selected_character_tmp.id != 0 && dormitory.IsNotNull()))
											{
												var rpc = new Dormitory.DEV_SpawnRPC()
												{
													h_character = h_selected_character_tmp
												};

												foreach (var h_kit in selected_items)
												{
													rpc.kits.TryAdd(in h_kit);
												}

												rpc.Send(ent_selected_spawn);

												h_selected_character = default;
											}
										}
										else
										{
											ent_selected_spawn = default;
										}
									}
								}
							}

							if (ent_selected_spawn_new.HasValue && ent_selected_spawn_new != ent_selected_spawn)
							{
								var rpc = new RespawnExt.SetSpawnRPC()
								{
									ent_spawn = ent_selected_spawn_new.Value
								};
								rpc.Send(player.ent_player);
								ent_selected_spawn_new = default;
							}

							//GUI.SeparatorThick();

							//this.DrawSpawns(ref region, size: new(GUI.GetRemainingWidth(), (24 * 4.50f) + 8));

							//GUI.SeparatorThick();

							////var h_character = default(ICharacter.Handle);

							//var spawn_info = default(Respawn.SpawnInfo);

							////var span_keys = this.respawn.spawns_keys.AsSpan();
							////var span_values = this.respawn.spawns_values.AsSpan();

							////var index = span_keys.IndexOf(Spawn.RespawnGUI.ent_selected_spawn);
							////if (index >= 0)
							////{
							////	spawn_info = span_values[index];

							////	//ref var info = ref span_values[index];
							////	//h_character = info.character;
							////}

							////using (var group_row = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 80)))
							////{
							////	RespawnExt.DrawCharacter(ref spawn_info);
							////}

							////using (var scrollbox = GUI.Scrollbox.New("levels", size: new(GUI.GetRemainingWidth(), GUI.GetRemainingHeight() - 48), padding: new(0)))
							////{
							////	//GUI.Title("Experience", size: 20);

							////	var h_character = spawn_info.character;
							////	ref var character = ref h_character.GetData();
							////	if (character.IsNotNull())
							////	{
							////		Experience.DrawTableSmall2(ref character.experience);
							////	}
							////}

							//GUI.SeparatorThick();


							////ref var character_data = ref h_character.GetData();
							////if (character_data.IsNotNull())
							////{
							////	ref var origin_data = ref character_data.origin.GetData();
							////	if (origin_data.IsNotNull())
							////	{
							////		Experience.DrawTableSmall(ref origin_data.experience);
							////	}
							////}

							//if (Spawn.RespawnGUI.ent_selected_spawn.IsAlive())
							//{
							//	ref var faction = ref Spawn.RespawnGUI.ent_selected_spawn.GetComponent<Faction.Data>();
							//	if (faction.IsNull() || faction.id == player.faction_id)
							//	{
							//		ref var spawn = ref Spawn.RespawnGUI.ent_selected_spawn.GetComponent<Spawn.Data>();
							//		if (!spawn.IsNull())
							//		{
							//			if (GUI.DrawButton((this.respawn.cooldown > 0.00f ? $"Respawn ({MathF.Floor(this.respawn.cooldown):0}s)" : "Respawn"), new Vector2(168, 48), enabled: this.respawn.cooldown <= float.Epsilon && current_cost <= this.respawn.tokens && spawn_info.character.id != 0, font_size: 24, color: GUI.font_color_green_b))
							//			{
							//				var rpc = new RespawnExt.SpawnRPC
							//				{
							//					ent_spawn = Spawn.RespawnGUI.ent_selected_spawn
							//				};
							//				rpc.Send(player.ent_player);
							//			}
							//			if (GUI.IsItemHovered())
							//			{
							//				using (GUI.Tooltip.New())
							//				{
							//					GUI.Text("Respawn as this character at the selected spawn point.");
							//				}
							//			}
							//		}
							//		else
							//		{

							//		}
							//	}
							//	else
							//	{
							//		Spawn.RespawnGUI.ent_selected_spawn = default;
							//	}
							//}

							//if (ent_selected_spawn_new.HasValue && ent_selected_spawn_new != Spawn.RespawnGUI.ent_selected_spawn)
							//{
							//	var rpc = new RespawnExt.SetSpawnRPC()
							//	{
							//		ent_spawn = ent_selected_spawn_new.Value
							//	};
							//	rpc.Send(player.ent_player);
							//	ent_selected_spawn_new = default;
							//}
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
								var faction_id_tmp = this.faction_id;

								foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
								{
									row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
									{
										if (faction.id != 0 && faction.id == faction_id_tmp)
										{
											//GUI.DrawBackground(GUI.tex_panel_white, GUI.GetRemainingRect(), new(4), faction.color_a);

											DrawSpawnsRow(entity, in spawn, in nameable, in faction);
										}
									});
								}

								//region.Query<Region.GetSpawnsQuery>(FuncA).Execute(ref this);
								//static void FuncA(ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction)
								//{
								//	ref var data = ref info.GetParameter<RespawnGUI>();
								//	if (!data.IsNull())
								//	{
								//		if (faction.id != 0 && faction.id == data.faction_id)
								//		{
								//			//GUI.DrawBackground(GUI.tex_panel_white, GUI.GetRemainingRect(), new(4), faction.color_a);

								//			DrawSpawnsRow(entity, in spawn, in nameable, in faction);
								//		}
								//	}
								//}

								//GUI.NewLine(4);
								//GUI.Separator(faction.color_a.WithAlphaMult(0.50f));
								//GUI.NewLine(4);
							}

							foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
							{
								row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
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
								});
							}

							//	region.Query<Region.GetSpawnsQuery>(FuncB).Execute(ref this);
							//static void FuncB(ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction)
							//{
							//	ref var data = ref info.GetParameter<RespawnGUI>();
							//	if (!data.IsNull())
							//	{
							//		if (faction.id == 0)
							//		{
							//			var pressed = DrawSpawnsRow(entity, in spawn, in nameable, in faction);
							//			if (pressed)
							//			{
							//				//var rpc = new RespawnExt.SetSpawnRPC()
							//				//{
							//				//	ent_spawn = entity
							//				//};
							//				//rpc.Send(data.ent_respawn);
							//			}
							//		}
							//	}
							//}
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

