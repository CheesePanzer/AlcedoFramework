using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.StateMachine;
using Microsoft.AspNetCore.WebUtilities;

namespace Alcedo.Frontend.Core.HttpClient;

public class AlcedoDispatchProxy : DispatchProxy
{
    private System.Net.Http.HttpClient _http = null!;
    private Type _interfaceType = null!;

    public static T Create<T>(System.Net.Http.HttpClient http) where T : class
    {
        var proxy = Create<T, AlcedoDispatchProxy>();
        ((AlcedoDispatchProxy)(object)proxy)._http = http;
        ((AlcedoDispatchProxy)(object)proxy)._interfaceType = typeof(T);
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        try
        {
            var (httpMethod, template) = HttpMethodResolver.Resolve(method!);
            var resolved = ParameterResolver.Resolve(method!, args, httpMethod);
            var url = UrlBuilder.Build(_interfaceType, template, resolved.RouteParams, resolved.QueryParams);
            var resultType = ReturnTypeHelper.GetResultType(method!.ReturnType);

            var ct = args?
                .OfType<CancellationToken>()
                .FirstOrDefault() ?? CancellationToken.None;

            Console.WriteLine($"[AlcedoProxy] {httpMethod.Method} {url}");

            return HttpDispatcher.Dispatch(_http, httpMethod.Method, url, resolved.Body, resolved.FormFields, resultType, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AlcedoProxy] Error on {method?.Name}: {ex}");
            throw;
        }
    }
}

// --- HTTP Method Resolution ---

internal static class HttpMethodResolver
{
    public static (HttpMethod method, string? template) Resolve(MethodInfo method) =>
        method.GetCustomAttribute<HttpGetAttribute>() is { } get ? (HttpMethod.Get, get.Template)
        : method.GetCustomAttribute<HttpPostAttribute>() is { } post ? (HttpMethod.Post, post.Template)
        : method.GetCustomAttribute<HttpPutAttribute>() is { } put ? (HttpMethod.Put, put.Template)
        : method.GetCustomAttribute<HttpPatchAttribute>() is { } patch ? (HttpMethod.Patch, patch.Template)
        : method.GetCustomAttribute<HttpDeleteAttribute>() is { } delete ? (HttpMethod.Delete, delete.Template)
        : throw new InvalidOperationException($"{method.Name} missing Http method attribute");
}

// --- Parameter Resolution ---

internal sealed record ResolvedParameters(
    IDictionary<string, string?> RouteParams,
    object? Body,
    List<KeyValuePair<string, string?>> QueryParams,
    List<(string Name, object? Value)> FormFields);

internal static class ParameterResolver
{
    public static ResolvedParameters Resolve(MethodInfo method, object?[]? args, HttpMethod httpMethod)
    {
        var routeDict = new Dictionary<string, string?>();
        var queryList = new List<KeyValuePair<string, string?>>();
        var formFields = new List<(string Name, object? Value)>();
        object? body = null;

        var parameters = method.GetParameters();

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var arg = args?[i];

            if (param.ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            if (param.GetCustomAttribute<ToRouteAttribute>() is not null)
            {
                RouteParamHelper.Merge(routeDict, param.Name!, arg);
            }

            else if (param.GetCustomAttribute<ToBodyAttribute>() is not null)
            {
                body = arg;
            }

            else if (param.GetCustomAttribute<ToQueryAttribute>() is not null)
            {
                QueryParamHelper.Merge(queryList, param.Name!, arg);
            }
            
            else if (param.GetCustomAttribute<ToFormAttribute>() is not null)
            {
                formFields.Add((param.Name!, arg));
            }
            
            else
            {
                ResolveByConvention(routeDict, queryList, ref body, param, arg, httpMethod);
            }
        }

        return new ResolvedParameters(routeDict, body, queryList, formFields);
    }

    private static void ResolveByConvention(
        IDictionary<string, string?> routeDict,
        List<KeyValuePair<string, string?>> queryList,
        ref object? body,
        ParameterInfo param,
        object? arg,
        HttpMethod httpMethod)
    {
        if (TypeHelper.IsSimple(param.ParameterType))
        {
            RouteParamHelper.Merge(routeDict, param.Name!, arg);
        }
        else if (httpMethod == HttpMethod.Get)
        {
            QueryParamHelper.Merge(queryList, param.Name!, arg);
        }
        else
        {
            body = arg;
        }
    }
}

// --- Route Param Helper ---

internal static class RouteParamHelper
{
    public static void Merge(IDictionary<string, string?> dict, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        dict[name] = value.ToString();
    }
}

// --- Query Param Helper ---

