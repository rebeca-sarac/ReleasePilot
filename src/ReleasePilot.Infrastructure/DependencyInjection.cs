using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Application.Ports;
using ReleasePilot.Infrastructure.Messaging;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Infrastructure.Persistence.Repositories;
using ReleasePilot.Infrastructure.Stubs;

namespace ReleasePilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core / PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionReadRepository, PromotionReadRepository>();

        // Messaging
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Stubs (swap for real implementations)
        services.AddTransient<IDeploymentPort, StubDeploymentPort>();
        services.AddTransient<IIssueTrackerPort, StubIssueTrackerPort>();
        services.AddTransient<INotificationPort, StubNotificationPort>();

        return services;
    }
}
