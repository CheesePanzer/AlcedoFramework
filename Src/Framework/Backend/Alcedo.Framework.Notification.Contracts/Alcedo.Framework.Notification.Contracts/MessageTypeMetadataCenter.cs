using System.Reflection;

namespace Alcedo.Framework.Notification.Contracts;

public sealed record MessageTypeMetadata
{
    public string Code { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public Type PayloadType { get; init; }
    public string JsonExample { get; init; }

    public MessageTypeMetadata(string code, string name, string description, Type payloadType)
    {
        Code = code;
        Name = name;
        Description = description;
        PayloadType = payloadType;
        JsonExample = MessageExampleGenerator.GenerateJsonExample(payloadType);
    }
}

public static class MessageTypeMetadataCenter
{
    private static IReadOnlyDictionary<string, MessageTypeMetadata> _codeCache
        = new Dictionary<string, MessageTypeMetadata>();

    private static IReadOnlyDictionary<Type, MessageTypeMetadata> _typeCache
        = new Dictionary<Type, MessageTypeMetadata>();

    public static void Initialize(IEnumerable<Assembly> assemblies)
    {
        var codeDict = new Dictionary<string, MessageTypeMetadata>(StringComparer.OrdinalIgnoreCase);
        var typeDict = new Dictionary<Type, MessageTypeMetadata>();

        foreach (var assembly in assemblies)
        {
            var payloadTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(INotificationMessage)));

            foreach (var payloadType in payloadTypes)
            {
                var typeAttr = payloadType.GetCustomAttribute<MessageTypeAttribute>();

                var code = typeAttr?.Code ?? payloadType.Name;
                var name = typeAttr?.Name ?? payloadType.Name;
                var description = typeAttr?.Description ?? string.Empty;

                var meta = new MessageTypeMetadata(code, name, description, payloadType);

                typeDict[payloadType] = meta;

                if (!codeDict.TryAdd(meta.Code, meta))
                {
                    throw new InvalidOperationException(
                        $"Duplicate message type code: {meta.Code} in {payloadType.FullName}");
                }
            }
        }

        _codeCache = codeDict;
        _typeCache = typeDict;
    }

    public static MessageTypeMetadata GetMetadata(string code)
    {
        if (_codeCache.TryGetValue(code, out var meta))
        {
            return meta;
        }

        throw new InvalidOperationException($"Message type with code '{code}' is not registered.");
    }

    public static MessageTypeMetadata GetMetadata<TPayload>()
        where TPayload : INotificationMessage
        => GetMetadata(typeof(TPayload));

    public static MessageTypeMetadata GetMetadata(Type payloadType)
    {
        if (!payloadType.IsAssignableTo(typeof(INotificationMessage)))
        {
            throw new InvalidOperationException($"{payloadType.Name} is not a MessagePayload.");
        }

        if (_typeCache.TryGetValue(payloadType, out var meta))
        {
            return meta;
        }

        throw new InvalidOperationException($"Message type for payload {payloadType.Name} is not registered.");
    }

    public static IReadOnlyCollection<MessageTypeMetadata> GetAllMetadata()
        => _codeCache.Values.ToList();

    public static bool TryGetMetadata(string code, out MessageTypeMetadata? metadata)
        => _codeCache.TryGetValue(code, out metadata);

    public static bool IfCodeExists(string code)
        => _codeCache.ContainsKey(code);

    public static (int Count, IReadOnlyCollection<MessageTypeMetadata> Items) GetPagedMetadata(
        int pageNo,
        int pageSize,
        string? codeLike = null,
        string? nameLike = null)
    {
        var query = _codeCache.Values.ToList().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameLike))
        {
            query = query.Where(m => m.Name.Contains(nameLike, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(codeLike))
        {
            query = query.Where(m => m.Code.Contains(codeLike, StringComparison.OrdinalIgnoreCase));
        }

        var total = query.Count();

        var items = query.OrderBy(m => m.Code)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (total, items);
    }
}