using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentService.Ba;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequirementAnalyzer(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<RequirementAnalyzerOptions>? configure = null)
    {
        services.AddOptions<RequirementAnalyzerOptions>();

        if (configuration != null)
        {
            services.Configure<RequirementAnalyzerOptions>(configuration.GetSection("RequirementAnalyzer"));
        }

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<IRequirementAnalyzer, RequirementAnalyzer>();
        return services;
    }
}
