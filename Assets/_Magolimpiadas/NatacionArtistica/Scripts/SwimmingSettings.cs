using UnityEngine;

[CreateAssetMenu(menuName = "Magolimpiadas/Natación artística/Ajustes", fileName = "AjustesNatacion")]
public class SwimmingSettings : ScriptableObject
{
    public float perfectWindow = 0.08f;
    public float goodWindow = 0.16f;
    [Range(0.2f, 1f)] public float windowDuringWave = 0.5f;
    public float waveBeats = 4f;
    public float audioLatency = 0f;

    public int perfectPoints = 100;
    public int goodPoints = 60;
    public int missPoints = 0;
    public int trapPoints = -80;
    public int whiteMultiplier = 2;
    public int goldenBonus = 150;
    public int hitsPerStar = 8;
    public int pointsPerStar = 100;

    public float perfectWeight = 1f;
    public float goodWeight = 0.6f;
    public float trapPenalty = 0.5f;

    [Range(0f, 1f)] public float minAccuracy = 0.6f;
    public int maxRetries = 2;
    public int countdownBeats = 4;
    public float transitionSeconds = 4f;

    public float[] owlsVariation = { -0.3f, 0.2f, 0f, 0.4f, -0.5f, 0.1f, -0.2f };
    public float goldNote = 9f;
    public float silverNote = 7.5f;
    public float bronzeNote = 6f;

    [Range(0f, 1f)] public float botAccuracy = 0.97f;
    [Range(0f, 1f)] public float botsPerfect = 0.8f;
    [Range(0f, 1f)] public float botWhiteAccuracy = 0.985f;

    public int Medal(float note) => note >= goldNote ? 0 : note >= silverNote ? 1 : note >= bronzeNote ? 2 : -1;
}
