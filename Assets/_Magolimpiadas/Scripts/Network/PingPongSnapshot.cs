using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public struct PingPongBall : INetworkStruct
    {
        public int LaunchId;
        public NetworkBool Active;
        public NetworkBool TowardMages;
        public NetworkBool Power;
        public int Color;
        public Vector2 From;
        public Vector2 To;
        public float Duration;
        public float Elapsed;
        public float Peak;
    }

    public class PingPongSnapshot
    {
        public const int MaxBalls = 4;
        public const int MaxMages = 4;

        public readonly PingPongBall[] Balls = new PingPongBall[MaxBalls];
        public readonly float[] PaddleY = new float[MaxMages];
        public readonly int[] Swings = new int[MaxMages];

        public float GuardianY;
        public int GuardianAnimation;
        public int GuardianAnimationId;
        public int MagesScore;
        public int GuardianScore;
        public float Charge;
        public int Phase;
        public int Attempts;
        public int Outcome;
        public int OutcomeId;
        public string Message = string.Empty;
        public bool HasData;
    }
}
