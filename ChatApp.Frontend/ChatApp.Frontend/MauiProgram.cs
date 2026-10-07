using ChatApp.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace ChatApp.Frontend
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            //dang ki httpClient để gọi api
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5255/")
            });
            //dang ki kho luu tru token
            builder.Services.AddSingleton<UserStateService>();
            builder.Services.AddTransient<Views.LoginPage>();
            builder.Services.AddTransient<Views.ChatsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
