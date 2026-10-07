using ChatApp.Frontend.Views;
namespace ChatApp.Frontend
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = _serviceProvider.GetRequiredService<Views.LoginPage>();
            var window = new Window(loginPage)
            {
                Title = "Chat App" // Cài đặt tiêu đề cho cửa sổ trên máy tính
            };
            return window;
        }
    }
}