internal static class QueryParamHelper
{
    public static void Merge(List<KeyValuePair<string, string?>> list, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        Flatten(list, name, value, isRoot: true); // 新增 isRoot
    }

    private static void Flatten(List<KeyValuePair<string, string?>> list, string key, object value, bool isRoot = false)
    {
        if (value is string str)
        {
            list.Add(new(key, str));
            return;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                if (TypeHelper.IsSimple(item.GetType()))
                {
                    list.Add(new($"{key}[{index}]", item.ToString()));
                }
                else
                {
                    Flatten(list, $"{key}[{index}]", item);
                }

                index++;
            }

            return;
        }

        if (value is ValueAtom atom)
        {
            list.Add(new(key, atom.Value));
            return;
        }

        if (TypeHelper.IsSimple(value.GetType()))
        {
            list.Add(new(key, value.ToString()));
            return;
        }

        foreach (var prop in value.GetType().GetProperties())
        {
            var propValue = prop.GetValue(value);
            if (propValue is null)
            {
                continue;
            }

            var childKey = isRoot ? prop.Name : $"{key}.{prop.Name}";
            Flatten(list, childKey, propValue);
        }
    }
}

// --- URL Builder ---

internal static class UrlBuilder
{
    public static string Build(
        Type interfaceType,
        string? template,
        IDictionary<string, string?> routeParams,
        List<KeyValuePair<string, string?>> queryParams)
    {
        var prefix = BuildPrefix(interfaceType);
        var path = string.IsNullOrEmpty(template) ? prefix : $"{prefix}/{template}";

        path = ApplyRouteParams(path, routeParams);
        path = ApplyQueryParams(path, queryParams);

        return path;
    }

    private static string BuildPrefix(Type interfaceType)
    {
        var attr = interfaceType.GetCustomAttribute<ApiClientAttribute>()!;

        return attr.Area is null
            ? $"/api/v{attr.Version}/{attr.Controller}"
            : $"/api/v{attr.Version}/{attr.Area}/{attr.Controller}";
    }

    private static string ApplyRouteParams(string path, IDictionary<string, string?> routeParams) =>
        routeParams.Aggregate(path, (current, kv) =>
            current.Replace($"{{{kv.Key}}}", kv.Value));

    private static string ApplyQueryParams(string path, List<KeyValuePair<string, string?>> queryParams) =>
        queryParams.Count > 0
            ? QueryHelpers.AddQueryString(path, queryParams)
            : path;
}

// --- Return Type ---

internal static class ReturnTypeHelper
{
    public static Type? GetResultType(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? returnType.GetGenericArguments()[0]
            : null;
}

// --- HTTP Dispatch ---

internal static class HttpDispatcher
{
    public static object? Dispatch(
        System.Net.Http.HttpClient http,
        string httpMethod,
        string url,
        object? body,
        List<(string Name, object? Value)> formFields,
        Type? resultType,
        CancellationToken ct)
    {
        if (formFields.Count > 0)
        {
            return DispatchForm(http, httpMethod, url, formFields, resultType, ct);
        }

        return httpMethod switch
        {
            "GET" => resultType is not null ? InvokeGet(http, resultType, url, ct) : Task.CompletedTask,
            "POST" => resultType is not null
                ? InvokeWithBody(http, resultType, url, body, HttpMethod.Post, ct)
                : http.PostAsJsonAsync(url, body, cancellationToken: ct),
            "PUT" => resultType is not null
                ? InvokeWithBody(http, resultType, url, body, HttpMethod.Put, ct)
                : http.PutAsJsonAsync(url, body, cancellationToken: ct),
            "PATCH" => resultType is not null
                ? InvokeWithBody(http, resultType, url, body, HttpMethod.Patch, ct)
                : http.PatchAsJsonAsync(url, body, cancellationToken: ct),
            "DELETE" => resultType is not null ? InvokeDelete(http, resultType, url, ct) : http.DeleteAsync(url, ct),
            _ => throw new NotSupportedException(httpMethod)
        };
    }
    
    private static object? DispatchForm(
        System.Net.Http.HttpClient http,
        string httpMethod,
        string url,
        List<(string Name, object? Value)> formFields,
        Type? resultType,
        CancellationToken ct)
    {
        var method = httpMethod switch
        {
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            _ => throw new NotSupportedException($"[ToForm] does not support {httpMethod}")
        };

        return resultType is not null
            ? InvokeWithForm(http, resultType, url, formFields, method, ct)
            : SendFormAsync(http, url, formFields, method, ct);
    }
    
