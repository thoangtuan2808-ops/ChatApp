using ChatApp.Frontend.Models;
using ChatApp.Frontend.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ChatApp.Frontend.Views;

// 1. CHAT MODEL (Đã bổ sung đầy đủ thuộc tính để Mapping)
public class ChatModel
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; }
    public bool IsGroup { get; set; }
    public string OtherUserName { get; set; }
    public DateTime? LastMessageTime { get; set; }

    public string Name => IsGroup ? Title : OtherUserName ?? "Người dùng";
    public string AvatarInitials => string.IsNullOrEmpty(Name) ? "U" : Name.Substring(0, 1).ToUpper();

    public string LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public bool HasUnread => UnreadCount > 0;

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
}

public partial class ChatsPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserStateService _userState;

    private string _currentAvatar = "dotnet_bot.png";
    public string CurrentAvatar
    {
        get => _currentAvatar;
        set { _currentAvatar = value; OnPropertyChanged(); }
    }

    private string _currentUsername = "Đang tải...";
    public string CurrentUsername
    {
        get => _currentUsername;
        set { _currentUsername = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ChatModel> Chats { get; set; } = new();

    public ChatsPage(HttpClient httpClient, UserStateService userState)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _userState = userState;

        BindingContext = this;

        if (!string.IsNullOrEmpty(_userState.Username))
        {
            CurrentUsername = _userState.Username;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserAvatarAsync();
        await LoadChatsAsync(); // Đã sửa lại thành gọi LoadChatsAsync
    }

    private async Task LoadUserAvatarAsync()
    {
        try
        {
            // Sửa _userState.token thành Token viết hoa (tùy thuộc vào class của bạn)
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _userState.token);

            var response = await _httpClient.GetAsync("api/user/profile");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiData = JsonSerializer.Deserialize<UserProfileDTO>(json, options);

                if (apiData != null)
                {
                    CurrentUsername = apiData.Username;

                    if (!string.IsNullOrEmpty(apiData.AvatarUrl))
                    {
                        CurrentAvatar = apiData.AvatarUrl;
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi mạng
        }
    }

    // 2. LƯỢC BỎ HÀM CŨ: Đã xóa hàm LoadConversationsAsync bị lỗi

    private async Task LoadChatsAsync()
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.token);
            var response = await _httpClient.GetAsync("api/chat/conversations");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var dtoList = JsonSerializer.Deserialize<List<ConversationDTO>>(json, options);

                if (dtoList != null)
                {
                    Chats.Clear();

                    foreach (var dto in dtoList)
                    {
                        var chatModel = new ChatModel
                        {
                            ConversationId = dto.ConversationId,
                            Title = dto.Title,
                            IsGroup = dto.IsGroup,
                            OtherUserName = dto.OtherUser?.Username,
                            IsOnline = dto.OtherUser?.IsOnline ?? false,

                            LastMessage = string.IsNullOrEmpty(dto.LastMessageContent) ? "Bắt đầu cuộc trò chuyện..." : dto.LastMessageContent,
                            LastMessageTime = dto.LastMessageCreatedAt,
                            UnreadCount = 0
                        };

                        Chats.Add(chatModel);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi tải danh sách Chat: {ex.Message}");
        }
    }

    private async void OnChatTapped(object sender, TappedEventArgs e)
    {
        // "sender" ở đây chính là thẻ Border chứa dòng chat mà bạn vừa nhấn.
        // Lấy BindingContext của nó để ép kiểu về ChatModel.
        if (e.Parameter is ChatModel selectedChat)
        {
            // 1. Lấy trang ChatRoomPage từ hệ thống DI
            var chatRoomPage = Handler.MauiContext.Services.GetRequiredService<ChatRoomPage>();

            // 2. Truyền ID và Thông tin cơ bản sang trang ChatRoom
            await chatRoomPage.Initialize(selectedChat.ConversationId, selectedChat.Name, selectedChat.IsOnline);

            // 3. Chuyển giao diện sang phòng chat
            this.Window.Page = chatRoomPage;
            
        }
    }

    private void OnProfileTapped(object sender, EventArgs e)
    {
        var profilePage = Handler.MauiContext.Services.GetRequiredService<ChatApp.Frontend.Views.ProfilePage>();
        this.Window.Page = profilePage;
    }
}