using ConnectorLib;
using JetBrains.Annotations;
using ConnectorType = CrowdControl.Common.ConnectorType;

namespace CrowdControl.Games.Packs.DrMario;

[UsedImplicitly]
public class DrMario : NESEffectPack
{
    public DrMario(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler) { }

    private const ushort ADDR_PILL_SPEED = 0x030A;
    private const ushort ADDR_GAME_SPEED = 0x030B;
    private const ushort ADDR_P1_NEXT_PILL = 0x031A;
    private const ushort ADDR_MUSIC = 0x0731; //00 = Fever, 01 = Chill, 02 = Off
    private const ushort ADDR_PIECE_GRAVITY = 0x0724; //00 = Normal, 01 = No Gravity
    private const ushort ADDR_GAMEPLAY_MODE = 0x0046;
    private const ushort ADDR_PAUSE_CHECK = 0x0240;
    private const ushort ADDR_STAGECLEAR_CHECK = 0x0408;
    private const ushort ADDR_AUDIO_SFX = 0x06F1;
    private const ushort ADDR_AUDIO_GARBAGE = 0x06F4;
    private const ushort ADDR_AUDIO_MUSIC = 0x06F5;
    
    private enum Pills : ushort
    {
        YY = 0x0000,
        YR = 0x0100,
        YB = 0x0200,
        RY = 0x0001,
        RR = 0x0101,
        RB = 0x0201,
        BY = 0x0002,
        BR = 0x0102,
        BB = 0x0202
    }
    
    private enum MusicSetting : byte
    {
        Fever = 0x00,
        Chill = 0x01,
        Off = 0x02
    }

    private enum MusicImmediate : byte
    {
        Chill = 0x02,
        Off = 0x03,
        Fever = 0x04,
        TopOut = 0x05,
        Title = 0x06,
        Menu = 0x07,
        FeverWin = 0x09,
        ChillWin = 0x0A,
        TwoPWin = 0x0B,
        TwentyHiCutScene = 0x0C,
        TwentyLowCutscene = 0x0D,
    }
    
    private enum SFX : byte
    {
        SpeedUp = 0x06,
        CrownBling = 0x08,
        Alarm = 0x0A,
    }

    private enum GarbageAlerts : byte
    {
        Small = 0x01,
        Large = 0x02,
    }
    
    private enum GameSpeed : byte
    {
        LOW = 0x00,
        MED = 0x01,
        HI = 0x02
    }
    
