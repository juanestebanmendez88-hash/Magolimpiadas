using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher : MonoBehaviour
{
    public Key key = Key.Tab;
    public List<Mage> mages = new List<Mage>();
    public float wait = 0.3f;

    float next;

    void Update()
    {
        var cm = WorldSwitcher.Instance;
        var t = Keyboard.current;
        if (cm == null || !cm.InWorld2D || cm.Switching || t == null || Time.time < next) return;
        if (ArcaneGames.Network.OnlineMatch.IsActive) return;
        if (t[key].wasPressedThisFrame) Next();
    }

    public void Next()
    {
        var cm = WorldSwitcher.Instance;
        if (cm == null || mages.Count < 2) return;
        int i = Mathf.Max(0, mages.IndexOf(cm.player2D));
        for (int k = 1; k < mages.Count; k++)
        {
            var m = mages[(i + k) % mages.Count];
            if (m != null && m.isActiveAndEnabled && m.Alive) { SwitchTo(m); return; }
        }
    }

    public void SwitchTo(Mage isNew, bool announce = true)
    {
        var cm = WorldSwitcher.Instance;
        var old = cm.player2D;
        if (isNew == null || isNew == old) return;
        next = Time.time + wait;

        var newBot = isNew.GetComponent<ControlBot>();
        float distance = newBot != null ? newBot.distance : 1.6f;
        if (old != null) MakeBot(old, isNew, distance);

        var cj = isNew.GetComponent<PlayerControl>();
        if (cj == null) cj = isNew.gameObject.AddComponent<PlayerControl>();
        cj.sceneCamera = cm.camera2D;
        cj.enabled = true;
        if (newBot != null) newBot.enabled = false;
        isNew.control = cj;
        Order(isNew, 23);

        cm.player2D = isNew;
        foreach (var m in mages)
        {
            var b = m != null ? m.GetComponent<ControlBot>() : null;
            if (b != null && m != isNew) b.leader = isNew;
        }

        var follow = cm.camera2D != null ? cm.camera2D.GetComponent<FollowCamera2D>() : null;
        if (follow != null) follow.target = isNew.transform;
        var textIndicator = FindFirstObjectByType<MageTextIndicator>(FindObjectsInactive.Include);
        if (textIndicator != null) textIndicator.Show(isNew);

        if (!announce) return;
        DamageNumbers.Message((Vector2)isNew.transform.position + Vector2.up * 3f, "¡" + isNew.data.displayName + "!", isNew.data.color);
        LevelEvent.Particles(isNew.transform.position + Vector3.up, isNew.data.color, 15, 0.25f, new Vector2(1f, 3f), 0.2f, new Vector3(0.8f, 1.5f, 0f));
        Notices.Tip("cambio_personaje", "Pulsa Tab para cambiar de mago");
    }

    void MakeBot(Mage m, Mage leader, float distance)
    {
        var cj = m.GetComponent<PlayerControl>();
        if (cj != null) cj.enabled = false;
        var b = m.GetComponent<ControlBot>();
        if (b == null) b = m.gameObject.AddComponent<ControlBot>();
        b.leader = leader;
        b.distance = distance;
        b.enabled = true;
        m.control = b;
        Order(m, 21);
    }

    static void Order(Mage m, int order)
    {
        if (m.body != null) m.body.sortingOrder = order;
    }
}
