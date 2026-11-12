using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Alcedo.Core.Abstractions;

/// <summary>
/// Register this class mapped to a specific interface in DI.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RegisterAsAttribute : Attribute
{
    public Type ServiceType { get; }
    public ServiceLifetime Lifetime { get; }

    public RegisterAsAttribute(Type serviceType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
}

/// <summary>
/// Register this class as itself in DI.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RegisterSelfAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }

    public RegisterSelfAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Lifetime = lifetime;
    }
}

internal static class AutoServiceRegister
{
    public static void AutoRegisterInAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var implementationType in types)
        {
            var registerAsAttrs = implementationType.GetCustomAttributes<RegisterAsAttribute>().ToList();
            var registerSelfAttr = implementationType.GetCustomAttribute<RegisterSelfAttribute>();

            if (registerAsAttrs.Count > 0 && registerSelfAttr is not null)
            {
                throw new ArgumentException(
                    $"Service {implementationType} cannot have both [RegisterAs] and [RegisterSelf] attributes.");
            }

            if (registerAsAttrs.Count > 0)
            {
                if (registerAsAttrs.Count == 1)
                {
                    var attr = registerAsAttrs.First();
                    Register(services, attr.ServiceType, implementationType, attr.Lifetime);
                }
                else
                {
                    var lifeTimeList = registerAsAttrs.Select(a => a.Lifetime).Distinct().ToList();
                    if (lifeTimeList.Count > 1)
                    {
                        throw new ArgumentException(
                            $"Service {implementationType} shouldn't be registered as interfaces with different lifetimes.");
                    }

                    var lifetime = lifeTimeList.First();
                    var serviceTypes = registerAsAttrs.Select(a => a.ServiceType).Distinct().ToList();

                    services.Add(new ServiceDescriptor(implementationType, implementationType, lifetime));

                    foreach (var serviceType in serviceTypes)
                    {
                        if (serviceType == implementationType)
                        {
                            continue;
                        }

                        services.TryAddEnumerable(new ServiceDescriptor(
                            serviceType,
                            sp => sp.GetRequiredService(implementationType),
                            lifetime));
                    }
                }
            }
            else if (registerSelfAttr is not null)
            {
                Register(services, implementationType, implementationType, registerSelfAttr.Lifetime);
            }
        }
    }

    private static void Register(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
        services.Add(descriptor);
    }
}