    private static object InvokeWithForm(
        System.Net.Http.HttpClient http,
        Type resultType,
        string url,
        List<(string Name, object? Value)> formFields,
        HttpMethod method,
        CancellationToken ct)
    {
        return typeof(HttpDispatcher)
            .GetMethod(nameof(SendFormAndReadAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType)
            .Invoke(null, [http, url, formFields, method, ct])!;
    }

    private static async Task SendFormAsync(
        System.Net.Http.HttpClient http,
        string url,
        List<(string Name, object? Value)> formFields,
        HttpMethod method,
        CancellationToken ct)
    {
        using var content = BuildMultipartContent(formFields);
        var response = await http.SendAsync(new HttpRequestMessage(method, url) { Content = content }, ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<T> SendFormAndReadAsync<T>(
        System.Net.Http.HttpClient http,
        string url,
        List<(string Name, object? Value)> formFields,
        HttpMethod method,
        CancellationToken ct)
    {
        using var content = BuildMultipartContent(formFields);
        using var response = await http.SendAsync(new HttpRequestMessage(method, url) { Content = content }, ct);

        if (typeof(T) == typeof(string))
            return (T)(object)await response.Content.ReadAsStringAsync(ct);

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }
    
    private static MultipartFormDataContent BuildMultipartContent(List<(string Name, object? Value)> formFields)
    {
        var content = new MultipartFormDataContent();

        foreach (var (name, value) in formFields)
        {
            if (value is null) continue;

            switch (value)
            {
                case FormFile file:
                    AppendFile(content, name, file);
                    break;

                case IEnumerable<FormFile> files:
                    foreach (var f in files)
                        AppendFile(content, name, f);
                    break;

                case string str:
                    content.Add(new StringContent(str), name);
                    break;

                default:
                    content.Add(new StringContent(JsonSerializer.Serialize(value, JsonOptions)), name);
                    break;
            }
        }

        return content;
    }

    private static void AppendFile(MultipartFormDataContent content, string name, FormFile file)
    {
        var sc = new StreamContent(file.Stream);
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(sc, name, file.FileName);
    }

    private static object InvokeGet(System.Net.Http.HttpClient http, Type resultType, string url, CancellationToken ct)
    {
        return typeof(HttpDispatcher)
            .GetMethod(nameof(GetAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType)
            .Invoke(null, [http, url, ct])!;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new ValueAtomConverterFactory(), new JsonStringEnumConverter() }
    };

    private static async Task<T?> GetAsync<T>(System.Net.Http.HttpClient http, string url, CancellationToken ct)
    {
        if (typeof(T) == typeof(string))
        {
            return (T?)(object)await http.GetStringAsync(url, ct);
        }

        return await http.GetFromJsonAsync<T>(url, JsonOptions, ct);
    }

    private static object InvokeWithBody(System.Net.Http.HttpClient http, Type resultType, string url, object? body,
        HttpMethod httpMethod, CancellationToken ct)
    {
        return typeof(HttpDispatcher)
            .GetMethod(nameof(SendAndRead), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType)
            .Invoke(null, [http, url, body, httpMethod, ct])!;
    }

    private static object InvokeDelete(System.Net.Http.HttpClient http, Type resultType, string url,
        CancellationToken ct)
    {
        return typeof(HttpDispatcher)
            .GetMethod(nameof(SendAndReadDelete), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType)
            .Invoke(null, [http, url, ct])!;
    }

    private static async Task<T> SendAndRead<T>(System.Net.Http.HttpClient http, string url, object? body,
        HttpMethod method, CancellationToken ct)
    {
        using var response = method.Method switch
        {
            "POST" => await http.PostAsJsonAsync(url, body, ct),
            "PUT" => await http.PutAsJsonAsync(url, body, ct),
            "PATCH" => await http.PatchAsJsonAsync(url, body, ct),
            _ => throw new NotSupportedException(method.Method)
        };

        if (typeof(T) == typeof(string))
        {
            return (T)(object)await response.Content.ReadAsStringAsync(ct);
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }

    private static async Task<T> SendAndReadDelete<T>(System.Net.Http.HttpClient http, string url, CancellationToken ct)
    {
        using var response = await http.DeleteAsync(url, ct);

        if (typeof(T) == typeof(string))
        {
            return (T)(object)await response.Content.ReadAsStringAsync(ct);
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }
}

// --- Type Helper ---

internal static class TypeHelper
{
    public static bool IsSimple(Type type) =>
        type.IsPrimitive
        || type == typeof(string)
        || type == typeof(Guid)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(decimal)
        || Nullable.GetUnderlyingType(type) is not null;
}