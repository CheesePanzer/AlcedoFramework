using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Alcedo.Core.Presentation;

public sealed record EndpointMetadata
{
    public required string? Area { get; init; }

    public required string Resource { get; init; }
    
    public required IReadOnlyList<Attribute> ResourceAttributes { get; init; }

    public required string Operation { get; init; }

    public required string EndpointKey { get; init; }

    public required string ResourceDescription { get; init; }

    public required string OperationDescription { get; init; }
    
    public required IReadOnlyList<Attribute> OperationAttributes { get; init; }
}

public sealed class EndpointResourceOperationCenter
{
    private readonly ConcurrentDictionary<(Type, MethodInfo), string> _keys = new();
    private readonly ConcurrentDictionary<(Type, MethodInfo), (string, string)> _descs = new();
    private readonly HashSet<string> _keysList = new();
    private readonly ICollection<EndpointMetadata> _metaData = new HashSet<EndpointMetadata>();

    public EndpointResourceOperationCenter(IEnumerable<Assembly> assemblies)
    {
        Initialize(assemblies.ToList());
    }

    public string GetKey(Type controller, MethodInfo method)
        => _keys.TryGetValue((controller, method), out var key)
            ? key
            : throw new ArgumentException("Endpoint Permission Cache not initialized");

    public (string controllerDesc, string actionDesc) GetDescription(Type controller, MethodInfo method)
        => _descs.TryGetValue((controller, method), out var val)
            ? val
            : throw new ArgumentException("Endpoint Permission Cache not initialized");

    private void Initialize(IReadOnlyList<Assembly> assemblies)
    {
        var controllers = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var controller in controllers)
        {
            var methods = controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsActionMethod);

            foreach (var method in methods)
            {
                BuildAndAddKey(controller, method);
            }
        }
    }

    private bool IsActionMethod(MethodInfo method) =>
        !method.IsDefined(typeof(NonActionAttribute)) &&
        method.GetCustomAttributes(true).Any(a => a is IActionHttpMethodProvider);

    private void BuildAndAddKey(Type controller, MethodInfo method)
    {
        var resourceAttributes = controller.GetCustomAttributes<Attribute>(true).ToList();
        var operationAttributes = method.GetCustomAttributes<Attribute>(true).ToList();
        
        // Area
        var area = method.GetCustomAttribute<AreaAttribute>(true)?.RouteValue ??
                   controller.GetCustomAttribute<AreaAttribute>(true)?.RouteValue;

        // Resource
        var resourceAttr = controller.GetCustomAttribute<ResourceAttribute>(true);
        var resource = resourceAttr?.Code
                       ?? StripSuffix(controller.Name, "Controller").Kebaberize();
        var resourceDesc = resourceAttr?.Description
                           ?? controller.GetCustomAttribute<DescriptionAttribute>(true)?.Description
                           ?? StripSuffix(controller.Name, "Controller").Humanize();

        // Operation
        var operationAttr = method.GetCustomAttribute<OperationAttribute>(true);
        var operation = operationAttr?.Code
                        ?? StripSuffix(method.Name, "Async").Kebaberize();
        var operationDesc = operationAttr?.Description
                            ?? method.GetCustomAttribute<DescriptionAttribute>(true)?.Description
                            ?? StripSuffix(method.Name, "Async").Humanize();

        var key = string.IsNullOrWhiteSpace(area) ? $"{resource}.{operation}" : $"{area}.{resource}.{operation}";

        key = key.ToLowerInvariant();

        if (!_keysList.Add(key))
        {
            throw new ArgumentException($"Duplicate controller endpoint key {key}");
        }

        _keys.TryAdd((controller, method), key);

        _descs.TryAdd((controller, method), (resourceDesc, operationDesc));

        _metaData.Add(new EndpointMetadata
        {
            Area = area,
            Resource = resource,
            Operation = operation,
            EndpointKey = key,
            ResourceDescription = resourceDesc,
            OperationDescription = operationDesc,
            ResourceAttributes = resourceAttributes,
            OperationAttributes = operationAttributes
        });
    }
    
    private static string StripSuffix(string name, string suffix)
        => name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;

    public bool IfEndPointKeyExists(string endPointKey)
        => _metaData.Any(m => m.EndpointKey == endPointKey);

    public (int Count, IReadOnlyList<EndpointMetadata> Metas) GetPagedEndpointMetadata(int pageNo, int pageSize,
        string? areaLike = null, string? resLike = null, string? oprLike = null,
        string? resDescLike = null, string? actDescLike = null,
        Func<EndpointMetadata, bool>? customPredicate = null)
    {
        var query = _metaData.AsQueryable();

        if (!string.IsNullOrWhiteSpace(areaLike))
        {
            query = query.Where(m => m.Area != null && m.Area.Contains(areaLike, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(resLike))
        {
            query = query.Where(m => m.Resource.Contains(resLike, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(oprLike))
        {
            query = query.Where(m => m.Operation.Contains(oprLike, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(resDescLike))
        {
            query = query.Where(m => m.ResourceDescription.Contains(resDescLike, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(actDescLike))
        {
            query = query.Where(m => m.OperationDescription.Contains(actDescLike, StringComparison.OrdinalIgnoreCase));
        }
        
        if (customPredicate != null)
        {
            query = query.Where(m => customPredicate(m));
        }

        var total = query.Count();

        var items = query.OrderBy(m => m.EndpointKey)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (total, items);
    }

    public (int Count, IReadOnlyList<EndpointMetadata> Metas) GetPagedEndpointMetadataByKeyList(
        int pageNo, int pageSize, IReadOnlyCollection<string> keys)
    {
        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var query = _metaData.Where(q => keySet.Contains(q.EndpointKey)).ToList();

        var missingKeys = keySet.Except(query.Select(q => q.EndpointKey), StringComparer.OrdinalIgnoreCase).ToList();
        if (missingKeys.Count > 0)
        {
            throw new ArgumentException($"Endpoint key(s) not found: {string.Join(", ", missingKeys)}");
        }

        var total = query.Count;

        var items = query
            .OrderBy(m => m.EndpointKey)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (total, items);
    }
}