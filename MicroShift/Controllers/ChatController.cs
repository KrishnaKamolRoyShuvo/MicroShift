using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroShift.Data;
using MicroShift.Models;

namespace MicroShift.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly MicroShiftDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(MicroShiftDBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Chat/Index?jobId=5&receiverId=abc-123
        // GET: /Chat/Index?jobId=5&receiverId=abc-123
        public async Task<IActionResult> Index(int jobId, string receiverId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var job = await _context.Jobs.FindAsync(jobId);
            var receiver = await _userManager.FindByIdAsync(receiverId);

            if (job == null || receiver == null) return NotFound("Job or User not found.");

            // Load Chat History
            var messages = await _context.Messages
                .Where(m => m.JobId == jobId &&
                           ((m.SenderId == currentUser.Id && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == currentUser.Id)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // --- NEW LOGIC: MARK UNREAD MESSAGES AS READ ---
            // Find messages sent TO the current user that are still unread
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync(); // Save the "Read" status to the database
            }
            // -----------------------------------------------

            ViewBag.Job = job;
            ViewBag.Receiver = receiver;
            ViewBag.CurrentUserId = currentUser.Id;

            return View(messages);
        }
        // GET: /Chat/Inbox
        public async Task<IActionResult> Inbox()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Fetch all messages where the user is either the sender or receiver
            var allMessages = await _context.Messages
                .Include(m => m.Job)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                .ToListAsync();



            // Group the messages into unique conversations (by Job and the Other User)
            var conversations = allMessages
                .GroupBy(m => new
                {
                    m.JobId,
                    JobTitle = m.Job.Title,
                    // Determine who the "Other User" is
                    OtherUserId = m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId,
                    OtherUserName = m.SenderId == currentUser.Id
                        ? (m.Receiver.FullName ?? m.Receiver.Email)
                        : (m.Sender.FullName ?? m.Sender.Email)
                })
                .Select(g => new ConversationViewModel
                {
                    JobId = g.Key.JobId,
                    JobTitle = g.Key.JobTitle,
                    OtherUserId = g.Key.OtherUserId,
                    OtherUserName = g.Key.OtherUserName,

                    // Get the content and time of the very last message in this group
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Content,
                    LastMessageTime = g.OrderByDescending(m => m.SentAt).First().SentAt,

                    // Count messages sent TO the current user that are unread
                    UnreadCount = g.Count(m => m.ReceiverId == currentUser.Id && !m.IsRead)
                })
                // Order the inbox so the newest conversations are at the top
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return View(conversations);
        }
        // POST: /Chat/UploadImage
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            // Create a folder in wwwroot to store chat images safely
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Give the file a unique name so users don't overwrite each other's files
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the path so SignalR can send it to the other user
            var imageUrl = "/uploads/chat/" + uniqueFileName;
            return Ok(new { imageUrl = imageUrl });
        }
    }
}