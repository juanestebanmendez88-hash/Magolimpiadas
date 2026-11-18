using UnityEngine;

namespace ArcaneGames.Network
{
    public struct MageNetworkState
    {
        private const int RunFlag = 1 << 0;
        private const int HoldJumpFlag = 1 << 1;
        private const int JumpFlag = 1 << 2;
        private const int AttackFlag = 1 << 3;
        private const int SecondaryFlag = 1 << 4;
        private const int ZoneFlag = 1 << 5;
        private const int BowModeFlag = 1 << 6;
        private const int DeadFlag = 1 << 7;

        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Aim;
        public float Move;
        public float Health;
        public bool Run;
        public bool HoldJump;
        public bool Jump;
        public bool Attack;
        public bool Secondary;
        public bool Zone;
        public bool BowMode;
        public bool Dead;

        public int PackFlags()
        {
            int flags = 0;
            if (Run) flags |= RunFlag;
            if (HoldJump) flags |= HoldJumpFlag;
            if (Jump) flags |= JumpFlag;
            if (Attack) flags |= AttackFlag;
            if (Secondary) flags |= SecondaryFlag;
            if (Zone) flags |= ZoneFlag;
            if (BowMode) flags |= BowModeFlag;
            if (Dead) flags |= DeadFlag;
            return flags;
        }

        public void UnpackFlags(int flags)
        {
            Run = (flags & RunFlag) != 0;
            HoldJump = (flags & HoldJumpFlag) != 0;
            Jump = (flags & JumpFlag) != 0;
            Attack = (flags & AttackFlag) != 0;
            Secondary = (flags & SecondaryFlag) != 0;
            Zone = (flags & ZoneFlag) != 0;
            BowMode = (flags & BowModeFlag) != 0;
            Dead = (flags & DeadFlag) != 0;
        }
    }
}
