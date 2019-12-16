using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HandBrakeRenderer
{
    public class SettingsFile
    {
        //this is all still WIP

        // Options
        public static bool EnableStatusLog { get; set; }
        public static string HandbrakeCLI { get; set; } = Path.GetFullPath(Path.Combine(RootFolder, "HandBrakeCLI.exe"));
        public static bool DeleteOriginalFiles { get; set; } = false; //this does nothing right now
        public static string[] FileTypes { get; set; } = { ".mkv", ".mp4", ".webm", ".avi", ".mov", ".flv", ".wmv", ".ts", ".m4v", ".mpg", ".mpeg", ".vob", ".mts", ".m2ts" };


        // Directories
        private static string ThisProgram = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string RootFolder { get; set; } = Path.GetDirectoryName(ThisProgram);
        public static string UtilitiesFolder { get; set; } = Path.Combine(RootFolder, "utils");
        public static string InboxFolder { get; set; } = Path.Combine(RootFolder, "Inbox");
        public static string OutboxFolder { get; set; } = Path.Combine(RootFolder, "Encoded");
        public static string OriginalFilesFolder { get; set; } = Path.Combine(RootFolder, "OriginalFiles");
        public static string HTMLStatusDirectory { get; set; }


    }
}
