using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;

namespace SmartSupervisorBot.ConsoleApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = BuildConfiguration();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var botService = serviceProvider.GetRequiredService<BotService>();

                var receivingTask = Task.Run(() => botService.StartReceivingMessages());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("The bot is now active. Listening for commands...");
                Console.ResetColor();

                await ManageUserInterface(botService);

                await receivingTask;
            }
        }

        static IConfiguration BuildConfiguration()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BotConfigurationOptions>(configuration.GetSection("BotConfiguration"));
            services.AddSingleton<IConfiguration>(configuration);
            services.AddHttpClient();
            services.AddSingleton<BotService>(sp => new BotService(
                sp.GetRequiredService<IOptions<BotConfigurationOptions>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IGroupAccess>(), sp.GetRequiredService<ILogger<BotService>>()));

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                //builder.AddDebug();
                //builder.AddConsole();
            });

            services.AddSingleton<IGroupAccess, RedisGroupAccess>(sp => new RedisGroupAccess("localhost,abortConnect=false"));
        }

        static async Task ManageUserInterface(BotService botService)
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Enter command (add, delete, edit, setlang, list, exit):");
                Console.ForegroundColor = ConsoleColor.White;
                var command = Console.ReadLine();
                switch (command.ToLower())
                {
                    case "add":
                        await ExecuteAddGroup(botService);
                        break;
                    case "delete":
                        await ExecuteDeleteGroup(botService);
                        break;
                    case "edit":
                        await ExecuteEditGroup(botService);
                        break;
                    case "setlang":
                        await ExecuteSetLanguage(botService);
                        break;
                    case "list":
                        await ExecuteListGroups(botService);
                        break;
                    case "exit":
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("Stopping bot and exiting...");
                        Console.ResetColor();
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command, please try again.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        static async Task ExecuteAddGroup(BotService botService)
        {
            Console.WriteLine("Enter a group name to add:");
            var groupName = Console.ReadLine();

            Console.WriteLine("Enter a language to add:");
            var language = Console.ReadLine();

            await botService.AddGroup(groupName, language.Trim());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Group added successfully.");
            Console.ResetColor();
        }

        static async Task ExecuteDeleteGroup(BotService botService)
        {
            Console.WriteLine("Enter a group name to delete:");
            var groupName = Console.ReadLine();
            var isDeleted = await botService.DeleteGroup(groupName.Trim());
            if (isDeleted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Group deleted successfully.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There is no group with this name.");
                Console.ResetColor();
            }

        }

        static async Task ExecuteEditGroup(BotService botService)
        {
            Console.WriteLine("Enter the current group name:");
            var currentName = Console.ReadLine();
            Console.WriteLine("Enter the new group name:");
            var newName = Console.ReadLine();

            var renamed = await botService.EditGroup(currentName.Trim(), newName.Trim());
            if (renamed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Group renamed successfully.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to rename group or group not found.");
            }
            Console.ResetColor();
        }

        static async Task ExecuteSetLanguage(BotService botService)
        {
            Console.WriteLine("Enter the group name:");
            var groupName = Console.ReadLine();
            Console.WriteLine("Enter the new language for the group (English, Deutsch, Persisch, Spanisch, Französisch, Arabisch):");
            var languageInput = Console.ReadLine().Trim();

            var supportedLanguagesa = new List<string> { "English", "Deutsch", "Persisch", "Spanisch", "Französisch", "Arabisch" };

            var supportedLanguages = new Dictionary<string, string>
            {
                {"english", "English"},
                {"deutsch", "Deutsch"},
                {"persisch", "Persisch"},
                {"spanisch", "Spanisch"},
                {"französisch", "Französisch"}
            };

            // Check if the entered language is supported
            if (!supportedLanguages.TryGetValue(languageInput.ToLower(), out var normalizedLanguage))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid language entered. Please enter a valid language from the supported list.");
                Console.ResetColor();
                return;
            }

            var languageSet = await botService.EditLanguage(groupName.Trim(), normalizedLanguage);
            if (languageSet)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Language updated successfully for the group.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to update language or group not found.");
            }
            Console.ResetColor();
        }

        static async Task ExecuteListGroups(BotService botService)
        {
            var groups = await botService.ListGroups();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("List of groups with their languages:");
            foreach (var group in groups)
            {
                Console.WriteLine($"Group: {group.GroupName}, Language: {group.Language}");
            }
            Console.ResetColor();
        }
    }
}