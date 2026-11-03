// Rect: an axis-aligned rectangle in reference pixels, Core's own so the layout model needs no Godot types.
namespace FleetCommand.Data;

public readonly record struct Rect(double X, double Y, double W, double H)
{
    public double Right => X + W;
    public double Bottom => Y + H;
    public bool Contains(double px, double py) => px >= X && px < X + W && py >= Y && py < Y + H;
    public override string ToString() => $"[{X:0.##}, {Y:0.##}, {W:0.##}, {H:0.##}]";
}
