using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.CaptureEngine
{

    class ConversationEventArgs : EventArgs
    {
        public Concept.ConversationInfo Info { get; set; }
    }

    class MessageEventArgs : EventArgs
    {
        public Concept.Message Message { get; set; }
    }

    class LogEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    interface ICaptureEngine
    {
        void Connect();

        event EventHandler<ConversationEventArgs> ConversationStarted;
        event EventHandler<ConversationEventArgs> ConversationFinished;

        event EventHandler Disconnected;
        event EventHandler Connected;

        event EventHandler<MessageEventArgs> MessageCaptured;

        // TODO: remove dependencies on this event and remove it
        event EventHandler<LogEventArgs> LogRequested;
    }
}
