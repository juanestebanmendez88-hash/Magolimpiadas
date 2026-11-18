namespace ArcaneGames
{
    public static class GameSession
    {
        public const string SelectedWizardKey = "selected_wizard";

        public static WizardData SelectedWizard { get; set; }

        public static bool IsOnline { get; set; }
    }
}
