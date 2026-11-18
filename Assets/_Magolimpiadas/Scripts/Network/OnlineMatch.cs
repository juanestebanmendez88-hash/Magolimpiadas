using Fusion;

namespace ArcaneGames.Network
{
    public static class OnlineMatch
    {
        public static NetworkRunner Runner => GameSession.IsOnline ? SessionLauncher.Instance.Runner : null;
        public static bool IsActive => Runner != null && Runner.IsRunning;
        public static bool IsHost => IsActive && Runner.IsServer;
        public static bool IsClient => IsActive && !Runner.IsServer;

        public static bool IsHumanWizard(string wizardId)
        {
            MageRoster roster = IsActive ? MageRoster.For(Runner) : null;
            if (roster == null || string.IsNullOrEmpty(wizardId))
                return false;

            for (int i = 0; i < roster.Slots.Length; i++)
            {
                RosterSlot slot = roster.Slots[i];
                if (slot.IsTaken && roster.TryGetMage(slot.MageIndex, out WizardData mage) && mage.wizardId == wizardId)
                    return true;
            }

            return false;
        }

        public static bool IsRemoteHuman(string wizardId)
        {
            return wizardId != ChosenMage.Chosen && IsHumanWizard(wizardId);
        }

        public static bool IsLocalWizard(string wizardId)
        {
            return wizardId == ChosenMage.Chosen;
        }

        public static bool IsPuppet(string wizardId)
        {
            return IsActive && !IsLocalWizard(wizardId) && (IsClient || IsHumanWizard(wizardId));
        }

        public static bool TryGetMageIndex(string wizardId, out int mageIndex)
        {
            mageIndex = MageRoster.NoMage;
            MageRoster roster = IsActive ? MageRoster.For(Runner) : null;
            if (roster == null || string.IsNullOrEmpty(wizardId))
                return false;

            for (int i = 0; i < roster.Mages.Length; i++)
            {
                if (roster.TryGetMage(i, out WizardData mage) && mage.wizardId == wizardId)
                {
                    mageIndex = i;
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetWizardId(int mageIndex, out string wizardId)
        {
            MageRoster roster = IsActive ? MageRoster.For(Runner) : null;
            wizardId = roster != null && roster.TryGetMage(mageIndex, out WizardData mage) ? mage.wizardId : null;
            return wizardId != null;
        }

        public static string DisplayNameOf(int mageIndex)
        {
            MageRoster roster = IsActive ? MageRoster.For(Runner) : null;
            if (roster == null)
                return null;

            for (int i = 0; i < roster.Slots.Length; i++)
            {
                RosterSlot slot = roster.Slots[i];
                if (slot.IsTaken && slot.MageIndex == mageIndex && !string.IsNullOrEmpty(slot.PlayerName.Value))
                    return slot.PlayerName.Value;
            }

            return roster.TryGetMage(mageIndex, out WizardData mage) ? mage.displayName : null;
        }

        public static bool LoadSceneForEveryone(string sceneName)
        {
            return IsHost && SessionLauncher.Instance.LoadSceneForEveryone(sceneName);
        }
    }
}
