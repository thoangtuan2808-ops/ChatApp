using System;
using System.Collections.Generic;

namespace ChatApp.Backend.Models;

public partial class Conversation
{
    public Guid ConversationId { get; set; }

    public string? Title { get; set; }

    public bool? IsGroup { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
