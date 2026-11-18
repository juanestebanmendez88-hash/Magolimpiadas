using UnityEngine;

public static class Weaknesses
{
    public static readonly Color ColorElemental = new Color(1f, 0.5f, 0.15f);
    public static readonly Color NebulaColor = new Color(0.72f, 0.38f, 1f);
    public static readonly Color DruidColor = new Color(0.55f, 0.9f, 0.2f);
    public static readonly Color LightningColor = new Color(0.3f, 0.75f, 1f);

    public static float Multiplier(MagicElement attack, MagicElement family)
    {
        if (family == MagicElement.None || attack == MagicElement.None) return 1f;
        if (attack == family) return 0f;
        return Wins(attack, family) ? 2f : 1f;
    }

    public static bool Wins(MagicElement attack, MagicElement family)
    {
        switch (family)
        {
            case MagicElement.Ice:
            case MagicElement.Plant: return attack == MagicElement.Fire;
            case MagicElement.Fire: return attack == MagicElement.Lightning;
            case MagicElement.Lightning: return attack == MagicElement.Nebula;
            case MagicElement.Nebula: return attack == MagicElement.Plant;
        }
        return false;
    }

    public static MagicElement Winner(MagicElement family)
    {
        switch (family)
        {
            case MagicElement.Ice:
            case MagicElement.Plant: return MagicElement.Fire;
            case MagicElement.Fire: return MagicElement.Lightning;
            case MagicElement.Lightning: return MagicElement.Nebula;
            case MagicElement.Nebula: return MagicElement.Plant;
        }
        return MagicElement.None;
    }

    public static Color WinnerColor(MagicElement family)
    {
        switch (family)
        {
            case MagicElement.Ice:
            case MagicElement.Plant: return ColorElemental;
            case MagicElement.Fire: return LightningColor;
            case MagicElement.Lightning: return NebulaColor;
            case MagicElement.Nebula: return DruidColor;
        }
        return Color.clear;
    }
}
