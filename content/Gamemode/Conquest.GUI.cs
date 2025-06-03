using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
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

									var ret = region.TriggerSystem(Conquest.FetchNearestSpawn, ref args);
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

									var ret = region.TriggerSystem(Conquest.FetchNearestSpawn, ref args);
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

		[Shitcode]
		[HasTag("local", true, Source.Modifier.Owned)]
		[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked, order: -55)]
		public static void UpdateRespawn([Source.Owned] in Respawn.Data respawn, [Source.Owned] in Player.Data player)
		{
			if (!WorldMap.IsOpen && !player.h_character && respawn.ent_selected_spawn.IsAlive())
			{
				ref var interactable = ref respawn.ent_selected_spawn.GetComponent<Interactable.Data>();
				if (interactable.IsNotNull())
				{
					interactable.SetActive(true);
					Interactor.local_target = respawn.ent_selected_spawn;
				}
			}
		}

		[Shitcode]
		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIRespawn(Entity entity, [Source.Owned] in Player.Data player, [Source.Owned] in Respawn.Data respawn)
		{
			//Spawn.RespawnGUI.enabled = false;

			//if (player.IsLocal())
			{
				//if (!WorldMap.IsOpen && player.flags.HasNone(Player.Flags.Alive) && !(player.flags.HasAny(Player.Flags.Editor) && !Editor.show_respawn_menu))
				if (!WorldMap.IsOpen && !player.ent_controlled.IsValid() && !(player.flags.HasAny(Player.Flags.Editor) && !Editor.show_respawn_menu))
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

