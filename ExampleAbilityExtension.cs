using System;
using UnityEngine;
using AdvancedSmartAIMod;

namespace AdvancedSmartAIMod.Examples
{
    // =========================================================================================================
    // 3RD-PARTY MOD EXTENSION EXAMPLE: CUSTOM ABILITIES & EVENT HOOK INTEGRATION
    // =========================================================================================================
    /// <summary>
    /// This example class demonstrates how another mod or developer can easily hook into
    /// the Advanced Smart AI NPC system to inject custom abilities, subscribe to events,
    /// or define dynamic combat conditions (e.g. Health < 50% -> Shield, Enemy Far -> Teleport).
    /// </summary>
    public class ExampleAbilityExtensionMod
    {
        public static void Main()
        {
            // -------------------------------------------------------------------------------------------------
            // METHOD 1: REGISTER GLOBAL ABILITY FACTORY
            // This ensures ANY Smart AI spawned in the game automatically receives this ability!
            // -------------------------------------------------------------------------------------------------
            AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new EnergyShieldAbility());
            AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new PhaseTeleportAbility());

            // -------------------------------------------------------------------------------------------------
            // METHOD 2: LISTEN TO GLOBAL AI SPAWN EVENTS & ATTACH CUSTOM LOGIC / LISTENERS
            // -------------------------------------------------------------------------------------------------
            AdvancedSmartAI.OnAnyAISpawned += (smartAI) =>
            {
                Debug.Log($"[ExampleMod] Custom logic attached to newly spawned AI: {smartAI.name}");

                // Example Hook 1: Listen to health changes
                smartAI.OnHealthChanged += (context, oldHp, newHp) =>
                {
                    if (newHp < 0.25f && oldHp >= 0.25f)
                    {
                        Debug.LogWarning($"[ExampleMod] AI {smartAI.name} is in critical condition! (HP: {newHp:P0})");
                    }
                };

                // Example Hook 2: Listen to target acquisition
                smartAI.OnTargetAcquired += (context, target) =>
                {
                    Debug.Log($"[ExampleMod] AI {smartAI.name} locked onto target: {target.name}");
                };

                // Example Hook 3: Listen to weapon throwing
                smartAI.OnWeaponThrown += (context, weapon, velocity) =>
                {
                    Debug.Log($"[ExampleMod] AI {smartAI.name} threw {weapon.name} at velocity {velocity}!");
                };

                // Example Hook 4: Listen to ability triggers
                smartAI.OnAbilityTriggered += (context, ability) =>
                {
                    Debug.Log($"[ExampleMod] AI {smartAI.name} triggered ability: {ability.AbilityName}");
                };
            };
        }
    }

    // =========================================================================================================
    // EXAMPLE CUSTOM ABILITY 1: ENERGY SHIELD
    // Condition: Triggered when Health < 50% and AI is taking damage or engaged in combat.
    // =========================================================================================================
    public class EnergyShieldAbility : SmartAIAbility
    {
        private GameObject shieldVisual;

        // Name, Cooldown = 15s, Duration = 4s
        public EnergyShieldAbility() : base("Energy Shield", cooldown: 15.0f, duration: 4.0f) { }

        /// <summary>
        /// Condition check: Triggers if health drops below 50% while in combat.
        /// </summary>
        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.HealthPercent < 0.50f && context.TargetEnemy != null;
        }

        public override void OnActivate(SmartAIContext context)
        {
            ModAPI.Notify($"{context.AI.name} ACTIVATED ENERGY SHIELD!");

            // Example effect: Temporarily reinforce all limb health and heal slightly
            if (context.Person != null && context.Person.Limbs != null)
            {
                foreach (var limb in context.Person.Limbs)
                {
                    if (limb != null)
                    {
                        limb.Health += 30f; // Instant shield buffer
                        limb.Numbness = 0f;
                    }
                }
            }
        }

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
            // Sustained effect: Repel fast incoming projectiles near the AI
            Collider2D[] incomingObjects = Physics2D.OverlapCircleAll(context.Position, 2.5f);
            foreach (var col in incomingObjects)
            {
                if (col.transform.root == context.Transform.root) continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null && rb.velocity.sqrMagnitude > 25f)
                {
                    // Deflect projectile backwards
                    rb.velocity = -rb.velocity * 0.8f;
                }
            }
        }

        public override void OnDeactivate(SmartAIContext context)
        {
            base.OnDeactivate(context);
            ModAPI.Notify($"{context.AI.name}'s Energy Shield expired.");
        }
    }

    // =========================================================================================================
    // EXAMPLE CUSTOM ABILITY 2: PHASE TELEPORT / BLINK
    // Condition: Triggered when Enemy is very far (> 12 units) or AI is trapped/stuck.
    // =========================================================================================================
    public class PhaseTeleportAbility : SmartAIAbility
    {
        // Name, Cooldown = 10s, Duration = instantaneous (0s)
        public PhaseTeleportAbility() : base("Phase Teleport", cooldown: 10.0f, duration: 0f) { }

        /// <summary>
        /// Condition check: Triggers if enemy is distant (> 12 units) or AI is in Moderate/Severe stuck state.
        /// </summary>
        public override bool ShouldTrigger(SmartAIContext context)
        {
            if (context.CurrentStuckLevel >= StuckLevel.Moderate) return true;
            return context.TargetEnemy != null && context.TargetDistance > 12.0f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            if (context.TargetEnemy == null && context.CurrentStuckLevel == StuckLevel.None) return;

            Vector2 destination;

            if (context.TargetEnemy != null)
            {
                // Teleport 3 units behind or in front of the enemy
                float offset = (context.TargetEnemy.transform.position.x > context.Position.x) ? -3.0f : 3.0f;
                destination = (Vector2)context.TargetEnemy.transform.position + new Vector2(offset, 1.0f);
            }
            else
            {
                // If stuck, teleport 4 units upward and forward
                destination = context.Position + new Vector2(context.Navigation.FacingDirection * 3.5f, 3.0f);
            }

            // Move all limbs to the new position safely
            Vector2 displacement = destination - context.Position;
            if (context.Person != null && context.Person.Limbs != null)
            {
                foreach (var limb in context.Person.Limbs)
                {
                    if (limb != null)
                    {
                        limb.transform.position += (Vector3)displacement;
                        if (limb.PhysicalBehaviour.rigidbody != null)
                        {
                            limb.PhysicalBehaviour.rigidbody.velocity = Vector2.zero;
                        }
                    }
                }
            }

            ModAPI.Notify($"{context.AI.name} PHASE TELEPORTED!");
        }
    }
}
