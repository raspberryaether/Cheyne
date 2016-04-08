using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.LogEngine
{
    public class LogEventArgs
    {
        public LogEventArgs(string message, uint severity)
        {
            Message = message;
            Severity = severity;
        }

        public string Message { get; private set; }
        public uint Severity { get; private set; }
    }
}
