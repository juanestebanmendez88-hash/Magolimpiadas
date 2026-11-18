using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class ChosenMage : MonoBehaviour
{
    public const string PlayerMageId = "nebulosa";

    public bool useLastChoice = true;
    public string testMage = "";

    public static string Chosen { get; private set; } = PlayerMageId;

    void Awake()
    {
        Chosen = Read();
        if (Chosen != PlayerMageId) Change3D(Chosen);
    }

    void Start()
    {
        Choose2D(Chosen);
    }

    string Read()
    {
        string id = ArcaneGames.GameSession.SelectedWizard != null ? ArcaneGames.GameSession.SelectedWizard.wizardId : "";
        if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(testMage)) id = testMage;
        if (string.IsNullOrEmpty(id) && useLastChoice) id = PlayerPrefs.GetString(ArcaneGames.GameSession.SelectedWizardKey, "");
        id = string.IsNullOrEmpty(id) ? PlayerMageId : id.Trim().ToLowerInvariant();
        return id;
    }

    void Change3D(string id)
    {
        var playerMage = FindFirstObjectByType<PlayerMage>();
        var bot = FindObjectsByType<BotMage3D>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(b => b.mageId == id);
        if (playerMage == null || bot == null)
        {
            Debug.LogWarning("[ChosenMage] No encontré al mago \"" + id + "\" en la escena: se juega con la Maga Nebulosa.");
            Chosen = PlayerMageId;
            return;
        }

        var playerMageBody = playerMage.animator.transform;
        var botBody = bot.animator.transform;
        playerMageBody.SetParent(bot.transform, false);
        botBody.SetParent(playerMage.transform, false);
        (playerMage.animator, bot.animator) = (bot.animator, playerMage.animator);
        (playerMage.torch, bot.torch) = (bot.torch, playerMage.torch);
        playerMage.model = botBody.gameObject;

        string chosenName = bot.displayName;
        bot.displayName = "Maga Nebulosa";
        bot.mageId = PlayerMageId;
        bot.gameObject.name = "Maga Nebulosa";
        playerMage.gameObject.name = chosenName;

        var cinematic = FindFirstObjectByType<IslandCinematic>();
        if (cinematic != null) (cinematic.arrivalPortal, bot.islandPortal) = (bot.islandPortal, cinematic.arrivalPortal);

        var chosenBow = bot.bow;
        var playerMageBow = FindObjectsByType<Collectible>(FindObjectsSortMode.None).FirstOrDefault(r => r.owner == null && r != chosenBow);
        if (chosenBow != null) chosenBow.owner = null;
        if (playerMageBow != null) playerMageBow.owner = bot;
        bot.bow = playerMageBow;
    }

    void Choose2D(string id)
    {
        var change = FindFirstObjectByType<CharacterSwitcher>(FindObjectsInactive.Include);
        var world = WorldSwitcher.Instance;
        if (change == null || world == null) return;
        var mage = change.mages.FirstOrDefault(m => m != null && m.data != null && Id(m.data.element) == id);
        if (mage != null && mage != world.player2D) change.SwitchTo(mage, false);
    }

    public static string Id(MagicElement e)
    {
        switch (e)
        {
            case MagicElement.Fire: return "elemental";
            case MagicElement.Nebula: return "nebulosa";
            case MagicElement.Plant: return "druida";
            case MagicElement.Lightning: return "rayo";
        }
        return "";
    }
}
