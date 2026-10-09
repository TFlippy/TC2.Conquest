using TC2.Base;
using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Depot
	{
		public static float GetUnitBuyPrice(ref readonly this Depot.Data depot, in Shipment.Item item)
		{
			var unit_market_price = item.GetUnitMarketPrice();
			return unit_market_price;
		}

		public static float GetUnitSellPrice(ref readonly this Depot.Data depot, in Shipment.Item item)
		{
			var unit_market_price = item.GetUnitMarketPrice();
			var t = Maths.InvLerp01(item.max * depot.low_price_threshold, item.max, item.quantity);
			var t_pow = Maths.Pow(t, depot.low_price_falloff);

			var ret = Maths.Lerp(unit_market_price, unit_market_price * depot.low_price_multiplier, t_pow);
			return ret;
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			public required Depot.Data.Flags flags;
			public ICatalogue.Handle h_catalogue;
			public ICoalition.Handle h_coalition;

			[Save.NewLine]
			[Save.Force] public required float stock_baseline_ratio = 0.80f;
			[Save.Force] public required float stock_initial_ratio = 0.40f;

			[Save.NewLine]
			[Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)]
			[Save.Force] public required float low_price_threshold = 0.75f;
			[Editor.Slider.Clamped(0.00f, 10.00f, snap: 0.001f)]
			[Save.Force] public required float low_price_multiplier = 0.50f;
			[Editor.Slider.Clamped(0.00f, 10.00f, snap: 0.001f)]
			[Save.Force] public required float low_price_falloff = 0.75f;
		}

		public struct DEV_SummonZeppelinRPC: Net.IRPC<Depot.Data>
		{
			public ICoalition.Handle h_coalition;
			//public Vec2f pos_target;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Depot.Data data)
			{
				App.WriteLine("summon");

				ref var coalition_data = ref this.h_coalition.GetData();
				Assert.IsNotNull(ref coalition_data);

				ref var transform = ref rpc.record.GetTransform();
				Assert.IsNotNull(ref transform);

				var region_id = rpc.GetRegionID();

				// TODO: currently assuming the coalition region entity is a actually a zeppelin
				var ent_zeppelin = this.h_coalition.GetRegionEntity(region_id);
				var ent_dock = rpc.entity;

				var pos_target = transform.position;
				var pos_spawn = pos_target.WithY(-80);

				this.h_coalition.GetOrSpawn(region_id).ContinueWith(ent_zeppelin =>
				{
					ref var zeppelin = ref ent_zeppelin.GetComponent<Zeppelin.Data>();
					if (zeppelin.IsNotNull())
					{
						ref var transform = ref ent_zeppelin.GetTransform();
						if (transform.IsNotNull())
						{
							if (transform.position.IsZero())
							{
								transform.SetPosition(pos_spawn);
								transform.Modified(ent_zeppelin, sync: true);
							}
						}

						zeppelin.pos_move = pos_target.WithY(-zeppelin.unused_00);
						zeppelin.pos_aim = pos_target;
						zeppelin.ent_target_dock = ent_dock;

						zeppelin.Sync(ent_zeppelin);
					}
				});


			}
#endif
		}

		public struct EditRPC: Net.IRPC<Depot.Data>
		{

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Depot.Data data)
			{
				var sync = false;

				if (sync)
				{
					rpc.Sync(ref data, true);
				}
			}
