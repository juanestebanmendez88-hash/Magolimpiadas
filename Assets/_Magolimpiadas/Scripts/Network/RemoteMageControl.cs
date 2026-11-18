using UnityEngine;

namespace ArcaneGames.Network
{
    [RequireComponent(typeof(Mage))]
    public class RemoteMageControl : MageControl
    {
        private const float SnapDistance = 3f;
        private const float CorrectionRate = 10f;
        private const float MaxPrediction = 0.25f;

        private Mage mage;
        private Rigidbody2D body;
        private MageOrders held;
        private bool jumpQueued;
        private bool attackQueued;
        private bool secondaryQueued;
        private bool zoneQueued;
        private bool hasTarget;
        private Vector2 targetPosition;
        private Vector2 targetVelocity;
        private float receivedAt;

        private void Awake()
        {
            mage = GetComponent<Mage>();
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            hasTarget = false;
            jumpQueued = attackQueued = secondaryQueued = zoneQueued = false;
        }

        public override MageOrders ReadOrders(Mage target)
        {
            MageOrders orders = held;
            orders.jump = jumpQueued;
            orders.attackInput = attackQueued;
            orders.secondaryPower = secondaryQueued;
            orders.useZonePower = zoneQueued;
            orders.changeMode = false;
            jumpQueued = attackQueued = secondaryQueued = zoneQueued = false;
            return orders;
        }

        public void Receive(MageNetworkState state)
        {
            if (mage == null)
                return;

            if (state.BowMode != (mage.Mode == MageMode.Bow) && !mage.Attacking)
                mage.ChangeMode();

            held = new MageOrders
            {
                move = state.Move,
                run = state.Run,
                holdJump = state.HoldJump,
                aim = state.Aim,
            };

            if (state.Dead)
                jumpQueued = attackQueued = secondaryQueued = zoneQueued = false;
            else
            {
                jumpQueued |= state.Jump;
                attackQueued |= state.Attack;
                secondaryQueued |= state.Secondary;
                zoneQueued |= state.Zone;
            }

            mage.ApplyNetworkHealth(state.Health, state.Dead);
            targetPosition = state.Position;
            targetVelocity = state.Velocity;
            receivedAt = Time.time;
            hasTarget = true;

            if (!state.Dead && mage.Alive)
                body.linearVelocity = state.Velocity;
        }

        private void FixedUpdate()
        {
            if (!hasTarget || mage == null || !mage.Alive)
                return;

            float age = Mathf.Min(Time.time - receivedAt, MaxPrediction);
            Vector2 predicted = targetPosition + targetVelocity * age;
            Vector2 error = predicted - body.position;
            if (error.magnitude > SnapDistance)
            {
                mage.Place(predicted);
                body.linearVelocity = targetVelocity;
                return;
            }

            body.position += error * Mathf.Clamp01(CorrectionRate * Time.fixedDeltaTime);
        }
    }
}
