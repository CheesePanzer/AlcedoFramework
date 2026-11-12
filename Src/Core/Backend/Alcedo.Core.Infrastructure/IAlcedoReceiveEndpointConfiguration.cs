using MassTransit;

namespace Alcedo.Core.Infrastructure;

/// <summary>
/// Inherit This to manually declare receive endpoint
/// </summary>
public interface IAlcedoRabbitMqReceiveEndpointConfiguration
{
    void Configure(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context);
}

/// <summary>
/// Inherit This to manually declare receive endpoint for PostgreSQL transport
/// </summary>
public interface IAlcedoPostgresMqReceiveEndpointConfiguration
{
    void Configure(ISqlBusFactoryConfigurator  cfg, IBusRegistrationContext context);
}