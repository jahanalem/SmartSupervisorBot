namespace SmartSupervisorBot.Utilities
{
    public class OpenAiCostCalculator
    {
        private const decimal Gpt_4o_mini_InputCostPerThousand = 0.00015m;
        private const decimal Gpt_4o_mini_OutputCostPerThousand = 0.0006m;
        private const decimal Gpt3_5TurboInstructInputCostPerThousand = 0.0015m;
        private const decimal Gpt3_5TurboInstructOutputCostPerThousand = 0.002m;
        private const decimal Gpt4InputCostPerThousand = 0.03m;
        private const decimal Gpt4OutputCostPerThousand = 0.06m;

        // https://openai.com/api/pricing/
        public static decimal CalculateCost(int inputTokens, int outputTokens, string model)
        {
            decimal inputCost = 0;
            decimal outputCost = 0;

            switch (model)
            {
                case "gpt-4o-mini":
                    inputCost = (inputTokens / 1000.0m) * Gpt_4o_mini_InputCostPerThousand;
                    outputCost = (outputTokens / 1000.0m) * Gpt_4o_mini_OutputCostPerThousand;
                    break;
                case "gpt-3.5-turbo-instruct":
                    inputCost = (inputTokens / 1000.0m) * Gpt3_5TurboInstructInputCostPerThousand;
                    outputCost = (outputTokens / 1000.0m) * Gpt3_5TurboInstructOutputCostPerThousand;
                    break;
                case "gpt-4":
                    inputCost = (inputTokens / 1000.0m) * Gpt4InputCostPerThousand;
                    outputCost = (outputTokens / 1000.0m) * Gpt4OutputCostPerThousand;
                    break;
                default:
                    throw new ArgumentException("Unsupported model specified.");
            }

            return inputCost + outputCost;
        }
    }
}
