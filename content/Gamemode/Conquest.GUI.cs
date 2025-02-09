﻿using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		[Shitcode]
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
				//var rem_height = Maths.Max(GUI.CanvasSize.Y - total_height, 0.00f);

				//var rem_height = GUI.CanvasSize.Y - RespawnGUI.window_offset.Y

				//RespawnGUI.window_size.Y = Maths.Clamp(RespawnGUI.window_size.Y, 0, GUI.CanvasSize.Y - RespawnGUI.window_offset.Y - 40);

				var max_height = GUI.CanvasSize.Y - Spawn.RespawnGUI.window_offset.Y - 12;

				Spawn.RespawnGUI.window_size.Y = Maths.Clamp(Spawn.RespawnGUI.window_size.Y, 0, max_height);

				//Spawn.RespawnGUI.ent_selected_spawn = this.respawn.ent_selected_spawn;
				ref var ent_selected_spawn = ref Spawn.RespawnGUI.ent_selected_spawn;
				ent_selected_spawn = this.respawn.ent_selected_spawn;

				var h_faction = this.faction_id;

				Spawn.RespawnGUI.window_size = new Vector2(580, 480);

				using (var window = GUI.Window.Standalone("Respawn"u8, position: new Vector2(GUI.CanvasSize.X * 0.40f, 0) + Spawn.RespawnGUI.window_offset, pivot: Spawn.RespawnGUI.window_pivot, size: Spawn.RespawnGUI.window_size, flags: GUI.Window.Flags.No_Appear_Focus))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						ref var region = ref this.ent_respawn.GetRegion();
						ref var player = ref Client.GetPlayerData(out var player_asset);

						var random = XorRandom.New(true);

						GUI.DrawWindowBackground(GUI.tex_window_menu, padding: new Vector4(8, 8, 8, 8));

						using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(8, 8)))
						{
							var is_selected_spawn_valid = false;

							if (ent_selected_spawn.IsAlive())
							{
								ref var spawn = ref ent_selected_spawn.GetComponent<Spawn.Data>();
								ref var dormitory = ref ent_selected_spawn.GetComponent<Dormitory.Data>();

								if (spawn.IsNotNull() && dormitory.IsNotNull())
								{
									var h_faction_spawn = ent_selected_spawn.GetFactionHandle();
									var is_visible = false;
									var has_characters = false;

									is_selected_spawn_valid = (is_visible = spawn.IsVisibleToFaction(h_faction, h_faction_spawn))
										&& ((has_characters = dormitory.HasSpawnableCharacters(h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: player_asset, spawn_flags: spawn.flags)) || spawn.IsSelectableByFaction(h_faction, h_faction_spawn, has_characters, is_visible));
								}
							}

							if (!is_selected_spawn_valid && false)
							{
								ent_selected_spawn = default;

								//var player_faction_id = player.faction_id;

								foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
								{
									row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
									{
										if (spawn.IsVisibleToFaction(h_faction, faction.id))
										{
											ref var dormitory = ref entity.GetComponent<Dormitory.Data>();
											if (dormitory.IsNotNull())
											{
												var h_faction_spawn = faction.id;
												var is_visible = false;
												var has_characters = false;

												is_selected_spawn_valid = (is_visible = spawn.IsVisibleToFaction(h_faction, h_faction_spawn))
													&& ((has_characters = dormitory.HasSpawnableCharacters(h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: player_asset, spawn_flags: spawn.flags)) || spawn.IsSelectableByFaction(h_faction, h_faction_spawn, has_characters, is_visible));

												//is_selected_spawn_valid = spawn.IsVisibleToFaction(h_faction, h_faction_spawn) && (h_faction_spawn == h_faction || dormitory.HasSpawnableCharacters(h_faction, h_faction_spawn, spawn.flags));
											}

											//if (Spawn.RespawnGUI.ent_selected_spawn.id == 0 || random.NextBool(0.30f))
											//{
											//	ent_selected_spawn_new = entity;
											//}
										}
									});

									if (is_selected_spawn_valid)
									{
										ent_selected_spawn_new = row.Entity;
										//ent_selected_spawn = row.Entity;
										break;
									}
								}
							}

							{
								using (GUI.Wrap.Push(GUI.RmX))
								{
									using (var group = GUI.Group.New(size: new(GUI.RmX, 92), padding: new(4)))
									{
										//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
										//if (minimap != null)
										//{
										//	var map_frame_size = minimap.GetFrameSize(2);
										//	map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.RmX, 80));

										//	Minimap.DrawMap(ref region, minimap, map_frame_size, map_scale: 1.00f);
										//}

										ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
										if (minimap != null)
										{
											var map_frame_size = minimap.GetFrameSize(2);
											map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.RmX, 80));

											using (group.Split(size: map_frame_size, GUI.AlignX.Left, GUI.AlignY.Top))
											{
												using (var map = GUI.Map.New(ref region, minimap, size: map_frame_size, map_scale: 1.00f, draw_markers: false))
												{
													//ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable

													foreach (ref var row in region.IterateQuery<Minimap.GetMarkersQuery>())
													{
														var selected = row.Entity == ent_selected_spawn;
														var is_selectable = false;
														var is_visible = false;
														var has_characters = false;

														var transform_copy = default(Transform.Data);
														//var nameable_copy = default(Nameable.Data);
														var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);
														var marker_copy = default(Minimap.Marker.Data);
														//var alpha = 1.00f;

														var faction_id_tmp = this.faction_id;

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

																	is_selectable = spawn.IsSelectableByFaction(faction_id_tmp, faction.id, has_characters, is_visible); // has_characters || faction.id == faction_id_tmp || (faction.id == 0 && spawn.flags.HasAny(Spawn.Flags.Public));
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
															var alpha = has_characters ? 1.00f : 0.65f;
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
																			//App.WriteLine("press");
																			ent_selected_spawn_new = row.Entity;
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
							}

							GUI.SeparatorThick();

							if (ent_selected_spawn.IsAlive())
							{
								var context = GUI.ItemContext.Begin(is_readonly: true);
								var available_items = Span<Shipment.Item>.Empty;

								ref var faction = ref ent_selected_spawn.GetComponent<Faction.Data>();
								ref var spawn = ref ent_selected_spawn.GetComponent<Spawn.Data>();

								var oc_shipment = ent_selected_spawn.GetComponentWithOwner<Shipment.Data>(Relation.Type.Instance);
								if (oc_shipment.IsValid() && oc_shipment.data.flags.HasAnyExcept(Shipment.Flags.Allow_Withdraw, Shipment.Flags.No_GUI | Shipment.Flags.Staging | Shipment.Flags.Locked))
								{
									available_items = oc_shipment.data.items.AsSpan();
								}

								var h_inventory = default(Inventory.Handle);

								var h_selected_character_tmp = h_selected_character;

								ref var dormitory = ref ent_selected_spawn.GetComponent<Dormitory.Data>();
								if (dormitory.IsNotNull())
								{
									var characters = dormitory.GetCharacterSpan();
									if (h_selected_character_tmp && !characters.Contains(h_selected_character_tmp))
									{
										h_selected_character_tmp = default;
									}
								}

								ref var armory = ref ent_selected_spawn.GetComponent<Armory.Data>();
								if (armory.IsNotNull())
								{
									armory.inv_storage.TryGetHandle(out h_inventory);
								}

								var has_storage = h_inventory.IsValid() || !available_items.IsEmpty;
								var is_empty = true;

								//if (dormitory.IsNotNull())
								//{
								//	var characters = dormitory.characters.Slice(dormitory.characters_capacity);
								//	is_empty = characters.GetFilledCount() == 0;
								//}

								//Crafting.Context.New(ref region, ent_selected_spawn, ent_selected_spawn, out var crafting_context, inventory: h_inventory, shipment: oc_shipment, search_radius: 0.00f, h_faction: this.faction_id);
								Crafting.Context.NewFromSelf(ref region.AsCommon(), ent_selected_spawn, out var crafting_context, search_radius: 0.00f);

								//var context = GUI.ItemContext.Begin();

								using (var group_title = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(4, 0)))
								{
									var spawn_name = ent_selected_spawn.GetFullName();
									GUI.TitleCentered(spawn_name, size: 32, pivot: new(0.00f, 0.50f));

									//if (is_empty)
									//{
									//	GUI.TitleCentered("This spawnpoint is empty.", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, -8));
									//	GUI.TitleCentered("Select another one on the map!", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, 8));
									//}
								}

								GUI.SeparatorThick();

								using (GUI.Group.New(size: GUI.Rm with { X = 304 }, padding: new(0, 0)))
								{
									//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4, 0)))
									//{
									//	var spawn_name = ent_selected_spawn.GetFullName();
									//	GUI.TitleCentered(spawn_name, size: 24, pivot: new(0.00f, 0.50f));
									//}

									//GUI.SeparatorThick();

									using (var scrollable = GUI.Scrollbox.New("characters"u8, size: GUI.GetRemainingSpace(y: (has_storage ? -96 - 8 - 8 : 0)), padding: new(4, 4), force_scrollbar: true))
									{
										if (dormitory.IsNotNull())
										{
											var characters = dormitory.GetCharacterSpan();

											//var characters_count_max = Math.Min(characters.Length, dormitory.characters_capacity);
											for (var i = 0; i < characters.Length; i++)
											{
												//DrawCharacter(characters[i].GetHandle());

												var h_character = characters[i];
												using (GUI.ID<Conquest.RespawnGUI, Character.Data>.Push(h_character))
												{
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
													{
														ref var character_data = ref h_character.GetData();

														if (h_character && !h_selected_character_tmp)
														{
															h_selected_character_tmp = h_character;
															h_selected_character = h_character;
														}

														is_empty &= !h_character;
														//var selectable = h_character.CanSpawnAsCharacter(faction_id, faction.id, spawn.flags); //  character_data.IsNotNull() && character_data.faction == faction_id;

														Dormitory.DrawCharacterSmall(h_character);

														var selected = h_selected_character_tmp && h_character == h_selected_character_tmp; // selected_index;
														if (GUI.Selectable3("selectable"u8, group_row.GetOuterRect(), selected))
														{
															h_selected_character = h_character;
														}

														GUI.FocusableAsset(h_character);
													}
												}
											}
										}
									}

									if (has_storage)
									{
										using (var group_storage = GUI.Group.New(size: GUI.Rm, padding: new(8, 8)))
										{
											GUI.DrawBackground(GUI.tex_frame, group_storage.GetOuterRect(), new(8, 8, 8, 8));

											if (h_inventory.IsValid())
											{
												using (GUI.Group.New(size: h_inventory.GetFrameSize(2, 0)))
												{
													GUI.DrawInventory(h_inventory, is_readonly: true);
												}
											}

											GUI.SameLine();

											if (oc_shipment.data.IsNotNull())
											{
												using (GUI.Group.New(size: GUI.Rm))
												{
													GUI.DrawShipment(ref context, ent_selected_spawn, ref oc_shipment.data, slot_size: new(96, 48));
												}
											}
										}
									}
								}

								GUI.SameLine();

								using (var group_character = GUI.Group.New(size: GUI.Rm))
								{
									var selected_items = Spawn.RespawnGUI.character_id_to_selected_items.GetOrAdd(h_selected_character_tmp);

									ref var character_data = ref h_selected_character_tmp.GetData();
									using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(2, 4)))
									{
										GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

										//if (character_data.IsNotNull())
										//{
										//	if (selected_items != null)
										//	{
										//		foreach (var h_kit in character_data.kits)
										//		{
										//			selected_items.Add(h_kit);
										//		}
										//	}
										//}

										//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(8, 8)))
										//{
										//	if (character_data.IsNotNull())
										//	{
										//		GUI.TitleCentered(character_data.name, size: 24, pivot: new(0.00f, 0.00f));
										//	}
										//	else
										//	{
										//		GUI.TitleCentered("<no character selected>"u8, size: 24, pivot: new(0.00f, 0.00f));
										//	}
										//}

										if (dormitory.flags.HasNone(Dormitory.Flags.Hide_XP))
										{
											using (var group_xp = GUI.Group.New(size: GUI.GetRemainingSpace(y: -128)))
											{
												using (var scrollbox = GUI.Scrollbox.New("scrollbox_xp"u8, size: GUI.Rm))
												{
													GUI.DrawBackground(GUI.tex_panel, scrollbox.group_frame.GetOuterRect(), new(8, 8, 8, 8));

													if (character_data.IsNotNull())
													{
														Experience.DrawTableSmall2(ref character_data.experience);
													}

													//if (origin_data.IsNotNull())
													//{
													//	Experience.DrawTableSmall(ref origin_data.experience);
													//}
												}
											}

											GUI.SeparatorThick();
										}

										if (dormitory.flags.HasNone(Dormitory.Flags.Hide_Kits))
										{
											//GUI.SeparatorThick();

											//using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(2, 4)))
											//{
											//	GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

											using (var scrollable = GUI.Scrollbox.New("kits"u8, size: GUI.Rm, padding: new(4, 4), force_scrollbar: true))
											{
												Dormitory.DrawKits(ref dormitory, ref crafting_context, ref character_data, h_inventory, has_storage, available_items, selected_items);
											}
											//}

											//GUI.SeparatorThick();

										}
									}

									//if (spawn.IsNotNull() && (faction.IsNull() || faction.id == 0 || faction.id == player.faction_id || (player.faction_id == 0 && spawn.flags.HasAny(Spawn.Flags.Neutral_Only))))
									//{
									if (is_empty)
									{
										if (GUI.DrawButton("No characters available."u8, size: new Vector2(GUI.RmX, 48), color: GUI.col_button_error, error: true))
										{

										}
										GUI.DrawHoverTooltip("This spawnpoint doesn't have any more characters left.\n\nSelect another spawnpoint on the map."u8);
									}
									else
									{
										var is_selected_character_spawnable = h_selected_character_tmp.CanSpawnAsCharacter(h_faction: h_faction, h_faction_spawn: faction.id, h_player: player_asset, spawn_flags: spawn.flags);
										if (is_selected_character_spawnable)
										{
											if (GUI.DrawButton("Spawn"u8, size: new Vector2(GUI.RmX, 48), color: GUI.col_button_ok))
											{
												var rpc = new Spawn.SpawnRPC()
												{
													h_character = h_selected_character_tmp,
													h_component = IComponent.Handle.FromComponent<Dormitory.Data>(),
													control = true
												};

												foreach (var h_kit in selected_items)
												{
													rpc.kits.TryAdd(in h_kit);
												}

												//rpc.Send(ent_selected_spawn);
												rpc.Send(ent_selected_spawn);
												//AsTask(ent_selected_spawn).ContinueWith((result) =>
												//{
												//	//App.WriteLine(result.out_ent_spawned);
												//});

												h_selected_character = default;
											}
										}
										else
										{
											if (GUI.DrawButton("Cannot spawn as this character."u8, size: new Vector2(GUI.RmX, 48), color: GUI.col_button_error, error: true))
											{

											}
											GUI.DrawHoverTooltip("Character belongs to another faction."u8);
										}
									}

								}
							}
							else
							{
								using (var group_title = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(4, 0)))
								{
									GUI.TitleCentered("No Spawns Available"u8, size: 32, pivot: new(0.50f, 1.00f));

									//if (is_empty)
									//{
									//	GUI.TitleCentered("This spawnpoint is empty.", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, -8));
									//	GUI.TitleCentered("Select another one on the map!", size: 16, pivot: new(1.00f, 0.50f), color: GUI.font_color_yellow, offset: new(0, 8));
									//}
								}

								using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
								{
									GUI.SeparatorThick();

									ref var faction_data = ref this.faction_id.GetData();
									if (faction_data.IsNotNull())
									{
										using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
										{
											GUI.Title("Your faction has no established presence in this region."u8, size: 20);

											//if (GUI.DrawButton($"Deploy a Scout ({faction_data.scout_count} available)", size: new(300, 40), font_size: 20, color: GUI.col_button_yellow, enabled: faction_data.scout_count > 0))
											//{
											//	var rpc = new Conquest.DeployInfiltratorRPC()
											//	{
											//		h_character = 0
											//	};
											//	rpc.Send();
											//}
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
								rpc.Send(Client.GetEntity());

								//ent_selected_spawn = ent_selected_spawn_new.Value;
								ent_selected_spawn_new = default;
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIRespawn(Entity entity, [Source.Owned] in Player.Data player, [Source.Owned] in Respawn.Data respawn)
		{
			Spawn.RespawnGUI.enabled = false;

			//if (player.IsLocal())
			{
				if (!WorldMap.IsOpen && player.flags.HasNone(Player.Flags.Alive) && !(player.flags.HasAny(Player.Flags.Editor) && !Editor.show_respawn_menu))
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

