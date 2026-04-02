using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mullai.Skills;

public static class SkillExtension
{
    /// <summary>
    ///     Registers the FileAgentSkillsProvider in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddMullaiSkills(this IServiceCollection services)
    {
        // services.AddKeyedSingleton<AgentSkillsProvider>("skill-no-advertise", (sp, key) =>
        // {
        //     AgentSkillsProviderOptions providerOptions = new()
        //     {
        //         SkillsInstructionPrompt = ""
        //     };
        //
        //     var skillProvider = new AgentSkillsProvider(
        //         Path.Combine(AppContext.BaseDirectory, "skills"),
        //         options:  providerOptions,
        //         loggerFactory: sp.GetRequiredService<ILoggerFactory>());
        //
        //     return skillProvider;
        // });
        
        services.AddKeyedSingleton<AgentSkillsProvider>("skill-advertise", (sp, key) =>
        {
            var skillProvider = new AgentSkillsProvider(
                Path.Combine(AppContext.BaseDirectory, "skills"),
                loggerFactory: sp.GetRequiredService<ILoggerFactory>());
    
            return skillProvider;
        });

        return services;
    }
}