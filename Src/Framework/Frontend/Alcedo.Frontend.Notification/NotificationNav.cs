using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.Frontend.Notification;

public class NotificationNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Notification",
            Icon = Icons.Material.Outlined.Campaign,
            Children =
            [
                new() { Title = "Types", Icon = Icons.Material.Outlined.Message, Href = "/notification/types" },
                new()
                {
                    Title = "Templates",
                    Icon = Icons.Material.Outlined.Description,
                    Children =
                    [
                        new() { Title = "Create New Template", Icon = Icons.Material.Outlined.NoteAdd,       Href = "/notification/templates/new" },
                        new() { Title = "All Templates",       Icon = Icons.Material.Outlined.LibraryBooks,  Href = "/notification/templates" },
                    ]
                },
                new()
                {
                    Title = "Configs",
                    Icon = Icons.Material.Outlined.Tune,
                    Children =
                    [
                        new() { Title = "Create New Config", Icon = Icons.Material.Outlined.AddCircleOutline, Href = "/notification/configs/new" },
                        new() { Title = "All Configs",       Icon = Icons.Material.Outlined.DisplaySettings,  Href = "/notification/configs" },
                    ]
                },
                new()
                {
                    Title = "Errors",
                    Icon = Icons.Material.Outlined.Error,
                    Href = "/notification/errors",
                },
            ]
        }
    ];
}