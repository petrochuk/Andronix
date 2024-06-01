using Andronix.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Andronix.AssistantAI;

public class QueuedAssistantService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;

    public QueuedAssistantService(IBackgroundTaskQueue taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    #region Background Service

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error occurred executing task work item. {ex.Message}");
            }
        }
    }

    #endregion
}