#endif
		}

		public struct DEV_SetCatalogueRPC: Net.IRPC<Depot.Data>
		{
			public ICatalogue.Handle h_catalogue;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Depot.Data data)
			{
				Assert.IsDevMode();
				Assert.IsAdmin(ref rpc.connection);

				var sync = false;

				ref var catalogue_data = ref this.h_catalogue.GetData();
				if (catalogue_data.IsNotNull())
				{
					ref var stockpile = ref rpc.GetComponent<Stockpile.Data>();
					if (stockpile.IsNotNull())
					{
						ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
						if (stockpile_data.IsNotNull())
						{
							var span_items_stockpile = stockpile_data.items.AsSpan();
							var span_items_catalogue = catalogue_data.items.AsSpan();

							span_items_stockpile.Clear();

							var count = Maths.Min(span_items_stockpile.Length, span_items_catalogue.Length);
							for (var i = 0; i < count; i++)
							{
								ref var item_stockpile = ref span_items_stockpile[i];
								ref var item_catalogue = ref span_items_catalogue[i];

								item_stockpile = item_catalogue;
								item_stockpile.max = item_catalogue.quantity; // Maths.Max(item_catalogue.quantity, item_stockpile.material.GetQuantityFromMass(100.00f).SnapCeil(25));

								//item_stockpile.quantity = 0.00f;
								item_stockpile.quantity = Maths.Clamp((item_stockpile.max * data.stock_baseline_ratio).SnapCeil(5), 0.00f, item_stockpile.max);
							}

							//stockpile_data.items

							stockpile_asset.Sync();
						}
					}
				}

				if (sync)
				{
					rpc.Sync(ref data, true);
				}
			}
#endif
		}

		public struct DEV_TradeRPC: Net.IRPC<Depot.Data>
		{
			//public Inventory.Slot inv_slot;
			public int stockpile_slot_index;
			public float amount;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Depot.Data data)
			{
				var amount_abs = this.amount.Abs();
				Assert.Check(amount_abs >= 1);

				var sync = false;

				ref var stockpile = ref rpc.GetComponent<Stockpile.Data>();
				if (stockpile.IsNotNull())
				{
					ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
					if (stockpile_data.IsNotNull())
					{
						var span_items_stockpile = stockpile_data.items.AsSpan();

						ref var selected_item = ref span_items_stockpile.GetRefAtIndexOrNull(this.stockpile_slot_index);
						Assert.IsNotNull(ref selected_item);

						//var amount_abs_clamped = Maths.Min(selected_item.max);

						var unit_market_price = selected_item.GetUnitMarketPrice();
						Assert.Check(unit_market_price > 0.00f);

						var unit_market_price_sell = data.GetUnitSellPrice(in selected_item);
						var unit_market_price_buy = data.GetUnitBuyPrice(in selected_item);

						//var market_price = amount_abs * market_price_base;

						Crafting.Context.NewFromCharacter(region: ref rpc.GetRegionCommon(),
							h_character: rpc.GetSenderCharacterHandle(),
							ent_producer: rpc.entity,
							context: out var context,
							search_radius: 12.00f);

						if (this.amount.IsPositive()) // character buying from shop
						{
							var amount_abs_clamped = Maths.Min(amount_abs, Maths.Clamp(selected_item.quantity, 0, selected_item.max));

							Span<Crafting.Requirement> reqs =
							[
								Crafting.Requirement.Money(unit_market_price_buy) with
								{
									snapping = 1.00f,
									amount_min = 0.00f,
									amount_max = 0.00f,
									flags = Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite
								}
							];

							Span<Crafting.Product> prds =
							[
								selected_item.ToProduct() with
								{
									amount = 1.00f,
									amount_extra = 0.00f,
									flags = Crafting.Product.Flags.Primary
								}
							];

							Assert.Check(context.Evaluate(requirements: reqs, evaluation_flags: Crafting.EvaluateFlags.Prerequisite, amount_multiplier: amount_abs_clamped));

							context.Consume(requirements: reqs, amount_multiplier: amount_abs_clamped, evaluation_flags: Crafting.EvaluateFlags.Prerequisite);
							context.Produce(products: prds, amount_multiplier: amount_abs_clamped);

							selected_item.quantity -= amount_abs_clamped;
							sync = true;
						}
						else // character selling to shop
						{
							var amount_abs_clamped = Maths.Min(amount_abs, selected_item.max - Maths.Clamp(selected_item.quantity, 0, selected_item.max));

							Span<Crafting.Requirement> reqs =
							[
								selected_item.ToRequirement() with
								{
									amount = 1.00f,
									amount_min = 0.00f,
									amount_max = 0.00f,
									flags = Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite
								}
							];

							Span<Crafting.Product> prds =
							[
								Crafting.Product.Money(unit_market_price_sell) with
								{
									snapping = 1.00f,
									amount_extra = 0.00f,
									flags = Crafting.Product.Flags.Primary
								}
							];

							App.WriteValue((amount, amount_abs, amount_abs_clamped, unit_market_price_sell, amount_abs_clamped * unit_market_price_sell));

							Assert.Check(context.Evaluate(requirements: reqs, evaluation_flags: Crafting.EvaluateFlags.Prerequisite, amount_multiplier: amount_abs_clamped));

							context.Consume(requirements: reqs, amount_multiplier: amount_abs_clamped, evaluation_flags: Crafting.EvaluateFlags.Prerequisite);
							context.Produce(products: prds, amount_multiplier: amount_abs_clamped);

							selected_item.quantity += amount_abs_clamped;
							sync = true;
						}

						if (sync)
						{
							Sound.Play(ref rpc.GetRegionCommon(), sound: Shop.snd_buy, world_position: rpc.record.GetPosition(), dist_multiplier: 0.60f, priority: 0.20f);
							stockpile_asset.Sync();
						}
					}
				}


				if (sync)
				{
					rpc.Sync(ref data, true);
				}
			}
