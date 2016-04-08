using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.Concept
{
    public class ConversationInfo
    {
        internal ConversationInfo(uint identifier)
        {
            Identifier = identifier;
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MaxValue;
            Participants = new List<Individual>();
            ActiveParticipants = new List<Individual>();            
        }

        /// <summary>
        /// Identifies the conversation by a unique unsigned integer.
        /// </summary>
        public uint Identifier { get; private set; }

        /// <summary>
        /// Reflects the time of the creation of the conversation.
        /// If this is DateTime.MinValue, the conversation has not started yet.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Reflects the time the conversation ended. 
        /// If this is DateTime.MaxValue, the conversation has not ended yet.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// A list of all individuals which have participated in the conversation.
        /// This is only guaranteed to include individuals which have sent messages.
        /// </summary>
        public List<Individual> Participants { get; private set; }

        /// <summary>
        /// A list of all individuals which are currently participating in the conversation.
        /// Must be an empty list if EndTime is set to anything other than MaxValue.
        /// </summary>
        public List<Individual> ActiveParticipants { get; private set; }

    }
}
