using System.Reflection;

using DentalClinic.Application.Common.Behaviours;
using DentalClinic.Application.Features.Imports.Patients;

using FluentValidation;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // The parser and template builder are stateless; the processor holds a
        // scoped DbContext for the duplicate-phone check.
        services.AddSingleton<PatientImportParser>();
        services.AddSingleton<PatientImportTemplateBuilder>();
        services.AddScoped<PatientImportProcessor>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(CachingBehaviour<,>));
        });

        return services;
    }
}