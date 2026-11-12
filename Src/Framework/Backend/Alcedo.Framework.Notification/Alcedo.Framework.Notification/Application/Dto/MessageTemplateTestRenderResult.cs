namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageTemplateTestRenderResult
{
    public required string Title { get; init; }

    public required string Body { get; init; }
}