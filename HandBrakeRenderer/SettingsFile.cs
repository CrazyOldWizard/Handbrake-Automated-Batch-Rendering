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
        public bool EnableStatusLog { get; set; } = false;
        public string HandbrakeCLI { get; set; } = Path.GetFullPath(Path.Combine(RootFolder, "HandBrakeCLI.exe"));
        public bool DeleteOriginalFiles { get; set; } = false; //this does nothing right now
        public string[] FileTypes { get; set; } = { ".mkv", ".mp4", ".webm", ".avi", ".mov", ".flv", ".wmv", ".ts", ".m4v", ".mpg", ".mpeg", ".vob", ".mts", ".m2ts" };


        // Directories
        public static string ThisProgram = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string RootFolder { get; set; } = Path.GetDirectoryName(ThisProgram);
        public string UtilitiesFolder { get; set; } = Path.Combine(RootFolder, "utils");
        public string InboxFolder { get; set; } = Path.Combine(RootFolder, "Inbox");
        public string OutboxFolder { get; set; } = Path.Combine(RootFolder, "Encoded");
        public string OriginalFilesFolder { get; set; } = Path.Combine(RootFolder, "OriginalFiles");
        public string HTMLStatusDirectory { get; set; } = Path.Combine(RootFolder, "StatusDirectory");


    }
}
