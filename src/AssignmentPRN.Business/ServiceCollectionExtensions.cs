using AssignmentPRN.DataAccess.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssignmentPRN.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDataAccess(connectionString);
        services.TryAddSingleton<IQuestionRandomizer, SecureQuestionRandomizer>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IQuestionAllocationService, QuestionAllocationService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();
        return services;
    }
}
