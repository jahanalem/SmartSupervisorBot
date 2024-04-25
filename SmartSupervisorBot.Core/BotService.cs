﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using System.Text;
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

        public BotService(IHttpClientFactory httpClientFactory, string botToken, string openAiToken)
        {
            _httpClientFactory = httpClientFactory;
            _botToken = botToken;
            _api = new OpenAIAPI(openAiToken);
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
                    var messageId = update.Message.MessageId;
                    if (chatId == 0)
                    {
                        return;
                    }
                    var groupUserName = update.Message.Chat.Username;

                    if (groupUserName != "ForumFuerAlle" && (update.Message.Chat.Title != "TestMyRobot"))
                    {
                        Console.WriteLine($"Received a message in chat {chatId} __ {update.Message.Chat.Title}: {messageText}");
                        var correctedText = $"<i> Dieser Roboter ist nur für bestimmte Gruppen aktiv. Bitte wenden Sie sich an den Bot-Hersteller: @Roohi_C </i>";

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: correctedText,
                            replyToMessageId: messageId,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        return;
                    }
                    //  && (update.Message.MessageThreadId != null && update.Message?.ReplyToMessage?.MessageThreadId != null)
                    if (groupUserName == "ForumFuerAlle" && 
                        (update.Message.MessageThreadId != null && 
                         update.Message?.ReplyToMessage?.MessageThreadId != null))
                    {
                        return;
                    }
                    var phoneNumber = update?.Message?.Contact?.PhoneNumber;
                    var userName = update?.Message.From?.Username ?? $"{update?.Message.From?.FirstName} {update?.Message.From?.LastName}";

                    var language = await DetectLanguageAsync(messageText);
                    var promptText = language.StartsWith("Deutsch") ?
                                $"Bitte prüfen Sie den folgenden Text gründlich auf grammatikalische Genauigkeit und korrigieren Sie Fehler, falls vorhanden: '{messageText}'"
                                :
                                $"Bitte übersetzen Sie den folgenden Text ins Deutsche und korrigieren Sie ihn, wenn nötig: '{messageText}'.";

                    var chatGptResponse = await _api.Completions.CreateCompletionAsync(new OpenAI_API.Completions.CompletionRequest
                    {
                        Model = "gpt-3.5-turbo-instruct",
                        Prompt = promptText,
                        MaxTokens = 100,
                        Temperature = 0.7
                    });

                    Console.WriteLine($"Received a message in chat {chatId}: {messageText}");
                    string response = chatGptResponse.ToString().Replace("\"","").Trim();
                    if (response != messageText)
                    {
                        var correctedText = $"<i> {userName} sagte</i>: <blockquote><i> {response} </i></blockquote>";

                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: correctedText,
                            replyToMessageId: messageId,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
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
                Model = "gpt-3.5-turbo-instruct",
                Prompt = $"Identifizieren Sie die Sprache dieses Textes: '{text}'",
                MaxTokens = 10,
                Temperature = 0.1
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