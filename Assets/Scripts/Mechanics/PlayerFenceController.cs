using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.SceneManagement; // Needed to switch scenes.
using TMPro; // For TextMeshPro, remove if you use Unity's standard Text

namespace Platformer.Mechanics
{
    public class PlayerFenceController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7; // Max horizontal speed
        public float baseJumpTakeOffSpeed = 7; // Base jump speed
        private float jumpTakeOffSpeed; // Adjusted jump speed

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;

        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public string sceneToLoad = "Dummy";

        public Bounds Bounds => collider2d.bounds;

        public Sprite knockedDownFenceSprite; // The new sprite for the knocked-down fence

        private bool isRecoveringSpeed = false; // Bandera para indicar si se está recuperando la velocidad
        public TMP_Text timeText; // Assign this in the Inspector if using TextMeshPro
        private float elapsedTime = 0f; // Stores the total time elapsed

        public void Jump()
        {
            if (IsGrounded)
            {
                Debug.Log("PlayerFenceController: Saltando...");
                jumpTakeOffSpeed = baseJumpTakeOffSpeed;
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
            }
        }

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            // Cargar el sprite desde la carpeta Resources
            knockedDownFenceSprite = Resources.Load<Sprite>("Sprites/knockedDownFenceSprite");

            // Verificar si el sprite fue cargado correctamente
            if (knockedDownFenceSprite != null)
            {
                Debug.Log("Sprite knockedDownFenceSprite cargado exitosamente.");
            }
            else
            {
                Debug.LogError("Error: El sprite knockedDownFenceSprite no se pudo cargar. Verifica la ruta.");
            }
        }

        protected override void Update()
        {
            elapsedTime += Time.deltaTime;

            // Optional: Update the displayed time
            if (timeText != null)
            {
                timeText.text = "Time: " + FormatTime(elapsedTime) + "\nScore: " + (60 - elapsedTime);
            }

            if (controlEnabled)
            {
                // Move forward only if not recovering speed
                if (!isRecoveringSpeed)
                {
                    move.x = 1;
                }

                // Handle jump input
                if (Input.GetButtonDown("Jump") && IsGrounded)
                {
                    // Jump once when spacebar is pressed (no accumulation)
                    jumpTakeOffSpeed = baseJumpTakeOffSpeed;
                    velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                }

                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                }
            }
            else
            {
                move.x = 0; // Stop movement when control is disabled
            }

            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            // Update sprite and animations
            spriteRenderer.flipX = false;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        // Coroutine to recover speed gradually
        IEnumerator RecoverSpeed()
        {
            isRecoveringSpeed = true; // Start recovering speed
            float recoveryDuration = 3f; // Time to fully recover speed
            float recoveryTime = 0f;

            while (recoveryTime < recoveryDuration)
            {
                // Linearly interpolate speed from 0 to maxSpeed over recoveryDuration
                float speedFactor = recoveryTime / recoveryDuration;
                move.x = Mathf.Lerp(0, 1, speedFactor); // Gradually increase movement speed

                recoveryTime += Time.deltaTime;
                yield return null;
            }

            // Ensure speed is set to maximum at the end
            move.x = 1;
            isRecoveringSpeed = false; // Speed recovery complete
        }

        // Handle collisions with the fence
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Fence"))
            {
                // Print collision message
                Debug.Log("Collided with fence");

                // Obtener el SpriteRenderer y Collider de la cerca con la que colisionamos
                SpriteRenderer fenceSpriteRenderer = other.GetComponent<SpriteRenderer>();
                Collider2D fenceCollider = other.GetComponent<Collider2D>();

                // Cambiar el sprite de la cerca derribada
                if (fenceSpriteRenderer != null && knockedDownFenceSprite != null)
                {
                    fenceSpriteRenderer.sprite = knockedDownFenceSprite;

                    // Ajustar la escala de la cerca (escala x = 3, escala y = 2)
                    other.transform.localScale = new Vector3(0.35f, 0.35f, other.transform.localScale.z);

                    // Ajustar la posición en el eje Y a -0.6 (sin modificar el eje X ni Z)
                    other.transform.position = new Vector3(other.transform.position.x, -1f, other.transform.position.z);

                    // Disable the fence's collider to allow the player to pass through
                    if (fenceCollider != null)
                    {
                        fenceCollider.enabled = false;
                    }

                    // Re-enable player control after the fence is knocked down
                    controlEnabled = true;

                    // Stop the player's movement and start recovering speed
                    move.x = 0;
                    StartCoroutine(RecoverSpeed()); // Start the speed recovery process
                }
                else
                {
                    Debug.LogError("Error: fenceSpriteRenderer o knockedDownFenceSprite es nulo.");
                }
            }
            if(other.CompareTag("JumpDetector"))
            {
                StartCoroutine(SwitchSceneAfterDelay(2f));
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }

        IEnumerator SwitchSceneAfterDelay(float delay)
        {
            Time.timeScale = 0;
            // Reset time scale to normal before changing the scene.
            yield return new WaitForSecondsRealtime(delay);
            Time.timeScale = 1;
            // Switch to the specified scene (GameOverScene in this case).
            SceneManager.LoadScene(sceneToLoad);
        }
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}