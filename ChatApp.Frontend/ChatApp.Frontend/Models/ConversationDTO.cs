using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChatApp.Frontend.Models.ConversationDTO;

namespace ChatApp.Frontend.Models
{
    public class OtherUserDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ConversationDTO
    {
        public Guid ConversationId { get; set; }
        public string Title { get; set; }
        public bool IsGroup { get; set; }
        public OtherUserDto OtherUser { get; set; }

        // --- Hỗ trợ hiển thị XAML (UI Helpers) ---
        public string DisplayName => IsGroup ? Title : OtherUser?.Username ?? "Người dùng";
        public string DisplayAvatar => string.IsNullOrEmpty(OtherUser?.AvatarUrl) ? "dotnet_bot.png" : OtherUser.AvatarUrl;

        public string LastMessageContent { get; set; }
        public DateTime? LastMessageCreatedAt { get; set; }
    }
}
