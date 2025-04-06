using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using QuestShare.Common;
using QuestShare.Server.Hubs;
using QuestShare.Server.Managers;
using QuestShare.Server.Models;

namespace QuestShare.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
#if DEBUG
                options.EnableDetailedErrors = true;
#endif
            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseWebSockets();
#if DEBUG
            app.UseDeveloperExceptionPage();
#endif
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ShareHub>("/Hub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                });

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("QuestShare Server");
                });

                endpoints.MapGet("/version", async context =>
                {
                    await context.Response.WriteAsync(Constants.Version);
                });

                endpoints.MapGet("/status", async context =>
                {
                    // simulate database query
                    try
                    {
                        var db = new QuestShareContext();
                        var count = db.Clients.Count();
                        await context.Response.WriteAsync($"OK");
                    }
                    catch (Exception e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"ERROR");
                    }

                });

                endpoints.MapGet("/clients", async context =>
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        ConnectedClients = ShareHub.ConnectedClients,
                        ActiveSessions = await SessionManager.GetSessionCount(),
                    });
                });
            });
        }
    }
}
