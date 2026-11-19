using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class TempleSync : TrialSync
    {
        private const float SendInterval = 0.05f;
        private const string SwitchMageHint = "Panel Tab";
        private const string LeaveTempleHint = "Panel Esc";
        private const string OnlineControlsHint = "Q cambiar modo · E usar poder";

        private readonly List<Mage> mages = new List<Mage>();
        private readonly List<bool> ownedMages = new List<bool>();
        private readonly List<MageNetworkState> pendingActions = new List<MageNetworkState>();
        private readonly HashSet<int> creditedBullseyes = new HashSet<int>();
        private readonly HashSet<int> creditedCoins = new HashSet<int>();
        private List<Enemy> enemies = new List<Enemy>();
        private List<Coin> coins = new List<Coin>();
        private List<LevelEvent> levelEvents = new List<LevelEvent>();
        private List<PowerZone> zones = new List<PowerZone>();
        private List<Encounter> encounters = new List<Encounter>();
        private List<ElementalCircle> circles = new List<ElementalCircle>();
        private List<ElementalDoor> doors = new List<ElementalDoor>();
        private WorldSwitcher world;
        private CharacterSwitcher characterSwitch;
        private bool configured;
        private bool applyingRemote;
        private float nextSend;

        public override OnlineTrial Trial => OnlineTrial.Temple;

        public override void Spawned()
        {
            world = WorldSwitcher.Instance != null ? WorldSwitcher.Instance : FindFirstObjectByType<WorldSwitcher>(FindObjectsInactive.Include);
            characterSwitch = FindFirstObjectByType<CharacterSwitcher>(FindObjectsInactive.Include);
            if (world != null)
                world.onReturnTo3D.AddListener(OnBackTo3D);

            Enemy.AnyEliminated += OnEnemyEliminated;
            Enemy.AnyBullseyeHit += OnBullseyeHit;
            Coin.AnyCollected += OnCoinCollected;
            LevelEvent.AnyActivated += OnEventActivated;
            PowerZone.AnyUsed += OnZoneUsed;
            Encounter.AnyStarted += OnEncounterStarted;
            ElementalCircle.AnyLit += OnCircleLit;
            ElementalDoor.AnyCompleted += OnDoorCompleted;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (world != null)
                world.onReturnTo3D.RemoveListener(OnBackTo3D);

            Enemy.AnyEliminated -= OnEnemyEliminated;
            Enemy.AnyBullseyeHit -= OnBullseyeHit;
            Coin.AnyCollected -= OnCoinCollected;
            LevelEvent.AnyActivated -= OnEventActivated;
            PowerZone.AnyUsed -= OnZoneUsed;
            Encounter.AnyStarted -= OnEncounterStarted;
            ElementalCircle.AnyLit -= OnCircleLit;
            ElementalDoor.AnyCompleted -= OnDoorCompleted;
            Enemy.ScoredByHost = false;
            Coin.ScoredByHost = false;
        }

        public override bool TryEnterLocally()
        {
            if (world == null || characterSwitch == null || world.InWorld2D || ElementalDoor.TempleCompleted)
                return true;
            if (world.Busy)
                return false;

            WorldEntrance2D entrance = SceneOrdered<WorldEntrance2D>().FirstOrDefault();
            if (entrance == null)
                return true;

            Configure();
            world.Enter2D(entrance);
            return world.Switching || world.InWorld2D;
        }

        private void Configure()
        {
            enemies = SceneOrdered<Enemy>();
            coins = SceneOrdered<Coin>();
            levelEvents = SceneOrdered<LevelEvent>();
            zones = SceneOrdered<PowerZone>();
            encounters = SceneOrdered<Encounter>();
            circles = SceneOrdered<ElementalCircle>();
            doors = SceneOrdered<ElementalDoor>();

            mages.Clear();
            ownedMages.Clear();
            pendingActions.Clear();
            creditedBullseyes.Clear();
            creditedCoins.Clear();
            Enemy.ScoredByHost = true;
            Coin.ScoredByHost = true;
            Mage localMage = null;
            foreach (Mage mage in characterSwitch.mages)
            {
                if (mage == null || mage.data == null)
                    continue;

                mages.Add(mage);
                if (WizardIdOf(mage) == ChosenMage.Chosen)
                    localMage = mage;
            }

            if (localMage != null && world.player2D != localMage)
                characterSwitch.SwitchTo(localMage, false);

            foreach (Mage mage in mages)
            {
                bool isLocal = mage == localMage;
                bool isHostBot = OnlineMatch.IsHost && !OnlineMatch.IsHumanWizard(WizardIdOf(mage));
                ownedMages.Add(isLocal || isHostBot);
                pendingActions.Add(default);

                if (isLocal)
                    MakeOwned(mage, null);
                else if (isHostBot)
                    MakeOwned(mage, localMage);
                else
                    MakeReplica(mage);
            }

            HideOfflineHints();
            configured = true;
        }

        private static void HideOfflineHints()
        {
            var indicator = FindFirstObjectByType<MageTextIndicator>(FindObjectsInactive.Include);
            if (indicator != null)
                indicator.controls = OnlineControlsHint;

            var hud = FindFirstObjectByType<TempleHud>(FindObjectsInactive.Include);
            if (hud == null)
                return;

            foreach (Transform child in hud.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == SwitchMageHint || child.name == LeaveTempleHint)
                    child.gameObject.SetActive(false);
            }
        }

        private static void MakeOwned(Mage mage, Mage leader)
        {
            mage.IsNetworkReplica = false;
            var remote = mage.GetComponent<RemoteMageControl>();
            if (remote != null)
                remote.enabled = false;
            if (leader == null)
                return;

            var bot = mage.GetComponent<ControlBot>();
            if (bot == null)
                bot = mage.gameObject.AddComponent<ControlBot>();
            bot.leader = leader;
            bot.enabled = true;
            mage.control = bot;
        }

        private static void MakeReplica(Mage mage)
        {
            mage.IsNetworkReplica = true;
            var bot = mage.GetComponent<ControlBot>();
            if (bot != null)
                bot.enabled = false;
            var player = mage.GetComponent<PlayerControl>();
            if (player != null)
                player.enabled = false;

            var remote = mage.GetComponent<RemoteMageControl>();
            if (remote == null)
                remote = mage.gameObject.AddComponent<RemoteMageControl>();
            remote.enabled = true;
            mage.control = remote;
        }

        private void LateUpdate()
        {
            if (!configured || world == null || !world.InWorld2D || Object == null || !Object.IsValid)
                return;

            bool urgent = false;
            for (int i = 0; i < mages.Count; i++)
            {
                if (!ownedMages[i] || !mages[i].isActiveAndEnabled)
                    continue;

                MageOrders orders = mages[i].LastOrders;
                MageNetworkState pending = pendingActions[i];
                pending.Jump |= orders.jump;
                pending.Attack |= orders.attackInput;
                pending.Secondary |= orders.secondaryPower;
                pending.Zone |= orders.useZonePower;
                pendingActions[i] = pending;
                urgent |= pending.Jump || pending.Attack || pending.Secondary || pending.Zone;
            }

            if (!urgent && Time.unscaledTime < nextSend)
                return;

            nextSend = Time.unscaledTime + SendInterval;
            for (int i = 0; i < mages.Count; i++)
            {
                if (!ownedMages[i] || !mages[i].isActiveAndEnabled)
                    continue;

                Mage mage = mages[i];
                MageOrders orders = mage.LastOrders;
                MageNetworkState state = pendingActions[i];
                state.Run = orders.run;
                state.HoldJump = orders.holdJump;
                state.BowMode = mage.Mode == MageMode.Bow;
                state.Dead = !mage.Alive;
                RPC_MageState(i, mage.transform.position, mage.Speed, orders.aim, orders.move, mage.Health, state.PackFlags());
                pendingActions[i] = default;
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_MageState(int mage, Vector2 position, Vector2 velocity, Vector2 aim, float move, float health, int flags)
        {
            if (!configured || mage < 0 || mage >= mages.Count || ownedMages[mage] || !mages[mage].isActiveAndEnabled)
                return;

            var remote = mages[mage].GetComponent<RemoteMageControl>();
            if (remote == null || !remote.enabled)
                return;

            var state = new MageNetworkState { Position = position, Velocity = velocity, Aim = aim, Move = move, Health = health };
            state.UnpackFlags(flags);
            remote.Receive(state);
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_EnemyEliminated(int index)
        {
            if (configured && IsValidIndex(enemies, index))
                ApplyRemote(() => enemies[index].EliminateFromNetwork());
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, TickAligned = false)]
        public void RPC_ClaimCoin(int index, int mage)
        {
            if (configured && IsValidIndex(coins, index) && IsValidIndex(mages, mage) && creditedCoins.Add(index))
                RPC_CoinCredited(index, mage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, TickAligned = false)]
        public void RPC_CoinCredited(int index, int mage)
        {
            if (IsValidIndex(coins, index) && IsValidIndex(mages, mage))
                coins[index].CreditFromNetwork(mages[mage]);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, TickAligned = false)]
        public void RPC_ClaimBullseye(int index, int mage)
        {
            if (configured && IsValidIndex(enemies, index) && IsValidIndex(mages, mage) && creditedBullseyes.Add(index))
                RPC_BullseyeCredited(mage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, TickAligned = false)]
        public void RPC_BullseyeCredited(int mage)
        {
            if (IsValidIndex(mages, mage))
                Enemy.AddBullseye(mages[mage].data);
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_EventActivated(int index)
        {
            if (configured && IsValidIndex(levelEvents, index))
                ApplyRemote(() => levelEvents[index].Activate());
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_ZoneUsed(int index)
        {
            if (configured && IsValidIndex(zones, index))
                ApplyRemote(() => zones[index].Activate());
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_EncounterStarted(int index)
        {
            if (configured && world.InWorld2D && IsValidIndex(encounters, index))
                ApplyRemote(() => encounters[index].Begin());
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_CircleLit(int index)
        {
            if (configured && IsValidIndex(circles, index))
                ApplyRemote(() => circles[index].LightFromNetwork());
        }

        [Rpc(RpcSources.All, RpcTargets.All, TickAligned = false)]
        public void RPC_DoorCompleted(int index)
        {
            if (configured && IsValidIndex(doors, index))
                ApplyRemote(() => doors[index].CompleteFromNetwork());
        }

        private void OnEnemyEliminated(Enemy enemy) => Broadcast(enemies, enemy, RPC_EnemyEliminated);
        private void OnEventActivated(LevelEvent levelEvent) => Broadcast(levelEvents, levelEvent, RPC_EventActivated);
        private void OnZoneUsed(PowerZone zone) => Broadcast(zones, zone, RPC_ZoneUsed);
        private void OnEncounterStarted(Encounter encounter) => Broadcast(encounters, encounter, RPC_EncounterStarted);
        private void OnCircleLit(ElementalCircle circle) => Broadcast(circles, circle, RPC_CircleLit);
        private void OnDoorCompleted(ElementalDoor door) => Broadcast(doors, door, RPC_DoorCompleted);

        private void OnCoinCollected(Coin coin, Mage mage)
        {
            if (!CanBroadcast)
                return;

            int coinIndex = coins.IndexOf(coin);
            int mageIndex = mages.IndexOf(mage);
            if (coinIndex >= 0 && mageIndex >= 0)
                RPC_ClaimCoin(coinIndex, mageIndex);
        }

        private void OnBullseyeHit(Enemy enemy, Mage mage)
        {
            if (!CanBroadcast)
                return;

            int enemyIndex = enemies.IndexOf(enemy);
            int mageIndex = mages.IndexOf(mage);
            if (enemyIndex >= 0 && mageIndex >= 0)
                RPC_ClaimBullseye(enemyIndex, mageIndex);
        }

        private bool CanBroadcast => configured && !applyingRemote && Object != null && Object.IsValid;

        private void Broadcast<T>(List<T> list, T item, Action<int> send)
        {
            if (!CanBroadcast)
                return;

            int index = list.IndexOf(item);
            if (index >= 0)
                send(index);
        }

        private void ApplyRemote(Action action)
        {
            applyingRemote = true;
            try
            {
                action();
            }
            finally
            {
                applyingRemote = false;
            }
        }

        private void OnBackTo3D()
        {
            if (!configured)
                return;

            configured = false;
            if (HasStateAuthority)
                OnlineTrials.Finished(Trial);
        }

        private static string WizardIdOf(Mage mage)
        {
            return ChosenMage.Id(mage.data.element);
        }

        private static bool IsValidIndex<T>(List<T> list, int index)
        {
            return index >= 0 && index < list.Count && list[index] != null;
        }

        private static List<T> SceneOrdered<T>() where T : Component
        {
            return FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(HierarchyKey, StringComparer.Ordinal)
                .ToList();
        }

        private static string HierarchyKey(Component component)
        {
            var parts = new List<string>();
            for (Transform current = component.transform; current != null; current = current.parent)
                parts.Add(current.name + "#" + SameNameIndex(current));
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static int SameNameIndex(Transform item)
        {
            int index = 0;
            if (item.parent != null)
            {
                for (int i = 0; i < item.GetSiblingIndex(); i++)
                {
                    if (item.parent.GetChild(i).name == item.name)
                        index++;
                }

                return index;
            }

            foreach (GameObject root in item.gameObject.scene.GetRootGameObjects())
            {
                if (root.transform == item)
                    break;
                if (root.name == item.name)
                    index++;
            }

            return index;
        }
    }
}
