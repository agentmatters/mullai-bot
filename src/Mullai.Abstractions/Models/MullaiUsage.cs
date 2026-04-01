namespace Mullai.Abstractions.Models;

public sealed record MullaiUsage(long InputTokenCount, long OutputTokenCount, long TotalTokenCount);