namespace SmartSupervisorBot.Utilities
{
    public class OpenAiCostCalculator
    {
        // https://openai.com/api/pricing/
        public static decimal CalculateCost(int inputTokens, int outputTokens, string modelId)
        {
            // Get the model details from the registry
            var model = OpenAiModelRegistry.GetModelInfo(modelId);

            // Calculate the cost dynamically
            decimal inputCost = (inputTokens / 1000.0m) * model.InputCostPerThousand;
            decimal outputCost = (outputTokens / 1000.0m) * model.OutputCostPerThousand;

            return inputCost + outputCost;
        }
    }
}
