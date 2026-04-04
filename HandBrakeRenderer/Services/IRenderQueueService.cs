using System.Threading;
using System.Threading.Tasks;

namespace HandBrakeRenderer.Services;

public interface IRenderQueueService
{
    Task ProcessRenderQueueAsync(CancellationToken cancellationToken = default);
}
