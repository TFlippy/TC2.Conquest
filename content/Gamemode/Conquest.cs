using System.Threading;
using TC2.Base.Components;

namespace TC2.Conquest
{
	public static partial class Conquest
	{
		[IGamemode.Data("Conquest", "")]
		public partial struct Gamemode: IGamemode
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Active = 1 << 0,
				Paused = 1 << 1
			}

			public float elapsed;
			public Conquest.Gamemode.Flags flags;

			public Gamemode()
			{

			}

			public static void Configure()
			{

			}

			public static void Init()
			{
				//App.WriteLine("Conquest Init!", App.Color.Magenta);

				//App.WriteLine(System.IO.File.Exists("derp"), App.Color.Green);
			}
		}

		public struct DeployInfiltratorRPC: Net.IGRPC<Conquest.Gamemode>
		{
			public ICharacter.Handle h_character;

#if SERVER
			public void Invoke(ref NetConnection connection, ref Conquest.Gamemode data)
			{
				ref var player = ref connection.GetPlayer();
				Assert.NotNull(ref player, Assert.Level.Error);

				ref var region = ref connection.GetRegion();
				Assert.NotNull(ref region, Assert.Level.Error);

				var h_faction = player.h_faction;
				ref var faction_data = ref h_faction.GetData(out IFaction.Definition s_faction);
				Assert.NotNull(ref faction_data, Assert.Level.Error);
				Assert.Check(faction_data.scout_count > 0, Assert.Level.Error);

				var random = XorRandom.New(true);

				this.h_character = Spawner.CreateCharacter(ref region, ref random, "human.scout", h_faction: h_faction, scope: Asset.Scope.World);
				if (this.h_character.IsValid())
				{
					faction_data.scout_count--;
					s_faction.Sync();

					Spawner.TryGenerateKits(ref random, this.h_character);

					//App.WriteLine(this.h_character);

					Span<Entity> ents_span = stackalloc Entity[16];
					var ents_span_count = 0;

					foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
					{
						var ok = false;
						row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
						{
							if (faction.id == 0 && spawn.IsVisibleToFaction(h_faction, faction.id))
							{
								//App.WriteLine(entity.GetFullName());
								ok = true;
							}
						});

						if (ok)
						{
							ents_span[ents_span_count++] = row.Entity;
						}
					}

					ents_span = ents_span.Slice(0, ents_span_count);
					//App.WriteLine(ents_span.Length);

					var ent_spawn = ents_span.GetRandom(ref random);
					ref var dormitory = ref ent_spawn.GetComponent<Dormitory.Data>();
					if (dormitory.IsNotNull())
					{
						var span_characters = dormitory.GetCharacterSpan();
						if (span_characters.TryAdd(in h_character))
						{
							App.WriteLine("added spy");
						}
						else
						{
							App.WriteLine("replaced spy");
							span_characters[random.NextIntRange(0, span_characters.Length - 1)] = h_character;
						}

						if (dormitory.kit_default.IsValid())
						{
							ref var character_data = ref h_character.GetData();
							if (character_data.IsNotNull() && !character_data.kits.Contains(dormitory.kit_default))
							{
								character_data.kits.TryAdd(dormitory.kit_default);
							}
						}

						dormitory.Sync(ent_spawn, true);
					}
				}
			}
#endif
		}

