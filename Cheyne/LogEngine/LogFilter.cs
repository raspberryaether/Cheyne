﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheyne.LogEngine
{
    public class LogFilter
    {
        internal LogFilter(uint highPassFilter)
        {
            HighPassFilter = highPassFilter;
            Log.MessageCaptured += OnMessageCaptured;
        }

        private void OnMessageCaptured(object sender, LogEventArgs e)
        {
            if(e.Severity >= HighPassFilter && MessageCaptured != null)
            {
                MessageCaptured?.Invoke(this, e);
            }
        }

        public uint HighPassFilter { get; set; }

        event EventHandler<LogEventArgs> MessageCaptured;
    }
}
