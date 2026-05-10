namespace AppTemplate.Models;

public class SavedWindowState
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double? X { get; set; } = null;
    public double? Y { get; set; } = null;
    public bool IsMaximized { get; set; } = false;
}
