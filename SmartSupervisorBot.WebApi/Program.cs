using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.Model.Dtos;
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

            // Map endpoints
            ConfigureEndpoints(app);

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
            builder.Services.AddSingleton<IBotService, BotService>();

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

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, OpenAiJsonSerializerContext.Default);
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
            var botService = scope.ServiceProvider.GetRequiredService<IBotService>();
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

        private static void ConfigureEndpoints(WebApplication app)
        {
            app.MapPost("/groups/add/", async ([FromServices] IBotService botService, long groupId, [FromBody] GroupInfo groupInfo) =>
            {
                await botService.AddGroup(groupId, groupInfo);
                return Results.Ok(new SuccessResponse { Message = "Group added successfully." });
            });

            app.MapDelete("/groups/{groupId}", async ([FromServices] IBotService botService, string groupId) =>
            {
                var result = await botService.DeleteGroup(groupId);
                return result ? Results.Ok(new SuccessResponse { Message = "Group deleted successfully." })
                              : Results.NotFound(new ErrorResponse { Error = "Group not found." });
            });

            app.MapPut("/groups/{groupId}/language", async ([FromServices] IBotService botService, string groupId, string language) =>
            {
                var result = await botService.EditLanguage(groupId, language);
                return result ? Results.Ok(new SuccessResponse { Message = "Language updated successfully." })
                              : Results.BadRequest(new ErrorResponse { Error = "Failed to update language." });
            });

            app.MapGet("/groups", async ([FromServices] IBotService botService) =>
            {
                var groups = await botService.ListGroups();
                return Results.Ok(groups);
            });

            app.MapPatch("/groups/{groupId}/active", async ([FromServices] IBotService botService, string groupId, bool isActive) =>
            {
                var result = await botService.ToggleGroupActive(groupId, isActive);
                return result ? Results.Ok(new SuccessResponse { Message = $"Group {(isActive ? "activated" : "deactivated")} successfully." })
                              : Results.BadRequest(new ErrorResponse { Error = "Failed to update group status." });
            });

            app.MapPost("/groups/{groupId}/credit", async ([FromServices] IBotService botService, string groupId, decimal creditAmount) =>
            {
                await botService.AddCreditToGroupAsync(groupId, creditAmount);
                return Results.Ok(new SuccessResponse { Message = "Credit added successfully." });
            });
        }
    }
}
