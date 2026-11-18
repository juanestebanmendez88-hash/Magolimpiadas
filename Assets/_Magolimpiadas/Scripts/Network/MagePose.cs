using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public struct MagePose : INetworkInput
    {
        public Vector3 Position;
        public float Yaw;
        public float MoveSpeed;
        public NetworkBool Grounded;
        public NetworkBool Visible;
        public NetworkBool HasTorch;
        public NetworkBool Huntable;

        public Quaternion Rotation => Quaternion.Euler(0f, Yaw, 0f);
    }
}
