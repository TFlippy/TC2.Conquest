using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		[Net.MsgPack]
		public struct PinnedItem
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Done = 1 << 0,

			}

			[Net.Key(00), Save.Force] public PinnedItem.Flags flags;

			[Save.NewLine]
			[Net.Key(01), Save.Force] public Shipment.Item2 item;
			[Net.Key(02), Save.Force] public float amount_req;
		}

		// WIP/experimental stuff
		public partial struct SideMenuGUI: IGUICommand
		{
			public Entity ent_player;

			public static readonly HashSet<IHelp.Handle> hs_selected_handles = new(32);

			public static IHelp.Handle edit_h_selected;
			public static string edit_help_filter;
			[FixedAddressValueType] public static PinnedList<PinnedItem> pinned_items = new(32);

			public const bool enable_help = false;
			public const bool enable_pins = false;
			public const bool enable_interact = true;

			public void Draw()
			{
				ref var region = ref this.ent_player.GetRegion();
				if (region.IsNull()) return;

				ref var player_data = ref Client.GetPlayerData();
				if (player_data.IsNull()) return;

				var h_character = Client.GetCharacterHandle();
				ref var character = ref region.GetCharacter(h_character);
				if (character.IsNotNull())
				{
					using (var window = GUI.Window.Standalone("sidemenu"u8, position: new(GUI.CanvasSize.X - 8, 36),
					pivot: new(1.00f, 0.00f), size: new((24 * 9) + 14, 48 * 10), padding: new(8)))
					{
						if (window.show)
						{
							var hs_selected_handles = SideMenuGUI.hs_selected_handles;

							//GUI.DrawWindowBackground(GUI.tex_window_popup_l, 4, color: GUI.col_button);
							GUI.DrawWindowBackground(GUI.tex_window_sidebar, 4);




							//using (var group_top = GUI.Group.New(size: new(GUI.RmX, 32)))
							//{
							//	if (GUI.DrawIconButton("button.a"u8, Sprite.Empty, size: new(GUI.RmY)))
							//	{

							//	}
							//}

							//GUI.SeparatorThick();

							using (var scrollbox = GUI.Scrollbox.New("sidemenu.scroll"u8, size: GUI.Rm))
							{
								if (true)
								{
									using (var collapsible = GUI.Collapsible2.New("col.help"u8, size: new(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Tutorial"u8, size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner())
										{
											//using (var group_help = GUI.Group.New(size: new(GUI.RmX, 0)))
											{
												if (GUI.TextInput("help.search"u8, "<search>"u8, ref edit_help_filter, size: new(GUI.RmX, 32), max_length: 32))
												{

												}
												GUI.FocusOnCtrlF();

												//var assets = IHelp.Database.GetAssetsSpan();
												var categories = IHelp.Database.GetAssets().GroupBy(x => x.data.category, StringComparer.Ordinal).OrderBy(x => x.Key);
												//foreach (var asset in assets)

												//var ts = Timestamp.Now();

												//var categories = IHelp.category_to_handles;
												//foreach (var (category, list) in categories.OrderBy(x => x.Key, StringComparer.Ordinal))
												//foreach (var pair in categories.OrderBy(x => x.Key, StringComparer.Ordinal))
												foreach (var pair in categories)
												{
													var category = pair.Key;

													using (var push_id_category = GUI.ID<SideMenuGUI>.Push(category))
													using (var collapsible_category = GUI.Collapsible2.New("col.category"u8, size: new(GUI.RmX, 32), default_open: false))
													{
														GUI.TitleCentered(category, size: 16, pivot: new(0.00f, 0.50f));

														if (collapsible_category.Inner())
														{
															foreach (var asset in pair.OrderByDescending(x => x.data.order))
															//foreach (var asset in assets)
															{
																ref var help_data = ref asset.GetData(out var h_help);
																//if (help_data.IsNull()) continue;

																var is_visible = false;
																var is_selected = h_help == edit_h_selected; // hs_selected_handles.Contains(h_help);

																using (var push_id = GUI.ID<SideMenuGUI, IHelp.Data>.Push(h_help))
																using (var group_row = GUI.Group.New(size: new(GUI.RmX, 28)))
																{
																	is_visible = group_row.IsVisible();
																	if (is_visible)
																	{
																		//group_row.DrawBackground(GUI.tex_panel);
																		//GUI.TitleCentered(ent_result.GetName(), pivot: new(0.00f, 0.50f), offset: new(6, 0));

																		using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
																		{
																			group_icon.DrawBackground(GUI.tex_slot_filled);

																		}

																		GUI.SameLine(-4);

																		using (var group_text = GUI.Group.New(size: GUI.Rm))
																		{
																			group_text.DrawBackground(GUI.tex_window_popup_r, color: GUI.col_button);

																			GUI.TitleCentered(help_data.name, pivot: new(0.00f, 0.50f), offset: new(10, 0));
																		}

																		var row_rect = group_row.GetOuterRect();

																		if (GUI.Selectable3(id: push_id.hash, rect: row_rect, selected: is_selected))
																		{
																			edit_h_selected.Toggle(h_help);
																			//hs_selected_handles.Toggle(h_help);
																		}
																	}
																}

																if (is_visible)
																{
																	//var row_rect = GUI.GetLastItemRect(out var row_hash);
																	//var is_selected = hs_selected_handles.Contains(h_help);

																	//if (GUI.Selectable3(id: row_hash, rect: row_rect, selected: is_selected))
																	//{
																	//	//hs_selected_handles.Toggle(h_help);
																	//}


																	//if (!GUI.IsAnyTooltipVisible() && (is_selected || GUI.IsItemHovered()))
																	if (GUI.IsItemHovered())
																	{
																		using (var tooltip = GUI.Tooltip.New(pos: GUI.GetLastItemRect().tl, pivot: new(1.00f, 0.00f), size: new(244.00f, 0.00f)))
																		using (GUI.Wrap.Push(GUI.RmX))
																		{
																			GUI.Title(help_data.name);

																			GUI.SeparatorThick(spacing: 2);

																			GUI.NewLine(4);
																			GUI.TextShaded(help_data.desc);

																			var entries = help_data.entries.AsSpan();
																			if (!entries.IsEmpty)
																			{
																				GUI.NewLine(4);

																				var entry_index = 0;
																				foreach (ref var entry in entries)
																				{
																					if (entry_index != 0)
																					{
																						GUI.NewLine(4);
																					}
																					//else if (entry.flags.HasAny(IHelp.Entry.Flags.Newline))
																					//{
																					//	GUI.NewLine(4);
																					//}

																					if (entry.flags.HasAny(IHelp.Entry.Flags.Bullet))
																					{
																						if (entry.flags.HasAny(IHelp.Entry.Flags.Nested))
																						{
																							GUI.SameLine(10);
																						}

																						GUI.Text("-"u8, color: entry.color);
																						GUI.SameLine(2);
																					}
																					//GUI.Text("- "u8);
																					//GUI.SameLine();
																					GUI.TextShaded(entry.text, color: entry.color);

																					entry_index++;
																				}
																			}
																			//GUI.DrawHoverTooltip(help_data.desc);
																		}
																	}

																	GUI.FocusableAsset(asset);
																}
															}
														}
													}
												}

												//var ts_elapsed = ts.GetMilliseconds();
												//GUI.Text(ts_elapsed, "0.0000'ms'");
											}
										}
									}
								}

								if (enable_pins)
								{
									using (var collapsible = GUI.Collapsible2.New("col.pins"u8, size: new(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Pins"u8, size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner())
										{

										}
									}
								}

								if (enable_interact)
								{
									using (var collapsible = GUI.Collapsible2.New("col.interact"u8, size: new(GUI.RmX, 32), default_open: true))
									{
										GUI.TitleCentered("Interactions"u8, size: 24, pivot: new(0.00f, 0.50f));

										if (collapsible.Inner())
										{
											using (var group_interactables = GUI.Group.New(size: new(GUI.RmX, 0)))
											{
												var results_span = FixedArray.CreateSpan32NoInit<OverlapResult>(out var results_buffer);
												if (region.TryOverlapPointAll(world_position: character.world_position.Snap(1.00f), radius: Interactor.c_max_distance,
												hits: ref results_span, mask: Physics.Layer.Interactable, exclude: Physics.Layer.Ignore_Hover))
												{
													results_span.Sort(static (a, b) => a.entity.lower.CompareToFast(b.entity.lower));
													//results_span.SortByDistance();
													var ent_controlled = character.ent_controlled;
													var ent_prev = Entity.None;

													foreach (var result in results_span)
													{
														//ref var faction = ref ent_squad.GetComponent<Faction.Data>();
														//if (faction.IsNotNull() && faction.id == player.faction_id)
														//{

														var ent_result = result.entity;
														if (ent_result == ent_controlled) continue;
														if (ent_result == ent_prev) continue;

														const float slot_size = 56.00f;

														GUI.TrySameLine(slot_size);

														using (var push_id = GUI.ID<Interactable.Data, Selection.Data>.Push(ent_result))
														//using (var group_row = GUI.Group.New(new Vector2(GUI.RmX, 24)))
														using (var group_row = GUI.Group.New(new Vector2(slot_size)))
														{
															//group_row.DrawBackground(GUI.tex_panel);
															//GUI.TitleCentered(ent_result.GetName(), pivot: new(0.00f, 0.50f), offset: new(6, 0));

															group_row.DrawBackground(GUI.tex_slot_simple);
															GUI.DrawEntityIcon(ent_result);

															var selected = ent_result == Interactable.GetCurrentTarget();
															if (GUI.Selectable3("select"u8, group_row.GetOuterRect(), selected: selected))
															{
																if (selected)
																{
																	Interactable.Close();
																}
																else
																{
																	Interactable.Open(ent_result);
																}

																//var rpc = new Selection.SelectSquadRPC()
																//{
																//	ent_squad = ent_squad
																//};
																//rpc.Send(this.ent_selection);

																//dropdown.Close();
															}
														}

														if (GUI.IsItemHovered())
														{
															using (var tooltip = GUI.Tooltip.New(pivot: new(1.00f, 0.125f), offset: new(-16, 0)))
															{
																GUI.HighlightEntity(ent_result, color: GUI.col_button_yellow.WithAlpha(128), layer: GUI.Layer.Background);
																GUI.DrawEntityMarker(ent_result);

																//GUI.Title(ent_result.GetName());
															}
														}

														// to avoid duplicates on entities that have more than 1 shape
														ent_prev = ent_result;
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

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUI_SideMenu(Entity ent_player, [Source.Owned] in Player.Data player)
		{
			if (true)
			{
				var gui = new SideMenuGUI()
				{
					ent_player = ent_player,
				};
				gui.Submit();
			}
		}
#endif
	}
}

