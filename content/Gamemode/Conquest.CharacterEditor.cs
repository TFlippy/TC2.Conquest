﻿
using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
		public struct CreateCharacterRPC: Net.IGRPC<Conquest.Gamemode>
		{
			public CustomCharacter.Vars vars;

			public Entity ent_character_out;
			public ICharacter.Handle h_character_out;

#if SERVER
			public void Invoke(ref NetConnection connection, ref Conquest.Gamemode data)
			{
				ref var player = ref connection.GetPlayer(out var player_asset);
				Assert.IsNotNull(ref player, Assert.Level.Error);

				ref var region = ref connection.GetRegionCommon();
				Assert.IsNotNull(ref region, Assert.Level.Error);

				ref var g_world = ref region.GetGlobalComponent<World.Global>();
				Assert.IsNotNull(ref g_world);

				Assert.Check(g_world.time_total >= player.t_next_respawn);
				
				Assert.Check(this.vars.h_species.IsValid());
				Assert.Check(this.vars.h_origin.IsValid());
				Assert.Check(this.vars.h_location.IsValid());

				var random = XorRandom.New(true);

				var props = new CustomCharacter.Props();

				CustomCharacter.Apply(ref this.vars, ref props);

				var character = new ICharacter.Data();
				character.origin = this.vars.h_origin;
				//character.h_location_current = this.vars.h_location;
				character.faction = player.h_faction;
				character.species = this.vars.h_species;
				character.h_location_home = this.vars.h_location;

				character.gender = this.vars.gender;
				character.prefab = props.h_prefab;
				character.players = [player_asset.GetHandle()];

				//character.money = props.money;
				character.age = props.age;
				character.traits = this.vars.character_flags;
				character.flags = Character.Flags.Main;
				character.experience = props.experience;

				character.hair_color = props.hair_color;
				character.hair = this.vars.gender == Organic.Gender.Female ? this.vars.h_hair_female : this.vars.h_hair_male;
				character.beard = this.vars.gender == Organic.Gender.Female ? this.vars.h_beard_female : this.vars.h_beard_male;
				character.sprite_head = props.sprite_head;

				var name = Spawner.GenerateName(ref random, props.species_flags, character.traits, character.gender);
				character.name = name;

				var kits_span = FixedArray.CreateSpan16<IKit.Handle>(out var kits_buffer);
				var kits_count = 0;

				kits_span.AddIfValid(this.vars.h_kit_primary, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_secondary, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_tool, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_utility, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_resources, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_harness, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_head, ref kits_count);
				kits_span.AddIfValid(this.vars.h_kit_chest, ref kits_count);

				var kits_span_sliced = kits_span.WithLength(kits_count);
				character.kits = kits_span_sliced.ToArray();

				var identifier = $"m.{character.species.GetIdentifier().OrDefault("misc")}.{Asset.GenerateRandomIdentifier()}";
				//App.WriteLine(identifier);

				Prefab.Handle h_prefab_unit = default;
				ref var species = ref character.species.GetData();
				if (species.IsNotNull())
				{
					h_prefab_unit = species.prefab_unit;
				}

				if (!h_prefab_unit)
				{
					h_prefab_unit = "unit.guy";
				}

				var character_asset = ICharacter.Database.RegisterOrUpdate(identifier,
					index: null,
					scope: Asset.Scope.World,
					h_prefab: h_prefab_unit,
					h_prefab_region: "region.character",
					region_id: 0,
					data: ref character);

				character_asset.Sync(true);

				var h_location = this.vars.h_location;

				var ent_location = h_location.GetGlobalEntity();
				var ent_character = character_asset.GetGlobalEntity();

				var h_kit_vehicle = this.vars.h_kit_vehicle;
				var h_character = character_asset.GetHandle();
				var h_prefab_vehicle = default(Prefab.Handle);

				if (h_kit_vehicle.TryGetDefinition(out var kit_vehicle) && kit_vehicle.data.slot == Kit.Slot.Vehicle)
				{
					h_prefab_vehicle = kit_vehicle.data.shipment.items[0].prefab;

					//var product = kit_vehicle.data.shipment.items[0].prefab;
					//if (Assert.Check(product.IsValid(), level: Assert.Level.Warn))
					//{
					//	h_prefab_vehicle = product.prefab;
					//}
				}

				var money = props.money;

				character_asset.SpawnEntity(0).ContinueWith((ent) =>
				{
					ref var transform = ref ent.GetComponent<Transform.Data>();
					if (transform.IsNotNull())
					{
						if (h_location.TryGetDefinition(out var location_asset))
						{
							var pos = (Vector2)location_asset.data.point;
							if (h_location.TryGetRoad(out var road))
							{
								pos = road.GetNearestPosition(pos, out _);


								//Crafting.Context.NewFromCharacter(ref World.GetGlobalRegion().AsCommon(), h_character, ent_producer: ent_location_global, out var context, search_radius: 0.00f);
								//if (h_recipe_vehicle.TryGetDefinition(out var recipe_vehicle) && recipe_vehicle.data.flags.HasAny(Crafting.Recipe.Flags.Overworld))
								//{
								//	var product = recipe_vehicle.data.products.FirstOrDefault(x => x.type == Crafting.Product.Type.Prefab);
								//	Assert.Check(product.IsValid());

								//	var h_prefab_vehicle = 

								//	//Crafting.Produce()
								//}
							}

							ref var region_global = ref World.GetGlobalRegion();
							region_global.SpawnPrefab(h_prefab_vehicle, position: pos).ContinueWith((ent_vehicle) =>
							{
								ref var enterable = ref ent_vehicle.GetComponent<WorldMap.Enterable.Data>();
								if (enterable.IsNotNull())
								{
									WorldMap.Unit.TryEnter(ent_character, ent_vehicle);
								}

								if (ent_vehicle.TryGetInventory(Inventory.Type.Storage, out var h_inventory))
								{
									var resource_money = Money.GetResource(money);
									h_inventory.Deposit(ref resource_money, resource_money.quantity);
								}
							});

							transform.SetPosition(pos);
						}
					}
				});

				//ent_asset.AddRelation(ent_location, Relation.Type.Child);

				player.t_next_respawn = g_world.time_total + props.cooldown;
				player.h_character_main = character_asset;
				player_asset.Sync();

				this.ent_character_out = ent_character;
				this.h_character_out = h_character;
			}
#endif
		}

		public sealed class CustomCharacter
		{
			public struct Vars()
			{
				public ISpecies.Handle h_species;
				public ILocation.Handle h_location;
				public IOrigin.Handle h_origin;

				public IHair.Handle h_hair_male;
				public IHair.Handle h_beard_male;

				public IHair.Handle h_hair_female;
				public IHair.Handle h_beard_female;

				public Character.Traits character_flags;
				public IMap.Industry industry_flags;
				public IMap.Services service_flags;
				public IMap.Crime crime_flags;

				public float hair_color_ratio;
				public float age_ratio;

				public uint name_seed;

				public IKit.Handle h_kit_primary;
				public IKit.Handle h_kit_secondary;
				public IKit.Handle h_kit_tool;
				public IKit.Handle h_kit_utility;
				public IKit.Handle h_kit_head;
				public IKit.Handle h_kit_chest;
				public IKit.Handle h_kit_resources;
				public IKit.Handle h_kit_harness;
				public IKit.Handle h_kit_vehicle;

				public Organic.Gender gender = Organic.Gender.Male;

				public override readonly int GetHashCode()
				{
					return HashCode.Combine(this.h_species, this.h_origin, this.gender);
				}
			}

			public struct Props()
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

				public Prefab.Handle h_prefab;
				public Experience.Levels experience;

				public int age_min = 0;
				public int age_max = 1;
				public int age = 0;

				public ImperialDateTime date_birth;

				public float visual_age_mult = 1.00f;

				public float money = 0.00f;
				public float cost = 0.00f;
				public float cooldown = 0.00f;

				public ISpecies.Flags species_flags;

				public Character.Traits character_flags_default;
				public Character.Traits character_flags_optional;

				public IMap.Industry industry_flags_default;
				public IMap.Industry industry_flags_optional;

				public IMap.Services service_flags_default;
				public IMap.Services service_flags_optional;

				public IMap.Crime crime_flags_default;
				public IMap.Crime crime_flags_optional;
			}

			public CustomCharacter.Vars vars = new();
			public CustomCharacter.Props props = new();

			public CustomCharacter()
			{

			}

			public CustomCharacter(ILocation.Handle h_location, ISpecies.Handle h_species, IOrigin.Handle h_origin, Organic.Gender gender, IHair.Handle h_hair_male, IHair.Handle h_hair_female, IHair.Handle h_beard_male, IHair.Handle h_beard_female)
			{
				var random = XorRandom.New(true);

				this.vars.h_location = h_location;
				this.vars.h_species = h_species;
				this.vars.h_origin = h_origin;
				this.vars.h_kit_vehicle = "overworld.car.00";
				this.vars.gender = gender;

				this.vars.h_hair_male = h_hair_male;
				this.vars.h_hair_female = h_hair_female;

				this.vars.h_beard_male = h_beard_male;
				this.vars.h_beard_female = h_beard_female;

				this.vars.age_ratio = random.NextFloat01();
				this.vars.hair_color_ratio = random.NextFloat01();

				this.vars.name_seed = random.NextUInt();
			}

			public static void Apply(ref Vars vars, ref Props props)
			{
				ref var species_data = ref vars.h_species.GetData();
				ref var origin_data = ref vars.h_origin.GetData();
				ref var location_data = ref vars.h_location.GetData();

				ref var h_selected_hair = ref (vars.gender == Organic.Gender.Female ? ref vars.h_hair_female : ref vars.h_hair_male);
				ref var h_selected_beard = ref (vars.gender == Organic.Gender.Female ? ref vars.h_beard_female : ref vars.h_beard_male);

				ref var hair_data = ref h_selected_hair.GetData();
				ref var beard_data = ref h_selected_beard.GetData();
				ref var vehicle_data = ref vars.h_kit_vehicle.GetData();

				vars.age_ratio.Clamp01Ref();
				vars.hair_color_ratio.Clamp01Ref();

				if (species_data.IsNotNull())
				{
					props.sprite_head = vars.gender == Organic.Gender.Female ? species_data.sprite_head_female : species_data.sprite_head_male;
					props.hair_color = species_data.hair_colors.AsSpan().GetLerped(vars.hair_color_ratio);
					props.h_prefab = vars.gender == Organic.Gender.Female ? species_data.prefab_female : species_data.prefab_male;
					props.species_flags = species_data.flags;
				}

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

				if (hair_data.IsNotNull())
				{
					props.sprite_hair = hair_data.sprite;
				}

				if (beard_data.IsNotNull())
				{
					props.sprite_beard = beard_data.sprite;
				}

				if (vehicle_data.IsNotNull())
				{
					props.cost += vehicle_data.cost;
				}

				if (location_data.IsNotNull())
				{
					ref var stats = ref location_data.statistics;
					props.money += ((props.money * Maths.Avg(stats.economy, stats.wealth)) * stats.urbanization) * 0.25f;
				}

				ICharacterModifier.Apply(ref props, in vars, (x, flags) => x.args.character_flags.HasAny(flags));

				if (species_data.IsNotNull())
				{
					//hair_color = Color32BGRA.Saturate(hair_color, 1.00f - Maths.InvLerp01(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, age));
					props.hair_color = Color32BGRA.Lerp(props.hair_color, Color32BGRA.White.WithAlpha(40), Maths.InvLerp01(Maths.Lerp(species_data.lifecycle.age_mature, species_data.lifecycle.age_elder, 0.70f), species_data.lifecycle.age_elder * 1.40f, props.age * props.visual_age_mult));
				}
				//props.hair_color.a = 255;

				props.money = props.money.SnapCeil(11);
				props.cooldown = (MathF.Pow(props.cost + MathF.Pow(props.money, 0.86f), 1.10f)).SnapCeil(60.00f * 5);
			}
		}

		public interface ICharacterModifier: IModifier<ICharacterModifier, CustomCharacter.Props, CustomCharacter.Vars, Character.Traits>
		{

		}

		//public interface ICharacterStatus: IModifier<ICharacterStatus, CustomCharacter.Props, CustomCharacter.Vars, ILocation.Statistics>
		//{

		//}

		public static class Modifiers
		{
			public static ICharacterModifier.Function modifier_alcoholic =
			[ICharacterModifier.Info(Character.Traits.Alcoholic, order: -1)] static (x) =>
			{
				x.value.bias_health -= 0.32f; // liver doesn't like booze
				x.value.bias_loser += 0.21f; // sleeps in a ditch
				x.value.bias_fancy -= 0.25f; // smells like piss
				x.value.bias_sanity -= 0.15f; // hangover
				x.value.mult_wealth *= 0.88f; // spends money on booze

				x.value.visual_age_mult += 0.11f;
			};

			public static ICharacterModifier.Function modifier_strong =
			[ICharacterModifier.Info(Character.Traits.Strong, order: 1)] static (x) =>
			{
				x.value.experience[Experience.Type.Strength].MulS(1.14f);
				x.value.experience[Experience.Type.Strength].AddS(3);
				x.value.experience[Experience.Type.Endurance].AddS(2);
			};

			public static ICharacterModifier.Function modifier_brawler =
			[ICharacterModifier.Info(Character.Traits.Brawler, order: 5)] static (x) =>
			{
				x.value.experience[Experience.Type.Intellect].AddS(-3); // brain damage
				x.value.experience[Experience.Type.Strength].AddS(3);
				x.value.experience[Experience.Type.Endurance].AddS(2);
				x.value.experience[Experience.Type.Endurance].MulS(1.21f);
				x.value.experience[Experience.Type.Charisma].MulS(0.87f); // beaten up and missing some teeth
			};

			//public static ICharacterModifier.Function modifier_test = static x => x.value.age = 4;

			//public static void Derp()
			//{
			//	static int Foof()
			//	{
			//		var b = 2;
			//		return b;
			//	}

			//	var t = Foof();
			//	var meth = Foof;

			//	App.WriteLine(meth.Method.DeclaringType);
			//}
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

			public static CustomCharacter custom_character = new("krachtel", "human", "human.adventurer", Organic.Gender.Male, "human.male.wings", "human.female.updo", "human.male.beard.bartender", default);
			public static Sprite icons_gender = new Sprite("ui_icons_gender", 16, 16, 0, 0);

			public void Draw()
			{
				//var size = new Vector2(48 * 18, 600);

				//using (var window = GUI.Window.Standalone("CreateCharacter"u8, size: size, position: new(GUI.CanvasSize.X * 0.50f, GUI.CanvasSize.Y * 0.50f), pivot: new(0.50f, 0.50f), padding: new(8, 8), force_position: false))
				using (var group_main = GUI.Group.New(size: GUI.Av))
				{
					//this.StoreCurrentWindowTypeID();
					//if (window.show)
					{
						//GUI.DrawWindowBackground(GUI.tex_window_character, new Vector4(16, 16, 16, 16));

						var reset = false;

						ref var vars = ref custom_character.vars;
						ref var props = ref custom_character.props;
						props = new();

						ref var h_selected_hair = ref (vars.gender == Organic.Gender.Female ? ref vars.h_hair_female : ref vars.h_hair_male);
						ref var h_selected_beard = ref (vars.gender == Organic.Gender.Female ? ref vars.h_beard_female : ref vars.h_beard_male);

						CustomCharacter.Apply(ref vars, ref props);

						using (var group_left = GUI.Group.New(size: new(GUI.RmX - 244, GUI.RmY)))
						{
							using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
							{
								group_b.DrawBackground(GUI.tex_window);

								if (GUI.AssetInput2("edit.location"u8, ref vars.h_location, size: new(GUI.RmX, GUI.RmY), show_label: false, tab_height: 40.00f, close_on_select: false,
								filter: static (x) => x.data.flags.HasNone(ILocation.Flags.Hidden | ILocation.Flags.Restricted) && x.data.buildings.HasAny(ILocation.Buildings.Train_Station | ILocation.Buildings.Trainyard | ILocation.Buildings.Fuel_Depot | ILocation.Buildings.Docks | ILocation.Buildings.Hotel | ILocation.Buildings.Apartments | ILocation.Buildings.Barracks) && x.data.flags.HasAny(ILocation.Flags.Spawn),
								draw: (asset, group, is_title) =>
								{
									if (asset != null)
									{
										using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
										{
											using (var clip = GUI.Clip.Push(group_icon.GetInnerRect()))
											{
												if (asset.data.thumbnail.texture.id != 0)
												{
													GUI.DrawSpriteCentered(asset.data.thumbnail, clip.rect, GUI.Layer.Window, scale: 0.25f);
												}
												else
												{
													GUI.DrawSpriteCentered(asset.data.icon, clip.rect, GUI.Layer.Window, scale: 2.00f, color: asset.data.color.WithAlpha(255));
												}
											}
											group_icon.DrawBackground(GUI.tex_frame);
										}

										GUI.SameLine();

										GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);

										ref var prefecture_data = ref asset.data.h_prefecture.GetData();
										if (prefecture_data.IsNotNull())
										{
											GUI.TitleCentered(prefecture_data.name, pivot: new(1.00f, 0.50f), offset: new(-8, 0), size: 16);
										}

										if (GUI.IsHoveringRect(group.GetOuterRect()))
										{
											using (var tooltip = GUI.Tooltip.New(size: new(300, 0)))
											{
												using (GUI.Wrap.Push(GUI.RmX))
												{
													GUI.TextShaded(asset.data.desc);
												}
											}
										}
									}
									else
									{
										GUI.TitleCentered("<location>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
									}
								}))
								{
									WorldMap.FocusLocation(vars.h_location, interact: false);
									//reset = true;
								}
							}

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

										if (GUI.AssetInput2("edit.species"u8, ref vars.h_species, size: new(160, GUI.RmY), show_label: false, tab_height: 24.00f, close_on_select: true,
										filter: static (x) => x.data.flags.HasAny(ISpecies.Flags.Sapient) && x.data.flags.HasNone(ISpecies.Flags.Feral | ISpecies.Flags.Wild),
										draw: (asset, group, is_title) =>
										{
											if (asset != null)
											{
												GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
											else
											{
												GUI.TitleCentered("<species>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
										}))
										{
											reset = true;
										}

										GUI.SameLine();

										if (GUI.AssetInput2("edit.origin"u8, ref vars.h_origin, size: new(GUI.RmX, GUI.RmY), show_label: false, tab_height: 40.00f, close_on_select: false,
										filter: static (x) => x.data.species == custom_character.vars.h_species && x.data.flags.HasAny(IOrigin.Flags.Special | IOrigin.Flags.Selectable),
										draw: (asset, group, is_title) =>
										{
											if (asset != null)
											{
												GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
											else
											{
												GUI.TitleCentered("<origin>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 24);
											}
										}))
										{
											vars.character_flags = default;
											//reset = true;
										}
									}

									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										if (GUI.DrawIconButton("edit.gender.male"u8, icons_gender.WithFrame(1, 0), size: new(GUI.RmY), color: vars.gender == Organic.Gender.Male ? GUI.col_button_highlight : null))
										{
											vars.gender = Organic.Gender.Male;
										}

										GUI.SameLine();

										if (GUI.DrawIconButton("edit.gender.female"u8, icons_gender.WithFrame(2, 0), size: new(GUI.RmY), color: vars.gender == Organic.Gender.Female ? GUI.col_button_highlight : null))
										{
											vars.gender = Organic.Gender.Female;
										}

										GUI.SameLine();

										GUI.AssetInput2("edit.hair"u8, ref h_selected_hair, size: new(160, GUI.RmY), show_label: false, tab_height: 64.00f, close_on_select: false,
											filter: static (x) => x.data.species == custom_character.vars.h_species && x.data.gender == custom_character.vars.gender && x.data.flags.HasAny(IHair.Flags.Hair)
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
													group.DrawHoverTooltip(in asset.data, static x => GUI.TextShaded(x.arg.desc));
													//GUI.DrawSpriteCentered(asset.data.sprite, AABB.Simple(group.a, new Vector2(group.size.Y)), layer: GUI.Layer.Window, scale: 3.00f, color: hair_color);
												}
												else
												{
													GUI.TitleCentered("Hair:\n<none>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
												}
											});

										GUI.SameLine();

										GUI.AssetInput2("edit.beard"u8, ref h_selected_beard, size: new(160, GUI.RmY), show_label: false, tab_height: 64.00f, close_on_select: false,
											filter: (x) => x.data.species == custom_character.vars.h_species && x.data.gender == custom_character.vars.gender && x.data.flags.HasAny(IHair.Flags.Beard)
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
													group.DrawHoverTooltip(in asset.data, static x => GUI.TextShaded(x.arg.desc));
													//GUI.DrawSpriteCentered(asset.data.sprite, AABB.Simple(group.a, new Vector2(group.size.Y)), layer: GUI.Layer.Window, scale: 3.00f, color: hair_color);
												}
												else
												{
													GUI.TitleCentered("Beard:\n<none>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16);
												}
											});

										GUI.SameLine();

										GUI.SliderFloat("Hair Color"u8, ref vars.hair_color_ratio, 0.00f, 1.00f, size: GUI.Rm);
									}
								}
							}

							using (var group_mid = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(4)))
							{
								group_mid.DrawBackground(GUI.tex_window_menu);

								using (var group_a = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
								{
									using (var group_b = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										var w = GUI.RmX;

										//using (GUI.Group.New(size: new(w * 0.50f, 32)))
										//{
										//	GUI.SliderIntLerp("Age", ref edit_info.age_ratio, age_min, age_max, size: new(GUI.RmX * 0.75f, GUI.RmY));
										//}

										GUI.SliderIntLerp("Age"u8, ref vars.age_ratio, props.age_min, props.age_max, size: new(128, 24), show_label: true);

										// GUI.NewLine();
										GUI.SeparatorThick();

										{
											var max_flag_count = props.character_flags_default.GetCount() + Math.Min(4, props.character_flags_optional.GetCount());
											GUI.EnumInput("flags.character"u8, ref vars.character_flags, size: new(w * 0.50f, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.character_flags_optional, required: props.character_flags_default, columns: 5);
										}
										GUI.SameLine();
										{
											var max_flag_count = props.industry_flags_default.GetCount() + Math.Min(4, props.industry_flags_optional.GetCount());
											GUI.EnumInput("flags.industry"u8, ref vars.industry_flags, size: new(w * 0.50f, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.industry_flags_optional, required: props.industry_flags_default, columns: 5);
										}

										{
											var max_flag_count = props.service_flags_default.GetCount() + Math.Min(4, props.service_flags_optional.GetCount());
											GUI.EnumInput("flags.service"u8, ref vars.service_flags, size: new(w * 0.50f, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.service_flags_optional, required: props.service_flags_default, columns: 5);
										}
										GUI.SameLine();
										{
											var max_flag_count = props.crime_flags_default.GetCount() + Math.Min(4, props.crime_flags_optional.GetCount());
											GUI.EnumInput("flags.crime"u8, ref vars.crime_flags, size: new(w * 0.50f, 32), show_label: false, height: 256,
												max_flags: max_flag_count, mask: props.crime_flags_optional, required: props.crime_flags_default, columns: 5);
										}
									}
								}

								GUI.SeparatorThick();

								using (var group_loadout = GUI.Group.New(GUI.Rm))
								{
									using (var group_row = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
									{
										using (var group_kits = GUI.Group.New(size: new(GUI.RmX * 0.60f, GUI.RmY)))
										{
											var w = group_kits.size.X;

											static bool ValidateKit(ref IKit.Data kit_data, Kit.Slot slot, ref CustomCharacter.Vars vars, ref CustomCharacter.Props props, bool skip_flags = false)
											{
												return kit_data.slot == slot
													&& !kit_data.faction
													&& kit_data.flags.HasAnyExcept(Kit.Flags.Selectable, Kit.Flags.Hidden)
													&& kit_data.species.IsSameOrEmpty(vars.h_species)
													&& (skip_flags || kit_data.character_flags.Evaluate(props.character_flags_default | vars.character_flags) > 0.00f);
											}

											static void DrawKit(IKit.Definition asset, GUI.Group group, bool is_title)
											{
												Dormitory.DrawKit(h_kit: asset?.GetHandle() ?? default,
													rect: group.GetInnerRect(),
													valid: true, //asset?.data.character_flags.Evaluate(custom_character.props.character_flags_default) > 0.00f,
													selected: false,
													force_readonly: true,
													ignore_requirements: true,
													no_select: true,
													show_type: true,
													show_background: true);
											}

											GUI.AssetInput2("edit.h_kit_primary"u8, ref vars.h_kit_primary, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Primary, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.SameLine();

											GUI.AssetInput2("edit.h_kit_secondary"u8, ref vars.h_kit_secondary, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Secondary, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.AssetInput2("edit.h_kit_tool"u8, ref vars.h_kit_tool, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Tool, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.SameLine();

											GUI.AssetInput2("edit.h_kit_utility"u8, ref vars.h_kit_utility, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Utility, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.AssetInput2("edit.h_kit_head"u8, ref vars.h_kit_head, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Head, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.SameLine();

											GUI.AssetInput2("edit.h_kit_chest"u8, ref vars.h_kit_chest, size: new(w * 0.50f, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Chest, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);

											GUI.AssetInput2("edit.h_kit_harness"u8, ref vars.h_kit_harness, size: new(w, 48), show_label: false, tab_height: 40, close_on_select: true, show_null: true,
												filter: static (x) => ValidateKit(ref x.data, Kit.Slot.Harness, ref custom_character.vars, ref custom_character.props, skip_flags: false),
												draw: DrawKit);
										}

										GUI.SameLine();

										using (var group_items = GUI.Group.New(size: GUI.Rm, padding: new(4)))
										{
											group_items.DrawBackground(GUI.tex_panel);

											if (GUI.AssetInput2("edit.vehicle"u8, ref vars.h_kit_vehicle, size: new(GUI.RmX, 64), show_label: false, tab_height: 64.00f, close_on_select: true,
											filter: static (x) => x.data.slot == Kit.Slot.Vehicle && x.data.flags.HasAll(Kit.Flags.Overworld),
											draw: (asset, group, is_title) =>
											{
												if (asset != null)
												{
													using (var group_icon = GUI.Group.New(size: new(80, GUI.RmY)))
													{
														using (var clip = GUI.Clip.Push(group_icon.GetInnerRect()))
														{
															GUI.DrawSpriteCentered(asset.data.icon, clip.rect, GUI.Layer.Window, scale: 3.00f);
														}
														group_icon.DrawBackground(GUI.tex_frame);
													}

													GUI.SameLine();

													using (var group_right = GUI.Group.New(size: GUI.Rm))
													{
														GUI.TitleCentered(asset.data.name, rect: group_right.GetInnerRect(), pivot: new(0.00f, 0.00f), offset: new(4, 4), size: 24);
													}
												}
												else
												{
													GUI.TitleCentered("<vehicle>"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 24);
												}
											}))
											{
												//reset = true;
											}

											GUI.SeparatorThick();

											GUI.NewLine(8);

											GUI.DrawMoney(props.money, size: new(GUI.RmX, 24));
										}
									}
								}


							}
						}

						GUI.SameLine();

						using (var group_right = GUI.Group.New(size: GUI.Rm))
						{
							using (var group_top = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 48), padding: new(4)))
							{
								group_top.DrawBackground(GUI.tex_window);

								using (var scrollbox = GUI.Scrollbox.New("scroll.experience"u8, size: new(GUI.RmX, GUI.RmY - 64)))
								{
									Experience.DrawTableSmall2(ref props.experience);
								}

								GUI.SeparatorThick();

								using (var group_bottom = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(4)))
								{
									GUI.TitleCentered("Cooldown"u8, pivot: new(0.50f, 0.00f), offset: new(0, 0), size: 32);
									GUI.TitleCentered(GUI.FormatTime(props.cooldown), pivot: new(0.50f, 1.00f), offset: new(0, 0), size: 24);
								}
							}

							ref var player_data = ref Client.GetPlayerData(out var player_asset);
							if (player_data.IsNotNull())
							{
								ref var region_global = ref World.GetGlobalRegion();
								if (region_global.IsNotNull())
								{
									ref var g_world = ref region_global.GetGlobalComponent<World.Global>();
									if (g_world.IsNotNull())
									{
										var respawn_seconds_rem = player_data.t_next_respawn - g_world.time_total;
										var respawn_seconds_rem_clamped = Maths.ClampMin(respawn_seconds_rem, 0.00);

										if (respawn_seconds_rem <= 0.00f)
										{
											var is_valid = vars.h_origin.IsValid() && vars.h_species.IsValid() && vars.h_location.IsValid() && vars.h_kit_vehicle.IsValid();
											if (GUI.DrawConfirmButton("character.create"u8, "Create Character"u8, "Do you want to create\n    this character?"u8, size: GUI.Rm, font_size: 24, color: GUI.col_button_ok, enabled: is_valid))
											{
												var rpc = new Conquest.CreateCharacterRPC();
												rpc.vars = vars;
												rpc.SendAsTask().ContinueWith((x) =>
												{
													//WorldMap.FocusEntity(x.ent_character_out);
													WorldMap.SelectEntity(x.ent_character_out);
												});
											}
										}
										else
										{
											if (GUI.DrawButton("Create Character"u8, size: GUI.Rm, font_size: 24, color: GUI.col_button_error, error: true))
											{
												
											}

											if (GUI.IsItemHovered())
											{
												using (var tooltip = GUI.Tooltip.New())
												{
													GUI.Title("Time until next character"u8, size: 22);
													using (GUI.Group.New(size: new(GUI.RmX, 24)))
													{
														GUI.TitleCentered(GUI.FormatTime((float)respawn_seconds_rem_clamped), pivot: new(0.50f), size: 20);
													}
												}
											}
										}
									}
								}
							}
						}

						if (reset)
						{
							vars.h_origin = default;
							vars.h_kit_vehicle = default;

							vars.character_flags = default;
							vars.industry_flags = default;
							vars.service_flags = default;
							vars.crime_flags = default;

							vars.h_hair_male = default;
							vars.h_beard_male = default;

							vars.h_hair_female = default;
							vars.h_beard_female = default;
						}
					}
				}
			}
		}

		//public const bool debug_log_character_switch = false;

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Global)]
		public static void OnCharacterGUI(ISystem.Info.Global info, ref Region.Data.Global region, [Source.Owned] ref World.Global world)
		{
			ref var player_data = ref Client.GetPlayerData(out var player_asset);
			if (player_data.IsNotNull())
			{
				var h_character = player_data.h_character_main;
				var h_character_current = Client.GetCharacterHandle();

				ref var character_data = ref h_character.GetData(out var character_asset);
				if (character_data.IsNotNull())
				{
					var color = GUI.col_button_yellow;

					var is_selected = h_character_current == h_character && ((Client.GetRegionID() == character_asset.region_id && character_asset.region_id > 0) || !WorldMap.IsOpen || WorldMap.hs_selected_entities.Contains(h_character.GetGlobalEntity()));
					using (var widget = Sidebar.Widget.New(identifier: "character.main", name: character_data.name, icon: character_data.sprite_head, size: new Vector2(48 * 6, 48 * 4), has_window: false, show_as_selected: is_selected, color: color, order: (10.00f - 0.10f), flags: Sidebar.Widget.Flags.Starts_Open))
					{
						widget.func_draw = (widget, group, icon_color) =>
						{
							GUI.DrawCharacterHead(h_character: h_character, group.GetInnerRect(), color: icon_color);
							GUI.FocusableAsset(h_character);
						};

						var kb = GUI.GetKeyboard();
						if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show)) // && !is_selected)
						{
							//if (widget.IsAppearing()) WorldMap.FocusLocation(Conquest.CreationGUI.custom_character.vars.h_location);

							//App.WriteLine($"switch begin ({h_character_current} to {h_character})", color: App.Color.Magenta);
							//Client.SetCharacter(h_character, true, force: !is_selected);
							//if (h_character_current != h_character | !is_selected)
							//{
							//	Client.SetCharacter(h_character, true);
							//	//var rpc = new Character.SwitchRPC()
							//	//{
							//	//	h_character = h_character
							//	//};
							//	//rpc.Send();
							//}

							Client.SetCharacter(h_character, true, force: !is_selected).WaitForRender().ContinueWith((x) =>
							{
								var h_character = x.h_character;
								//App.WriteLine($"switch done ({h_character_current} to {h_character}; {x.h_character})", color: App.Color.Magenta);

								var ent_character_global = h_character.GetGlobalEntity();
								if (ent_character_global.IsAlive())
								{
									if (ent_character_global.TryGetParent(Relation.Type.Stored, out var ent_character_parent))
									{
										if (ent_character_parent.TryGetAsset(out ILocation.Definition location_asset))
										{
											var h_location = location_asset.GetHandle();

											var region_id_location = h_location.GetRegionID();
											if (region_id_location != 0 && region_id_location == Client.GetRegionID())
											{
												if (WorldMap.IsOpen)
												{
													GUI.RegionMenu.ToggleWidget(false);
													//WorldMap.FocusLocation(h_location, interact: false, open_widget: false);
													//WorldMap.SelectEntity(ent_character_global, focus: false, interact: false, open_widget: false);
												}
											}
											else
											{
												WorldMap.FocusLocation(h_location, interact: false);
												WorldMap.SelectEntity(ent_character_global, focus: false, interact: false);
											}
										}
										else if (ent_character_parent.TryGetAsset(out IEntrance.Definition entrance_asset))
										{
											var h_entrance = entrance_asset.GetHandle();

											var region_id_entrance = h_entrance.GetRegionID();
											if (region_id_entrance != 0 && region_id_entrance == Client.GetRegionID())
											{
												ref var entrance = ref ent_character_parent.GetComponent<Entrance.Data>();
												if (entrance.IsNotNull() && entrance.GetCharacterSpan().Contains(h_character))
												{
													if (WorldMap.IsOpen)
													{
														GUI.RegionMenu.ToggleWidget(false);
													}

													//if (WorldMap.IsOpen)
													//{
													//	WorldMap.FocusLocation(entrance_asset.data.h_location_parent, interact: false, open_widget: false);
													//	WorldMap.FocusEntity(entrance_asset.GetGlobalEntity(), interact: true, open_widget: false);
													//	WorldMap.SelectEntity(ent_character_global, focus: false, interact: false, open_widget: false);
													//}
												}
												else
												{
													WorldMap.FocusLocation(entrance_asset.data.h_location_parent, interact: false);
													WorldMap.FocusEntity(entrance_asset.GetGlobalEntity(), interact: true);
													WorldMap.SelectEntity(ent_character_global, focus: false, interact: false);
												}
											}
											else
											{
												WorldMap.FocusLocation(entrance_asset.data.h_location_parent, interact: false);
												WorldMap.FocusEntity(entrance_asset.GetGlobalEntity(), interact: true);
												WorldMap.SelectEntity(ent_character_global, focus: false, interact: false);
											}
										}
										else
										{
											if (WorldMap.CanPlayerControlUnit(ent_character_parent, player_asset)) WorldMap.hs_selected_entities.Add(ent_character_parent);
											WorldMap.SelectEntity(ent_character_global, interact: false);
										}
									}
									else
									{
										WorldMap.SelectEntity(ent_character_global, interact: false);
									}
								}
							});
						}

						//if (widget.IsHovered())
						//{
						//	ref var transform = ref character.ent_controlled.GetComponent<Transform.Data>();
						//	if (transform.IsNotNull())
						//	{
						//		var cpos_pivot = GUI.CanvasSize * 0.50f;

						//		var cpos_a = ImGui.GetMousePos(); // cpos_pivot;
						//		var cpos_b = region.WorldToCanvas(transform.GetInterpolatedPosition());

						//		//cpos_a = cpos_a.ClampRadius(cpos_pivot, 50);
						//		//cpos_b = cpos_b.ClampRadius(cpos_pivot, 800);

						//		GUI.DrawLine2(cpos_a, cpos_b, GUI.font_color_default.WithAlphaMult(0.05f), GUI.font_color_default.WithAlphaMult(0.25f), 2, 1);
						//		GUI.DrawCircleFilled(cpos_b, 2, GUI.font_color_default.WithAlphaMult(0.70f), segments: 10);
						//	}
						//}
					}
				}
				else
				{
					using (var widget = Sidebar.Widget.New(identifier: "character.main", name: "New Main Character", icon: new Sprite(GUI.tex_icons_widget, 16, 16, 2, 0), size: new Vector2(48 * 18, 500), order: (10.00f - 0.10f), flags: Sidebar.Widget.Flags.Starts_Open))
					{
						widget.func_draw = null;

						if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
						{
							if (widget.IsAppearing()) WorldMap.FocusLocation(Conquest.CreationGUI.custom_character.vars.h_location, interact: false);

							var gui = new Conquest.CreationGUI();
							gui.Draw();
							//selected_slot = selected_slot == -1 ? null : -1;
						}

						//// TODO: add some hook or ECS event for when the client has finished joining the server 
						//if (Conquest.CreationGUI.pending_focus_character)
						//{
						//	WorldMap.FocusLocation(Conquest.CreationGUI.custom_character.vars.h_location);
						//	Conquest.CreationGUI.pending_focus_character = false;
						//}
					}
				}
			}

			//if (WorldMap.IsOpen && Client.GetRegionID() == 0 && Character.CharacterHUD.selected_slot.HasValue && Client.GetCharacterHandle().id == 0)
			//if (Character.CharacterHUD.selected_slot == -1 && !Client.GetPlayerData().h_character_main.IsValid() && !Client.IsLoadingRegion())
			//{
			//	var gui = new Conquest.CreationGUI();
			//	gui.Draw();
			//}
		}
#endif
	}
}

