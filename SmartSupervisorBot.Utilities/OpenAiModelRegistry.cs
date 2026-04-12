using SmartSupervisorBot.Model;

namespace SmartSupervisorBot.Utilities
{
    public static class OpenAiModelRegistry
    {
        public static readonly SupportedOpenAiModel Gpt4oMini = new("gpt-4o-mini", 0.00015m, 0.0006m, false);
        public static readonly SupportedOpenAiModel Gpt54Nano = new("gpt-5.4-nano", 0.0002m, 0.00125m, true);
        public static readonly SupportedOpenAiModel Gpt35TurboInstruct = new("gpt-3.5-turbo-instruct", 0.0015m, 0.002m, false);

        private static readonly Dictionary<string, SupportedOpenAiModel> Models = new()
        {
            { Gpt4oMini.Id, Gpt4oMini },
            { Gpt54Nano.Id, Gpt54Nano },
            { Gpt35TurboInstruct.Id, Gpt35TurboInstruct }
        };

        public static SupportedOpenAiModel GetModelInfo(string modelId)
        {
            if (Models.TryGetValue(modelId, out var model))
            {
                return model;
            }
            throw new ArgumentException($"Unsupported model specified: {modelId}");
        }
    }
}
