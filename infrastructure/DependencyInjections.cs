
using Microsoft.EntityFrameworkCore;
using qa_portal_apis.domain.interfaces;
using qa_portal_apis.infrastructure.persistence;

namespace qa_portal_apis.infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IQuestionsRepository, QuestionsRepository>();
        services.AddScoped<IAnswersRepository, AnswersRepository>();
        services.AddScoped<IRoleRequestRepository, RoleRequestRepository>();

        return services;
    }
}