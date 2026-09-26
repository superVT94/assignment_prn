using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Repositories;
using AssignmentPRN.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentPRN.DataAccess.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A SQL Server connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped(typeof(IEntityDao<>), typeof(EntityDao<>));
        services.AddScoped<IExamSessionDao, ExamSessionDao>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddHostedService<DatabaseInitializerHostedService>();
        return services;
    }
}
