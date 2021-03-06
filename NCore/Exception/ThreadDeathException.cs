﻿using System;

namespace NCore.netcraft.server.api.exceptions
{
    public class ThreadDeathException : Exception
    {
        public override string Message
        {
            get
            {
                return "Watchdog detected main thread death";
            }
        }
    }
}