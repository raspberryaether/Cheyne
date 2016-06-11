using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System.Timers;
using System.IO;

using LyncIMLocalHistory.LogEngine;

namespace LyncIMLocalHistory.CaptureEngine
{
    class LyncCaptureEngine : ICaptureEngine
    {
        public LyncCaptureEngine()
        {
            Prepare();
        }

        private StorageEngine.IStorageEngine _storageEngine;
        public StorageEngine.IStorageEngine StorageEngine
        {
            get
            {
                return _storageEngine;
            }
            set
            {
                if (_storageEngine != null)
                {
                    _storageEngine.Unsubscribe(this);
                }

                _storageEngine = value;
                _storageEngine.Subscribe(this);                
            }
        }

        private LyncClient _client;
        private Timer _keepAliveTimer;

        /**
         * A list of currently active conversations.
         */
        private Dictionary<Conversation, Concept.ConversationInfo> ActiveConversations = new Dictionary<Conversation, Concept.ConversationInfo>();

        private const int BALLOON_POPUP_TIMEOUT_MS = 3000;
        private const int KEEP_ALIVE_INTERVAL_MS = 5000;
        private const int CONNECT_RETRY_WAIT_TIME_MS = 5000;
        private const int CONNECT_RETRY_MAX = -1; // -1 to retry indefinitely

        public event EventHandler<ConversationEventArgs> ConversationStarted;
        public event EventHandler<ConversationEventArgs> ConversationFinished;
        public event EventHandler Disconnected; //TODO: implementation
        public event EventHandler Connected; //TODO: implementation
        public event EventHandler<MessageEventArgs> MessageCaptured;

        void OnKeepAliveTimerElapsed(Object myObject, EventArgs myEventArgs)
        {
            if (_client != null && _client.State != ClientState.Invalid)
            {
                return;
            }

            _keepAliveTimer.Stop();
            Log.Error("Lost connection to Lync client; retrying connection");
            Connect();
        }

        void Prepare()
        {
            _keepAliveTimer = new Timer(KEEP_ALIVE_INTERVAL_MS);
            _keepAliveTimer.Elapsed += OnKeepAliveTimerElapsed;
            _keepAliveTimer.Enabled = false;
        }

        public async void Connect()
        {
            int connectRetriesRemaining = CONNECT_RETRY_MAX;
            bool insist = (connectRetriesRemaining < 0);
            bool wait = false;

            _client = null;

            while (_client == null &&
                   (insist || connectRetriesRemaining-- > 0))
            {
                if (wait)
                {
                    Log.Debug(String.Format("Waiting {0}ms before next connection attempt...",
                                            CONNECT_RETRY_WAIT_TIME_MS));
                    await Task.Delay(CONNECT_RETRY_WAIT_TIME_MS);
                }

                try
                {
                    Log.Info(String.Format("Establishing connection to Lync ({0} attempts remain)...",
                                           (insist ? "infinite" : connectRetriesRemaining.ToString())));
                    _client = LyncClient.GetClient();
                }
                catch (LyncClientException)
                {
                    Log.Error("Connection attempt failed...");
                }
                catch (Exception)
                {
                    Log.WriteLine(Severity.Calamity, ("error: Connection attempt failed catastrophically..."));
                }

                wait = true;
            }


            _client.ConversationManager.ConversationAdded += OnConversationAdded;
            _client.ConversationManager.ConversationRemoved += OnConversationRemoved;
            Log.Info("Intialization complete.");

            _keepAliveTimer.Enabled = true;
        }

        void OnConversationAdded(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            Concept.ConversationInfo newInfo = _storageEngine.AllocateConversation();
            ActiveConversations.Add(e.Conversation, newInfo);

            e.Conversation.ParticipantAdded += OnParticipantAdded;
            e.Conversation.ParticipantRemoved += OnParticipantRemoved;

            ConversationEventArgs args = new ConversationEventArgs(ActiveConversations[e.Conversation]);
            ConversationStarted?.Invoke(this, args);

            Log.Info(String.Format("Conversation #{0} started.", newInfo.Identifier));
        }

        void OnParticipantRemoved(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived -= OnInstantMessageReceived;
            if (args.Participant.Contact == _client.Self.Contact)
                Log.Info("You were removed from the conversation."); // TODO: what conversation? specify conversation ID
            else
                Log.Info("Participant was removed from the conversation: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void OnParticipantAdded(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived += OnInstantMessageReceived;
            if (args.Participant.Contact == _client.Self.Contact)
                Log.Info("You were added to the conversation.");
            else
                Log.Info("Participant was added to the conversation: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void OnInstantMessageReceived(object sender, Microsoft.Lync.Model.Conversation.MessageSentEventArgs args)
        {
            InstantMessageModality imm = (sender as InstantMessageModality);
            Concept.ConversationInfo info = ActiveConversations[imm.Conversation];
            DateTime now = DateTime.Now;

            
            string authorName = imm.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName) as string;
            Concept.Individual author = _storageEngine.GetIndividualByName(authorName);
            Concept.Message newMessage = new Concept.Message(info, DateTime.Now, author, args.Text);

            MessageCaptured?.Invoke(this, new MessageEventArgs(newMessage));
            
        }

        void OnConversationRemoved(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            e.Conversation.ParticipantAdded -= OnParticipantAdded;
            e.Conversation.ParticipantRemoved -= OnParticipantRemoved;
            if (ActiveConversations.ContainsKey(e.Conversation) )
            {
                Concept.ConversationInfo info = ActiveConversations[e.Conversation];
                ActiveConversations.Remove(e.Conversation);

                ConversationFinished?.Invoke(this, new ConversationEventArgs(info));
            }
        }
    }
}
