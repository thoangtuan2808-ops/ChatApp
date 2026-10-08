using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Frontend.Models
{
    public class ChatModel
    {
        public Guid ConversationId { get; set; }

        // Thuộc tính gốc từ Database
        public string Title { get; set; }
        public bool IsGroup { get; set; }
        public string OtherUserName { get; set; }
        public DateTime? LastMessageTime { get; set; }

        // --- CÁC THUỘC TÍNH BINDING TRỰC TIẾP LÊN XAML ---

        public string Name => IsGroup ? Title : OtherUserName ?? "Người dùng";

        // Tự động lấy chữ cái đầu tiên của Name làm Avatar (VD: "Liam Chen" -> "L")
        public string AvatarInitials => string.IsNullOrEmpty(Name) ? "U" : Name.Substring(0, 1).ToUpper();

        public bool IsOnline { get; set; }

        public string LastMessage { get; set; }

        // Tính toán thời gian (VD: "2h ago", "Vừa xong")
        public string TimeAgo
        {
            get
            {
                if (!LastMessageTime.HasValue) return "";
                var span = DateTime.Now - LastMessageTime.Value;
                if (span.TotalMinutes < 1) return "Vừa xong";
                if (span.TotalHours < 1) return $"{span.Minutes}m ago";
                if (span.TotalDays < 1) return $"{span.Hours}h ago";
                return LastMessageTime.Value.ToString("dd/MM");
            }
        }

        public int UnreadCount { get; set; }
        public bool HasUnread => UnreadCount > 0;
    }
}
