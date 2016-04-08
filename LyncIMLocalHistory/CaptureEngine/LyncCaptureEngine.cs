using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System.Timers;
using System.IO;

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
        public event EventHandler Disconnected;
        public event EventHandler Connected;
        public event EventHandler<MessageEventArgs> MessageCaptured;
        public event EventHandler<LogEventArgs> LogRequested;

        void OnKeepAliveTimerElapsed(Object myObject, EventArgs myEventArgs)
        {
            if (_client != null && _client.State != ClientState.Invalid)
            {
                return;
            }

            _keepAliveTimer.Stop();
            ConsoleWriteLine("Lost connection to Lync client; retrying connection");
            Connect();
        }

        void ConsoleWriteLine(String text = "")
        {
            // TODO: send other events in all places this is called if it makes sense...

            Console.WriteLine(text);

            if( LogRequested != null )
            {
                LogRequested(this, new LogEventArgs(text));
            }
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
                    ConsoleWriteLine(String.Format("Waiting {0}ms before next connection attempt...",
                                                   CONNECT_RETRY_WAIT_TIME_MS));
                    await Task.Delay(CONNECT_RETRY_WAIT_TIME_MS);
                }

                try
                {
                    ConsoleWriteLine(String.Format("Establishing connection to Lync ({0} attempts remain)...",
                                                   (insist ? "infinite" : connectRetriesRemaining.ToString())));
                    _client = LyncClient.GetClient();
                }
                catch (LyncClientException)
                {
                    ConsoleWriteLine("Connection attempt failed...");
                }
                catch (Exception)
                {
                    ConsoleWriteLine("Connection attempt failed catastrophically...");
                }

                wait = true;
            }


            _client.ConversationManager.ConversationAdded += OnConversationAdded;
            _client.ConversationManager.ConversationRemoved += OnConversationRemoved;
            ConsoleWriteLine("Ready!");
            ConsoleWriteLine();

            _keepAliveTimer.Enabled = true;
        }

        void OnConversationAdded(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            Concept.ConversationInfo newInfo = _storageEngine.AllocateConversation();
            ActiveConversations.Add(e.Conversation, newInfo);

            if (ConversationStarted != null)
            {
                ConversationEventArgs args = new ConversationEventArgs(ActiveConversations[e.Conversation]);
            }
            
            e.Conversation.ParticipantAdded += OnParticipantAdded;
            e.Conversation.ParticipantRemoved += OnParticipantRemoved;

            // TODO: raise an event for consumption by UI and get rid of write line
            String s = String.Format("Conversation #{0} started.", newInfo.Identifier);
            ConsoleWriteLine(s);
        }

        void OnParticipantRemoved(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived -= OnInstantMessageReceived;
            if (args.Participant.Contact == _client.Self.Contact)
                ConsoleWriteLine("You were removed.");
            else
                ConsoleWriteLine("Participant was removed: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void OnParticipantAdded(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived += OnInstantMessageReceived;
            if (args.Participant.Contact == _client.Self.Contact)
                ConsoleWriteLine("You were added.");
            else
                ConsoleWriteLine("Participant was added: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void OnInstantMessageReceived(object sender, Microsoft.Lync.Model.Conversation.MessageSentEventArgs args)
        {
            InstantMessageModality imm = (sender as InstantMessageModality);
            Concept.ConversationInfo info = ActiveConversations[imm.Conversation];
            DateTime now = DateTime.Now;

            
            string authorName = imm.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName) as string;
            Concept.Individual author = _storageEngine.GetIndividualByName(authorName);
            Concept.Message newMessage = new Concept.Message(info, DateTime.Now, author, args.Text);

            if( MessageCaptured != null )
            {
                MessageCaptured(this, new MessageEventArgs(newMessage));
            }
        }

        void OnConversationRemoved(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            e.Conversation.ParticipantAdded -= OnParticipantAdded;
            e.Conversation.ParticipantRemoved -= OnParticipantRemoved;
            if (ActiveConversations.ContainsKey(e.Conversation) )
            {
                Concept.ConversationInfo info = ActiveConversations[e.Conversation];
                ActiveConversations.Remove(e.Conversation);

                if (ConversationFinished != null)
                {
                    ConversationFinished(this, new ConversationEventArgs(info));
                }                               
            }
        }
    }
}
