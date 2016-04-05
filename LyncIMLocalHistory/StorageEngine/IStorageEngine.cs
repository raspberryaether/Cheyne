using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.StorageEngine
{
    interface IStorageEngine
    {
        /// <summary>
        /// Gets a comprehensive list of all conversations (by Id) stored at this time (including incomplete).
        /// </summary>
        List<Concept.ConversationInfo> Conversations { get; }

        /// <summary>
        /// Get a list of individuals known 
        /// </summary>
        /// <returns></returns>
        List<Concept.Individual> Individuals { get; }

        /// <summary>
        /// Gets a list of messages in the given conversation.
        /// </summary>
        /// <param name="conversationId">the identifier of the conversation to retrieve</param>
        /// <returns>a list of messages in the given conversation</returns>        
        List<Concept.Message> GetMessages(uint conversationId);
    }

}
