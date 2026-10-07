using ChatApp.Frontend.Services;
using System.Text;
using System.Text.Json;

namespace ChatApp.Frontend.Views {
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userState;
        public LoginPage(HttpClient httpClient, UserStateService userStateService)
        {
            InitializeComponent();
            _httpClient = httpClient;
            _userState = userStateService;

            // Gắn sự kiện click cho nút
            LoginButton.Clicked += OnLoginButtonClicked;
        }
        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            // 1. Lấy dữ liệu từ giao diện
            var username = EmailEntry.Text; // Dùng ô Email làm Username
            var password = PasswordEntry.Text;
            // 2. Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
                return;
            }
            // 3. Hiệu ứng UX: Đổi chữ nút và khóa lại để tránh user bấm nhiều lần
            LoginButton.IsEnabled = false;
            LoginButton.Text = "Đang xử lý...";
            try
            {
                // 4. Đóng gói dữ liệu thành JSON
                var loginData = new { Username = username, Password = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 5. Bắn API Post đến Server
                var response = await _httpClient.PostAsync("api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // 6. Chuyển JSON thành Object (tắt phân biệt hoa thường)
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<UserStateService>(responseString, options);

                    // 7. Cất Token vào State để các màn hình khác dùng
                    _userState.token = result.token;
                    _userState.UserId = result.UserId;
                    _userState.Username = result.Username;

                    var chatPage = Handler.MauiContext.Services.GetRequiredService<ChatsPage>();
                    // 8. Chuyển sang màn hình Chat
                    this.Window.Page = chatPage;
                }
                else
                {
                    await DisplayAlert("Thất bại", "Sai tên đăng nhập hoặc mật khẩu!", "Thử lại");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi mạng", "Không thể kết nối đến Server. " + ex.Message, "OK");
            }
            finally
            {
                // 9. Khôi phục lại trạng thái nút
                LoginButton.IsEnabled = true;
                LoginButton.Text = "Đăng nhập";
            }
        }
    }
}

