
namespace TC2.Conquest
{
	public static partial class Conquest
	{
		public sealed class CustomCharacter
		{
			public struct Vars
			{
				public ISpecies.Handle h_species;
				public ILocation.Handle h_location;
				public IOrigin.Handle h_origin;

				public IHair.Handle h_hair_male;
				public IHair.Handle h_beard_male;

				public IHair.Handle h_hair_female;
				public IHair.Handle h_beard_female;

				public Character.Flags character_flags;
				public IMap.Industry industry_flags;
				public IMap.Services service_flags;
				public IMap.Crime crime_flags;

				public float hair_color_ratio;
				public float age_ratio;

				public Organic.Gender gender = Organic.Gender.Male;

				public override readonly int GetHashCode()
				{
					return HashCode.Combine(this.h_species, this.h_origin, this.gender);
				}

				public Vars()
				{
				}
			}

			public struct Props
			{
				public float mult_tech = 1.00f;
				public float mult_social = 1.00f;
				public float mult_academic = 1.00f;
				public float mult_laborer = 1.00f;
				public float mult_nobility = 1.00f;
				public float mult_fighter = 1.00f;
				public float mult_loser = 1.00f;
				public float mult_health = 1.00f;
				public float mult_wealth = 1.00f;
				public float mult_fancy = 1.00f;
				public float mult_builder = 1.00f;
				public float mult_expert = 1.00f;
				public float mult_smart = 1.00f;
				public float mult_criminal = 1.00f;
				public float mult_evil = 1.00f;
				public float mult_stress = 1.00f;
				public float mult_industrial = 1.00f;
				public float mult_rural = 1.00f;
				public float mult_naval = 1.00f;
				public float mult_urban = 1.00f;
				public float mult_sanity = 1.00f;

				public float bias_tech = 0.00f;
				public float bias_social = 0.00f;
				public float bias_academic = 0.00f;
				public float bias_laborer = 0.00f;
				public float bias_nobility = 0.00f;
				public float bias_fighter = 0.00f;
				public float bias_loser = 0.00f;
				public float bias_health = 0.00f;
				//public float bias_wealth = 0.00f;
				public float bias_fancy = 0.00f;
				public float bias_builder = 0.00f;
				public float bias_expert = 0.00f;
				public float bias_smart = 0.00f;
				public float bias_criminal = 0.00f;
				public float bias_evil = 0.00f;
				public float bias_stress = 0.00f;
				public float bias_industrial = 0.00f;
				public float bias_rural = 0.00f;
				public float bias_naval = 0.00f;
				public float bias_urban = 0.00f;
				public float bias_sanity = 0.00f;

				public Sprite sprite_head;
				public Sprite sprite_hair;
				public Sprite sprite_beard;
				public Color32BGRA hair_color;

				public Experience.Levels experience;

				public int age_min = 0;
				public int age_max = 1;
				public int age = 0;

				public float visual_age_mult = 1.00f;

				public float money = 0.00f;
				public float cost = 0.00f;

				public Character.Flags character_flags_default;
				public Character.Flags character_flags_optional;

				public IMap.Industry industry_flags_default;
				public IMap.Industry industry_flags_optional;

				public IMap.Services service_flags_default;
				public IMap.Services service_flags_optional;

				public IMap.Crime crime_flags_default;
				public IMap.Crime crime_flags_optional;


				public Props()
				{
				}
			}

			public CustomCharacter.Vars vars = new();
			public CustomCharacter.Props props = new();
		}

		public interface ICharacterModifier: IModifier<ICharacterModifier, CustomCharacter.Props, CustomCharacter.Vars, Character.Flags>
		{

		}

