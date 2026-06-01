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

        // --- PUBLIC PROFILE DETAILS ---
        public string? ProfilePictureUrl { get; set; } // NEW: For Public Profiles
        public string Address { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Interests { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }

        // GPS Coordinates from the Free Map API
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // --- SECURITY & MODERATION PENALTY BOX ---
        public int RecoveryFailedAttempts { get; set; } = 0;
        public DateTime? RecoveryLockoutEnd { get; set; }
        public bool IsAccountFrozen { get; set; } = false;

        public DateTime? SuspensionEndDate { get; set; } // NEW: 15-day / 1-month bans
        public bool IsPermanentlyBanned { get; set; } = false; // NEW: The ultimate kill switch

        // --- RATINGS SYSTEM ---
        public double AverageRating { get; set; } = 0.0;
        public int TotalReviews { get; set; } = 0;

        // --- WALLET SYSTEM ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal WalletBalance { get; set; } = 0;
    }
}