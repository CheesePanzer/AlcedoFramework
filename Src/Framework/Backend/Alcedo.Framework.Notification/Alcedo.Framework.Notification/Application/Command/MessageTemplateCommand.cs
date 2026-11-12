using System.ComponentModel.DataAnnotations;
using Alcedo.Core.Presentation.Validation;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.Framework.Notification.Application.Command;

public sealed record MessageTemplateCreateCommand : MasterDataCreateCommandBase, IValidatableObject
{
    public required NotificationChannel Channel { get; init; }
    [TextValue]
    public string? TitleTemplate { get; init; }
    [TextValue]
    public string? BodyTemplate { get; init; }

    public string? ExternalTemplateId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExternalTemplateId is null && (TitleTemplate is null || BodyTemplate is null))
        {
            yield return new ValidationResult("Must declare External TemplateId or set both of the templates",
                new[] { nameof(TitleTemplate), nameof(BodyTemplate) });
        }
    }
}

public sealed record MessageTemplateUpdateCommand : MasterDataUpdateCommandBase, IValidatableObject
{
    public required NotificationChannel Channel { get; init; }
    [TextValue]
    public string? TitleTemplate { get; init; }
    [TextValue]
    public string? BodyTemplate { get; init; }

    public string? ExternalTemplateId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExternalTemplateId is null && (TitleTemplate is null || BodyTemplate is null))
        {
            yield return new ValidationResult("Must declare External TemplateId or set both of the templates",
                new[] { nameof(TitleTemplate), nameof(BodyTemplate) });
        }
    }
}