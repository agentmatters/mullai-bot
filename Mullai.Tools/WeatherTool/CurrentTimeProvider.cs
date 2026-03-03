namespace Mullai.Tools.WeatherTool;

/// <summary>
/// Provides the current date and time.
/// </summary>
/// <remarks>
/// This class returns the current date and time using the system's clock.
/// </remarks>
public class CurrentTimeProvider
{
    /// <summary>
    /// Gets the current date and time.
    /// </summary>
    /// <param name="location">The location to get the current time for (not used in this implementation).</param>
    /// <returns>The current date and time as a <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset GetCurrentTime(string location)
    {
        return DateTimeOffset.Now;
    }
}