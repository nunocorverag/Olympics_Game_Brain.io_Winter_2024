using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Platformer.Core;
using System.Threading;
using Platformer.Mechanics;
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


        private TcpListener server;
        private TcpClient client;
        private Thread thread;
        private string receivedInput;
        public int connectionPort = 25001;
        private bool running;
        public float updateInterval = 1f;

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
        private GameManager gameManager;

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
                jumpTakeOffSpeed = baseJumpTakeOffSpeed; // Se mantiene la velocidad base
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
            }
        }

        void Awake()
        {
            gameManager = FindObjectOfType<GameManager>();
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

            // Iniciar el servidor de sockets en un hilo separado
            thread = new Thread(StartSocketServer);
            thread.IsBackground = true;
            thread.Start();

            // Iniciar la coroutine para verificar el input recibido
            StartCoroutine(HandleReceivedInput());
        }

        // Método para iniciar el servidor de sockets
        void StartSocketServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, connectionPort);
                server.Start();
                Debug.Log("Servidor TCP iniciado en el puerto: " + connectionPort);

                client = server.AcceptTcpClient();
                running = true;

                while (running)
                {
                    HandleConnection();
                }
            }
            catch (SocketException ex)
            {
                Debug.LogError("Error de socket: " + ex.Message);
            }
            finally
            {
                server?.Stop();
                Debug.Log("Servidor detenido.");
            }
        }

        // Método para manejar la conexión y recibir datos del cliente
        void HandleConnection()
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    receivedInput = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Datos recibidos: " + receivedInput);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error durante la conexión: " + ex.Message);
            }
        }

        // Coroutine para manejar el input recibido y ejecutar el salto
        IEnumerator HandleReceivedInput()
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(receivedInput))
                {
                    Debug.Log("Procesando input: " + receivedInput);
                    HandleInput(receivedInput);
                    receivedInput = null; // Reiniciar el input para la próxima iteración
                }

                // Esperar antes de volver a consultar
                yield return new WaitForSeconds(updateInterval);
            }
        }

        // Método para manejar el input recibido del socket
        void HandleInput(string input)
        {
            // Si el input recibido es "1", se ejecuta la acción de salto
            if (input == "1")
            {
                Jump();
            }
            else
            {
                Debug.LogWarning("Input desconocido: " + input);
            }
        }


        protected override void Update()
        {
            elapsedTime += Time.deltaTime;

            // Actualizar el tiempo mostrado (opcional)
            if (timeText != null)
            {
                timeText.text = "Time: " + FormatTime(elapsedTime) + "\nScore: " + (60 - elapsedTime);
            }

            if (controlEnabled)
            {
                // Mover hacia adelante
                move.x = 1;

                // Saltar si el jugador está en el suelo y se presiona el botón de salto
                if (Input.GetButtonDown("Jump") && IsGrounded)
                {
                    Jump(); // Se llamará al método Jump()
                }
            }
            else
            {
                move.x = 0; // Detener movimiento cuando el control está deshabilitado
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
            gameManager.AddScore(60 - elapsedTime);
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