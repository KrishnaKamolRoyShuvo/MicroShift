using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroShift.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        public string ReviewerId { get; set; }
        [ForeignKey("ReviewerId")]
        public virtual ApplicationUser Reviewer { get; set; }

        public string RevieweeId { get; set; }
        [ForeignKey("RevieweeId")]
        public virtual ApplicationUser Reviewee { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}