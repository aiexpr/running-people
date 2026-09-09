using System;
using UnityEngine;
<<<<<<< HEAD
using AdvancedSmartAIMod;

namespace AdvancedSmartAIMod.Examples
{
    // =========================================================================================================
    // 3RD-PARTY MOD EXTENSION EXAMPLE: CUSTOM ABILITIES & CROSS-MOD SYNERGY
    // Compatible with vanilla People Playground, Human Tiers (Reforged), and Beyond Nowhere
=======

namespace AdvancedSmartAIMod
{
    // =========================================================================================================
    // EXAMPLE 3RD-PARTY ABILITY EXTENSION (SAFE PASSIVE & STATUS ABILITIES)
>>>>>>> 0b62996 (Fix weapon holding alignment, ignore self-collisions to prevent crushing damage, and remove physics abilities)
    // =========================================================================================================
    public class ExampleAbilityExtensionMod
    {
        public static void Main()
        {
<<<<<<< HEAD
            // -------------------------------------------------------------------------------------------------
            // METHOD 1: REGISTER GLOBAL ABILITY FACTORIES
            // -------------------------------------------------------------------------------------------------
            AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new EnergyShieldAbility());
            AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new PhaseTeleportAbility());
            AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new TieredVoidBurstAbility());

            // -------------------------------------------------------------------------------------------------
            // METHOD 2: LISTEN TO GLOBAL AI SPAWN EVENTS & ATTACH CUSTOM LISTENERS
            // -------------------------------------------------------------------------------------------------
            AdvancedSmartAI.OnAnyAISpawned += (smartAI) =>
            {
                Debug.Log("[ExampleMod] Custom logic attached to AI: " + smartAI.name + " (Tier: " + smartAI.DetectedTier + ")");

                // Hook 1: Listen to health changes
                smartAI.OnHealthChanged += (context, oldHp, newHp) =>
                {
                    if (newHp < 0.25f && oldHp >= 0.25f)
                    {
                        Debug.LogWarning("[ExampleMod] AI " + smartAI.name + " is in critical condition! (HP: " + (int)(newHp * 100) + "%)");
                    }
                };

                // Hook 2: Listen to target acquisition
                smartAI.OnTargetAcquired += (context, target) =>
                {
                    int enemyTier = CrossModAdapterEngine.DetectEntityTier(target);
                    string tierNote = enemyTier >= 0 ? " [Enemy Tier " + enemyTier + "]" : "";
                    Debug.Log("[ExampleMod] AI " + smartAI.name + " locked onto: " + target.name + tierNote);
                };

                // Hook 3: Listen to weapon throwing
                smartAI.OnWeaponThrown += (context, weapon, velocity) =>
                {
                    Debug.Log("[ExampleMod] AI " + smartAI.name + " hurled " + weapon.name + " at " + velocity + "!");
                };
            };
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 1: ENERGY SHIELD (Health < 50%)
    // =========================================================================================================
    public class EnergyShieldAbility : SmartAIAbility
    {
        public EnergyShieldAbility() : base("Energy Shield", 14.0f, 4.0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.HealthPercent < 0.50f && context.TargetEnemy != null;
=======
            // Register an example passive ability with the Advanced Smart AI global factory
            AdvancedSmartAI.RegisterGlobalAbilityFactory((ai) => new PassiveCellularRegenerationAbility());

            ModAPI.Notify("Advanced Smart AI: Example Ability Extension loaded!");
        }
    }

    /// <summary>
    /// A safe passive regeneration ability that gently mends injured limbs over time without applying physics impulses.
    /// </summary>
    public class PassiveCellularRegenerationAbility : SmartAIAbility
    {
        public PassiveCellularRegenerationAbility() : base("Passive Cellular Regeneration", 10.0f, 3.0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.HealthPercent < 0.85f;
>>>>>>> 0b62996 (Fix weapon holding alignment, ignore self-collisions to prevent crushing damage, and remove physics abilities)
        }

