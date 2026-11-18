using System;
using System.IO;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneGames.Network
{
    public class SessionLauncher : MonoBehaviour
    {
        private static SessionLauncher instance;

        private OnlineSessionSettings settings;
        private bool leavingOnPurpose;
        private bool sessionRunning;
        private bool avatarsSpawned;
        private string pendingDisconnectMessage;

        public static SessionLauncher Instance
        {
            get
            {
                if (instance == null)
                    instance = Create();
                return instance;
            }
        }

        public NetworkRunner Runner { get; private set; }
        public string RoomCode { get; private set; }
        public string LocalPlayerName { get; private set; }
        public bool IsConnecting { get; private set; }
        public bool IsHost => Runner != null && Runner.IsServer;
        public bool HasDisconnectMessage => !string.IsNullOrEmpty(pendingDisconnectMessage);

        public event Action<string> StatusChanged;
        public event Action SessionStarted;
        public event Action<string> SessionFailed;
        public event Action<string> SessionEnded;

        public async void CreateRoom(string playerName)
        {
            if (!TryBeginConnecting(playerName))
                return;

            StartGameResult result = null;
            for (int attempt = 0; attempt < settings.CreateRoomAttempts; attempt++)
            {
                result = await StartRunner(GameMode.Host, RoomCodeRules.Generate());
                if (result.Ok || result.ShutdownReason != ShutdownReason.GameIdAlreadyExists)
                    break;
            }

            if (FinishConnecting(result))
                await SpawnRoster();
        }

        public async void JoinRoom(string roomCode, string playerName)
        {
            string code = RoomCodeRules.Normalize(roomCode);
            if (!RoomCodeRules.IsValid(code))
            {
                SessionFailed?.Invoke(SessionErrorMessages.InvalidRoomCode);
                return;
            }

            if (!TryBeginConnecting(playerName))
                return;

            FinishConnecting(await StartRunner(GameMode.Client, code));
        }

        public async void Leave()
        {
            if (Runner == null)
                return;

            leavingOnPurpose = true;
            await Runner.Shutdown();
        }

        public bool StartMatch()
        {
            MageRoster roster = MageRoster.For(Runner);
            int sceneIndex = BuildIndexOf(settings.GameSceneName);
            if (!IsHost || roster == null || !roster.CanStartMatch || sceneIndex < 0)
                return false;

            roster.BeginMatch();
            Runner.SessionInfo.IsOpen = false;
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
            return true;
        }

        public bool LoadSceneForEveryone(string sceneName)
        {
            int sceneIndex = BuildIndexOf(sceneName);
            if (!IsHost || sceneIndex < 0)
                return false;

            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
            return true;
        }

        public bool TryTakeDisconnectMessage(out string message)
        {
            message = pendingDisconnectMessage;
            pendingDisconnectMessage = null;
            return !string.IsNullOrEmpty(message);
        }

        private static SessionLauncher Create()
        {
            var launcherObject = new GameObject("Session Launcher");
            DontDestroyOnLoad(launcherObject);
            var launcher = launcherObject.AddComponent<SessionLauncher>();
            launcher.settings = Resources.Load<OnlineSessionSettings>(OnlineSessionSettings.ResourcePath);
            return launcher;
        }

        private async Task SpawnRoster()
        {
            try
            {
                await Runner.SpawnAsync(settings.RosterPrefab);
            }
            catch (NetworkObjectSpawnException exception)
            {
                Debug.LogWarning("[SessionLauncher] Room roster could not be spawned: " + exception.Message);
                pendingDisconnectMessage = SessionErrorMessages.RoomSetupFailed;
                Leave();
            }
        }

        private bool TryBeginConnecting(string playerName)
        {
            if (IsConnecting || Runner != null)
            {
                SessionFailed?.Invoke(SessionErrorMessages.AlreadyConnecting);
                return false;
            }

            if (settings == null || settings.RosterPrefab == null || settings.AvatarPrefab == null)
            {
                SessionFailed?.Invoke(SessionErrorMessages.SettingsMissing);
                return false;
            }

            if (!PlayerNameRules.IsValid(playerName))
            {
                SessionFailed?.Invoke(SessionErrorMessages.InvalidName);
                return false;
            }

            IsConnecting = true;
            LocalPlayerName = PlayerNameRules.Normalize(playerName);
            StatusChanged?.Invoke(SessionErrorMessages.Connecting);
            return true;
        }

        private bool FinishConnecting(StartGameResult result)
        {
            IsConnecting = false;
            if (result == null || !result.Ok)
            {
                SessionFailed?.Invoke(SessionErrorMessages.ForStartFailure(result != null ? result.ShutdownReason : ShutdownReason.Error));
                return false;
            }

            sessionRunning = true;
            GameSession.IsOnline = true;
            SessionStarted?.Invoke();
            return true;
        }

        private async Task<StartGameResult> StartRunner(GameMode mode, string code)
        {
            Runner = CreateRunner();
            RoomCode = code;

            var args = new StartGameArgs
            {
                GameMode = mode,
                SessionName = code,
                PlayerCount = MageRoster.MaxPlayers,
                SceneManager = Runner.GetComponent<NetworkSceneManagerDefault>(),
            };

            NetworkRunner runner = Runner;
            StartGameResult result = await runner.StartGame(args);
            if (!result.Ok)
            {
                Runner = null;
                if (runner != null)
                    Destroy(runner.gameObject);
            }

            return result;
        }

        private NetworkRunner CreateRunner()
        {
            var runnerObject = new GameObject("Network Runner");
            DontDestroyOnLoad(runnerObject);
            var runner = runnerObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
            runnerObject.AddComponent<NetworkSceneManagerDefault>();

            var events = runnerObject.AddComponent<NetworkEvents>();
            if (events.OnShutdown == null)
                events.OnShutdown = new NetworkEvents.ShutdownEvent();
            events.OnShutdown.AddListener(OnRunnerShutdown);
            if (events.OnSceneLoadDone == null)
                events.OnSceneLoadDone = new NetworkEvents.RunnerEvent();
            events.OnSceneLoadDone.AddListener(OnSceneLoadDone);
            runnerObject.AddComponent<LocalMageInputProvider>().Attach(events);
            return runner;
        }

        private async void OnSceneLoadDone(NetworkRunner runner)
        {
            MageRoster roster = MageRoster.For(runner);
            if (!runner.IsServer || avatarsSpawned || roster == null || SceneManager.GetActiveScene().name != settings.GameSceneName)
                return;

            avatarsSpawned = true;
            if (!await MatchAvatarSpawner.SpawnAll(runner, roster, settings.AvatarPrefab))
                Debug.LogWarning("[SessionLauncher] Some mages will not be synchronized in this match.");
        }

        private void OnRunnerShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (runner != Runner && Runner != null)
                return;

            bool wasRunning = sessionRunning;
            Runner = null;
            sessionRunning = false;
            avatarsSpawned = false;
            GameSession.IsOnline = false;

            if (leavingOnPurpose)
            {
                leavingOnPurpose = false;
                SessionEnded?.Invoke(string.Empty);
                return;
            }

            if (!wasRunning)
                return;

            pendingDisconnectMessage = SessionErrorMessages.ForDisconnect(reason);
            SessionEnded?.Invoke(pendingDisconnectMessage);
            ReturnToMenu();
        }

        private void ReturnToMenu()
        {
            if (SceneManager.GetActiveScene().name != settings.MenuSceneName)
                SceneManager.LoadScene(settings.MenuSceneName);
        }

        private static int BuildIndexOf(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                if (Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName)
                    return i;
            }

            return -1;
        }
    }
}
