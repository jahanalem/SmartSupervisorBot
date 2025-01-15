using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing;
using SmartSupervisorBot.TextProcessing.Model;
using SmartSupervisorBot.Utilities;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartSupervisorBot.Core
{
    public class BotService : IBotService, IDisposable
    {
        private readonly BotConfigurationOptions _botConfigurationOptions;
        private readonly ITextProcessingService _textProcessingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CancellationTokenSource _cts;
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<BotService> _logger;
        private readonly IGroupAccess _groupAccess;
        private readonly string _botToken;

        public BotService(IOptions<BotConfigurationOptions> botConfigurationOptions,
            IHttpClientFactory httpClientFactory,
            IGroupAccess groupAccess, ILogger<BotService> logger,
            ITextProcessingService textProcessingService)
        {
            _botConfigurationOptions = botConfigurationOptions.Value;
            _botToken = _botConfigurationOptions.BotSettings.BotToken;
            _textProcessingService = textProcessingService;
            _httpClientFactory = httpClientFactory;
            _cts = new CancellationTokenSource();
            _groupAccess = groupAccess;
            _logger = logger;
            _botClient = new TelegramBotClient(_botToken, _httpClientFactory.CreateClient());
        }

        public static UpdateType[] ConvertStringToUpdateType(string[] updateStrings)
        {
            return updateStrings.Select(update => Enum.Parse<UpdateType>(update, ignoreCase: true)).ToArray();
        }

        public void StartReceivingMessages()
        {
            _logger.LogInformation("Starting to receive messages...");
            string[] updateStrings = _botConfigurationOptions.AllowedUpdatesSettings?.AllowedUpdates ?? Array.Empty<string>();
            UpdateType[] updates = ConvertStringToUpdateType(updateStrings);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = updates
            };

            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, _cts.Token);
        }

        public async Task AddGroup(long groupId, GroupInfo groupInfo)
        {
            ValidateGroupParameters(groupId, groupInfo);

            await _groupAccess.AddGroupAsync(groupId, groupInfo);
        }
        private void ValidateGroupParameters(long groupId, GroupInfo groupInfo)
        {
            if (groupId == 0)
            {
                throw new ArgumentException("Group Id cannot be 0.", nameof(groupId));
            }

            if (groupInfo == null)
            {
                throw new ArgumentException("Group info cannot be null.", nameof(groupInfo));
            }

            groupInfo.Validate(); // Validation within the GroupInfo class
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

        public async Task AddCreditToGroupAsync(string groupId, decimal creditAmount)
        {
            await _groupAccess.AddCreditToGroupAsync(groupId, creditAmount);
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
                        case MessageType.NewChatTitle:
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

            if (!(messageText.EndsWith("..") || messageText.EndsWith("。。")))
            {
                return;
            }
            var countWords = messageText.CountWords();
            if (!(countWords >= 1 && countWords <= 80))
            {
                return;
            }

            messageText = messageText.Remove(messageText.Length - 2);

            bool isValidGroup = await IsValidGroup(update.Message);
            if (!isValidGroup)
            {
                await InformInvalidGroupAsync(botClient, update, cancellationToken);
                return;
            }

            var groupId = update.Message.Chat.Id.ToString();

            var result = await BuildTextProcessingRequestAsync(messageText, groupId);

            if (!result.groupIsActive)
            {
                await botClient.SendMessage(update.Message.Chat.Id, result.MessageToUser, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                return;
            }

            if (result.Result != messageText)
            {
                await SendProcessedTextAsync(botClient, update.Message, result.Result, cancellationToken);
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

            var correctedText = $"<i> This robot is only active for certain groups. Please contact the bot manufacturer: @Roohi_C </i>";

            await botClient.SendMessage(
                chatId: chatId,
                text: correctedText,
                parseMode: ParseMode.Html,
                replyParameters: new ReplyParameters { MessageId = messageId },
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

        private async Task<TextProcessingResult> BuildTextProcessingRequestAsync(string messageText, string groupId)
        {
            try
            {
                var settings = _botConfigurationOptions.UnifiedTextSettings;
                var languageGroup = await _groupAccess.GetGroupLanguageAsync(groupId);
                var languageToUse = languageGroup ?? _botConfigurationOptions.TranslateTheTextTo;

                string prompt = settings.Prompt.Replace("{language}", languageToUse);
                string promptWithMessage = $"{prompt} '{messageText}'";
                var openAiModel = _botConfigurationOptions.OpenAiModel;
                var message = messageText.Trim();
                var request = new TextProcessingRequest(prompt, promptWithMessage, openAiModel, settings.MaxTokens, settings.Temperature, groupId, message);

                return await _textProcessingService.ProcessTextAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing the text: {ex.Message}");
                return new TextProcessingResult(null, false, "An error occurred while processing the text. Please try again later.");
            }
        }

        private string FormatPrompt(string language, string currentPrompt)
        {
            return currentPrompt.Replace("{language}", language);
        }

        private async Task SendProcessedTextAsync(ITelegramBotClient botClient, Message message, string response, CancellationToken cancellationToken)
        {
            var chatId = message?.Chat.Id ?? 0;
            var messageId = message?.MessageId ?? 0;
            var writer = !string.IsNullOrEmpty(message?.From?.Username) ? $"@{message.From.Username}" : $"{message?.From?.FirstName} {message.From?.LastName}";
            var correctedText = $"<i>{writer}</i> 🗨️: <blockquote><i>{response}</i></blockquote>";

            await botClient.SendMessage(
                chatId: chatId,
                text: correctedText,
                 replyParameters: new ReplyParameters { MessageId = messageId },
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
    }
}