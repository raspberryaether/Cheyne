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

        private LyncClient _client;
        private Timer _keepAliveTimer;
        private Concept.ConversationInfoFactory _conversationInfoFactory;

        /**
         * A list of currently active conversations.
         */
        private Dictionary<Conversation, Concept.ConversationInfo> ActiveConversations = new Dictionary<Conversation, Concept.ConversationInfo>();

        private readonly string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private readonly string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private const string programFolder = @"\LyncIMHistory";

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
                LogRequested(this, new LogEventArgs(){ Message = text });
            }
        }

        void Prepare()
        {
            //read previous conversation ID
            try
            {
                StreamReader idFile = new StreamReader(appDataPath + programFolder + @"\nextConvId.txt");
                uint nextConvId = uint.Parse(idFile.ReadLine());
                _conversationInfoFactory = new Concept.ConversationInfoFactory(nextConvId - 1);

                ConsoleWriteLine("Last conversation number found: " + nextConvId);
            }
            catch (Exception)
            {
                _conversationInfoFactory = new Concept.ConversationInfoFactory(1);
                ConsoleWriteLine("No previous conversation number found. Using default.");
            }

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

            if (!Directory.Exists(mydocpath + programFolder))
                Directory.CreateDirectory(mydocpath + programFolder);
            if (!Directory.Exists(appDataPath + programFolder))
                Directory.CreateDirectory(appDataPath + programFolder);
            _client.ConversationManager.ConversationAdded += OnConversationAdded;
            _client.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
            ConsoleWriteLine("Ready!");
            ConsoleWriteLine();

            _keepAliveTimer.Enabled = true;
        }

        void OnConversationAdded(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            Concept.ConversationInfo newInfo = _conversationInfoFactory.AllocateConversationInfo();

            ActiveConversations.Add(e.Conversation, newInfo);

            try
            {
                using (StreamWriter outfile = new StreamWriter(appDataPath + programFolder + @"\nextConvId.txt", false))
                {
                    outfile.WriteLine(_conversationInfoFactory._lastId);
                    outfile.Close();
                }
            }
            catch (Exception)
            {
                //ignore
                // TODO: get rid of no-op catch
            }

            e.Conversation.ParticipantAdded += OnParticipantAdded;
            e.Conversation.ParticipantRemoved += OnParticipantRemoved;

            String s = String.Format("Conversation #{0} started.", newInfo.Identifier);
            ConsoleWriteLine(s);

            // TODO: raise an event for consumption by UI            
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

            // TODO: factor out into events
            String convlog = "[" + now + "] (Conv. #" + info.Identifier + ") <" + imm.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName) + ">";
            convlog += Environment.NewLine + args.Text;
            using (StreamWriter outfile = new StreamWriter(mydocpath + programFolder + @"\AllLyncIMHistory.txt", true))
            {
                outfile.WriteLine(convlog);
                outfile.Close();
            }
            foreach (Participant participant in imm.Conversation.Participants)
            {
                if (participant.Contact == _client.Self.Contact)
                    continue;
                String directory = mydocpath + programFolder + @"\" + participant.Contact.GetContactInformation(ContactInformationType.DisplayName);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                string dateString = now.ToString("yyyy-MM-dd");
                String filename = directory + @"\" + dateString + ".txt";
                //consoleWriteLine(filename);
                using (StreamWriter partfile = new StreamWriter(filename, true))
                {
                    partfile.WriteLine(convlog);
                    partfile.Close();
                }
            }

            ConsoleWriteLine(convlog);
        }

        void ConversationManager_ConversationRemoved(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            e.Conversation.ParticipantAdded -= OnParticipantAdded;
            e.Conversation.ParticipantRemoved -= OnParticipantRemoved;
            if (ActiveConversations.ContainsKey(e.Conversation))
            {
                Concept.ConversationInfo info = ActiveConversations[e.Conversation];
                TimeSpan conversationLength = DateTime.Now.Subtract(info.StartTime);
                ConsoleWriteLine(String.Format("Conversation #{0} ended. It lasted {1} seconds", info.Identifier, conversationLength.ToString(@"hh\:mm\:ss")));
                ActiveConversations.Remove(e.Conversation);

                String s = String.Format("Conversation #{0} ended.", info.Identifier);

                // TODO: raise event for UI consumption                
            }
        }
    }
}
