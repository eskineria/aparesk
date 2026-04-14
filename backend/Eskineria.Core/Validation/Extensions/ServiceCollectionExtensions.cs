using Eskineria.Core.Validation.Filters;
using Eskineria.Core.Validation.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Eskineria.Core.Validation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaValidation(this IServiceCollection services, IEnumerable<Assembly> assembliesWithValidators)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembliesWithValidators);

        var validatorAssemblies = assembliesWithValidators
            .Where(assembly => assembly != null)
            .Append(typeof(LocalizedContentValidator).Assembly)
            .DistinctBy(assembly => assembly.FullName, StringComparer.Ordinal)
            .ToArray();

        if (validatorAssemblies.Length == 0)
            throw new ArgumentException("At least one validator assembly must be provided.", nameof(assembliesWithValidators));

        services.AddFluentValidationAutoValidation(options =>
        {
            // Keep a single validation source to avoid duplicated error entries.
            options.DisableDataAnnotationsValidation = true;
        });
        services.AddValidatorsFromAssemblies(validatorAssemblies);
        services.TryAddScoped<ValidationFilter>();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.Configure<MvcOptions>(options =>
        {
            if (options.Filters.OfType<ServiceFilterAttribute>().Any(f => f.ServiceType == typeof(ValidationFilter)))
                return;

            options.Filters.AddService<ValidationFilter>();
        });

        return services;
    }
}
