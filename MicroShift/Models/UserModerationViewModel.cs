namespace MicroShift.Models
{
    public class UserModerationViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public decimal TotalEarnings { get; set; }
        public decimal OngoingWorkValue { get; set; }
        public int TotalCompleted { get; set; }
        public int ApplicationsCount { get; set; }
        public int DisputeCount { get; set; }

        public bool IsSuspended { get; set; }
    }
}