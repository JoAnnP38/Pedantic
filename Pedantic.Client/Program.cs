using Microsoft.Extensions.Configuration;
using System.Runtime;

namespace Pedantic.Client
{
    internal static class Program
    {
        public static IConfiguration? Configuration;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
            AppSettings = Configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();
            ApplicationConfiguration.Initialize();
            
            Application.Run(new EvolutionForm());
        }

        public static AppSettings AppSettings { get; private set; } = new();
    }
}