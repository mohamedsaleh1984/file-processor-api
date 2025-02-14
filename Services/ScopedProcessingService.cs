using FileProcessorApi.Hubs;
using FileProcessorApi.Models;
using Microsoft.AspNetCore.SignalR;

namespace FileProcessorApi.Services
{
    public interface IScopedProcessingService
    {
        Task DoWork(FileProcessingTask task, CancellationToken stoppingToken);
    }

    public class ScopedProcessingService : IScopedProcessingService
    {
        private int executionCount = 0;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly ILogger _logger;

        public ScopedProcessingService(ILogger<ScopedProcessingService> logger,
            IHubContext<ProcessingHub> hubContext)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task DoWork(FileProcessingTask task, CancellationToken ct)
        {
            try
            {
                task.Status = ProcessingStatus.Processing;

                // Simulate processing - replace with actual logic
                for (int i = 0; i <= 100; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    task.ProgressPercentage = i;
                    await _hubContext.Clients.Group(task.Id).SendAsync("ProgressUpdate", i, cancellationToken: ct);
                    await Task.Delay(100, ct);
                }

                task.ProcessedFilePath = "NEW_FILE....txt";
                task.Status = ProcessingStatus.Completed;

                // Notify clients of completion
                await _hubContext.Clients.Group(task.Id).SendAsync("ProcessingCompleted", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing task {task.Id}");
                task.Status = ProcessingStatus.Failed;
                task.ProgressPercentage = 0;
                await _hubContext.Clients.Group(task.Id).SendAsync("ProcessingFailed", ex.Message);
            }
        }
    }
}
