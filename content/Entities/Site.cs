using TC2.Base.Components;
using System.Runtime.InteropServices;

namespace TC2.Conquest
{
	public static partial class Site
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public Site.Data.Flags flags;

		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		[Source.Owned] ref Site.Data site, [Source.Owned] ref Transform.Data transform)
		{

		}

#if CLIENT
		public partial struct SiteGUI: IGUICommand
		{
			public Entity ent_site;
			public Site.Data site;
			public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_site.GetRegionCommon();

				using (var window = GUI.Window.Interaction("Region Overview"u8, this.ent_site))
				{
					if (window.show)
					{
						ref var mod_context = ref App.GetModContext();

						var h_location = this.ent_site.GetAssetHandle<ILocation.Handle>();
						ref var location_data = ref h_location.GetData(out var location_asset);
						if (location_data.IsNotNull())
						{
							var is_loading = Client.IsLoadingRegion();
							var location_region_id = location_asset.region_id;

							{
								//var ent_asset = location_asset.GetGlobalEntity();
								//selected_region_id = h_selected_location.GetRegionID();

								//for (var i = 0; i < Region.max_count; i++)
								//{
								//	ref var region_info = ref World.GetRegionInfo((byte)i);
								//	if (region_info.IsNotNull())
								//	{
								//		ref var map_info = ref region_info.map_info.GetRefOrNull();
								//		if (map_info.IsNotNull() && map_info.h_location == h_selected_location)
								//		{
								//			selected_region_id = (byte)i;
								//			break;
								//		}
								//	}
								//}

								using (GUI.Group.New(size: GUI.Rm))
								{
									//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(8, 0)))
									//{
									//	GUI.TitleCentered(location_data.name, size: 32, pivot: new(0.00f, 0.50f));
									//}
									//GUI.FocusableAsset(h_location);

									//GUI.SeparatorThick();

									var map_asset = default(MapAsset);

									ref var region_info = ref World.GetRegionInfo(location_region_id);
									if (region_info.IsNotNull())
									{
										ref var map_info = ref region_info.map_info.GetRefOrNull();
										if (map_info.IsNotNull() && map_info.h_location == h_location)
										{
											map_asset = App.GetModContext().GetMap(region_info.map);
										}
									}

									using (var group_top = GUI.Group.New(size: new(140 + 224, 0), padding: new(4, 4)))
									{
										using (var group_left = GUI.Group.New(size: new(140)))
										{
											using (var group_thumbnail = GUI.Group.New(size: new(GUI.RmX)))
											{
												if (map_asset != null)
												{
													GUI.DrawMapThumbnail(map_asset, size: GUI.Rm, show_frame: false);
												}
												else
												{
													//GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
													GUI.DrawSpriteCentered(location_data.thumbnail, group_thumbnail.GetInnerRect(), GUI.Layer.Window, scale: 1.00f);
												}

												GUI.DrawBackground(GUI.tex_frame_white, rect: group_thumbnail.GetOuterRect(), padding: new(4), color: GUI.col_button);
											}

											if (map_asset != null)
											{
												//var color = GUI.col_button_ok;
												//var alpha = 1.00f;

												//if (GUI.DrawIconButton("info"u8, new(GUI.tex_icons_widget, 16, 16, 6, 1), size: new(48, 48)))
												//{

												//}

												//GUI.SameLine();

												//if (Client.GetRegionID() != selected_region_id)
												//{
												//	if (GUI.DrawButton("Join"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
												//	{
												//		Client.RequestSetActiveRegion(selected_region_id, delay_seconds: 0.75f);

												//		window.Close();
												//		GUI.RegionMenu.ToggleWidget(false);

												//		//Client.TODO_LoadRegion(region_id);
												//	}
												//}
												//else
												//{
												//	color = GUI.col_button_error;
												//	if (GUI.DrawButton("Leave"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
												//	{
												//		Client.RequestSetActiveRegion(0, delay_seconds: 0.10f);
												//	}
												//}
											}
											else
											{
												//if (GUI.DrawButton("Button"u8, size: new(GUI.RmX, 48), font_size: 24, enabled: false, color: GUI.col_button, text_color: GUI.font_color_button_text))
												//{

												//}
											}
										}

										GUI.SameLine();

										using (var group_desc = GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
										{
											group_desc.DrawBackground(GUI.tex_panel, inner: true);

											using (GUI.Wrap.Push(GUI.RmX))
											{
												if (map_asset != null)
												{
													GUI.TextShaded(map_asset.Description);
												}
												else
												{
													GUI.TextShaded(location_data.desc);
												}
											}
										}
									}

									GUI.SeparatorThick();

									using (var group_bottom = GUI.Group.New(size: GUI.Rm, padding: new(2, 2)))
									{
										using (var group_left = GUI.Group.New2(size: new(384, GUI.RmY)))
										{
											using (var scrollbox = GUI.Scrollbox.New("scroll.bottom"u8, size: GUI.Rm))
											{
												Span<Entity> children_span = FixedArray.CreateSpan32NoInit<Entity>(out var buffer_children);
												//ent_asset.GetAllChildren(ref children_span, false);
												ent_site.GetChildren(ref children_span, Relation.Type.Child);
												children_span.Sort();

												foreach (var ent_child in children_span)
												{
													//if (ILocation.TryGetAsset(ent_child, out var h_location_child))
													{
														var asset_entrance = default(IEntrance.Definition);
														if (ent_child.TryGetAsset(out ILocation.Definition asset_location) || ent_child.TryGetAsset(out asset_entrance))
														{
															var h_faction_child = ent_child.GetFactionHandle();

															using (GUI.ID<Region.Info>.Push(ent_child))
															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
															{
																ref var checkpoint = ref ent_child.GetComponent<Checkpoint.Data>();

																using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
																{
																	var color_frame = Color32BGRA.GUI;

																	ref var faction_data = ref h_faction_child.GetData();
																	if (faction_data.IsNotNull())
																	{
																		color_frame = faction_data.color_a;
																	}

																	group_icon.DrawBackground(GUI.tex_slot_white, color: color_frame);
																	GUI.FocusableAsset(h_faction_child);

																	//ref var marker = ref ent_child.GetComponent<Marker.Data>();
																	//if (marker.IsNotNull())
																	//{
																	//	GUI.DrawSpriteCentered(marker.icon, group_icon.GetInnerRect(), layer: GUI.Layer.Window, scale: 2.00f);
																	//}

																	//if (GUI.Selectable3(ent_child.GetShortID(), group_icon.GetInnerRect(), selected: selected))
																	//{
																	//	WorldMap.selected_entity = selected ? default : ent_child;
																	//	GUI.SetDebugEntity(ent_child);
																	//}
																}

																//if (GUI.IsItemHovered())
																//{
																//	using (GUI.Tooltip.New(size: new(128, 0)))
																//	{
																//		using (GUI.Wrap.Push(GUI.RmX))
																//		{
																//			//GUI.Title(location_data_child.name_short, size: 20);
																//			GUI.Title(ent_child.GetName(), size: 20);
																//		}

																//		GUI.SeparatorThick(new(-4, -4));

																//		using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new(4)))
																//		{
																//			using (GUI.Wrap.Push(GUI.RmX))
																//			{
																//				//GUI.Text(location_data_child.desc);
																//			}
																//		}
																//	}
																//}
																//GUI.FocusableAsset(h_location_child);

																GUI.SameLine();

																using (var group_right = GUI.Group.New2(size: GUI.Rm, padding: new(10, 6, 4, 6)))
																{
																	group_right.DrawBackground(GUI.tex_window_sidebar_b);
																	GUI.TitleCentered(ent_child.GetName(), size: 24, pivot: new(0.00f, 0.00f));
																	//if ( GUI.FocusableAsset()

																	if (checkpoint.IsNotNull())
																	{
																		GUI.TextShadedCentered(ent_child.GetPrefabName(), size: 14, pivot: new(0.00f, 1.00f));
																		//GUI.TextShadedCentered(checkpoint., size: 24, pivot: new(0.00f, 0.00f));
																	}

																	//group_right.DrawBackground(GUI.tex_window_popup);

																	//if (GUI.DrawIconButton("info"u8, new(GUI.tex_icons_widget, 16, 16, 6, 1), size: new(48, 48)))
																	//{

																	//}

																	//GUI.SameLine();

																	using (var group_button = group_right.Split(size: new(80, GUI.RmY), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
																	{
																		var alpha = 1.00f;
																		if (Client.GetRegionID() != location_region_id)
																		{
																			var color = GUI.col_button_ok;
																			if (GUI.DrawButton("Enter"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
																			//if (GUI.DrawSpriteButton("enter"u8, sprite: GUI.spr_icons_widget.WithFrame(1, 13), size: GUI.Rm, enabled: !is_loading, color: color.WithAlphaMult(alpha)))
																			{
																				//Client.RequestSetActiveRegion(location_region_id, delay_seconds: 0.75f);
																				//var ent_entrance_region = asset_entrance.GetRegionEntity(location_region_id);

																				//asset_entrance.data.

																				// TODO: giga shithack
																				var ent_entrance_region = asset_entrance.GetRegionEntity(location_region_id);
																				Client.RequestSetActiveRegion(location_region_id, delay_seconds: 0.75f).ContinueWith((x) =>
																				{
																					if (ent_entrance_region.IsAlive())
																					{
																						var ent_spawn = ent_entrance_region.GetComponentOwner<Spawn.Data>(Relation.Type.Instance);
																						if (ent_spawn.IsAlive())
																						{
																							var rpc = new RespawnExt.SetSpawnRPC()
																							{
																								ent_spawn = ent_spawn
																							};
																							rpc.Send(Client.GetEntity());
																						}
																					}
																				});


																				window.Close();
																				GUI.RegionMenu.ToggleWidget(false);

																				//Client.TODO_LoadRegion(region_id);
																			}
																			GUI.DrawHoverTooltip("Switch to this region."u8);
																		}
																		else
																		{
																			var color = GUI.col_button_error;
																			if (GUI.DrawButton("Leave"u8, size: GUI.Rm, font_size: 24, enabled: !is_loading, color: color.WithAlphaMult(alpha), text_color: GUI.font_color_button_text.WithAlphaMult(alpha)))
																			//if (GUI.DrawSpriteButton("exit"u8, sprite: GUI.spr_icons_widget.WithFrame(2, 13), size: GUI.Rm, enabled: !is_loading, color: color.WithAlphaMult(alpha)))
																			{
																				Client.RequestSetActiveRegion(0, delay_seconds: 0.10f).ContinueWith(async (x) =>
																				{
																					await App.WaitRender();

																					WorldMap.FocusLocation(location_asset);
																				});

																				//WorldMap.FocusLocation(location_asset);
																			}
																		}
																	}
																}

																var is_selected = WorldMap.interacted_entity_cached == ent_child;
																if (GUI.Selectable3(ent_child.GetShortID(), group_row.GetInnerRect(), selected: is_selected))
																{
																	WorldMap.interacted_entity.Toggle(ent_child, !is_selected);
																	GUI.SetDebugEntity(ent_child);
																}
															}

															//GUI.SeparatorThick();
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

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
		[Source.Owned] ref Site.Data site, [Source.Owned] ref Transform.Data transform)
		{
			if (WorldMap.IsOpen)
			{
				var gui = new Site.SiteGUI()
				{
					ent_site = entity,
					site = site,
					transform = transform
				};
				gui.Submit();
			}

			//App.WriteLine("GUI");
		}
#endif
	}
}

