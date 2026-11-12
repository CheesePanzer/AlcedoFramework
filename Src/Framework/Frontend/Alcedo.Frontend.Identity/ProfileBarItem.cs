using Alcedo.Frontend.Blazor;
using MudBlazor;

namespace Alcedo.Frontend.Identity;

public class ProfileBarItem : IBarItem
{
    public string Title { get; set; } = "Profile";
    public string? Href { get; set; } = "/identity/profile";
    public string? Icon { get; set; } = Icons.Material.Filled.AccountCircle;
}