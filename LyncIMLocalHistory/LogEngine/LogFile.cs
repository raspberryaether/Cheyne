using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.LogEngine
{
    public class LogFile
    {
        public static void Activate()
        {
            Log.MessageCaptured += OnLogCaptured;
        }

        /// <summary>
        /// Only write messages with this severity or higher. Default 2 (Info).
        /// </summary>
        public static uint HighPassFilter { get; set; }
        
        private static void OnLogCaptured(object sender, LogEventArgs e)
        {
            if (e.Severity < HighPassFilter) return;
            
            if (!Directory.Exists(_appDataRoot))
            {
                Directory.CreateDirectory(_appDataRoot);
            }

            string filepath = _appDataRoot + @"\" + _logFileName;

            using (StreamWriter outfile = new StreamWriter(filepath, true))
            {
                outfile.WriteLine(e.Message);
                outfile.Close();
            }
        }
        
        const string _programFolder = @"\LyncIMHistory";
        const string _logFileName = "LyncIMHistory.log";
        static readonly string _appDataRoot =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + _programFolder;

    }
}
