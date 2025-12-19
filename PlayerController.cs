using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip walkLoop;
    public AudioClip runLoop;
    public AudioClip crouchLoop;
    [Range(0f, 1f)] public float walkVolume = 0.35f;
    [Range(0f, 1f)] public float runVolume = 0.55f;
    [Range(0f, 1f)] public float crouchVolume = 0.25f;
    public float minSpeedForSteps = 0.6f;      // dead-zone (m/s) to ignore tiny drift
    public Vector2 pitchRange = new Vector2(0.98f, 1.02f);

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private CharacterController characterController;
    private bool canMove = true;

    // footstep state
    private bool footstepsPlaying = false;
    private Coroutine fadeRoutine;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;   // <-- ensure it won't auto-play
        footstepSource.loop = true;
        footstepSource.spatialBlend = 1f;
        footstepSource.volume = 0f;
        footstepSource.clip = null;           // <-- no clip until moving
    }

    void Update()
    {
        // --- Move input ---
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float baseSpeed = isRunning ? runSpeed : walkSpeed;

        float curSpeedX = canMove ? baseSpeed * Input.GetAxis("Vertical") : 0f;
        float curSpeedY = canMove ? baseSpeed * Input.GetAxis("Horizontal") : 0f;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // Crouch (hold R)
        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
            isRunning = false; // no running while crouched
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // Look
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        // Footsteps driven by actual controller velocity
        HandleFootsteps(isRunning);
    }

    void HandleFootsteps(bool isRunning)
    {
        // Use real velocity from CharacterController
        Vector3 v = characterController.velocity;
        v.y = 0f; // ignore vertical
        float horizontalSpeed = v.magnitude;

        bool grounded = characterController.isGrounded;
        bool shouldPlay = grounded && horizontalSpeed >= minSpeedForSteps;

        if (shouldPlay)
        {
            AudioClip targetClip;
            float targetVolume;

            bool crouched = characterController.height <= crouchHeight + 0.01f;
            if (crouched)
            {
                targetClip = crouchLoop ? crouchLoop : walkLoop;
                targetVolume = crouchLoop ? crouchVolume : walkVolume;
            }
            else if (isRunning)
            {
                targetClip = runLoop ? runLoop : walkLoop;
                targetVolume = runLoop ? runVolume : walkVolume;
            }
            else
            {
                targetClip = walkLoop;
                targetVolume = walkVolume;
            }

            if (footstepSource.clip != targetClip)
                footstepSource.clip = targetClip;

            footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

            if (!footstepsPlaying)
            {
                StartFade(targetVolume, 0.06f, ensurePlay: true);
                footstepsPlaying = true;
            }
            else if (!Mathf.Approximately(footstepSource.volume, targetVolume))
            {
                StartFade(targetVolume, 0.08f, ensurePlay: true);
            }
        }
        else if (footstepsPlaying)
        {
            StartFade(0f, 0.08f, ensurePlay: false, stopOnEnd: true);
            footstepsPlaying = false;
        }
    }

    void OnDisable()
    {
        if (footstepSource != null) footstepSource.Stop();
        footstepsPlaying = false;
    }

    void StartFade(float to, float duration, bool ensurePlay, bool stopOnEnd = false)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeVolume(to, duration, ensurePlay, stopOnEnd));
    }

    IEnumerator FadeVolume(float to, float duration, bool ensurePlay, bool stopOnEnd)
    {
        if (ensurePlay && footstepSource.clip != null && !footstepSource.isPlaying)
            footstepSource.Play();

        float from = footstepSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            footstepSource.volume = Mathf.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }

        footstepSource.volume = to;

        if (stopOnEnd && Mathf.Approximately(to, 0f))
            footstepSource.Stop();

        fadeRoutine = null;
    }
}
