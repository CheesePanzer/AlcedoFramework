namespace Alcedo.Frontend.Blazor;

public interface IBarItem
{
    public string Title { get; set; }

    public string? Href { get; set; }

    public string? Icon { get; set; }
}

public class BarRegistry
{
    private readonly List<IBarItem> _groups = [];
    public IReadOnlyList<IBarItem> Groups => _groups;

    public void Register(IBarItem group) => _groups.Add(group);
}