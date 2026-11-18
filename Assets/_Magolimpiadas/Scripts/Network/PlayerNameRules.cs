namespace ArcaneGames.Network
{
    public static class PlayerNameRules
    {
        public const int MaxLength = 16;

        public static string Normalize(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();
        }

        public static bool IsValid(string playerName)
        {
            string name = Normalize(playerName);
            if (name.Length == 0 || name.Length > MaxLength)
                return false;

            foreach (char character in name)
            {
                if (!char.IsLetterOrDigit(character) && character != ' ' && character != '_' && character != '-')
                    return false;
            }

            return true;
        }
    }
}
