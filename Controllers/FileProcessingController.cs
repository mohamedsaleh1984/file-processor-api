using FileProcessorApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessorApi.Controllers
{
    public class FileProcessingController: BaseController
    {
        private readonly FileProcessingService _processingService;
        public FileProcessingController(FileProcessingService processingService)
        {
            _processingService = processingService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var taskId = await _processingService.PreProcessTask(file);
            return Ok(new { taskId });
        }

        [HttpGet("download/{taskId}")]
        public IActionResult DownloadFile(string taskId)
        {
            // Implement logic to get the processed file path from your storage
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "processed", $"{taskId}_processed_filename");
            return PhysicalFile(filePath, "application/octet-stream", Path.GetFileName(filePath));
        }
    }
}
