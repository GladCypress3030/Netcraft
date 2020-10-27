﻿using System;
using System.Collections.Generic;
using global::System.Reflection;

namespace NCore
{
    public class PluginManager
    {
        public static List<Plugin> Plugins { get; set; } = new List<Plugin>();

        public static Plugin Load(string path)
        {
            try
            {
                var p = Assembly.LoadFrom(path);
                Plugin pl = ((Plugin)GetObject(p, "Plugin")).Create();
                pl.Assembly = p;
                Plugins.Add(pl);
                return pl;
            }
            catch (Exception ex)
            {
                NCore.CrashReport(ex);
            }

            return default;
        }

        public static object GetObject(Assembly plugin, string obj)
        {
            try
            {
                var Plugin_MainWindowType = plugin.GetType($"{plugin.GetName().Name}.{obj}", true, false);
                var plugin_MainWindow = Activator.CreateInstance(Plugin_MainWindowType);
                return plugin_MainWindow;
            }
            catch (Exception ex)
            {
                NCore.CrashReport(ex);
            }

            return default;
        }

        public static void Unload(Plugin p)
        {
            p.OnUnload();
            Plugins.Remove(p);
            NCore.Log($"Плагин выгружен: {p.Name}");
        }
    }
}