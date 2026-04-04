using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public interface IFileManagementService
{
    Task EnsureDirectoriesExistAsync();
    Task<bool> IsFileReadyAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetPresetFilesAsync();
    Task CreatePresetDirectoriesAsync();
    Task<IEnumerable<(string PresetFolder, string VideoFile)>> GetFilesToProcessAsync();
    Task HandleCompletedFileAsync(string originalFile, string outputFile, string nodeJobFile, CancellationToken cancellationToken = default);
}
