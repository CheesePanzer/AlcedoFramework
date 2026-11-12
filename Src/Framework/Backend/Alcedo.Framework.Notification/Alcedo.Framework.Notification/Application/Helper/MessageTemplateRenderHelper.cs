using System.Text.Json;
using Scriban;

namespace Alcedo.Framework.Notification.Application.Helper;

public static class MessageTemplateRenderHelper
{
    public static async Task<(string title, string body)> RenderAsync(Template? titleTemplate, Template? bodyTemplate,
        string payloadJson)
    {
        var model = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        string? titleResult = null;
        string? bodyResult = null;

        if (titleTemplate is not null)
        {
            titleResult = await titleTemplate.RenderAsync(model);
        }

        if (bodyTemplate is not null)
        {
            bodyResult = await bodyTemplate.RenderAsync(model);
        }

        return (titleResult ?? string.Empty, bodyResult ?? string.Empty);
    }
}