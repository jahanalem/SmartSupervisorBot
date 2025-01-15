using SmartSupervisorBot.Model;
using SmartSupervisorBot.Model.OpenAI.Chat.Completions;
using SmartSupervisorBot.Model.OpenAI.Completions;
using System.Text.Json.Serialization;

namespace SmartSupervisorBot.TextProcessing
{
    /// <summary>
    /// OpenAiJsonSerializerContext is a JSON Source Generator context that optimizes 
    /// serialization and deserialization for specified types, improving performance and safety.
    /// 
    /// ### Key Resources:
    /// - JSON Source Generators: 
    ///   https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-source-generation
    /// - OpenAI API Reference: 
    ///   https://platform.openai.com/docs/api-reference/
    /// - System.Text.Json API: 
    ///   https://learn.microsoft.com/en-us/dotnet/api/system.text.json
    /// </summary>

    [JsonSerializable(typeof(TextProcessingRequest))]
    [JsonSerializable(typeof(ChatCompletionResponse))]
    [JsonSerializable(typeof(ChatCompletionChoice))]
    [JsonSerializable(typeof(ChatMessageResponse))]
    [JsonSerializable(typeof(TokenUsage))]
    [JsonSerializable(typeof(ChatCompletionRequest))]
    [JsonSerializable(typeof(ChatMessage))]
    [JsonSerializable(typeof(CompletionRequest))]
    [JsonSerializable(typeof(CompletionResponse))]
    [JsonSerializable(typeof(CompletionChoice))]
    internal partial class OpenAiJsonSerializerContext : JsonSerializerContext
    {
    }
}
