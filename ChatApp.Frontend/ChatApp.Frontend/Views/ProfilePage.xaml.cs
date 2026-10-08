using ChatApp.Frontend.Models;
using ChatApp.Frontend.Services;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ChatApp.Frontend.Views;

public class ProfileViewModel : INotifyPropertyChanged
{
    private string _avatarUrl;
    private string _displayName;
    private string _username;
    private string _bio;
    private bool _isOnline;
    private string _userId;
    private DateTime _createdAt;

    public string AvatarUrl { get => _avatarUrl; set { _avatarUrl = value; OnPropertyChanged(); } }
    public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }
    public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
    public string Bio { get => _bio; set { _bio = value; OnPropertyChanged(); } }
    public bool IsOnline { get => _isOnline; set { _isOnline = value; OnPropertyChanged();  } }
    public string UserId { get => _userId; set { _userId = value; OnPropertyChanged(); } }
    public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(MemberSince)); } }

    public string MemberSince => $"Thành viên từ: {CreatedAt:MM/yyyy}";

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class ProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserStateService _userState;
    public ProfileViewModel ViewModel { get; set; }

    public ProfilePage(HttpClient httpClient, UserStateService userState)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _userState = userState;

        // Mock data mapping cho các trường dữ liệu yêu cầu
        ViewModel = new ProfileViewModel
        {
            UserId = "USR-7829-AURA",
            Username = "@alex_neon26",
            DisplayName = "Alex Nguyen",
            Bio = "Đang code dở dự án... Xin đừng làm phiền 🚀",
            AvatarUrl = "dotnet_bot.png", // Demo ảnh local
            IsOnline = true,
            CreatedAt = new DateTime()
        };

        BindingContext = ViewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await FetchUserProfileAsync();
    }

    private async Task FetchUserProfileAsync()
    {
        try
        {
            // 1. Gắn Token vào Header để Backend biết bạn là ai
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _userState.token);

            // 2. Gọi API
            var response = await _httpClient.GetAsync("api/user/profile"); // Đường dẫn API Backend

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Giả sử class DTO của Backend trả về có cấu trúc tương ứng
                var apiData = JsonSerializer.Deserialize<UserProfileDTO>(json, options);

                if (apiData != null)
                {
                    // 3. Đổ dữ liệu thật vào ViewModel (Giao diện sẽ tự động đổi text nhờ INotifyPropertyChanged)
                    ViewModel.UserId = apiData.UserId.ToString();
                    ViewModel.Username = apiData.Username;
                    ViewModel.DisplayName = apiData.Username; // Tạm dùng Username làm DisplayName
                    ViewModel.IsOnline = apiData.IsOnline;
                    ViewModel.CreatedAt = apiData.CreatedAt;
                    ViewModel.Bio = "Sẵn sàng kết nối! 🚀"; // Có thể làm tính năng đổi Bio sau

                    if (!string.IsNullOrEmpty(apiData.AvatarUrl))
                    {
                        ViewModel.AvatarUrl = apiData.AvatarUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi mạng", "Không thể tải thông tin. " + ex.Message, "OK");
        }
    }

    // Hàm xử lý khi bấm nút Đăng xuất
    private void OnLogoutTapped(object sender, TappedEventArgs e)
    {
        // 1. Xóa trạng thái Token
        _userState.token = string.Empty;
        _userState.UserId = Guid.Empty;

        // 2. Chuyển về màn hình đăng nhập (Dùng Window để tránh lỗi gán MainPage)
        var loginPage = Handler.MauiContext.Services.GetRequiredService<LoginPage>();
        this.Window.Page = loginPage;
    }
    private void OnBackButtonClicked(object sender, EventArgs e)
    {
        // Lấy lại màn hình Main (Chats) thông qua Dependency Injection
        var mainPage = Handler.MauiContext.Services.GetRequiredService<ChatApp.Frontend.Views.ChatsPage>();

        // Chuyển trang (Thay thế màn hình Profile hiện tại bằng màn hình Chats)
        this.Window.Page = mainPage;
    }
}
