using System.Diagnostics.CodeAnalysis;
using TC2.Conquest;

namespace TC2.Base.Components
{
	public static partial class Checkpoint
	{
		public struct DEV_CreatePassportRPC: Net.IRPC<Checkpoint.Data>
		{
			public required ICharacter.Handle h_character;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Checkpoint.Data data)
			{
				Assert.IsDevMode();

				ref var character_data = ref this.h_character.GetData(out var character_asset);
				ref var player_data = ref rpc.GetSenderPlayer(out var player_asset);

				ref var region_common = ref rpc.GetRegionCommon();
				ref var region_global = ref rpc.GetRegionGlobal();

				ref var g_world = ref region_global.GetGlobalComponent<World.Global>();

				var passport_asset = IPassport.Database.Register(character_asset.identifier, index: null, scope: Asset.Scope.World, region_id: 0, flags: Asset.Flags.Recycle);
				ref var passport_data = ref passport_asset.GetData(out var h_passport);

				var date_current = g_world.date;

				passport_data.name = character_data.name;
				passport_data.h_faction = character_data.faction;
				passport_data.date_registration = date_current;
				//passport_data.h_location_residence = date_current;


			}
#endif
		}

		[Save.Inline]
		public struct Transit
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,

			}

			public Checkpoint.Transit.Flags flags;
		}

		[Flags]
		public enum Flags: uint
		{
			None = 0,

			Manual_Entry = 1u << 0,
			Manual_Exit = 1u << 1,
		}

		[Flags]
		public enum Policies: ulong
		{
			None = 0ul,
		}

		public enum Action: byte
		{
			Undefined = 0,

			Approve,
			Deny,
			Detain,
			Dismiss,
			Exit
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public struct Data(): IComponent //, ICharacterStorage
		{
			public Checkpoint.Flags flags;

			//[Editor.Slider.Clamped(0.00f, 16.00f)]
			//public Vec4f test;

			public FixedArray8<Checkpoint.Transit> transit;
			//public FixedArray8<ICharacter.Handle> characters;

			//[UnscopedRef]
			//Span<ICharacter.Handle> ICharacterStorage.GetCharacterSpan() => this.GetCharacterSpan();
		}

		//public static Span<ICharacter.Handle> GetCharacterSpan(ref this Checkpoint.Data checkpoint)
		//{
		//	return checkpoint.characters.AsSpan();
		//}

		//		[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnAddRemCharacter(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity, Entity ent_entrance, Entity ent_character,
		//		[Source.SharedAny] ref Entrance.Data entrance, [Source.Owned] ref Transform.Data transform, [Source.Stored] Character.Data character)
		//		{
		//			var h_character = ent_character.GetAssetHandle<ICharacter.Handle>();
		//			App.WriteLine($"OnAddRemCharacter {entity}; {ent_entrance}; {ent_character}");

		//			if (h_character.IsValid())
		//			{
		//#if SERVER
		//				switch (info.EventType)
		//				{
		//					case ISystem.EventType.Add:
		//					{
		//						entrance.characters.AsSpan().AddUnique(h_character);
		//					}
		//					break;

		//					case ISystem.EventType.Remove:
		//					{
		//						entrance.characters.AsSpan().TryRemove(h_character);
		//					}
		//					break;
		//				}

		//				entrance.Sync(ent_entrance);
		//#endif
		//			}
		//		}

		//		[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnAddRemCharacter(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity, Entity ent_entrance, Entity ent_character,
		//		[Source.Stored] ref Entrance.Data entrance, [Source.Owned] ref Character.Data character)
		//		{
		//			var h_character = ent_character.GetAssetHandle<ICharacter.Handle>();
		//			App.WriteLine($"OnAddRemCharacter {entity}; {ent_entrance}; {ent_character}");

		//			if (h_character.IsValid())
		//			{
		//#if SERVER
		//				switch (info.EventType)
		//				{
		//					case ISystem.EventType.Add:
		//					{
		//						entrance.characters.AsSpan().AddUnique(h_character);
		//					}
		//					break;

		//					case ISystem.EventType.Remove:
		//					{
		//						entrance.characters.AsSpan().TryRemove(h_character);
		//					}
		//					break;
		//				}

		//				entrance.Sync(ent_entrance);
		//#endif
		//			}
		//		}

		[ISystem.Event<Entrance.EnterEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnEnterEvent(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity entity, Entity ent_entrance,
		[Source.Owned] ref Entrance.EnterEvent ev, [Source.Shared] ref Entrance.Data entrance, [Source.Shared] ref Transform.Data transform)
		{
			App.WriteLine($"OnEnterEvent {entity}; {ent_entrance}", App.Color.Magenta);

#if SERVER
			if (ev.flags.HasNone(Entrance.EnterEvent.Flags.Spawn) && entrance.flags.HasNone(Entrance.Flags.Force_Spawn)) // && (dormitory.GetReservedCharacterSpan().TryAdd(data.h_character) || dormitory.GetCharacterSpan().TryAdd(data.h_character)))
			{
				ref var character_data = ref ev.h_character.GetData(out var character_asset);
				if (character_data.IsNotNull())
				{
					ev.h_character.Spawn(region.GetID()).ContinueWith((ent_character) =>
					{
						ent_character.AddRelation(entity, Relation.Type.Stored, check_recursion: false);
						return ent_character;
					});
				}
				//dormitory.Sync(ent_dormitory);
			}
			else
			{
				ref var character_data = ref ev.h_character.GetData(out var character_asset);
				if (character_data.IsNotNull())
				{
					Spawner.SpawnCharacter(ref region, h_character: ev.h_character, position: transform.position, h_faction: ev.h_faction, h_player: ev.h_player, flags: Spawn.SpawnEvent.Flags.Control).ContinueWith((ent) =>
					{
						Loadout.Spawn(ent, character_asset.data.kits, money: character_asset.data.money);
						character_asset.data.kits = null;
						character_asset.data.money = 0.00f;
						character_asset.Sync();

						App.WriteLine("spawned");
						return ent;
					});
				}
			}
#endif
		}

		//		[ISystem.Event<Entrance.EnterEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnEnterEvent(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity entity, Entity ent_dormitory, Entity ent_entrance,
		//		[Source.Shared] ref Entrance.EnterEvent data, [Source.SharedAny] ref Entrance.Data entrance, [Source.Owned, Optional(true)] ref Dormitory.Data dormitory, [Source.Owned] ref Transform.Data transform)
		//		{
		//			App.WriteLine($"OnEnterEvent {entity}; {ent_entrance}; {ent_dormitory}", App.Color.Magenta);

		//#if SERVER
		//			if (dormitory.IsNotNull() && data.flags.HasNone(Entrance.EnterEvent.Flags.Spawn) && entrance.flags.HasNone(Entrance.Flags.Force_Spawn) && (dormitory.GetReservedCharacterSpan().TryAdd(data.h_character) || dormitory.GetCharacterSpan().TryAdd(data.h_character)))
		//			{
		//				dormitory.Sync(ent_dormitory);
		//			}
		//			else
		//			{
		//				ref var character_data = ref data.h_character.GetData(out var character_asset);
		//				if (character_data.IsNotNull())
		//				{
		//					Spawner.SpawnCharacter(ref region, h_character: data.h_character, position: transform.position, h_faction: data.h_faction, h_player: data.h_player, control: true).ContinueWith((ent) =>
		//					{
		//						Loadout.Spawn(ent, character_asset.data.kits, money: character_asset.data.money);
		//						character_asset.data.kits = null;
		//						character_asset.data.money = 0.00f;
		//						character_asset.Sync();


		//						App.WriteLine("spawned");
		//						return ent;
		//					});
		//				}
		//			}
		//#endif
		//		}


		public struct ConfigureRPC: Net.IRPC<Checkpoint.Data>
		{

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Checkpoint.Data data)
			{

			}
#endif
		}

		//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, flags: ISystem.Flags.Unchecked)]
		//public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region,
		//[Source.Owned] in Transform.Data transform,
		//[Source.Owned] ref Checkpoint.Data checkpoint, [Source.Owned, Optional] in Faction.Data faction)
		//{

		//}

		[Flags]
		public enum TransitFlags: uint
		{
			None = 0u,

			Is_Owned = 1u << 0,
			Is_Inside = 1u << 1,
			Is_Region_Only = 1u << 2,
			Is_Parented = 1u << 3,
			//Is_ = 1u << 4,



			Can_Spawn = 1u << 16,
			Can_Enter = 1u << 17,
			Can_Exit = 1u << 18,
			Can_Move = 1u << 19,

		}

		//public static void GetTransitInfo()
		//{
		//	var h_origin = character_data.origin;
		//	ref var origin_data = ref h_origin.GetData();

		//	var h_species = character_data.species;
		//	ref var species_data = ref h_species.GetData();

		//	var is_owned = character_data.players.Contains(h_player);

		//	var character_region_id = character_asset.RegionID;
		//	var ent_character_g = character_asset.GetGlobalEntity();
		//	var ent_character_r = character_asset.GetRegionEntity(region_id_location);

		//	var is_region_only = character_asset.Scope == Asset.Scope.Region;
		//	var is_g_alive = ent_character_g.IsAlive();
		//	var is_r_alive = ent_character_r.IsAlive();

		//	var is_inside = h_character && characters_region.Contains(h_character);
		//	var is_g_valid = is_g_alive;
		//	var is_r_valid = is_r_alive;

		//	var is_parented_to_entrance = children_g_span.Contains(ent_character_g);

		//	var can_enter = (is_current_region & !h_character & h_character_active) && (this.ent_checkpoint == Interactable.GetCurrentTarget() && !children_r_span.Contains(ent_character_active)); // is_owned && !Client.IsLoadingRegion();
		//	var can_exit = h_character & !is_inside & is_parented_to_entrance & is_owned;
		//	var can_spawn = h_character & is_inside & is_parented_to_entrance & is_owned; // children_r_span.Contains(ent_r); // is_current_region && h_character && this.ent_checkpoint == Interactable.GetCurrentTarget(); // is_owned && !Client.IsLoadingRegion();
		//	var can_move = h_character & is_parented_to_entrance;
		//}

#if CLIENT
		public const int current_year = 752;
		//public static Tickstamp Dormitory.ts_next_respawn;

		public struct CommonCheckpointGUI: IGUICommand
		{
			public Entity ent_checkpoint;
			//public Entity ent_g_checkpoint;
			//public Entity ent_r_checkpoint;

			public Entity ent_entrance;
			//public Entity ent_g_entrance;
			//public Entity ent_r_entrance;

			public Checkpoint.Data checkpoint;
			//public Checkpoint.Data g_checkpoint;

			public Entrance.Data entrance;
			//public Entrance.Data g_entrance;

			public Transform.Data transform;
			public IFaction.Handle h_faction;

			public static Entity edit_ent_selected;
			public static ICharacter.Handle edit_h_character_selected;


			//public static Entity ent_checkpoint_g;
			//public static Entity ent_checkpoint_r;

			public static string edit_search_filter;

			[Shitcode] // TODO: clean up this mess
			public void Draw()
			{
				//var ent_checkpoint_r = this.ent_checkpoint.AsGlobalEntity();

				ref var region_common = ref this.ent_checkpoint.GetRegionCommon();
				//ref var region = ref region_common.AsRegion();

				ref var entrance_data = ref this.ent_entrance.GetAssetData(out IEntrance.Definition entrance_asset);
				if (entrance_data.IsNotNull())
				{
					var h_location = entrance_data.h_location_parent;

					var col_faction_entrance = Color32BGRA.Neutral;

					var h_faction_entrance = this.h_faction;
					ref var faction_data_entrance = ref h_faction_entrance.GetData();
					if (faction_data_entrance.IsNotNull())
					{
						col_faction_entrance = faction_data_entrance.color_a;
					}

					var region_id_client = Client.GetRegionID();
					var region_id_location = h_location.GetRegionID();

					var ent_location_g = h_location.GetGlobalEntity();
					//var ent_location_r = ent_location_g.WithRegionID(region_id_location);

					var is_location_g_alive = ent_location_g.IsAlive();
					//var is_entrance_r_alive = ent_entrance_r.IsAlive();

					var is_locked = false;

					ref var site_g = ref Site.Data.Null;
					if (is_location_g_alive)
					{
						site_g = ref ent_location_g.GetComponent<Site.Data>();
					}

					if (site_g.IsNotNull())
					{
						is_locked = site_g.flags.HasAny(Site.Data.Flags.Locked);
					}

					//using (var window = GUI.Window.InteractionMisc("Checkpoint.Misc"u8, this.ent_checkpoint, size: new(144, 144 + 40), min_width: 96.00f, min_height: 40.00f)) // color_tab: GUI.col_src))
					using (var window = GUI.Window.InteractionMisc(title: "Checkpoint.Misc"u8,
					entity: this.ent_checkpoint,
					size: new(144, 40),
					min_width: 96.00f,
					min_height: 40.00f)) // color_tab: GUI.col_src))
					{
						if (window.show)
						{
							var alpha = 1.00f;

							//GUI.DrawMapThumbnail(region_id_location, size: new(GUI.RmX), show_frame: true);

							if (region_id_client != entrance_asset.region_id)
							{
								var color = GUI.col_button_ok;
								if (GUI.DrawButton("Load Region"u8, size: GUI.Rm, font_size: 20, error: is_locked || Client.IsLoadingRegion() || App.CurrentTickstamp < Dormitory.ts_next_respawn,
								color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
								{
									Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;
									Client.RequestSetActiveRegion(region_id_location, delay_seconds: 0.75f);

									window.Close();
									GUI.RegionMenu.ToggleWidget(false);

									//Client.TODO_LoadRegion(region_id);
								}
							}
							else
							{
								var color = GUI.col_button_error;
								if (GUI.DrawButton(text: "Unload Region"u8, size: GUI.Rm, font_size: 20, error: is_locked || Client.IsLoadingRegion() || App.CurrentTickstamp < Dormitory.ts_next_respawn,
								color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
								{
									Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;
									Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
								}
							}

							//if (GUI.DrawButton("Exit"u8, size: new(GUI.RmX, 32), color: GUI.col_button_error))
							//{

							//}
						}
					}

					using (var window = GUI.Window.Interaction(identifier: "Border Crossing"u8, entity: this.ent_checkpoint, color_tab: GUI.col_src, tooltip_tab: "Travel between this\nregion entrance and the world map."))
					{
						this.StoreCurrentWindowTypeID(order: -200);
						if (window.show)
						{
							var ent_entrance_g = entrance_asset.GetGlobalEntity();
							var ent_entrance_r = ent_entrance_g.WithRegionID(region_id_location);

							var is_entrance_g_alive = ent_entrance_g.IsAlive();
							var is_entrance_r_alive = ent_entrance_r.IsAlive();

							if (this.ent_entrance.IsAlive() && is_entrance_g_alive)
							{
								var entrance_g = ent_entrance_g.GetComponent<Entrance.Data>().OrDefault();

								//var is_selected_character_valid = h_character_selected.IsValid();

								var children_g_span = FixedArray.CreateSpan32<Entity>(out var children_g_buffer);
								var children_r_span = FixedArray.CreateSpan16<Entity>(out var children_r_buffer);
								var characters_dormitory = Span<ICharacter.Handle>.Empty;

								var characters_all_list = FixedArray.CreateSpanList64<ICharacter.Handle>(out var charactes_all_buffer);
								var characters_global_list = FixedArray.CreateSpanList32<ICharacter.Handle>(out var charactes_global_buffer);
								var characters_region_list = FixedArray.CreateSpanList32<ICharacter.Handle>(out var charactes_region_buffer);

								var spawn_flags = Spawn.Flags.None;

								ref var dormitory = ref this.ent_checkpoint.GetComponent<Dormitory.Data>();
								if (dormitory.IsNotNull())
								{
									characters_dormitory = dormitory.GetCharacterSpan();
								}

								ref var spawn = ref this.ent_checkpoint.GetComponent<Spawn.Data>();
								if (spawn.IsNotNull())
								{
									spawn_flags = spawn.flags;
								}

								var characters_inside = entrance_g.GetCharacterSpan();

								ent_entrance_g.GetChildren(ref children_g_span, Relation.Type.Stored);
								for (var i = 0; i < children_g_span.Length; i++)
								{
									var ent_child = children_g_span[i];
									if (ent_child.IsValid() && ent_child.TryGetAsset(out ICharacter.Definition character_asset))
									{
										var h_character = character_asset.GetHandle();

										characters_all_list.AddUnique(h_character);
										//if (!h_character.GetRegionBits().TestBit(region_id_location))
										if (character_asset.region_id == 0)
										{
											characters_global_list.Add(h_character);
										}
										else if (character_asset.region_id == region_id_location) // h_character.GetRegionBits().TestBit(region_id_location))
										{
											characters_region_list.Add(h_character);
										}
									}
								}

								ent_entrance_r.GetChildren(ref children_r_span, Relation.Type.Stored);
								for (var i = 0; i < children_r_span.Length; i++)
								{
									var ent_child = children_r_span[i];
									if (ent_child.IsValid() && ent_child.TryGetAsset(out ICharacter.Definition character_asset))
									{
										var h_character = character_asset.GetHandle();

										characters_all_list.AddUnique(h_character);
										if (character_asset.region_id == region_id_location) // h_character.GetRegionBits().TestBit(region_id_location))
										{
											characters_region_list.AddUnique(h_character);
										}
										//characters_region_list.Add(h_character);
									}
								}

								for (var i = 0; i < characters_dormitory.Length; i++)
								{
									var h_character = characters_dormitory[i];
									if (h_character)
									{
										characters_all_list.AddUnique(h_character);
									}
								}

								characters_global_list.Sort();
								characters_region_list.Sort();
								characters_all_list.Sort();

								ref var character_data_active = ref Client.GetCharacter(out var character_asset_client);
								var h_character_active = character_asset_client.GetHandle();
								var ent_character_active = h_character_active.GetEntity(region_id_client);

								var h_character_selected = edit_h_character_selected.OrDefault(h_character_active);
								if (h_character_selected != h_character_active && !characters_all_list.Contains(h_character_selected)) h_character_selected = default;
								//if (!h_character_selected && h_character_active.GetRegionID() == region_id_client) h_character_selected = h_character_active;

								//GUI.Text(edit_h_character_selected.ToString());

								//var is_selected_parented_to_entrance = children_g_span.Contains(edit_ent_selected.AsGlobalEntity());

								//var ent_selected = is_selected_parented_to_entrance ? edit_ent_selected : default; // is_selected_character_valid ? h_character_selected.GetGlobalEntity() : default;

								//var ent_selected_g = ent_selected.AsGlobalEntity();
								//var ent_selected_r = ent_selected_g.WithRegionID(region_id_location);

								//var is_selected_g_alive = ent_selected_g.IsAlive();
								//var is_selected_r_alive = ent_selected_r.IsAlive();

								//ref var character_data_active = ref Client.GetCharacter(out var character_asset_client);
								//var h_character_active = character_asset_client.GetHandle();
								//var ent_character_active = h_character_active.GetEntity(region_id_client);

								ref var player_data = ref Client.GetPlayerData(out var player_asset);
								var h_player = player_asset.GetHandle();

								var h_faction_client = Client.GetFactionHandle();

								var can_manage = this.h_faction.IsOwned(h_faction_client);
								var is_selected_owned = false;
								var can_access_inventory = false;
								var character_selected_region_id = 0;
								var character_selected_scope = Asset.Scope.Undefined;

								ref var character_data_selected = ref h_character_selected.GetData(out var character_asset_selected);
								if (character_data_selected.IsNotNull())
								{
									is_selected_owned = character_data_selected.players.Contains(h_player);
									character_selected_region_id = character_asset_selected.RegionID;
									character_selected_scope = character_asset_selected.Scope;
									can_access_inventory = true;
								}

								var character_selected_region_bits = h_character_selected.GetRegionBits();
								var is_current_region = region_id_location == region_id_client;

								var scroll_w = (GUI.RmX * 0.50f); // 24.00f * 14;

								static void DrawCharacterRow(ICharacter.Handle h_character, ICharacter.Handle h_character_selected, ICharacter.Handle h_character_active, Span<ICharacter.Handle> characters_region, byte region_id_location, bool right)
								{
									ref var character_data = ref h_character.GetData(out var character_asset);
									if (character_data.IsNotNull())
									{
										using (var hash = GUI.ID<Checkpoint.Data, ICharacter.Data>.Push(h_character))
										{
											var character_region_id = character_asset.RegionID;
											var h_origin = character_data.origin;

											var is_inside = characters_region.Contains(h_character);
											var is_region_only = character_asset.Scope == Asset.Scope.Region;
											var is_selected = h_character_selected == h_character; // h_character_selected && h_character == h_character_selected; // selected_index;

											var h_faction_character = character_data.faction;
											var color_character_faction = h_faction_character.GetColorA();
											var color_character_faction_blended = GUI.font_color_title.LumaBlend(color_character_faction, 0.80f);

											var row_h = 40.00f;
											//using (var group_row = GUI.Group.New(size: new(GUI.RmX, row_h + (is_selected ? 32 : 0))))
											using (var group_row = GUI.Group.New(size: new(GUI.RmX, row_h)))
											{
												if (right)
												{
													using (var group_bar = GUI.Group.New(size: new(8, row_h)))
													//using (var group_bar = GUI.Group.New(size: new(32, GUI.RmY)))
													{
														var col_bar = GUI.col_default;

														if (is_region_only) col_bar = GUI.col_edit;
														else if (character_region_id == 0) col_bar = GUI.col_output;
														else if (character_region_id == region_id_location) col_bar = GUI.col_input;

														//if (h_character == h_character_active) col_bar = GUI.col_button_ok;

														//if (character_region_id == 0) col_bar = GUI.col_output;
														//else if (character_region_id == region_id_location) col_bar = GUI.col_input;

														group_bar.DrawBackground(GUI.tex_slot_filled_white, color: col_bar);
													}

													GUI.SameLine();
												}

												using (var group_top = GUI.Group.New(size: new(right ? (GUI.RmX) : (GUI.RmX - 8), row_h)))
												{
													using (var group_character = GUI.Group.New(size: GUI.Rm))
													{
														if (right)
														{
															Dormitory.DrawCharacterHead(h_character, frame_size: new(GUI.RmY));

															GUI.SameLine();

															using (var group_name = GUI.Group.New(size: GUI.Rm))
															{
																group_name.DrawBackground(GUI.tex_panel);
																//group_name.DrawBackground(GUI.tex_panel_white, color: ((h_character == h_character_active) ? Color32BGRA.Orange.WithColorDiv(Maths.Factor.x4) : GUI.col_black).WithAlpha(128));
																GUI.TitleCentered(character_data.name, size: 16, pivot: new(0.00f, 0.00f), offset: new(4, 2), color: color_character_faction_blended);
																GUI.FocusableAsset(h_character);

																GUI.TextShadedCentered(h_origin.GetShortName(), pivot: new(0.00f, 0.00f), offset: new(6, 16), size: 14, color: GUI.font_color_desc);
																GUI.FocusableAsset(h_origin);


																//GUI.SameLine();

																//GUI.DrawSpriteCentered(GUI.spr_icons_widget.WithFrame(3, 0), rect: group_name.GetOuterRect().GetFittedRect(new(group_name.GetHeight()), 1.00f), layer: GUI.Layer.Window, scale: 2);
															}
														}
														else
														{
															using (var group_name = GUI.Group.New(size: GUI.Rm.SubX(GUI.RmY)))
															{
																group_name.DrawBackground(GUI.tex_panel);
																GUI.TitleCentered(character_data.name, size: 16, pivot: new(1.00f, 0.00f), offset: new(-2, 2), color: color_character_faction_blended);
																GUI.FocusableAsset(h_character);

																GUI.TextShadedCentered(h_origin.GetShortName(), pivot: new(1.00f, 0.00f), offset: new(-6, 16), size: 14, color: GUI.font_color_desc);
																GUI.FocusableAsset(h_origin);
															}

															GUI.SameLine();

															Dormitory.DrawCharacterHead(h_character, frame_size: new(GUI.RmY));
														}
													}

													if (GUI.Selectable3(hash, group_top.GetOuterRect(), selected: is_selected))
													{
														//App.WriteValue(hash.hash);
														//App.WriteValue((edit_h_character_selected, h_character_selected, h_character));
														edit_h_character_selected.Toggle(h_character);
														//WorldMap.SelectUnitBehavior(ent_child, WorldMap.SelectUnitMode.Single, WorldMap.SelectUnitFlags.Toggle, selected: is_selected);
													}

													//GUI.FocusableAsset(h_character);
												}

												if (!right)
												{
													GUI.SameLine();

													using (var group_bar = GUI.Group.New(size: new(8, row_h)))
													{
														var col_bar = GUI.col_default;

														if (is_region_only) col_bar = GUI.col_edit;
														else if (character_region_id == 0) col_bar = GUI.col_output;
														else if (character_region_id == region_id_location) col_bar = GUI.col_input;

														//if (h_character == h_character_active) col_bar = GUI.col_button_ok;

														//if (character_region_id == 0) col_bar = GUI.col_output;
														//else if (character_region_id == region_id_location) col_bar = GUI.col_input;

														group_bar.DrawBackground(GUI.tex_slot_filled_white, color: col_bar);
													}
												}
											}
										}
									}
								}

								//{
								//	var is_selected_inside = h_character_selected && characters_region.Contains(h_character_selected);
								//	var is_selected_g_valid = is_selected_g_alive;
								//	var is_selected_r_valid = is_selected_r_alive;

								//	var can_enter = (is_current_region & !h_character_selected & h_character_active) && (this.ent_checkpoint == Interactable.GetCurrentTarget() && !children_r_span.Contains(ent_character_active)); // is_selected_owned && !Client.IsLoadingRegion();
								//	var can_exit = h_character_selected & !is_selected_inside & is_selected_parented_to_entrance & is_selected_owned;
								//	var can_spawn = h_character_selected & is_selected_inside & is_selected_parented_to_entrance & is_selected_owned; // children_r_span.Contains(ent_selected_r); // is_current_region && h_character_selected && this.ent_checkpoint == Interactable.GetCurrentTarget(); // is_selected_owned && !Client.IsLoadingRegion();
								//	var can_move = h_character_selected & is_selected_parented_to_entrance;
								//}

								if (true)
								{
									using (var group_world = GUI.Group.New(size: new(scroll_w + 32 - 5, GUI.RmY)))
									{
										//group_world.DrawBackground(GUI.tex_frame);

										using (var group_a = GUI.Group.New(size: new(GUI.RmX, 24 * 12)))
										{
											using (var group_left = GUI.Group.New(size: GUI.Rm.SubX(64)))
											{
												using (var group_header = GUI.Group.New(size: new(GUI.RmX, 32)))
												{
													group_header.DrawBackground(GUI.tex_slot_filled_white, color: GUI.col_output);

													GUI.TextShadedCentered("World"u8, size: 18, font: GUI.Font.Superstar,
														pivot: new(1.00f, 0.50f), box_shadow: false, offset: new(-8, 0));


													//GUI.TextShadedCentered($"{entrance_data.GetShortName()} | {h_faction_entrance.GetShortName().OrDefault("<unowned>")}", size: 18, font: GUI.Font.Superstar,
													//	pivot: new(0.00f, 0.50f), box_shadow: false, offset: new(8, 0));

													//GUI.SameLine();

													//GUI.TextShadedCentered(" - "u8, size: 18, font: GUI.Font.Superstar,
													//	pivot: new(0.00f, 0.50f), box_shadow: false, offset: new(0, 0));




													//string edit_search_filter = null;
													//if (GUI.TextInput("##search"u8, "search (Ctrl+F)"u8, ref edit_search_filter, new Vector2(GUI.RmX - 40, GUI.RmY), max_length: 24, show_label: false))
													//{

													//}
													//GUI.FocusOnCtrlF();

													//if (GUI.TextInput("search.checkpoint"u8, "<search>"u8, ))

													//group.DrawBackground(GUI.tex_slot_filled_white, color: GUI.col_input);
													//GUI.TextShadedCentered("REGION"u8, size: 16, font: GUI.Font.Superstar, pivot: new(0.00f, 0.50f), box_shadow: false, offset: new(8, 0));
												}

												GUI.SeparatorThick();

												using (var group_mid = GUI.Group.New(size: GUI.Rm.SubY(40 + 2)))
												using (var scroll = GUI.Scrollbox.New("scroll.checkpoint.world"u8, size: GUI.Rm, padding: new(2), force_scrollbar: true))
												{
													//group_mid.DrawBackground(GUI.tex_frame);
													//scroll.group_frame.DrawBackground(GUI.tex_frame);

													//using (var scroll = GUI.Scrollbox.New("scroll.checkpoint.world"u8, size: GUI.Rm, force_scrollbar: true))
													{
														for (var i = 0u; i < characters_global_list.Count; i++)
														{
															var h_character = characters_global_list[i];
															DrawCharacterRow(h_character, h_character_selected, h_character_active, characters_inside, region_id_location, false);
														}
													}
												}

												GUI.SeparatorThick();

												using (var group = GUI.Group.New(size: GUI.Rm))
												{

												}

												if (true) //this.dormitory.flags.HasNone(Dormitory.Flags.No_Reject | Dormitory.Flags.No_Hiring))
												{
													//if (GUI.DrawButton(text: "Exit"u8,
													//size: new Vector2(80, 40),
													//error: !can_exit || App.CurrentTickstamp < Dormitory.ts_next_respawn, // false && h_character && characters_dormitory.Contains(h_character),
													//color: GUI.col_remove))
													//{
													//	Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;
													//	var ent_spawn = this.ent_checkpoint;

													//	var rpc = new WorldMap.Unit.ActionRPC();
													//	rpc.action = WorldMap.Unit.Action.Exit;
													//	rpc.ent_target = ent_entrance_g;
													//	//rpc.pos_target =  wpos_mouse_snapped + ((transform.position - wpos_mouse_snapped).GetNormalized(out var dist) * Maths.Min((unit_index++) * 0.30f, dist * 0.50f));
													//	rpc.SendAsTask(ent_character_g).WaitForRender().ContinueWith((x) =>
													//	{
													//		GUI.RegionMenu.ToggleWidget(true);
													//		//WorldMap.hs_selected_entities.Clear();
													//		WorldMap.hs_selected_entities.Add(ent_character_g);
													//		//WorldMap.FocusEntity(x.ent_character_global);
													//		WorldMap.FocusEntity(ent_character_g, interact: false);

													//		var rpc = new RespawnExt.SetSpawnRPC()
													//		{
													//			ent_spawn = ent_spawn
													//		};
													//		rpc.Send(Client.GetEntity());
													//	});
													//}
												}
											}

											GUI.SameLine();

											using (var group_col = GUI.Group.New(size: GUI.Rm))
											{
												using (var group_buttons = GUI.Group.New(size: new(GUI.RmX, 48)))
												{
													//group_right.DrawBackground(GUI.tex_frame);

													//using (var group_fill = GUI.Group.New(size: GUI.Rm.SubY(48 * 2)))
													//{

													//}

													if (GUI.DrawIconButton(identifier: "checkpoint.move.l"u8,
													sprite: GUI.tex_icons_widget.GetSprite(8, 16, 4, 8),
													size: new(32, 48),
													color: GUI.col_output,
													color_icon: GUI.col_button_yellow,
													error: App.CurrentTickstamp < Dormitory.ts_next_respawn || !(h_character_selected && character_selected_region_id == region_id_location && characters_region_list.Contains(h_character_selected))))
													{
														Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;

														var rpc = new Entrance.MoveRPC()
														{
															h_character = h_character_selected,
															action = Entrance.MoveRPC.Action.Move_To_World
														};
														rpc.SendAsTask(ent_entrance_g).WaitForRender().ContinueWith((x) =>
														{
															if (x.results.ok)
															{
																edit_h_character_selected = x.h_character;
																//GUI.RegionMenu.ToggleWidget(true);

																//WorldMap.hs_selected_entities.Clear();
																//WorldMap.hs_selected_entities.Add(x.results.ent_character);
																//WorldMap.FocusEntity(x.ent_character_global);
																//WorldMap.FocusEntity(ent_entrance_g, interact: true);
															}
															//Checkpoint.CommonCheckpointGUI.ent_selected = x.ent_character_global;
														});
													}
													GUI.DrawHoverTooltip("Move the selected character outside the region."u8);

													GUI.SameLine();

													//GUI.Text($"{characters_region_list.Count}");

													if (GUI.DrawIconButton(identifier: "checkpoint.move.r"u8,
													sprite: GUI.tex_icons_widget.GetSprite(8, 16, 5, 8),
													size: new(32, 48),
													color: GUI.col_input,
													color_icon: GUI.col_button_yellow,
													error: is_locked || App.CurrentTickstamp < Dormitory.ts_next_respawn || !(h_character_selected && character_selected_region_id == 0 && characters_global_list.Contains(h_character_selected) && !characters_region_list.IsFull)))
													{
														Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;

														var rpc = new Entrance.MoveRPC()
														{
															h_character = h_character_selected,
															action = Entrance.MoveRPC.Action.Move_To_Region
														};
														rpc.SendAsTask(ent_entrance_g).ContinueWith(async (x) =>
														{
															if (x.results.ok)
															{
																if (x.h_character && !Client.GetCharacterHandle() && Client.NetConnection.CanControlCharacter(x.h_character))
																{
																	var rpc_result = await Client.SetCharacter(x.h_character, sync: true, force: true);
																	App.WriteValue(rpc_result.h_character, color: App.Color.Magenta);
																}

																await App.WaitRender();

																edit_h_character_selected = x.h_character;
																//if (x.h_character)
																//{
																//	var rpc_result = await Client.SetCharacter(x.h_character, sync: true);
																//	App.WriteValue(rpc_result.h_character, color: App.Color.Magenta);
																//}

																//GUI.RegionMenu.ToggleWidget(true);

																//WorldMap.hs_selected_entities.Clear();
																//WorldMap.hs_selected_entities.Add(x.results.ent_character);
																//WorldMap.FocusEntity(x.ent_character_global);
																//WorldMap.FocusEntity(ent_entrance_g, interact: true);
															}
															//Checkpoint.CommonCheckpointGUI.ent_selected = x.ent_character_global;
														});
													}
													GUI.DrawHoverTooltip("Move the selected character inside the region."u8);
												}

												using (var group_buttons = GUI.Group.New(size: GUI.Rm, padding: new(6)))
												{
													group_buttons.DrawBackground(GUI.tex_frame);

													//if (GUI.DrawIconButton(identifier: "checkpoint.btn.reject"u8,
													//sprite: GUI.tex_icons_widget.GetSprite(16, 16, 0, 9),
													//size: new(GUI.RmX, 40),
													//color: GUI.col_button_error,
													//color_icon: GUI.font_color_default.WithColorDiv(Maths.Factor.x4),
													//enabled: h_character_selected && !character_selected_region_bits.TestBit(region_id_location)))
													//{
													//}


													if (GUI.DrawIconButton(identifier: "checkpoint.btn.detain"u8,
													sprite: GUI.tex_icons_widget.GetSprite(16, 16, 0, 10),
													size: new(GUI.RmX, 40),
													color: GUI.col_button, //.WithColorDiv(Maths.Factor.x2),
													color_icon: GUI.col_button,
													enabled: false && can_manage && (h_character_selected & (h_character_selected != h_character_active))))
													{
														Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;
													}
													GUI.DrawHoverTooltip("Detain the selected character."u8);

													using (var group_reserve = GUI.Group.New(size: GUI.Rm.SubY(40)))
													{

													}

													if (GUI.DrawIconButton(identifier: "checkpoint.btn.reject"u8,
													sprite: GUI.tex_icons_widget.GetSprite(16, 16, 0, 9),
													size: new(GUI.RmX, 40),
													color: GUI.col_edit, //.WithColorDiv(Maths.Factor.x2),
													color_icon: GUI.col_src,
													error: !(can_manage && (h_character_selected & (characters_dormitory.Contains(h_character_selected))))))
													{
														var rpc = new Dormitory.DEV_FireRPC()
														{
															h_character = h_character_selected
														};
														rpc.SendAsTask(this.ent_checkpoint).WaitForRender().ContinueWith((x) =>
														{
															edit_h_character_selected = default;
														});
													}
													GUI.DrawHoverTooltip("Reject the selected character."u8);
												}
											}
										}

										using (var group_bottom = GUI.Group.New(size: GUI.Rm))
										{
											group_bottom.DrawBackground(GUI.tex_frame);
										}
									}

									GUI.SameLine();

									using (var group_region = GUI.Group.New(size: GUI.Rm))
									{
										using (var group_header = GUI.Group.New(size: new(GUI.RmX, 32)))
										{
											group_header.DrawBackground(GUI.tex_slot_filled_white, color: GUI.col_input);

											GUI.TextShadedCentered("Region"u8, size: 18, font: GUI.Font.Superstar,
												pivot: new(0.00f, 0.50f), box_shadow: false, offset: new(8, 0));
										}

										GUI.SeparatorThick();

										//using (var group_mid = GUI.Group.New(size: GUI.Rm.SubY(40), padding: new(6)))
										using (var scroll = GUI.Scrollbox.New("scroll.checkpoint.region"u8, size: GUI.Rm.SubY(40 + 2), padding: new(2), force_scrollbar: true))
										{
											//group_mid.DrawBackground(GUI.tex_frame);

											//using (var scroll = GUI.Scrollbox.New("scroll.checkpoint.region"u8, size: GUI.Rm, force_scrollbar: true))
											{
												for (var i = 0u; i < characters_region_list.Count; i++)
												{
													var h_character = characters_region_list[i];
													DrawCharacterRow(h_character, h_character_selected, h_character_active, characters_inside, region_id_location, true);
												}

												if (!characters_region_list.IsEmpty)
												{
													GUI.SeparatorThick();
												}

												for (var i = 0; i < characters_dormitory.Length; i++)
												{
													var h_character = characters_dormitory[i];
													DrawCharacterRow(h_character, h_character_selected, h_character_active, characters_inside, region_id_location, true);
												}
											}
										}

										GUI.SeparatorThick();

										using (var group_bottom = GUI.Group.New(size: GUI.Rm))
										{
											//var h_character = h_character_selected; //.OrDefault(h_character_active);
											var h_character = h_character_selected.OrDefault(h_character_active);
											ref var character_data = ref h_character.GetData(out var character_asset);

											//GUI.Text(h_character.ToString());

											if (character_data.IsNotNull())
											{
												var h_origin = character_data.origin;
												ref var origin_data = ref h_origin.GetData();

												var h_species = character_data.species;
												ref var species_data = ref h_species.GetData();

												var is_owned = character_data.faction == h_faction_client || character_data.players.Contains(h_player);

												var character_region_id = character_asset.RegionID;
												var ent_character_g = character_asset.GetGlobalEntity();
												var ent_character_r = character_asset.GetRegionEntity(region_id_location);

												var is_region_only = character_asset.Scope == Asset.Scope.Region;
												var is_g_alive = ent_character_g.IsAlive();
												var is_r_alive = ent_character_r.IsAlive();

												var is_inside = h_character && characters_inside.Contains(h_character);
												var is_g_valid = is_g_alive;
												var is_r_valid = is_r_alive;

												var is_parented_to_entrance = children_g_span.Contains(ent_character_g);



												var can_enter = (is_current_region & h_character == h_character_active) && is_owned && (this.ent_checkpoint == Interactable.GetCurrentTarget() && !children_r_span.Contains(ent_character_active)); // is_owned && !Client.IsLoadingRegion();
												var can_exit = h_character & !is_inside & is_parented_to_entrance & is_owned;
												//var can_spawn = h_character & is_inside & is_parented_to_entrance & is_owned; // children_r_span.Contains(ent_r); // is_current_region && h_character && this.ent_checkpoint == Interactable.GetCurrentTarget(); // is_owned && !Client.IsLoadingRegion();
												var can_move = h_character & is_parented_to_entrance;


												var character_count_max = Constants.Characters.character_count_max;
												if (h_faction_client == 0 && !Constants.Characters.allow_neutral_multi_characters)
												{
													character_count_max = 1;
												}

												ref var region = ref region_common.AsRegion();
												var player_characters = FixedArray.CreateSpanList8<ICharacter.Handle>(out var player_characters_buffer, (uint)Constants.Characters.character_count_max);
												if (region.IsNotNull())
												{
													region.GetActiveCharacters(h_player: h_player, results: ref player_characters, filter: new(require: Character.StateFlags.Alive, exclude: Character.StateFlags.Dead | Character.StateFlags.Main));
												}

												var is_available = h_character && character_selected_region_id == region_id_location && characters_all_list.Contains(h_character);
												var can_spawn_as = is_available && h_character.CanSpawnAsCharacter(h_faction: h_faction_client, h_faction_spawn: h_faction_entrance, h_player: h_player, spawn_flags: spawn_flags);
												can_spawn_as &= character_data.flags.HasAny(Character.Flags.Main) || h_character == h_character_active || h_character_active == default || player_characters.Count < character_count_max;

												if (true) //this.dormitory.flags.HasNone(Dormitory.Flags.No_Reject | Dormitory.Flags.No_Hiring))
												{
													if (GUI.DrawButton(text: "Exit"u8,
													size: new Vector2(80, 40),
													error: !can_exit || App.CurrentTickstamp < Dormitory.ts_next_respawn, // false && h_character && characters_dormitory.Contains(h_character),
													color: GUI.col_remove))
													{
														Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;
														var ent_spawn = this.ent_checkpoint;

														var rpc = new WorldMap.Unit.ActionRPC();
														rpc.action = WorldMap.Unit.Action.Exit;
														rpc.ent_target = ent_entrance_g;
														//rpc.pos_target =  wpos_mouse_snapped + ((transform.position - wpos_mouse_snapped).GetNormalized(out var dist) * Maths.Min((unit_index++) * 0.30f, dist * 0.50f));
														rpc.SendAsTask(ent_character_g).WaitForRender().ContinueWith((x) =>
														{
															GUI.RegionMenu.ToggleWidget(true);
															//WorldMap.hs_selected_entities.Clear();
															WorldMap.hs_selected_entities.Add(ent_character_g);
															//WorldMap.FocusEntity(x.ent_character_global);
															WorldMap.FocusEntity(ent_character_g, interact: false);

															var rpc = new RespawnExt.SetSpawnRPC()
															{
																ent_spawn = ent_spawn
															};
															rpc.Send(Client.GetEntity());
														});
													}
													//GUI.DrawHoverTooltip("Reject this character."u8);

													GUI.SameLine();
												}

												if (false) //this.dormitory.flags.HasNone(Dormitory.Flags.No_Hiring))
												{
													//Crafting.Context.NewFromCharacter(ref region.AsCommon(), character_self_asset, this.ent_dormitory, out var context, search_radius: 4.00f);
													//if (GUI.DrawRequirementButton(ref context, character_data.requirements_hire, "Recruit"u8, size: new Vector2(80, 40), enabled: is_character_self_valid & is_hireable, color: GUI.col_button_yellow))
													if (GUI.DrawButton(text: "Recruit"u8,
													size: new Vector2(80, 40),
													enabled: false && h_character && characters_dormitory.Contains(h_character),
													color: GUI.col_button_yellow))
													{
														//var rpc = new DEV_HireRPC()
														//{
														//	h_character = h_character_selected
														//};
														//rpc.Send(this.ent_dormitory);
													}
													GUI.DrawHoverTooltip("Recruit this character."u8);

													GUI.SameLine();
												}

												if (true)
												{
													if (GUI.DrawButton(text: "Enter"u8,
													size: new(80, GUI.RmY),
													color: GUI.col_button_ok,
													error: is_locked || !can_enter || App.CurrentTickstamp < Dormitory.ts_next_respawn))
													{
														Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;

														var h_character_tmp = h_character_active;
														if (is_current_region)
														{
															//var ent_entrance_region = entrance_asset.GetEntity(region_id);
															if (ent_entrance_r.IsAlive())
															{
																var rpc = new Entrance.EnterRPC()
																{
																	h_character = h_character_tmp,
																	flags = Entrance.EnterEvent.Flags.None
																};
																rpc.SendAsTask(ent_entrance_r).WaitForRender().ContinueWith((x) =>
																{
																	edit_h_character_selected = x.h_character;
																	//edit_ent_selected = ent_character_active;
																});
															}
														}

														//window.Close();
														//GUI.RegionMenu.ToggleWidget(false);
													}

													GUI.SameLine();
												}

												if (GUI.DrawButton(text: ">> Spawn <<"u8,
												size: GUI.Rm,
												color: GUI.col_button_ok,
												error: is_locked || !can_spawn_as || App.CurrentTickstamp < Dormitory.ts_next_respawn))
												{
													Dormitory.ts_next_respawn = App.CurrentTickstamp + Dormitory.button_cooldown;

													var h_character_tmp = h_character;
													if (is_current_region)
													{
														if (character_asset.scope == Asset.Scope.Region)
														{
															var rpc = new Spawn.SpawnRPC()
															{
																h_character = h_character_tmp,
																h_component = IComponent.Handle.FromComponent<Dormitory.Data>(),
																flags = Spawn.SpawnEvent.Flags.Control | Spawn.SpawnEvent.Flags.Claim
															};
															rpc.Send(this.ent_checkpoint);
														}
														else if (ent_entrance_r.IsAlive())
														{
															var rpc = new Entrance.SpawnRPC()
															{
																h_character = h_character_tmp
															};
															rpc.SendAsTask(ent_entrance_r).WaitForRender().ContinueWith((x) =>
															{
																edit_h_character_selected = default;
																//edit_ent_selected = default;
																GUI.RegionMenu.ToggleWidget(false);
															});
														}
													}
													else
													{
														Client.RequestSetActiveRegion(region_id: region_id_location, delay_seconds: 0.75f).ContinueWith((task) =>
														{
															App.WriteLine("loaded region", App.Color.Green);

															if (ent_entrance_r.IsAlive())
															{
																var rpc = new Entrance.SpawnRPC()
																{
																	h_character = h_character_tmp,
																};
																rpc.SendAsTask(ent_entrance_r).WaitForRender().ContinueWith((x) =>
																{
																	edit_h_character_selected = x.h_character;
																	//edit_ent_selected = default;
																	GUI.RegionMenu.ToggleWidget(false);
																});
															}
														});
													}
												}
											}
										}
									}
								}

								{
									var h_character = h_character_selected; //.OrDefault(h_character_active);

									ref var character_data = ref h_character.GetData(out var character_asset);
									if (character_data.IsNotNull())
									{
										var h_origin = character_data.origin;
										ref var origin_data = ref h_origin.GetData();

										var h_species = character_data.species;
										ref var species_data = ref h_species.GetData();

										var max_carry_weight = 15.00f + Maths.Avg((character_data.experience[Experience.Type.Endurance] * 6.00f), (character_data.experience[Experience.Type.Strength] * 4.50f)).SnapCeilFast(5);

										//var is_owned = character_data.players.Contains(h_player);

										var character_region_id = character_asset.RegionID;
										var ent_character_g = character_asset.GetGlobalEntity();
										var ent_character_r = character_asset.GetRegionEntity(region_id_location);

										var is_region_only = character_asset.Scope == Asset.Scope.Region;
										var is_g_alive = ent_character_g.IsAlive();
										//var is_r_alive = ent_character_r.IsAlive();

										var is_inside = h_character && characters_inside.Contains(h_character);
										var is_g_valid = is_g_alive;
										//var is_r_valid = is_r_alive;

										if (!is_inside && h_character == h_character_active)
										{
											// TODO
										}
										else
										{
											using (var window_child = window.BeginChildWindow(identifier: "checkpoint.sub"u8,
											anchor_x: GUI.AlignX.Right,
											anchor_y: GUI.AlignY.Top,
											open: true,
											size: new Vector2(48 * 5, 448),
											padding: new(4)))
											{
												if (window_child.show)
												{
													//var h_origin = character_data.origin;
													//ref var origin_data = ref h_origin.GetData();

													//var h_species = character_data.species;
													//ref var species_data = ref h_species.GetData();

													////var is_owned = character_data.players.Contains(h_player);

													//var character_region_id = character_asset.RegionID;
													//var ent_character_g = character_asset.GetGlobalEntity();
													//var ent_character_r = character_asset.GetRegionEntity(region_id_location);

													//var is_region_only = character_asset.Scope == Asset.Scope.Region;
													//var is_g_alive = ent_character_g.IsAlive();
													////var is_r_alive = ent_character_r.IsAlive();

													//var is_inside = h_character && characters_inside.Contains(h_character);
													//var is_g_valid = is_g_alive;
													////var is_r_valid = is_r_alive;

													//var is_parented_to_entrance = children_g_span.Contains(ent_character_g);

													//var can_enter = (is_current_region & h_character == h_character_active) && (this.ent_checkpoint == Interactable.GetCurrentTarget() && !children_r_span.Contains(ent_character_active)); // is_owned && !Client.IsLoadingRegion();
													//var can_exit = h_character & !is_inside & is_parented_to_entrance & is_owned;
													//var can_spawn = h_character & is_inside & is_parented_to_entrance & is_owned; // children_r_span.Contains(ent_r); // is_current_region && h_character && this.ent_checkpoint == Interactable.GetCurrentTarget(); // is_owned && !Client.IsLoadingRegion();
													//var can_move = h_character & is_parented_to_entrance;

													//using (var group = GUI.Group.New(size: GUI.Rm, padding: new(0)))
													{
														using (var group_bar = GUI.Group.New(size: new(GUI.RmX, 24)))
														{
															var col_bar = GUI.col_default;
															var text = Utf8String.Empty;

															if (is_region_only)
															{
																col_bar = GUI.col_edit;
																if (h_character == h_character_active) text = "LOCAL | CURRENT"u8; // can this actually happen?
																else text = "LOCAL | RECRUITABLE"u8;
															}
															else if (character_region_id == 0)
															{
																col_bar = GUI.col_output;
																text = "WORLD | STAGING"u8;
															}
															else if (character_region_id == region_id_location)
															{
																col_bar = GUI.col_input;
																text = "REGION | STAGING"u8;
															}

															//if (h_character == h_character_active) col_bar = GUI.col_button_ok;

															group_bar.DrawBackground(GUI.tex_slot_filled_white, color: col_bar);
															GUI.TextShadedCentered(text, size: 16, font: GUI.Font.Superstar, pivot: new(0.00f, 0.50f), box_shadow: false, offset: new(8, 0));
														}

														using (var group = GUI.Group.New(size: new(GUI.RmX, 48)))
														{
															Dormitory.DrawCharacterHead(h_character, new Vector2(GUI.RmY));

															GUI.SameLine();

															//var ent_name = h_character.GetName().OrDefault(ent.GetName());
															GUI.TitleCentered(character_data.GetShortName(), pivot: new(0.00f, 0.00f), offset: new(4, 0), size: 18);

															//h_character.getreg

															var str_status = Utf8String.Empty;
															var col_status = GUI.font_color_desc;

															//if (h_character)
															//{
															//	if (characters_region.Contains(h_character)) str_status = "- STATIONED"u8;
															//	else str_status = "- STAGING"u8;
															//}
															//GUI.TitleCentered(str_status.OrDefault("- <N/A>"u8), pivot: new(0.00f, 0.50f), offset: new(12, 10), size: 16, color: col_status, font: GUI.Font.Superstar);
															GUI.TextShadedCentered(h_origin.GetShortName(), pivot: new(0.00f, 0.00f), offset: new(6, 16), size: 14, color: col_status);

														}

														GUI.SeparatorThick();

														var inventory_width = 48.00f * 2;

														using (var group = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40 - 2 - 48 - (48 * 2) - 2)))
														{
															using (var scrollbox = GUI.Scrollbox.New("scroll.experience"u8, size: GUI.Rm))
															{
																Experience.DrawTableSmall2(ref character_data.experience);
															}

															//using (var group_inventories = GUI.Group.New(size: new(inventory_width, GUI.RmY)))
															//{
															//	if (is_g_valid)
															//	{
															//		var inventories = ent_character_g.GetInventories();
															//		foreach (var h_inventory in inventories)
															//		{
															//			if (h_inventory.IsValid() && h_inventory.Flags.HasNone(Inventory.Flags.Hidden))
															//			{
															//				using (GUI.Group.New(size: h_inventory.GetFrameSize(0, 2)))
															//				{
															//					GUI.DrawInventory(h_inventory, is_readonly: !can_access_inventory);
															//				}
															//			}
															//		}
															//	}
															//}
														}

														GUI.SeparatorThick();

														var is_too_heavy = false;

														using (var group = GUI.Group.New(size: GUI.Rm.SubY(40 + 2), padding: new(6)))
														{
															if (is_g_valid)
															{
																var inventories = ent_character_g.GetInventories();
																foreach (var h_inventory in inventories)
																{
																	if (h_inventory.IsValid() && h_inventory.Flags.HasNone(Inventory.Flags.Hidden))
																	{
																		is_too_heavy = h_inventory.Mass > max_carry_weight;

																		//using (GUI.Group.New(size: h_inventory.GetFrameSize(2, 0)))
																		//using (group.Split(size: h_inventory.GetFrameSize(2, 0), align_x: GUI.AlignX.Center, align_y: GUI.AlignY.Top))
																		using (group.Split(size: h_inventory.GetFrameSize(2, 0).WithY(GUI.RmY), 
																		align_x: GUI.AlignX.Center, align_y: GUI.AlignY.Top))
																		{
																			GUI.DrawInventory(h_inventory, is_readonly: !can_access_inventory);

																			GUI.NewLine(4);

																			GUI.LabelShaded("Weight:"u8, $"{h_inventory.Mass:0.##} kg / {max_carry_weight:0.##} kg",
																					font_a: GUI.Font.Superstar, size_a: 16,
																					color_b: is_too_heavy ? GUI.font_color_red_b : GUI.font_color_desc);


																			//using (var group_weight = GUI.Group.New(size: new(GUI.RmX, 16)))
																			//{
																			//	GUI.LabelShaded("Weight:"u8, h_inventory.Mass, format: "0.##' kg'",
																			//		font_a: GUI.Font.Superstar, size_a: 16, color_b: GUI.font_color_desc, width: GUI.RmX * 0.625f);
																			//	GUI.SameLine();

																			//	GUI.TextShadedCentered(max_carry_weight, format: "' / '0.##' kg'", color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));
																			//}
																			GUI.NewLine(2);
																			if (is_too_heavy) GUI.TextShaded("* Too heavy!"u8, color: GUI.font_color_red_b);
																			//GUI.TitleCentered("Text"u8,, pivot: new(0.00f, 1.00f));
																		}

																		//using (group.Split(size: new(GUI.RmX, 32), align_x: GUI.AlignX.Center, align_y: GUI.AlignY.Bottom))
																		//{
																		//	//GUI.LabelShaded("Weight:"u8, h_inventory.Mass, format: "0.##' kg'",
																		//	//	font_a: GUI.Font.Superstar, size_a: 16, 
																		//	//	color_b: GUI.font_color_desc, width: GUI.RmX * 0.625f);

																		//	GUI.LabelShaded("Weight:"u8, $"{h_inventory.Mass:0.##} kg / {max_carry_weight:0.##} kg",
																		//		font_a: GUI.Font.Superstar, size_a: 16,
																		//		color_b: GUI.font_color_desc, width: GUI.RmX * 0.75f);


																		//	//using (var group_weight = GUI.Group.New(size: new(GUI.RmX, 16)))
																		//	//{
																		//	//	GUI.LabelShaded("Weight:"u8, h_inventory.Mass, format: "0.##' kg'",
																		//	//		font_a: GUI.Font.Superstar, size_a: 16, color_b: GUI.font_color_desc, width: GUI.RmX * 0.625f);
																		//	//	GUI.SameLine();

																		//	//	GUI.TextShadedCentered(max_carry_weight, format: "' / '0.##' kg'", color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));
																		//	//}
																		//	GUI.NewLine(2);
																		//	GUI.TextShaded("* Too heavy!"u8, color: GUI.font_color_red_b);
																		//	//GUI.TitleCentered("Text"u8,, pivot: new(0.00f, 1.00f));
																		//}

																		//GUI.TitleCentered("Text"u8, rect: group.GetInnerRect(), pivot: new(0.00f, 1.00f));

																		break;
																	}
																}
															}
														}

														//GUI.SeparatorThick();

														GUI.SeparatorThick();

														using (var group = GUI.Group.New(size: GUI.Rm))
														{
															if (GUI.DrawButton("Recruit"u8, size: new Vector2(80, GUI.RmY), 
															enabled: false && h_character && characters_dormitory.Contains(h_character), color: GUI.col_button_yellow))
															{
																var rpc = new Dormitory.DEV_HireRPC()
																{
																	h_character = h_character
																};
																rpc.Send(this.ent_checkpoint);
															}
															GUI.DrawHoverTooltip("Recruit this character."u8);
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

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI_Region(ISystem.Info info, ref Region.Data region,
		Entity ent_checkpoint, Entity ent_entrance,
		[Source.Shared] in Transform.Data transform, [Source.Shared] in Interactable.Data interactable,
		[Source.Shared] in Checkpoint.Data checkpoint, [Source.Owned] in Entrance.Data entrance,
		//[Source.Owned, Optional] in Dormitory.Data dormitory,
		[Source.Shared, Optional(true)] ref Faction.Data faction)
		{
			if (interactable.IsActive())
			{
				//App.WriteLine(info.TableCount);
				var gui = new CommonCheckpointGUI()
				{
					ent_checkpoint = ent_checkpoint,
					ent_entrance = ent_entrance,

					checkpoint = checkpoint,
					entrance = entrance,

					transform = transform,
					h_faction = faction.OrDefault().id
				};
				gui.Submit();
			}

			//return;
			//if (interactable.IsActive())
			//{
			//	var gui = new RegionCheckpointGUI()
			//	{
			//		ent_checkpoint = ent_checkpoint,
			//		ent_entrance = ent_entrance,
			//		checkpoint = checkpoint,
			//		entrance = entrance,
			//		transform = transform,
			//		dormitory = dormitory,
			//		h_faction = faction.id
			//	};
			//	gui.Submit();
			//}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI_Global(ISystem.Info.Global info, ref Region.Data.Global region, // Entity entity,
		Entity ent_checkpoint, Entity ent_entrance,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Checkpoint.Data checkpoint, [Source.Owned] in Entrance.Data entrance,
		[Source.Owned, Optional(true)] ref Faction.Data faction)
		{
			if (interactable.IsActive())
			{
				//App.WriteLine(info.TableCount);
				var gui = new CommonCheckpointGUI()
				{
					ent_checkpoint = ent_checkpoint,
					ent_entrance = ent_entrance,

					checkpoint = checkpoint,
					entrance = entrance,

					transform = transform,
					h_faction = faction.OrDefault().id
				};
				gui.Submit();
			}

			//return;
			//if (interactable.IsActive())
			//{
			//	var gui = new GlobalCheckpointGUI()
			//	{
			//		ent_checkpoint = entity,
			//		checkpoint = checkpoint,
			//		entrance = entrance,
			//		transform = transform,
			//		h_faction = faction.id
			//	};
			//	gui.Submit();
			//}
		}
#endif
	}
}
