using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMage : MonoBehaviour
{
    public Animator animator;
    public Transform sceneCamera;
    public Minotaur minotaur;
    public GameObject model;
    public GameObject torch;

    public float walkSpeed = 2.2f;
    public float runSpeed = 4.5f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.1f;
    public float gravity = -20f;
    public float jumpGroundMargin = 0.15f;
    public float jumpMemory = 0.15f;
    public float fallHeight = -1.5f;
    public float fallHeightOutside = -45f;
    public Bounds labyrinthZone = new Bounds(new Vector3(-2f, 0f, 0f), new Vector3(36f, 40f, 40f));

    private CharacterController control;
    private Vector3 startPoint;
    private Vector3 respawnOutside;
    private bool hasRespawnOutside;
    private float verticalSpeed;
    private bool caught;
    private float lastGroundedTime = -10f;
    private float lastJumpRequest = -10f;
    private Vector3 platformPush;

    public bool TouchingGround
    {
        get { return Time.time - lastGroundedTime <= jumpGroundMargin; }
    }

    public bool MovingWithArrows { get; private set; }

    public void Push(Vector3 displacement)
    {
        platformPush += displacement;
    }

    public bool InLabyrinth
    {
        get
        {
            Vector3 p = transform.position;
            p.y = labyrinthZone.center.y;
            return labyrinthZone.Contains(p);
        }
    }

    public void SetRespawnOutside(Vector3 point)
    {
        respawnOutside = point;
        hasRespawnOutside = true;
    }

    public bool InShelter
    {
        get { return SafeZone.Contains(transform.position + Vector3.up); }
    }

    public bool Available
    {
        get { return !caught && enabled; }
    }

    public bool WithTorch
    {
        get { return torch != null && torch.activeSelf; }
    }

    public void DropTorch()
    {
        if (torch != null) torch.SetActive(false);
        RefreshTorchAnimation();
    }

    void RefreshTorchAnimation()
    {
        foreach (var p in animator.parameters)
            if (p.name == "ConAntorcha") { animator.SetBool("ConAntorcha", WithTorch); return; }
    }

    void Start()
    {
        control = GetComponent<CharacterController>();
        startPoint = transform.position;
        RefreshTorchAnimation();
    }

    void Update()
    {
        if (caught) return;

        if (transform.position.y < (InLabyrinth ? fallHeight : fallHeightOutside))
        {
            Respawn();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Vector3 direction = keyboard != null ? ReadDirection(keyboard) : Vector3.zero;
        bool moving = direction.sqrMagnitude > 0.01f;
        bool running = moving && keyboard.shiftKey.isPressed;

        if (moving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (control.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalSpeed < 0f) verticalSpeed = -2f;
        }

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) lastJumpRequest = Time.time;
        bool canJump = Time.time - lastGroundedTime <= jumpGroundMargin && verticalSpeed <= 0f;
        if (canJump && Time.time - lastJumpRequest <= jumpMemory)
        {
            verticalSpeed = Mathf.Sqrt(2f * jumpHeight * -gravity);
            lastJumpRequest = -10f;
            lastGroundedTime = -10f;
            animator.SetBool("EnSuelo", false);
            animator.SetTrigger("Saltar");
            GameSound.Play(GameAudio.Sound.Jump, 0.5f, Random.Range(0.95f, 1.05f));
        }

        verticalSpeed += gravity * Time.deltaTime;

        float speed = running ? runSpeed : walkSpeed;
        Vector3 movement = direction * speed + Vector3.up * verticalSpeed;
        control.Move(movement * Time.deltaTime + platformPush);
        platformPush = Vector3.zero;

        if (control.isGrounded) lastGroundedTime = Time.time;
        Steps(moving && control.isGrounded, running);
        if (moving && GameSound.HasCelebration) GameSound.SilenceCelebrations(0.6f);
        animator.SetBool("EnSuelo", control.isGrounded || (Time.time - lastGroundedTime <= jumpGroundMargin && verticalSpeed <= 0f));
        float animationValue = !moving ? 0f : running ? 2f : 1f;
        animator.SetFloat("Velocidad", animationValue, 0.1f, Time.deltaTime);
    }

    float nextStep;
    void Steps(bool walking, bool running)
    {
        if (!walking) { nextStep = Time.time + 0.12f; return; }
        if (Time.time < nextStep) return;
        nextStep = Time.time + (running ? 0.3f : 0.45f);
        GameSound.Play(GameAudio.Sound.Step, running ? 0.35f : 0.25f, Random.Range(0.9f, 1.1f), null, 0.1f);
    }

    Vector3 ReadDirection(Keyboard keyboard)
    {
        float horizontal = 0f;
        float vertical = 0f;

        MovingWithArrows = keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed
                          || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;

        Vector3 forward = sceneCamera.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = sceneCamera.right;
        right.y = 0f;
        right.Normalize();

        return Vector3.ClampMagnitude(forward * vertical + right * horizontal, 1f);
    }

    public void Teleport(Vector3 destination)
    {
        Place(destination);
        if (minotaur != null) minotaur.LoseTrack();
    }

    public void Catch(float respawnTime)
    {
        caught = true;
        control.enabled = false;
        model.SetActive(false);
        Invoke(nameof(Respawn), respawnTime);
    }

    void Respawn()
    {
        caught = false;
        model.SetActive(true);
        Place(!InLabyrinth && hasRespawnOutside ? respawnOutside : startPoint);
    }

    void Place(Vector3 position)
    {
        control.enabled = false;
        transform.position = position;
        verticalSpeed = 0f;
        control.enabled = true;
        Physics.SyncTransforms();
    }
}
