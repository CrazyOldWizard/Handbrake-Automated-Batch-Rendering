using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace HandBrakeRenderer
{
    class Program
    {
        static SettingsFile TheSettingsFile = new SettingsFile();


        //File Paths
        public static string RenderEXE = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string RootFolder = Path.GetDirectoryName(RenderEXE);
        public static string utilsFolder = Path.Combine(RootFolder, "utils");
        public static string InboxFolder = Path.Combine(RootFolder, "Inbox");
        public static string OutboxFolder = Path.Combine(RootFolder, "Encoded");
        public static string HandBrakeCLI = Path.GetFullPath(Path.Combine(RootFolder, "HandBrakeCLI.exe")); // this will not work on other os's - hardcoded .exe
        public static string quote = "\"";
        public static string OriginalFilesFolder = Path.Combine(RootFolder, "OriginalFiles");
        public static string logFile = Path.GetFullPath(Path.Combine(RootFolder, "EncodeLog.txt"));
        public static string htmlFolder = ConfigurationManager.AppSettings["HTMLStatusDir"];
        public static string statusLog = Path.GetFullPath(Path.Combine(htmlFolder, "RenderStatus.txt"));

        public bool statusLogEnabled = bool.Parse(ConfigurationManager.AppSettings["statusLogEnabled"]);
        int numOfFiles;

        public static readonly string[] fileTypes = {".mkv", ".mp4", ".webm", ".avi", ".mov", ".flv", ".wmv", ".ts", ".m4v", ".mpg", ".mpeg", ".vob", ".mts", ".m2ts"};

        private static readonly string[] programFolders = { OriginalFilesFolder, OutboxFolder, utilsFolder, InboxFolder };

        private static void createMissingFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(folder + " does not exist, creating...");
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }


        public static void MissingItems()
        {
            // Creates the folders required.

            foreach (string folder in programFolders)
            {
                createMissingFolder(folder);
            }


            // first time startup for presets- checks to see if you have any presets in the utils folder.  
            // If you don't, then it will give you info on what you need to do
            if (Directory.GetFiles(utilsFolder, "*.json",  SearchOption.TopDirectoryOnly).Length < 1)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("");
                Console.WriteLine("It looks like you don't have any presets.  You will need to add a preset in: ");
                Console.WriteLine(utilsFolder);
                Console.WriteLine("The preset should have the same name in the Handbrake UI as the .json file name");
                Console.WriteLine("For example, MyPreset.json should be named MyPreset when visible in the Handbrake UI");
            }
            // Checks to see if HandbrakeCLI.exe exists in root folder.
            if (!File.Exists(HandBrakeCLI))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("");
                Console.WriteLine(HandBrakeCLI + " Does not exist!!! - HandbrakeCLI.exe needs to live at " + HandBrakeCLI);
                Console.WriteLine("Opening download page");
                Thread.Sleep(5000);
                string url = @"https://handbrake.fr/downloads2.php";
                OpenBrowser(url);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(1);
            }
            if(!File.Exists(statusLog))
            {
                statusLog = Path.GetFullPath(Path.Combine(RootFolder, "RenderStatus.txt"));
                Console.WriteLine("Can't access specified file path for the Status Log! - Using " + statusLog);
            }
        }


        private static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); // Not tested
            }
            else
            {
                Console.WriteLine("Could not open URL to " + url);
            }
        }

        public static void CreatePresetDirs()
        {
            //For Each Preset

            foreach (string preset in Directory.GetFiles(utilsFolder, "*.json", SearchOption.TopDirectoryOnly))
            {
                var presetNameNoEXT = Path.GetFileNameWithoutExtension(preset);
                var PresetNameFolder = Path.Combine(InboxFolder, presetNameNoEXT);

                //Checks if folder has jobs waiting in queue, if it does, it shows the count - this is similar to "Render Status()"
                if (Directory.Exists(PresetNameFolder))
                {
                    int numOfFiles = Directory.GetFiles(PresetNameFolder).Length;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("The folder " + presetNameNoEXT + " has " + numOfFiles + " files in the render queue.");
                    Console.ResetColor();
                }
                //If the preset folder doesn't exist, it makes it
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Directory.CreateDirectory(PresetNameFolder);
                    Console.WriteLine("Creating folder " + presetNameNoEXT + " in " + RootFolder);
                    Console.ResetColor();

                }
            }

        }

        public static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RenderStatus(string currentFileLog, bool clear)
        {
            if (statusLogEnabled == true)
            {
               
                // This is for creating a simple txt file that can be hosted on a web server for a semi-live status page
                // first line overwrites txt file with new blank doc and then a string of text
                File.WriteAllText(statusLog, ("Last status update was " + DateTime.Now + " Current Jobs:"));
                File.AppendAllText(statusLog, "" + Environment.NewLine);
                foreach (string preset in Directory.GetFiles(utilsFolder, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var presetNameNoEXT = Path.GetFileNameWithoutExtension(preset);
                    var PresetNameFolder = (InboxFolder + "\\" + presetNameNoEXT);

                    //Checks if folder is not done and then shows the total files inside as well as how many it still needs
                    if (Directory.Exists(PresetNameFolder))
                    {
                        var filesInFolder = Directory.GetFiles(PresetNameFolder);
                        int countFiles = filesInFolder.Length;

                        foreach (string FT in fileTypes)
                        {
                            foreach (string file in filesInFolder)
                            {
                                if (file.EndsWith(FT) == true)
                                {
                                    numOfFiles++;
                                }
                            }
                        }

                        if(countFiles == 0)
                        {
                            numOfFiles = 0;
                        }

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        // status text string
                        var statusInfo = ("The folder " + presetNameNoEXT + " has " + numOfFiles + " files in the render queue.");
                        Console.WriteLine(statusInfo);
                        File.AppendAllText(statusLog, "" + Environment.NewLine);
                        File.AppendAllText(statusLog, statusInfo + Environment.NewLine);
                        Console.ResetColor();
                        Console.Clear();
                    }
                }
                if (clear == false)
                {
                    File.AppendAllText(statusLog, currentFileLog + Environment.NewLine);
                }

                
            }
            else
            {
                Console.WriteLine("Status log not enabled...");
                Thread.Sleep(1000);
            }
        }

        public static void RenderFiles()
        {
            // checks each preset folder
            foreach (string presetFolder in Directory.GetDirectories(InboxFolder, "*", SearchOption.TopDirectoryOnly))
            {
                // for each movie in the current preset folder
                foreach (string movie in Directory.GetFiles(presetFolder, "*.*", SearchOption.AllDirectories))
                {
                    // now it will go through the utils folder and look for preset names that share the name of the current preset folder
                    // if they match it will use that preset as the handbrake preset.json file
                    foreach (string presetFile in Directory.GetFiles(utilsFolder, "*.json", SearchOption.TopDirectoryOnly))
                    {
                        string presetFolderNoPth = new DirectoryInfo(presetFolder).Name;
                        string presetFileName = Path.GetFileNameWithoutExtension(presetFile);
                        bool contains = presetFolderNoPth.Equals(presetFileName);
                        var movieName = Path.GetFileName(movie);
                        var movieNameNoExt = Path.GetFileNameWithoutExtension(movie);
                        string nodeJobFile = Path.GetFullPath(Path.Combine(presetFolder, (movieNameNoExt + ".renderjob")));
                        string outMovie = Path.GetFullPath(Path.Combine(OutboxFolder, (movieNameNoExt + ".mkv")));
                        //if the preset file contains the name of the current preset folder
                        if (contains == true)
                        {
                            // passes the movie onto the "IsFileReady()" and if true, starts
                            if (IsFileReady(movie) == true)
                            {
                                // file extension filter
                                string ext = Path.GetExtension(movie);
                                int pos = Array.IndexOf(fileTypes, ext);
                                //checks if array contains ext
                                if (pos > -1)
                                {
                                    //node job
                                    if (File.Exists(nodeJobFile))
                                    {
                                        Console.WriteLine(movieNameNoExt + " is being rendered by another node, skipping");
                                    }
                                    else
                                    {
                                        //creates .renderjob file for nodes
                                        var jobFile = File.Create(nodeJobFile);
                                        jobFile.Close();
                                        File.WriteAllText(nodeJobFile, ("Node " + Environment.MachineName + " got this job at " + DateTime.Now));
                                        // creates handbrake command
                                        Console.WriteLine("I can render " + Path.GetFileName(movie));
                                        var handbrakeCommand = (" --preset-import-file " + quote + presetFile + quote + " -Z " + quote + presetFileName + quote + " -i " + quote + movie + quote + " -o " + quote + outMovie + quote);
                                        Console.WriteLine(handbrakeCommand);

                                        Console.Title = "Handbrake-Automated-Rendering:  " + movieName + " -- " + DateTime.Now;

                                        //start handbrake with the args
                                        // writes sring to log file with current movie and the preset in use
                                        string currentFileLog = (DateTime.Now + " Current file is " + movie + " Using preset " + presetFileName);
                                        File.AppendAllText(logFile, currentFileLog + Environment.NewLine);
                                        // updates render status
                                        Program p = new Program();
                                        p.RenderStatus(currentFileLog, false);
                                        // starts handbrake with handbrakecommand argument
                                        ProcessStartInfo StartHandbrake = new ProcessStartInfo();
                                        StartHandbrake.CreateNoWindow = false;
                                        StartHandbrake.UseShellExecute = false;
                                        StartHandbrake.FileName = HandBrakeCLI;
                                        StartHandbrake.Arguments = handbrakeCommand;
                                        try
                                        {
                                            // Start the process with the info we specified.
                                            // Call WaitForExit and then the using-statement will close.
                                            using (Process exeProcess = Process.Start(StartHandbrake))
                                            {
                                                exeProcess.WaitForExit();
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e.ToString());
                                        }
                                        // when file is done, it will copy the original file to the "original files" folder and then delete the original file from the preset inbox
                                        File.Copy(movie, Path.GetFullPath(Path.Combine(OriginalFilesFolder, movieName)), true);
                                        File.Delete(movie);
                                        File.Delete(nodeJobFile);
                                        // updates log file
                                        string completeStatusLog = (DateTime.Now + " Moved " + movie + " to " + Path.GetFullPath(Path.Combine(OriginalFilesFolder, movieName)));
                                        File.AppendAllText(logFile, completeStatusLog + Environment.NewLine);
                                        Console.WriteLine("RENDER FINISHED!!!");
                                        // updates render status
                                        p.RenderStatus(currentFileLog, true);
                                        return;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(Path.GetFileName(movie) + " Is not a file I can render");
                                }

                            }
                            else
                            {
                                bool renderingFile = Path.GetExtension(movie).Contains(".renderjob");
                                if (File.Exists(nodeJobFile))
                                {
                                    Console.WriteLine(movieNameNoExt + " is being rendered by another node, skipping");
                                    continue;
                                }

                                if (renderingFile == false)
                                {
                                    // if it can't open the file and it is a valid movie, then it will show this message until file is ready to open
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(movieName + " Do I have Read/Write access to this file? " + IsFileReady(movie) + " Probably still copying...");
                                    Console.ResetColor();
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void WriteTestJSON()
        {
            var jsonSettings = new JsonSerializerOptions();
            jsonSettings.WriteIndented = true;
            var serializedJSON = JsonSerializer.Serialize(TheSettingsFile, jsonSettings);
            File.WriteAllText(@"C:\Settings.json", serializedJSON);
        }



        static void Main()
        {
            
            MissingItems(); //this doesn't need to be run in the loop
            while (true)
            {
                Console.Title = "Handbrake-Automated-Rendering: Looking for work";
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(DateTime.Now);
                CreatePresetDirs();
                RenderFiles();
                Thread.Sleep(1000);
                Console.Clear();
            }
        }
    }
}