#endif
		}

		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity ent_depot,
		[Source.Owned] ref Depot.Data depot,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Company.Data company)
		{

			//#if SERVER
			//			ent_depot.TryGetInventory(Inventory.Type.Output, out var h_inventory)
			//#endif

		}

#if CLIENT
		public struct DepotGUI: IGUICommand
		{
			public Entity ent_depot;

			public Depot.Data depot;
			public Transform.Data transform;
			public Stockpile.Data stockpile;
			public Entrance.Linkable.Data entrance_linkable;

			public IFaction.Handle h_faction;
			public ICompany.Handle h_company;

			[Region.Local] public static IRecipe.Handle h_selected_recipe_cached;
			[Region.Local] public static Shipment.Item2.Header selected_item_header_cached;
			[Region.Local] public static int? selected_stockpile_item_slot_cached;
			[Region.Local] public static int selected_stockpile_item_amount_cached;

			[Region.Local] public static int selected_tab_index_cached;
			[Region.Local] public static ICoalition.Handle h_selected_coalition_cached;
			[Region.Local] public static Vector2 edit_picker_airstrike;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction(identifier: "Coalition Depot"u8, entity: this.ent_depot,
				tooltip_tab: "TODO: Desc"))
				{
					this.StoreCurrentWindowTypeID(order: -1000);
					if (window.show)
					{
						ref var region_common = ref this.ent_depot.GetRegionCommon();
						var h_character_client = Client.GetCharacterHandle();

						var h_stockpile = this.stockpile.h_stockpile;
						ref var stockpile_data = ref h_stockpile.GetData();

						var h_coalition = this.depot.h_coalition;
						ref var coalition_data = ref h_coalition.GetData();

						Crafting.Context.NewFromCurrentCharacter(this.ent_depot, out var context, search_radius: 12.00f);

						using (var group_left = GUI.Group.New(size: new(298 - 48, GUI.RmY), padding: new(6)))
						{
							group_left.DrawBackground(GUI.tex_window);

							using (var collapsible = GUI.Collapsible2.New("col.coalition"u8, size: new(GUI.RmX, 32), default_open: false))
							{
								GUI.TitleCentered("Coalitions"u8, size: 24, pivot: new(0.00f, 0.50f));

								if (collapsible.Inner())
								{
									using (var group_col_inner = GUI.Group.New(size: new(GUI.RmX, 0)))
									{
										var coalitions_span = ICoalition.Database.GetAssetsSpan();
										for (var i = 0; i < coalitions_span.Length; i++)
										{
											var d_coalition_tmp = coalitions_span[i];
											ref var coalition_tmp_data = ref d_coalition_tmp.GetData();
											if (coalition_tmp_data.IsNotNull())
											{
												using (var hash = GUI.ID<Depot.Data, ICoalition.Data>.Push(d_coalition_tmp.GetHandle()))
												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(6)))
												using (GUI.Wrap.Push(GUI.RmX))
												{
													//group_row.DrawBackground(GUI.tex_slot_white, color: coalition_tmp_data.color_gui);
													group_row.DrawBackground(GUI.tex_window_popup_embed, color: coalition_tmp_data.color_gui.WithColorMult(0.75f));

													GUI.TitleCentered(coalition_tmp_data.GetShortName(), font: GUI.Font.Superstar, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0));

													var is_selected = h_selected_coalition_cached == d_coalition_tmp;
													if (GUI.Selectable3(id: hash, rect: group_row.GetInnerRect(), selected: is_selected))
													{
														h_selected_coalition_cached.Toggle(d_coalition_tmp);
													}
												}
												if (GUI.IsItemHovered())
												{
													using (var tooltip = GUI.Tooltip.New(size: new(224, 0)))
													using (GUI.Wrap.Push(GUI.RmX))
													{
														GUI.TextShaded(coalition_tmp_data.GetDescription());
													}
												}
												GUI.FocusableAsset(d_coalition_tmp);
												
												//GUI.DrawHoverTooltip()
											}
										}

										//if (coalition_data.IsNotNull())
										//{
										//	GUI.NewLine(4);
										//	GUI.Title(coalition_data.GetName(), size: 20);
										//	GUI.FocusableAsset(h_coalition);
										//	GUI.NewLine(4);
										//	GUI.TextShaded(coalition_data.GetDescription(), color: GUI.font_color_desc);
										//	GUI.NewLine(4);

										//	GUI.SeparatorThick();
										
										//	GUI.NewLine(4);

										//	GUI.Title("- TODO -"u8, size: 20);
										//}
									}
								}
							}

							using (var collapsible = GUI.Collapsible2.New("col.services"u8, size: new(GUI.RmX, 32), default_open: false))
							{
								GUI.TitleCentered("Services"u8, size: 24, pivot: new(0.00f, 0.50f));

								if (collapsible.Inner())
								{
									using (var group_col_inner = GUI.Group.New(size: new(GUI.RmX, 0)))
									{
										GUI.Title("- TODO -"u8, size: 20);
									}
								}
							}

							using (var collapsible = GUI.Collapsible2.New("col.production"u8, size: new(GUI.RmX, 32), default_open: false))
							{
								GUI.TitleCentered("Production"u8, size: 24, pivot: new(0.00f, 0.50f));

								if (collapsible.Inner())
								{
									using (var group_col_inner = GUI.Group.New(size: new(GUI.RmX, 0)))
									{
										GUI.Title("- TODO -"u8, size: 20);
									}
								}
							}
						}

						GUI.SameLine();

						const float item_cell_width = 56.00f;

						using (var group_right = GUI.Group.New(size: GUI.Rm))
						{
							var ts = Timestamp.Now();
							var ts_elapsed = 0.00;



							using (var group_tabs = GUI.Group.New(size: new(GUI.RmX, 40)))
							{
								GUI.DrawTab3(text: "Overview"u8, size: new(0, GUI.RmY),
									index: 0, selected_index: ref selected_tab_index_cached, inner: true);

								GUI.SameLine();

								GUI.DrawTab3(text: "Market"u8, size: new(0, GUI.RmY), 
									index: 1, selected_index: ref selected_tab_index_cached, inner: true);

								GUI.SameLine();

								GUI.DrawTab3(text: "Zeppelin"u8, size: new(0, GUI.RmY),
									index: 2, selected_index: ref selected_tab_index_cached, inner: true);
							}

							GUI.SeparatorThick();
							switch (selected_tab_index_cached)
							{
								case 0:
								{

								}
								break;

								case 1:
								{
									var items_span = stockpile_data.items.AsSpan();
									if (stockpile_data.IsNotNull())
									{
										var amount_multiplier_abs = selected_stockpile_item_amount_cached.Abs();
										var amount_multiplier_abs_clamped = amount_multiplier_abs;

										//new IStockpile.SlotID(0, Stockpile.SlotType.Item)

										using (var group_top = GUI.Group.New(size: GUI.Rm.SubY(48)))
										{
											using (var group_title = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(6)))
											{
												GUI.TitleCentered(this.depot.h_catalogue.GetName(), pivot: new(0.00f, 0.50f), font: GUI.Font.Editia, size: 20);

												GUI.FocusableAsset(h_stockpile);
											}

											GUI.SeparatorThick();

											//using (var group_items = GUI.Group.New(size: new(GUI.RmX, 0)))
											using (var group_items = GUI.Scrollbox.New("sb.depot.trade", size: GUI.Rm.SubY(128)))
											{
												var sameline = false;

												//if (GUI.GetMouse().GetKeyDown(Mouse.Key.Forward))
												//{
												//	selected_stockpile_item_slot_cached++;
												//}

												for (var i = 0; i < items_span.Length; i++)
												{
													ref var item = ref items_span[i];
													//if (!item.IsValid()) continue;
													//if (item.GetHeader().id == 0) continue;

													if (sameline) GUI.TrySameLine(item_cell_width);

													using (var hash = GUI.ID<Depot.Data, Shipment.Item>.Push(i))
													using (var group_item = GUI.Group.New(size: new(item_cell_width, item_cell_width + 12), padding: new(4)))
													{
														//group_item.DrawBackground(GUI.tex_slot_white, color: GUI.col_frame);
														//group_item.DrawBackground(GUI.tex_window_sidebar_c);
														group_item.DrawBackground(GUI.tex_panel, inner: true);

														if (item.IsValid())
														{
															//GUI.DrawResourceSmall()
															GUI.DrawItem(item: ref item, size: new Vec2f(GUI.RmX).SubY(8), clip: false);
															var is_hovered = GUI.IsItemHovered();

															var is_selected = selected_stockpile_item_slot_cached == i;
															if (GUI.Selectable3(hash, rect: group_item.GetInnerRect(), selected: is_selected))
															{
																selected_stockpile_item_slot_cached.Toggle(i);
															}

															var unit_market_price_buy = this.depot.GetUnitBuyPrice(in item); // item.GetUnitMarketPrice();
															var unit_market_price_sell = this.depot.GetUnitSellPrice(in item); // item.GetUnitMarketPrice();

															if (false)
															{
																if (selected_stockpile_item_amount_cached > 0)
																{
																	GUI.TextShadedCenteredRect(unit_market_price_buy * amount_multiplier_abs, pivot: new(0.50f, 1.00f), rect: group_item.GetOuterRect(),
																		font: GUI.Font.Monaco, size: 11, box_shadow: true, offset: new(0, -10),
																		format: "0' Đk'", color: GUI.col_buy);
																}
																else if (selected_stockpile_item_amount_cached < 0)
																{
																	GUI.TextShadedCenteredRect(unit_market_price_sell * amount_multiplier_abs, pivot: new(0.50f, 1.00f), rect: group_item.GetOuterRect(),
																		font: GUI.Font.Monaco, size: 11, box_shadow: true, offset: new(0, -10),
																		format: "0' Đk'", color: GUI.col_sell);
																}
																else
																{
																	GUI.TextShadedCenteredRect(unit_market_price_buy, pivot: new(0.50f, 1.00f), rect: group_item.GetOuterRect(),
																		font: GUI.Font.Monaco, size: 11, box_shadow: true, offset: new(0, -10),
																		format: "0' Đk'", color: GUI.font_color_default);
																}
															}
															else
															{
																GUI.TextShadedCenteredRect(unit_market_price_buy * amount_multiplier_abs, pivot: new(0.00f, 1.00f), rect: group_item.GetOuterRect(),
																	font: GUI.Font.Monaco, size: 11, box_shadow: true, offset: new(6, -14),
																	format: "0' Đk'", color: GUI.col_buy);

																GUI.TextShadedCenteredRect(unit_market_price_sell * amount_multiplier_abs, pivot: new(0.00f, 1.00f), rect: group_item.GetOuterRect(),
																	font: GUI.Font.Monaco, size: 11, box_shadow: true, offset: new(6, -2),
																	format: "0' Đk'", color: GUI.col_sell);
															}

															if (is_hovered) //GUI.IsHoveringRect(item_rect) group_item.IsHovered())
															{
																using (var tooltip = GUI.Tooltip.New())
																{
																	Span<Crafting.Requirement> reqs_buy = stackalloc[]
																	{
																Crafting.Requirement.Money(unit_market_price_sell)
																.WithFlags(add: Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite)
																with
																{
																	snapping = 1.00f,
																}
															};

																	Span<Crafting.Requirement> reqs_sell = stackalloc[]
																	{
																item.ToRequirement() with
																{
																	amount = 1.00f,
																	flags =  Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite
																}
															};

																	//var amount_multiplier_abs = item.quantity.Abs();

																	GUI.DrawRequirements(context: ref context,
																		requirements: reqs_buy,
																		amount_multiplier: amount_multiplier_abs,
																		evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																		selectable: false,
																		highlight: true);

																	GUI.SeparatorThick();

																	GUI.DrawRequirements(context: ref context,
																		requirements: reqs_sell,
																		amount_multiplier: amount_multiplier_abs,
																		evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																		selectable: false,
																		highlight: true);
																}
															}
														}
													}

													sameline = true;
												}
											}

											GUI.SeparatorThick();

											using (var group_trade = GUI.Group.New(size: new(GUI.RmX, 48)))
											{
												ref var selected_item = ref items_span.GetRefAtIndexOrNull(selected_stockpile_item_slot_cached);

												//Crafting.Context.NewFromCurrentCharacter(this.ent_depot, out var context, search_radius: 12.00f);
												//group_trade.DrawBackground(GUI.tex_window);

												//var amount_multiplier_abs = selected_stockpile_item_amount_cached.Abs();
												//var amount_multiplier_abs_clamped = amount_multiplier_abs;

												var amount_multiplier_max = 0;
												var base_market_price = 0.00f;
												if (selected_item.IsNotNull())
												{
													amount_multiplier_max = (int)selected_item.quantity;
													base_market_price = selected_item.GetUnitMarketPrice();
												}

												amount_multiplier_abs_clamped = Maths.Min(amount_multiplier_abs, Maths.Max(1, amount_multiplier_max));

												Span<Crafting.Requirement> reqs_buy = stackalloc[]
												{
											Crafting.Requirement.Money(base_market_price)
											.WithFlags(add: Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite)
											with
											{
												snapping = 1.00f,
											}
										};

												Span<Crafting.Requirement> reqs_sell = stackalloc[]
												{
											selected_item.IsNotNull() ? selected_item.ToRequirement() with
											{
												amount = 1.00f,
												flags =  Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite
											} : default
										};

												using (var group_item_left = GUI.Group.New(size: new(GUI.RmX - 128 - 80, GUI.RmY), padding: new(6)))
												{
													group_item_left.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_frame);
													var rm_x = GUI.RmX - GUI.RmY - 16;

													using (GUI.ID<Depot.Data, int>.Push(1))
													{
														using (var group_item = GUI.Group.New(size: new(rm_x * 0.50f, GUI.RmY)))
														{
															if (selected_item.IsNotNull())
															{
																var amount_new = (int)GUI.DrawRequirements(context: ref context,
																	requirements: reqs_sell,
																	amount_multiplier: amount_multiplier_abs,
																	evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																	selectable: true).selected_value;

																if (amount_new != 0)
																{
																	selected_stockpile_item_amount_cached = amount_new;
																}
															}
														}
													}

													{
														GUI.SameLine(8);

														using (var group_item = GUI.Group.New(size: new(GUI.RmY)))
														{
															GUI.TextShadedCentered("FOR"u8, font: GUI.Font.Editia, size: 16, pivot: new(0.50f, 0.50f));
														}
													}

													using (GUI.ID<Depot.Data, int>.Push(2))
													{
														GUI.SameLine(8);

														using (var group_item = GUI.Group.New(size: new(rm_x * 0.50f, GUI.RmY)))
														{
															if (selected_item.IsNotNull())
															{
																var amount_new = (int)GUI.DrawRequirements(context: ref context,
																	requirements: reqs_buy,
																	amount_multiplier: amount_multiplier_abs,
																	evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																	selectable: true).selected_value;

																if (amount_new != 0)
																{
																	selected_stockpile_item_amount_cached = (amount_new / base_market_price).RoundToInt();
																}
															}
														}
													}
												}

												GUI.SameLine();

												//if (GUI.ScrollInput(rect: group_amount.GetInnerRect(), ref selected_stockpile_item_amount_cached, step: 1, min: 1, max: 10))
												if (GUI.DrawCounter("input"u8,
												value: ref selected_stockpile_item_amount_cached,
												size: new(80, GUI.RmY),
												step: 1,
												min: 1,
												max: 1000,
												//max: amount_multiplier_max,
												format: Maths.NumberFormat.Int))
												{

												}

												GUI.SameLine();

												{
													if (GUI.DrawRequirementButton(ref context, requirements: reqs_buy, text: "Buy"u8, size: new(64, GUI.RmY), color: GUI.col_buy,
													amount_multiplier: amount_multiplier_abs,
													eval_flags: Crafting.EvaluateFlags.Prerequisite,
													error: selected_item.IsNull() || amount_multiplier_abs == 0 || base_market_price <= 0.00f || amount_multiplier_max <= 0))
													{
														var rpc = new Depot.DEV_TradeRPC
														{
															stockpile_slot_index = selected_stockpile_item_slot_cached ?? -1,
															amount = amount_multiplier_abs
														};
														rpc.Send(this.ent_depot);
													}

													if (selected_item.IsNotNull() && GUI.IsItemHovered())
													{
														using (var tooltip = GUI.Tooltip.New())
														{
															GUI.SeparatorThick();
															GUI.NewLine(8);

															Span<Crafting.Product> prds =
															[
																selected_item.ToProduct() with
														{
															amount = 1.00f,
															amount_extra = 0.00f,
															flags = Crafting.Product.Flags.Primary
														}
															];

															GUI.DrawProducts(context: ref context,
																products: prds,
																evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																amount_multiplier: amount_multiplier_abs_clamped,
																selectable: false);
														}
													}
												}

												GUI.SameLine();

												{
													if (GUI.DrawRequirementButton(ref context, requirements: reqs_sell, text: "Sell"u8, size: new(64, GUI.RmY), color: GUI.col_sell,
													amount_multiplier: amount_multiplier_abs,
													eval_flags: Crafting.EvaluateFlags.Prerequisite,
													error: selected_item.IsNull() || amount_multiplier_abs == 0 || base_market_price <= 0.00f || selected_item.quantity >= selected_item.max))
													{
														var rpc = new Depot.DEV_TradeRPC
														{
															stockpile_slot_index = selected_stockpile_item_slot_cached ?? -1,
															amount = -amount_multiplier_abs
														};
														rpc.Send(this.ent_depot);
													}

													if (selected_item.IsNotNull() && GUI.IsItemHovered())
													{
														using (var tooltip = GUI.Tooltip.New())
														{
															GUI.SeparatorThick();
															GUI.NewLine(8);

															var unit_market_price_sell = this.depot.GetUnitSellPrice(in selected_item);
															Span<Crafting.Product> prds =
															[
																Crafting.Product.Money(unit_market_price_sell) with
														{
															snapping = 1.00f,
															amount_extra = 0.00f,
															flags = Crafting.Product.Flags.Primary
														}
															];

															GUI.DrawProducts(context: ref context,
																products: prds,
																evaluation_flags: Crafting.EvaluateFlags.Prerequisite,
																amount_multiplier: amount_multiplier_abs_clamped,
																selectable: false);
														}
													}
												}
											}
										}
									}

									using (var group = GUI.Group.New(size: GUI.Rm))
									{
										if (Client.HasDebugAuthority())
										{
											if (GUI.DrawButton("DEV: Load Catalogue"u8, size: new(168, GUI.RmY), color: GUI.col_button_debug))
											{
												var rpc = new Depot.DEV_SetCatalogueRPC
												{
													h_catalogue = this.depot.h_catalogue
												};
												rpc.Send(this.ent_depot);
											}
											//GUI.TextShaded("TODO"u8);

											ts_elapsed = ts.GetMilliseconds();

											GUI.SameLine();

											GUI.TextShaded($"{ts_elapsed:0.000} ms");
										}
									}
								}
								break;

								case 2:
								{
									using (var group = GUI.Group.New(size: GUI.Rm))
									{
										var ent_zeppelin = h_selected_coalition_cached.GetRegionEntity(region_common.GetID());

										if (GUI.DrawButton("Summon"u8, size: new(80, 40)))
										{
											var rpc = new Depot.DEV_SummonZeppelinRPC
											{
												h_coalition = h_selected_coalition_cached
											};
											rpc.Send(this.ent_depot);
										}

										GUI.SameLine();

										if (GUI.DrawButton("Dock"u8, size: new(80, 40)))
										{
											var rpc = new Zeppelin.DEV_DockRPC
											{
												ent_dock = this.ent_depot,
												//pos_target = transform.position
											};
											rpc.Send(ent_zeppelin);
										}

										GUI.SameLine();

										//if (GUI.Checkbox("Skyhook"u8, size: new(80, 40)))
										if (GUI.DrawButton("Skyhook"u8, size: new(80, 40)))
										{
											var rpc = new Zeppelin.DEV_SendRequestRPC
											{
												flags = Zeppelin.Flags.Skyhook_Deployed
											};
											rpc.Send(ent_zeppelin);
										}

										GUI.SameLine();

										//if (GUI.Checkbox("Skyhook"u8, size: new(80, 40)))
										if (GUI.DrawButton("Reset"u8, size: new(80, 40)))
										{
											var rpc = new Zeppelin.DEV_SendRequestRPC
											{
												flags = Zeppelin.Flags.None
											};
											rpc.Send(ent_zeppelin);
										}

										GUI.SameLine();

										//if (GUI.Checkbox("Skyhook"u8, size: new(80, 40)))
										if (GUI.Picker("air_strike"u8, "Air Strike"u8, size: new(40, 40), ref edit_picker_airstrike, new Vector2(-4000), new Vector2(4000), sensitivity: 1.00f, absolute: true))
										{
											var rpc = new Zeppelin.DEV_SendRequestRPC
											{
												flags = Zeppelin.Flags.Airstrike_Pending,
												pos_target = edit_picker_airstrike
											};
											rpc.Send(ent_zeppelin);
										}
									}
								}
								break;
							}

							//GUI.TextShaded($"{total_inventories} inventories in {ts_elapsed:0.000} ms");
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI([Source.Owned] in Interactable.Data interactable, Entity ent_depot,
		[Source.Owned] in Depot.Data depot, [Source.Owned] in Transform.Data transform,
		[Source.Owned] in Stockpile.Data stockpile,
		[Source.Owned] in Entrance.Linkable.Data entrance_linkable,
		[Source.Owned, Optional] in Faction.Data faction,
		[Source.Owned, Optional] in Company.Data company)
		{
			if (interactable.IsActive())
			{
				var gui = new DepotGUI()
				{
					ent_depot = ent_depot,

					depot = depot,
					transform = transform,
					stockpile = stockpile,
					entrance_linkable = entrance_linkable,

					h_faction = faction.id,
					h_company = company.h_company,
				};
				gui.Submit();
			}
		}
#endif
	}
}
