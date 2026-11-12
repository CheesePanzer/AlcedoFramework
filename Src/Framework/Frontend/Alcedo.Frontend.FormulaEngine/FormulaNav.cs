using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.Frontend.FormulaEngine;

public class FormulaNavContributor : INavContributor
{
    public IEnumerable<NavItem> GetItems() =>
    [
        new()
        {
            Title = "Formula Engine",
            Icon = Icons.Material.Outlined.Calculate,
            Children =
            [
                new() { Title = "Formulas", Icon = Icons.Material.Outlined.TextFields, Href = "/formula-engine/formulas" },
            ]
        }
    ];
}