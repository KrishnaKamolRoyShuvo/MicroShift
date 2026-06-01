using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroShift.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsShadowBanned { get; set; } = false;
        public string? NationalIdNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- NEW FIELDS FOR FRAUD PREVENTION & MATCHING ---

        public string Address { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Interests { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }

        // GPS Coordinates from the Free Map API
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // --- SECURITY: EXPONENTIAL BACKOFF TRACKING ---
        public int RecoveryFailedAttempts { get; set; } = 0;
        public DateTime? RecoveryLockoutEnd { get; set; }
        public bool IsAccountFrozen { get; set; } = false;

        // --- RATINGS SYSTEM ---
        public double AverageRating { get; set; } = 0.0;
        public int TotalReviews { get; set; } = 0;

        // --- WALLET SYSTEM ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal WalletBalance { get; set; } = 0;
    }
}