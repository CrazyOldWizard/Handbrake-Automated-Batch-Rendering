using System.IO;

namespace HandBrakeRenderer;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public bool EnableStatusLog { get; set; }
    public string HandbrakeCLI { get; set; } = "HandBrakeCLI.exe";
    public bool DeleteOriginalFiles { get; set; }
    public string[] FileTypes { get; set; } = 
    { 
        ".mkv", ".mp4", ".webm", ".avi", ".mov", ".flv", 
        ".wmv", ".ts", ".m4v", ".mpg", ".mpeg", ".vob", ".mts", ".m2ts" 
    };
    public bool CopyRenderFileLocal { get; set; }

    public string UtilitiesFolder { get; set; } = "utils";
    public string InboxFolder { get; set; } = "Inbox";
    public string OutboxFolder { get; set; } = "Encoded";
    public string OriginalFilesFolder { get; set; } = "OriginalFiles";
    public string HTMLStatusDirectory { get; set; } = "StatusDirectory";
    public int PollingIntervalSeconds { get; set; } = 1;

    public string GetFullPath(string relativePath)
    {
        var rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        return Path.GetFullPath(Path.Combine(rootFolder, relativePath));
    }
}
