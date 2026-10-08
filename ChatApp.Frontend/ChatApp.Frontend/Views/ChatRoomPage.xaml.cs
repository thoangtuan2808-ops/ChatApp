using ChatApp.Frontend.Models;
using ChatApp.Frontend.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ChatApp.Frontend.Views;

public class MessageModel
{
    public string Text { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public bool IsMine { get; set; }
}

public class ChatRoomViewModel : INotifyPropertyChanged
{
    public ObservableCollection<MessageModel> Messages { get; set; } = new();

    private string _chatName = string.Empty;
    public string ChatName { get => _chatName; set { _chatName = value; OnPropertyChanged(); } }

    private bool _isOnline;
    public bool IsOnline { get => _isOnline; set { _isOnline = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class ChatRoomPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserStateService _userState;
    private Guid _conversationId;
    private HubConnection _hubConnection;
    public ChatRoomViewModel ViewModel { get; set; }

    public ChatRoomPage(HttpClient httpClient, UserStateService userState)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _userState = userState;

        // Mock data cho phòng chat
        ViewModel = new ChatRoomViewModel();
        BindingContext = ViewModel;
    }
    public async Task Initialize(Guid conversationId, string chatName, bool isOnline)
    {
        _conversationId = conversationId;

        // Cập nhật Header
        ViewModel.ChatName = chatName;
        ViewModel.IsOnline = isOnline;

        // Xóa sạch tin nhắn cũ/mock data mỗi khi vào phòng mới
        ViewModel.Messages.Clear();

        await LoadMessagesAsync();
        await SetupSignalRAsync();
    }
    private async Task SetupSignalRAsync()
    {
        try
        {
            // URL của Backend (Thay đổi tùy theo thiết bị chạy MAUI: Windows dùng localhost, Android Emulator dùng 10.0.2.2)
            // Lấy từ BaseAddress của HttpClient cho đồng bộ
            var backendUrl = $"{_httpClient.BaseAddress}chathub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(backendUrl, options =>
                {
                    // Truyền Token JWT vào mỗi request của SignalR
                    options.AccessTokenProvider = () => Task.FromResult(_userState.token);
                })
                .WithAutomaticReconnect() // Tự động kết nối lại nếu rớt mạng
                .Build();

            // LẮNG NGHE TIN NHẮN TỪ SERVER BẮN VỀ
            _hubConnection.On<MessageDTO>("ReceiveMessage", (message) =>
            {
                // Khi nhận tin nhắn mới, bắt buộc phải update UI trên MainThread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ViewModel.Messages.Add(new MessageModel
                    {
                        Text = message.Content,
                        Time = message.CreatedAt.ToString("HH:mm"),
                        IsMine = message.SenderId == _userState.UserId // So sánh ID để chia trái/phải
                    });

                    // Tự động cuộn xuống tin nhắn mới
                    MessagesListView.ScrollTo(ViewModel.Messages.Last(), position: ScrollToPosition.End, animate: true);
                });
            });

            // Khởi động kết nối
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi SignalR: {ex.Message}");
        }
    }
    private async Task LoadMessagesAsync()
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userState.token);

            // ⚠️ LƯU Ý: Nếu hôm trước ở Backend bạn chưa sửa lỗi đánh máy (conversatonId), 
            // thì hãy nhớ sửa lại đường dẫn URL này cho khớp nhé!
            var url = $"api/chat/conversations/{_conversationId}/messages";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"\n[DEBUG JSON TIN NHẮN]: {json}\n");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiMessages = JsonSerializer.Deserialize<List<MessageDTO>>(json, options);

                if (apiMessages != null && apiMessages.Any())
                {
                    // Vì Backend dùng OrderByDescending (mới nhất xếp đầu)
                    // Ta phải đảo ngược lại để hiển thị từ trên xuống dưới (cũ -> mới)
                    apiMessages.Reverse();

                    foreach (var msg in apiMessages)
                    {
                        ViewModel.Messages.Add(new MessageModel
                        {
                            Text = msg.Content,
                            Time = msg.CreatedAt.ToString("HH:mm"),

                            // Quyết định tin nhắn nằm bên Phải (True) hay Trái (False)
                            IsMine = (msg.SenderId == _userState.UserId)
                        });
                    }

                    // Cuộn mượt mà xuống tin nhắn cuối cùng
                    MessagesListView.ScrollTo(ViewModel.Messages.Last(), position: ScrollToPosition.End, animate: false);
                }
            }
            else
            {
                // Thêm khối else này để báo lỗi trực tiếp trên màn hình app
                var errorMsg = await response.Content.ReadAsStringAsync();
                await Dispatcher.DispatchAsync(() =>
                {
                    DisplayAlert("Lỗi API", $"Mã lỗi: {response.StatusCode}\n{errorMsg}", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi tải tin nhắn: {ex.Message}");
        }
    }
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
        // Chuyển về màn hình chính
        var mainPage = Handler.MauiContext.Services.GetRequiredService<ChatsPage>();
        this.Window.Page = mainPage;
    }

    private async void OnSendButtonClicked(object sender, EventArgs e)
    {
        var text = MessageInput.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        // Làm sạch ô input ngay lập tức để người dùng có thể gõ tiếp
        MessageInput.Text = string.Empty;

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try
            {
                // GỌI HÀM TRÊN BACKEND (Chú ý: Đảm bảo tên hàm trong ChatHub.cs của bạn là SendMessage hay SendMassage thì gọi cho đúng)
                await _hubConnection.InvokeAsync("SendMessage", _conversationId, text);

                // LƯU Ý QUAN TRỌNG: Chúng ta KHÔNG CẦN add MessageModel thủ công vào giao diện ở đây nữa.
                // Vì Backend sẽ broadcast (phát sóng) tin nhắn này tới toàn bộ phòng chat (bao gồm cả chính bạn).
                // Sự kiện _hubConnection.On("ReceiveMessage") ở trên sẽ tự động bắt được và vẽ lên màn hình.
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể gửi tin nhắn.", "OK");
            }
        }
        else
        {
            await DisplayAlert("Lỗi mạng", "Đang mất kết nối tới máy chủ.", "OK");
        }
    }
}
