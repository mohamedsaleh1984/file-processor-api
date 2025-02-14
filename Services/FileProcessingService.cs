using FileProcessorApi.Hubs;
using FileProcessorApi.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FileProcessorApi.Services
{
    public class FileProcessingService : BackgroundService
    {
        public IServiceProvider Services { get; }
        
        private readonly ConcurrentQueue<FileProcessingTask> _taskQueue = new();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly ILogger<FileProcessingService> _logger;

        public FileProcessingService(IServiceProvider services, ILogger<FileProcessingService> logger)
        {
            Services = services;
            _logger = logger;
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

            _taskQueue.Enqueue(task);
            _signal.Release();

            return taskId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    await _signal.WaitAsync(stoppingToken);

            //    if (_taskQueue.TryDequeue(out var task))
            //    {
            //        try
            //        {
            //            _logger.LogInformation("Signal received, processing task");

            //            _logger.LogInformation($"Processing task {task.Id}");
            //            await DoWork(task, stoppingToken);
            //        }
            //        catch (Exception ex)
            //        {
            //            task.Status = ProcessingStatus.Failed;
            //            await _hubContext.Clients.Group(task.Id).SendAsync("ProcessingFailed", ex.Message);
            //        }
            //    }
            //}
        }



        private string ProcessFile(FileProcessingTask task)
        {
            // Implement your actual file processing here
            var processedPath = Path.Combine(Directory.GetCurrentDirectory(), "processed", $"{task.Id}_processed_{task.OriginalFileName}");
            File.Copy(Path.Combine("uploads", $"{task.Id}_{task.OriginalFileName}"), processedPath);
            return processedPath;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
