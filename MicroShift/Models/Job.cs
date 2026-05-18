using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroShift.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // --- Descriptive Images (Up to 5) ---
        public string? JobImageUrl { get; set; }  // Acts as the Cover Photo/Evidence Reference
        public string? JobImageUrl2 { get; set; }
        public string? JobImageUrl3 { get; set; }
        public string? JobImageUrl4 { get; set; }
        public string? JobImageUrl5 { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty; // General area (e.g., Gulshan)

        // --- Geospatial Mapping (Leaflet.js Integration) ---
        // Changed to non-nullable double as per Sprint 1 requirement for Pin-Must logic
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? LocationDirections { get; set; } // Additional text instructions for location

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAmount { get; set; }

        public string RequiredSkills { get; set; } = string.Empty;

        // --- Category System ---
        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // --- Commission & Finance ---
        // Stored at the moment of posting to protect against future global rate changes
        public double FinalCommissionPercentage { get; set; } = 10.0;
        public bool IsCommissionOverridden { get; set; } = false;

        // --- Analytics & Reach ---
        public int ViewCount { get; set; } = 0;      // "Clicks"
        public int ImpressionCount { get; set; } = 0; // "Reach"

        // --- Status & Urgency ---
        public string UrgencyLevel { get; set; } = "Normal"; // kept for backward compatibility
        public bool IsEmergency { get; set; } = false;       // New pulsing red border logic

        // "Open", "InProgress", "ReviewPending", "Completed", "Cancelled", "Disputed"
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- AI Moderation ---
        public bool IsAIApproved { get; set; } = false;
        public string? AIModerationNote { get; set; }

        // --- Relational Data ---
        [Required]
        public string EmployerId { get; set; } = string.Empty;

        [ForeignKey("EmployerId")]
        public virtual ApplicationUser? Employer { get; set; }

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

        // --- Calculated Logic ---
        [NotMapped]
        public double DistanceFromUser { get; set; }

        [Required]
        public string JobType { get; set; } = "Offline"; // "Online", "Remote", "Offline"

        [Required]
        public string Shift { get; set; } = "Flexible"; // "Day", "Night", "Flexible"
    }
}