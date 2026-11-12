namespace Alcedo.Frontend.Blazor;

public class GlobalOverlayService
{
    public bool IsVisible { get; private set; }
    public bool DarkBackground { get; private set; }
    public bool AutoClose { get; private set; }
    public string OverlayText { get; private set; } = "Executing... Please Wait!";

    public event Action? OnChanged;

    public void Show(bool darkBackground = true, bool autoClose = false, string? overlayText = null)
    {
        IsVisible = true;
        DarkBackground = darkBackground;
        AutoClose = autoClose;
        if (overlayText != null)
        {
            OverlayText = overlayText;
        }

        OnChanged?.Invoke();
    }

    public void Hide()
    {
        IsVisible = false;
        OnChanged?.Invoke();
    }
}