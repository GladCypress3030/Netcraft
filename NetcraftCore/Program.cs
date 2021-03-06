﻿using System;
using System.Collections;
using System.Collections.Generic;
using global::System.Collections.Specialized;
using System.Diagnostics;
using global::System.Drawing;
using global::System.IO;
using System.Linq;
using global::System.Net;
using global::System.Net.Sockets;
using global::System.Text;
using global::System.Text.RegularExpressions;
using global::System.Threading;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using NCore.netcraft.server.api;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;

namespace NCore
{
    public class Config
    {
        public static string GetValue(string var, string cfg, string ifNull = null)
        {
            cfg = cfg.Replace(Constants.vbCrLf, Constants.vbLf);
            foreach (var a in cfg.Split('\n'))
            {
                var b = a.Split('=');
                if (a.Length == 0) continue;
                if (a[0] == '#') continue;
                if ((b[0].ToLower() ?? "") == (var.ToLower() ?? ""))
                {
                    return string.Join("=", b.Skip(1).ToArray());
                }
            }

            return ifNull;
        }
    }

    public class Context
    {
        public NCore nCore { get; private set; }
        
        public Context(params object[] opt)
        {
            nCore = (NCore)opt[0];
        }

        public void Print(string message, string t = "INFO")
        {
            if (t != "INFO" &&
                t != "WARNING" &&
                t != "SEVERE" &&
                t != "ERROR") t = "INFO";
            nCore.Log(message, t);
        }

        public delegate void OnStartedEventHandler();
        public event OnStartedEventHandler ServerStarted;
        internal void started()
        {
            ServerStarted?.Invoke();
        }

        public delegate void OnLoopActionEventHandler();
        public event OnLoopActionEventHandler LoopAction;
        internal void loop()
        {
            LoopAction?.Invoke();
        }

        public async Task ExecuteScript(string name)
        {
            await nCore.StartScript(name);
        }

        public async Task ResetEnvironment(NetcraftPlayer player)
        {
            if(nCore.scripts.ContainsKey(player.Username))
            {
                nCore.scripts.Remove(player.Username);
            } else
            {
                throw new ArgumentNullException();
            }
        }
    }

    public partial class NCore
    {
        public class Lang
        {

            internal Hashtable formats;
            public static Lang FromText(string t)
            {
                Lang lang = new Lang();
                lang.formats = new Hashtable(new Dictionary<string, string>());
                t = t.Replace("\r\n", "\n");
                foreach (string i in t.Split('\n'))
                {
                    lang.formats.Add(i.Split('=')[0], Config.GetValue(i.Split('=')[0], t, i.Split('=')[0]));
                }
                return lang;
            }

            public static Lang FromFile(string p)
            {
                return FromText(System.IO.File.ReadAllText(p, Encoding.UTF8));
            }

            public string get(string i)
            {
                if (!formats.ContainsKey(i)) return i;
                return formats[i].ToString().Replace("&CRLF", "\r\n").Replace("&CR", "\r");
            }

            public string get(string i, params string[] args)
            {
                if (args == null) return get(i);
                if (!formats.ContainsKey(i)) return i;
                return String.Format(formats[i].ToString(), args).Replace("&CRLF", "\r\n").Replace("&CR", "\r");
            }

        }

        internal WorldServer World { get; set; }

        private TcpListener Listning;
        internal readonly List<NetcraftPlayer> players = new List<NetcraftPlayer>();
        private NetcraftPlayer pClient;
        public const string NCORE_WORLDFILE = "./world.json"; 
        public const string NCORE_CONFIGFILE = "./server.properties";
        private readonly SaveLoad sv = new SaveLoad();
        internal int maxPlayers = 20;
        internal string chatFormat = "%1 %2";
        internal int everyBodyAdmin = 0;
        internal int allowFlight = 0;
        internal string motd = "A NCore server";
        internal string name;
        internal int allowQuery = 0;
        internal int commandsConsoleOnly = 0;
        internal int enableAuth = -1;
        internal int enableConsole = 0;
        internal int whitelist = 0;
        internal int autosave = 0;
        internal int netcheatProtect = 0;
        public const int WORLDGEN_CAVE_MIN_HEIGHT = 8;
        public const int WORLDGEN_CAVE_MAX_HEIGHT = 12;
        public Lang lang;
        public TimeSpan keepAliveTimeout = new TimeSpan(0, 0, 0, 5, 0);
        public Player.Permissions permissions;
        public Dictionary<string, string[]> groups;
        public List<string> whitelistedPlayers;

        public bool IsSingleplayerServer { get; private set; } = false;

        public void LoadConfig()
        {
            string cfg = File.ReadAllText(Conversions.ToString(NCORE_CONFIGFILE), Encoding.UTF8);
            string nccfg = File.ReadAllText("./server.cfg", Encoding.UTF8);
            maxPlayers = Conversions.ToInteger(Config.GetValue("max-players", cfg, "-1"));
            chatFormat = Config.GetValue("chat-format", cfg, "%1 %2");
            everyBodyAdmin = Conversions.ToInteger(Config.GetValue("everybody-admin", cfg, "0"));
            motd = Config.GetValue("server-motd", cfg, "null"); 
            name = Config.GetValue("server-name", cfg, "null");
            allowFlight = Conversions.ToInteger(Config.GetValue("allow-flight", cfg, "0"));
            netcheatProtect = Conversions.ToInteger(Config.GetValue("netcheat-protect", cfg, "0"));
            allowQuery = Conversions.ToInteger(Config.GetValue("allow-query", nccfg, "0"));
            commandsConsoleOnly = Conversions.ToInteger(Config.GetValue("commands-console-only", nccfg, "0"));
            enableAuth = Conversions.ToInteger(Config.GetValue("enable-auth", cfg, "0"));
            enableConsole = Conversions.ToInteger(Config.GetValue("enable-csharp-script-console", nccfg, "0"));
            whitelist = Conversions.ToInteger(Config.GetValue("enable-whitelist", cfg, "0"));
            autosave = Conversions.ToInteger(Config.GetValue("enable-autosave", cfg, "0"));
            lang = Lang.FromFile($"./lang/{Config.GetValue("def-lang", cfg, "english")}.txt");
            permissions = JsonConvert.DeserializeObject<Player.Permissions>(File.ReadAllText("./permissions.json", Encoding.UTF8));
            groups = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText("./groups.json", Encoding.UTF8));
            whitelistedPlayers = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("./whitelist.json"));