#if SERVER
		[ChatCommand.Global("pause", "", admin: true)]
		public static void PauseCommand(ref ChatCommand.Context context, bool? value = null)
		{
			ref var region = ref World.GetGlobalRegion();
			if (region.IsNotNull())
			{
				ref var g_conquest = ref region.GetGlobalComponent<Conquest.Gamemode>();
				if (g_conquest.IsNotNull())
				{
					var sync = false;
					sync |= g_conquest.flags.TrySetFlag(Conquest.Gamemode.Flags.Paused, value ?? g_conquest.flags.HasNone(Conquest.Gamemode.Flags.Paused));
					Server.SendChatMessage(g_conquest.flags.HasAny(Conquest.Gamemode.Flags.Paused) ? "Paused Conquest." : "Unpaused Conquest.", channel: Chat.Channel.System);

					if (sync)
					{
						region.SyncGlobal(ref g_conquest);
					}
				}
			}
		}

		//[ChatCommand.Global("setmap", "", creative: true)]
		//public static void SetMapCommand(ref ChatCommand.Context context, byte region_id, string map)
		//{
		//	ref var world = ref Server.GetWorld();

		//	ref var region_new = ref world.ImportRegion2(region_id, map);
		//	if (region_new.IsNotNull())
		//	{
		//		//world.SetContinueRegionID(region_id);

		//		//region_new.Wait().ContinueWith(() =>
		//		//{
		//		//    Net.SetActiveRegionForAllPlayers(region_id_new);
		//		//});
		//	}
		//}

		[ChatCommand.Region("scout", "", creative: true)]
		public static void ScoutCommand(ref ChatCommand.Context context, int count)
		{
			ref var region = ref context.GetRegion();
			ref var player = ref context.GetPlayerData();

			ref var faction_data = ref player.faction_id.GetData(out IFaction.Definition faction_asset);
			if (faction_data.IsNotNull())
			{
				faction_data.scout_count += count;
				faction_data.scout_count.Min(0);

				faction_asset.Sync();
			}
		}
#endif

		//#if SERVER
		//		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnAdd(ISystem.Info info, ref Region.Data region, [Source.Owned] ref MapCycle.Global mapcycle)
		//		{
		//			mapcycle.AddMaps(ref region, "conquest");
		//		}
		//#endif

		//		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnUpdate(ISystem.Info info, [Source.Global] ref Conquest.Gamemode conquest, [Source.Global] in MapCycle.Global mapcycle, [Source.Global] ref MapCycle.Voting voting)
		//		{
		//			if (true)
		//			{
		//				if (!conquest.flags.HasAny(Conquest.Gamemode.Flags.Paused))
		//				{
		//					conquest.elapsed += info.DeltaTime;
		//				}
		//			}
		//		}

		//#if SERVER
		//		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 5.00f)]
		//		public static void OnUpdate(ISystem.Info info, Entity entity, ref XorRandom random, [Source.Owned] in Player.Data player, [Source.Owned] ref Respawn.Data respawn)
		//		{
		//			ref var region = ref info.GetRegion();

		//			if (player.ent_controlled.IsValid()) return;

		//			var time = info.WorldTime;
		//			var sync = false;

		//			//App.WriteLine($"tick {time}; {info.DeltaTime}");

		//			var index = 0;
		//			foreach (ref var spawn_info in respawn.spawns_values)
		//			{
		//				//if (spawn_info.character.id == 0 || (time >= spawn_info.next_reroll && respawn.spawns_keys[index].id != 0))
		//				//if (spawn_info.character.id == 0 && time >= spawn_info.next_reroll)
		//				if (((player.faction_id == 0 && respawn.spawns_keys[index].IsValid()) || spawn_info.character.id == 0) && time >= spawn_info.next_reroll)
		//				{
		//					var map_info_copy = region.GetMapInfo();

		//					var species_tmp = new ISpecies.Handle("human");
		//					var origins = new WeightedList<IOrigin.Handle>(IOrigin.Database.GetAssets().Where(x => x.id != 0 && x.data.species == species_tmp).Select(x => new WeightedList<IOrigin.Handle>.Item(x.data.conditions.CalculateWeight(map_info_copy), x)));

		//					var h_character = Dormitory.CreateCharacter(ref region, ref random, origins.GetRandom(ref random), spawn_info.character);
		//					spawn_info = default;

		//					var definition = h_character.GetDefinition();
		//					if (definition != null)
		//					{
		//						definition.Flags.SetFlag(Asset.Flags.No_Save, true);
		//					}

		//					spawn_info.character = h_character;
		//					spawn_info.kits.TryAdd("survival");

		//					ref var character_data = ref spawn_info.character.GetData();
		//					if (character_data.IsNotNull())
		//					{
		//						var character_flags = character_data.traits;

		//						// old
		//						//var kits = IKit.Database.GetAssets().Where(x => x.id != 0 && character_flags.HasAll(x.data.character_flags)).ToArray();
		//						//spawn_info.kits.TryAdd(kits.GetRandom(ref random)?.GetHandle() ?? default);

		//						// new
		//						Span<IKit.Handle> kits = stackalloc IKit.Handle[16];
		//						//IKit.Database.GetHandles(ref kits, (x) => character_flags.HasAll(x.data.character_flags));
		//						IKit.Database.GetHandles(ref kits, (x) => x.data.character_flags.Evaluate(character_flags) >= 0.50f);
		//						//App.WriteLine($"{kits.Length}; {character_flags}");

		//						spawn_info.kits.TryAdd(kits.GetRandom(ref random));
		//						//if (random.NextBool(0.50f)) spawn_info.kits.TryAdd(kits.GetRandom(ref random));
		//					}

		//					spawn_info.next_reroll = time + 120.00f;

		//					//App.WriteLine($"rerolled character {spawn_info.character}");

		//					sync = true;
		//				}

		//				index++;
		//			}

		//			if (sync)
		//			{
		//				respawn.Sync(entity);
		//			}
		//		}
		//#endif

