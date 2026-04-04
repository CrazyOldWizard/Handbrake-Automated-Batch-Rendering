using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public interface IHandBrakeService
{
    Task<bool> RenderFileAsync(string inputFile, string outputFile, string presetFile, string presetName, CancellationToken cancellationToken = default);
}
