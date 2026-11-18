using System.Collections.Generic;
using UnityEngine;

namespace ArcaneGames.Network
{
    public class MageBody
    {
        private const string SpeedParameter = "Velocidad";
        private const string GroundedParameter = "EnSuelo";
        private const string JumpParameter = "Saltar";
        private const string TorchParameter = "ConAntorcha";
        private const float SpeedDamping = 0.1f;

        private readonly BotMage3D bot;
        private readonly PlayerMage player;
        private readonly Renderer[] renderers;
        private readonly Renderer skin;
        private readonly HashSet<string> animatorParameters = new HashSet<string>();
        private bool wasGrounded = true;

        private MageBody(Transform root, Animator animator, GameObject torch, BotMage3D bot, PlayerMage player)
        {
            Root = root;
            Animator = animator;
            Torch = torch;
            this.bot = bot;
            this.player = player;
            renderers = root.GetComponentsInChildren<Renderer>(true);
            if (animator == null)
                return;

            skin = animator.GetComponentInChildren<SkinnedMeshRenderer>(true);

            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animatorParameters.Add(parameter.name);
        }

        public Transform Root { get; }
        public Animator Animator { get; }
        public GameObject Torch { get; }
        public bool IsAlive => Root != null;

        public static MageBody Find(string wizardId)
        {
            if (string.IsNullOrEmpty(wizardId))
                return null;

            if (wizardId == ChosenMage.Chosen)
            {
                var localPlayer = Object.FindFirstObjectByType<PlayerMage>();
                return localPlayer != null ? new MageBody(localPlayer.transform, localPlayer.animator, localPlayer.torch, null, localPlayer) : null;
            }

            foreach (BotMage3D candidate in Object.FindObjectsByType<BotMage3D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.mageId == wizardId)
                    return new MageBody(candidate.transform, candidate.animator, candidate.torch, candidate, null);
            }

            return null;
        }

        public MagePose ReadPose()
        {
            return new MagePose
            {
                Position = Root.position,
                Yaw = Root.eulerAngles.y,
                MoveSpeed = HasParameter(SpeedParameter) ? Animator.GetFloat(SpeedParameter) : 0f,
                Grounded = !HasParameter(GroundedParameter) || Animator.GetBool(GroundedParameter),
                Visible = IsBodyShown(),
                HasTorch = Torch != null && Torch.activeSelf,
                Huntable = player != null && player.Available && !player.InShelter,
            };
        }

        public static bool WorldIsFree
        {
            get
            {
                WorldSwitcher world = WorldSwitcher.Instance;
                PodiumCeremony ceremony = PodiumCeremony.Instance;
                return (world == null || !world.Busy) && (ceremony == null || !ceremony.InProgress) && !SwimmingRace.AnyRunning && !IslandCinematic.AnyInProgress;
            }
        }

        public void BecomePuppet()
        {
            if (bot == null || (!bot.enabled && !bot.Control.enabled))
                return;

            bot.state = BotMage3D.State.Cinematic;
            bot.enabled = false;
            bot.Control.enabled = false;
        }

        public void ApplyPose(Vector3 position, Quaternion rotation, MagePose pose)
        {
            Root.SetPositionAndRotation(position, rotation);
            ApplyAnimation(pose);
            ApplyVisibility(pose.Visible);

            if (Torch != null && Torch.activeSelf != pose.HasTorch)
                Torch.SetActive(pose.HasTorch);
        }

        private void ApplyAnimation(MagePose pose)
        {
            if (Animator == null)
                return;

            if (HasParameter(SpeedParameter))
                Animator.SetFloat(SpeedParameter, pose.MoveSpeed, SpeedDamping, Time.deltaTime);
            if (HasParameter(GroundedParameter))
                Animator.SetBool(GroundedParameter, pose.Grounded);
            if (HasParameter(TorchParameter))
                Animator.SetBool(TorchParameter, pose.HasTorch);
            if (wasGrounded && !pose.Grounded && HasParameter(JumpParameter))
                Animator.SetTrigger(JumpParameter);
            wasGrounded = pose.Grounded;
        }

        private void ApplyVisibility(bool visible)
        {
            foreach (Renderer bodyRenderer in renderers)
            {
                if (bodyRenderer != null && bodyRenderer.forceRenderingOff == visible)
                    bodyRenderer.forceRenderingOff = !visible;
            }
        }

        private bool IsBodyShown()
        {
            if (Animator == null)
                return true;
            return Animator.gameObject.activeInHierarchy && (skin == null || !skin.forceRenderingOff);
        }

        private bool HasParameter(string parameterName)
        {
            return Animator != null && animatorParameters.Contains(parameterName);
        }
    }
}
