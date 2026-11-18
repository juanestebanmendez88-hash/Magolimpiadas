namespace ArcaneGames.Network
{
    public static class OnlineTrials
    {
        public static bool TryRequest(OnlineTrial trial)
        {
            TrialDirector director = TrialDirector.Current;
            if (!OnlineMatch.IsActive || director == null)
                return false;

            director.RPC_RequestStart(trial);
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
