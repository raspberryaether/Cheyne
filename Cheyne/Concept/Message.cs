using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheyne.Concept
{
    class Message
    {
        public Message(
            ConversationInfo conversation,
            DateTime timestamp,
            Individual author,
            string content
            )
        {
            Conversation = conversation;
            Timestamp = timestamp;
            Author = author;
            Content = content;
        }

        public ConversationInfo Conversation { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Individual Author { get; private set; }
        public string Content { get; private set; }
    }
}
