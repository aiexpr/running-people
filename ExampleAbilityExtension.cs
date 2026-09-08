using System;
using UnityEngine;
using AdvancedSmartAIMod;

namespace AdvancedSmartAIMod.Examples
{
    // =========================================================================================================
    // 3RD-PARTY MOD EXTENSION EXAMPLE: CUSTOM ABILITIES & CROSS-MOD SYNERGY
    // Compatible with vanilla People Playground, Human Tiers (Reforged), and Beyond Nowhere
    // =========================================================================================================
    public class ExampleAbilityExtensionMod
    {
        public static void Main()
        {
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
                Debug.Log($"[ExampleMod] Custom logic attached to AI: {smartAI.name} (Tier: {smartAI.DetectedTier})");

                // Hook 1: Listen to health changes
                smartAI.OnHealthChanged += (context, oldHp, newHp) =>
                {
                    if (newHp < 0.25f && oldHp >= 0.25f)
                    {
                        Debug.LogWarning($"[ExampleMod] AI {smartAI.name} is in critical condition! (HP: {newHp:P0})");
                    }
                };

                // Hook 2: Listen to target acquisition
                smartAI.OnTargetAcquired += (context, target) =>
                {
                    int enemyTier = CrossModAdapterEngine.DetectEntityTier(target);
                    string tierNote = enemyTier >= 0 ? $" [Enemy Tier {enemyTier}]" : "";
                    Debug.Log($"[ExampleMod] AI {smartAI.name} locked onto: {target.name}{tierNote}");
                };

                // Hook 3: Listen to weapon throwing
                smartAI.OnWeaponThrown += (context, weapon, velocity) =>
                {
                    Debug.Log($"[ExampleMod] AI {smartAI.name} hurled {weapon.name} at {velocity}!");
                };
            };
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 1: ENERGY SHIELD (Health < 50%)
    // =========================================================================================================
    public class EnergyShieldAbility : SmartAIAbility
    {
        public EnergyShieldAbility() : base("Energy Shield", cooldown: 14.0f, duration: 4.0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.HealthPercent < 0.50f && context.TargetEnemy != null;
        }

        public override void OnActivate(SmartAIContext context)
        {
            ModAPI.Notify($"{context.AI.name} ACTIVATED ENERGY SHIELD!");
            if (context.Person != null && context.Person.Limbs != null)
            {
                foreach (var limb in context.Person.Limbs)
                {
                    if (limb != null)
                    {
                        limb.Health += 35f;
                        limb.Numbness = 0f;
                    }
                }
            }
        }

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
            // Deflect fast incoming projectiles
            Collider2D[] incoming = Physics2D.OverlapCircleAll(context.Position, 2.6f);
            foreach (var col in incoming)
            {
                if (col.transform.root == context.Transform.root) continue;
                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null && rb.velocity.sqrMagnitude > 25f)
                {
                    rb.velocity = -rb.velocity * 0.85f;
                }
            }
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 2: PHASE TELEPORT (Distant Enemy or Stuck)
    // =========================================================================================================
    public class PhaseTeleportAbility : SmartAIAbility
    {
        public PhaseTeleportAbility() : base("Phase Teleport", cooldown: 9.0f, duration: 0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            if (context.CurrentStuckLevel >= StuckLevel.Moderate) return true;
            return context.TargetEnemy != null && context.TargetDistance > 12.0f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            Vector2 destination = (context.TargetEnemy != null)
                ? (Vector2)context.TargetEnemy.transform.position + new Vector2(-3.0f, 1.0f)
                : context.Position + new Vector2(context.Navigation.FacingDirection * 3.5f, 3.0f);

            Vector2 delta = destination - context.Position;
            foreach (var limb in context.Person.Limbs)
            {
                if (limb != null)
                {
                    limb.transform.position += (Vector3)delta;
                    if (limb.PhysicalBehaviour.rigidbody != null)
                        limb.PhysicalBehaviour.rigidbody.velocity = Vector2.zero;
                }
            }

            ModAPI.Notify($"{context.AI.name} PHASE TELEPORTED!");
        }
    }

    // =========================================================================================================
    // CUSTOM ABILITY 3: TIERED VOID BURST (Synergizes with Human Tiers & Beyond Nowhere)
    // Condition: Entity Tier >= 2 and Energy >= 20, or Target Distance < 5m
    // =========================================================================================================
    public class TieredVoidBurstAbility : SmartAIAbility
    {
        public TieredVoidBurstAbility() : base("Void Burst", cooldown: 8.0f, duration: 0.2f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            // Triggers if entity has Human Tiers energy or high tier status in close combat
            return context.IsHumanTiersEntity && context.ModdedEnergy >= 15f && context.TargetDistance < 6.0f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            ModAPI.Notify($"{context.AI.name} UNLEASHED TIERED VOID BURST!");

            Collider2D[] victims = Physics2D.OverlapCircleAll(context.Position, 5.0f);
            foreach (var col in victims)
            {
                if (col.transform.root == context.Transform.root) continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 forceDir = ((Vector2)col.transform.position - context.Position).normalized;
                    rb.AddForce(forceDir * (700f + (context.ModdedTier * 100f)), ForceMode2D.Impulse);
                }
            }
        }
    }
}
