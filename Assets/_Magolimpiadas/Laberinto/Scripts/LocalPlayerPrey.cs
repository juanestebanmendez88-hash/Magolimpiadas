using UnityEngine;

public class LocalPlayerPrey : IMinotaurPrey
{
    private const float CaughtRoarVolume = 0.8f;

    private readonly PlayerMage player;

    public LocalPlayerPrey(PlayerMage player)
    {
        this.player = player;
    }

    public Transform Body => player.transform;
    public Vector3 Position => player.transform.position;
    public bool CanBeHunted => player.Available && !player.InShelter;

    public void Catch(float respawnSeconds)
    {
        CatchLocalPlayer(player, respawnSeconds);
    }

    public static void CatchLocalPlayer(PlayerMage localPlayer, float respawnSeconds)
    {
        if (localPlayer == null || !localPlayer.Available)
            return;

        GameSound.Play(GameAudio.Sound.MonsterRoar, CaughtRoarVolume);
        localPlayer.Catch(respawnSeconds);
    }
}
