﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using SmartSupervisorBot.Core.Settings;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartSupervisorBot.Core
{
    public class BotService : IDisposable
    {
        private readonly ILogger<BotService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _botToken;
        private readonly OpenAIAPI _api;
        private readonly CancellationTokenSource _cts;
        private TelegramBotClient _botClient;
        private readonly BotConfigurationOptions _botConfigurationOptions;

        public BotService(IOptions<BotConfigurationOptions> botConfigurationOptions, IHttpClientFactory httpClientFactory)
        {
            _botConfigurationOptions = botConfigurationOptions.Value;
            _httpClientFactory = httpClientFactory;
            _botToken = _botConfigurationOptions.BotSettings.BotToken;
            _api = new OpenAIAPI(_botConfigurationOptions.BotSettings.OpenAiToken);
            _cts = new CancellationTokenSource();
            _botClient = new TelegramBotClient(_botToken, _httpClientFactory.CreateClient());
        }

        public void StartReceivingMessages()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, _cts.Token);

            Console.WriteLine("Press any key to stop the bot...");
            Console.ReadKey();

            _cts.Cancel();
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

                    if (!IsValidGroup(update.Message))
                    {
                        await InformInvalidGroupAsync(botClient, update, cancellationToken);

                        return;
                    }

                    if (groupUserName == "ForumFuerAlle" &&
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

                    Console.WriteLine($"Received a message in chat {chatId}: {messageText}");
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
                Console.WriteLine($"Invalid language detected.");
                return false;
            }
            return true;
        }

        private bool IsValidGroup(Message message)
        {
            return message.Chat.Username == "ForumFuerAlle" || (message.Chat.Title == "TestMyRobot");
        }

        private async Task InformInvalidGroupAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var chatId = update.Message?.Chat.Id ?? 0;
            var messageId = update.Message?.MessageId ?? 0;
            Console.WriteLine($"Received a message in chat {chatId} __ {update.Message.Chat.Title}: {update.Message.Text.Trim()}");
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
                Console.WriteLine($"Failed to send reaction: {response.StatusCode}");
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
            Console.WriteLine($"An error occurred: {exception.Message}");

            if (exception.Message.Contains("message to reply not found"))
            {
                Console.WriteLine("Die Nachricht, auf die geantwortet werden sollte, wurde nicht gefunden. Keine Aktion wird durchgeführt.");
                return Task.CompletedTask;
            }

            Console.WriteLine("Ein anderer Fehler ist aufgetreten. Bitte überprüfen Sie die Details.");

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

        public void Dispose()
        {
            _cts?.Dispose();
            _httpClientFactory?.CreateClient().Dispose();
        }
    }
}