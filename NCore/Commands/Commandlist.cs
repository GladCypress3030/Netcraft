﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using NCore;
using NCore.netcraft.server.api;

namespace NCore
{
    public class Commandlist : Command
    {
        public Commandlist() : base("list", "Показывает список игроков", "list")
        {
        }

        public override async Task<bool> OnCommand(CommandSender sender, Command cmd, string[] args, string label)
        {
            if (args.Length == 0)
            {
                if (sender.IsPlayer)
                {
                    NetworkPlayer p = (NetworkPlayer)sender;
                    await p.PacketQueue.AddQueue($"chat?Сейчас {NCore.GetNCore().players.Count} из {NCore.GetNCore().maxPlayers} игроков на сервере:");
                    var sc = new System.Collections.Specialized.StringCollection();
                    foreach (var a in Netcraft.GetOnlinePlayers())
                        sc.Add(a.Username);
                    await p.PacketQueue.AddQueue($"chat?{string.Join(", ", sc.Cast<string>().ToArray())}");
                    await p.PacketQueue.SendQueue();
                }
                else
                {
                    await sender.SendMessage($"Сейчас {NCore.GetNCore().players.Count} из {NCore.GetNCore().maxPlayers} игроков на сервере:");
                    var sc = new System.Collections.Specialized.StringCollection();
                    foreach (var a in Netcraft.GetOnlinePlayers())
                        sc.Add(a.Username);
                    await sender.SendMessage(string.Join(", ", sc.Cast<string>().ToArray()));
                }

                return true;
            }

            return false;
        }
    }
}