using System.Text;
using UnityEngine;

namespace ArcaneGames.Network
{
    public static class RoomCodeRules
    {
        public const int Length = 4;
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ";

        public static string Generate()
        {
            var code = new StringBuilder(Length);
            for (int i = 0; i < Length; i++)
                code.Append(Alphabet[Random.Range(0, Alphabet.Length)]);
            return code.ToString();
        }

        public static string Normalize(string rawCode)
        {
            return string.IsNullOrWhiteSpace(rawCode) ? string.Empty : rawCode.Trim().ToUpperInvariant();
        }

        public static bool IsValid(string code)
        {
            if (code == null || code.Length != Length)
                return false;

            foreach (char letter in code)
            {
                if (Alphabet.IndexOf(letter) < 0)
                    return false;
            }

            return true;
        }
    }
}
