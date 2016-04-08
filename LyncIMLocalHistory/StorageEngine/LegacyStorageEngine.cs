using System;
using System.Collections.Generic;
using System.IO;

using LyncIMLocalHistory.CaptureEngine;

namespace LyncIMLocalHistory.StorageEngine
{

    class LegacyStorageEngine : IStorageEngine
    {
        /// <summary>
        /// Create a LegacyStorageEngine object.
        /// </summary>
        /// <param name="localUserName">name of the local user</param>
        public LegacyStorageEngine(string localUserName)
        {
            // User 0 must be the currently signed-in user.
            _individualCache = new Dictionary<string, Concept.Individual>();
            _individualCache[localUserName] = new Concept.Individual(0, localUserName);

            // Make sure the directories we need exist.
            if (!Directory.Exists(_appDataRoot))
                Directory.CreateDirectory(_appDataRoot);
            if (!Directory.Exists(_storageRoot))
                Directory.CreateDirectory(_storageRoot);
        }

        public List<Concept.ConversationInfo> Conversations
        {
            get
            {
                throw new InvalidOperationException("LegacyStorageEngine does not support retrieval");
            }
        }

        public List<Concept.Individual> Individuals
        {
            get
            {
                throw new InvalidOperationException("LegacyStorageEngine does not support retrieval");
            }
        }

        public List<Concept.Message> GetMessages(uint conversationId)
        {
            throw new InvalidOperationException("LegacyStorageEngine does not support retrieval");
        }

        public void Subscribe(ICaptureEngine captureEngine)
        {
            captureEngine.MessageCaptured += OnMessageCaptured;
        }

        public void Unsubscribe(ICaptureEngine captureEngine)
        {
            captureEngine.MessageCaptured -= OnMessageCaptured;
        }

        public Concept.ConversationInfo AllocateConversation()
        {
            uint nextConvId;

            using (StreamReader idFile = new StreamReader(_appDataRoot + "\\nextConvId.txt"))
            {
                nextConvId = uint.Parse(idFile.ReadLine());
            }
            
            Concept.ConversationInfo newInfo = new Concept.ConversationInfo(nextConvId);
            newInfo.StartTime = DateTime.Now;
            nextConvId += 1;

            using (StreamWriter idFile = new StreamWriter(_appDataRoot + "\\nextConvId.txt"))
            {
                idFile.WriteLine(nextConvId);
            }

            return newInfo;
        }

        public Concept.Individual GetIndividualByName(string name)
        {
            if(!_individualCache.ContainsKey(name))
            {
                _individualCache[name] = new Concept.Individual((uint)_individualCache.Count, name);
            }
                        
            return _individualCache[name];
        }

        private void OnMessageCaptured(object sender, MessageEventArgs e)
        {

            string entryString = CreateEntry(e.Message);

            using (StreamWriter outfile = new StreamWriter(_storageRoot + @"\AllLyncIMHistory.txt", true))
            {
                outfile.WriteLine(entryString);
                outfile.Close();
            }

            foreach (Concept.Individual participant in e.Message.Conversation.Participants)
            {
                if (participant.Identifier == 0) continue;

                string directoryPath = _storageRoot + String.Format("\\{0}", participant.Name);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string dateString = e.Message.Timestamp.ToString("yyyy-MM-dd");
                string filepath = directoryPath + String.Format("\\{0}.txt", dateString);

                using (StreamWriter outfile = new StreamWriter(filepath, true))
                {
                    outfile.WriteLine(entryString);
                    outfile.Close();
                }
            }

        }

        private string CreateEntry(Concept.Message message)
        {
            return String.Format(_messageTemplate, message.Timestamp, message.Conversation.Identifier, message.Author.Name);
        }

        private Dictionary<string, Concept.Individual> _individualCache;

        private const string _programFolder = @"\LyncIMHistory";

        private readonly string _appDataRoot =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + _programFolder;

        private readonly string _storageRoot =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + _programFolder;

        private readonly string _messageTemplate = "[{0}] (Conv. #{1}) <{3}>" + Environment.NewLine + "{4}";
    }
}
