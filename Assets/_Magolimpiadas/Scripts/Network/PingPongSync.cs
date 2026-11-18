using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class PingPongSync : TrialSync
    {
        private const float PaddleSendInterval = 0.05f;

        private readonly PingPongSnapshot hostSnapshot = new PingPongSnapshot();
        private PingPongLevel level;
        private float nextPaddleSend;

        public static PingPongSync Current { get; private set; }

        public static bool IsOnline => Current != null && OnlineMatch.IsActive;

        public override OnlineTrial Trial => OnlineTrial.PingPong;

        public PingPongSnapshot Latest { get; } = new PingPongSnapshot();

        [Networked, Capacity(PingPongSnapshot.MaxBalls)] private NetworkArray<PingPongBall> Balls => default;
        [Networked, Capacity(PingPongSnapshot.MaxMages)] private NetworkArray<float> PaddleY => default;
        [Networked, Capacity(PingPongSnapshot.MaxMages)] private NetworkArray<int> Swings => default;
        [Networked] private float GuardianY { get; set; }
        [Networked] private int GuardianAnimation { get; set; }
        [Networked] private int GuardianAnimationId { get; set; }
        [Networked] private int MagesScore { get; set; }
        [Networked] private int GuardianScore { get; set; }
        [Networked] private float Charge { get; set; }
        [Networked] private int Phase { get; set; }
        [Networked] private int Attempts { get; set; }
        [Networked] private int Outcome { get; set; }
        [Networked] private int OutcomeId { get; set; }
        [Networked] private NetworkString<_128> Message { get; set; }
        [Networked] private NetworkBool Live { get; set; }

        public override void Spawned()
        {
            Current = this;
            level = FindFirstObjectByType<PingPongLevel>(FindObjectsInactive.Include);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Current == this)
                Current = null;
        }

        public override bool TryEnterLocally()
        {
            if (level == null)
                return true;

            if (!level.Playing)
                level.Enter();
            return level.Playing;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || level == null)
                return;

            Live = level.Playing;
            if (!level.Playing)
                return;

            level.CaptureNetworkState(hostSnapshot);
            for (int i = 0; i < PingPongSnapshot.MaxBalls; i++)
                Balls.Set(i, hostSnapshot.Balls[i]);
            for (int i = 0; i < PingPongSnapshot.MaxMages; i++)
            {
                PaddleY.Set(i, hostSnapshot.PaddleY[i]);
                Swings.Set(i, hostSnapshot.Swings[i]);
            }

            GuardianY = hostSnapshot.GuardianY;
            GuardianAnimation = hostSnapshot.GuardianAnimation;
            GuardianAnimationId = hostSnapshot.GuardianAnimationId;
            MagesScore = hostSnapshot.MagesScore;
            GuardianScore = hostSnapshot.GuardianScore;
            Charge = hostSnapshot.Charge;
            Phase = hostSnapshot.Phase;
            Attempts = hostSnapshot.Attempts;
            Outcome = hostSnapshot.Outcome;
            OutcomeId = hostSnapshot.OutcomeId;
            Message = hostSnapshot.Message ?? string.Empty;
        }

        public override void Render()
        {
            if (HasStateAuthority)
                return;

            for (int i = 0; i < PingPongSnapshot.MaxBalls; i++)
                Latest.Balls[i] = Balls[i];
            for (int i = 0; i < PingPongSnapshot.MaxMages; i++)
            {
                Latest.PaddleY[i] = PaddleY[i];
                Latest.Swings[i] = Swings[i];
            }

            Latest.GuardianY = GuardianY;
            Latest.GuardianAnimation = GuardianAnimation;
            Latest.GuardianAnimationId = GuardianAnimationId;
            Latest.MagesScore = MagesScore;
            Latest.GuardianScore = GuardianScore;
            Latest.Charge = Charge;
            Latest.Phase = Phase;
            Latest.Attempts = Attempts;
            Latest.Outcome = Outcome;
            Latest.OutcomeId = OutcomeId;
            Latest.Message = Message.ToString();
            Latest.HasData = Live;
        }

        public void SendPaddle(int mage, float y)
        {
            if (Time.unscaledTime < nextPaddleSend)
                return;

            nextPaddleSend = Time.unscaledTime + PaddleSendInterval;
            RPC_Paddle(mage, y);
        }

        public void SendHit(int mage, int ball, int launchId, float aimY)
        {
            RPC_Hit(mage, ball, launchId, aimY);
        }

        public void MarkFinished()
        {
            OnlineTrials.Finished(Trial);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Unreliable, TickAligned = false)]
        public void RPC_Paddle(int mage, float y)
        {
            if (level != null)
                level.SetRemotePaddle(mage, y);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, TickAligned = false)]
        public void RPC_Hit(int mage, int ball, int launchId, float aimY)
        {
            if (level != null)
                level.TryRemoteHit(mage, ball, launchId, aimY);
        }
    }
}