		public static class Modifiers
		{
			public static ICharacterModifier.Function modifier_alcoholic =
			[ICharacterModifier.Info(Character.Flags.Alcoholic, order: -1)] static (x) =>
			{
				x.value.bias_health -= 0.32f; // liver doesn't like booze
				x.value.bias_loser += 0.21f; // sleeps in a ditch
				x.value.bias_fancy -= 0.25f; // smells like piss
				x.value.bias_sanity -= 0.15f; // hangover
				x.value.mult_wealth *= 0.88f; // spends money on booze

				x.value.visual_age_mult += 0.11f;
			};

			public static ICharacterModifier.Function modifier_strong =
			[ICharacterModifier.Info(Character.Flags.Strong, order: 1)] static (x) =>
			{
				x.value.experience[Experience.Type.Strength].MulS(1.14f);
				x.value.experience[Experience.Type.Strength].AddS(3);
				x.value.experience[Experience.Type.Endurance].AddS(2);
			};

			public static ICharacterModifier.Function modifier_brawler =
			[ICharacterModifier.Info(Character.Flags.Brawler, order: 5)] static (x) =>
			{
				var t = 10;

				x.value.experience[Experience.Type.Intellect].AddS(-3); // brain damage
				x.value.experience[Experience.Type.Strength].AddS(9);
				x.value.experience[Experience.Type.Endurance].AddS(2 + t);
				x.value.experience[Experience.Type.Endurance].MulS(1.21f);
				x.value.experience[Experience.Type.Charisma].MulS(0.87f); // beaten up and missing some teeth

			};

			public static void Derp()
			{
				static int Foof()
				{
					var b = 2;
					return b;
				}

				var t = Foof();
				var meth = Foof;

				App.WriteLine(meth.Method.DeclaringType);
			}
		}

#if CLIENT
		public struct CreationGUI: IGUICommand
		{


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

			public static CustomCharacter custom_character = new();

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
						//var sprite_head = default(Sprite);
						//var sprite_hair = default(Sprite);
						//var sprite_beard = default(Sprite);

						//var experience = default(Experience.Levels);

						//var age_min = 0;
						//var age_max = 1;
						//var age = 0;

						//var visual_age_mult = 1.00f;

						//var money = 0.00f;
						//var cost = 0.00;

						//var character_flags_default = default(Character.Flags);
						//var character_flags_optional = default(Character.Flags);

						//var industry_flags_default = default(IMap.Industry);
						//var industry_flags_optional = default(IMap.Industry);

						//var service_flags_default = default(IMap.Services);
						//var service_flags_optional = default(IMap.Services);

						//var crime_flags_default = default(IMap.Crime);
						//var crime_flags_optional = default(IMap.Crime);

						//var hair_color = Color32BGRA.White;


						ref var vars = ref custom_character.vars;
						ref var props = ref custom_character.props;

						ref var h_selected_hair = ref (vars.gender == Organic.Gender.Female ? ref vars.h_hair_female : ref vars.h_hair_male);
						ref var h_selected_beard = ref (vars.gender == Organic.Gender.Female ? ref vars.h_beard_female : ref vars.h_beard_male);

						ref var species_data = ref vars.h_species.GetData();
						if (species_data.IsNotNull())
						{
							props.sprite_head = vars.gender == Organic.Gender.Female ? species_data.sprite_head_female : species_data.sprite_head_male;
							props.hair_color = species_data.hair_colors.AsSpan().GetLerped(vars.hair_color_ratio);
						}

						ref var origin_data = ref vars.h_origin.GetData();
						if (origin_data.IsNotNull())
						{
							props.character_flags_default = origin_data.character_flags;
							props.character_flags_optional = origin_data.character_flags_optional;

							props.industry_flags_default = origin_data.industry_flags;
							props.industry_flags_optional = origin_data.industry_flags_optional;

							props.service_flags_default = origin_data.service_flags;
							props.service_flags_optional = origin_data.service_flags_optional;

							props.crime_flags_default = origin_data.crime_flags;
							props.crime_flags_optional = origin_data.crime_flags_optional;

							props.age_min = (int)origin_data.age.GetValue(0.00f);
							props.age_max = (int)origin_data.age.GetValue(1.00f);
							props.age = Maths.LerpInt(props.age_min, props.age_max, vars.age_ratio);

							props.cost = origin_data.cost;
							props.money = origin_data.money;

							props.experience = origin_data.experience;
						}

