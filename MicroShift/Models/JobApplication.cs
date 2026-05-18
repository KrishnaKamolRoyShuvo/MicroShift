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



        // --- NEW: WORKER EVIDENCE (For "Mark as Done") ---
        public string? WorkerEvidenceUrl { get; set; }
        public DateTime? WorkerEvidenceDate { get; set; }
        public bool IsWorkerTimeFraudDetected { get; set; } = false;



        // --- NEW: EMPLOYER EVIDENCE (For "Raise Dispute") ---
        public string? EmployerDisputeUrl { get; set; }
        public DateTime? EmployerDisputeDate { get; set; }
        public bool IsEmployerTimeFraudDetected { get; set; } = false;



        // --- NEW: AI MODERATION ---
        public double? AiFraudProbabilityScore { get; set; }
    }
}