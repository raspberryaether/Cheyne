using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;

using LyncIMLocalHistory.CaptureEngine;

namespace LyncIMLocalHistory
{
    class ControlWindow : System.Windows.Forms.Form
    {
        public string welcomeText = Resources.ResourceManager.GetString("ApplicationDescription");
        
        public ControlWindow()
        {
            InitializeComponent();
            this.Resize += Form_Resize;
            this.textBox1.Text = welcomeText.Replace("\n", Environment.NewLine);
        }

        private TextBox textBox1;
        private TextBox consoleBox;

        static string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        static Timer keepAliveTimer = new Timer();
        private NotifyIcon notifyIcon;
        private System.ComponentModel.IContainer components;

        private const int BALLOON_POPUP_TIMEOUT_MS = 3000;
        private const int KEEP_ALIVE_INTERVAL_MS = 5000;
        private const int CONNECT_RETRY_WAIT_TIME_MS = 5000;
        private const int CONNECT_RETRY_MAX = -1; // -1 to retry indefinitely

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            ControlWindow controlWindow = new ControlWindow();

            // StorageEngine.IStorageEngine storageEngine = new StorageEngine();
            ICaptureEngine captureEngine = new LyncCaptureEngine();

            // TODO: subscribe to more events
            captureEngine.LogRequested += controlWindow.OnLogRequested;

            captureEngine.Connect();
                                                
            Application.Run(controlWindow);            
        }

        private void OnLogRequested(object sender, LogEventArgs e)
        {
            ConsoleWriteLine(e.Message);
        }

        void ConsoleWriteLine(String text = "")
        {
            Console.WriteLine(text);
            if (this.consoleBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(ConsoleWriteLine);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.consoleBox.AppendText(text + System.Environment.NewLine);
            }
        }
        
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControlWindow));
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

    delegate void SetTextCallback(string text);
}
