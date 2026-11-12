using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Infrastructure;

public sealed class RabbitMqOptions
{
    [AppSettingsDemo("localhost", "RabbitMQ broker host.")]
    public string Host { get; init; } = "localhost";

    [AppSettingsDemo("5672", "RabbitMQ broker port.")]
    public ushort Port { get; init; } = 5672;

    [AppSettingsDemo("/", "RabbitMQ virtual host.")]
    public string VirtualHost { get; init; } = "/";

    [AppSettingsDemo("guest", "RabbitMQ username.")]
    public string Username { get; init; } = "guest";

    [AppSettingsDemo("guest", "RabbitMQ password.")]
    public string Password { get; init; } = "guest";
}

public sealed class DataSourceOptions : AppSettingsBase
{
    [AppSettingsDemo("Host=localhost;Database=alcedo;Username=postgres;Password=postgres",
        "PostgreSQL connection string.")]
    public string Postgres { get; init; } = string.Empty;

    [AppSettingsDemo("localhost:6379,password=,defaultDatabase=0", "StackExchange.Redis connection string.")]
    public string Redis { get; init; } = string.Empty;

    public RabbitMqOptions RabbitMQ { get; init; } = new();
}

public sealed class AlcedoCorsOptions : AppSettingsBase
{
    [AppSettingsDemo("[\"http://localhost:5173\"]", "Allowed CORS origins.")]
    public List<string> AllowedOrigins { get; init; } = [];

    [AppSettingsDemo("[\"Content-Type\",\"Authorization\"]", "Allowed request headers.")]
    public List<string> AllowedHeaders { get; init; } = [];

    [AppSettingsDemo("[\"GET\",\"POST\",\"PUT\",\"DELETE\",\"PATCH\"]", "Allowed HTTP methods.")]
    public List<string> AllowedMethods { get; init; } = [];
}

public sealed class AlcedoForwardedHeadersOptions : AppSettingsBase
{
    [AppSettingsDemo("[\"127.0.0.1\"]", "Trusted proxy IP addresses.")]
    public List<string> KnownProxies { get; init; } = [];

    [AppSettingsDemo("", "Trusted proxy network ranges in CIDR notation.")]
    public List<KnownNetworkOptions> KnownNetworks { get; init; } = [];
}

public sealed class KnownNetworkOptions
{
    public string Prefix { get; init; } = string.Empty;
    public int PrefixLength { get; init; }
}

public sealed class RedisOptions : AppSettingsBase
{
    [AppSettingsDemo("Alcedo", "Global key prefix applied to all Redis keys managed by the framework.")]
    public string GlobalPrefix { get; init; } = "Alcedo";
}

public sealed class MsgQueueOptions : AppSettingsBase
{
    [AppSettingsDemo("5", "Number of retry attempts for failed messages.")]
    public int RetryCount { get; init; } = 5;

    [AppSettingsDemo("5", "Interval in seconds between retry attempts.")]
    public int RetryIntervalSecs { get; init; } = 5;

    [AppSettingsDemo("16", "Number of messages prefetched from the broker per consumer.")]
    public int PrefetchCount { get; init; } = 16;

    [AppSettingsDemo("8", "Maximum number of messages processed concurrently per consumer.")]
    public int ConcurrentMessageLimit { get; init; } = 8;

    [AppSettingsDemo("Alcedo", "Service name used as the prefix for RabbitMQ endpoint (queue) names.")]
    public string ServiceName { get; init; } = "Alcedo";
}

public sealed class IdempotencyKeyOptions : AppSettingsBase
{
    [AppSettingsDemo("10", "Expiry in minutes for strict (server-generated) idempotency keys.")]
    public int StrictKeyExpireMinutes { get; init; } = 30;

    [AppSettingsDemo("1", "Expiry in minutes for content-based and soft idempotency keys.")]
    public int CommonKeyExpireMinutes { get; init; } = 1;
}

public sealed class AlcedoColumnEncryptorOptions : AppSettingsBase
{
    [AppSettingsDemo("",
        "Base64-encoded AES-256 key (32 bytes) for EF Core field-level encryption. Generate with: python -c \"import os,base64; print(base64.b64encode(os.urandom(32)).decode())\"")]
    public string Key { get; init; } = string.Empty;
}

public sealed class StorageOptions : AppSettingsBase
{
    [AppSettingsDemo("", "The file storage url could be a s3 public domain or cloudflare domain")]
    public string S3PublicUrl { get; init; } = string.Empty;
}