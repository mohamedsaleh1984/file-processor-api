using FileProcessorApi.Hubs;
using FileProcessorApi.Models;
using Microsoft.AspNetCore.SignalR;
namespace FileProcessorApi.Services
{
    public class FileProcessingService : BackgroundService
    {
        private readonly IBackgroundQueue<FileProcessingTask> _queue;
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly ILogger<FileProcessingService> _logger;
        private readonly IHubContext<ProcessingHub> _hubContext;

        public FileProcessingService(ILogger<FileProcessingService> logger,
                                IBackgroundQueue<FileProcessingTask> queue,
                                IHubContext<ProcessingHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
            _queue = queue;
        }

        public async Task<string> EnqueueTask(IFormFile file)
        {
            var taskId = Guid.NewGuid().ToString();
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(uploadsPath);
            var filePath = Path.Combine(uploadsPath, $"{taskId}_{file.FileName}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var task = new FileProcessingTask
            {
                Id = taskId,
                OriginalFileName = file.FileName,
                Status = ProcessingStatus.Pending
            };

            _queue.Enqueue(task);
            _signal.Release();

            return taskId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Type} is now running in the background.", 
                    nameof(FileProcessingService));

            await BackgroundProcessing(stoppingToken);
        }


        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    FileProcessingTask task = _queue.Dequeue();
                    if (task == null)
                        continue;
                    await _hubContext.Clients.Group(task.Id).SendAsync("ProcessStarted");
                    task.Status = ProcessingStatus.Processing;

                    // Simulate file processing
                    for (int i = 0; i <= 100; i++)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        task.ProgressPercentage = i;
                        await _hubContext.Clients.Group(task.Id).SendAsync("ProgressUpdate", i, cancellationToken: stoppingToken);
                        await Task.Delay(100, stoppingToken);
                    }

                    task.ProcessedFilePath = "NEW_FILE....txt";
                    task.Status = ProcessingStatus.Completed;


                    // Notify clients of completion
                    await _hubContext.Clients.Group(task.Id).SendAsync("ProcessingCompleted", cancellationToken: stoppingToken);

                }
                catch (Exception ex)
                {
                    _logger.LogCritical("An error occurred when publishing a customer. Exception: {@Exception}", ex);
                }
            }
        }

    }
}
