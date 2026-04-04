using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public class RenderQueueService : IRenderQueueService
{
    private readonly AppSettings _settings;
    private readonly IFileManagementService _fileService;
    private readonly IHandBrakeService _handBrakeService;
    private readonly ILogger<RenderQueueService> _logger;
    private readonly string _localWorkingFolder;

    public RenderQueueService(
        IOptions<AppSettings> settings,
        IFileManagementService fileService,
        IHandBrakeService handBrakeService,
        ILogger<RenderQueueService> logger)
    {
        _settings = settings.Value;
        _fileService = fileService;
        _handBrakeService = handBrakeService;
        _logger = logger;
        _localWorkingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HandBrakeRenderer");
    }

    public async Task ProcessRenderQueueAsync(CancellationToken cancellationToken = default)
    {
        var filesToProcess = await _fileService.GetFilesToProcessAsync();
        var utilsFolder = _settings.GetFullPath(_settings.UtilitiesFolder);
        var outboxFolder = _settings.GetFullPath(_settings.OutboxFolder);

        foreach (var (presetFolder, videoFile) in filesToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var presetFolderName = new DirectoryInfo(presetFolder).Name;
            var presetFile = Path.Combine(utilsFolder, $"{presetFolderName}.json");

            if (!File.Exists(presetFile))
            {
                _logger.LogWarning("Preset file not found: {PresetFile}", presetFile);
                continue;
            }

            var movieName = Path.GetFileName(videoFile);
            var movieNameNoExt = Path.GetFileNameWithoutExtension(videoFile);
            var nodeJobFile = Path.Combine(presetFolder, $"{movieNameNoExt}.renderjob");
            var outputFile = Path.Combine(outboxFolder, $"{movieNameNoExt}.mkv");

            if (File.Exists(nodeJobFile))
            {
                _logger.LogInformation("{Movie} is being rendered by another node, skipping", movieNameNoExt);
                continue;
            }

            var extension = Path.GetExtension(videoFile);
            if (!_settings.FileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("{Movie} has unsupported extension {Extension}", movieName, extension);
                continue;
            }

            if (!await _fileService.IsFileReadyAsync(videoFile, cancellationToken))
            {
                _logger.LogWarning("{Movie} is not ready (probably still copying)", movieName);
                continue;
            }

            await RenderFileAsync(videoFile, presetFile, presetFolderName, outputFile, nodeJobFile, cancellationToken);
            
            // Process one file at a time
            return;
        }
    }

    private async Task RenderFileAsync(
        string videoFile,
        string presetFile,
        string presetName,
        string outputFile,
        string nodeJobFile,
        CancellationToken cancellationToken)
    {
        var movieName = Path.GetFileName(videoFile);
        var movieNameNoExt = Path.GetFileNameWithoutExtension(videoFile);

        // Create node job file
        await File.WriteAllTextAsync(nodeJobFile, 
            $"Node {Environment.MachineName} started this job at {DateTime.Now}", 
            cancellationToken);

        _logger.LogInformation("Starting render of {Movie} using preset {Preset}", movieName, presetName);
        Console.Title = $"HandBrake Automated Rendering: {movieName} -- {DateTime.Now}";

        var videoFileToRender = videoFile;

        // Copy to local if configured
        if (_settings.CopyRenderFileLocal)
        {
            _logger.LogInformation("Copying file to local folder...");
            var localVideoFile = Path.Combine(_localWorkingFolder, movieName);
            File.Copy(videoFile, localVideoFile, true);
            videoFileToRender = localVideoFile;
        }

        // Render the file
        var success = await _handBrakeService.RenderFileAsync(
            videoFileToRender,
            outputFile,
            presetFile,
            presetName,
            cancellationToken);

        if (success)
        {
            _logger.LogInformation("Render completed successfully: {Movie}", movieName);
            await _fileService.HandleCompletedFileAsync(videoFile, outputFile, nodeJobFile, cancellationToken);

            // Clean up local copy if it was used
            if (_settings.CopyRenderFileLocal && File.Exists(videoFileToRender) && videoFileToRender != videoFile)
            {
                File.Delete(videoFileToRender);
            }
        }
        else
        {
            _logger.LogError("Render failed for {Movie}", movieName);
            if (File.Exists(nodeJobFile))
            {
                File.Delete(nodeJobFile);
            }
        }
    }
}
