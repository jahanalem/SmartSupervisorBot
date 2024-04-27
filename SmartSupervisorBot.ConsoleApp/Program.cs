using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;

namespace SmartSupervisorBot.ConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
              .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: true)
              .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{environmentName}.json"), optional: true)
              .AddEnvironmentVariables();

            IConfiguration configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var botService = serviceProvider.GetRequiredService<BotService>();
                botService.StartReceivingMessages();
                Console.WriteLine("Press any key to stop the bot...");
                Console.ReadKey();
            }
        }

        static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BotConfigurationOptions>(configuration.GetSection("BotConfiguration"));
            services.AddSingleton<IConfiguration>(configuration);
            services.AddHttpClient();
            services.AddSingleton<BotService>(sp => new BotService(
                sp.GetRequiredService<IOptions<BotConfigurationOptions>>(),
                sp.GetRequiredService<IHttpClientFactory>()));
        }
    }
}