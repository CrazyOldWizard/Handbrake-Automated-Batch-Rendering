using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public class RenderWorker : BackgroundService
{
    private readonly IFileManagementService _fileService;
    private readonly IRenderQueueService _renderQueueService;
    private readonly ILogger<RenderWorker> _logger;
    private readonly AppSettings _settings;

    public RenderWorker(
        IFileManagementService fileService,
        IRenderQueueService renderQueueService,
        ILogger<RenderWorker> logger,
        IOptions<AppSettings> settings)
    {
        _fileService = fileService;
        _renderQueueService = renderQueueService;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HandBrake Automated Rendering Service started");

        // Initial setup
        await _fileService.EnsureDirectoriesExistAsync();
        await ValidateHandBrakeInstallation(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.Title = "HandBrake Automated Rendering: Looking for work";
                _logger.LogInformation("Checking for work at {Time}", DateTime.Now);

                await _fileService.CreatePresetDirectoriesAsync();
                await _renderQueueService.ProcessRenderQueueAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in render loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("HandBrake Automated Rendering Service stopped");
    }

    private async Task ValidateHandBrakeInstallation(CancellationToken cancellationToken)
    {
        var handBrakePath = _settings.GetFullPath(_settings.HandbrakeCLI);
        
        if (!File.Exists(handBrakePath))
        {
            _logger.LogCritical("HandBrakeCLI not found at: {Path}", handBrakePath);
            _logger.LogInformation("Please download HandBrakeCLI from: https://handbrake.fr/downloads2.php");
            
            OpenBrowserToDownloadPage();
            
            _logger.LogCritical("Application will exit in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            Environment.Exit(1);
        }

        var utilsFolder = _settings.GetFullPath(_settings.UtilitiesFolder);
        var presetFiles = Directory.GetFiles(utilsFolder, "*.json", SearchOption.TopDirectoryOnly);
        
        if (presetFiles.Length == 0)
        {
            _logger.LogWarning("No preset files found in {UtilsFolder}", utilsFolder);
            _logger.LogWarning("Please add HandBrake preset JSON files to this folder");
            _logger.LogWarning("The preset file name should match the preset name in HandBrake UI");
        }
    }

    private void OpenBrowserToDownloadPage()
    {
        try
        {
            var url = "https://handbrake.fr/downloads2.php";
            
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open browser to download page");
        }
    }
}
