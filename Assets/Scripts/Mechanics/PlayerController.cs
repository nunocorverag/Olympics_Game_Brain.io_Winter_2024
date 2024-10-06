using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using System;

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

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                // El personaje siempre avanza hacia la derecha
                move.x = 1; // Siempre avanzamos en la dirección positiva en el eje X

                // Contar las presiones de la barra espaciadora
                if (Input.GetButtonDown("Jump"))
                {
                    spacebarPressCount++; // Incrementa el contador cada vez que se presiona la barra espaciadora
                }

                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0; // Si el control está deshabilitado, no se mueve
            }

            UpdateJumpState();
            base.Update();
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
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    // Reseteamos el contador de presiones y la variable hasJumped cuando aterriza
                    spacebarPressCount = 0;
                    hasJumped = false; // Permitir un nuevo salto
                    break;
            }

            if (jump && IsGrounded)
            {
                // La fuerza de salto se ajusta en función de la cantidad de veces que se presionó la barra espaciadora
                jumpTakeOffSpeed = baseJumpTakeOffSpeed + (spacebarPressCount * jumpMultiplier);

                // Aplicar impulso horizontal adicional
                velocity.x = horizontalJumpBoost; // Aumenta la velocidad horizontal durante el salto

                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
                hasJumped = true; // Marcar que se ha saltado
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

            // Actualización del sprite y animaciones
            spriteRenderer.flipX = false; // Como siempre avanza a la derecha, no volteamos el sprite

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        // Detectar si está cerca de la arena para permitir saltar
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
                }
            }
        }

        // Cuando salga de la arena, ya no puede saltar
        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("JumpDetector"))
            {
                // Aquí no es necesario hacer nada, ya que el salto se controla solo con hasJumped
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

        public static implicit operator PlayerController(PlayerFenceController v)
        {
            throw new NotImplementedException();
        }
    }
}