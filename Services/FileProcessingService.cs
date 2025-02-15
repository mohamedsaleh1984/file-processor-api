using FileProcessorApi.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FileProcessorApi.Services
{
    public class ProcessingHub : Hub
    {
        public async Task JoinTaskGroup(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
        }
    }

    public class FileProcessingService : BackgroundService
    {
        private readonly ILogger<FileProcessingService> _logger;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private static ConcurrentQueue<FileProcessingTask> _queue = new();

        public FileProcessingService(ILogger<FileProcessingService> logger,
                                IHubContext<ProcessingHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<string> PreProcessTask(IFormFile file)
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

            EnqueueTask(task);


            return taskId;
        }
        public static void EnqueueTask(FileProcessingTask task)
        {
            _queue.Enqueue(task);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {

                if (_queue.TryDequeue(out FileProcessingTask? task))
                {

                    await _hubContext.Clients.Group(task.Id).SendAsync("ProcessStarted");
                    task.Status = ProcessingStatus.Processing;

                    // Simulate file processing
                    for (int i = 0; i <= 100; i++)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        task.ProgressPercentage = i;
                        await _hubContext.Clients.Group(task.Id).SendAsync("ProgressUpdate", i, cancellationToken: stoppingToken);
                        await Task.Delay(50, stoppingToken);
                    }

                    task.ProcessedFilePath = "NEW_FILE....txt";
                    task.Status = ProcessingStatus.Completed;


                    // Notify clients of completion
                    await _hubContext.Clients.Group(task.Id).SendAsync("ProcessingCompleted", cancellationToken: stoppingToken);
                }
                else
                {
                    await Task.Delay(1000); // Avoid CPU overuse
                }
            }
        }



    }
}
