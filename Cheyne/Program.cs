using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;

using Cheyne.StorageEngine;
using Cheyne.CaptureEngine;
using Cheyne.LogEngine;
using Cheyne.UI;

namespace Cheyne
{
    class Program
    {

        static void InitControlWindow()
        {
            _controlWindow = new ControlWindow();
        }

        static void InitStorageEngine()
        {
            _storageEngine = new LegacyStorageEngine("Raspberry Aether");           
        }

        static void InitCaptureEngine()
        {
            _captureEngine = new LyncCaptureEngine();
        }

        static void InitLogEngine()
        {
            LogFile.HighPassFilter = 0;
            LogFile.Activate();
        }

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            InitLogEngine();

            InitCaptureEngine();
            InitStorageEngine();

            InitControlWindow();           

            _storageEngine.Subscribe(_captureEngine);
            _captureEngine.Connect();

            Application.Run(_controlWindow);
        }

        private static IStorageEngine _storageEngine;
        private static ICaptureEngine _captureEngine;
        private static ControlWindow _controlWindow;
        
        private const int BALLOON_POPUP_TIMEOUT_MS = 3000;
        private const int KEEP_ALIVE_INTERVAL_MS = 5000;
        private const int CONNECT_RETRY_WAIT_TIME_MS = 5000;
        private const int CONNECT_RETRY_MAX = -1; // -1 to retry indefinitely

    }
}
