public class PortalsEvent : LevelEvent
{
    public Portal[] portals;

    protected override void OnActivate()
    {
        foreach (var p in portals) if (p != null) p.Ignite();
        Notices.Tip("portales", "¡Portales activos! Entra en uno para salir por el otro");
    }
}
