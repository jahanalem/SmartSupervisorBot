using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartSupervisorBot.Core
{
    public class BotService : IBotService, IDisposable
    {
        private readonly ILogger<BotService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _botToken;
        private readonly OpenAIAPI _api;
        private readonly CancellationTokenSource _cts;
        private TelegramBotClient _botClient;
        private readonly BotConfigurationOptions _botConfigurationOptions;

        private readonly IGroupAccess _groupAccess;

        public BotService(IOptions<BotConfigurationOptions> botConfigurationOptions,
            IHttpClientFactory httpClientFactory,
            IGroupAccess groupAccess, ILogger<BotService> logger)
        {
            _botConfigurationOptions = botConfigurationOptions.Value;
            _httpClientFactory = httpClientFactory;
            _botToken = _botConfigurationOptions.BotSettings.BotToken;
            _api = new OpenAIAPI(_botConfigurationOptions.BotSettings.OpenAiToken);
            _cts = new CancellationTokenSource();
            _botClient = new TelegramBotClient(_botToken, _httpClientFactory.CreateClient());
            _logger = logger;
            _groupAccess = groupAccess;
        }


        public static UpdateType[] ConvertStringToUpdateType(string[] updateStrings)
        {
            return updateStrings.Select(update => Enum.Parse<UpdateType>(update, ignoreCase: true)).ToArray();
        }

        public void StartReceivingMessages()
        {
            string[] updateStrings = _botConfigurationOptions.AllowedUpdatesSettings?.AllowedUpdates ?? Array.Empty<string>();
            UpdateType[] updates = ConvertStringToUpdateType(updateStrings);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = updates
            };

            _logger.LogInformation("Starting message reception...");

            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, _cts.Token);

            _logger.LogInformation("Message reception started successfully.");
        }

        public async Task AddGroup(string groupName)
        {
            await _groupAccess.AddGroupAsync(groupName);
        }

        public async Task<bool> DeleteGroup(string groupName)
        {
            return await _groupAccess.RemoveGroupAsync(groupName);
        }

        public async Task EditGroup(string oldGroupName, string newGroupName)
        {
            bool deleteSuccess = await _groupAccess.RemoveGroupAsync(oldGroupName);
            if (!deleteSuccess)
            {
                Console.WriteLine("Failed to delete the old group.");
                return;
            }

            await _groupAccess.AddGroupAsync(newGroupName);
        }

        public async Task<List<string>> ListGroups()
        {
            return await _groupAccess.ListAllGroupsAsync();
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _httpClientFactory?.CreateClient().Dispose();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update?.Message?.Type == MessageType.Text)
                {
                    var messageText = update.Message?.Text?.Trim();
                    if (messageText?.Split(' ').Length < 4)
                    {
                        return;
                    }

                    var chatId = update.Message?.Chat.Id ?? 0;
                    var messageId = update.Message?.MessageId ?? 0;
                    if (chatId == 0)
                    {
                        return;
                    }
                    var groupUserName = update.Message.Chat.Username;

                    if (!(await IsValidGroup(update.Message)))
                    {
                        await InformInvalidGroupAsync(botClient, update, cancellationToken);

                        return;
                    }

                    if (await IsValidGroup(update.Message) &&
                        (update.Message.MessageThreadId != null &&
                         update.Message?.ReplyToMessage?.MessageThreadId != null))
                    {
                        return;
                    }

                    var language = await DetectLanguageAsync(messageText);
                    if (!IsValidLanguageResponse(language))
                    {
                        return;
                    }
                    var promptText = GetPromptText(language, messageText);

                    var chatGptResponse = await _api.Completions.CreateCompletionAsync(
                        new OpenAI_API.Completions.CompletionRequest
                        {
                            Prompt = promptText,
                            Model = _botConfigurationOptions.TextCorrectionSettings.Model,
                            MaxTokens = _botConfigurationOptions.TextCorrectionSettings.MaxTokens,
                            Temperature = _botConfigurationOptions.TextCorrectionSettings.Temperature
                        });

                    _logger.LogInformation($"Received a message in chat {chatId}: {messageText}");

                    string response = chatGptResponse.ToString().Replace("\"", "").Trim();
                    if (response != messageText)
                    {
                        await SendCorrectedTextAsync(botClient, update.Message, response, cancellationToken);
                    }
                    else
                    {
                        await SendReactionAsync(chatId, messageId, "🏆");
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(botClient, ex, cancellationToken);
            }
        }

        private bool IsValidLanguageResponse(string language)
        {
            var words = language.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var isValidLanguage = words.Length < 3;
            if (!isValidLanguage)
            {
                _logger.LogError("Invalid language detected.");
                return false;
            }
            return true;
        }

        private async Task<bool> IsValidGroup(Message message)
        {
            var groupName = message.Chat.Username ?? message.Chat.Title;
            return await _groupAccess.GroupExistsAsync(groupName);
        }

        private async Task InformInvalidGroupAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chatId = update.Message?.Chat.Id ?? 0;
            var messageId = update.Message?.MessageId ?? 0;

            var correctedText = $"<i> Dieser Roboter ist nur für bestimmte Gruppen aktiv. Bitte wenden Sie sich an den Bot-Hersteller: @Roohi_C </i>";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: correctedText,
                replyToMessageId: messageId,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task SendReactionAsync(long chatId, long messageId, string emoji)
        {
            var requestUrl = $"https://api.telegram.org/bot{_botToken}/setMessageReaction";
            var requestData = new
            {
                chat_id = chatId,
                message_id = messageId,
                reaction = new[]
                {
                    new { type = "emoji", emoji = emoji }
                },
                is_big = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            var response = await _httpClientFactory.CreateClient().PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to send reaction: {response.StatusCode}");
            }
        }

        private string GetPromptText(string language, string messageText)
        {
            return language.StartsWith("Deutsch") ?
                                $"{_botConfigurationOptions.TextCorrectionSettings.Prompt} '{messageText}'"
                                :
                                $"{_botConfigurationOptions.LanguageDetectionSettings.Prompt} '{messageText}'.";
        }

        private async Task SendCorrectedTextAsync(ITelegramBotClient botClient, Message message, string response, CancellationToken cancellationToken)
        {
            var chatId = message?.Chat.Id ?? 0;
            var messageId = message?.MessageId ?? 0;
            var writer = !string.IsNullOrEmpty(message?.From?.Username) ? $"@{message.From.Username}" : $"{message?.From?.FirstName} {message.From?.LastName}";
            var correctedText = $"<i> {writer} sagte</i>: <blockquote><i> {response} </i></blockquote>";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: correctedText,
                replyToMessageId: messageId,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogTrace($"An error occurred: {exception.Message}");

            if (exception.Message.Contains("message to reply not found"))
            {
                _logger.LogError("Die Nachricht, auf die geantwortet werden sollte, wurde nicht gefunden. Keine Aktion wird durchgeführt.");
                return Task.CompletedTask;
            }

            _logger.LogError("Ein anderer Fehler ist aufgetreten. Bitte überprüfen Sie die Details.");

            return Task.CompletedTask;
        }

        private async Task<string> DetectLanguageAsync(string text)
        {
            var response = await _api.Completions.CreateCompletionAsync(new OpenAI_API.Completions.CompletionRequest
            {
                Prompt = $"Identifizieren Sie die Sprache dieses Textes: '{text}'",
                Model = _botConfigurationOptions.LanguageDetectionSettings.Model,
                MaxTokens = _botConfigurationOptions.LanguageDetectionSettings.MaxTokens,
                Temperature = _botConfigurationOptions.LanguageDetectionSettings.Temperature
            });

            return response.ToString().Trim();
        }
    }
}