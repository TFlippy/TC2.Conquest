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
				Constants.World.multi_region = true;
			}

			public static void Init()
			{
				//App.WriteLine("Conquest Init!", App.Color.Magenta);

				//App.WriteLine(System.IO.File.Exists("derp"), App.Color.Green);
			}
		}

#if SERVER
		[ChatCommand.Region("pause", "", admin: true)]
		public static void PauseCommand(ref ChatCommand.Context context, bool? value = null)
		{
			ref var region = ref context.GetRegion();
			if (!region.IsNull())
			{
				ref var g_conquest = ref region.GetSingletonComponent<Conquest.Gamemode>();
				if (!g_conquest.IsNull())
				{
					var sync = false;
					sync |= g_conquest.flags.TrySetFlag(Conquest.Gamemode.Flags.Paused, value ?? !g_conquest.flags.HasAll(Conquest.Gamemode.Flags.Paused));
					Server.SendChatMessage(g_conquest.flags.HasAll(Conquest.Gamemode.Flags.Paused) ? "Paused Conquest." : "Unpaused Conquest.", channel: Chat.Channel.System);

					if (sync)
					{
						region.SyncGlobal(ref g_conquest);
					}
				}
			}
		}

		[ChatCommand.Region("setmap", "", creative: true)]
		public static void SetMapCommand(ref ChatCommand.Context context, byte region_id, string map)
		{
			ref var world = ref Server.GetWorld();

			ref var region_new = ref world.ImportRegion(region_id, map);
			if (!region_new.IsNull())
			{
				//world.SetContinueRegionID(region_id);

				//region_new.Wait().ContinueWith(() =>
				//{
				//    Net.SetActiveRegionForAllPlayers(region_id_new);
				//});
			}
		}
#endif

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single)]
		public static void OnAdd(ISystem.Info info, [Source.Owned] ref MapCycle.Global mapcycle)
		{
			ref var region = ref info.GetRegion();
			mapcycle.AddMaps(ref region, "conquest");
		}
#endif

		[ISystem.VeryLateUpdate(ISystem.Mode.Single)]
		public static void OnUpdate(ISystem.Info info, [Source.Global] ref Conquest.Gamemode conquest, [Source.Global] in MapCycle.Global mapcycle, [Source.Global] ref MapCycle.Voting voting)
		{
			if (true)
			{
				if (!conquest.flags.HasAny(Conquest.Gamemode.Flags.Paused))
				{
					conquest.elapsed += info.DeltaTime;
				}
			}
		}

#if CLIENT
		public struct ScoreboardGUI: IGUICommand
		{
			public Player.Data player;
			public Conquest.Gamemode gamemode;

			public static bool show;

			public void Draw()
			{
				var alive = this.player.flags.HasAny(Player.Flags.Alive);

				var window_pos = (GUI.CanvasSize * new Vector2(0.50f, 0.00f)) + new Vector2(100, 48);
				using (var window = GUI.Window.Standalone("Scoreboard", position: alive ? null : window_pos, size: new Vector2(700, 400), pivot: alive ? new Vector2(0.50f, 0.00f) : new(1.00f, 0.00f)))
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
							using (GUI.Group.New(size: new Vector2(GUI.GetRemainingWidth(), 32)))
							{
								GUI.Title($"{game_info.name}", size: 32);
								//GUI.SameLine();
								//GUI.TitleCentered($"Next map in: {GUI.FormatTime(MathF.Max(0.00f, this.gamemode.match_duration - this.gamemode.elapsed))}", size: 24, pivot: new Vector2(1, 1));
							}

							GUI.SeparatorThick();

							using (GUI.Group.New(padding: new Vector2(4, 4)))
							{
								using (GUI.Group.New(size: new(GUI.GetRemainingWidth() * 0.50f, 0), padding: new Vector2(8, 4)))
								{
									GUI.Label("Players:", $"{game_info.player_count}/{game_info.player_count_max}", font: GUI.Font.Superstar, size: 16);
									//GUI.Label("Map:", game_info.map, font: GUI.Font.Superstar, size: 16);
									GUI.Label("Gamemode:", $"{game_info.gamemode}", font: GUI.Font.Superstar, size: 16);
								}
							}

							GUI.NewLine(4);

							GUI.SeparatorThick();

							GUI.NewLine(4);

							using (GUI.Group.New(size: GUI.GetRemainingSpace(), padding: new Vector2(4, 4)))
							{
								using (var table = GUI.Table.New("Players", 3, size: new Vector2(0, GUI.GetRemainingHeight())))
								{
									if (table.show)
									{
										table.SetupColumnFlex(1);
										table.SetupColumnFixed(128);
										table.SetupColumnFixed(64);
										//table.SetupColumnFixed(64);
										//table.SetupColumnFixed(64);

										using (var row = GUI.Table.Row.New(size: new(GUI.GetRemainingWidth(), 16), header: true))
										{
											using (row.Column(0)) GUI.Title("Name", size: 20);
											using (row.Column(1)) GUI.Title("Faction", size: 20);
											//using (row.Column(2)) GUI.Title("Money", size: 20);
											using (row.Column(2)) GUI.Title("Status", size: 20);
											//using (row.Column(4)) GUI.Title("Deaths");
										}

										region.Query<Region.GetPlayersQuery>(Func).Execute(ref this);
										static void Func(ISystem.Info info, Entity entity, in Player.Data player)
										{
											var is_online = player.flags.HasAny(Player.Flags.Online);
											if (!is_online) return;

											ref var arg = ref info.GetParameter<ScoreboardGUI>();
											if (!arg.IsNull())
											{
												using (var row = GUI.Table.Row.New(size: new(GUI.GetRemainingWidth(), 16)))
												{
													using (GUI.ID.Push(entity))
													{
														var alpha = is_online ? 1.00f : 0.50f;

														using (row.Column(0))
														{
															GUI.Text(player.GetName(), color: GUI.font_color_default.WithAlphaMult(alpha));
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

		[ISystem.EarlyGUI(ISystem.Mode.Single)]
		public static void OnEarlyGUI(Entity entity, [Source.Owned] in Player.Data player, [Source.Global] in Conquest.Gamemode gamemode)
		{
			if (player.IsLocal())
			{
				ref readonly var kb = ref Control.GetKeyboard();
				if (kb.GetKeyDown(Keyboard.Key.Tab))
				{
					ScoreboardGUI.show = !ScoreboardGUI.show;
				}

				Spawn.RespawnGUI.window_offset = new Vector2(100, 90);
				Spawn.RespawnGUI.window_pivot = new Vector2(0, 0);

				if (ScoreboardGUI.show || (!player.flags.HasAny(Player.Flags.Alive) && Editor.show_respawn_menu))
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

