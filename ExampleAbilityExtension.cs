using System;
using UnityEngine;

namespace AdvancedSmartAIMod
{
    // =========================================================================================================
    // EXAMPLE 3RD-PARTY ABILITY EXTENSION (FOR MOD CREATORS)
    // =========================================================================================================
    // This is an example of how other mod creators can write external addon mods that register custom abilities
    // with the Advanced Smart AI without modifying the core mod files.
    // =========================================================================================================
    public class ExampleAbilityExtensionMod
    {
        public static void Main()
        {
            // Register an example custom ability with the Advanced Smart AI global factory
            AdvancedSmartAI.RegisterGlobalAbilityFactory((ai) => new ExamplePassiveRegenAbility());

            ModAPI.Notify("Advanced Smart AI: Example Ability Extension loaded!");
        }
    }

    /// <summary>
    /// Example custom ability: Gently regenerates limb health when injured.
    /// </summary>
    public class ExamplePassiveRegenAbility : SmartAIAbility
    {
        public ExamplePassiveRegenAbility() : base("Passive Regeneration", 10.0f, 3.0f)
        {
        }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            return context.HealthPercent < 0.85f;
        }

        public override void OnActivate(SmartAIContext context)
        {
            ModAPI.Notify(context.AI.name + " activated Passive Regeneration.");
        }

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
            if (context.Person != null && context.Person.Limbs != null)
            {
                LimbBehaviour[] limbs = context.Person.Limbs;
                for (int i = 0; i < limbs.Length; i++)
                {
                    LimbBehaviour limb = limbs[i];
                    if (limb != null && limb.Health < (100f * context.Config.HealthMultiplier))
                    {
                        limb.Health = Mathf.Min(100f * context.Config.HealthMultiplier, limb.Health + (8f * deltaTime));
                        limb.Numbness = 0f;
                    }
                }
            }
        }

        public override void OnDeactivate(SmartAIContext context)
        {
            base.OnDeactivate(context);
        }
    }
}
