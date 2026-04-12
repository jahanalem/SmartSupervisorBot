namespace SmartSupervisorBot.Model
{
    public record SupportedOpenAiModel(
         string Id,
         decimal InputCostPerThousand,
         decimal OutputCostPerThousand,
         bool IsReasoningModel
     );
}
