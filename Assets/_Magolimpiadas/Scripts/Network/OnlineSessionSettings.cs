using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    [CreateAssetMenu(fileName = "OnlineSessionSettings", menuName = "Juegos Arcanos/Online Session Settings")]
    public class OnlineSessionSettings : ScriptableObject
    {
        public const string ResourcePath = "OnlineSessionSettings";

        [SerializeField] private NetworkObject rosterPrefab;
        [SerializeField] private NetworkObject avatarPrefab;
        [SerializeField] private string gameSceneName = "LaberintoMinotauro";
        [SerializeField] private string menuSceneName = "00_Inicio";
        [SerializeField] private int createRoomAttempts = 3;

        public NetworkObject RosterPrefab => rosterPrefab;
        public NetworkObject AvatarPrefab => avatarPrefab;
        public string GameSceneName => gameSceneName;
        public string MenuSceneName => menuSceneName;
        public int CreateRoomAttempts => Mathf.Max(1, createRoomAttempts);
    }
}