        public override void OnActivate(SmartAIContext context)
        {
<<<<<<< HEAD
            ModAPI.Notify(context.AI.name + " ACTIVATED ENERGY SHIELD!");
=======
            ModAPI.Notify(context.AI.name + " initiated Cellular Regeneration.");
        }

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
>>>>>>> 0b62996 (Fix weapon holding alignment, ignore self-collisions to prevent crushing damage, and remove physics abilities)
            if (context.Person != null && context.Person.Limbs != null)
            {
                LimbBehaviour[] limbs = context.Person.Limbs;
                for (int i = 0; i < limbs.Length; i++)
                {
                    LimbBehaviour limb = limbs[i];
<<<<<<< HEAD
                    if (limb != null)
                    {
                        limb.Health += 35f;
=======
                    if (limb != null && limb.Health < (100f * context.Config.HealthMultiplier))
                    {
                        limb.Health = Mathf.Min(100f * context.Config.HealthMultiplier, limb.Health + (8f * deltaTime));
>>>>>>> 0b62996 (Fix weapon holding alignment, ignore self-collisions to prevent crushing damage, and remove physics abilities)
                        limb.Numbness = 0f;
                    }
                }
            }
        }
<<<<<<< HEAD

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
            // Deflect fast incoming hostile projectiles (excluding self)
            Collider2D[] incoming = Physics2D.OverlapCircleAll(context.Position, 2.6f);
            for (int i = 0; i < incoming.Length; i++)
            {
                Collider2D col = incoming[i];
                if (col == null || context.AI.IsOwnLimb(col)) continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null && rb.velocity.sqrMagnitude > 25f)
                {
                    rb.velocity = -rb.velocity * 0.75f;
                }
            }
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 2: PHASE TELEPORT (Distant Enemy or Stuck)
    // =========================================================================================================
    public class PhaseTeleportAbility : SmartAIAbility
    {
        public PhaseTeleportAbility() : base("Phase Teleport", 10.0f, 0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            if (context.CurrentStuckLevel >= StuckLevel.Moderate) return true;
            return context.TargetEnemy != null && context.TargetDistance > 12.0f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            Vector2 destination = (context.TargetEnemy != null)
                ? (Vector2)context.TargetEnemy.transform.position + new Vector2(-2.5f, 0.5f)
                : context.Position + new Vector2(context.Navigation.FacingDirection * 3.0f, 2.0f);

            Vector2 delta = destination - context.Position;
            if (context.Person != null && context.Person.Limbs != null)
            {
                LimbBehaviour[] limbs = context.Person.Limbs;
                for (int i = 0; i < limbs.Length; i++)
                {
                    LimbBehaviour limb = limbs[i];
                    if (limb != null)
                    {
                        limb.transform.position += (Vector3)delta;
                        if (limb.PhysicalBehaviour != null && limb.PhysicalBehaviour.rigidbody != null)
                        {
                            limb.PhysicalBehaviour.rigidbody.velocity = Vector2.zero;
                            limb.PhysicalBehaviour.rigidbody.angularVelocity = 0f;
                        }
                    }
                }
            }

            ModAPI.Notify(context.AI.name + " PHASE TELEPORTED!");
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 3: TIERED VOID BURST (Synergizes with Human Tiers & Beyond Nowhere)
    // =========================================================================================================
    public class TieredVoidBurstAbility : SmartAIAbility
    {
        public TieredVoidBurstAbility() : base("Void Burst", 10.0f, 0.2f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.IsHumanTiersEntity && context.ModdedEnergy >= 15f && context.TargetDistance < 5.0f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            Collider2D[] victims = Physics2D.OverlapCircleAll(context.Position, 4.5f);
            for (int i = 0; i < victims.Length; i++)
            {
                Collider2D col = victims[i];
                if (col == null || context.AI.IsOwnLimb(col)) continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 diff = (Vector2)col.transform.position - context.Position;
                    float dist = Mathf.Max(0.6f, diff.magnitude);
                    Vector2 forceDir = diff.normalized;
                    rb.AddForce(forceDir * (30f / dist), ForceMode2D.Impulse);
                }
            }

            ModAPI.Notify(context.AI.name + " UNLEASHED TIERED VOID BURST!");
        }
=======
>>>>>>> 0b62996 (Fix weapon holding alignment, ignore self-collisions to prevent crushing damage, and remove physics abilities)
    }
}
