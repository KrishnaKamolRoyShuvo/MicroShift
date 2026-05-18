using System;

namespace MicroShift.Models
{
    public class ConversationViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }

        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }

        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }

        public int UnreadCount { get; set; }
    }
}