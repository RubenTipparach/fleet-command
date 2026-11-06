// Ship.Cloak: a ship's cloak and sensors, the battle console mockup's s.cloak, s.eccm, s.sensorMode and s.scan. Plain
// state owned by Battle; the rules that change it are Battle.Cloak.cs and Battle.Sensors.cs, and the chance to hit that
// reads it is Rules.HitChance (CLAUDE.md 6.1). Its ECM is Ship.Ecm, the field the chance to hit already read.
namespace FleetCommand.Sim;

public sealed partial class Ship
{
    /// <summary>Ordered to cloak (the Science station's Cloak key, or a cloaking captain).</summary>
    public bool CloakOn;
    /// <summary>0 seen to 1 cloaked, running toward the order over the cloak's fade.</summary>
    public double CloakK;
    /// <summary>Seconds each side still sees this ship after a sweep of that side found it (index: the side).</summary>
    public readonly double[] SeenBy = new double[2];
    /// <summary>Seconds until this ship can sweep again.</summary>
    public double SweepT;
    /// <summary>Where an enemy captain last saw its target (metres), for heading there once it is lost; null before.</summary>
    public (double X, double Z)? LastSeen;
    /// <summary>A cloaking captain: cloaks to close, decloaks to strike (Battle.Cloak.cs).</summary>
    public bool AiCloak;
    /// <summary>Battle time the cloaking captain last decloaked, s.</summary>
    public double AiUncloakT;
    /// <summary>The reactor the cloak draws while cloaking or cloaked, E (set from the cloak's numbers when the battle starts).</summary>
    public double CloakCostE;

    /// <summary>Electronic counter-countermeasures: points of the sensors' energy (Battle.SetEw).</summary>
    public int Eccm;
    /// <summary>Passive sensors: harder to hit, and scans at half speed.</summary>
    public bool Passive;
    /// <summary>The ship being scanned (the locked target), and how far the scan has got, 0 to 1.</summary>
    public string? ScanId;
    public double ScanP;

    /// <summary>Fully cloaked.</summary>
    public bool Cloaked => CloakK >= 1;
    /// <summary>Cloaking, cloaked or decloaking: no shields, no fire, and the cloak's draw.</summary>
    public bool Cloaking => CloakOn || CloakK > 0;
    /// <summary>What the cloak draws now, E (Rules.Used adds it).</summary>
    public double CloakDrawE => Cloaking ? CloakCostE : 0;
}
