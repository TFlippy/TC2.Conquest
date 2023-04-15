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
													var marker_copy = default(Minimap.Marker.Data);
													var alpha = 1.00f;

													var faction_id_tmp = this.faction_id;

													var ok = false;

													row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable) =>
													{
														transform_copy = transform;
														//nameable_copy = nameable;
														marker_copy = marker;

														if ((faction.id == 0 || faction.id == faction_id_tmp) && marker.flags.HasAll(Minimap.Marker.Flags.Spawner))
														{
															if (faction.id.TryGetData(out var ref_faction))
															{
																color = ref_faction.value.color_a;
															}
															//sprite.frame.X = 1;
															ok = true;

															ref var dormitory = ref entity.GetComponent<Dormitory.Data>();
															if (dormitory.IsNotNull())
															{
																var dormitory_characters = dormitory.characters.Slice(dormitory.characters_capacity);

																var is_empty = dormitory_characters.GetFilledCount() == 0;
																if (is_empty)
																{
																	alpha = 0.65f;
																}
															}
														}
													});

													if (ok)
													{
														using (var node = map.DrawNode(marker_copy.sprite, transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: (selected ? Color32BGRA.White : color).WithAlphaMult(alpha), color_hovered: selected ? Color32BGRA.White : Color32BGRA.Lerp(color, Color32BGRA.White, 0.50f).WithAlphaMult(alpha)))
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
								var available_items = Span<Shipment.Item>.Empty;

								var oc_shipment = ent_selected_spawn.GetComponentWithOwner<Shipment.Data>(Relation.Type.Instance);
								if (oc_shipment.IsValid())
								{
									available_items = oc_shipment.data.items.AsSpan();
								}

								var h_inventory = default(Inventory.Handle);

								var h_selected_character_tmp = h_selected_character;

								ref var dormitory = ref ent_selected_spawn.GetComponent<Dormitory.Data>();
								if (dormitory.IsNotNull())
								{
									var characters = dormitory.characters.Slice(dormitory.characters_capacity);
									if (h_selected_character_tmp.id != 0 && !characters.Contains(h_selected_character_tmp))
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

								Crafting.Context.New(ref region, ent_selected_spawn, ent_selected_spawn, out var crafting_context, inventory: h_inventory, shipment: oc_shipment, search_radius: 0.00f);

								//var context = GUI.ItemContext.Begin();

								using (var group_title = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 40), padding: new(4, 0)))
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

								using (GUI.Group.New(size: GUI.GetRemainingSpace() with { X = 304 }, padding: new(0, 0)))
								{
									//using (var group_title = GUI.Group.New(size: new(GUI.GetRemainingWidth(), 32), padding: new(4, 0)))
									//{
									//	var spawn_name = ent_selected_spawn.GetFullName();
									//	GUI.TitleCentered(spawn_name, size: 24, pivot: new(0.00f, 0.50f));
									//}

									//GUI.SeparatorThick();

									using (var scrollable = GUI.Scrollbox.New("characters", size: GUI.GetRemainingSpace(y: (has_storage ? -96 - 8 - 8 : 0)), padding: new(4, 4), force_scrollbar: true))
									{
										if (dormitory.IsNotNull())
										{
											var characters = dormitory.characters.Slice(dormitory.characters_capacity);

											//var characters_count_max = Math.Min(characters.Length, dormitory.characters_capacity);
											for (var i = 0; i < characters.Length; i++)
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

														is_empty &= h_character.id == 0;

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

									if (has_storage)
									{
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

											if (oc_shipment.data.IsNotNull())
											{
												using (GUI.Group.New(size: GUI.GetRemainingSpace()))
												{
													GUI.DrawShipment(ref context, ent_selected_spawn, ref oc_shipment.data, slot_size: new(96, 48));
												}
											}
										}
									}
								}

								GUI.SameLine();

								using (var group_character = GUI.Group.New(size: GUI.GetRemainingSpace()))
								{
									var selected_items = Spawn.RespawnGUI.character_id_to_selected_items.GetOrAdd(h_selected_character_tmp);

									ref var character_data = ref h_selected_character_tmp.GetData();
									if (character_data.IsNotNull())
									{
										if (selected_items != null)
										{
											foreach (ref var h_kit in character_data.kits)
											{
												selected_items.Add(h_kit);
											}
										}
									}

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

									using (var group_title = GUI.Group.New(size: GUI.GetRemainingSpace(y: -300), padding: new(2, 4)))
									{
										using (var scrollbox = GUI.Scrollbox.New("scrollbox_xp", size: GUI.GetRemainingSpace()))
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

									using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(2, 4)))
									{
										GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

										using (var scrollable = GUI.Scrollbox.New("kits", size: GUI.GetRemainingSpace(), padding: new(4, 4), force_scrollbar: true))
										{
											if (character_data.IsNotNull())
											{
												if (dormitory.flags.HasAny(Dormitory.Flags.No_Kit_Selection))
												{
													foreach (var h_kit in selected_items)
													{
														Dormitory.DrawKit(in h_kit, true, true, h_inventory, available_items, force_readonly: true);
													}
												}
												else
												{
													var kits_unavailable_count = 0;
													Span<IKit.Handle> kits_unavailable = stackalloc IKit.Handle[32];

													foreach (var asset in IKit.Database.GetAssets())
													{
														if (asset.id == 0) continue;
														ref var kit_data = ref asset.GetData();
														var h_kit = asset.GetHandle();
														if (!dormitory.kit_filter.Evaluate(kit_data.flags) && !character_data.kits.Contains(h_kit)) continue;

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
															var selected = selected_items.Contains(h_kit);
															if (Dormitory.DrawKit(in h_kit, true, selected: selected, h_inventory: h_inventory, available_items: available_items))
															{
																if (selected) selected_items.Remove(h_kit);
																else selected_items.Add(h_kit);
															}
														}
														else
														{
															kits_unavailable.Add(h_kit, ref kits_unavailable_count);
														}
													}

													if (has_storage)
													{
														for (var i = 0; i < kits_unavailable_count; i++)
														{
															var h_kit = kits_unavailable[i];
															ref var kit_data = ref h_kit.GetData();

															if (kit_data.IsNotNull())
															{
																var selected = selected_items.Contains(h_kit);
																if (Dormitory.DrawKit(in h_kit, false, false, h_inventory, available_items))
																{
																	if (selected) selected_items.Remove(h_kit);
																	else selected_items.Add(h_kit);
																}
															}
														}
													}
												}
											}
										}
									}

									if (ent_selected_spawn.IsAlive())
									{
										ref var faction = ref ent_selected_spawn.GetComponent<Faction.Data>();
										if (faction.IsNull() || faction.id == 0 || faction.id == player.faction_id)
										{
											if (is_empty)
											{
												if (GUI.DrawButton("No characters available.", size: new Vector2(GUI.GetRemainingWidth(), 48), color: GUI.col_button_error, error: true))
												{

												}
												GUI.DrawHoverTooltip("This spawnpoint doesn't have any more characters left.\n\nSelect another spawnpoint on the map.");
											}
											else
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