						vars.character_flags &= props.character_flags_optional;
						vars.industry_flags &= props.industry_flags_optional;
						vars.service_flags &= props.service_flags_optional;
						vars.crime_flags &= props.crime_flags_optional;

						vars.character_flags |= props.character_flags_default;
						vars.industry_flags |= props.industry_flags_default;
						vars.service_flags |= props.service_flags_default;
						vars.crime_flags |= props.crime_flags_default;

						ref var hair_data = ref h_selected_hair.GetData();
						if (hair_data.IsNotNull())
						{
							props.sprite_hair = hair_data.sprite;
						}

						ref var beard_data = ref h_selected_beard.GetData();
						if (beard_data.IsNotNull())
						{
							props.sprite_beard = beard_data.sprite;
						}

						//{
						//	if (edit_info.character_flags.HasAny(Character.Flags.Alcoholic))
						//	{
						//		//mult_health *= 0.92f; // liver doesn't like booze
						//		//mult_loser += 0.21f; // sleeps in a ditch
						//		//mult_fancy *= 0.91f; // smells like piss
						//		//mult_sanity *= 0.90f; // hangover
						//		//mult_wealth *= 0.88f; // spends money on booze
						//		//mult_dumbass *= 1.11f; // drunkard

						//		bias_health -= 0.32f; // liver doesn't like booze
						//		bias_loser += 0.21f; // sleeps in a ditch
						//		bias_fancy -= 0.25f; // smells like piss
						//		bias_sanity -= 0.15f; // hangover
						//		mult_wealth *= 0.88f; // spends money on booze

						//		visual_age_mult += 0.11f;
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Junkie))
						//	{
						//		bias_health -= 0.17f; // screwed up metabolism
						//		bias_loser += 0.35f; // doesn't work and shower
						//		mult_fancy *= 0.79f; // scary eyes
						//		bias_sanity -= 0.26f; // withdrawals
						//		mult_wealth *= 0.52f; // spends money on drugs and doesn't work

