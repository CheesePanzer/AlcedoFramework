using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.Frontend.Admin;

public class AdminNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Admin",
            Icon = Icons.Material.Outlined.AdminPanelSettings,
            Children =
            [
                new() { Title = "Quartz",     Icon = Icons.Material.Outlined.Update,           Href = "/admin/quartz" },
            ]
        }
    ];
}