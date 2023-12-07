
namespace TC2.Conquest
{
	public static partial class Conquest
	{
#if CLIENT
		public struct CreationGUI: IGUICommand
		{
			public struct Info
			{
				public ISpecies.Handle h_species;
				public ILocation.Handle h_location;
				public IOrigin.Handle h_origin;

				public IHair.Handle h_hair_male;
				public IHair.Handle h_beard_male;

				public IHair.Handle h_hair_female;
				public IHair.Handle h_beard_female;

				public Character.Flags character_flags;
				public IMap.Industry industry;
				public IMap.Crime crime;
				public IMap.Services services;

				public float hair_color_ratio;
				public float age_ratio;
				public float current_cost;

				public Organic.Gender gender = Organic.Gender.Male;

				public override readonly int GetHashCode()
				{
					return HashCode.Combine(this.h_species, this.h_origin, this.gender);
				}

				public Info()
				{
				}
			}

			//public static readonly Dictionary<int, CreationGUI.Info> hash_to_info = new Dictionary<uint, Info>(8);
			//public static int selected_hash;

			//public static ISpecies.Handle h_selected_species;
			//public static ILocation.Handle h_selected_location;
			//public static IOrigin.Handle h_selected_origin;

			//public static IHair.Handle h_selected_hair_male;
			//public static IHair.Handle h_selected_beard_male;

			//public static IHair.Handle h_selected_hair_female;
			//public static IHair.Handle h_selected_beard_female;

			//public static float selected_hair_color_ratio;
			//public static float selected_age_ratio;
			//public static float current_cost;

			//public static ISpecies.Handle h_selected_species;
			//public static IOrigin.Handle h_selected_origin;
			//public static Organic.Gender selected_gender = Organic.Gender.Male;

			public static CreationGUI.Info edit_info = new();

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

						//selected_hash = HashCode.Combine(h_selected_species, h_selected_origin, selected_gender);

						//var selected_info = hash_to_info.GetOrAdd(selected_hash);

						//var character_flags = default(Character.Flags);
						var sprite_head = default(Sprite);
						var sprite_hair = default(Sprite);
						var sprite_beard = default(Sprite);

						var age_min = 0;
						var age_max = 1;
						var age = 0;

						var character_flags_default = default(Character.Flags);
						var character_flags_optional = default(Character.Flags);

						var hair_color = Color32BGRA.White;

						ref var h_selected_hair = ref (edit_info.gender == Organic.Gender.Female ? ref edit_info.h_hair_female : ref edit_info.h_hair_male);
						ref var h_selected_beard = ref (edit_info.gender == Organic.Gender.Female ? ref edit_info.h_beard_female : ref edit_info.h_beard_male);

						ref var species_data = ref edit_info.h_species.GetData();
						if (species_data.IsNotNull())
						{
							sprite_head = edit_info.gender == Organic.Gender.Female ? species_data.sprite_head_female : species_data.sprite_head_male;
							hair_color = species_data.hair_colors.AsSpan().GetLerped(edit_info.hair_color_ratio);
						}

						ref var origin_data = ref edit_info.h_origin.GetData();
						if (origin_data.IsNotNull())
						{
							character_flags_default = origin_data.character_flags;
							character_flags_optional = origin_data.character_flags_optional;

							age_min = (int)origin_data.age.GetValue(0.00f);
							age_max = (int)origin_data.age.GetValue(1.00f);
							age = Maths.LerpInt(age_min, age_max, edit_info.age_ratio);
						}

						edit_info.character_flags |= character_flags_default;

						if (species_data.IsNotNull())
						{
							//hair_color = Color32BGRA.Saturate(hair_color, 1.00f - Maths.InvLerp01(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, age));
							hair_color = Color32BGRA.Lerp(hair_color, Color32BGRA.White, Maths.InvLerp01(Maths.Lerp(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, 0.70f), species_data.lifecycle.age_elder * 1.40f, age));
						}

						hair_color.a = 255;

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

										if (GUI.AssetInput2("edit.species", ref edit_info.h_species, size: new(160, GUI.RmY), show_label: false, tab_height: 24.00f, close_on_select: true,
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
										}))
										{
											reset = true;
										}

