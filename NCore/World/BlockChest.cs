﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace NCore
{
    public class BlockChest : Block
    {
        public List<ItemStack> items = new List<ItemStack>();

        public BlockChest(Point point, bool bg = false) : base(point, EnumBlockType.CHEST, false, bg)
        {
        }

        public void AddItem(ref NetcraftPlayer player, ItemStack item)
        {
            if (player.PlayerInventory.CountOf(item.Type) < 1) throw new Exception("Inventory error, please ban the cheater; Player: " + player.Username);
            player.PlayerInventory.Items.Remove(item);
            player.UpdateInventory();
            items.Add(item);
        }

        public void RemoveItem(ref NetcraftPlayer player, int index)
        {
            player.Give(items[index].Type, items[index].Count);
            items.RemoveAt(index);
        }
    }
}
