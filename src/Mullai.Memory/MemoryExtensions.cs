using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Mullai.Memory.SystemContext;
using Mullai.Memory.UserMemory;

namespace Mullai.Memory;

/// <summary>
/// Extension methods for adding memory components to the service collection.
/// </summary>
public static class MemoryExtensions
{
    /// <summary>
    /// Adds the UserInfoMemory component to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUserMemory(this IServiceCollection services)
    {
        services.AddScoped<CurrentFolderContext>();
        return services;
    }
}
