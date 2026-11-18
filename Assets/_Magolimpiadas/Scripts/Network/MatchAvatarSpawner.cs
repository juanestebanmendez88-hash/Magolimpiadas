using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public static class MatchAvatarSpawner
    {
        public static async Task<bool> SpawnAll(NetworkRunner runner, MageRoster roster, NetworkObject avatarPrefab)
        {
            for (int mageIndex = 0; mageIndex < roster.Mages.Length; mageIndex++)
            {
                int index = mageIndex;
                try
                {
                    await runner.SpawnAsync(avatarPrefab, Vector3.zero, Quaternion.identity, OwnerOf(roster, index),
                        (spawnRunner, spawned) => spawned.GetComponent<MageAvatarState>().MageIndex = index);
                }
                catch (NetworkObjectSpawnException exception)
                {
                    Debug.LogWarning("[MatchAvatarSpawner] Avatar for mage " + index + " could not be spawned: " + exception.Message);
                    return false;
                }
            }

            return true;
        }

        private static PlayerRef OwnerOf(MageRoster roster, int mageIndex)
        {
            for (int i = 0; i < roster.Slots.Length; i++)
            {
                RosterSlot slot = roster.Slots[i];
                if (slot.IsTaken && slot.MageIndex == mageIndex)
                    return slot.Player;
            }

            return PlayerRef.None;
        }
    }
}
