﻿using NCore.netcraft.server.api;
using System.Threading.Tasks;

namespace NCore
{
    public class Commandban : Command
    {
        public Commandban() : base("ban", NCore.GetNCore().lang.get("commands.ban.description"), "netcraft.command.ban", NCore.GetNCore().lang.get("commands.ban.usage"))
        {
        }

        public async override Task<bool> OnCommand(CommandSender sender, Command cmd, string[] args, string label)
        {
            NCore.Lang lang = sender.IsPlayer ? ((NetcraftPlayer)sender).lang : NCore.GetNCore().lang;

            if (args.Length == 1)
            {
                string a = args[0];
                if (Netcraft.IsBanned(a))
                {
                    await sender.SendMessage(lang.get("commands.ban.failed.banned"));
                    return true;
                }
                await NCore.GetNCore().BroadcastChatTranslation("commands.ban.success", new string[] { sender.GetName(), a });
                await Netcraft.BanPlayer(a);
                return true;
            }

            return false;
        }
    }
}