﻿// ------------------------------------------------------------------------------
// <auto-generated>
// Этот код создан программой.
// Исполняемая версия:4.0.30319.42000
// 
// Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
// повторной генерации кода.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Minecraft2D.My
{

    // ПРИМЕЧАНИЕ. Этот файл создан автоматически; не изменяйте его самостоятельно.  Для внесения изменений
    // или, если в ходе сборки обнаружены ошибки в этом файле, перейдите в конструктор проектов
    // (перейдите к свойствам проекта или дважды щелкните узел "Мой проект" в
    // обозревателе решений), и внесите изменения на вкладке "Приложение".
    // 
    internal partial class MyApplication
    {
        [DebuggerStepThrough()]
        public MyApplication() : base(Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows)
        {
            IsSingleInstance = false;
            EnableVisualStyles = false;
            SaveMySettingsOnExit = true;
            ShutdownStyle = Microsoft.VisualBasic.ApplicationServices.ShutdownMode.AfterMainFormCloses; // исправить
        }

        [DebuggerStepThrough()]
        protected override void OnCreateMainForm()
        {
            MainForm = MyProject.Forms.MainMenu;
        }
    }
}