						//		visual_age_mult += 0.35f; // too much drug abuse turns you into a raisin
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Strong))
						//	{
						//		experience[Experience.Type.Strength].MulS(1.14f);
						//		experience[Experience.Type.Strength].AddS(3);
						//		experience[Experience.Type.Endurance].AddS(2);
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Brawler))
						//	{
						//		experience[Experience.Type.Intellect].AddS(-3); // brain damage
						//		experience[Experience.Type.Strength].AddS(2);
						//		experience[Experience.Type.Endurance].AddS(2);
						//		experience[Experience.Type.Endurance].MulS(1.21f);
						//		experience[Experience.Type.Charisma].MulS(0.87f); // beaten up and missing some teeth
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Educated))
						//	{
						//		experience[Experience.Type.Intellect].MulS(1.23f);

						//		experience[Experience.Type.Charisma].AddS(2);
						//		experience[Experience.Type.Leadership].AddS(2);

						//		visual_age_mult += 0.07f; // exam stress
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Insane))
						//	{
						//		mult_sanity *= 1.65f;
						//		bias_sanity -= 0.50f;
						//		mult_stress += 0.40f;
						//		mult_fancy *= 0.80f;
						//		mult_social *= 0.77f; // weird
						//		mult_smart *= mult_smart; // brain amplified
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Evil))
						//	{
						//		visual_age_mult *= 1.20f; // evil makes you bald
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Entertainer | Character.Flags.Social))
						//	{
						//		visual_age_mult *= 0.81f; // needs to look good
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Sedentary))
						//	{
						//		visual_age_mult *= 1.08f; // doesn't touch grass
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Professional))
						//	{
						//		visual_age_mult *= 1.14f; // work stress
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Nomad))
						//	{
						//		visual_age_mult *= 0.96f; // travels more
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Medical))
						//	{
						//		visual_age_mult *= 1.16f; // medical school and annoying patients
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Outdoor))
						//	{
						//		visual_age_mult *= 0.91f; // fresh air and touches grass
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Rich))
						//	{
						//		visual_age_mult *= 0.95f; // eats good stuff
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Poor))
						//	{
						//		visual_age_mult *= 1.06f; // eats crap stuff
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Farmer))
						//	{
						//		visual_age_mult *= 0.93f; // touches grass
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Unskilled))
						//	{
						//		experience[Experience.Type.Intellect].MulS(0.84f);

						//		experience[Experience.Type.Arcanology].MulS(0.14f);
						//		experience[Experience.Type.Chemistry].MulS(0.35f);
						//		experience[Experience.Type.Commerce].MulS(0.40f);
						//		experience[Experience.Type.Engineering].MulS(0.42f);
						//		experience[Experience.Type.Geology].MulS(0.64f);
						//		experience[Experience.Type.Medicine].MulS(0.35f);
						//		experience[Experience.Type.Law].MulS(0.39f);
						//		experience[Experience.Type.Charisma].MulS(0.84f);
						//		experience[Experience.Type.Metallurgy].MulS(0.65f);
						//		experience[Experience.Type.Construction].MulS(0.78f);
						//		experience[Experience.Type.Masonry].MulS(0.87f);
						//	}

						//	if (edit_info.character_flags.HasAny(Character.Flags.Illiterate))
						//	{
						//		visual_age_mult *= 0.85f; // can't read newspaper

						//		experience[Experience.Type.Intellect].MulS(0.82f);

						//		experience[Experience.Type.Arcanology].MulS(0.10f);
						//		experience[Experience.Type.Chemistry].MulS(0.05f);
						//		experience[Experience.Type.Commerce].MulS(0.02f);
						//		experience[Experience.Type.Engineering].MulS(0.72f);
						//		experience[Experience.Type.Medicine].MulS(0.35f);
						//		experience[Experience.Type.Law].MulS(0.03f);
						//		experience[Experience.Type.Charisma].MulS(0.64f);
						//		experience[Experience.Type.Metallurgy].MulS(0.85f);
						//	}






						//	if (edit_info.industry_flags.HasAny(IMap.Industry.Education))
						//	{
						//	}




						//	if (edit_info.gender == Organic.Gender.Male)
						//	{
						//		visual_age_mult *= 1.08f; // turns into a bald potato faster
						//	}

						//	if (edit_info.gender == Organic.Gender.Female)
						//	{

						//	}

						//	//experience[Experience.Type.Intellect].MulS();
						//	//experience[Experience.Type.Arcanology].MulS((mult_academic * mult_industrial * mult_urban * mult_industrial) / (MathF.Max(1.00f, mult_rural * mult_dumbass) * mult_sanity));
						//	//experience[Experience.Type.Endurance].MulS((mult_health * mult_rural * mult_laborer * mult_dumbass));
						//	//experience[Experience.Type.Strength].MulS(mult_fighter * mult_rural * mult_laborer * mult_dumbass);
						//	//experience[Experience.Type.Charisma].MulS((mult_fancy * mult_nobility * mult_social * mult_smart * mult_health) / (MathF.Max(1.00f, mult_loser) * mult_dumbass));
						//	//experience[Experience.Type.Engineering].MulS(mult_smart * mult_urban * mult_tech * mult_expert * mult_builder);
						//	//experience[Experience.Type.Leadership].MulS(mult_smart * mult_social * mult_fancy * mult_fighter * mult_evil * mult_nobility);
						//	//experience[Experience.Type.Commerce].MulS(mult_wealth * mult_urban * mult_social * mult_academic * mult_fancy);


						//	//experience[Experience.Type.Intellect].MulS(((mult_smart * Maths.Avg(mult_tech * mult_expert, mult_academic)) / (MathF.Max(1.00f, mult_dumbass) * mult_loser)));
						//	//experience[Experience.Type.Arcanology].MulS((mult_academic * mult_industrial * mult_urban * mult_industrial) / (MathF.Max(1.00f, mult_rural * mult_dumbass) * mult_sanity));
						//	//experience[Experience.Type.Endurance].MulS((mult_health * mult_rural * mult_laborer * mult_dumbass));
						//	//experience[Experience.Type.Strength].MulS(mult_fighter * mult_rural * mult_laborer * mult_dumbass);
						//	//experience[Experience.Type.Charisma].MulS((mult_fancy * mult_nobility * mult_social * mult_smart * mult_health) / (MathF.Max(1.00f, mult_loser) * mult_dumbass));
						//	//experience[Experience.Type.Engineering].MulS(mult_smart * mult_urban * mult_tech * mult_expert * mult_builder);
						//	//experience[Experience.Type.Leadership].MulS(mult_smart * mult_social * mult_fancy * mult_fighter * mult_evil * mult_nobility);
						//	//experience[Experience.Type.Commerce].MulS(mult_wealth * mult_urban * mult_social * mult_academic * mult_fancy);

						//}

						ICharacterModifier.Apply(ref props, in vars, (x, flags) => x.args.character_flags.HasAny(flags));

						if (species_data.IsNotNull())
						{
							//hair_color = Color32BGRA.Saturate(hair_color, 1.00f - Maths.InvLerp01(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, age));
							props.hair_color = Color32BGRA.Lerp(props.hair_color, Color32BGRA.White, Maths.InvLerp01(Maths.Lerp(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, 0.70f), species_data.lifecycle.age_elder * 1.40f, props.age * props.visual_age_mult));
						}
						props.hair_color.a = 255;

						using (var group_left = GUI.Group.New(size: new(GUI.RmX - 244, GUI.RmY)))
						{
							using (var group_top = GUI.Group.New(size: new(GUI.RmX, 96)))
							{
								using (var group_head = GUI.Group.New(size: new(GUI.RmY), padding: new(4)))
								{
									group_head.DrawBackground(GUI.tex_window);

									GUI.DrawCharacterHead(props.sprite_head, props.sprite_hair, props.sprite_beard, props.hair_color, GUI.Rm, scale: 5.00f);
								}

								GUI.SameLine();

								using (var group_a = GUI.Group.New(size: GUI.Rm))
								{
									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
									{
										group_b.DrawBackground(GUI.tex_window);

										if (GUI.AssetInput2("edit.species", ref vars.h_species, size: new(160, GUI.RmY), show_label: false, tab_height: 24.00f, close_on_select: true,
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

										if (GUI.AssetInput2("edit.origin", ref vars.h_origin, size: new(GUI.RmX, GUI.RmY), show_label: false, tab_height: 40.00f, close_on_select: true,
										filter: (x) => x.data.species == custom_character.vars.h_species && x.data.flags.HasAll(IOrigin.Flags.Special),
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
											vars.character_flags = default;
											//reset = true;
										}
									}

									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										if (GUI.DrawIconButton("edit.gender.male", new Sprite("ui_icons_gender", 16, 16, 1, 0), size: new(GUI.RmY), color: vars.gender == Organic.Gender.Male ? GUI.col_button_highlight : null))
										{
											vars.gender = Organic.Gender.Male;
										}

										GUI.SameLine();

										if (GUI.DrawIconButton("edit.gender.female", new Sprite("ui_icons_gender", 16, 16, 2, 0), size: new(GUI.RmY), color: vars.gender == Organic.Gender.Female ? GUI.col_button_highlight : null))
										{
											vars.gender = Organic.Gender.Female;
										}

										GUI.SameLine();

										GUI.AssetInput2("edit.hair", ref h_selected_hair, size: new(160, GUI.RmY), show_label: false, tab_height: 64.00f, close_on_select: false,
											filter: (x) => x.data.species == custom_character.vars.h_species && x.data.gender == custom_character.vars.gender && x.data.flags.HasAll(IHair.Flags.Hair)
											&& (x.data.character_flags.IsEmpty() || custom_character.vars.character_flags.HasAny(x.data.character_flags)) && (x.data.character_flags_exclude.IsEmpty() || !custom_character.vars.character_flags.HasAny(x.data.character_flags_exclude)),
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
														GUI.DrawCharacterHead(custom_character.props.sprite_head, asset.data.sprite, custom_character.props.sprite_beard, custom_character.props.hair_color, group.size, scale: 3.00f, draw_frame: true);
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
											filter: (x) => x.data.species == custom_character.vars.h_species && x.data.gender == custom_character.vars.gender && x.data.flags.HasAll(IHair.Flags.Beard)
											&& (x.data.character_flags.IsEmpty() || custom_character.vars.character_flags.HasAny(x.data.character_flags)) && (x.data.character_flags_exclude.IsEmpty() || !custom_character.vars.character_flags.HasAny(x.data.character_flags_exclude)),
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
														GUI.DrawCharacterHead(custom_character.props.sprite_head, custom_character.props.sprite_hair, asset.data.sprite, custom_character.props.hair_color, group.size, scale: 3.00f, draw_frame: true);
													}
													//GUI.DrawSpriteCentered(asset.data.sprite, AABB.Simple(group.a, new Vector2(group.size.Y)), layer: GUI.Layer.Window, scale: 3.00f, color: hair_color);
												}
												else
												{
													GUI.TitleCentered($"Beard:\n<none>", pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
												}
											});

										GUI.SameLine();

										GUI.SliderFloat("Hair Color", ref vars.hair_color_ratio, 0.00f, 1.00f, size: GUI.Rm);
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

										GUI.SliderIntLerp("Age", ref vars.age_ratio, props.age_min, props.age_max, size: new(128, 24), show_label: true);

										GUI.NewLine();
										GUI.SeparatorThick();

										{
											var max_flag_count = props.character_flags_default.GetCount() + Math.Min(4, props.character_flags_optional.GetCount());
											GUI.EnumInput("flags.character", ref vars.character_flags, size: new(w, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.character_flags_optional, required: props.character_flags_default);
										}

										{
											var max_flag_count = props.industry_flags_default.GetCount() + Math.Min(4, props.industry_flags_optional.GetCount());
											GUI.EnumInput("flags.industry", ref vars.industry_flags, size: new(w, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.industry_flags_optional, required: props.industry_flags_default);
										}

										{
											var max_flag_count = props.service_flags_default.GetCount() + Math.Min(4, props.service_flags_optional.GetCount());
											GUI.EnumInput("flags.service", ref vars.service_flags, size: new(w, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.service_flags_optional, required: props.service_flags_default);
										}

										{
											var max_flag_count = props.crime_flags_default.GetCount() + Math.Min(4, props.crime_flags_optional.GetCount());
											GUI.EnumInput("flags.crime", ref vars.crime_flags, size: new(w, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.crime_flags_optional, required: props.crime_flags_default);
										}
									}
								}
							}
						}

						GUI.SameLine();

						using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
						{
							group_right.DrawBackground(GUI.tex_window);

							using (var scrollbox = GUI.Scrollbox.New("scroll.experience", size: new(GUI.RmX, GUI.RmY - 244)))
							{
								Experience.DrawTableSmall2(ref props.experience);
							}

							GUI.SeparatorThick();
						}

						if (reset)
						{
							vars.h_origin = default;

							vars.character_flags = default;
							vars.industry_flags = default;
							vars.service_flags = default;
							vars.crime_flags = default;

							vars.h_hair_male = default;
							vars.h_beard_male = default;

							vars.h_hair_female = default;
							vars.h_beard_female = default;


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

