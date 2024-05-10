using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Completions;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using SmartSupervisorBot.Utilities;

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

        public async Task AddGroup(long groupId, GroupInfo groupInfo)
        {
            if (string.IsNullOrWhiteSpace(groupInfo.GroupName) || string.IsNullOrWhiteSpace(groupInfo.Language))
            {
                throw new ArgumentException("Group name and language must not be null or empty.");
            }

            int maxGroupNameLength = 255;
            if (groupInfo.GroupName.Length > maxGroupNameLength)
            {
                throw new ArgumentException($"Group name must not exceed {maxGroupNameLength} characters.");
            }

            await _groupAccess.AddGroupAsync(groupId, groupInfo);
        }

        public async Task<bool> DeleteGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group Id must not be null or empty.");
            }

            return await _groupAccess.RemoveGroupAsync(groupId);
        }

        public async Task<bool> EditLanguage(string groupId, string language)
        {
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Group Id and language cannot be null or empty.");
            }

            return await _groupAccess.SetGroupLanguageAsync(groupId, language);
        }

        public async Task<bool> ToggleGroupActive(string groupId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new ArgumentException("Group Id cannot be null or empty.");
            }

            return await _groupAccess.SetToggleGroupActive(groupId, isActive);
        }

        public async Task<List<(string GroupId, GroupInfo GroupInfo)>> ListGroups()
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
                if (update.Type == UpdateType.Message)
                {
                    switch (update.Message.Type)
                    {
                        case MessageType.Text:
                            await ProcessTextMessage(update, botClient, cancellationToken);
                            break;
                        case MessageType.ChatTitleChanged:
                            await UpdateGroupNameAsync(update);
                            break;
                    }
                }
                else if (update.Type == UpdateType.MyChatMember && update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Member)
                {
                    await HandleNewChatMember(update);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(botClient, ex, cancellationToken);
            }
        }

        private async Task ProcessTextMessage(Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var messageText = update.Message.Text;
            var countWords = messageText.CountWords();
            if (!messageText.EndsWith("..") || countWords < 4 || countWords > 35)
            {
                return;
            }
            messageText = messageText.Remove(messageText.Length - 1);

            bool isValidGroup = await IsValidGroup(update.Message);
            if (!isValidGroup)
            {
                await InformInvalidGroupAsync(botClient, update, cancellationToken);
                return;
            }

            var language = await DetectLanguageAsync(messageText);
            if (!IsValidLanguageResponse(language))
            {
                return;
            }

            var groupId = update.Message.Chat.Id.ToString();
            var completionRequest = await GetCompletionRequest(language, messageText, groupId);

            var chatGptResponse = await _api.Completions.CreateCompletionAsync(completionRequest);

            string response = chatGptResponse.ToString().Replace("\n", "").Replace("\r", "").Trim('"', ' ');
            if (response != messageText)
            {
                await SendCorrectedTextAsync(botClient, update.Message, response, cancellationToken);
            }
            else
            {
                await SendReactionAsync(update.Message.Chat.Id, update.Message.MessageId, "🏆");
            }
        }

        private async Task HandleNewChatMember(Update update)
        {
            var groupId = update.MyChatMember.Chat.Id;
            var groupName = update.MyChatMember.Chat.Title;
            var groupInfo = new GroupInfo { GroupName = groupName };
            await AddGroup(groupId, groupInfo);
            Console.WriteLine($"Bot was added to the group: {groupName} (ID: {groupId})");
        }

        private async Task UpdateGroupNameAsync(Update update)
        {
            var newGroupName = update.Message.NewChatTitle;
            if (!string.IsNullOrEmpty(newGroupName))
            {
                var groupId = update.Message.Chat.Id;
                await _groupAccess.UpdateGroupNameAsync(groupId.ToString(), newGroupName);
                _logger.LogInformation($"Group name updated to '{newGroupName}' for group ID: {groupId}");
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
            var groupId = message.Chat.Id.ToString();
            return await _groupAccess.IsActivatedGroup(groupId);
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

        private async Task<CompletionRequest> GetCompletionRequest(string language, string messageText, string groupId)
        {
            IBaseOpenAiTextSettings settings;
            string prompt;
            var languageGroup = await _groupAccess.GetGroupLanguageAsync(groupId);
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
            var correctedText = $"<i>{writer}</i> 🗨️: <blockquote><i>{response}</i></blockquote>";

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