using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MicroShift.Data;
using MicroShift.Models;
using System.Text.RegularExpressions;

namespace MicroShift.Hubs
{
    [Authorize] // Only logged-in users can use the chat
    public class ChatHub : Hub
    {
        private readonly MicroShiftDBContext _context;

        public ChatHub(MicroShiftDBContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int jobId, string receiverId, string messageContent, string imageUrl = null)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId)) return;

            bool isFlagged = false;
            string filteredContent = messageContent ?? ""; // Prevent null errors if they only send an image

            // Only run the security filter if they actually typed text
            if (!string.IsNullOrEmpty(filteredContent))
            {
                var forbiddenWords = new List<string> { "bkash", "nagad", "rocket", "call me", "phone", "whatsapp" };
                var phoneRegex = new Regex(@"\b(01[3-9]\d{8})\b");

                if (forbiddenWords.Any(w => filteredContent.ToLower().Contains(w)) || phoneRegex.IsMatch(filteredContent))
                {
                    isFlagged = true;
                    filteredContent = "[SYSTEM WARNING: Message blocked for containing restricted contact or payment information.]";
                }
            }

            var chatMessage = new Message
            {
                JobId = jobId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = filteredContent,
                ImageUrl = imageUrl,  // Save the image URL
                SentAt = DateTime.UtcNow,
                IsFlaggedBySystem = isFlagged,
                IsRead = false
            };

            _context.Messages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Push BOTH text and image URL to the clients
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, filteredContent, imageUrl ?? "", chatMessage.SentAt.ToString("MMM dd, h:mm tt"));
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, filteredContent, imageUrl ?? "", chatMessage.SentAt.ToString("MMM dd, h:mm tt"));
        }
    }
}