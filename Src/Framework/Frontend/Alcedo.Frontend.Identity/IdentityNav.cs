using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.Frontend.Identity;

public class IdentityNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Identity",
            Icon = Icons.Material.Outlined.ManageAccounts,
            Children =
            [
                new()
                {
                    Title = "Users",
                    Icon = Icons.Material.Outlined.People,
                    Children =
                    [
                        new() { Title = "Create New User", Icon = Icons.Material.Outlined.PersonAdd, Href = "/identity/users/new" },
                        new() { Title = "All Users",       Icon = Icons.Material.Outlined.PeopleAlt,  Href = "/identity/users" },
                    ]
                },
                new()
                {
                    Title = "Roles",
                    Icon = Icons.Material.Outlined.AdminPanelSettings,
                    Children =
                    [
                        new() { Title = "Create New Role", Icon = Icons.Material.Outlined.AddModerator, Href = "/identity/roles/new" },
                        new() { Title = "All Roles",       Icon = Icons.Material.Outlined.Shield,        Href = "/identity/roles" },
                    ]
                },
                new() { Title = "Permissions", Icon = Icons.Material.Outlined.Key,          Href = "/identity/permissions" },
                new() { Title = "Login Log",   Icon = Icons.Material.Outlined.TrackChanges, Href = "/identity/log/loginlog" },
            ]
        }
    ];
}