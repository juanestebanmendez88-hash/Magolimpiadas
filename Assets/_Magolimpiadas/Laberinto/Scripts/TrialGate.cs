using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrialGate : MonoBehaviour
{
    public enum Requirement { Bow, PingPongWon }
    public Requirement requirement = Requirement.Bow;

    [TextArea(2, 4)]
    public string message = "¡Alto, {mago}! Esta cortina mágica solo deja pasar a quien lleva su arco. Búscalo y vuelve para comenzar.";
    public float noticeDistance = 1f;
    public float noticeSeconds = 5f;
    public float waitBetweenNotices = 6f;

    public bool IsOpen { get; private set; }

    Collider solid;
    PlayerMage player;
    CharacterController playerControl;
    List<Collectible> bows;
    float nextNotice;

    void Start()
    {
        solid = GetComponents<Collider>().FirstOrDefault(c => !c.isTrigger && c.enabled);
        if (solid == null) { Debug.LogWarning("[TrialGate] Falta un collider sólido en " + name, this); enabled = false; return; }
        player = FindFirstObjectByType<PlayerMage>();
        if (player != null) playerControl = player.GetComponent<CharacterController>();
        bows = FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

        foreach (var bot in FindObjectsByType<BotMage3D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var cc = bot.GetComponent<CharacterController>();
            if (cc != null) Physics.IgnoreCollision(cc, solid, true);
        }
    }

    bool Meets() => requirement == Requirement.PingPongWon ? PingPongLevel.Won : PlayerHasBow();

    bool PlayerHasBow()
    {
        foreach (var a in bows) if (a != null && a.owner == null && a.Collected) return true;
        return false;
    }

    void Update()
    {
        if (player == null || playerControl == null) return;
        if (!IsOpen && Meets())
        {
            IsOpen = true;
            Physics.IgnoreCollision(playerControl, solid, true);
        }
        if (IsOpen || !player.enabled || Time.time < nextNotice) return;

        Vector3 chest = player.transform.position + Vector3.up;
        if ((solid.ClosestPoint(chest) - chest).magnitude > noticeDistance + playerControl.radius) return;
        if (!OwlNotice.Free) return;
        nextNotice = Time.time + noticeSeconds + waitBetweenNotices;
        StartCoroutine(OwlNotice.Show(this, message, noticeSeconds, Far));
    }

    bool Far()
    {
        if (player == null || IsOpen) return true;
        Vector3 chest = player.transform.position + Vector3.up;
        return (solid.ClosestPoint(chest) - chest).magnitude > noticeDistance + 4f;
    }
}
