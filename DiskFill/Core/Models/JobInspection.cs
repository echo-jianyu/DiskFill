namespace DiskFill.Core.Models
{
    public sealed class JobInspection
    {
        public FillJob Job { get; set; }

        public string Message { get; set; }

        public bool HasJob
        {
            get { return Job != null; }
        }

        public bool HasIssue
        {
            get { return !string.IsNullOrEmpty(Message); }
        }
    }
}
