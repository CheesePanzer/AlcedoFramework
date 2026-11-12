using Alcedo.Core.Abstractions;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Infrastructure;
using Alcedo.Framework.Notification.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Framework.Notification;

public class AlcedoNotificationModule : AlcedoModuleBase
{
    protected override void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        services.AddTransient<EmailService>();

        services.AddScoped<ITemplateMemoryCacheProvider, TemplateMemoryCacheProvider>();

        MessageTypeMetadataCenter.Initialize(registry.Modules.Assemblies);
    }
}

public static class AlcedoModuleRegistryExtensions
{
    public static AlcedoModulesRegistry AddAlcedoNotification(this AlcedoModulesRegistry registry)
    {
        registry.AddModule(new AlcedoNotificationModule());
        return registry;
    }
}