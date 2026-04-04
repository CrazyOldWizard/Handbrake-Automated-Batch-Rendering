using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public class FileManagementService : IFileManagementService
{
    private readonly AppSettings _settings;
    private readonly ILogger<FileManagementService> _logger;
    private readonly string _localWorkingFolder;

    public FileManagementService(IOptions<AppSettings> settings, ILogger<FileManagementService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _localWorkingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HandBrakeRenderer");
    }

    public async Task EnsureDirectoriesExistAsync()
    {
        var directories = new[]
        {
            _settings.GetFullPath(_settings.OriginalFilesFolder),
            _settings.GetFullPath(_settings.OutboxFolder),
            _settings.GetFullPath(_settings.UtilitiesFolder),
            _settings.GetFullPath(_settings.InboxFolder),
            _localWorkingFolder
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Directory {Directory} does not exist, creating...", directory);
                Directory.CreateDirectory(directory);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<bool> IsFileReadyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return await Task.FromResult(stream.Length > 0);
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetPresetFilesAsync()
    {
        var utilsFolder = _settings.GetFullPath(_settings.UtilitiesFolder);
        
        if (!Directory.Exists(utilsFolder))
        {
            return Enumerable.Empty<string>();
        }

        return await Task.FromResult(Directory.GetFiles(utilsFolder, "*.json", SearchOption.TopDirectoryOnly));
    }

    public async Task CreatePresetDirectoriesAsync()
    {
        var presetFiles = await GetPresetFilesAsync();
        var inboxFolder = _settings.GetFullPath(_settings.InboxFolder);

        foreach (var presetFile in presetFiles)
        {
            var presetName = Path.GetFileNameWithoutExtension(presetFile);
            var presetFolder = Path.Combine(inboxFolder, presetName);

            if (Directory.Exists(presetFolder))
            {
                var fileCount = Directory.GetFiles(presetFolder).Length;
                if (fileCount > 0)
                {
                    _logger.LogInformation("Preset folder '{PresetName}' has {FileCount} files in queue", presetName, fileCount);
                }
            }
            else
            {
                _logger.LogInformation("Creating preset folder '{PresetName}'", presetName);
                Directory.CreateDirectory(presetFolder);
            }
        }
    }

    public async Task<IEnumerable<(string PresetFolder, string VideoFile)>> GetFilesToProcessAsync()
    {
        var files = new List<(string, string)>();
        var inboxFolder = _settings.GetFullPath(_settings.InboxFolder);

        if (!Directory.Exists(inboxFolder))
        {
            return files;
        }

        foreach (var presetFolder in Directory.GetDirectories(inboxFolder, "*", SearchOption.TopDirectoryOnly))
        {
            foreach (var videoFile in Directory.GetFiles(presetFolder, "*.*", SearchOption.AllDirectories))
            {
                if (!videoFile.EndsWith(".renderjob"))
                {
                    files.Add((presetFolder, videoFile));
                }
            }
        }

        return await Task.FromResult(files);
    }

    public async Task HandleCompletedFileAsync(string originalFile, string outputFile, string nodeJobFile, CancellationToken cancellationToken = default)
    {
        var movieName = Path.GetFileName(originalFile);

        if (_settings.DeleteOriginalFiles)
        {
            _logger.LogInformation("Deleting original file: {File}", originalFile);
            File.Delete(originalFile);
        }
        else
        {
            var originalFilesFolder = _settings.GetFullPath(_settings.OriginalFilesFolder);
            var destination = Path.Combine(originalFilesFolder, movieName);
            
            _logger.LogInformation("Moving original file to: {Destination}", destination);
            
            try
            {
                File.Move(originalFile, destination, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file to original files folder");
            }
        }

        if (File.Exists(nodeJobFile))
        {
            File.Delete(nodeJobFile);
        }

        await Task.CompletedTask;
    }
}