            if (isIllegalValue(everyBodyAdmin, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(allowFlight, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(allowQuery, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(commandsConsoleOnly, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(enableAuth, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(autosave, 0, 1)) CrashReport(new Exception("Illegal property value"));
            if (isIllegalValue(netcheatProtect, 0, 1)) CrashReport(new Exception("Illegal property value"));

        }

        public string GetApplicationRoot()
        {
            //var exePath = Path.GetDirectoryName(System.Reflection
            //                  .Assembly.GetExecutingAssembly().CodeBase);
            //Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            //var appRoot = appPathMatcher.Match(exePath).Value;
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public async Task LoadScripts()
        {
            ctx = new Context(this);
            Log(GetApplicationRoot());
            foreach(string d in Directory.GetFiles("./scripts"))
            {
                string n = d.Split(Path.DirectorySeparatorChar).Last().Split('.')[0];
                await StartScript(n);
            }
        }

        public async Task StartScript(string name)
        {
            Log("Load script " + name);
            string i = $@"./scripts/{name}.cs";
            string n = i.Split('/', '\\').Last().Split('.')[0];
            ScriptOptions a = ScriptOptions.Default;
            a = a.AddReferences(new string[] { "System", "System.Core", "System.Linq", "System.IO", "System.Text", "System.Threading", "System.Threading.Tasks",
                GetApplicationRoot()});
            var script = await CSharpScript.RunAsync($@"Context ctx = global::NCore.NCore.GetNCore().ctx;void Log(string message, string t = ""INFO"") {{ ctx.Print(""[{n}] "" + message, t); }}", a.WithImports("System", "System.IO", "System.Net", "System.Linq", "NCore", "NCore.netcraft.server.api", "NCore.netcraft.server.api.events"));
            await script.ContinueWithAsync(File.ReadAllText(i), a, (Exception ex) => {
                Log($"Unhandled exception in script '{name}'\r\n" + ex.ToString(), "ERROR");
                return true;
            });
        }
        internal Dictionary<string, ScriptState<object>> scripts = new Dictionary<string, ScriptState<object>>();
        public async Task Eval(string scriptt, NetcraftPlayer p)
        {
            ScriptOptions a = ScriptOptions.Default;
            a = a.AddReferences(new string[] { "System", "System.Core", "System.Linq", "System.IO", "System.Text", "System.Threading", "System.Threading.Tasks",
                GetApplicationRoot()});
            var script = await CSharpScript.RunAsync($@"private readonly NetcraftPlayer self = Netcraft.GetPlayer(""{p.Username}"");Context ctx = global::NCore.NCore.GetNCore().ctx;void Log(string message, string t = ""INFO"") {{ ctx.Print(""[EvalScript] "" + message, t); }}
void help() {{
self.SendLog(@""C# Shell
self > type<NetcraftPlayer> [readonly] returns the script sender player object
NetcraftPlayer#UpdateHealth(int) [async] sets player health
NetcraftPlayer#GetConnection type<TcpClient> returns player TcpClient object
NetcraftPlayer#OpenChest(BlockChest) [async] open a chest to player
NetcraftPlayer#SetNoClip(bool) [async] sets player noclip mode
NetcraftPlayer#Give(Material, int) [async] give item
NetcraftPlayer#UpdateHunger(int) [async] sets player food level
NetcraftPlayer#DoWarning(string) [async] sends warn message to player
NetcraftPlayer#SendLog(string) [async] sends log message
NetcraftPlayer#Damage(int) [async] apply damage
NetcraftPlayer#Teleport(int, int) [async] teleport player
NetcraftPlayer#Chat(string) [async] sends chat message
NetcraftPlayer#EditChatLine(int, string) [async] sends edit chat line packet
NetcraftPlayer#Kick(string, bool) [async] kick player
Netcraft.NCore() type<NCore> returns NCore object
Netcraft.DispatchCommand(CommandSender, string) [async] execute command as another command sender
Netcraft.BanPlayer(string) [async] stop player from joining server
Netcraft.UnbanPlayer(string) [async] allow player joining the server
Netcraft.GetBannedPlayers() type<List<string>> returns list of banned players
Netcraft.IsBanned(string) type<bool> checks if a player is banned
Netcraft.Broadcast(string) [async] broadcasts a chat message
Netcraft.GetWorldTime() [async] type<int> returns world time
Netcraft.SetWorldTime(int) [async] sets world time"");
}}
", a.WithImports("System", "System.IO", "System.Net", "System.Linq", "NCore", "NCore.netcraft.server.api", "NCore.netcraft.server.api.events"));
            if(!scripts.ContainsKey(p.Username))
            {
                scripts.Add(p.Username, script);
            }
            scripts[p.Username] = await scripts[p.Username].ContinueWithAsync(scriptt, a);
        }

        public Context ctx;

        public void OnCrash(object sender, ThreadExceptionEventArgs e)
        {
            CrashReport(e.Exception);
        }

        public async Task<bool> PlaceBlock(EnumBlockType type, Point position, bool bg = false, bool unbreakable = false, bool pq = false)
        {
            Block b = World.GetBlockAt(position);
            if(b == null)
            {
                World.Blocks.Add(new Block(position, type, bg, unbreakable));
                foreach(NetcraftPlayer n in players)
                {
                    await n.SendBlockChange(position, type, false, pq, bg);
                }
                return true;
            }
            return false;
        }

        public async Task<bool> BreakBlock(Point position)
        {
            Block b = World.GetBlockAt(position);
            if (b != null)
            {
                World.Blocks.Remove(b);
                foreach (NetcraftPlayer n in players)
                {
                    await n.Send($"removeblock?{position.X.ToString()}?{position.Y.ToString()}");
                }
                return true;
            }
            return false;
        }

        public void LoadCommands()
        {
            Log("Loading commands...");
            Netcraft.AddCommand(new Commandhelp());
            Netcraft.AddCommand(new Commandban());
            Netcraft.AddCommand(new Commandunban());
            Netcraft.AddCommand(new Commanditems());
            Netcraft.AddCommand(new Commandtoggleadmin());
            Netcraft.AddCommand(new Commandkick());
            Netcraft.AddCommand(new Commandkill());
            Netcraft.AddCommand(new Commandgive());
            Netcraft.AddCommand(new Commandbroadcast());
            Netcraft.AddCommand(new Commandlist());
            Netcraft.AddCommand(new Commandstop());
            Netcraft.AddCommand(new Commandsudo());
            Netcraft.AddCommand(new Commandtppos());
            Netcraft.AddCommand(new Commandversion());
            Netcraft.AddCommand(new Commandaliases());
            Netcraft.AddCommand(new Commandplugins());
            Netcraft.AddCommand(new Commands.Commandsave());
            Netcraft.AddCommand(new Commands.Commandtell());
            Netcraft.AddCommand(new Commands.Commandrmitem());
            Netcraft.AddCommand(new Commands.Commandeval());
            Netcraft.AddCommand(new Commands.Commandgamerule());
            Netcraft.AddCommand(new Commands.Commandtime());
            Netcraft.AddCommand(new Commands.Commandpermissions());
            Log("Default commands loaded!");
            Log($"{Netcraft.GetCommands().Count.ToString()} total commands");
        }

        private void eventhandler_a(string m)
        {
            Log(m);
            Send("chat?" + m);
        }

        private Thread loopThread;
        internal const string NETCRAFT_VERSION = "0.1.7b";
        internal const string NCORE_VERSION = "0.8";
        int hungerDecreaseDelay = 200;
        public DateTime LastTick { get; private set; }
        DateTime lastSave;

        private async void ThreadLoop()
        {
            while (true)
            {
                Thread.Sleep(100);
                //if(lastSave == null)
                //{
                //    lastSave = DateTime.Now.AddSeconds(15);
                //}
                //if(lastSave < DateTime.Now)
                //{
                //    Log("World is autosaving");
                //    SaveWorld();
                //    Log("World was autosaved");
                //    lastSave = DateTime.Now.AddSeconds(15);
                //}
                logAppendDelay -= 1;
                hungerDecreaseDelay -= 1;
                if(hungerDecreaseDelay == 0)
                {
                    hungerDecreaseDelay = 200;
                    foreach(NetcraftPlayer player in players)
                    {
                        if(player.IsLoaded)
                        {
                            if (player.Hunger > 0) await player.UpdateHunger(player.Hunger - 1 + player.Saturation);
                        }
                    }
                }
                if(ctx != null) ctx.loop();
                LastTick = DateTime.Now;
                try
                {
                    for (int i = 0; i < World.Entities.Count; i++)
                    {
                        if (World.Entities.Count - 1 < i) continue;
                        World.Entities[i].Tick();
                    }
                } catch(Exception)
                {

                }
                if (logAppendDelay == 0)
                {
                    logAppendDelay = 40;
                    if (File.Exists("./log.txt"))
                    {
                        File.AppendAllText("./log.txt", toAppend, Encoding.UTF8);
                        toAppend = "";
                    }
                    else
                    {
                        File.WriteAllText("./log.txt", toAppend, Encoding.UTF8);
                        toAppend = "";
                    }

                    File.WriteAllText("./permissions.json", JsonConvert.SerializeObject(permissions), Encoding.UTF8);
                    File.WriteAllText("./whitelist.json", JsonConvert.SerializeObject(whitelistedPlayers), Encoding.UTF8);

                    SaveAuth();
                    try
                    {
                        for (int i = 0; i < World.Blocks.Count; i++)
                        {
                            if (World.Blocks.Count - 1 < i) continue;
                            Block b = World.Blocks[i];
                            if (b.Type == EnumBlockType.SAPLING)
                            {
                                World.Blocks.Remove(b);
                                await Send("removeblock?" + b.Position.X.ToString() + "?" + b.Position.Y.ToString());
                                await TreeGenerator.GrowthTree(b.Position, World, b.IsBackground);
                                Log($"Tree growth at [{b.Position.X.ToString() + ", " + b.Position.Y.ToString()}]");
                                break;
                            }
                            if (b.Type == EnumBlockType.SEEDS)
                            {
                                b.Type = EnumBlockType.WHEAT;
                                await Send($"removeblock?{b.Position.X.ToString()}?{b.Position.Y.ToString()}");
                                await Task.Delay(30);
                                foreach (NetcraftPlayer player in players)
                                    await player.SendBlockChange(b.Position, EnumBlockType.WHEAT, true, false, b.IsBackground);
                                Log($"Wheat growth at [{b.Position.X.ToString() + ", " + b.Position.Y.ToString()}]");
                                //await Send($"blockchange?{b.Position.X.ToString()}?{b.Position.Y.ToString()}?wheat7?non-solid{(b.IsBackground ? "?bg" : "?fg")}");
                                break;
                            }
                        }
                    } catch(IndexOutOfRangeException)
                    {

                    }
                }
                try
                {
                    foreach (NetcraftPlayer n in players)
                    {
                        if (n.Hunger == 0) await n.Damage(5, "died");
                        
                        foreach (var b in World.Blocks)
                        {
                            var bpos = new Point(b.Position.X * 32, b.Position.Y * 32);
                            var brec = new Rectangle(bpos, new Size(32, 32));
                            if (DistanceBetweenPoint(bpos, n.Position) > 10 * 32)
                                continue;
                            if (bpos.Y > n.Position.Y + 85)
                                continue;
                            if (b.IsBackground)
                                continue;
                            if (b.Type == EnumBlockType.WATER)
                                continue;
                            if (b.Type == EnumBlockType.SAPLING)
                                continue;
                            if (brec.IntersectsWith(n.PlayerRectangle))
                            {
                                if (n.NoClip)
                                    break;
                                if (b.Type == EnumBlockType.LAVA)
                                {
                                    await n.Damage(10, "deathmessage.lava");
                                    break;
                                }
                                if (b.Type == EnumBlockType.FIRE)
                                {
                                    await n.Damage(10, "deathmessage.fire");
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    for (int i = 0; i <= World.Blocks.Count; i++)
                    {
                        if (World.Blocks.Count - 1 < i) continue;
                        Block b = World.Blocks[i];
                        if (b.Type == EnumBlockType.CHEST)
                        {
                            if (World.GetChestAt(b.Position) == null)
                            {
                                BlockChest chest = new BlockChest(b.Position, b.IsBackground);
                                World.Chests.Add(chest);
                            }
                        }
                        else if(b.Type == EnumBlockType.SAND || b.Type == EnumBlockType.GRAVEL)
                        {
                            if(World.Gamerules.blockPhysics)
                            {
                                if (World.GetBlockAt(b.Position.X, b.Position.Y + 1) == null)
                                {
                                    World.Blocks.Remove(b);
                                    Point moveTo = new Point(b.Position.X, b.Position.Y + 1);
                                    netcraft.server.api.events.SandPhysicsEvent ev = new netcraft.server.api.events.SandPhysicsEvent(b, b.Position, moveTo);
                                    NCSApi.RESandPhysicsEvent(ev);
                                    moveTo = ev.GetTo();
                                    if (ev.GetCancelled()) continue;
                                    await Send("removeblock?" + b.Position.X.ToString() + "?" + b.Position.Y.ToString());
                                    b.Position = moveTo;
                                    foreach (NetcraftPlayer p in Netcraft.GetOnlinePlayers())
                                    {
                                        await p.SendBlockChange(b.Position, b.Type, false, false, b.IsBackground);
                                    }
                                    World.Blocks.Add(b);
                                }
                            }
                        }
                        else { continue; }
                    }
                    foreach (BlockChest b in World.Chests)
                    {
                        List<ItemStack> itemsToRemove = new List<ItemStack>();
                        foreach (ItemStack i in b.items)
                        {
                            if (i.Count < 1)
                            {
                                itemsToRemove.Add(i);
                            }
                        }
                        foreach (ItemStack i in itemsToRemove)
                        {
                            b.items.Remove(i);
                        }
                        if (itemsToRemove.Count > 0) Log($"Removed {itemsToRemove.Count} zero-count items in chest at [{b.Position.X.ToString()},{b.Position.Y.ToString()}]", "WARNING");
                    }
                } catch(Exception)
                {

                }
                

                //problem detected ---- Console.Title = $"NCore {NCORE_VERSION} (Netcraft {NETCRAFT_VERSION}) | {players.Count}/{maxPlayers.ToString()} players | {(Process.GetCurrentProcess().WorkingSet64 / 1024L / 1024L).ToString().Split('.')[0]}MB used of {Process.GetCurrentProcess().MaxWorkingSet.ToInt64() / 1024L}MB, {(Process.GetCurrentProcess().MaxWorkingSet.ToInt64() / 1024L - Process.GetCurrentProcess().WorkingSet64 / 1024L / 1024L).ToString().Split('.')[0]}MB free | Total Processor Time: {Process.GetCurrentProcess().TotalProcessorTime.ToString()} | Uptime: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString().Split(".")[0]}";
            }
        }

        internal List<Color> skyClr = new List<Color>();
        internal int worldtime;
        private int stp;
        private Thread daytimeThread;

        public void daytimeThreadLoop()
        {
            while (true)
            {
                Thread.Sleep(15000);
                if(World.Gamerules.daylightCycle)
                {
                    if (worldtime >= skyClr.Count - 1)
                    {
                        worldtime = 0;
                    }

                    if (worldtime <= 0)
                    {
                        stp = 1;

                    }

                    worldtime += stp;
                    BroadcastSkyChange(skyClr[worldtime]);
                }
            }
        }

        public async Task BroadcastSkyChange(Color clr)
        {
            try
            {
                foreach (var gg in players)
                    await gg.SendSkyColorChange(clr);
            }
            catch (Exception ex)
            {
            }
        }

        public void GametimeInitialize()
        {
            stp = 1;
            worldtime = 0;
            daytimeThread = new Thread(daytimeThreadLoop);
            daytimeThread.Name = "World Time";
            daytimeThread.Start();

            skyClr.Add(Color.Black); // ночь
            skyClr.Add(Color.Black); // ночь
            skyClr.Add(Color.Black); // ночь
            skyClr.Add(Color.Black); // ночь
            skyClr.Add(Color.DarkSlateBlue); // рассвет/закат
            skyClr.Add(Color.DarkBlue); // рассвет/закат
            skyClr.Add(Color.MediumBlue); // рассвет/закат
            skyClr.Add(Color.Blue); // день
            skyClr.Add(Color.DodgerBlue);
            skyClr.Add(Color.LightBlue);
            skyClr.Add(Color.DeepSkyBlue);
            skyClr.Add(Color.MediumBlue);
            skyClr.Add(Color.DarkBlue);
            skyClr.Add(Color.DarkSlateBlue);
            skyClr.Add(Color.Black);
            skyClr.Add(Color.Black);
            skyClr.Add(Color.Black);
            skyClr.Add(Color.Black);
        }

        public void ThreadAdd()
        {
            if (!threads.Contains(Thread.CurrentThread))
                threads.Add(Thread.CurrentThread);
        }

        

        public void watchdogThreadLoop()
        {
            ThreadAdd();
            Thread.CurrentThread.Name = "Watchdog";
            while (true)
            {
                Thread.Sleep(100);
                try
                {
                    if(LastTick.AddSeconds(10) < DateTime.Now)
                    {
                        Console.Title = $"NCore (not responding for {(DateTime.Now - LastTick).TotalSeconds.ToString()} seconds)";
                        //Log($"SERVER IS NOT RESPONDING!", "ERROR");
                        //Log($"Watchdog detected server lag!", "ERROR");
                        //Log($"The server has not responded " + (DateTime.Now - LastTick).TotalSeconds + " seconds!");
                        if((DateTime.Now - LastTick).TotalSeconds > 30)
                        {
                            Log($"SERVER HAS NOT RESPONDED WITH IN 30 SECONDS! STOPPING", "ERROR");
                            Environment.Exit(-1);
                        }
                    }
                    foreach (var th in threads)
                    {
                        if (!th.IsAlive)
                        {
                            Log($"Watchdog detected thread death. Thread unexpectedly stopped working. Thread ID: {th.ManagedThreadId}", "ERROR");
                            threads.Remove(th);
                        }

                        if (th.ThreadState == System.Threading.ThreadState.Suspended)
                        {
                            Log($"Watchdog detected thread death. Thread unexpectedly suspended.", "ERROR");
                            threads.Remove(th);
                        }

                        if (th.ThreadState == System.Threading.ThreadState.Aborted)
                        {
                            Log($"Watchdog detected thread death. Thread unexpectedly aborted.", "ERROR");
                            threads.Remove(th);
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                }
                catch (Exception ex)
                {
                    Log($"Error in watchdog thread:\r\n{ex.ToString()}", "ERROR");
                }
            }
        }

        private List<Thread> threads = new List<Thread>();
        private Thread threadWatchdog;
        private int logAppendDelay = 40;

        public static void Main(string[] args)
        {
            try
            {
                NCore app = new NCore();
                nCore = app;
                app.LastTick = DateTime.Now.AddSeconds(10);
                app.Server();
                if (args.Length == 1 && args[0] == "s")
                {
                    app.IsSingleplayerServer = true;
                }
            } catch (Exception e)
            {
                CrashReport(e);
                Console.ReadLine();
            }

            //Console.WriteLine("Что Вы хотите сделать?\r\n1 - запустить сервер");
            //char result = Console.ReadKey().KeyChar;
            //Console.WriteLine("\b");
            //if(result == '1')
            //{
            //    try
            //    {
            //        NCore instance = new NCore();
            //        nCore = instance;
            //        instance.Server();
            //    } catch(Exception ex)
            //    {
            //        Console.WriteLine(ex.ToString());
            //    }
                
            //}
        }

        static NCore nCore;
        public static NCore GetNCore()
        {
            return nCore;
        }

        

        public async Task Server()
        {
            try
            {
                ThreadAdd();
                Log($"PID: {Process.GetCurrentProcess().Id} | Netcraft Version: {NETCRAFT_VERSION}");
                Console.WriteLine(@"             _                  __ _   
  _ __   ___| |_ ___ _ __ __ _ / _| |_ 
 | '_ \ / _ \ __/ __| '__/ _` | |_| __|
 | | | |  __/ || (__| | | (_| |  _| |_ 
 |_| |_|\___|\__\___|_|  \__,_|_|  \__|
                                       ");
                if (Thread.CurrentThread.ManagedThreadId != 1)
                {
                    CrashReport(new Exception("The main thread ID was not 1"));
                }

                LoadConfig();
                //lang = Lang.FromFile("./lang.txt");
                
                Log(lang.get("server.starting"));

                Log("This is information");
                Log("WARNING!", "WARNING");
                Log("SEVERE!!!!", "SEVERE");
                Log("ERRROROORR!!!!!!!!!", "ERROR");
                loopThread = new Thread(ThreadLoop);
                loopThread.Name = "Loop";
                loopThread.Start();
                threadWatchdog = new Thread(watchdogThreadLoop);
                threadWatchdog.Start();
                    
                GametimeInitialize();

                Thread.CurrentThread.Name = "Main";

                LoadBanlist();
                foreach (var i in File.ReadAllText("./auth.txt", Encoding.UTF8).Split('\n'))
                {
                    if (i.Length < 2)
                        continue;
                    if (playerPasswords.ContainsKey(i.Split('=')[0]))
                        continue;
                    playerPasswords.Add(i.Split('=')[0], i.Split('=').Last());
                }
                LoadCommands();
                Netcraft.dobc += eventhandler_a;

                int pluginLoadErrors = 0;
                int all = 0;
                int pluginLoadSuccess = 0;
                foreach (var s in Directory.GetFiles("./plugins"))
                {
                    if (s.Split('.').Last() != "dll") continue;

                    all++;

                    var p = PluginManager.Load(s);
                    string result = p.OnLoad();
                    if (result != null)
                    {
                        pluginLoadErrors++;
                        Log(lang.get("pluginloader.error", p.Name, result), "ERROR");
                        PluginManager.Plugins.Remove(p);
                        continue;
                    }

                    Log(lang.get("pluginloader.success", p.Name));
                    pluginLoadSuccess++;
                }

                if (File.Exists(Conversions.ToString(NCORE_WORLDFILE)))
                {
                    World = sv.Load(File.ReadAllText(Conversions.ToString(NCORE_WORLDFILE)));
                    List<Block> blocksToRemove = new List<Block>();
                    foreach (Block b in World.Blocks)
                    {
                        if (b.Position.X > 64)
                        {
                            blocksToRemove.Add(b);
                        }
                        if (b.Position.Y > 17)
                        {
                            blocksToRemove.Add(b);
                        }
                    }
                    foreach (Block b in blocksToRemove)
                    {
                        World.Blocks.Remove(b);
                    }
                    if (blocksToRemove.Count > 0) Log(lang.get("info.removedblocks", blocksToRemove.Count.ToString()), "WARNING");

                }
                else
                {
                    World = WorldGenerator.Generate();
                    File.WriteAllText(Conversions.ToString(NCORE_WORLDFILE), sv.Save(World));
                }

                await LoadScripts();

                Start();

                Log(lang.get("server.started"));
                ctx.started();
                while (true)
                {
                    string m = Console.ReadLine();
                    var args = m.Split(' ').Skip(1).ToArray();
                    string cmd = m.Split(' ')[0];
                    var toRun = default(Command);
                    foreach (var local_a in Netcraft.GetCommands())
                    {
                        if ((local_a.Name.ToLower() ?? "") == (cmd.ToLower() ?? ""))
                        {
                            toRun = local_a;
                            break;
                        }
                        if (local_a.Aliases.Contains(cmd.ToLower()))
                        {
                            toRun = local_a;
                            break;
                        }
                    }

                    if (IsNothing(toRun))
                    {
                        Log(lang.get("command.unknown"));
                    }
                    else
                    {
                        bool y;
                        try
                        {
                            y = await toRun.OnCommand(Netcraft.ConsoleCommandSender, toRun, args, m);
                        }
                        catch (Exception ex)
                        {
                            Log(lang.get("command.console.error", ex.ToString()));
                            y = true;
                        }

                        if (!y)
                        {
                            Log(lang.get("command.usage", toRun.Usage));
                        }
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        string lastLog = "";
        public void writeline(string str)
        {
            ThreadAdd();
            Console.WriteLine(str);
            lastLog = str;
        }

        public delegate void xSend(string str, string except, bool readyClientsOnly);

        public async Task Send(string str, string except = null, bool readyClientsOnly = false)
        {
            ThreadAdd();
            for (int x = 0, loopTo = players.Count - 1; x <= loopTo; x++)
            {
                try
                {
                    if (except != null)
                    {
                        if ((players[x].Username ?? "") == (except ?? ""))
                            continue;
                    }

                    if (readyClientsOnly)
                    {
                        if (players[x].IsLoaded)
                            await players[x].Send(str);
                    }
                    else
                    {
                        await players[x].Send(str);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        players.RemoveAt(x);
                    }
                    catch (Exception ex1)
                    {
                    }
                }
            }
        }

        public async Task DoWaterPhysics(int x, int y)
        {
            Block b = World.GetBlockAt(x, y);
            if (b.Type == EnumBlockType.WATER)
            {
                Block b1 = World.GetBlockAt(new Point(b.Position.X, b.Position.Y + 1));
                Block b2 = World.GetBlockAt(new Point(b.Position.X - 1, b.Position.Y));
                Block b3 = World.GetBlockAt(new Point(b.Position.X + 1, b.Position.Y));
                if (b1 == null)
                {
                    Block _b1 = new Block(new Point(b.Position.X, b.Position.Y + 1), b.Type, b.Unbreakable, b.IsBackground);
                    World.Blocks.Add(_b1);
                    foreach (NetcraftPlayer p in players)
                    {
                        await p.SendBlockChange(_b1.Position, _b1.Type, true, false, _b1.IsBackground);
                    }
                }
                if (b2 == null)
                {
                    Block _b1 = new Block(new Point(b.Position.X - 1, b.Position.Y), b.Type, b.Unbreakable, b.IsBackground);
                    World.Blocks.Add(_b1);
                    foreach (NetcraftPlayer p in players)
                    {
                        await p.SendBlockChange(_b1.Position, _b1.Type, true, false, _b1.IsBackground);
                    }
                }
                if (b3 == null)
                {
                    Block _b1 = new Block(new Point(b.Position.X + 1, b.Position.Y), b.Type, b.Unbreakable, b.IsBackground);
                    World.Blocks.Add(_b1);
                    foreach (NetcraftPlayer p in players)
                    {
                        await p.SendBlockChange(_b1.Position, _b1.Type, true, false, _b1.IsBackground);
                    }
                }
            }
        }

        public async Task BroadcastChatTranslation(string translationKey, string[] format, string except = null, bool readyClientsOnly = false)
        {
            ThreadAdd();
            for (int x = 0, loopTo = players.Count - 1; x <= loopTo; x++)
            {
                try
                {
                    if (except != null)
                    {
                        if ((players[x].Username ?? "") == (except ?? ""))
                            continue;
                    }
                    if (readyClientsOnly)
                    {
                        if (players[x].IsLoaded)
                            await players[x].Chat(players[x].lang.get(translationKey, format));
                    }
                    else
                    {
                        await players[x].Chat(players[x].lang.get(translationKey, format));
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        players.RemoveAt(x);
                    }
                    catch (Exception ex1)
                    {
                    }
                }
            }
        }

        public void SaveBanlist()
        {
            ThreadAdd();
            string a = "";
            foreach (var b in Netcraft.GetBannedPlayers())
                a += b + Constants.vbLf;
            File.WriteAllText("banned-players.txt", a, Encoding.UTF8);
        }

        public void LoadBanlist()
        {
            ThreadAdd();
            foreach (var a in File.ReadAllText("banned-players.txt", Encoding.UTF8).Split('\n'))
                Netcraft.field_a.Add(a);
        }

        public async void AcceptClient(IAsyncResult ar)
        {
            ThreadAdd();
            try
            {
                if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                    Thread.CurrentThread.Name = "Network Join";
                pClient = new NetcraftPlayer(Listning.EndAcceptTcpClient(ar));
                pClient.a += MessageReceived;
                pClient.b += (_) => NCore.GetNCore().handleDisconnection(pClient, false);
                players.Add(pClient);
                Netcraft.clientList = players;
                Listning.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), Listning);
                Log(lang.get("client.connect",  pClient.GetIp()));
            }
            catch (Exception ex)
            {
            }
        }

        private string toAppend = "";

        internal void Log(string arg0, string arg1 = "INFO")
        {
            ThreadAdd();
            arg0 = arg0.Replace("&0", "").Replace("&1", "").Replace("&2", "").Replace("&3", "").Replace("&4", "").Replace("&5", "").Replace("&r", "");
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.Write($"{DateTime.Now.ToString()} [");
            if (arg1 == "ERROR")
                Console.ForegroundColor = ConsoleColor.DarkRed;
            if (arg1 == "SEVERE")
                Console.ForegroundColor = ConsoleColor.Magenta;
            if (arg1 == "WARNING")
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            if (arg1 == "INFO")
                Console.ForegroundColor = ConsoleColor.White;
            //Console.Write(arg1);
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.Write($"] {arg0}\n");
            //UpdateList($"[{DateTime.Now} {arg1}] [Thread {Thread.CurrentThread.ManagedThreadId}]: {arg0}");
            writeline($"[{DateTime.Now} {arg1}]: {arg0}");
            toAppend += $"[{DateTime.Now} {arg1}] [Thread {Thread.CurrentThread.ManagedThreadId}]: {arg0}" + Constants.vbCrLf;
            Console.ForegroundColor = ConsoleColor.White;
        }

        internal void LogPlugin(string arg0, Plugin arg2, string arg1 = "INFO")
        {
            ThreadAdd();
            Log($"{arg2.Name}: {arg0}", arg1);
        }

        public void LogError(Exception ex)
        {
            ThreadAdd();
            Log($"Exception in thread {Thread.CurrentThread.ManagedThreadId.ToString()}:" + Constants.vbCrLf + ex.ToString(), "ERROR");
        }

        private Thread MainThread;
        private bool ToExit = false;

        public void StopServer()
        {
            Log(lang.get("server.stopping"));
            foreach (var plugin in PluginManager.Plugins)
            {
                plugin.OnUnload();
                Log(lang.get("pluginloader.unload", plugin.Name));
            }

            Kickall("Server closed");
            Listning.Stop();
            SaveWorld();
            SaveBanlist();
            Process.GetCurrentProcess().Kill();
        }

        private bool isIllegalValue(object v, params object[] vs)
        {
            if (!vs.Contains(v)) return true;
            return false;
        }

        public async Task Kickall(string m)
        {
            for(int i = 0; i < players.Count; i++)
            {
                try
                {
                    players[i].Kick(m);
                } catch(NullReferenceException)
                {

                }
            }
        }
        const string RCON_PASSWORD = "12344321";
        private Hashtable playerPasswords = new Hashtable();
        private StringCollection loggedIn = new StringCollection();
        const bool ENABLE_STATISTICS_EXPERIMENTAL = true;

        public async void MessageReceived(string str, NetcraftPlayer n)
        {
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name = "Network Packet";
            ThreadAdd();
            if(str == "")
            {
                if (n.IsLoaded) return;
                n.Disconnect();
                Log(n.GetIp() + " disconnected for bad packet", "WARNING");
                players.Remove(n);
                return;

            }
            if(str.Length > 5000)
            {
                n.Disconnect();
                Log(n.GetIp() + " disconnected for too long packet", "WARNING");
                players.Remove(n);
                return;
            }
            try
            {
                var a = str.Split('?');
                if (str == "ping")
                {
                    Log($"Pinged from {n.GetIp()}");
                    if (allowQuery == 1)
                    {
                        await n.Send("name?" + name);
                        await Task.Delay(10);
                        await n.Send("motd?" + motd);
                        await Task.Delay(10);
                        await n.Send("players?" + players.Where(x => x.IsLoaded).ToArray().Length.ToString() + "/" + maxPlayers.ToString());
                    }
                    else
                    {
                        await n.Send("name?No Query");
                        await Task.Delay(10);
                        await n.Send("motd?No Query");
                        await Task.Delay(10);
                        await n.Send("players?No Query");
                    }

                    n.Disconnect();
                    players.Remove(n);
                    return;
                }
                if(a[0] == "setuprcon")
                {
                    if (a[1] == RCON_PASSWORD) 
                    {
                        n.Username = "RCON";
                        n.IsAdmin = true;
                        n.IsRconClient = true;
                    }
                }
                if(n.IsRconClient)
                {
                    if(a[0] == "cmd")
                    {
                        string cmd = string.Join("?", a.Skip(1).ToArray());
                        Log(this.lang.get("command.requested", "RCON", cmd));
                        await Netcraft.DispatchCommand(Netcraft.RconCommandSender, cmd);
                    }
                    return;
                }
                Lang lang = n.lang;
                if(str.Length < 1)
                {
                    n.Kick("Too short packet.");
                    return;
                }
                if(a[0] == "netcheat")
                {
                    await n.Kick("Netcheat is not allowed on this server!");
                    Log($"{n.Username}[/{n.GetIp()}] was kicked for logging in with NetCheat", "WARNING");
                    return;
                }
                if (a[0] == "setname")
                {
                    if (!string.IsNullOrEmpty(n.Username))
                    {
                        return;
                    }
                    n.lang = Lang.FromFile("./lang/" + a[2] + ".txt");
                    lang = n.lang;
                    Log(this.lang.get("console.joined", a[1], n.GetIp()));
                    if(whitelist == 1)
                    {
                        if(!whitelistedPlayers.Any(x => x.ToLower() == a[1].ToLower()))
                        {
                            await n.Kick("You are not whitelisted on this server.");
                            return;
                        }
                    }
                    if (maxPlayers + 1 == players.Where(x => x.IsLoaded).ToArray().Length)
                    {
                        await n.Kick("Сервер заполнен!");
                        return;
                    }

                    foreach (var netplayer in players)
                    {
                        if (netplayer.UUID == n.UUID) continue;
                        if (netplayer.Username == a[1])
                        {
                            await n.Kick(lang.get("error.alreadyplaying"));
                            return;
                        }
                    }



                    n.Username = a[1];
                    n.senderName = n.Username;
                    n.Sprite = a[3];

                    if (!Regex.Match(n.Username, "^[a-zA-Z0-9_]*").Success)
                    {
                        await n.Kick(lang.get("error.invalidnick"));
                        return;
                    }

                    if (Netcraft.IsBanned(n.Username))
                    {
                        await n.Kick(lang.get("error.banned"));
                        return;
                    }

                    if(enableAuth == 1)
                    {

                    } else if(enableAuth == 0)
                    {
                        loggedIn.Add(n.Username);
                        n.IsAuthorized = true;
                    } else
                    {
                        CrashReport(new Exception("Invalid config property value"));
                    }

                    await Send("addplayer?" + a[1] + "?" + a[3], n.Username);
                    n.PlayerInventory = new Inventory(n);
                    if (File.Exists(Conversions.ToString(Operators.AddObject(Operators.AddObject(Operators.AddObject(Application.StartupPath, "/playerdata/"), n.Username), ".json"))))
                    {
                        PlayerInfoSaveLoad.Load(n, File.ReadAllText(Conversions.ToString(Operators.AddObject(Operators.AddObject(Operators.AddObject(Application.StartupPath, "/playerdata/"), n.Username), ".txt")), Encoding.UTF8));
                    }
                    else
                    {
                        await n.PlayerInventory.AddItem(new ItemStack(Material.WOODEN_PICKAXE, 1));
                        await n.PlayerInventory.AddItem(new ItemStack(Material.WOODEN_AXE, 1));
                        await n.PlayerInventory.AddItem(new ItemStack(Material.WOODEN_SWORD, 1));
                        await n.PlayerInventory.AddItem(new ItemStack(Material.WOODEN_SHOVEL, 1));
                    }
                    if (permissions.GetPermissions(n.Username) != null) permissions.AddPlayer(n.Username);

                    Log(this.lang.get("player.joined", a[1]));
                    await Chat("&3" + this.lang.get("player.joined", a[1]));


                    // n.Send("blockchange?500?50?Red")
                }

                if (string.IsNullOrEmpty(n.Username))
                {
                    await n.Kick("You can't send another packets");
                    return;
                }

                n.senderAdmin = n.IsAdmin;
                if (a[0] == "world")
                {
                    foreach (var b in World.Blocks)
                        await n.SendBlockChange(b.Position, b.Type, packetQueue: true, isBackground: b.IsBackground);
                    await n.PacketQueue.SendQueue();
                    foreach (var p in players)
                    {
                        if (p.UUID == n.UUID)
                            continue;
                        if (p.IsSpectator)
                            continue;
                        await Task.Delay(100);
                        await n.PacketQueue.AddQueue("addplayer?" + p.Username + "?" + p.Sprite);
                        await Task.Delay(100);
                        await n.PacketQueue.AddQueue("moveplayer?" + p.Username + "?" + p.Position.X.ToString() + "?" + p.Position.Y.ToString());
                        
                        try
                        {
                            if (IsNothing(p.SelectedItem))
                                continue;
                            await Task.Delay(100);
                            await n.PacketQueue.AddQueue("itemset?" + p.Username + "?" + p.SelectedItem.Type.ToString());
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                    }
                    foreach(var en in World.Entities)
                    {
                        await n.PacketQueue.AddQueue("addentity?" + en.UUID + "?" + en.GetEntityType());
                        await Task.Delay(100);
                    }
                    await n.PacketQueue.SendQueue();
                    n.IsLoaded = true;
                    await Task.Delay(100);
                    await n.Send("completeload");
                    await Task.Delay(100);
                    if(enableAuth == 1) await n.Chat(lang.get("auth.require"));
                    if (everyBodyAdmin == 1)
                    {
                        n.IsAdmin = true;
                    }

                    var ev = new netcraft.server.api.events.PlayerJoinEventArgs(n);
                    NCSApi.REPlayerJoinEvent(ev);
                }

                if (!loggedIn.Contains(n.Username) && (enableAuth == 1))
                {
                    if (a[0] == "chat")
                    {
                        if (!Regex.Match(a[1], "^[a-zA-Z0-9_]*").Success)
                        {
                            n.SendMessage("Invalid password.");
                        }

                        if (!playerPasswords.Keys.Cast<string>().ToArray().Contains(n.Username))
                        {
                            playerPasswords.Add(n.Username, a[1]);
                            Log($"{n.Username} registered from {n.GetIp()}!");
                        }

                        if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(playerPasswords[n.Username], a[1], false)))
                        {
                            await n.SendMessage(lang.get("auth.success.login"));
                            loggedIn.Add(n.Username);
                            Log($"{n.Username} logged in!");
                            n.Send("teleport?" + n.Position.X.ToString() + "?" + n.Position.Y.ToString());
                            n.IsAuthorized = true;
                            netcraft.server.api.events.PlayerLoginEventArgs ev = new netcraft.server.api.events.PlayerLoginEventArgs(n);
                            NCSApi.REPlayerLoginEvent(ev);
                        }
                        else
                        {
                            Log($"{n.Username} used wrong password", "ERROR");
                            await n.SendMessage(lang.get("auth.password.wrong"));
                        }
                    }

                    return;
                }

                if (a[0] == "craft")
                {
                    try
                    {
                        var m = Enum.Parse(typeof(Material), a[1].ToUpper());
                        n.Craft((Material)m);
                    }
                    catch (Exception ex)
                    {
                        await n.SendMessage("An internal error occured.");
                    }
                }

                if(a[0] == "eval")
                {
                    if(enableConsole != 1)
                    {
                        await n.SendLog("Console is disabled");
                        return;
                    }
                    try
                    {
                        string m = string.Join("?", a.Skip(1).ToArray());
                        if(!n.IsAdmin)
                        {
                            await n.Message("No permissions", 2);
                            return;
                        }
                        await Eval(m, n);
                    } catch(Exception ex)
                    {
                        await n.Send("evalresult?" + ex.ToString().Replace("\r\n", "\r").Replace("\n", "\r"));
                    }
                }

                if(a[0] == "splititems")
                {
                    ItemStack toSplit = null;
                    foreach(ItemStack i in n.PlayerInventory.Items)
                    {
                        if(a[1] == $"{i.Type.ToString()} x {i.Count.ToString()}")
                        {
                            toSplit = i;
                            break;
                        }
                    }
                    if (toSplit == null) return;
                    toSplit.Count -= 1;
                    if (toSplit.Count < 1)
                    {
                        toSplit.Count += 1;
                        return;
                    }
                    n.PlayerInventory.Items.Add(new ItemStack(toSplit.Type, 1));
                    await n.UpdateInventory();
                }

                if (a[0] == "entityplayermove")
                {
                    var mto = new Point(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2]));
                    //n.PlayerRectangle = new Rectangle(mto, new Size(37, 72));
                    var ev = new netcraft.server.api.events.PlayerMoveEventArgs(n.Position, mto, n);
                    if (ev.GetCancelled())
                    {
                        await n.Send($"teleport?{n.Position.X.ToString()}?{n.Position.Y.ToString()}");
                        return;
                    }

                    
                    var v = Normalize(mto - (Size)n.Position);
                    if (!n.DisableMovementCheck)
                    {
                        if (v.X > 10 | v.Y > 10 | v.X < -10 | v.Y < -10)
                        {
                            Log(lang.get("move.toofast", n.Username, v.X.ToString(), v.Y.ToString()), "WARNING");
                            await n.Send($"teleport?{n.Position.X}?{n.Position.Y}");
                            return;
                        }

                        if (allowFlight == 0)
                        {
                            if (mto.Y - n.Position.Y > 0)
                            {
                                if(n.MovedInAir != 50) n.MovedInAir--;
                            }

                            if (!n.IsOnGround)
                            {
                                n.MovedInAir++;
                                if (n.MovedInAir == 50)
                                {
                                    n.AntiFlyWarnings++;
                                    Log(lang.get("move.wrong", n.Username, "Flight", v.X.ToString(), v.Y.ToString()), "WARNING");
                                    if(n.AntiFlyWarnings == 10)
                                    {
                                        await n.Kick("Flying is not enabled on this server");
                                    }
                                    return;
                                }
                            }
                        }

                        foreach (var b in World.Blocks)
                        {
                            var bpos = new Point(b.Position.X * 32, b.Position.Y * 32);
                            var brec = new Rectangle(bpos, new Size(32, 32));
                            if (DistanceBetweenPoint(bpos, n.Position) > 10 * 32)
                                continue;
                            if (bpos.Y > n.Position.Y + 85)
                                continue;
                            if (n.NoClip)
                                break;
                            if (brec.IntersectsWith(new Rectangle(mto, new Size(37, 70))))
                            {
                                if (b.IsBackground)
                                    continue;
                                if (b.Type == EnumBlockType.WATER)
                                    continue;
                                if (b.Type == EnumBlockType.LAVA)
                                    continue;
                                if (b.Type == EnumBlockType.FIRE)
                                    continue;
                                if (b.Type == EnumBlockType.SAPLING)
                                    continue;
                                if (b.Type == EnumBlockType.SEEDS)
                                    continue;
                                if (b.Type == EnumBlockType.WHEAT)
                                    continue;
                                if (b.Type == EnumBlockType.RED_FLOWER)
                                    continue;
                                if (b.Type == EnumBlockType.YELLOW_FLOWER)
                                    continue;
                                //Log(lang.get("move.wrong", n.Username, "Into block", v.X.ToString(), v.Y.ToString()), "WARNING");
                                if (v.Y > 1)
                                {
                                    await n.Teleport(n.Position.X, n.Position.Y - 2);
                                }
                                else
                                {
                                    await n.Send($"teleport?{n.Position.X}?{n.Position.Y}");
                                }
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    bool noPosUpdate = false;
                    if (mto.Y > n.Position.Y)
                    {
                        bool grounded = false;
                        bool inWater = false;
                        foreach (var b in World.Blocks)
                        {
                            var bpos = new Point(b.Position.X * 32, b.Position.Y * 32);
                            var brec = new Rectangle(bpos, new Size(32, 32));
                            if (DistanceBetweenPoint(bpos, n.Position) > 10 * 32)
                                continue;
                            if (brec.IntersectsWith(new Rectangle(mto, new Size(37, 72))))
                            {
                                if (b.IsBackground)
                                    continue;
                                if (b.Type == EnumBlockType.WATER)
                                    inWater = true;
                                grounded = true;
                                //break;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (!grounded)
                        {
                            n.FallDistance += 1;
                            if (inWater)
                            {
                                n.FallDistance = 0;
                            }
                        }
                        else
                        {
                            if (inWater)
                            {
                                n.FallDistance = 0;
                            }

                            if (n.FallDistance > 3 * 32)
                            {
                                await n.Damage(n.FallDistance / 16 / 3, "deathmessage.fall");
                                noPosUpdate = true;
                            }

                            n.FallDistance = 0;
                        }
                    }


                    if (!noPosUpdate) n.Position = mto;


                    if (n.IsSpectator)
                        return;
                    if (n.Position.Y > 619)
                    {
                        await n.Damage(1, "deathmessage.out");
                    }

                    await Send("updateplayerposition?" + n.Username + "?" + a[1] + "?" + a[2], n.Username);
                }

                try
                {
                    if (a[0] == "chat")
                    {
                        string message = string.Join("?", a.Skip(1).ToArray());
                        var ev = new netcraft.server.api.events.PlayerChatEventArgs(n, message);
                        NCSApi.REPlayerChatEvent(ev);
                        if (ev.GetCancelled())
                            return;
                        if (message.Length == 0) return;
                        if(!n.HasPermission("netcraft.bypass.spam"))
                        {
                            if(n.MessagePacketTimeout > DateTime.Now)
                            {
                                n.MessageTimeoutWarnings++;
                                if(n.MessageTimeoutWarnings > 10)
                                {
                                    await n.Kick("Kicked for spamming");
                                    Log(n.Username + " was kicked for spamming");
                                    return;
                                }
                            }
                            n.MessagePacketTimeout = DateTime.Now.AddSeconds(2);
                        }
                        if (message[0] == '/')
                        {
                            if (commandsConsoleOnly == 1)
                            {
                                await n.SendMessage(lang.get("command.disabled"));
                                return;
                            }

                            var arr = message.Split(' ');
                            var lbl = message.Skip(1).ToArray();
                            string label = new string(lbl);
                            var args = arr.Skip(1).ToArray();
                            Command cmd = null;
                            Log(lang.get("command.requested", n.Username, label));
                            foreach (var g in Netcraft.GetCommands())
                            {
                                if (g.Name.ToLower() == label.Split(' ')[0].ToLower())
                                {
                                    cmd = g;
                                    break;
                                }
                                if(g.Aliases.Contains(label.Split(' ')[0].ToLower()))
                                {
                                    cmd = g;
                                    break;
                                }
                            }

                            if (IsNothing(cmd))
                            {
                                await n.SendMessage(lang.get("command.unknown"));
                                return;
                            }
                            if(!n.HasPermission(cmd.Permission))
                            {
                                await n.SendMessage(lang.get("commands.generic.error.no-perms"));
                                return;
                            }
                            bool y;
                            try
                            {
                                
                                y = await cmd.OnCommand(n, cmd, args, label);
                            }
                            catch(CommandException ex)
                            {
                                await n.SendMessage(lang.get("commands.generic.error", ex.GetType().ToString(), ex.Message));
                                return;
                            }
                            catch (Exception ex)
                            {
                                await n.SendMessage(lang.get("command.player.error", ex.HResult.ToString(), ex.Message));
                                Log($"Command error occured in \"{cmd.Name}\" performing by {n.Username}:", "WARNING");
                                Log(ex.ToString(), "WARNING");
                                return;
                            }

                            if (!y)
                            {
                                await n.SendMessage(lang.get("command.usage", cmd.Usage));
                            }
                            return;
                        }
                        else
                        {
                            await Send("chat?" + chatFormat.Replace("%1", n.Username).Replace("%2", message));
                            Log(chatFormat.Replace("%1", n.Username).Replace("%2", message));
                        }
                    }
                }
                catch (IndexOutOfRangeException ex)
                {
                    LogError(ex);
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    Log(ex.ToString(), "ERROR");
                    CrashReport(ex);
                }

                try
                {
                    if (a[0] == "update_inventory")
                    {
                        await n.UpdateInventory();
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString() + Constants.vbCrLf + ex.Source, "SEVERE");
                    LogError(ex);
                }

                if (a[0] == "selectitem")
                {
                    foreach (var i in n.PlayerInventory.Items)
                    {
                        if ((i.Type.ToString() + " x " + i.Count.ToString() ?? "") == (a[1] ?? ""))
                        {
                            n.SelectedItem = i;
                            try
                            {
                                await Send("itemset?" + n.Username + "?" + n.SelectedItem.Type.ToString(), n.Username);
                                await n.Send("itemset?@?" + n.SelectedItem.Type.ToString());
                            }
                            catch (Exception ex)
                            {
                                Log(ex.ToString(), "ERROR");
                                LogError(ex);
                            }

                            break;
                        }
                    }
                }

                if (a[0] == "tochest")
                {
                    if(n.OpenChest == null)
                    {
                        await n.Kick("Internal server error");
                        return;
                    }
                    ItemStack item = null;

                    foreach(ItemStack i in n.PlayerInventory.Items)
                    {
                        if (a[1] == $"{i.Type.ToString()} x {i.Count.ToString()}")
                        {
                            item = i;
                            break;
                        }
                    }

                    if (item == null)
                    {
                        Log($"{n.SelectedItem} tried to put null item into chest.", "WARNING");
                        return;
                    }
                    n.OpenChest.AddItem(ref n, item);
                }

                if (a[0] == "fromchest")
                {
                    if (n.OpenChest == null)
                    {
                        await n.Kick("Internal server error");
                        return;
                    }
                    int item = -1;

                    foreach (ItemStack i in n.OpenChest.items)
                    {
                        if (a[1] == $"{i.Type.ToString()} x {i.Count.ToString()}")
                        {
                            item = n.OpenChest.items.IndexOf(i);
                            break;
                        }
                    }

                    if (item == -1)
                    {
                        Log($"{n.SelectedItem} tried to get null item from chest.", "WARNING");
                        return;
                    }
                    n.OpenChest.RemoveItem(ref n, item);
                }

                if (a[0] == "closechest")
                {
                    n.OpenChest = null;
                }

                if(a[0] == "hamburger")
                {
                    await n.UpdateStats();
                }

                if (a[0] == "rightclick")
                {
                    try
                    {
                        var block = World.GetBlockAt(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2]));
                        if (block == null)
                        {
                            n.Kick("Internal server error occured.");
                            return;
                        }

                        var ev = new netcraft.server.api.events.BlockRightClickEvent(n, block);
                        NCSApi.REBlockRightClickEvent(ev);
                        if (n.SelectedItem.Type == Material.RAW_BEEF)
                        {
                            n.Saturation += 1;
                            if (n.Hunger < 20) await n.UpdateHunger(n.Hunger + 2);
                            await n.RemoveItem(Material.RAW_BEEF, 1);
                            await n.Heal(5);
                        }
                        if (n.SelectedItem.Type == Material.COOKED_BEEF)
                        {
                            n.Saturation += 1;
                            if (n.Hunger < 20) await n.UpdateHunger(n.Hunger + 5);
                            await n.RemoveItem(Material.COOKED_BEEF, 1);
                            await n.Heal(10);
                        }
                        if (n.SelectedItem.Type == Material.BREAD)
                        {
                            n.Saturation += 2;
                            if (n.Hunger < 20) await n.UpdateHunger(n.Hunger + 5);
                            await n.RemoveItem(Material.BREAD, 1);
                            await n.Heal(10);
                        }
                        if (n.SelectedItem.Type == Material.HEALING_POTION)
                        {
                            await n.RemoveItem(Material.HEALING_POTION, 1);
                            await n.UpdateHealth(100);
                        }
                        if (DistanceBetweenPoint(block.Position, Normalize(n.Position)) > 6) return;
                        if(block.Type == EnumBlockType.TNT)
                        {
                            await Explode(4, block.Position);
                        }
                        if(block.Type == EnumBlockType.CHEST)
                        {
                            await n.UpdateInventory();
                            await Task.Delay(30);
                            await n.Chest(World.GetChestAt(block.Position));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{ex.ToString()}");
                        return;
                    }
                }

                if (a[0] == "block_break")
                {
                    try
                    {
                        if (IsNothing(n.SelectedItem))
                            return;

                        
                        foreach (var b in World.Blocks)
                        {
                            if (b.Position.X == Conversions.ToInteger(a[1]))
                            {
                                if (b.Position.Y == Conversions.ToInteger(a[2]))
                                {
                                    if (b.Unbreakable)
                                        return;
                                    var pos = Normalize(n.Position);

                                    var ev = new netcraft.server.api.events.BlockBreakEventArgs(n, b);
                                    NCSApi.REBlockBreakEvent(ev);
                                    if (!n.UnlimitedReach && (DistanceBetween(pos.X, pos.Y, b.Position.X, b.Position.Y) > 6d))
                                    {
                                        ev.SetCancelled(true);
                                        if (ev.GetCancelled())
                                        {
                                            await n.DoWarning("Unreachable block.");
                                        }
                                        return;
                                    }
                                    if (ev.GetCancelled())
                                    {
                                        return;
                                    }
                                    if (b.Type == EnumBlockType.LAVA)
                                    {
                                        if (n.SelectedItem.Type == Material.BUCKET)
                                        {
                                            n.SelectedItem.Type = Material.LAVA_BUCKET;
                                            await n.UpdateInventory();

                                        }
                                        else { return; }
                                    }
                                    if (b.Type == EnumBlockType.WATER)
                                    {
                                        if (n.SelectedItem.Type == Material.BUCKET)
                                        {
                                            n.SelectedItem.Type = Material.WATER_BUCKET;
                                            await n.UpdateInventory();
                                        }
                                        else { return; }
                                    }
                                    await Send("removeblock?" + a[1] + "?" + a[2]);
                                    //Он сделал что хотел
                                    if(ENABLE_STATISTICS_EXPERIMENTAL)
                                    {
                                        n.IncrementStatInt("Blocks Broken");
                                    }
                                    if (b.Type == EnumBlockType.STONE)
                                    {
                                        if (n.SelectedItem.Type == Material.WOODEN_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                    }

                                    if (b.Type == EnumBlockType.COAL_ORE)
                                    {
                                        if (n.SelectedItem.Type == Material.WOODEN_PICKAXE)
                                            await n.Give(Material.COAL, 3);
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.COAL, 3);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.COAL, 3);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.COAL, 3);
                                    }

                                    if (b.Type == EnumBlockType.IRON_ORE)
                                    {
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.IRON_ORE);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.IRON_ORE);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.IRON_ORE);
                                    }

                                    if (b.Type == EnumBlockType.END_STONE)
                                    {
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.END_STONE);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.END_STONE);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.END_STONE);
                                    }

                                    if (b.Type == EnumBlockType.IRON_BLOCK)
                                    {
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.IRON_BLOCK);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.IRON_BLOCK);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.IRON_BLOCK);
                                    }

                                    if (b.Type == EnumBlockType.DIAMOND_BLOCK)
                                    {
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.DIAMOND_BLOCK);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.DIAMOND_BLOCK);
                                    }

                                    if (b.Type == EnumBlockType.DIAMOND_ORE)
                                    {
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.DIAMOND, 3);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.DIAMOND, 3);
                                    }

                                    if (b.Type == EnumBlockType.DIRT)
                                    {
                                        await n.Give(Material.DIRT);
                                    }

                                    if (b.Type == EnumBlockType.GRAVEL)
                                    {
                                        await n.Give(Material.GRAVEL);
                                    }

                                    if (b.Type == EnumBlockType.WHEAT)
                                    {
                                        await n.Give(Material.WHEAT);
                                    }

                                    if (b.Type == EnumBlockType.COBBLESTONE)
                                    {
                                        if (n.SelectedItem.Type == Material.WOODEN_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.STONE_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.COBBLESTONE);
                                    }

                                    if (b.Type == EnumBlockType.GOLD_ORE)
                                    {
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.GOLD_ORE);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.GOLD_ORE);
                                    }

                                    if (b.Type == EnumBlockType.GOLD_BLOCK)
                                    {
                                        if (n.SelectedItem.Type == Material.IRON_PICKAXE)
                                            await n.Give(Material.GOLD_BLOCK);
                                        if (n.SelectedItem.Type == Material.DIAMOND_PICKAXE)
                                            await n.Give(Material.GOLD_BLOCK);
                                    }

                                    if (b.Type == EnumBlockType.GRASS_BLOCK)
                                    {
                                        await n.Give(Material.GRASS_BLOCK, 1);
                                    }

                                    if (b.Type == EnumBlockType.SNOWY_GRASS_BLOCK)
                                    {
                                        await n.Give(Material.DIRT, 1);
                                    }

                                    if (b.Type == EnumBlockType.RED_FLOWER) await n.Give(Material.RED_FLOWER);
                                    if (b.Type == EnumBlockType.YELLOW_FLOWER) await n.Give(Material.YELLOW_FLOWER);

                                    if (b.Type == EnumBlockType.WOOD)
                                    {
                                        await n.Give(Material.WOOD);
                                    }

                                    if (b.Type == EnumBlockType.PLANKS)
                                    {
                                        await n.Give(Material.PLANKS);
                                    }

                                    if (b.Type == EnumBlockType.SAND)
                                    {
                                        await n.Give(Material.SAND);
                                    }

                                    if (b.Type == EnumBlockType.FURNACE)
                                    {
                                        await n.Give(Material.FURNACE);
                                    }

                                    if (b.Type == EnumBlockType.LEAVES)
                                    {
                                        var rnd = new Random();
                                        int r = rnd.Next(1, 4);
                                        if (r == 2)
                                        {
                                            await n.Give(Material.SAPLING);
                                        }
                                        else
                                        {
                                            await n.Give(Material.LEAVES);
                                        }
                                    }

                                    if(b.Type == EnumBlockType.TNT)
                                    {
                                        await n.Give(Material.TNT);
                                    }

                                    if(b.Type == EnumBlockType.CHEST)
                                    {
                                        BlockChest chest = World.GetChestAt(b.Position);
                                        foreach(ItemStack i in chest.items)
                                        {
                                            n.PlayerInventory.Items.Add(i);
                                        }
                                        await n.Give(Material.CHEST);
                                    }

                                    World.Blocks.Remove(World.GetBlockAt(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2])));
                                    return;
                                }
                                else
                                {
                                    // UpdateList($"{n.Username} trying to break {a(1)} {a(2)}, but Y was {b.Position.Y}")
                                }
                            }
                            else
                            {
                                // UpdateList($"{n.Username} trying to break {a(1)} {a(2)}, but X was {b.Position.X}")
                            }
                        }
                        await Send("removeblock?" + a[1] + "?" + a[2]);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{ex.ToString()}", "WARNING");
                        return;
                    }
                }
                try
                {
                    if (a[0] == "block_place")
                    {
                        var placeAt = new Point(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2]));
                        EnumBlockType type;
                        var pos = Normalize(n.Position);
                        placeAt = Normalize(placeAt);
                        if (World.GetBlockAt(placeAt) != null)
                        {
                            Block block = World.GetBlockAt(placeAt);
                            await n.SendBlockChange(placeAt, block.Type, isBackground: block.IsBackground);
                            return;
                        }
                        if (n.SelectedItem.Type == Material.LAVA_BUCKET)
                        {
                            if (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d) return;
                                foreach (var g in players)
                                await g.SendBlockChange(placeAt, EnumBlockType.LAVA);
                            World.Blocks.Add(new Block(placeAt, EnumBlockType.LAVA, false, false));
                            n.SelectedItem.Type = Material.BUCKET;

                            await n.UpdateInventory();
                            return;
                        }
                        if (n.SelectedItem.Type == Material.WATER_BUCKET)
                        {
                            if (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d) return;
                            foreach (var g in players)
                                await g.SendBlockChange(placeAt, EnumBlockType.WATER);
                            World.Blocks.Add(new Block(placeAt, EnumBlockType.WATER, false, false));
                            n.SelectedItem.Type = Material.BUCKET;

                            await n.UpdateInventory();
                            return;
                        }
                        type = (EnumBlockType)Enum.Parse(typeof(EnumBlockType), n.SelectedItem.Type.ToString());
                        if(type == EnumBlockType.SEEDS)
                        {
                            Block under = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (under == null || under.Type != EnumBlockType.DIRT)
                            {
                                return;
                            }
                        }
                        if (type == EnumBlockType.RED_FLOWER || type == EnumBlockType.YELLOW_FLOWER)
                        {
                            Block under = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (under == null)
                            {
                                return;
                            }
                        }
                        var b = new Block(placeAt, type, false, false);
                        var ev = new netcraft.server.api.events.BlockPlaceEventArgs(n, b);
                        NCSApi.REBlockPlaceEvent(ev);
                        if (!n.UnlimitedReach && (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d))
                        {
                            ev.SetCancelled(true);
                            if(ev.GetCancelled())
                            {
                                await n.DoWarning("Unreachable block.");
                            }
                            return;
                        }
                        if (ev.GetCancelled())
                        {
                            return;
                        }
                        if(type == EnumBlockType.SAPLING)
                        {
                            Block c = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (c == null) return;
                            if (c.Type != EnumBlockType.GRASS_BLOCK && c.Type != EnumBlockType.SNOWY_GRASS_BLOCK && c.Type != EnumBlockType.DIRT) return;
                        }
                        if(type == EnumBlockType.FIRE)
                        {
                            Block c = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (c == null) return;
                            if (c.Type == EnumBlockType.FIRE) return;
                        }
                        foreach (var g in players)
                            await g.SendBlockChange(placeAt, type);
                        World.Blocks.Add(b);
                        n.SelectedItem.Count -= 1;
                        if (n.SelectedItem.Count <= 0)
                        {
                            n.PlayerInventory.Items.Remove(n.SelectedItem);
                            n.SelectedItem = null;
                        }
                        //Он сделал что хотел
                        if (ENABLE_STATISTICS_EXPERIMENTAL)
                        {
                            n.IncrementStatInt("Blocks Placed");
                        }
                        await n.UpdateInventory();
                    }

                    if (a[0] == "block_place_bg")
                    {
                        var placeAt = new Point(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2]));

                        EnumBlockType type;
                        var pos = Normalize(n.Position);
                        placeAt = Normalize(placeAt);
                        if (World.GetBlockAt(placeAt) != null)
                        {
                            Block block = World.GetBlockAt(placeAt);
                            await n.SendBlockChange(placeAt, block.Type, isBackground: block.IsBackground);
                            return;
                        }
                        if (n.SelectedItem.Type == Material.LAVA_BUCKET)
                        {
                            if (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d) return;
                            foreach (var g in players)
                                await g.SendBlockChange(placeAt, EnumBlockType.LAVA, true, false, true);
                            World.Blocks.Add(new Block(placeAt, EnumBlockType.LAVA, false, true));
                            n.SelectedItem.Type = Material.BUCKET;

                            await n.UpdateInventory();
                            return;
                        }
                        if (n.SelectedItem.Type == Material.WATER_BUCKET)
                        {
                            if (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d) return;
                            foreach (var g in players)
                                await g.SendBlockChange(placeAt, EnumBlockType.WATER, true, false, true);
                            World.Blocks.Add(new Block(placeAt, EnumBlockType.WATER, false, true));
                            n.SelectedItem.Type = Material.BUCKET;

                            await n.UpdateInventory();
                            return;
                        }
                        type = (EnumBlockType)Enum.Parse(typeof(EnumBlockType), n.SelectedItem.Type.ToString());
                        if (type == EnumBlockType.RED_FLOWER || type == EnumBlockType.YELLOW_FLOWER)
                        {
                            Block under = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (under == null)
                            {
                                return;
                            }
                        }
                        if (type == EnumBlockType.SAPLING)
                        {
                            Block c = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (c == null) return;
                            if (c.Type != EnumBlockType.GRASS_BLOCK && c.Type != EnumBlockType.SNOWY_GRASS_BLOCK && c.Type != EnumBlockType.DIRT) return;
                        }
                        if (type == EnumBlockType.FIRE)
                        {
                            Block c = World.GetBlockAt(placeAt.X, placeAt.Y + 1);
                            if (c == null) return;
                            if (c.Type == EnumBlockType.FIRE) return;
                        }
                        var b = new Block(placeAt, type, false, true);
                        var ev = new netcraft.server.api.events.BlockPlaceEventArgs(n, b);
                        NCSApi.REBlockPlaceEvent(ev);
                        if (!n.UnlimitedReach && (DistanceBetween(pos.X, pos.Y, placeAt.X, placeAt.Y) > 6d))
                        {
                            ev.SetCancelled(true);
                            if (ev.GetCancelled())
                            {
                                await n.DoWarning("Unreachable block.");
                            }
                            return;
                        }
                        if (ev.GetCancelled())
                        {
                            return;
                        }

                        foreach (var g in players)
                            await g.SendBlockChange(placeAt, type, isBackground: true);
                        World.Blocks.Add(b);
                        n.SelectedItem.Count -= 1;
                        if (n.SelectedItem.Count <= 0)
                        {
                            n.PlayerInventory.Items.Remove(n.SelectedItem);
                            n.SelectedItem = null;
                        }
                        if (ENABLE_STATISTICS_EXPERIMENTAL)
                        {
                            n.IncrementStatInt("Blocks Placed On Background");
                        }
                        await n.UpdateInventory();
                    }
                }
                catch (NullReferenceException ex)
                {
                }
                catch (Exception ex)
                {
                    Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{ex.ToString()}", "WARNING");
                    return;
                }

                try
                {
                    if (a[0] == "pvp")
                    {
                        var nd = default(NetcraftPlayer);
                        foreach (var i in players)
                        {
                            if ((i.Username ?? "") == (a[1] ?? ""))
                            {
                                nd = i;
                            }
                        }

                        if (IsNothing(nd))
                        {
                            await n.Kick("Attempting to attack an invalid player");
                            return;
                        }

                        if ((nd.Username ?? "") == (n.Username ?? ""))
                        {
                            await n.Kick("Attempting to attack self");
                            return;
                        }

                        if (DistanceBetweenPoint(Normalize(n.Position), Normalize(nd.Position)) > 5d)
                        {
                            await n.DoWarning("Unreachable player!");
                            return;
                        }

                        if (!IsNothing(n.SelectedItem))
                        {
                            if (n.SelectedItem.Type == Material.DIAMOND_SWORD)
                            {
                                await nd.Damage(35, n);
                            }
                            else if (n.SelectedItem.Type == Material.IRON_SWORD)
                            {
                                await nd.Damage(25, n);
                            }
                            else if (n.SelectedItem.Type == Material.STONE_SWORD)
                            {
                                await nd.Damage(20, n);
                            }
                            else if (n.SelectedItem.Type == Material.WOODEN_SWORD)
                            {
                                await nd.Damage(14, n);
                            }
                            else
                            {
                                nd.Damage(9, n);
                            }
                        }
                        else
                        {
                            await nd.Damage(5, n);
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                }
                catch (Exception ex)
                {
                    Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{ex.ToString()}", "WARNING");
                    return;
                }

                try
                {
                    if (a[0] == "furnace")
                    {
                        if (IsNothing(n.SelectedItem))
                            return;
                        await n.Furnace(World.GetBlockAt(Conversions.ToInteger(a[1]), Conversions.ToInteger(a[2])), Material.COAL, n.SelectedItem.Type);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{ex.ToString()}", "WARNING");
                    return;
                }
            }
            catch (Exception globalEx)
            {
                Log($"Error while processing {n.Username}'s packet:{Constants.vbCrLf}{globalEx.ToString()}", "WARNING");
                n.Kick("Internal server error occured.");
                return;
            }
        }

        public static void CrashReport(Exception ex)
        {
            Console.Clear();
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < 120; i++) Console.WriteLine(new string(' ', 1000));
            Console.Clear();
            string crashText = "Netcraft Crash Report" + Constants.vbCrLf + $"Server crashed at {DateTime.Now.ToString()}" + Constants.vbCrLf + $"{ex.ToString()}";
            File.WriteAllText("./crash-reports/" + DateTime.Now.ToString().Replace(" ", "_").Replace(".", "-").Replace(":", "-") +  ".txt", crashText);
            
            Console.WriteLine(crashText + "\n\nPress any key to exit...");
            Console.ReadKey();
            Console.ResetColor();
            Console.Clear();
            for (int i = 0; i < 120; i++) Console.WriteLine(new string(' ', 1000));
            Console.Clear();
            Environment.Exit(-1);
        }

        public static double DistanceBetween(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2d) + Math.Pow(y2 - y1, 2d));
        }

        public static double DistanceBetweenPoint(Point p1, Point p2)
        {
            int x1 = p1.X;
            int x2 = p2.X;
            int y1 = p1.Y;
            int y2 = p2.Y;
            return Math.Sqrt(Math.Pow(x2 - x1, 2d) + Math.Pow(y2 - y1, 2d));
        }

        public async Task Explode(double radius, Point point)
        {
            if(!World.Gamerules.disableExplosions)
            {
                List<Block> blocksToRemove = new List<Block>();
                foreach (Block b in World.Blocks)
                {
                    if (b.Type == EnumBlockType.IRON_BLOCK)
                        continue;
                    if (b.Type == EnumBlockType.DIAMOND_BLOCK)
                        continue;
                    if (b.Type == EnumBlockType.OBSIDIAN)
                        continue;
                    if (b.Type == EnumBlockType.BEDROCK)
                        continue;
                    if (DistanceBetweenPoint(b.Position, point) <= radius) blocksToRemove.Add(b);
                }

                foreach (Block b in blocksToRemove)
                {
                    foreach (NetcraftPlayer p in players)
                    {
                        await p.PacketQueue.AddQueue($"removeblock?{b.Position.X.ToString()}?{b.Position.Y.ToString()}");
                        World.Blocks.Remove(b);
                    }
                }
            }
            foreach (NetcraftPlayer p in players)
            {
                await p.PacketQueue.SendQueue();
            }
            foreach (NetcraftPlayer p in players)
            {
                if(DistanceBetweenPoint(Normalize(p.Position), point) <= radius)
                {
                    await p.Damage((int)(radius - DistanceBetweenPoint(Normalize(p.Position), point)), "deathmessage.exploded");
                }
            }
            Log($"Explosion at [{point.X},{point.Y}] with power {radius.ToString()}");

        }

        public static Point Normalize(Point p)
        {
            var point = new Point()
            {
                X = p.X / 32,
                Y = p.Y / 32
            };
            return point;
        }

        public static bool IsNothing(object expession)
        {
            return expession == null;
        }

        public async Task Chat(string arg0)
        {
            await Send("chat?" + arg0);
        }

        public void SaveAuth()
        {
            string txt = "";
            foreach (var t in playerPasswords.Keys.Cast<string>().ToArray())
                txt = Conversions.ToString(txt + Operators.AddObject(Operators.AddObject(t + "=", playerPasswords[t]), Constants.vbCrLf));
            File.WriteAllText("./auth.txt", txt, Encoding.UTF8);
        }
        string handlingDisconnection = null;
        public async void handleDisconnection(NetcraftPlayer client, bool isError = false)
        {
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name = "Network Leave";
            ThreadAdd();
            if(handlingDisconnection == client.UUID)
            {
                Log($"handleDisconnection({client.ToString().TrimEnd(';')}, {isError.ToString().ToLower()}) called twice", "WARNING");
                return;
            }
            if (handlingDisconnection == null) handlingDisconnection = client.UUID;
            if (client.IsLoaded)
            {
                var ev = new netcraft.server.api.events.PlayerLeaveEventArgs(client);
                NCSApi.REPlayerLeaveEvent(ev);
                File.WriteAllText($"./playerdata/{client.Username}.json", PlayerInfoSaveLoad.Save(client), Encoding.UTF8);
                if (!isError)
                {
                    await Chat("&3" + lang.get("player.left", client.Username));
                    Log(lang.get("player.left", client.Username));
                }
                else
                {
                    client.Kick("An internal server error occured");
                    await Chat(client.Username + " left the game due to an error.");
                    Log(client.Username + " left the game due to an error.");
                }

                await Send("removeplayer?" + client.Username);
                if (loggedIn.Contains(client.Username))
                {
                    loggedIn.Remove(client.Username);
                }
            }

            players.Remove(client);
            Netcraft.clientList = players;
        }

        private bool Listening = false;

        private void Start()
        {
            Listning = new TcpListener(IPAddress.Any, 6575);
            if (Listening == false)
            {
                Listning.Start();
                Listning.BeginAcceptTcpClient(new AsyncCallback(AcceptClient), Listning);
                Listening = true;
            }
            else
            {
                Listning.Stop();
            }
        }

        private void OnClose()
        {
            File.WriteAllText(Conversions.ToString(NCORE_WORLDFILE), sv.Save(World), Encoding.UTF8);
            foreach (var c in players)
                handleDisconnection(c);
        }

        public void ForceCrash()
        {
            throw new NullReferenceException();
        }

        public void SaveWorld()
        {
            File.WriteAllText(Conversions.ToString(NCORE_WORLDFILE), sv.Save(World));
        }

        public async Task SendCommandFeedback(string a, CommandSender b)
        {
            Log($"[{b.GetName()}: {a}]");
            await Send($"chat?&4[{b.GetName()}: {a}]");
        }

        public static string Left(string a, int b)
        {
            return a.Substring(0, Math.Min(b, a.Length));
        }
    }

    public class Application
    {
        public static string StartupPath { get; } = ".";
    }
}