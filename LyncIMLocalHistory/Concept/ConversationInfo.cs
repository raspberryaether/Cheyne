using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.Concept
{
    public class ConversationInfoFactory
    {
        public ConversationInfoFactory(uint lastId)
        {
            _lastId = lastId;
        }

        public ConversationInfo AllocateConversationInfo()
        {
            return new ConversationInfo(++_lastId, DateTime.Now);
        }

        // TODO: make this private 
        public uint _lastId;
    }

    public class ConversationInfo
    {
        internal ConversationInfo(uint identifier, DateTime startTime)
        {
            Identifier = identifier;
            StartTime = startTime;
            Participants = new List<Individual>();
        }

        /// <summary>
        /// Identifies the conversation by a unique unsigned integer.
        /// </summary>
        public uint Identifier { get; private set; }

        /// <summary>
        /// Reflects the time of the first message in the conversation.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// A list of all individuals which are participating in the conversation.
        /// </summary>
        public List<Individual> Participants { get; private set; }

    }
}
