public static class NewMatch
{
    public static void Restart()
    {
        PlayerMedals.RestartMatch();
        GameResults.RestartMatch();
        Coin.RestartMatch();
        Enemy.RestartMatch();
        PingPongLevel.RestartMatch();
        ElementalDoor.RestartMatch();
        ArtisticSwimmingLevel.RestartMatch();
    }
}
