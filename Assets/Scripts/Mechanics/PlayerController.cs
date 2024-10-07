using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Platformer.Mechanics;
using System.Collections;
using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Model;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7; // Velocidad máxima horizontal
        public float baseJumpTakeOffSpeed = 7; // Velocidad inicial base del salto
        private float jumpTakeOffSpeed; // Velocidad de salto ajustada
        private bool hasJumped = false; // Para controlar si ya se ha saltado

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;

        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        public int spacebarPressCount = 0; // Contador de presiones de barra espaciadora
        public float jumpMultiplier = 0.5f; // Multiplicador de salto por cada presión de barra espaciadora
        public float horizontalJumpBoost = 5f; // Impulso horizontal adicional al saltar

        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;
        bool shouldMove = true;

        // Variables para el servidor TCP
        public int connectionPort = 25001;  // Puerto del servidor
        private Thread thread;
        private TcpListener server; // Servidor TCP
        private TcpClient client;   // Cliente TCP
        private bool running;       // Para controlar si el servidor está activo
        private string receivedInput; // Para almacenar el input recibido

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            shouldMove = true;

            // Iniciar el servidor en un hilo separado
            thread = new Thread(StartSocketServer);
            thread.IsBackground = true;
            thread.Start();

            // Iniciar el Coroutine para verificar el input
            StartCoroutine(HandleReceivedInput());
        }


        protected override void Update()
        {
            if (controlEnabled)
            {
                // El personaje siempre avanza hacia la derecha
                if (shouldMove)
                {
                    move.x = 1; // Siempre avanzamos en la dirección positiva en el eje X
                }

                // Contar las presiones de la barra espaciadora, pero solo si no ha saltado aún en esta colisión
                if (Input.GetButtonDown("Jump") && !hasJumped) // No incrementar si ya saltó en JumpDetector
                {
                    IncrementSpacebarPressCount(); // Usar el método para incrementar
                }
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true; // Marcar para detener el salto
                }
            }
            else
            {
                move.x = 0; // Si el control está deshabilitado, no se mueve
            }

            UpdateJumpState();
            base.Update();
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

        // Coroutine que revisa si hay input recibido y lo procesa
        IEnumerator HandleReceivedInput()
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(receivedInput))
                {
                    Debug.Log("Procesando input: " + receivedInput);
                    HandleInput(receivedInput);
                    receivedInput = null; // Reiniciar input para la próxima iteración
                }

                // Esperar antes de volver a consultar
            }
        }

        // Método para manejar el input recibido y aplicar acciones en Unity
        void HandleInput(string input)
        {
            // Si el input recibido es "1", realiza la acción correspondiente
            if (input == "1")
            {
                IncrementSpacebarPressCount();
                // Puedes agregar otras acciones aquí
            }
            // Si el input recibido es "0", realiza otra acción
            else if (input == "0")
            {
                Debug.Log("Recibido input '0', deteniendo acción.");
                Stop(); // Ejemplo: Detener el movimiento
            }
            else
            {
                Debug.LogWarning("Input desconocido: " + input);
            }
        }

        // Método para incrementar el contador de la barra espaciadora
        public void IncrementSpacebarPressCount()
        {
            spacebarPressCount++;
            Debug.Log("Contador de espacio incrementado: " + spacebarPressCount);
        }

        void UpdateJumpState()
        {
            bool jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;

                    // Ejecutar acción de salto directamente
                    JumpAction();

                    Debug.Log("El personaje ha saltado. Contador de saltos: " + spacebarPressCount);
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
                    spacebarPressCount = 0; // Reinicia el contador de saltos
                    hasJumped = false; // Permitir un nuevo salto
                    break;
            }

            if (jump && IsGrounded)
            {
                jumpTakeOffSpeed = baseJumpTakeOffSpeed + (spacebarPressCount * jumpMultiplier);
                velocity.x = horizontalJumpBoost; // Impulso horizontal adicional al saltar
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
                hasJumped = true; // Marcar que se ha saltado
            }
        }

        // Método para ejecutar la acción de salto
        void JumpAction()
        {
            // Aquí puedes agregar cualquier lógica adicional que necesites
            jumpTakeOffSpeed = baseJumpTakeOffSpeed + (spacebarPressCount * jumpMultiplier);
            velocity.y = jumpTakeOffSpeed * model.jumpModifier;
        }

        protected override void ComputeVelocity()
        {
            if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y *= model.jumpDeceleration; // Aplicar desaceleración al salto
                }
            }

            // Incrementa la velocidad en X con base en las presiones de la barra espaciadora
            float horizontalSpeedMultiplier = 1 + (spacebarPressCount * 0.1f); // Aumenta 10% por cada presión
            move.x = horizontalSpeedMultiplier; // Ajusta el movimiento horizontal basado en el contador

            spriteRenderer.flipX = false; // No volteamos el sprite

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("JumpDetector"))
            {
                if (!hasJumped) // Solo permitir el salto una vez
                {
                    // Forzar el salto inmediatamente al tocar la arena
                    jumpTakeOffSpeed = baseJumpTakeOffSpeed + (spacebarPressCount * jumpMultiplier);
                    velocity.y = jumpTakeOffSpeed * model.jumpModifier;

                    // Aplicar impulso horizontal adicional
                    velocity.x = horizontalJumpBoost; // Aumenta la velocidad horizontal durante el salto

                    hasJumped = true; // Marcar que se ha saltado

                    // Aquí puedes agregar código para deshabilitar la entrada de salto mientras está en el aire si es necesario
                }
            }
        }

        public void Stop()
        {
            Debug.Log("Deteniendo movimiento.");
            move.x = 0;
            shouldMove = false;
            velocity.y = 0;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }

        // Cuando la aplicación se cierra
        private void OnApplicationQuit()
        {
            running = false;
            thread?.Join();
            client?.Close();
            server?.Stop();
            Debug.Log("Servidor finalizado.");
        }
    }
}
