namespace SmartSupervisorBot.Utilities
{
    public static class TokenUtility
    {
        public static TokenCostData CalculateTokenAndCost(string prompt, int maxTokens, string model)
        {
            int inputTokens = prompt.CountTokens();
            int estimatedOutputTokens = Math.Min(maxTokens, (int)(inputTokens * 1.1));
            decimal estimatedCost = OpenAiCostCalculator.CalculateCost(inputTokens, estimatedOutputTokens, model);

            return new TokenCostData(inputTokens, estimatedOutputTokens, estimatedCost);
        }
    }
}
