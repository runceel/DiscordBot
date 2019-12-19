using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.HostApp
{
    class Program
    {
        static void Main(string[] args) => ConfigureHostBuider(args).Build().Run();

        public static IHostBuilder ConfigureHostBuider(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, b) =>
            {
                b.ClearProviders();
                if (ctx.HostingEnvironment.IsProduction())
                {
                    b.AddApplicationInsights(ctx.Configuration.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY"));
                }
                else
                {
                    b.AddConsole();
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DiscordBotService>();
            });
    }

    class DiscordBotService : BackgroundService
    {
        private DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiscordBotService> _logger;

        private string Token => _configuration.GetValue<string>("Token");
        public DiscordBotService(IConfiguration configuration, ILogger<DiscordBotService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig { LogLevel = Discord.LogSeverity.Info });
            _client.Log += x =>
            {
                _logger.LogInformation($"{x.Message}, {x.Exception}"); // とりあえず何も考えずに information で
                return Task.CompletedTask;
            };
            _client.MessageReceived += MessageReceived;
            await _client.LoginAsync(Discord.TokenType.Bot, Token);
            await _client.StartAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage m) || m.Author.IsBot)
            {
                return;
            }
            _logger.LogInformation($"{m.Content} というメッセージを受信しました。");
            await m.Channel.SendMessageAsync($"{m.Content} と言いましたね。");
        }
    };
}
