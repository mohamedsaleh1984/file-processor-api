namespace FileProcessorApi.Models
{
    public class FileProcessingTask
    {
        public string Id { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string ProcessedFilePath { get; set; } = "";
        public ProcessingStatus Status { get; set; } = ProcessingStatus.NotStarted;
        public int ProgressPercentage { get; set; } = 0;
    }

    public enum ProcessingStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        NotStarted
    }
}
