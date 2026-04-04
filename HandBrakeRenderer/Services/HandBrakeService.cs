using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HandBrakeRenderer.Services;

public class HandBrakeService : IHandBrakeService
{
    private readonly AppSettings _settings;
    private readonly ILogger<HandBrakeService> _logger;

    public HandBrakeService(IOptions<AppSettings> settings, ILogger<HandBrakeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> RenderFileAsync(string inputFile, string outputFile, string presetFile, string presetName, CancellationToken cancellationToken = default)
    {
        var handBrakePath = _settings.GetFullPath(_settings.HandbrakeCLI);
        
        var arguments = $"--preset-import-file \"{presetFile}\" -Z \"{presetName}\" -i \"{inputFile}\" -o \"{outputFile}\"";
        _logger.LogInformation("Starting HandBrake: {Arguments}", arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = handBrakePath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start HandBrake process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Successfully rendered {InputFile}", Path.GetFileName(inputFile));
                return true;
            }
            else
            {
                _logger.LogError("HandBrake exited with code {ExitCode}", process.ExitCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering file {InputFile}", inputFile);
            return false;
        }
    }
}
