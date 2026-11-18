using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class MageAvatarState : NetworkBehaviour
    {
        private MageBody body;
        private RemotePlayerPrey prey;

        [Networked] public int MageIndex { get; set; }
        [Networked] public float MoveSpeed { get; private set; }
        [Networked] public NetworkBool Grounded { get; private set; }
        [Networked] public NetworkBool Visible { get; private set; }
        [Networked] public NetworkBool HasTorch { get; private set; }
        [Networked] public NetworkBool Huntable { get; private set; }

        public bool IsBot => !Object.InputAuthority.IsRealPlayer;
        public Transform BodyRoot => TryGetBody(out MageBody found) ? found.Root : transform;

        private bool DrivesBodyLocally => HasInputAuthority || (HasStateAuthority && IsBot);

        public override void Spawned()
        {
            Visible = true;
            if (HasStateAuthority && !IsBot && !HasInputAuthority)
            {
                prey = new RemotePlayerPrey(this);
                MinotaurPreyRegistry.Register(prey);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (prey != null)
                MinotaurPreyRegistry.Unregister(prey);
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out MagePose pose))
            {
                ApplyToState(pose);
                return;
            }

            if (HasStateAuthority && IsBot && TryGetBody(out MageBody botBody))
                ApplyToState(botBody.ReadPose());
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        public void RPC_Catch(float respawnSeconds)
        {
            LocalPlayerPrey.CatchLocalPlayer(FindFirstObjectByType<PlayerMage>(), respawnSeconds);
        }

        private void LateUpdate()
        {
            if (Object == null || !Object.IsValid || DrivesBodyLocally || !MageBody.WorldIsFree || !TryGetBody(out MageBody puppet))
                return;

            puppet.BecomePuppet();
            puppet.ApplyPose(transform.position, transform.rotation, new MagePose
            {
                MoveSpeed = MoveSpeed,
                Grounded = Grounded,
                Visible = Visible,
                HasTorch = HasTorch,
            });
        }

        private void ApplyToState(MagePose pose)
        {
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            MoveSpeed = pose.MoveSpeed;
            Grounded = pose.Grounded;
            Visible = pose.Visible;
            HasTorch = pose.HasTorch;
            Huntable = pose.Huntable;
        }

        private bool TryGetBody(out MageBody found)
        {
            if (body == null || !body.IsAlive)
                body = MageBody.Find(WizardId());
            found = body;
            return found != null;
        }

        private string WizardId()
        {
            MageRoster roster = MageRoster.For(Runner);
            return roster != null && roster.TryGetMage(MageIndex, out WizardData mage) ? mage.wizardId : null;
        }
    }
}
