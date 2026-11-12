using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.JuiceStore.Frontend;

public sealed class JuiceStoreNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Juice Store",
            Icon = Icons.Material.Outlined.LocalDrink,
            Children =
            [
                new()
                {
                    Title = "Create Juice",
                    Icon = Icons.Material.Outlined.AddCircleOutline,
                    Href = "/juicestore/juices/new"
                },
                new()
                {
                    Title = "Manage Juices",
                    Icon = Icons.Material.Outlined.Inventory2,
                    Href = "/juicestore/juices"
                }
            ]
        }
    ];
}