#if CLIENT
		public struct ScoreboardGUI: IGUICommand
		{
			public Player.Data player;
			public Conquest.Gamemode gamemode;

			public static bool show;

			public void Draw()
			{
				var alive = this.player.flags.HasAny(Player.Flags.Alive);

				var window_pos = (GUI.CanvasSize * new Vector2(0.50f, 0.00f)) + new Vector2(000, 48);
				using (var window = GUI.Window.Standalone("Scoreboard"u8, position: alive ? null : window_pos, size: new Vector2(600, 400), pivot: alive ? new Vector2(0.50f, 0.00f) : new(1.00f, 0.00f), flags: GUI.Window.Flags.No_Appear_Focus | GUI.Window.Flags.No_Click_Focus))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						ref var region = ref Client.GetRegion();
						ref var world = ref Client.GetWorld();
						ref var game_info = ref Client.GetGameInfo();

						if (alive)
						{
							GUI.DrawWindowBackground(GUI.tex_scoreboard_bg);
						}

						using (GUI.Group.New(size: GUI.GetAvailableSize(), padding: new(14, 12)))
						{
							using (GUI.Group.New(size: new Vector2(GUI.RmX, 32)))
							{
								GUI.Title($"{game_info.name}", size: 32);
								//GUI.SameLine();
								//GUI.TitleCentered($"Next map in: {GUI.FormatTime(Maths.Max(0.00f, this.gamemode.match_duration - this.gamemode.elapsed))}", size: 24, pivot: new Vector2(1, 1));
							}

							GUI.SeparatorThick();

							using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new Vector2(4, 4)))
							{
								using (GUI.Group.New(size: new(GUI.RmX * 0.50f, 0), padding: new Vector2(8, 4)))
								{
									GUI.LabelShaded("Players:", $"{game_info.player_count}/{game_info.player_count_max}", font_a: GUI.Font.Superstar, size_a: 16);
									//GUI.Label("Map:", game_info.map, font: GUI.Font.Superstar, size: 16);
									GUI.LabelShaded("Gamemode:", $"{game_info.gamemode}", font_a: GUI.Font.Superstar, size_a: 16);
								}
							}

							GUI.NewLine(4);

							GUI.SeparatorThick();

							GUI.NewLine(4);

							using (GUI.Group.New(size: GUI.Rm, padding: new Vector2(4, 4)))
							{
								using (var table = GUI.Table.New("Players", 3, size: new Vector2(0, GUI.RmY)))
								{
									if (table.show)
									{
										table.SetupColumnFlex(1);
										table.SetupColumnFixed(128);
										table.SetupColumnFixed(64);
										//table.SetupColumnFixed(64);
										//table.SetupColumnFixed(64);

										using (var row = GUI.Table.Row.New(size: new(GUI.RmX, 16), header: true))
										{
											using (row.Column(0)) GUI.Title("Name", size: 20);
											using (row.Column(1)) GUI.Title("Faction", size: 20);
											//using (row.Column(2)) GUI.Title("Money", size: 20);
											using (row.Column(2)) GUI.Title("Status", size: 20);
											//using (row.Column(4)) GUI.Title("Deaths");
										}

										foreach (ref var row in region.IterateQuery<Region.GetPlayersQuery>())
										{
											row.Run((ISystem.Info info, Entity entity, in Player.Data player) =>
											{
												var is_online = player.flags.HasAny(Player.Flags.Online);
												if (!is_online) return;

												using (var row = GUI.Table.Row.New(size: new(GUI.RmX, 16)))
												{
													using (GUI.ID.Push(entity))
													{
														var alpha = is_online ? 1.00f : 0.50f;

														using (row.Column(0))
														{
															GUI.Text(player.h_player.GetName(), color: GUI.font_color_default.WithAlphaMult(alpha));
														}

														using (row.Column(1))
														{
															if (player.faction_id.TryGetData(out var ref_faction))
															{
																GUI.Title(ref_faction.value.name, color: ref_faction.value.color_a.WithAlphaMult(alpha));
															}
														}

														//ref var money = ref player.GetMoneyReadOnly().Value;
														//if (!money.IsNull())
														//{
														//	using (row.Column(2))
														//	{
														//		GUI.Text($"{money.amount:0}", color: GUI.font_color_default.WithAlphaMult(alpha));
														//	}
														//}

														using (row.Column(2))
														{
															GUI.Text(is_online ? "Online" : "Offline", color: GUI.font_color_default.WithAlphaMult(alpha));
														}

														GUI.SameLine();
														GUI.Selectable2(false, play_sound: false, enabled: false, size: new Vector2(0, 0), is_readonly: true);
													}
												}
											});
										}

										//region.Query<Region.GetPlayersQuery>(Func).Execute(ref this);
										//static void Func(ISystem.Info info, Entity entity, in Player.Data player)
										//{
										//	var is_online = player.flags.HasAny(Player.Flags.Online);
										//	if (!is_online) return;

										//	ref var arg = ref info.GetParameter<ScoreboardGUI>();
										//	if (!arg.IsNull())
										//	{
										//		using (var row = GUI.Table.Row.New(size: new(GUI.RmX, 16)))
										//		{
										//			using (GUI.ID.Push(entity))
										//			{
										//				var alpha = is_online ? 1.00f : 0.50f;

										//				using (row.Column(0))
										//				{
										//					GUI.Text(player.GetName(), color: GUI.font_color_default.WithAlphaMult(alpha));
										//				}

										//				using (row.Column(1))
										//				{
										//					if (player.faction_id.TryGetData(out var ref_faction))
										//					{
										//						GUI.Title(ref_faction.value.name, color: ref_faction.value.color_a.WithAlphaMult(alpha));
										//					}
										//				}

										//				//ref var money = ref player.GetMoneyReadOnly().Value;
										//				//if (!money.IsNull())
										//				//{
										//				//	using (row.Column(2))
										//				//	{
										//				//		GUI.Text($"{money.amount:0}", color: GUI.font_color_default.WithAlphaMult(alpha));
										//				//	}
										//				//}

										//				using (row.Column(2))
										//				{
										//					GUI.Text(is_online ? "Online" : "Offline", color: GUI.font_color_default.WithAlphaMult(alpha));
										//				}

										//				GUI.SameLine();
										//				GUI.Selectable2(false, play_sound: false, enabled: false, size: new Vector2(0, 0), is_readonly: true);
										//			}
										//		}
										//	}
										//}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnEarlyGUI(Entity entity, [Source.Owned] in Player.Data player, [Source.Singleton] in Conquest.Gamemode gamemode)
		{
			if (player.IsLocal())
			{
				ref readonly var kb = ref Control.GetKeyboard();
				if (kb.GetKeyDown(Keyboard.Key.Tab))
				{
					ScoreboardGUI.show = !ScoreboardGUI.show;
				}

				Spawn.RespawnGUI.window_offset = new Vector2(0, 90);
				Spawn.RespawnGUI.window_pivot = new Vector2(0, 0);

				if (!WorldMap.IsOpen && (ScoreboardGUI.show || (!player.flags.HasAny(Player.Flags.Alive) && Editor.show_respawn_menu)))
				{
					var gui = new ScoreboardGUI()
					{
						player = player,
						gamemode = gamemode,
					};
					gui.Submit();
				}
			}
		}
#endif
	}
}