    public override EffectList Effects
    {
        get
        {
            List<Effect> effects =
            [
                new("Speed Up", "speedup") { Description = "Increases the speed of the game.", Quantity = 1..10, Price = 10 },
                new("Speed Down", "speeddown") { Description = "Decreases the speed of the game.", Quantity = 1..10, Price = 10 },
                
                new("Game Speed: HI", "gameSpeedHI") { Description = "Sets the game speed to HI.", Price = 100 },
                new("Game Speed: MED", "gameSpeedMED") { Description = "Sets the game speed to MED.", Price = 100 },
                new("Game Speed: LOW", "gameSpeedLOW") { Description = "Sets the game speed to LOW.", Price = 100 },
                
                new("Send Yellow Rush", "YYrush") { Description = "Sends a yellow rush (all yellow pills).", Duration = TimeSpan.FromSeconds(10), Price = 100 },
                new("Send Blue Rush", "BBrush") { Description = "Sends a blue rush (all blue pills).", Duration = TimeSpan.FromSeconds(10), Price = 100 },
                new("Send Red Rush", "RRrush") { Description = "Sends a red rush (all red pills).", Duration = TimeSpan.FromSeconds(10), Price = 100 },
                
                new("No Gravity", "noGravity") { Description = "Pills don't fall after clears.", Duration = TimeSpan.FromSeconds(15), Price = 250 },
                
                new("Drop 2 Garbage", "dropGarbage2") { Description = "Drop garbage in 2 random columns.", Price = 15 },
                new("Drop 3 Garbage", "dropGarbage3") { Description = "Drop garbage in 3 random columns.", Price = 20 },
                new("Drop 4 Garbage", "dropGarbage4") { Description = "Drop garbage in 4 random columns.", Price = 25 },
                new("Drop 8 Garbage", "dropGarbage8") { Description = "Drop garbage in all 8 columns.", Price = 50 },
                
                new("Set Next Pill: Yellow/Yellow", "pillYY") { Description = "Sets next pill to Yellow/Yellow.", Price = 5 },
                new("Set Next Pill: Yellow/Red", "pillYR") { Description = "Sets next pill to Yellow/Red.", Price = 5 },
                new("Set Next Pill: Yellow/Blue", "pillYB") { Description = "Sets next pill to Yellow/Blue.", Price = 5 },
                new("Set Next Pill: Red/Yellow", "pillRY") { Description = "Sets next pill to Red/Yellow.", Price = 5 },
                new("Set Next Pill: Red/Red", "pillRR") { Description = "Sets next pill to Red/Red.", Price = 5 },
                new("Set Next Pill: Red/Blue", "pillRB") { Description = "Sets next pill to Red/Blue.", Price = 5 },
                new("Set Next Pill: Blue/Yellow", "pillBY") { Description = "Sets next pill to Blue/Yellow.", Price = 5 },
                new("Set Next Pill: Blue/Red", "pillBR") { Description = "Sets next pill to Blue/Red.", Price = 5 },
                new("Set Next Pill: Blue/Blue", "pillBB") { Description = "Sets next pill to Blue/Blue.", Price = 5 },
                
                new("Set Music: Fever", "musicFever") { Description = "Sets the music to Fever, takes effect at next level.", Price = 5 },
                new("Set Music: Chill", "musicChill") { Description = "Sets the music to Chill, takes effect at next level.", Price = 5 },
                new("Set Music: Off", "musicOff") { Description = "Sets the music to Off, takes effect at next level.", Price = 5 },
            ];

            return effects;
        }
    }
    
    public override Game Game { get; } = new("Dr. Mario", "DrMario", "NES", ConnectorType.NESConnector);
    
    public override bool StopAllEffects()
    {
        bool success = base.StopAllEffects();
        /*try
        {
            success &= Connector.Write8(0x8904, 0x29);
            success &= Connector.Write8(0xd3c8, 0x00);
        }
        catch { success = false; }*/
        return success;
    }

    protected override GameState GetGameState()
    {
        byte modeByte = 0;
        
        bool success = Connector.Read8(ADDR_GAMEPLAY_MODE, out modeByte) && modeByte <= 16;

        if (modeByte == 1 || modeByte == 2 || modeByte == 7)
        {
            return GameState.Menu;
        }

        if (modeByte == 8)
        {
            return GameState.Loading;
        }

        if (modeByte == 4)
        {
            bool paused = Connector.Read8(ADDR_PAUSE_CHECK, out byte pauseByte) && pauseByte == 0xFF;
            bool stageClear = Connector.Read8(ADDR_STAGECLEAR_CHECK, out byte stageClearByte) && stageClearByte == 0x8B;
            
            if (paused)
            {
                return GameState.Paused;
            }

            if (stageClear)
            {
                return GameState.Cutscene;
            }
            
            return GameState.Ready;
        }
        
        return GameState.Unknown;
    }
    
