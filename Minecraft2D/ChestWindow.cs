﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minecraft2D
{
    public partial class ChestWindow : Form
    {
        public ChestWindow()
        {
            InitializeComponent();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                if(e.Button == MouseButtons.Left)
                {
                    Form1.GetInstance().SendPacket("tochest", listBox1.SelectedItem.ToString());
                    listBox2.Items.Add(listBox1.SelectedItem.ToString());
                    listBox1.Items.Remove(listBox1.SelectedItem.ToString());
                }
            }
        }

        private void listBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                if(e.Button == MouseButtons.Left)
                {
                    Form1.GetInstance().SendPacket("fromchest", listBox2.SelectedItem.ToString());
                    listBox1.Items.Add(listBox2.SelectedItem.ToString());
                    listBox2.Items.Remove(listBox2.SelectedItem.ToString());
                }
            }
        }

        private void ChestWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form1.GetInstance().SendSinglePacket("closechest");
        }
    }
}
