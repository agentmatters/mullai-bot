using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mullai.Skills;

public static class SkillExtension
{
    /// <summary>
    /// Registers the FileAgentSkillsProvider in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddMullaiSkills(this IServiceCollection services)
    {
        services.AddSingleton(sp => 
            new AgentSkillsProvider(
            skillPath: Path.Combine(AppContext.BaseDirectory, "skills"),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>()));
        
        return services;
    }
}
