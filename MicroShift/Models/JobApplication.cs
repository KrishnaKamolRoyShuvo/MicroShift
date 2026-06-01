using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroShift.Models
{
    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        // Links to the Job they are applying for
        [Required]
        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public virtual Job? Job { get; set; }

        // Links to the Worker who is applying
        [Required]
        public string WorkerId { get; set; } = string.Empty;
        [ForeignKey("WorkerId")]
        public virtual ApplicationUser? Worker { get; set; }

        // Tracks the application state: "Pending", "Accepted", "Rejected"
        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        // --- WORKER EVIDENCE (For "Mark as Done") ---
        public string? WorkerEvidenceUrl { get; set; }
        public DateTime? WorkerEvidenceDate { get; set; }
        public bool IsWorkerTimeFraudDetected { get; set; } = false;

        // --- NEW: FORENSIC DISPUTE COURTROOM ---
        public string? DisputeInitiator { get; set; } // Will store "Worker" or "Employer"

        // Employer's Side of the Dispute
        public string? EmployerDisputeText { get; set; }
        public string? EmployerDisputeImageUrl { get; set; }
        public DateTime? EmployerDisputeExifTime { get; set; }
        public bool IsEmployerTimeFraudDetected { get; set; } = false;

        // Worker's Side of the Dispute
        public string? WorkerDisputeText { get; set; }
        public string? WorkerDisputeImageUrl { get; set; }
        public DateTime? WorkerDisputeExifTime { get; set; }

        // --- AI MODERATION ---
        public double? AiFraudProbabilityScore { get; set; }
    }
}