    public override ROMTable ROMTable
    {
        get
        {
            return new[]
            {
                new ROMInfo("Dr. Mario (Japan, USA) (Rev A)", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "8181d696756578fc92e6c4c86da01904")),
                new ROMInfo("Dr. Mario (Japan, USA)", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "d3ec44424b5ac1a4dc77709829f721c9")),
                new ROMInfo("Dr. Mario (Europe)", null, Patching.Ignore, ROMStatus.ValidPatched, s => Patching.MD5(s, "3f27eda62c6692f96790af9a1d917ef6"))
            };
        }
    }
    
    
    protected override void StartEffect(EffectRequest request)
    {
        if (GetGameState() != GameState.Ready)
        {
            DelayEffect(request, TimeSpan.FromSeconds(5));
            return;
        }

        string[] codeParams = FinalCode(request).Split('_');
        switch (codeParams[0])
        {
            case "speedup":
            {
                if (!byte.TryParse(codeParams[1], out byte speed))
                {
                    Respond(request, EffectStatus.FailTemporary, "Invalid speed quantity.", ErrorSource.Unknown, true);
                    return;
                }
                TryEffect(request,
                    () => Connector.RangeAdd8(ADDR_PILL_SPEED, speed, 0, 255, false) && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.SpeedUp),
                    () => true,
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sped up the pills.");
                    });
                return;
            }
            case "speeddown":
            {
                if (!byte.TryParse(codeParams[1], out byte speed))
                {
                    Respond(request, EffectStatus.FailTemporary, "Invalid speed quantity.", ErrorSource.Unknown, true);
                    return;
                }
                TryEffect(request,
                    () => Connector.RangeAdd8(ADDR_PILL_SPEED, -1 * speed, 0, 255, false) && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.SpeedUp),
                    () => true,
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} slowed down the pills.");
                    });
                return;
            }
            case "gameSpeedHI":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_GAME_SPEED, out byte b) && b <= 2,
                    () => Connector.Write8(ADDR_GAME_SPEED, (byte)GameSpeed.HI),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} changed the game speed to HI.");
                    });
                return;
            }
            case "gameSpeedMED":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_GAME_SPEED, out byte b) && b <= 2,
                    () => Connector.Write8(ADDR_GAME_SPEED, (byte)GameSpeed.MED),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} changed the game speed to MED.");
                    });
                return;
            }
            case "gameSpeedLOW":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_GAME_SPEED, out byte b) && b <= 2,
                    () => Connector.Write8(ADDR_GAME_SPEED, (byte)GameSpeed.LOW),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} changed the game speed to LOW.");
                    });
                return;
            }
            case "pillYY":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.YY),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a double yellow pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillYR":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.YR),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a yellow/red pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillYB":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.YB),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a yellow/blue pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillRY":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.RY),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a red/yellow pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillRR":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.RR),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a double red pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillRB":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.RB),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a red/blue pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillBY":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.BY),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a blue/yellow pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillBR":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.BR),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a blue/red pill.");
                    }, mutex: "pills");
                return;
            }
            case "pillBB":
            {
                TryEffect(request,
                    () => Connector.Read16(ADDR_P1_NEXT_PILL, out ushort b),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.BB),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent a double blue pill.");
                    }, mutex: "pills");
                return;
            }
            case "YYrush":
            {
                var s = RepeatAction(request,
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} sent a yellow rush!") && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.Alarm), TimeSpan.FromSeconds(1),
                    () => true, TimeSpan.FromSeconds(1),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.YY), TimeSpan.FromSeconds(.01), false, "pills");
                s.WhenCompleted.Then(_ =>
                {
                    Connector.SendMessage("Yellow rush has ended.");
                });
                return;
            }
            case "BBrush":
            {
                var s = RepeatAction(request,
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} sent a blue rush!") && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.Alarm), TimeSpan.FromSeconds(1),
                    () => true, TimeSpan.FromSeconds(1),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.BB), TimeSpan.FromSeconds(.01), false, "pills");
                s.WhenCompleted.Then(_ =>
                {
                    Connector.SendMessage("Blue rush has ended.");
                });
                return;
            }
            case "RRrush":
            {
                var s = RepeatAction(request,
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} sent a red rush!") && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.Alarm), TimeSpan.FromSeconds(1),
                    () => true, TimeSpan.FromSeconds(1),
                    () => Connector.Write16(ADDR_P1_NEXT_PILL, (ushort)Pills.RR), TimeSpan.FromSeconds(.01), false, "pills");
                s.WhenCompleted.Then(_ =>
                {
                    Connector.SendMessage("Red rush has ended.");
                });
                return;
            }
            case "noGravity":
            {
                var s = RepeatAction(request,
                    () => true,
                    () => Connector.SendMessage($"{request.DisplayViewer} stopped gravity!") && Connector.Write8(ADDR_AUDIO_SFX, (byte)SFX.CrownBling), TimeSpan.FromSeconds(1),
                    () => true, TimeSpan.FromSeconds(1),
                    () => Connector.Write8(ADDR_PIECE_GRAVITY, 0x01), TimeSpan.FromSeconds(.5), false, "gravity");
                s.WhenCompleted.Then(_ =>
                {
                    Connector.Write8(ADDR_PIECE_GRAVITY, 0x00);
                    Connector.SendMessage("Gravity has been restored.");
                });
                return;
            }
            case "dropGarbage2":
            {
                List<ulong> allAddresses = new List<ulong>(){0x04F8, 0x04F9, 0x04FA, 0x04FB, 0x04FC, 0x04FD, 0x04FE, 0x04FF};
                
                // Create a random number generator.
                Random random = new Random();

                // Shuffle the list of addresses.
                allAddresses = allAddresses.OrderBy(x => random.Next()).ToList();

                // Take the first three unique addresses.
                var addresses = allAddresses.Take(2).ToList();

                // Define the range boundaries.
                const byte start = 0x80;
                const byte end = 0x82;

                // Create a random number generator.
                Random random2 = new Random();

                // Generate three random bytes, duplicates allowed.
                byte[] randomBytes = new byte[2];
                for (int i = 0; i < randomBytes.Length; i++)
                {
                    randomBytes[i] = (byte)random2.Next(start, end + 1); // Generate a byte in the inclusive range.
                }
                
                TryEffect(request,
                    () => Connector.Read8(addresses[0], out byte b),
                    () => Connector.Write8(addresses[0], randomBytes[0]) && Connector.Write8(addresses[1], randomBytes[1]) && Connector.Write8(ADDR_AUDIO_GARBAGE, (byte)GarbageAlerts.Small),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent 2 garbage.");
                    }, mutex: "garbage", holdMutex: TimeSpan.FromSeconds(5));
                return;
            }
            case "dropGarbage3":
            {
                List<ulong> allAddresses = new List<ulong>(){0x04F8, 0x04F9, 0x04FA, 0x04FB, 0x04FC, 0x04FD, 0x04FE, 0x04FF};
                
                // Create a random number generator.
                Random random = new Random();

                // Shuffle the list of addresses.
                allAddresses = allAddresses.OrderBy(x => random.Next()).ToList();

                // Take the first three unique addresses.
                var addresses = allAddresses.Take(3).ToList();

                // Define the range boundaries.
                const byte start = 0x80;
                const byte end = 0x82;

                // Create a random number generator.
                Random random2 = new Random();

                // Generate three random bytes, duplicates allowed.
                byte[] randomBytes = new byte[3];
                for (int i = 0; i < randomBytes.Length; i++)
                {
                    randomBytes[i] = (byte)random2.Next(start, end + 1); // Generate a byte in the inclusive range.
                }
                
                TryEffect(request,
                    () => Connector.Read8(addresses[0], out byte b),
                    () => Connector.Write8(addresses[0], randomBytes[0]) && Connector.Write8(addresses[1], randomBytes[1]) && Connector.Write8(addresses[2], randomBytes[2]) && Connector.Write8(ADDR_AUDIO_GARBAGE, (byte)GarbageAlerts.Large),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent 3 garbage.");
                    }, mutex: "garbage", holdMutex: TimeSpan.FromSeconds(5));
                return;
            }
            case "dropGarbage4":
            {
                List<ulong> allAddresses = new List<ulong>(){0x04F8, 0x04F9, 0x04FA, 0x04FB, 0x04FC, 0x04FD, 0x04FE, 0x04FF};
                
                // Create a random number generator.
                Random random = new Random();

                // Shuffle the list of addresses.
                allAddresses = allAddresses.OrderBy(x => random.Next()).ToList();

                // Take the first three unique addresses.
                var addresses = allAddresses.Take(4).ToList();

                // Define the range boundaries.
                const byte start = 0x80;
                const byte end = 0x82;

                // Create a random number generator.
                Random random2 = new Random();

                // Generate three random bytes, duplicates allowed.
                byte[] randomBytes = new byte[4];
                for (int i = 0; i < randomBytes.Length; i++)
                {
                    randomBytes[i] = (byte)random2.Next(start, end + 1); // Generate a byte in the inclusive range.
                }
                
                TryEffect(request,
                    () => Connector.Read8(addresses[0], out byte b),
                    () => Connector.Write8(addresses[0], randomBytes[0]) && Connector.Write8(addresses[1], randomBytes[1]) && Connector.Write8(addresses[2], randomBytes[2]) && Connector.Write8(addresses[3], randomBytes[3]) && Connector.Write8(ADDR_AUDIO_GARBAGE, (byte)GarbageAlerts.Large),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent 4 garbage.");
                    }, mutex: "garbage", holdMutex: TimeSpan.FromSeconds(5));
                return;
            }
            case "dropGarbage8":
            {
                List<ulong> addresses = new List<ulong>(){0x04F8, 0x04F9, 0x04FA, 0x04FB, 0x04FC, 0x04FD, 0x04FE, 0x04FF};
                
                // Define the range boundaries.
                const byte start = 0x80;
                const byte end = 0x82;

                // Create a random number generator.
                Random random2 = new Random();

                // Generate three random bytes, duplicates allowed.
                byte[] randomBytes = new byte[8];
                for (int i = 0; i < randomBytes.Length; i++)
                {
                    randomBytes[i] = (byte)random2.Next(start, end + 1); // Generate a byte in the inclusive range.
                }
                
                TryEffect(request,
                    () => Connector.Read8(addresses[0], out byte b),
                    () => Connector.Write8(addresses[0], randomBytes[0]) && Connector.Write8(addresses[1], randomBytes[1]) && Connector.Write8(addresses[2], randomBytes[2]) && Connector.Write8(addresses[3], randomBytes[3]) && Connector.Write8(addresses[4], randomBytes[4]) && Connector.Write8(addresses[5], randomBytes[5]) && Connector.Write8(addresses[6], randomBytes[6]) && Connector.Write8(addresses[7], randomBytes[7]) && Connector.Write8(ADDR_AUDIO_GARBAGE, (byte)GarbageAlerts.Large),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} sent 8 garbage.");
                    }, mutex: "garbage", holdMutex: TimeSpan.FromSeconds(5));
                return;
            }
            case "musicFever":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_MUSIC, out byte b),
                    () => Connector.Write8(ADDR_MUSIC, (byte)MusicSetting.Fever) && Connector.Write8(ADDR_AUDIO_MUSIC,
                        (byte)MusicImmediate.Fever),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} set the music to FEVER.");
                    });
                return;
            }
            case "musicChill":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_MUSIC, out byte b),
                    () => Connector.Write8(ADDR_MUSIC, (byte)MusicSetting.Chill) && Connector.Write8(ADDR_AUDIO_MUSIC,
                        (byte)MusicImmediate.Chill),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} set the music to CHILL.");
                    });
                return;
            }
            case "musicOff":
            {
                TryEffect(request,
                    () => Connector.Read8(ADDR_MUSIC, out byte b),
                    () => Connector.Write8(ADDR_MUSIC, (byte)MusicSetting.Off) && Connector.Write8(ADDR_AUDIO_MUSIC,
                        (byte)MusicImmediate.Off),
                    () =>
                    {
                        Connector.SendMessage($"{request.DisplayViewer} set the music to OFF.");
                    });
                return;
            }
            default:
                Respond(request, EffectStatus.FailTemporary, "Unknown effect.", ErrorSource.Unknown, true);
                return;
        }
    }
}