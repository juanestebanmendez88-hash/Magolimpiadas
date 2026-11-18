namespace ArcaneGames.Network
{
    public static class OnlineTrials
    {
        public static bool TryRequest(OnlineTrial trial)
        {
            TrialDirector director = TrialDirector.Current;
            if (!OnlineMatch.IsActive || director == null)
                return false;

            if (OnlineMatch.TryGetMageIndex(ChosenMage.Chosen, out int mageIndex))
                director.RPC_ReportArrival(trial, mageIndex);
            return true;
        }

        public static void Finished(OnlineTrial trial)
        {
            TrialDirector director = TrialDirector.Current;
            if (director != null && director.HasStateAuthority)
                director.Finish(trial);
        }
    }
}
