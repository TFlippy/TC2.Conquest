namespace TC2.Base.Components
{
	public static partial class Factory
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			public required Factory.Data.Flags flags;
			public ICatalogue.Handle h_catalogue;

			public float stock_baseline_ratio = 0.80f;
			public float stock_initial_ratio = 0.40f;
			//public float stock_fill_ratio_baseline = 0.20f;
			public uint unused_03;
		}

		public struct EditRPC: Net.IRPC<Factory.Data>
		{

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Factory.Data data)
			{
				var sync = false;

				if (sync)
				{
					rpc.Sync(ref data, true);
				}
			}
#endif
		}

		public struct DEV_SetCatalogueRPC: Net.IRPC<Factory.Data>
		{
			public ICatalogue.Handle h_catalogue;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Factory.Data data)
			{
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

		public struct DEV_TradeRPC: Net.IRPC<Factory.Data>
		{
			//public Inventory.Slot inv_slot;
			public int stockpile_slot_index;
			public float amount;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Factory.Data data)
			{
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

						var amount_abs = this.amount.Abs();
						//var amount_abs_clamped = Maths.Min(selected_item.max);

						var market_price_base = selected_item.material.GetMarketPrice();
						Assert.Check(market_price_base > 0.00f);

						var market_price = amount_abs * market_price_base;

						Crafting.Context.NewFromCharacter(region: ref rpc.GetRegionCommon(), 
							h_character: rpc.GetSenderCharacterHandle(), 
							ent_producer: rpc.entity, 
							context: out var context, 
							search_radius: 8.00f);

						if (this.amount < 0.00f)
						{
							var amount_abs_clamped = Maths.Min(amount_abs, Maths.Clamp(selected_item.quantity, 0, selected_item.max));

							Span<Crafting.Requirement> reqs =
							[
								Crafting.Requirement.Money(market_price_base) with
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
						else
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
								Crafting.Product.Money(market_price_base) with
								{
									snapping = 1.00f,
									amount_extra = 0.00f,
									flags = Crafting.Product.Flags.Primary
								}
							];

							App.WriteValue((amount, amount_abs, amount_abs_clamped, market_price_base, amount_abs_clamped * market_price_base));

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

						//Crafting.Context.NewFromConnection(ref rpc.connection, ent_producer: rpc.entity, out var context, search_radius: 8.00f);
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
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity ent_factory,
		[Source.Owned] ref Factory.Data factory,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Company.Data company)
		{

			//#if SERVER
			//			ent_factory.TryGetInventory(Inventory.Type.Output, out var h_inventory)
			//#endif

		}

#if CLIENT
		public struct FactoryGUI: IGUICommand
		{
			public Entity ent_factory;

			public Factory.Data factory;
			public Transform.Data transform;
			public Stockpile.Data stockpile;
			public Entrance.Linkable.Data entrance_linkable;

			public IFaction.Handle h_faction;
			public ICompany.Handle h_company;


			public static IRecipe.Handle h_selected_recipe_cached;
			public static Shipment.Item2.Header selected_item_header_cached;
			public static int? selected_stockpile_item_slot_cached;
			public static int selected_stockpile_item_amount_cached;


			public void Draw()
			{
				using (var window = GUI.Window.Interaction(identifier: "Factory"u8, entity: this.ent_factory,
				tooltip_tab: "You can control the means of production here."))
				{
					this.StoreCurrentWindowTypeID(order: -1000);
					if (window.show)
					{
						ref var region_common = ref this.ent_factory.GetRegionCommon();
						var h_character_client = Client.GetCharacterHandle();

						var h_stockpile = this.stockpile.h_stockpile;
						ref var stockpile_data = ref h_stockpile.GetData();

						using (var group_left = GUI.Group.New(size: new(298 - 48, GUI.RmY), padding: new(6)))
						{
							group_left.DrawBackground(GUI.tex_window);

							using (var collapsible = GUI.Collapsible2.New("col.production"u8, size: new(GUI.RmX, 32), default_open: true))
							{
								GUI.TitleCentered("Production"u8, size: 24, pivot: new(0.00f, 0.50f));

								if (collapsible.Inner())
								{
									using (var group_col_inner = GUI.Group.New(size: new(GUI.RmX, 0)))
									{

									}
								}
							}
						}

						GUI.SameLine();

						var ts = Timestamp.Default;
						var ts_elapsed = 0.00;

						const float item_cell_width = 56.00f;

						using (var group_right = GUI.Group.New(size: GUI.Rm))
						{
							var items_span = stockpile_data.items.AsSpan();
							if (stockpile_data.IsNotNull())
							{
								using (var group_top = GUI.Group.New(size: GUI.Rm.SubY(48)))
								{
									using (var group_title = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(6)))
									{
										GUI.TitleCentered(this.factory.h_catalogue.GetName(), pivot: new(0.00f, 0.50f), font: GUI.Font.Editia, size: 20);

										GUI.FocusableAsset(h_stockpile);
									}

									GUI.SeparatorThick();

									using (var group_items = GUI.Group.New(size: new(GUI.RmX, 0)))
									{

										var sameline = false;

										for (var i = 0; i < items_span.Length; i++)
										{
											ref var item = ref items_span[i];
											//if (!item.IsValid()) continue;
											//if (item.GetHeader().id == 0) continue;

											if (sameline) GUI.TrySameLine(item_cell_width);

											using (var hash = GUI.ID<Factory.Data, Shipment.Item>.Push(i))
											using (var group_item = GUI.Group.New(size: new(item_cell_width), padding: new(4)))
											{
												group_item.DrawBackground(GUI.tex_slot_white, color: GUI.col_frame);

												//GUI.DrawResourceSmall()
												GUI.DrawItem(ref item, size: GUI.Rm);

												var is_selected = selected_stockpile_item_slot_cached == i;
												if (GUI.Selectable3(hash, rect: group_item.GetInnerRect(), selected: is_selected))
												{
													selected_stockpile_item_slot_cached.Toggle(i);
												}
											}

											sameline = true;
										}

									}

									GUI.SeparatorThick();

									using (var group_trade = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										ref var selected_item = ref items_span.GetRefAtIndexOrNull(selected_stockpile_item_slot_cached);

										Crafting.Context.NewFromCurrentCharacter(this.ent_factory, out var context, search_radius: 8.00f);
										//group_trade.DrawBackground(GUI.tex_window);

										var amount_multiplier_abs = selected_stockpile_item_amount_cached.Abs();
										var amount_multiplier_abs_clamped = amount_multiplier_abs;

										var amount_multiplier_max = 0;
										var base_market_price = 0.00f;
										if (selected_item.IsNotNull())
										{
											amount_multiplier_max = (int)selected_item.quantity;
											base_market_price = selected_item.material.GetMarketPrice();
										}

										amount_multiplier_abs_clamped = Maths.Min(amount_multiplier_abs, Maths.Max(1, amount_multiplier_max));

										Span<Crafting.Requirement> reqs_buy =
										[
											Crafting.Requirement.Money(base_market_price)
											.WithFlags(add: Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite)
											with
											{
												snapping = 1.00f,
											}
										];

										Span<Crafting.Requirement> reqs_sell =
										[
											selected_item.IsNotNull() ? selected_item.ToRequirement() with
											{
												amount = 1.00f,
												flags =  Crafting.Requirement.Flags.Primary | Crafting.Requirement.Flags.Argument | Crafting.Requirement.Flags.Prerequisite
											} : default
										];

										using (var group_item_left = GUI.Group.New(size: new(GUI.RmX - 128 - 80, GUI.RmY), padding: new(6)))
										{
											group_item_left.DrawBackground(GUI.tex_window_popup_l, color: GUI.col_frame);
											var rm_x = GUI.RmX - GUI.RmY - 16;

											using (GUI.ID<Factory.Data, int>.Push(1))
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

											using (GUI.ID<Factory.Data, int>.Push(2))
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
										//max: amount_multiplier_max,
										format: Maths.NumberFormat.Int))
										{

										}

										GUI.SameLine();

										if (GUI.DrawRequirementButton(ref context, requirements: reqs_buy, text: "Buy"u8, size: new(64, GUI.RmY), color: GUI.col_buy,
										amount_multiplier: amount_multiplier_abs,
										eval_flags: Crafting.EvaluateFlags.Prerequisite, error: selected_item.IsNull() || amount_multiplier_max <= 0))
										{
											var rpc = new Factory.DEV_TradeRPC
											{
												stockpile_slot_index = selected_stockpile_item_slot_cached ?? -1,
												amount = -amount_multiplier_abs
											};
											rpc.Send(this.ent_factory);
										}

										GUI.SameLine();

										if (GUI.DrawRequirementButton(ref context, requirements: reqs_sell, text: "Sell"u8, size: new(64, GUI.RmY), color: GUI.col_sell,
										amount_multiplier: amount_multiplier_abs, 
										eval_flags: Crafting.EvaluateFlags.Prerequisite, error: selected_item.IsNull() || (selected_item.quantity + amount_multiplier_abs) > selected_item.max))
										{
											var rpc = new Factory.DEV_TradeRPC
											{
												stockpile_slot_index = selected_stockpile_item_slot_cached ?? -1,
												amount = amount_multiplier_abs
											};
											rpc.Send(this.ent_factory);
										}
									}

								}
							}

							GUI.SeparatorThick();

							using (var group = GUI.Group.New(size: GUI.Rm))
							{
								if (GUI.DrawButton("DEV: Load Catalogue"u8, size: new(168, GUI.RmY), color: GUI.col_button_debug))
								{
									var rpc = new Factory.DEV_SetCatalogueRPC
									{
										h_catalogue = this.factory.h_catalogue
									};
									rpc.Send(this.ent_factory);
								}
								//GUI.TextShaded("TODO"u8);

							}

							//GUI.TextShaded($"{total_inventories} inventories in {ts_elapsed:0.000} ms");
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI([Source.Owned] in Interactable.Data interactable, Entity ent_factory,
		[Source.Owned] in Factory.Data factory, [Source.Owned] in Transform.Data transform,
		[Source.Owned] in Stockpile.Data stockpile,
		[Source.Owned] in Entrance.Linkable.Data entrance_linkable,
		[Source.Owned, Optional] in Faction.Data faction,
		[Source.Owned, Optional] in Company.Data company)
		{
			if (interactable.IsActive())
			{
				var gui = new FactoryGUI()
				{
					ent_factory = ent_factory,

					factory = factory,
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
