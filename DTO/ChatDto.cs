using System;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.DTO
{
    public class ChatCreateDto
    {
        [Required]
        public int TourGuideId { get; set; }
    }

    public class ChatDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public string TourGuideProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LastMessageContent { get; set; }
        public DateTime? LastMessageSentAt { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    public class MessageCreateDto
    {
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
    public class MessageDeletedDto { public int MessageId { get; set; } public int ChatId { get; set; } }
}