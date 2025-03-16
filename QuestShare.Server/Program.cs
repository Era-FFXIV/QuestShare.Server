global using QuestShare.Common.API;
global using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using QuestShare.Common;
using QuestShare.Server.Models;
using System.Diagnostics;
using System.Net;

namespace QuestShare.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Logger = log;
            var builder = new HostBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(IPAddress.Any, 8080);
                        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                        {
                            httpsOptions.ServerCertificate = null;
                            httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                }).ConfigureServices(services =>
                {
                    services.AddRateLimiter(_ => _.AddFixedWindowLimiter(policyName: "ClientPolicy", options =>
                    {
                        options.PermitLimit = 5;
                        options.Window = TimeSpan.FromSeconds(15);
                    }));
                    services.AddRateLimiter(_ => _.AddSlidingWindowLimiter(policyName: "UpdatePolicy", options =>
                    {
                        options.PermitLimit = 5;
                        options.Window = TimeSpan.FromSeconds(5);
                    }));
                    services.AddRateLimiter(_ => _.AddSlidingWindowLimiter(policyName: "PartyCheckPolicy", options =>
                    {
                        options.PermitLimit = 1;
                        options.Window = TimeSpan.FromSeconds(30);
                    }));
                }).ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
                    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Information);
                }).UseSerilog();
            var database = Environment.GetEnvironmentVariable("QUESTSHARE_DATABASE");
            if (string.IsNullOrEmpty(database))
            {
                Console.WriteLine("Please set the QUESTSHARE_DATABASE environment variable.");
                Environment.Exit(-1);
            }
            var app = builder.Build();
            Log.Information($"Starting QuestShare Server - API Version {Constants.Version}");
            app.Run();
        }
    }
}