										GUI.SameLine();

										if (GUI.AssetInput2("edit.origin", ref edit_info.h_origin, size: new(GUI.RmX, GUI.RmY), show_label: false, tab_height: 40.00f, close_on_select: true,
										filter: (x) => x.data.species == edit_info.h_species && x.data.flags.HasAll(IOrigin.Flags.Special),
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
										}))
										{
											edit_info.character_flags = default;
											//reset = true;
										}
									}

									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										if (GUI.DrawIconButton("edit.gender.male", new Sprite("ui_icons_gender", 16, 16, 1, 0), size: new(GUI.RmY), color: edit_info.gender == Organic.Gender.Male ? GUI.col_button_highlight : null))
										{
											edit_info.gender = Organic.Gender.Male;
										}

										GUI.SameLine();

										if (GUI.DrawIconButton("edit.gender.female", new Sprite("ui_icons_gender", 16, 16, 2, 0), size: new(GUI.RmY), color: edit_info.gender == Organic.Gender.Female ? GUI.col_button_highlight : null))
										{
											edit_info.gender = Organic.Gender.Female;
										}

										GUI.SameLine();

										GUI.AssetInput2("edit.hair", ref h_selected_hair, size: new(160, GUI.RmY), show_label: false, tab_height: 64.00f, close_on_select: false,
											filter: (x) => x.data.species == edit_info.h_species && x.data.gender == edit_info.gender && x.data.flags.HasAll(IHair.Flags.Hair)
											&& (x.data.character_flags.IsEmpty() || edit_info.character_flags.HasAny(x.data.character_flags)) && (x.data.character_flags_exclude.IsEmpty() || !edit_info.character_flags.HasAny(x.data.character_flags_exclude)),
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

										GUI.AssetInput2("edit.beard", ref h_selected_beard, size: new(160, GUI.RmY), show_label: false, tab_height: 64.00f, close_on_select: false,
											filter: (x) => x.data.species == edit_info.h_species && x.data.gender == edit_info.gender && x.data.flags.HasAll(IHair.Flags.Beard)
											&& (x.data.character_flags.IsEmpty() || edit_info.character_flags.HasAny(x.data.character_flags)) && (x.data.character_flags_exclude.IsEmpty() || !edit_info.character_flags.HasAny(x.data.character_flags_exclude)),
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

										GUI.SameLine();

										GUI.SliderFloat("Hair Color", ref edit_info.hair_color_ratio, 0.00f, 1.00f, size: GUI.Rm);
									}
								}
							}

							using (var group_mid = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 64), padding: new(4)))
							{
								group_mid.DrawBackground(GUI.tex_window_menu);

								using (var group_a = GUI.Group.New(size: new(GUI.RmX, 400), padding: new(4)))
								{
									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{


										var w = GUI.RmX;

										//using (GUI.Group.New(size: new(w * 0.50f, 32)))
										//{
										//	GUI.SliderIntLerp("Age", ref edit_info.age_ratio, age_min, age_max, size: new(GUI.RmX * 0.75f, GUI.RmY));
										//}

										GUI.SliderIntLerp("Age", ref edit_info.age_ratio, age_min, age_max, size: new(128, 24), show_label: true);

										GUI.EnumInput("flags", ref edit_info.character_flags, size: new(w * 0.75f, 32), show_label: false, max_flags: 64, mask: character_flags_optional, required: character_flags_default, height: 256);
									}
								}
							}
						}

						GUI.SameLine();

						using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
						{
							group_right.DrawBackground(GUI.tex_window);

						}

						if (reset)
						{
							edit_info.h_origin = default;
							edit_info.character_flags = default;

							edit_info.h_hair_male = default;
							edit_info.h_beard_male = default;

							edit_info.h_hair_female = default;
							edit_info.h_beard_female = default;


							//species_data = ref h_selected_species.GetData();
							//if (species_data.IsNotNull())
							//{
							//	selected_gender = Organic.Gender.Male;
							//	h_selec

							//}
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

