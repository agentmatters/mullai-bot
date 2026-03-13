namespace Mullai.Providers.Models;

public class ModelPricing
{
    /// <summary>Cost in USD per 1,000 input tokens.</summary>
    public decimal InputPer1kTokens { get; set; }

    /// <summary>Cost in USD per 1,000 output tokens.</summary>
    public decimal OutputPer1kTokens { get; set; }
}
