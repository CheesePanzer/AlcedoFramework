namespace Alcedo.Frontend.Blazor;

public interface INavContributor
{
    IEnumerable<NavItem> GetItems();
}

public sealed class NavItem
{
    public required string Title { get; set; }
    public string? Href { get; set; }
    public required string Icon { get; set; }
    public List<NavItem> Children { get; set; } = [];
    public bool HasChildren => Children.Count > 0;
}

public class NavRegistry
{
    private readonly List<NavItem> _items = [];
    public IReadOnlyList<NavItem> Items => _items;

    public void Register(NavItem item) => _items.Add(item);
    
    public void Register(params NavItem[] items) => _items.AddRange(items);
}