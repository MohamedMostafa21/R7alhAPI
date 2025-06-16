using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Hubs;
using R7alaAPI.Models;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatsController(ApplicationDBContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        /// <summary>
        /// Creates a new chat between the authenticated user and a tour guide.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] ChatCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int userId = GetCurrentUserId();
            var tourGuide = await _context.TourGuides
                .Include(tg => tg.User)
                .FirstOrDefaultAsync(tg => tg.Id == dto.TourGuideId);

            if (tourGuide == null)
                return NotFound(new { message = "Tour guide not found." });

            if (tourGuide.UserId == userId)
                return BadRequest(new { message = "Cannot create a chat with yourself." });

            if (await ChatExists(userId, dto.TourGuideId))
                return Conflict(new { message = "Chat already exists.", chatId = await GetExistingChatId(userId, dto.TourGuideId) });

            var chat = new Chat
            {
                UserId = userId,
                TourGuideId = dto.TourGuideId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetChats), new { id = chat.Id }, await MapToChatDto(chat));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create chat: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves all chats for the authenticated user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            int userId = GetCurrentUserId();

            var chats = await _context.Chats
                .Include(c => c.User)
                .Include(c => c.TourGuide).ThenInclude(tg => tg.User)
                .Include(c => c.Messages)
                .Where(c => c.UserId == userId || c.TourGuide.UserId == userId)
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
                .ToListAsync();

            var chatDtos = await Task.WhenAll(chats.Select(MapToChatDto));
            return Ok(chatDtos);
        }

        /// <summary>
        /// Retrieves all messages in a specific chat.
        /// </summary>
        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            int userId = GetCurrentUserId();

            var chat = await _context.Chats
                .Include(c => c.TourGuide).ThenInclude(tg => tg.User)
                .Include(c => c.Messages).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound(new { message = "Chat not found." });

            if (!HasAccessToChat(chat, userId))
                return StatusCode(403, new { message = "You do not have access to this chat." });

            await MarkMessagesAsRead(chat, userId);

            var messageDtos = chat.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FirstName,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                })
                .ToList();

            return Ok(messageDtos);
        }

        /// <summary>
        /// Sends a new message in a specific chat and broadcasts it to the recipient.
        /// </summary>
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] MessageCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int userId = GetCurrentUserId();

            var chat = await _context.Chats
                .Include(c => c.TourGuide)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound(new { message = "Chat not found." });

            if (!HasAccessToChat(chat, userId))
                return StatusCode(403, new { message = "You do not have access to this chat." });

            var message = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow
            };

            try
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var messageDto = await MapToMessageDto(message);
                await BroadcastMessage(chat, userId, messageDto);

                return CreatedAtAction(nameof(GetMessages), new { chatId }, messageDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to send message: {ex.Message}" });
            }
        }

        /// <summary>
        /// Deletes a specific message in a chat if sent by the authenticated user and notifies the recipient.
        /// </summary>
        [HttpDelete("{chatId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int chatId, int messageId)
        {
            int userId = GetCurrentUserId();

            var chat = await _context.Chats
                .Include(c => c.TourGuide)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound(new { message = "Chat not found." });

            if (!HasAccessToChat(chat, userId))
                return StatusCode(403, new { message = "You do not have access to this chat." });

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId);

            if (message == null)
                return NotFound(new { message = "Message not found." });

            if (message.SenderId != userId)
                return StatusCode(403, new { message = "You can only delete your own messages." });

            try
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                var messageDeletedDto = new MessageDeletedDto
                {
                    MessageId = messageId,
                    ChatId = chatId
                };
                await BroadcastMessageDeleted(chat, userId, messageDeletedDto);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete message: {ex.Message}" });
            }
        }

        #region Private Helper Methods

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found in token."));
        }

        private async Task<bool> ChatExists(int userId, int tourGuideId)
        {
            return await _context.Chats.AnyAsync(c => c.UserId == userId && c.TourGuideId == tourGuideId);
        }

        private async Task<int?> GetExistingChatId(int userId, int tourGuideId)
        {
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.UserId == userId && c.TourGuideId == tourGuideId);
            return chat?.Id;
        }

        private bool HasAccessToChat(Chat chat, int userId)
        {
            return chat.UserId == userId || chat.TourGuide.UserId == userId;
        }

        private async Task MarkMessagesAsRead(Chat chat, int userId)
        {
            var unreadMessages = chat.Messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<ChatDto> MapToChatDto(Chat chat)
        {
            var lastMessage = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            int userId = GetCurrentUserId();

            return new ChatDto
            {
                Id = chat.Id,
                UserId = chat.UserId,
                UserName = chat.User?.FirstName,
                TourGuideId = chat.TourGuideId,
                TourGuideName = chat.TourGuide?.User?.FirstName,
                TourGuideProfilePictureUrl = chat.TourGuide?.User?.ProfilePictureUrl,
                CreatedAt = chat.CreatedAt,
                LastMessageContent = lastMessage?.Content,
                LastMessageSentAt = lastMessage?.SentAt,
                HasUnreadMessages = chat.Messages.Any(m => m.SenderId != userId && !m.IsRead)
            };
        }

        private async Task<MessageDto> MapToMessageDto(Message message)
        {
            var sender = await _context.Users.FindAsync(message.SenderId);
            return new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                SenderName = sender?.FirstName,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
        }

        private async Task BroadcastMessage(Chat chat, int senderId, MessageDto messageDto)
        {
            int recipientId = chat.UserId == senderId ? chat.TourGuide.UserId : chat.UserId;
            await _hubContext.Clients.Group($"user-{recipientId}").SendAsync("ReceiveMessage", messageDto);
        }

        private async Task BroadcastMessageDeleted(Chat chat, int senderId, MessageDeletedDto messageDeletedDto)
        {
            int recipientId = chat.UserId == senderId ? chat.TourGuide.UserId : chat.UserId;
            await _hubContext.Clients.Group($"user-{recipientId}").SendAsync("ReceiveMessageDeleted", messageDeletedDto);
        }

        #endregion
    }
}