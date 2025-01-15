using Microsoft.Extensions.Options;
using Serilog;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.TextProcessing;

namespace SmartSupervisorBot.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            // Configure services
            ConfigureServices(builder);

            // Build the app
            var app = builder.Build();

            // Configure middleware
            ConfigureMiddleware(app);

            // Start the bot service
            StartBotService(app);

            // Run the application
            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Configure Bot options
            builder.Services.Configure<BotConfigurationOptions>(builder.Configuration.GetSection("BotConfiguration"));

            // Add Redis-based group access
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            builder.Services.AddSingleton<IGroupAccess, RedisGroupAccess>(sp => new RedisGroupAccess(redisConnectionString));

            // Add HTTP client
            builder.Services.AddHttpClient();

            // Configure logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            });

            // Add text processing service
            builder.Services.AddSingleton<ITextProcessingService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<BotConfigurationOptions>>();
                var groupAccess = provider.GetRequiredService<IGroupAccess>();

                return options.Value.OpenAiModel switch
                {
                    "gpt-4o-mini" => new OpenAiChatProcessingService(options.Value.BotSettings.OpenAiToken, groupAccess),
                    _ => new OpenAiCompletionsService(options.Value.BotSettings.OpenAiToken, groupAccess)
                };
            });

            // Add BotService
            builder.Services.AddSingleton<BotService>();

            // Configure API documentation (Swagger)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Smart Supervisor Bot API",
                    Version = "v1"
                });
            });

            // Configure route options
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-9.0#route-constraint-reference
            // https://nemi-chand.github.io/creating-custom-routing-constraint-in-aspnet-core-mvc/?utm_source=chatgpt.com
            builder.Services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap["regex"] = typeof(Microsoft.AspNetCore.Routing.Constraints.RegexRouteConstraint);
            });
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        private static void StartBotService(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var botService = scope.ServiceProvider.GetRequiredService<BotService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                botService.StartReceivingMessages();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while starting the bot.");
            }
        }
    }
}
