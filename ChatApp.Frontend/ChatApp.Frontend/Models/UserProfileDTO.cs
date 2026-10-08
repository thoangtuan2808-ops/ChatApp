using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Frontend.Models
{
    public class UserProfileDTO
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }

        // --- UI Helpers (Giúp XAML hiển thị đẹp hơn mà không cần code logic) ---
        public string DisplayAvatar => string.IsNullOrEmpty(AvatarUrl) ? "dotnet_bot.png" : AvatarUrl;
        public string StatusColor => IsOnline ? "#22c55e" : "#64748b"; // Xanh lá nếu online, Xám nếu offline
        public string StatusText => IsOnline ? "Đang hoạt động" : "Ngoại tuyến";
        public string JoinedDateText => $"Tham gia từ: {CreatedAt:MM/yyyy}";
    }
}
