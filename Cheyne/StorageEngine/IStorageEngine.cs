using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cheyne.CaptureEngine;

namespace Cheyne.StorageEngine
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

        /// <summary>
        /// Get a new, blank ConversationInfo object with a unique identifier.
        /// </summary>
        /// <returns>empty ConversationInfo object with a unique ID</returns>
        Concept.ConversationInfo AllocateConversation();

        /// <summary>
        /// Get an Individual object reflecting the given name, registering it if necessary.
        /// </summary>
        /// <returns></returns>
        Concept.Individual GetIndividualByName(string name);

        /// <summary>
        /// Subscribe to the events exposed by the given capture engine.
        /// </summary>
        /// <param name="captureEngine">capture engine to subscribe to</param>
        void Subscribe(CaptureEngine.ICaptureEngine captureEngine);

        /// <summary>
        /// Unsubscribe from the events exposed by the given capture engine.
        /// </summary>
        /// <param name="captureEngine">capture engine to unsubscribe from</param>
        void Unsubscribe(CaptureEngine.ICaptureEngine captureEngine);

    }

}
