namespace DiskFill.Core.Models
{
    public enum FillJobState
    {
        Ready = 0,
        Running = 1,
        Paused = 2,
        Completed = 3,
        Failed = 4
    }
}
