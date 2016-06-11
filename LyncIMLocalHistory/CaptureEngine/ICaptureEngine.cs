using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.CaptureEngine
{

    class ConversationEventArgs : EventArgs
    {
        public ConversationEventArgs( Concept.ConversationInfo info )
        {
            Info = info;
        }

        public Concept.ConversationInfo Info { get; private set; }
    }

    class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Concept.Message message)
        {
            Message = message;
        }

        public Concept.Message Message { get; private set; }
    }

    class LogEventArgs : EventArgs
    {
        public LogEventArgs( string message )
        {
            Message = message;
        }

        public string Message { get; private set; }
    }

    interface ICaptureEngine
    {
        void Connect();

        event EventHandler<ConversationEventArgs> ConversationStarted;
        event EventHandler<ConversationEventArgs> ConversationFinished;

        event EventHandler Disconnected;
        event EventHandler Connected;

        event EventHandler<MessageEventArgs> MessageCaptured;
    }
}
