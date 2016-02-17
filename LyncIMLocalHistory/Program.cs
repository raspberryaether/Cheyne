using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace LyncIMLocalHistory
{
    class Program : System.Windows.Forms.Form
    {
        public string welcomeText =
@"LyncIMLocalHistory ver. 1.1
=============================
Simple IM conversation tracker for people who want to keep the conversation 
history and can not use lync for it directly (i.e. it may be disabled by corp).
Conversations are stored in [your documents]\LyncIMHistory folder.

LICENCE
=======
This program is a Beerware and is distributed under GBL (General Beer Licence).
Which meens when you buy me a beer you can use modify and do whatever you 
please with the program. I don't take any responsibilities whatsoever. 

contact:
Jonas Bujok
gbl@bujok.cz";

        Program()
        {
            InitializeComponent();
            this.Resize += Form_Resize;
            this.textBox1.Text = welcomeText.Replace("\n", Environment.NewLine);
        }

        static Dictionary<Microsoft.Lync.Model.Conversation.Conversation, ConversationContainer> ActiveConversations =
            new Dictionary<Microsoft.Lync.Model.Conversation.Conversation, ConversationContainer>();

        /**
         * this user (participant) using the lync
         */
        static Self myself;

        static int nextConvId = 0;
        private TextBox textBox1;
        private TextBox consoleBox;

        static string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string programFolder = @"\LyncIMHistory";

        static Program ProgramRef;
        static Timer keepAliveTimer = new Timer();
        private NotifyIcon notifyIcon;
        private System.ComponentModel.IContainer components;

        static LyncClient lyncClient;

        private const int BALLOON_POPUP_TIMEOUT_MS = 3000;
        private const int KEEP_ALIVE_INTERVAL_MS = 5000;
        private const int CONNECT_RETRY_WAIT_TIME_MS = 5000;
        private const int CONNECT_RETRY_MAX = -1; // -1 to retry indefinitely

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            ProgramRef = new Program();
            ProgramRef.Shown += ApplicationShown;
            Application.Run(ProgramRef);
            ProgramRef.Shown -= ApplicationShown;
        }

        static void ApplicationShown(object sender, EventArgs args)
        {
            (sender as Program).prepare();
            (sender as Program).connect();
        }

        void KeepAliveTimerProcessor(Object myObject, EventArgs myEventArgs)
        {
            if (lyncClient != null && lyncClient.State != ClientState.Invalid)
            {
                return;
            }

            keepAliveTimer.Stop();
            consoleWriteLine("Lost connection to Lync client; retrying connection");
            connect();
        }

		void consoleWriteLine(String text = "")
        {
            Console.WriteLine(text);
            if (this.consoleBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(consoleWriteLine);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.consoleBox.AppendText(text + System.Environment.NewLine);
            }
        }

        void prepare()
        {
            Console.WriteLine(welcomeText);

            //read previous conversation ID
            try
            {
                StreamReader idFile = new StreamReader(appDataPath + programFolder + @"\nextConvId.txt");
                nextConvId = int.Parse(idFile.ReadLine());
                consoleWriteLine("Last conversation number found: " + nextConvId);
            }
            catch (Exception)
            {
                nextConvId = 1;
                consoleWriteLine("No previous conversation number found. Using default.");
            }

            keepAliveTimer.Tick += new EventHandler(KeepAliveTimerProcessor);
            keepAliveTimer.Interval = KEEP_ALIVE_INTERVAL_MS;
        }

        async void connect()
        {		
			int connectRetriesRemaining = CONNECT_RETRY_MAX;
			bool insist = (connectRetriesRemaining < 0);
			bool wait = false;

			lyncClient = null;

            while (lyncClient == null &&
                   (insist || connectRetriesRemaining-- > 0))
            {
                if (wait)
                {
                    consoleWriteLine(String.Format("Waiting {0}ms before next connection attempt...",
                                                   CONNECT_RETRY_WAIT_TIME_MS));
                    await Task.Delay(CONNECT_RETRY_WAIT_TIME_MS);
                }

                try
                {
                    consoleWriteLine(String.Format("Establishing connection to Lync ({0} attempts remain)...",
                                                   (insist ? "infinite" : connectRetriesRemaining.ToString())));
                    lyncClient = LyncClient.GetClient();
                }
                catch (LyncClientException)
                {
                    consoleWriteLine("Connection attempt failed...");
                }
                catch (Exception)
                {
                    consoleWriteLine("Connection attempt failed catastrophically...");
                }

                wait = true;
            }
                       
            myself = lyncClient.Self;
            if (!Directory.Exists(mydocpath + programFolder))
                Directory.CreateDirectory(mydocpath + programFolder);
            if (!Directory.Exists(appDataPath + programFolder))
                Directory.CreateDirectory(appDataPath + programFolder);
            lyncClient.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            lyncClient.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
            consoleWriteLine("Ready!");
            consoleWriteLine();
           // Console.ReadLine();

            keepAliveTimer.Enabled = true;
        }

        void ConversationManager_ConversationAdded(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            ConversationContainer newcontainer = new ConversationContainer()
            {
                Conversation = e.Conversation,
                ConversationCreated = DateTime.Now,
                m_convId = nextConvId++
            };
            ActiveConversations.Add(e.Conversation, newcontainer);
            try
            {
                using (StreamWriter outfile = new StreamWriter(appDataPath + programFolder + @"\nextConvId.txt", false))
                {
                    outfile.WriteLine(nextConvId);
                    outfile.Close();
                }
            }
            catch(Exception)
            {
                //ignore
            }
            e.Conversation.ParticipantAdded += Conversation_ParticipantAdded;
            e.Conversation.ParticipantRemoved += Conversation_ParticipantRemoved;
            String s = String.Format("Conversation #{0} started.", newcontainer.m_convId);
            consoleWriteLine(s);
            if (WindowState == FormWindowState.Minimized)
            {
                this.notifyIcon.BalloonTipText = s;
                this.notifyIcon.ShowBalloonTip(BALLOON_POPUP_TIMEOUT_MS);
            }
        }

        void Conversation_ParticipantRemoved(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived -= InstantMessageModality_InstantMessageReceived;
            if (args.Participant.Contact == myself.Contact)
                consoleWriteLine("You were removed.");
            else
                consoleWriteLine("Participant was removed: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void Conversation_ParticipantAdded(object sender, Microsoft.Lync.Model.Conversation.ParticipantCollectionChangedEventArgs args)
        {
            (args.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality).InstantMessageReceived += InstantMessageModality_InstantMessageReceived;
            if (args.Participant.Contact == myself.Contact)
                consoleWriteLine("You were added.");
            else
                consoleWriteLine("Participant was added: " + args.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName));
        }

        void InstantMessageModality_InstantMessageReceived(object sender, Microsoft.Lync.Model.Conversation.MessageSentEventArgs args)
        {
            InstantMessageModality imm = (sender as InstantMessageModality);
            ConversationContainer container = ActiveConversations[imm.Conversation];
            DateTime now = DateTime.Now;
            String convlog = "[" + now + "] (Conv. #" + container.m_convId + ") <" + imm.Participant.Contact.GetContactInformation(ContactInformationType.DisplayName) + ">";
            convlog += Environment.NewLine + args.Text;
            using (StreamWriter outfile = new StreamWriter(mydocpath + programFolder +@"\AllLyncIMHistory.txt", true))
            {
                outfile.WriteLine(convlog);
                outfile.Close();
            }
            foreach (Participant participant in container.Conversation.Participants)
            {
                if (participant.Contact == myself.Contact)
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

            consoleWriteLine(convlog);
        }

        void ConversationManager_ConversationRemoved(object sender, Microsoft.Lync.Model.Conversation.ConversationManagerEventArgs e)
        {
            e.Conversation.ParticipantAdded -= Conversation_ParticipantAdded;
            e.Conversation.ParticipantRemoved -= Conversation_ParticipantRemoved;
            if (ActiveConversations.ContainsKey(e.Conversation))
            {
                ConversationContainer container = ActiveConversations[e.Conversation];
                TimeSpan conversationLength = DateTime.Now.Subtract(container.ConversationCreated);
                consoleWriteLine(String.Format("Conversation #{0} ended. It lasted {1} seconds", container.m_convId, conversationLength.ToString(@"hh\:mm\:ss")));
                ActiveConversations.Remove(e.Conversation);

                String s = String.Format("Conversation #{0} ended.", container.m_convId);
                if (WindowState == FormWindowState.Minimized)
                {
                    this.notifyIcon.BalloonTipText = s;
                    this.notifyIcon.ShowBalloonTip(BALLOON_POPUP_TIMEOUT_MS);
                }
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Program));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.consoleBox = new System.Windows.Forms.TextBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.MinimumSize = new System.Drawing.Size(100, 50);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(447, 202);
            this.textBox1.TabIndex = 0;
            this.textBox1.TabStop = false;
            // 
            // consoleBox
            // 
            this.consoleBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.consoleBox.Location = new System.Drawing.Point(12, 220);
            this.consoleBox.Multiline = true;
            this.consoleBox.Name = "consoleBox";
            this.consoleBox.ReadOnly = true;
            this.consoleBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleBox.Size = new System.Drawing.Size(447, 152);
            this.consoleBox.TabIndex = 1;
            this.consoleBox.TabStop = false;
            // 
            // Program
            // 
            this.ClientSize = new System.Drawing.Size(471, 384);
            this.Controls.Add(this.consoleBox);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Program";
            this.Text = "Lync IM Local History";
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info; //Shows the info icon so the user doesn't thing there is an error.
            this.notifyIcon.BalloonTipText = "Lync history minimized";
            this.notifyIcon.BalloonTipTitle = "Lync history";
            this.notifyIcon.Icon = this.Icon; //The tray icon to use
            this.notifyIcon.Text = "Lync history recorder";
            this.notifyIcon.DoubleClick += notifyIcon_MouseDoubleClick;

            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon.Visible = true;
                notifyIcon.BalloonTipText = "Lync history minimized";
                notifyIcon.ShowBalloonTip(BALLOON_POPUP_TIMEOUT_MS);
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon.Visible = false;
        }
    }

    class ConversationContainer
    {
        public Microsoft.Lync.Model.Conversation.Conversation Conversation { get; set; }
        public DateTime ConversationCreated { get; set; }
        public int m_convId;
    }

    delegate void SetTextCallback(string text);
}
