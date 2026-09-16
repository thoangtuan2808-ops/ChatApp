using ChatApp.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ChatApp.Backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatAppDbContext _context;
        public ChatController(ChatAppDbContext context)
        {
            _context = context;
        }
        //hàm hỗ trợ lấy userid của người đang gửi request
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.Parse(userIdString);
        }
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetCurrentUserId();
            var conversations = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => new
                {
                    cp.Conversation.ConversationId,
                    cp.Conversation.Title,
                    cp.Conversation.IsGroup,
                    // Xử lý UI: Nếu là chat 1-1, lấy thông tin người kia (avatar, tên, online)
                    OtherUser = cp.Conversation.IsGroup == true ? null :
                        _context.ConversationParticipants
                            .Where(other => other.ConversationId == cp.ConversationId && other.UserId != userId)
                            .Select(other => new
                        {
                            other.User.UserId,
                            other.User.Username,
                            other.User.AvatarUrl,
                            other.User.IsOnline,
                        })
                    .FirstOrDefault()
                })
                .ToListAsync();
            return Ok(conversations);
        }
        [HttpGet("conversations/{conversatonId}/messages")]
        public async Task<IActionResult> GetMassages(Guid conversatonId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            // BẢO MẬT: Kiểm tra xem user hiện tại có nằm trong phòng chat này không?
            // (Chống hack: Lấy token của mình nhưng truyền ID phòng chat của người khác)
            var isParticipant = await _context.ConversationParticipants.
                AnyAsync(cp => cp.ConversationId == conversatonId && cp.UserId == userId);
            if (!isParticipant)
            {
                return Forbid("Bạn không có quyền xem tin nhắn của phòng chat này!");
            }
            // Truy vấn lấy tin nhắn (Dùng order by giảm dần theo thời gian)
            // Nhờ dòng "CREATE NONCLUSTERED INDEX" hôm qua, truy vấn này sẽ chạy tính bằng mili-giây
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversatonId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(page - 1)
                .Take(pageSize)
                .Select(m => new
                {
                    m.MessageId,
                    m.SenderId,
                    m.Content,
                    m.IsRead,
                    m.CreatedAt
                }).ToListAsync();
            return Ok(messages);
        }
    } 
}
