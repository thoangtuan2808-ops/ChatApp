using ChatApp.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace ChatApp.Backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatAppDbContext _context;
        public ChatHub(ChatAppDbContext context)
        {
            _context = context;
        }
        public override async Task OnConnectedAsync()
        {
            var userIdString = Context.UserIdentifier;
            if(Guid.TryParse(userIdString, out Guid userId))
            {
                var conversationIds = await _context.ConversationParticipants.Where(cp => cp.UserId == userId).Select(cp => cp.ConversationId).ToListAsync();
                foreach(var conversationId in conversationIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
                }
            }
            await base.OnConnectedAsync();
        }
        public async Task SendMassage(Guid conversationId, string content)
        {
            var userId = Guid.Parse(Context.UserIdentifier);
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content,
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            await Clients.Groups(conversationId.ToString()).SendAsync("ReceiveMessage", message);
        }
    }
}
