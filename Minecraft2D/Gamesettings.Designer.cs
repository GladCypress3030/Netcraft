﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace Minecraft2D
{
    [DesignerGenerated()]
    public partial class Gamesettings : Form
    {

        // Форма переопределяет dispose для очистки списка компонентов.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components is object)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Является обязательной для конструктора форм Windows Forms
        private System.ComponentModel.IContainer components;

        // Примечание: следующая процедура является обязательной для конструктора форм Windows Forms
        // Для ее изменения используйте конструктор форм Windows Form.  
        // Не изменяйте ее в редакторе исходного кода.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Gamesettings));
            this.BackButton = new System.Windows.Forms.Button();
            this.Label1 = new System.Windows.Forms.Label();
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.CheckBox2 = new System.Windows.Forms.CheckBox();
            this._CheckBox1 = new System.Windows.Forms.CheckBox();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.GroupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // BackButton
            // 
            this.BackButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.BackButton.BackColor = System.Drawing.Color.White;
            this.BackButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("BackButton.BackgroundImage")));
            this.BackButton.FlatAppearance.BorderColor = System.Drawing.Color.Lime;
            this.BackButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Lime;
            this.BackButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.BackButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BackButton.ForeColor = System.Drawing.Color.White;
            this.BackButton.Location = new System.Drawing.Point(484, 157);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(310, 36);
            this.BackButton.TabIndex = 0;
            this.BackButton.Text = "settings.button.back";
            this.BackButton.UseVisualStyleBackColor = false;
            this.BackButton.Click += new System.EventHandler(this.Button1_Click);
            this.BackButton.MouseEnter += new System.EventHandler(this.BackButton_MouseEnter);
            this.BackButton.MouseLeave += new System.EventHandler(this.BackButton_MouseLeave);
            // 
            // Label1
            // 
            this.Label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Label1.BackColor = System.Drawing.Color.Transparent;
            this.Label1.ForeColor = System.Drawing.Color.White;
            this.Label1.Location = new System.Drawing.Point(481, 76);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(313, 62);
            this.Label1.TabIndex = 1;
            this.Label1.Text = "Игра приостановлена";
            this.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // GroupBox1
            // 
            this.GroupBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.GroupBox1.BackColor = System.Drawing.Color.Transparent;
            this.GroupBox1.Controls.Add(this.checkBox1);
            this.GroupBox1.Controls.Add(this.CheckBox2);
            this.GroupBox1.Controls.Add(this._CheckBox1);
            this.GroupBox1.Location = new System.Drawing.Point(71, 139);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(294, 138);
            this.GroupBox1.TabIndex = 2;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Visible = false;
            // 
            // checkBox1
            // 
            this.checkBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.checkBox1.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBox1.BackColor = System.Drawing.Color.Transparent;
            this.checkBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("checkBox1.BackgroundImage")));
            this.checkBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBox1.ForeColor = System.Drawing.Color.White;
            this.checkBox1.Location = new System.Drawing.Point(6, 96);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(282, 33);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Music (bad)";
            this.checkBox1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBox1.UseVisualStyleBackColor = false;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged_1);
            this.checkBox1.MouseEnter += new System.EventHandler(this._CheckBox1_MouseEnter);
            this.checkBox1.MouseLeave += new System.EventHandler(this._CheckBox1_MouseLeave);
            // 
            // CheckBox2
            // 
            this.CheckBox2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.CheckBox2.Appearance = System.Windows.Forms.Appearance.Button;
            this.CheckBox2.AutoCheck = false;
            this.CheckBox2.BackColor = System.Drawing.Color.Transparent;
            this.CheckBox2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("CheckBox2.BackgroundImage")));
            this.CheckBox2.Checked = true;
            this.CheckBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBox2.Enabled = false;
            this.CheckBox2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CheckBox2.ForeColor = System.Drawing.Color.White;
            this.CheckBox2.Location = new System.Drawing.Point(6, 57);
            this.CheckBox2.Name = "CheckBox2";
            this.CheckBox2.Size = new System.Drawing.Size(282, 33);
            this.CheckBox2.TabIndex = 1;
            this.CheckBox2.Text = "Block Highlighting";
            this.CheckBox2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.CheckBox2.UseVisualStyleBackColor = false;
            this.CheckBox2.MouseEnter += new System.EventHandler(this._CheckBox1_MouseEnter);
            this.CheckBox2.MouseLeave += new System.EventHandler(this._CheckBox1_MouseLeave);
            // 
            // _CheckBox1
            // 
            this._CheckBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._CheckBox1.Appearance = System.Windows.Forms.Appearance.Button;
            this._CheckBox1.BackColor = System.Drawing.Color.Transparent;
            this._CheckBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("_CheckBox1.BackgroundImage")));
            this._CheckBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._CheckBox1.ForeColor = System.Drawing.Color.White;
            this._CheckBox1.Location = new System.Drawing.Point(6, 18);
            this._CheckBox1.Name = "_CheckBox1";
            this._CheckBox1.Size = new System.Drawing.Size(282, 33);
            this._CheckBox1.TabIndex = 0;
            this._CheckBox1.Text = "Button controls (Sensor mode)";
            this._CheckBox1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._CheckBox1.UseVisualStyleBackColor = false;
            this._CheckBox1.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            this._CheckBox1.Click += new System.EventHandler(this._CheckBox1_Click);
            this._CheckBox1.MouseEnter += new System.EventHandler(this._CheckBox1_MouseEnter);
            this._CheckBox1.MouseLeave += new System.EventHandler(this._CheckBox1_MouseLeave);
            // 
            // SettingsButton
            // 
            this.SettingsButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.SettingsButton.BackColor = System.Drawing.Color.White;
            this.SettingsButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("SettingsButton.BackgroundImage")));
            this.SettingsButton.FlatAppearance.BorderColor = System.Drawing.Color.Lime;
            this.SettingsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Lime;
            this.SettingsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.SettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SettingsButton.ForeColor = System.Drawing.Color.White;
            this.SettingsButton.Location = new System.Drawing.Point(484, 199);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(310, 36);
            this.SettingsButton.TabIndex = 3;
            this.SettingsButton.Text = "settings.button.settings";
            this.SettingsButton.UseVisualStyleBackColor = false;
            this.SettingsButton.Click += new System.EventHandler(this.Button2_Click);
            this.SettingsButton.MouseEnter += new System.EventHandler(this.BackButton_MouseEnter);
            this.SettingsButton.MouseLeave += new System.EventHandler(this.BackButton_MouseLeave);
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ExitButton.BackColor = System.Drawing.Color.White;
            this.ExitButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("ExitButton.BackgroundImage")));
            this.ExitButton.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.ExitButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Lime;
            this.ExitButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.ExitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ExitButton.ForeColor = System.Drawing.Color.White;
            this.ExitButton.Location = new System.Drawing.Point(484, 241);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(310, 36);
            this.ExitButton.TabIndex = 4;
            this.ExitButton.Text = "settings.button.exit";
            this.ExitButton.UseVisualStyleBackColor = false;
            this.ExitButton.Click += new System.EventHandler(this.Button3_Click);
            this.ExitButton.MouseEnter += new System.EventHandler(this.BackButton_MouseEnter);
            this.ExitButton.MouseLeave += new System.EventHandler(this.BackButton_MouseLeave);
            // 
            // Gamesettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImage = global::Minecraft2D.My.Resources.Resources.menubackground;
            this.ClientSize = new System.Drawing.Size(863, 423);
            this.ControlBox = false;
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.SettingsButton);
            this.Controls.Add(this.GroupBox1);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.BackButton);
            this.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Gamesettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Game paused";
            this.TransparencyKey = System.Drawing.Color.LightPink;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Gamesettings_FormClosing);
            this.Load += new System.EventHandler(this.Gamesettings_Load);
            this.VisibleChanged += new System.EventHandler(this.Gamesettings_VisibleChanged);
            this.GroupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private Button BackButton;

        internal Button Button1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return BackButton;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (BackButton != null)
                {
                    BackButton.Click -= Button1_Click;
                }

                BackButton = value;
                if (BackButton != null)
                {
                    BackButton.Click += Button1_Click;
                }
            }
        }

        internal Label Label1;
        internal GroupBox GroupBox1;
        private CheckBox _CheckBox1;

        internal CheckBox CheckBox1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _CheckBox1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged -= CheckBox1_CheckedChanged;
                }

                _CheckBox1 = value;
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged += CheckBox1_CheckedChanged;
                }
            }
        }

        private Button SettingsButton;

        internal Button Button2
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return SettingsButton;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (SettingsButton != null)
                {
                    SettingsButton.Click -= Button2_Click;
                }

                SettingsButton = value;
                if (SettingsButton != null)
                {
                    SettingsButton.Click += Button2_Click;
                }
            }
        }

        private Button ExitButton;

        internal Button Button3
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return ExitButton;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (ExitButton != null)
                {
                    ExitButton.Click -= Button3_Click;
                }

                ExitButton = value;
                if (ExitButton != null)
                {
                    ExitButton.Click += Button3_Click;
                }
            }
        }

        internal CheckBox CheckBox2;
        internal CheckBox checkBox1;
    }
}