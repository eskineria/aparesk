using System.Reflection;
using Mapster;

namespace Eskineria.Core.Mapping;

public sealed class MappingProfile
{
    private readonly IReadOnlyList<Type> _mappingTypes;

    public MappingProfile(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _mappingTypes = assembly.GetExportedTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Eskineria.Core.Mapping.Abstractions.IMapFrom<>)))
            .ToList();
    }

    public void Apply(TypeAdapterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        foreach (var type in _mappingTypes)
        {
            object? instance;
            try
            {
                instance = Activator.CreateInstance(type, nonPublic: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create mapping type '{type.FullName}'. Ensure it has a parameterless constructor.",
                    ex);
            }

            var methodInfo = type.GetMethod(
                                 nameof(Eskineria.Core.Mapping.Abstractions.IMapFrom<object>.Mapping),
                                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                 binder: null,
                                 types: new[] { typeof(TypeAdapterConfig) },
                                 modifiers: null)
                             ?? type.GetInterfaces()
                                 .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Eskineria.Core.Mapping.Abstractions.IMapFrom<>))
                                 .Select(i => i.GetMethod(nameof(Eskineria.Core.Mapping.Abstractions.IMapFrom<object>.Mapping)))
                                 .FirstOrDefault(m => m != null);

            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    $"Mapping method was not found for '{type.FullName}'. Implement Mapping(TypeAdapterConfig config).");
            }

            try
            {
                methodInfo.Invoke(instance, new object[] { config });
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    $"Mapping registration failed for '{type.FullName}'.",
                    ex.InnerException ?? ex);
            }
        }
    }
}
