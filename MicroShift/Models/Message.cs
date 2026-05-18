using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroShift.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        // The job this conversation is about
        [Required]
        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public virtual Job Job { get; set; }

        // The user sending the message
        [Required]
        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }

        // The user receiving the message
        [Required]
        public string ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }




        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Security Flag: True if the system detected restricted keywords
        public bool IsFlaggedBySystem { get; set; } = false;

        // Tells the UI if the receiver has seen it yet
        public bool IsRead { get; set; } = false;
    }
}