﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using NCore.netcraft.server.api;

namespace NCore
{
    public class Commandgive : Command
    {
        public Commandgive() : base("give", "Выдать предмет игроку/себе", "give <игрок|@s|@a|@r> <предмет> [кол-во]")
        {
        }

        public override bool OnCommand(CommandSender sender, Command cmd, string[] args, string label)
        {
            if (!sender.GetAdmin())
            {
                sender.SendMessage("У Вас недостаточно прав!");
                return true;
            }

            if (args.Length == 2)
            {
                Material t = (Material)Enum.Parse(typeof(Material), args[1].ToUpper());
                NetworkPlayer p;
                if (args[0] == "@s")
                {
                    if (!sender.IsPlayer)
                    {
                        sender.SendMessage("Только для игрока!");
                        return true;
                    }

                    p = (NetworkPlayer)sender;
                    p.Give(t);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} (1 шт.) игроку " + p.Username, sender);
                    return true;
                }
                else if (args[0] == "@a")
                {
                    foreach (var g in Netcraft.GetOnlinePlayers())
                        g.Give(t);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} (1 шт.) {Netcraft.GetOnlinePlayers().Count} игрокам", sender);
                    return true;
                }
                else if (args[0] == "@r")
                {
                    Random rnd = new Random();
                    List<NetworkPlayer> g = Netcraft.GetOnlinePlayers();
                    p = g[rnd.Next(0, g.Count - 1)];
                    p.Give(t);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} (1 шт.) игроку " + p.Username, sender);
                    return true;
                }
                else
                {
                    p = Netcraft.GetPlayer(args[0]);
                    if (NCore.IsNothing(p))
                    {
                        sender.SendMessage("Игрок не найден!");
                        return true;
                    }

                    p.Give(t);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} (1 шт.) игроку " + p.Username, sender);
                    return true;
                }
            }

            if (args.Length == 3)
            {
                Material t = (Material)Enum.Parse(typeof(Material), args[1].ToUpper());
                int count = Conversions.ToInteger(args[2]);
                NetworkPlayer p;
                if (args[0] == "@s")
                {
                    if (!sender.IsPlayer)
                    {
                        sender.SendMessage("Только для игрока!");
                        return true;
                    }

                    p = (NetworkPlayer)sender;
                    p.Give(t, count);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} ({count} шт.) игроку " + p.Username, sender);
                    return true;
                }
                else if (args[0] == "@a")
                {
                    foreach (var g in Netcraft.GetOnlinePlayers())
                        g.Give(t, count);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} ({count} шт.) {Netcraft.GetOnlinePlayers().Count} игрокам", sender);
                    return true;
                }
                else if (args[0] == "@r")
                {
                    var rnd = new Random();
                    var g = Netcraft.GetOnlinePlayers();
                    p = g[rnd.Next(0, g.Count - 1)];
                    p.Give(t, count);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} ({count} шт.) игроку " + p.Username, sender);
                    return true;
                }
                else
                {
                    p = Netcraft.GetPlayer(args[0]);
                    if (NCore.IsNothing(p))
                    {
                        sender.SendMessage("Игрок не найден!");
                        return true;
                    }

                    p.Give(t, count);
                    NCore.SendCommandFeedback($"Выдано {t.ToString().ToLower()} ({count} шт.) игроку " + p.Username, sender);
                    return true;
                }
            }

            return false;
        }
    }
}