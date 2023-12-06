using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		public struct CreationGUI: IGUICommand
		{
			public static ISpecies.Handle h_selected_species;
			public static ILocation.Handle h_selected_location;
			public static IOrigin.Handle h_selected_origin;

			public static IHair.Handle h_selected_hair_male;
			public static IHair.Handle h_selected_beard_male;

			public static IHair.Handle h_selected_hair_female;
			public static IHair.Handle h_selected_beard_female;

			public static float selected_hair_color_ratio;

			public static Organic.Gender selected_gender;

			public void Draw()
			{
				var size = new Vector2(48 * 18, 600);

				using (var window = GUI.Window.Standalone("CreateCharacter", size: size, position: new(GUI.CanvasSize.X * 0.50f, GUI.CanvasSize.Y * 0.50f), pivot: new(0.50f, 0.50f), padding: new(8, 8), force_position: false))
				{
					//this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						GUI.DrawWindowBackground(GUI.tex_window_character, new Vector4(16, 16, 16, 16));

						var reset = false;

						ref var origin_data = ref h_selected_origin.GetData();

						var sprite_head = default(Sprite);
						var sprite_hair = default(Sprite);
						var sprite_beard = default(Sprite);

						var hair_color = Color32BGRA.White;

						ref var h_selected_hair = ref (selected_gender == Organic.Gender.Female ? ref h_selected_hair_female : ref h_selected_hair_male);
						ref var h_selected_beard = ref (selected_gender == Organic.Gender.Female ? ref h_selected_beard_female : ref h_selected_beard_male);

						ref var species_data = ref h_selected_species.GetData();
						if (species_data.IsNotNull())
						{
							sprite_head = selected_gender == Organic.Gender.Female ? species_data.sprite_head_female : species_data.sprite_head_male;

							hair_color = species_data.hair_colors.AsSpan().GetLerped(selected_hair_color_ratio);
							hair_color.a = 255;
						}

						ref var hair_data = ref h_selected_hair.GetData();
						if (hair_data.IsNotNull())
						{
							sprite_hair = hair_data.sprite;
						}

						ref var beard_data = ref h_selected_beard.GetData();
						if (beard_data.IsNotNull())
						{
							sprite_beard = beard_data.sprite;
						}

						using (var group_left = GUI.Group.New(size: new(GUI.RmX - 244, GUI.RmY)))
						{
							using (var group_top = GUI.Group.New(size: new(GUI.RmX, 96)))
							{
								using (var group_head = GUI.Group.New(size: new(GUI.RmY), padding: new(4)))
								{
									group_head.DrawBackground(GUI.tex_window);

									GUI.DrawCharacterHead(sprite_head, sprite_hair, sprite_beard, hair_color, GUI.Rm, scale: 5.00f);
								}

								GUI.SameLine();

								using (var group_a = GUI.Group.New(size: GUI.Rm))
								{
									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
									{
										group_b.DrawBackground(GUI.tex_window);

										GUI.AssetInput2("edit.species", ref h_selected_species, size: new(128, GUI.RmY), show_label: false, tab_height: 24.00f,
										filter: (x) => x.data.flags.HasAll(ISpecies.Flags.Sapient),
										draw: (asset, group, is_title) =>
										{
											if (asset != null)
											{
												GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
											else
											{
												GUI.TitleCentered("<species>", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
										});

										GUI.SameLine();

										GUI.AssetInput2("edit.origin", ref h_selected_origin, size: new(300, GUI.RmY), show_label: false, tab_height: 40.00f,
											filter: (x) => x.data.species == h_selected_species && x.data.flags.HasAll(IOrigin.Flags.Special),
											draw: (asset, group, is_title) =>
											{
												if (asset != null)
												{
													GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
												}
												else
												{
													GUI.TitleCentered("<origin>", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
												}
											});
									}

									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										if (GUI.DrawIconButton("edit.gender.male", new Sprite("ui_icons_gender", 16, 16, 1, 0), size: new(GUI.RmY), color: selected_gender == Organic.Gender.Male ? GUI.col_button_highlight : null))
										{
											if (selected_gender != Organic.Gender.Male) reset = true;
											selected_gender = Organic.Gender.Male;
										}

										GUI.SameLine();

										if (GUI.DrawIconButton("edit.gender.female", new Sprite("ui_icons_gender", 16, 16, 2, 0), size: new(GUI.RmY), color: selected_gender == Organic.Gender.Female ? GUI.col_button_highlight : null))
										{
											if (selected_gender != Organic.Gender.Female) reset = true;
											selected_gender = Organic.Gender.Female;
										}

										GUI.SameLine();

										GUI.AssetInput2("edit.hair", ref h_selected_hair, size: new(128, GUI.RmY), show_label: false, tab_height: 64.00f,
											filter: (x) => x.data.species == h_selected_species && x.data.gender == selected_gender && x.data.flags.HasAll(IHair.Flags.Hair),
											draw: (asset, group, is_title) =>
											{
												if (asset != null)
												{
													//GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
													if (is_title)
													{
														GUI.TitleCentered($"Hair:\n{asset.data.name}", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
													}
													else
													{
														GUI.DrawTextCentered(asset.data.name, position: group.GetInnerRect().GetPosition(new Vector2(0.00f, 1.00f), new Vector2(8.00f, -4.00f)), pivot: new(0.00f, 1.00f), layer: GUI.Layer.Window, color: GUI.font_color_desc);
														GUI.DrawCharacterHead(sprite_head, asset.data.sprite, sprite_beard, hair_color, group.size, scale: 3.00f, draw_frame: true);
													}
													//GUI.DrawSpriteCentered(asset.data.sprite, AABB.Simple(group.a, new Vector2(group.size.Y)), layer: GUI.Layer.Window, scale: 3.00f, color: hair_color);
												}
												else
												{
													GUI.TitleCentered($"Hair:\n<none>", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
												}
											});

										GUI.SameLine();

										GUI.AssetInput2("edit.beard", ref h_selected_beard, size: new(128, GUI.RmY), show_label: false, tab_height: 64.00f,
											filter: (x) => x.data.species == h_selected_species && x.data.gender == selected_gender && x.data.flags.HasAll(IHair.Flags.Beard),
											draw: (asset, group, is_title) =>
											{
												if (asset != null)
												{
													//GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
													if (is_title)
													{
														GUI.TitleCentered($"Beard:\n{asset.data.name}", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
													}
													else
													{
														GUI.DrawTextCentered(asset.data.name, position: group.GetInnerRect().GetPosition(new Vector2(0.00f, 1.00f), new Vector2(8.00f, -4.00f)), pivot: new(0.00f, 1.00f), layer: GUI.Layer.Window, color: GUI.font_color_desc);
														GUI.DrawCharacterHead(sprite_head, sprite_hair, asset.data.sprite, hair_color, group.size, scale: 3.00f, draw_frame: true);
													}
													//GUI.DrawSpriteCentered(asset.data.sprite, AABB.Simple(group.a, new Vector2(group.size.Y)), layer: GUI.Layer.Window, scale: 3.00f, color: hair_color);
												}
												else
												{
													GUI.TitleCentered($"Beard:\n<none>", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
												}
											});
									}
								}
							}

							using (var group_mid = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 64), padding: new(4)))
							{
								group_mid.DrawBackground(GUI.tex_window_menu);
							}
						}

						GUI.SameLine();

						using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
						{
							group_right.DrawBackground(GUI.tex_window);

						}

						if (reset)
						{
							//h_selected_origin = default;
							//h_selected_hair = default;
							//h_selected_beard = default;
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnCharacterGUI(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			if (WorldMap.IsOpen && Client.GetRegionID() == 0 && Character.CharacterHUD.selected_slot.HasValue && Client.GetCharacterHandle().id == 0)
			{
				var gui = new Conquest.CreationGUI();
				gui.Draw();
			}
		}
#endif
	}
}

