using Fusion;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class MinotaurSync : NetworkBehaviour
    {
        private const string ChasingParameter = "Persiguiendo";
        private const string CatchParameter = "Atrapar";

        [SerializeField] private Minotaur minotaur;

        private int shownCatches;
        private int shownRoars;
        private PropertyReader<Vector3> positionReader;
        private PropertyReader<Quaternion> rotationReader;

        [Networked] public Vector3 Position { get; private set; }
        [Networked] public Quaternion Rotation { get; private set; }
        [Networked] public NetworkBool IsChasing { get; private set; }
        [Networked] public float AnimationSpeed { get; private set; }
        [Networked] public int Catches { get; private set; }
        [Networked] public int Roars { get; private set; }

        public override void Spawned()
        {
            if (minotaur == null)
                minotaur = GetComponent<Minotaur>();
            positionReader = GetPropertyReader<Vector3>(nameof(Position));
            rotationReader = GetPropertyReader<Quaternion>(nameof(Rotation));

            if (HasStateAuthority)
            {
                minotaur.ChaseStarted += CountRoar;
                minotaur.PreyCaught += CountCatch;
                return;
            }

            minotaur.enabled = false;
            minotaur.Agent.enabled = false;
            shownCatches = Catches;
            shownRoars = Roars;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (minotaur == null)
                return;

            minotaur.ChaseStarted -= CountRoar;
            minotaur.PreyCaught -= CountCatch;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            Position = transform.position;
            Rotation = transform.rotation;
            IsChasing = minotaur.IsChasing;
            AnimationSpeed = minotaur.Animator.speed;
        }

        public override void Render()
        {
            if (HasStateAuthority)
                return;

            ApplyInterpolatedPose();
            Animator animator = minotaur.Animator;
            animator.SetBool(ChasingParameter, IsChasing);
            animator.speed = AnimationSpeed;

            if (Catches != shownCatches)
            {
                shownCatches = Catches;
                animator.SetTrigger(CatchParameter);
            }

            if (Roars != shownRoars)
            {
                shownRoars = Roars;
                GameSound.PlayAt(GameAudio.Sound.MinotaurRoar, transform.position);
            }
        }

        private void ApplyInterpolatedPose()
        {
            if (!TryGetSnapshotsBuffers(out NetworkBehaviourBuffer from, out NetworkBehaviourBuffer to, out float alpha))
                return;

            var (fromPosition, toPosition) = positionReader.Read(from, to);
            var (fromRotation, toRotation) = rotationReader.Read(from, to);
            transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, alpha), Quaternion.Slerp(fromRotation, toRotation, alpha));
        }

        private void CountRoar()
        {
            Roars++;
        }

        private void CountCatch()
        {
            Catches++;
        }
    }
}
