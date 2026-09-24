using System.Reflection;

namespace CaroClient;

public static class CaroBranding
{
    public const string DisplayName = "C A R O";
    public static Icon CreateIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Caro.Icon")!;
        using var icon = new Icon(stream, 48, 48);
        return (Icon)icon.Clone();
    }
    public static Image CreateImage()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Caro.Image")!;
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }
}

// All app windows share the embedded brand, including secondary dialogs.
public class CaroForm : Form
{
    private readonly Icon _brandIcon = CaroBranding.CreateIcon();
    public CaroForm() { Icon = _brandIcon; Text = CaroBranding.DisplayName; }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _brandIcon.Dispose();
    }
}
