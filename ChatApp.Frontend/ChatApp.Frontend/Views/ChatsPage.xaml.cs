using System.Collections.ObjectModel;

namespace ChatApp.Frontend.Views;

public class ChatModel
{
    public string Name { get; set; }
    public string LastMessage { get; set; }
    public string TimeAgo { get; set; }
    public string AvatarInitials { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public bool HasUnread => UnreadCount > 0;
}

public partial class ChatsPage : ContentPage
{
    public ObservableCollection<ChatModel> Chats { get; set; }

	public ChatsPage()
	{
		InitializeComponent();
        Chats = new ObservableCollection<ChatModel>
        {
            new ChatModel { Name = "AI Assistant (AURA)", LastMessage = "Good morning! Looking for flights to Tokyo? ✈️", TimeAgo = "Online", AvatarInitials = "AI", IsOnline = true },
            new ChatModel { Name = "Liam Chen", LastMessage = "Yeah, that works for the prototype sync.", TimeAgo = "2h ago", AvatarInitials = "LC", UnreadCount = 3 },
            new ChatModel { Name = "Maya Sharma", LastMessage = "Hey, can you review the designs? ✨", TimeAgo = "10m ago", AvatarInitials = "MS", UnreadCount = 1, IsOnline = true },
            new ChatModel { Name = "Design Team", LastMessage = "Ethan: Let's discuss the final mockups in 10.", TimeAgo = "4h ago", AvatarInitials = "DT", UnreadCount = 2 }
        };
        BindingContext = this;
	}
}