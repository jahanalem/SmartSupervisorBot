using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
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

        public async Task AddGroup(string groupName, string language)
        {
            if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Group name and language must not be null or empty.");
            }

            int maxGroupNameLength = 255;
            if (groupName.Length > maxGroupNameLength)
            {
                throw new ArgumentException($"Group name must not exceed {maxGroupNameLength} characters.");
            }

            await _groupAccess.AddGroupAsync(groupName, language);
        }

        public async Task<bool> DeleteGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Group name must not be null or empty.");
            }

            return await _groupAccess.RemoveGroupAsync(groupName);
        }

        public async Task<bool> EditGroup(string oldGroupName, string newGroupName)
        {
            if (string.IsNullOrWhiteSpace(oldGroupName) || string.IsNullOrWhiteSpace(newGroupName))
            {
                throw new ArgumentException("Group names cannot be null or empty.");
            }

            return await _groupAccess.RenameGroupAsync(oldGroupName, newGroupName);
        }

        public async Task<bool> EditLanguage(string groupName, string language)
        {
            if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Group name and language cannot be null or empty.");
            }

            return await _groupAccess.SetGroupLanguageAsync(groupName, language);
        }

        public async Task<List<(string GroupName, string Language)>> ListGroups()
        {
            return await _groupAccess.ListAllGroupsWithLanguagesAsync();
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
                        _logger.LogInformation("Your sentence has less than four words, that's why the bot doesn't react to it.");
                        return;
                    }

                    var chatId = update.Message?.Chat.Id ?? 0;
                    var messageId = update.Message?.MessageId ?? 0;
                    if (chatId == 0)
                    {
                        return;
                    }


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
                    var groupName = GetGroupName(update.Message);
                    var completionRequest = await GetCompletionRequest(language, messageText, groupName);

                    var chatGptResponse = await _api.Completions.CreateCompletionAsync(completionRequest);

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
            var groupName = GetGroupName(message);
            return await _groupAccess.GroupExistsAsync(groupName);
        }

        private string GetGroupName(Message message)
        {
            return message.Chat.Username ?? message.Chat.Title;
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

        private async Task<CompletionRequest> GetCompletionRequest(string language, string messageText, string groupName)
        {
            IBaseOpenAiTextSettings settings;
            string prompt;
            var languageGroup = await _groupAccess.GetGroupLanguageAsync(groupName);
            var languageToTranslate = languageGroup ?? _botConfigurationOptions.TranslateTheTextTo;
            if (language.StartsWith(languageToTranslate, StringComparison.OrdinalIgnoreCase))
            {
                settings = _botConfigurationOptions.TextCorrectionSettings;
                prompt = settings.Prompt;
            }
            else
            {
                settings = _botConfigurationOptions.TextTranslateSettings;
                prompt = FormatPrompt(languageToTranslate, settings.Prompt);
            }

            return new OpenAI_API.Completions.CompletionRequest
            {
                Prompt = $"{prompt} '{messageText}'",
                Model = settings.Model,
                MaxTokens = settings.MaxTokens,
                Temperature = settings.Temperature
            };
        }

        private string FormatPrompt(string language, string currentPrompt)
        {
            return currentPrompt.Replace("{language}", language);
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

            _logger.LogError($"Ein anderer Fehler ist aufgetreten. Bitte überprüfen Sie die Details.{exception.StackTrace}");

            return Task.CompletedTask;
        }

        private async Task<string> DetectLanguageAsync(string text)
        {
            var response = await _api.Completions.CreateCompletionAsync(new OpenAI_API.Completions.CompletionRequest
            {
                Prompt = $"{_botConfigurationOptions.LanguageDetectionSettings.Prompt} '{text}'",
                Model = _botConfigurationOptions.LanguageDetectionSettings.Model,
                MaxTokens = _botConfigurationOptions.LanguageDetectionSettings.MaxTokens,
                Temperature = _botConfigurationOptions.LanguageDetectionSettings.Temperature
            });

            return response.ToString().Trim();
        }
    }
}