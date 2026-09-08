using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace AdvancedSmartAIMod
{
    // =========================================================================================================
    // MOD ENTRY POINT
    // =========================================================================================================
    /// <summary>
    /// Mod entry point loaded by People Playground upon game initialization.
    /// Registers the custom "Advanced Smart AI" spawnable entity and sets up global context menu options.
    /// </summary>
    public class Mod
    {
        public static void Main()
        {
            // Register the custom Smart AI entity under the "Entities" category
            ModAPI.Register(new Modification
            {
                OriginalItem = ModAPI.FindSpawnable("Human"),
                NameOverride = "Advanced Smart AI",
                DescriptionOverride = "A highly intelligent, autonomous NPC featuring active balance stabilization, dynamic obstacle navigation, advanced combat tactics (firearms, melee & tactical throwing), customizable context menu settings, and a modular ability hook API.",
                CategoryOverride = ModAPI.FindCategory("Entities"),
                AfterSpawn = (instance) =>
                {
                    // Attach the advanced AI controller to the spawned human
                    AdvancedSmartAI ai = instance.AddComponent<AdvancedSmartAI>();
                    ai.InitializeAI();
                }
            });

            // Register global context menu action allowing players to convert ANY existing vanilla human into a Smart AI
            ModAPI.RegisterContextMenu<PersonBehaviour>(new ContextMenuOption(
                "convert_to_smart_ai",
                "Convert to Smart AI",
                "Inject Advanced Smart AI intelligence and balance into this human.",
                (person) =>
                {
                    if (person == null || person.gameObject == null) return;

                    AdvancedSmartAI existingAI = person.GetComponent<AdvancedSmartAI>();
                    if (existingAI == null)
                    {
                        AdvancedSmartAI newAI = person.gameObject.AddComponent<AdvancedSmartAI>();
                        newAI.InitializeAI();
                        ModAPI.Notify("Human converted to Advanced Smart AI!");
                    }
                    else
                    {
                        ModAPI.Notify("This human is already an Advanced Smart AI!");
                    }
                }
            ));

            ModAPI.Notify("Advanced Smart AI Mod loaded successfully!");
        }
    }

    // =========================================================================================================
    // ENUMS & DATA STRUCTURES
    // =========================================================================================================

    /// <summary>
    /// Behavioral stances determining overall AI combat and environmental posture.
    /// </summary>
    public enum AIStance
    {
        Aggressive, // Actively hunts enemies, seeks weapons, attacks on sight
        Defensive,  // Holds ground, arms itself, attacks only when approached or fired upon
        Follower,   // Follows the nearest friendly/player-designated human, protects them
        Neutral     // Wanders peacefully unless harmed
    }

    /// <summary>
    /// Preset archetypes for quick scaling of the AI's capabilities.
    /// </summary>
    public enum AIPreset
    {
        Godlike,       // 5.0x HP, 2.0x Speed, 100% Accuracy, 2.5x Reaction Time
        EliteSoldier,  // 2.5x HP, 1.4x Speed, 90% Accuracy, 1.8x Reaction Time
        StandardAI,    // 1.5x HP, 1.0x Speed, 75% Accuracy, 1.0x Reaction Time
        GruntWeakling, // 0.8x HP, 0.85x Speed, 45% Accuracy, 0.7x Reaction Time
        Civilian       // 0.5x HP, 0.75x Speed, 25% Accuracy, 0.5x Reaction Time (Cowardly)
    }

    /// <summary>
    /// Severity level of physical entrapment / obstruction.
    /// </summary>
    public enum StuckLevel
    {
        None,
        Minor,    // Mild snag or low obstacle obstruction
        Moderate, // Stuck against a wall, barrier, or pile of debris
        Severe    // Trapped in a tight corner, under heavy prop, or inverted
    }

    /// <summary>
    /// Weapon categorization for generic weapon handling.
    /// </summary>
    public enum WeaponType
    {
        None,
        Firearm,  // Pistols, rifles, shotguns, energy blasters, modded guns
        Melee,    // Swords, knives, axes, batons, blunt objects
        Throwable // Grenades, rocks, or improvised projectiles
    }

    // =========================================================================================================
    // AI CONFIGURATION COMPONENT
    // =========================================================================================================
    /// <summary>
    /// Manages configurable multipliers, stances, and context menu options.
    /// Allows live attribute scaling (Health, Speed, Accuracy, Reaction Time).
    /// </summary>
    [Serializable]
    public class AIConfig
    {
        [Header("Attribute Multipliers")]
        [Range(0.1f, 10.0f)] public float HealthMultiplier = 1.8f;
        [Range(0.2f, 3.0f)]  public float MovementSpeedMultiplier = 1.25f;
        [Range(0.1f, 1.0f)]  public float AccuracyMultiplier = 0.85f;
        [Range(0.1f, 5.0f)]  public float ReactionTimeMultiplier = 1.4f;

        [Header("Behavioral Settings")]
        public AIStance Stance = AIStance.Aggressive;
        public AIPreset CurrentPreset = AIPreset.StandardAI;
        public bool EnableJumpNavigation = true;
        public bool EnableWeaponThrowing = true;
        public bool EnableAbilities = true;
        public bool EnableActiveBalance = true;

        [Header("Tuning Parameters")]
        public float BaseWalkForce = 28f;
        public float BaseJumpForce = 260f;
        public float BaseJumpForwardForce = 120f;
        public float VisionRange = 26f;
        public float WeaponSearchRadius = 14f;
        public float MeleeRange = 2.2f;
        public float ThrowThresholdDistance = 5.5f;

        /// <summary>
        /// Applies a predefined preset to scale all AI attributes simultaneously.
        /// </summary>
        public void ApplyPreset(AIPreset preset, AdvancedSmartAI ai)
        {
            CurrentPreset = preset;
            switch (preset)
            {
                case AIPreset.Godlike:
                    HealthMultiplier = 5.0f;
                    MovementSpeedMultiplier = 2.0f;
                    AccuracyMultiplier = 1.0f;
                    ReactionTimeMultiplier = 2.5f;
                    Stance = AIStance.Aggressive;
                    break;
                case AIPreset.EliteSoldier:
                    HealthMultiplier = 2.5f;
                    MovementSpeedMultiplier = 1.4f;
                    AccuracyMultiplier = 0.90f;
                    ReactionTimeMultiplier = 1.8f;
                    Stance = AIStance.Aggressive;
                    break;
                case AIPreset.StandardAI:
                    HealthMultiplier = 1.5f;
                    MovementSpeedMultiplier = 1.15f;
                    AccuracyMultiplier = 0.75f;
                    ReactionTimeMultiplier = 1.2f;
                    Stance = AIStance.Aggressive;
                    break;
                case AIPreset.GruntWeakling:
                    HealthMultiplier = 0.8f;
                    MovementSpeedMultiplier = 0.85f;
                    AccuracyMultiplier = 0.45f;
                    ReactionTimeMultiplier = 0.7f;
                    Stance = AIStance.Aggressive;
                    break;
                case AIPreset.Civilian:
                    HealthMultiplier = 0.5f;
                    MovementSpeedMultiplier = 0.75f;
                    AccuracyMultiplier = 0.25f;
                    ReactionTimeMultiplier = 0.5f;
                    Stance = AIStance.Neutral;
                    break;
            }

            if (ai != null)
            {
                ai.ApplyHealthMultiplier();
                ModAPI.Notify($"Applied Preset: {preset} (HP: {HealthMultiplier}x, Spd: {MovementSpeedMultiplier}x, Acc: {AccuracyMultiplier:P0}, React: {ReactionTimeMultiplier}x)");
            }
        }
    }

    // =========================================================================================================
    // SMART AI EXECUTION CONTEXT (FOR ABILITIES & HOOKS)
    // =========================================================================================================
    /// <summary>
    /// Contextual state passed to abilities, condition checks, and external mod event listeners.
    /// </summary>
    public class SmartAIContext
    {
        public AdvancedSmartAI AI { get; private set; }
        public PersonBehaviour Person => AI.Person;
        public AINavigationController Navigation => AI.Navigation;
        public AICombatController Combat => AI.Combat;
        public AIConfig Config => AI.Config;

        public Transform Transform => AI.transform;
        public Vector2 Position => AI.TorsoLimb != null ? (Vector2)AI.TorsoLimb.transform.position : (Vector2)AI.transform.position;
        public Vector2 Velocity => AI.TorsoRigidbody != null ? AI.TorsoRigidbody.velocity : Vector2.zero;

        public float HealthPercent => AI.GetAverageHealthPercent();
        public bool IsAlive => AI.IsAlive;
        public bool IsConscious => AI.IsConscious;
        public bool IsArmed => AI.Combat.HeldWeapon != null;
        public PhysicalBehaviour HeldWeapon => AI.Combat.HeldWeapon;
        public GameObject TargetEnemy => AI.Combat.CurrentTarget;
        public float TargetDistance => AI.Combat.TargetDistance;
        public StuckLevel CurrentStuckLevel => AI.Navigation.CurrentStuckLevel;

        public SmartAIContext(AdvancedSmartAI ai)
        {
            AI = ai;
        }
    }

    // =========================================================================================================
    // MODULAR ABILITY SYSTEM BASE CLASS & HOOKS
    // =========================================================================================================
    /// <summary>
    /// Abstract base class for modular AI abilities.
    /// Custom abilities from this mod or 3rd-party mods inherit from this class.
    /// </summary>
    public abstract class SmartAIAbility
    {
        public string AbilityName { get; protected set; }
        public float Cooldown { get; protected set; }
        public float Duration { get; protected set; }
        public float LastExecutionTime { get; protected set; } = -999f;
        public bool IsActive { get; protected set; } = false;

        public float RemainingCooldown => Mathf.Max(0f, (LastExecutionTime + Cooldown) - Time.time);
        public bool IsReady => Time.time >= LastExecutionTime + Cooldown;

        protected SmartAIAbility(string name, float cooldown, float duration = 0f)
        {
            AbilityName = name;
            Cooldown = cooldown;
            Duration = duration;
        }

        /// <summary>
        /// Evaluates whether the ability conditions are met (e.g. Health < 40%, Enemy far, Stuck, etc.)
        /// </summary>
        public abstract bool ShouldTrigger(SmartAIContext context);

        /// <summary>
        /// Execution logic triggered when the ability activates.
        /// </summary>
        public abstract void OnActivate(SmartAIContext context);

        /// <summary>
        /// Update tick for sustained/channeled abilities (called every frame while IsActive is true).
        /// </summary>
        public virtual void OnUpdate(SmartAIContext context, float deltaTime) { }

        /// <summary>
        /// Teardown logic when the ability duration expires or is cancelled.
        /// </summary>
        public virtual void OnDeactivate(SmartAIContext context)
        {
            IsActive = false;
        }

        public void Execute(SmartAIContext context)
        {
            LastExecutionTime = Time.time;
            IsActive = Duration > 0f;
            OnActivate(context);
        }
    }

    // =========================================================================================================
    // BUILT-IN ABILITIES
    // =========================================================================================================

    /// <summary>
    /// Tactical Dash: Allows the AI to perform a high-velocity evasive leap / dash towards or away from danger.
    /// Condition: Enemy is at medium/long distance (> 8 units) or actively aiming at AI.
    /// </summary>
    public class TacticalDashAbility : SmartAIAbility
    {
        private float dashForce = 340f;

        public TacticalDashAbility() : base("Tactical Dash", cooldown: 5.5f, duration: 0.35f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            if (context.TargetEnemy == null) return false;
            // Trigger if enemy is distant, or if stuck and needing a kinetic burst
            return context.TargetDistance > 7.5f || context.CurrentStuckLevel == StuckLevel.Moderate;
        }

        public override void OnActivate(SmartAIContext context)
        {
            if (context.AI.TorsoRigidbody == null) return;

            float moveDir = context.Combat.CurrentTarget != null
                ? Mathf.Sign(context.Combat.CurrentTarget.transform.position.x - context.Position.x)
                : context.Navigation.FacingDirection;

            Vector2 dashVector = new Vector2(moveDir * dashForce, 80f);
            context.AI.TorsoRigidbody.AddForce(dashVector, ForceMode2D.Impulse);
            if (context.AI.PelvisRigidbody != null)
                context.AI.PelvisRigidbody.AddForce(dashVector * 0.8f, ForceMode2D.Impulse);

            // Optional visual feedback / audio cue
            ModAPI.Notify($"{context.AI.name} performed Tactical Dash!");
        }
    }

    /// <summary>
    /// Adrenaline Surge: Grants rapid health regeneration and massive speed boost when critically wounded.
    /// Condition: Health drops below 40%.
    /// </summary>
    public class AdrenalineSurgeAbility : SmartAIAbility
    {
        private float originalSpeedMultiplier;

        public AdrenalineSurgeAbility() : base("Adrenaline Surge", cooldown: 20f, duration: 6.0f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            // Trigger when injured below 40% health while alive and in combat
            return context.HealthPercent < 0.40f && context.TargetEnemy != null;
        }

        public override void OnActivate(SmartAIContext context)
        {
            originalSpeedMultiplier = context.Config.MovementSpeedMultiplier;
            context.Config.MovementSpeedMultiplier *= 1.75f;
            ModAPI.Notify($"{context.AI.name} activated ADRENALINE SURGE!");
        }

        public override void OnUpdate(SmartAIContext context, float deltaTime)
        {
            // Rapidly heal damaged limbs over time
            if (context.Person != null && context.Person.Limbs != null)
            {
                foreach (var limb in context.Person.Limbs)
                {
                    if (limb != null && limb.Health < 100f)
                    {
                        limb.Health = Mathf.Min(100f, limb.Health + (15f * deltaTime));
                        limb.Numbness = 0f; // Resist shock/pain
                    }
                }
            }
        }

        public override void OnDeactivate(SmartAIContext context)
        {
            base.OnDeactivate(context);
            context.Config.MovementSpeedMultiplier = originalSpeedMultiplier;
        }
    }

    /// <summary>
    /// Kinetic Shockwave: Unleashes a radial physics pulse that flings nearby obstacles, enemies, and projectiles away.
    /// Condition: AI is severely stuck or surrounded by 2 or more close-range enemies.
    /// </summary>
    public class KineticShockwaveAbility : SmartAIAbility
    {
        private float blastRadius = 4.5f;
        private float blastForce = 600f;

        public KineticShockwaveAbility() : base("Kinetic Shockwave", cooldown: 12f, duration: 0.1f) { }

        public override bool ShouldTrigger(SmartAIContext context)
        {
            if (context.CurrentStuckLevel == StuckLevel.Severe) return true;

            // Check if enemies are crowding the AI
            Collider2D[] nearby = Physics2D.OverlapCircleAll(context.Position, 2.5f);
            int hostileCount = 0;
            foreach (var col in nearby)
            {
                if (col.transform.root != context.Transform.root && col.GetComponent<LimbBehaviour>() != null)
                {
                    hostileCount++;
                }
            }
            return hostileCount >= 2;
        }

        public override void OnActivate(SmartAIContext context)
        {
            Collider2D[] affected = Physics2D.OverlapCircleAll(context.Position, blastRadius);
            foreach (var col in affected)
            {
                if (col.transform.root == context.Transform.root) continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 diff = (Vector2)col.transform.position - context.Position;
                    float dist = Mathf.Max(0.5f, diff.magnitude);
                    Vector2 forceDir = diff.normalized;
                    rb.AddForce(forceDir * (blastForce / dist), ForceMode2D.Impulse);
                }
            }
            ModAPI.Notify($"{context.AI.name} unleashed Kinetic Shockwave!");
        }
    }

    // =========================================================================================================
    // MAIN AI CONTROLLER COMPONENT
    // =========================================================================================================
    /// <summary>
    /// Core component orchestrating the AI lifecycle, balance, locomotion, combat, and ability execution.
    /// </summary>
    public class AdvancedSmartAI : MonoBehaviour
    {
        // -----------------------------------------------------------------------------------------------------
        // Global & Instance Events / API Hooks for 3rd-Party Mod Compatibility
        // -----------------------------------------------------------------------------------------------------
        /// <summary> Global hook invoked whenever any AdvancedSmartAI instance is spawned. </summary>
        public static event Action<AdvancedSmartAI> OnAnyAISpawned;

        /// <summary> Global factory registry for 3rd-party mods to automatically inject custom abilities into every AI. </summary>
        public static readonly List<Func<AdvancedSmartAI, SmartAIAbility>> GlobalAbilityFactories = new List<Func<AdvancedSmartAI, SmartAIAbility>>();

        /// <summary> Invoked after the AI finishes initializing its body parts and subsystems. </summary>
        public event Action<SmartAIContext> OnAIInitialized;

        /// <summary> Invoked when AI health changes significantly (context, oldHPPercent, newHPPercent). </summary>
        public event Action<SmartAIContext, float, float> OnHealthChanged;

        /// <summary> Invoked when AI acquires a new combat target. </summary>
        public event Action<SmartAIContext, GameObject> OnTargetAcquired;

        /// <summary> Invoked when AI picks up or equips a weapon. </summary>
        public event Action<SmartAIContext, PhysicalBehaviour> OnWeaponEquipped;

        /// <summary> Invoked when AI shoots / uses a firearm. </summary>
        public event Action<SmartAIContext, PhysicalBehaviour> OnWeaponFired;

        /// <summary> Invoked when AI throws a weapon at an enemy. </summary>
        public event Action<SmartAIContext, PhysicalBehaviour, Vector2> OnWeaponThrown;

        /// <summary> Invoked when any ability is successfully triggered. </summary>
        public event Action<SmartAIContext, SmartAIAbility> OnAbilityTriggered;

        /// <summary> Invoked when the unstuck routine changes severity level. </summary>
        public event Action<SmartAIContext, StuckLevel> OnStuckLevelChanged;

        // -----------------------------------------------------------------------------------------------------
        // Body References & Components
        // -----------------------------------------------------------------------------------------------------
        public PersonBehaviour Person { get; private set; }
        public LimbBehaviour HeadLimb { get; private set; }
        public LimbBehaviour TorsoLimb { get; private set; }
        public LimbBehaviour PelvisLimb { get; private set; }
        public LimbBehaviour FrontArmLimb { get; private set; }
        public LimbBehaviour BackArmLimb { get; private set; }
        public LimbBehaviour FrontLegLimb { get; private set; }
        public LimbBehaviour BackLegLimb { get; private set; }
        public LimbBehaviour FrontFootLimb { get; private set; }
        public LimbBehaviour BackFootLimb { get; private set; }

        public Rigidbody2D TorsoRigidbody => TorsoLimb != null ? TorsoLimb.PhysicalBehaviour.rigidbody : null;
        public Rigidbody2D PelvisRigidbody => PelvisLimb != null ? PelvisLimb.PhysicalBehaviour.rigidbody : null;
        public Rigidbody2D HeadRigidbody => HeadLimb != null ? HeadLimb.PhysicalBehaviour.rigidbody : null;
        public Rigidbody2D FrontArmRigidbody => FrontArmLimb != null ? FrontArmLimb.PhysicalBehaviour.rigidbody : null;

        public GripBehaviour Grip { get; private set; }

        // -----------------------------------------------------------------------------------------------------
        // Subsystems
        // -----------------------------------------------------------------------------------------------------
        public AIConfig Config { get; private set; } = new AIConfig();
        public SmartAIContext Context { get; private set; }
        public AIBalanceController Balance { get; private set; }
        public AINavigationController Navigation { get; private set; }
        public AICombatController Combat { get; private set; }
        public AIAbilityController AbilitySystem { get; private set; }

        // Internal tracking
        private float lastKnownHealthPercent = 1f;
        private float healthCheckTimer = 0f;
        private bool isInitialized = false;

        public bool IsAlive => Person != null && !Person.IsZombie && Person.Consciousness > 0.05f && HeadLimb != null && HeadLimb.Health > 1f && TorsoLimb != null && TorsoLimb.Health > 1f;
        public bool IsConscious => Person != null && Person.Consciousness > 0.2f && Person.ShockLevel < 0.9f;

        // =====================================================================================================
        // INITIALIZATION
        // =====================================================================================================
        private void Awake()
        {
            Person = GetComponent<PersonBehaviour>();
            Context = new SmartAIContext(this);
        }

        public void InitializeAI()
        {
            if (isInitialized) return;

            CacheLimbReferences();
            SetupSubsystems();
            ApplyHealthMultiplier();
            RegisterContextMenuOptions();

            // Register default built-in abilities
            AbilitySystem.RegisterAbility(new TacticalDashAbility());
            AbilitySystem.RegisterAbility(new AdrenalineSurgeAbility());
            AbilitySystem.RegisterAbility(new KineticShockwaveAbility());

            // Instantiate any global 3rd-party registered ability factories
            foreach (var factory in GlobalAbilityFactories)
            {
                try
                {
                    SmartAIAbility customAbility = factory?.Invoke(this);
                    if (customAbility != null)
                    {
                        AbilitySystem.RegisterAbility(customAbility);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AdvancedSmartAI] Error registering global ability factory: {ex.Message}");
                }
            }

            isInitialized = true;

            // Fire initialization callbacks
            OnAIInitialized?.Invoke(Context);
            OnAnyAISpawned?.Invoke(this);
        }

        private void CacheLimbReferences()
        {
            if (Person == null || Person.Limbs == null) return;

            foreach (var limb in Person.Limbs)
            {
                if (limb == null) continue;
                string limbName = limb.gameObject.name.ToLower();

                if (limbName.Contains("head")) HeadLimb = limb;
                else if (limbName.Contains("upperbody") || limbName.Contains("torso")) TorsoLimb = limb;
                else if (limbName.Contains("lowerbody") || limbName.Contains("middlebody")) PelvisLimb = limb;
                else if (limbName.Contains("armfront") || limbName.Contains("lowerarmfront")) FrontArmLimb = limb;
                else if (limbName.Contains("armback") || limbName.Contains("lowerarmback")) BackArmLimb = limb;
                else if (limbName.Contains("legfront") || limbName.Contains("lowerlegfront")) FrontLegLimb = limb;
                else if (limbName.Contains("legback") || limbName.Contains("lowerlegback")) BackLegLimb = limb;
                else if (limbName.Contains("footfront")) FrontFootLimb = limb;
                else if (limbName.Contains("footback")) BackFootLimb = limb;

                // Cache hand grip
                GripBehaviour limbGrip = limb.GetComponent<GripBehaviour>();
                if (limbGrip != null && Grip == null)
                {
                    Grip = limbGrip;
                }
            }

            if (Grip == null)
            {
                Grip = GetComponentInChildren<GripBehaviour>();
            }
        }

        private void SetupSubsystems()
        {
            Balance = new AIBalanceController(this);
            Navigation = new AINavigationController(this);
            Combat = new AICombatController(this);
            AbilitySystem = new AIAbilityController(this);
        }

        /// <summary>
        /// Scales limb health according to Config.HealthMultiplier and reinforces vital organ thresholds.
        /// </summary>
        public void ApplyHealthMultiplier()
        {
            if (Person == null || Person.Limbs == null) return;

            float targetMaxHealth = 100f * Config.HealthMultiplier;
            foreach (var limb in Person.Limbs)
            {
                if (limb == null) continue;
                limb.Health = targetMaxHealth;
                limb.InitialHealth = targetMaxHealth;
                limb.HealRate = Mathf.Max(limb.HealRate, 1.5f * Config.HealthMultiplier);
                limb.Numbness = Mathf.Clamp(limb.Numbness * 0.5f, 0f, 0.2f);
            }

            // Lower pain sensitivity and raise bravery
            Person.Bravery = 1.0f;
            Person.Consciousness = 1.0f;
            Person.ShockLevel = 0.0f;
            Person.PainLevel = 0.0f;
        }

        // =====================================================================================================
        // CONTEXT MENU (RIGHT-CLICK CONFIGURATION UI)
        // =====================================================================================================
        private void RegisterContextMenuOptions()
        {
            if (TorsoLimb == null || TorsoLimb.PhysicalBehaviour == null) return;

            PhysicalBehaviour pb = TorsoLimb.PhysicalBehaviour;
            if (pb.ContextMenuOptions == null) pb.ContextMenuOptions = new List<ContextMenuOption>();

            // Option: Cycle Preset
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_preset",
                $"AI Preset [{Config.CurrentPreset}]",
                "Cycle through AI difficulty/behavior presets.",
                () =>
                {
                    int nextPreset = ((int)Config.CurrentPreset + 1) % Enum.GetValues(typeof(AIPreset)).Length;
                    Config.ApplyPreset((AIPreset)nextPreset, this);
                }
            ));

            // Option: Cycle Stance
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_stance",
                $"AI Stance [{Config.Stance}]",
                "Cycle stance (Aggressive, Defensive, Follower, Neutral).",
                () =>
                {
                    int nextStance = ((int)Config.Stance + 1) % Enum.GetValues(typeof(AIStance)).Length;
                    Config.Stance = (AIStance)nextStance;
                    ModAPI.Notify($"AI Stance set to: {Config.Stance}");
                }
            ));

            // Option: Cycle Health Multiplier
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_health",
                $"Cycle Health [{Config.HealthMultiplier}x]",
                "Scale AI health (0.5x, 1.0x, 1.8x, 3.0x, 5.0x).",
                () =>
                {
                    float[] healthSteps = new float[] { 0.5f, 1.0f, 1.8f, 3.0f, 5.0f };
                    int currentIndex = Array.FindIndex(healthSteps, h => Mathf.Approximately(h, Config.HealthMultiplier));
                    int nextIndex = (currentIndex + 1) % healthSteps.Length;
                    Config.HealthMultiplier = healthSteps[nextIndex];
                    ApplyHealthMultiplier();
                    ModAPI.Notify($"AI Health Multiplier set to {Config.HealthMultiplier}x");
                }
            ));

            // Option: Cycle Speed Multiplier
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_speed",
                $"Cycle Speed [{Config.MovementSpeedMultiplier}x]",
                "Scale AI movement speed (0.75x, 1.0x, 1.25x, 1.6x, 2.2x).",
                () =>
                {
                    float[] speedSteps = new float[] { 0.75f, 1.0f, 1.25f, 1.6f, 2.2f };
                    int currentIndex = Array.FindIndex(speedSteps, s => Mathf.Approximately(s, Config.MovementSpeedMultiplier));
                    int nextIndex = (currentIndex + 1) % speedSteps.Length;
                    Config.MovementSpeedMultiplier = speedSteps[nextIndex];
                    ModAPI.Notify($"AI Speed Multiplier set to {Config.MovementSpeedMultiplier}x");
                }
            ));

            // Option: Cycle Accuracy Multiplier
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_accuracy",
                $"Cycle Accuracy [{(int)(Config.AccuracyMultiplier * 100)}%]",
                "Scale weapon aiming accuracy (30%, 60%, 85%, 100%).",
                () =>
                {
                    float[] accSteps = new float[] { 0.30f, 0.60f, 0.85f, 1.0f };
                    int currentIndex = Array.FindIndex(accSteps, a => Mathf.Approximately(a, Config.AccuracyMultiplier));
                    int nextIndex = (currentIndex + 1) % accSteps.Length;
                    Config.AccuracyMultiplier = accSteps[nextIndex];
                    ModAPI.Notify($"AI Accuracy set to {(int)(Config.AccuracyMultiplier * 100)}%");
                }
            ));

            // Option: Status & Diagnostics
            pb.ContextMenuOptions.Add(new ContextMenuOption(
                "smart_ai_stats",
                "Inspect AI Diagnostics",
                "Display current health, combat target, equipped weapon, and active ability status.",
                () =>
                {
                    string targetName = Combat.CurrentTarget != null ? Combat.CurrentTarget.name : "None";
                    string weaponName = Combat.HeldWeapon != null ? Combat.HeldWeapon.name : "Unarmed";
                    ModAPI.Notify($"[Smart AI Diagnostics]\nHP: {GetAverageHealthPercent():P0} | Target: {targetName}\nWeapon: {weaponName} | Stuck: {Navigation.CurrentStuckLevel}\nPreset: {Config.CurrentPreset} | Stance: {Config.Stance}");
                }
            ));
        }

        // =====================================================================================================
        // UPDATE LOOPS
        // =====================================================================================================
        private void Update()
        {
            if (!IsAlive || !IsConscious) return;

            float dt = Time.deltaTime;

            // Monitor health changes for hooks
            healthCheckTimer += dt;
            if (healthCheckTimer >= 0.2f)
            {
                healthCheckTimer = 0f;
                float currentHp = GetAverageHealthPercent();
                if (Mathf.Abs(currentHp - lastKnownHealthPercent) > 0.05f)
                {
                    OnHealthChanged?.Invoke(Context, lastKnownHealthPercent, currentHp);
                    lastKnownHealthPercent = currentHp;
                }
            }

            // Update subsystems
            Navigation.UpdateNavigation(dt);
            Combat.UpdateCombat(dt);
            if (Config.EnableAbilities)
            {
                AbilitySystem.UpdateAbilities(dt);
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive || !IsConscious) return;

            float fixedDt = Time.fixedDeltaTime;

            // Apply active balance stabilization
            if (Config.EnableActiveBalance)
            {
                Balance.FixedUpdateBalance(fixedDt);
            }

            // Apply physics locomotion
            Navigation.FixedUpdateNavigation(fixedDt);
            Combat.FixedUpdateCombat(fixedDt);
        }

        // =====================================================================================================
        // HELPER METHODS & API WRAPPERS
        // =====================================================================================================
        public float GetAverageHealthPercent()
        {
            if (Person == null || Person.Limbs == null || Person.Limbs.Length == 0) return 0f;
            float total = 0f;
            int count = 0;
            foreach (var limb in Person.Limbs)
            {
                if (limb != null)
                {
                    total += Mathf.Clamp01(limb.Health / (100f * Config.HealthMultiplier));
                    count++;
                }
            }
            return count > 0 ? (total / count) : 0f;
        }

        public void NotifyWeaponEquipped(PhysicalBehaviour weapon) => OnWeaponEquipped?.Invoke(Context, weapon);
        public void NotifyWeaponFired(PhysicalBehaviour weapon) => OnWeaponFired?.Invoke(Context, weapon);
        public void NotifyWeaponThrown(PhysicalBehaviour weapon, Vector2 velocity) => OnWeaponThrown?.Invoke(Context, weapon, velocity);
        public void NotifyTargetAcquired(GameObject target) => OnTargetAcquired?.Invoke(Context, target);
        public void NotifyAbilityTriggered(SmartAIAbility ability) => OnAbilityTriggered?.Invoke(Context, ability);
        public void NotifyStuckLevelChanged(StuckLevel level) => OnStuckLevelChanged?.Invoke(Context, level);

        /// <summary>
        /// Registers an ability directly to this AI instance.
        /// </summary>
        public void RegisterAbility(SmartAIAbility ability)
        {
            AbilitySystem.RegisterAbility(ability);
        }

        /// <summary>
        /// Registers a global factory for 3rd-party mods to supply abilities to all future Smart AIs.
        /// </summary>
        public static void RegisterGlobalAbilityFactory(Func<AdvancedSmartAI, SmartAIAbility> factory)
        {
            if (factory != null && !GlobalAbilityFactories.Contains(factory))
            {
                GlobalAbilityFactories.Add(factory);
            }
        }
    }

    // =========================================================================================================
    // ACTIVE BALANCE & STABILITY CONTROLLER
    // =========================================================================================================
    /// <summary>
    /// Active Proportional-Derivative (PD) balance controller that stabilizes the 2D ragdoll,
    /// keeping the torso upright, dampening violent oscillations, and preventing premature tripping.
    /// </summary>
    public class AIBalanceController
    {
        private readonly AdvancedSmartAI ai;

        // PD Controller constants
        private const float Torso_P = 280f;
        private const float Torso_D = 22f;
        private const float Pelvis_P = 180f;
        private const float Pelvis_D = 15f;
        private const float Head_P = 120f;
        private const float Head_D = 10f;

        public AIBalanceController(AdvancedSmartAI ai)
        {
            this.ai = ai;
        }

        public void FixedUpdateBalance(float fixedDeltaTime)
        {
            if (ai.TorsoLimb == null || ai.TorsoRigidbody == null) return;

            // Desired upright posture (0 degrees is straight up in Unity standard orientation for PPG ragdolls)
            float targetAngle = CalculateTargetLeanAngle();

            // Stabilize Torso
            StabilizeLimb(ai.TorsoRigidbody, targetAngle, Torso_P, Torso_D);

            // Stabilize Pelvis
            if (ai.PelvisRigidbody != null)
            {
                StabilizeLimb(ai.PelvisRigidbody, targetAngle, Pelvis_P, Pelvis_D);
            }

            // Stabilize Head towards look direction
            if (ai.HeadRigidbody != null)
            {
                float headLookAngle = targetAngle;
                if (ai.Combat.CurrentTarget != null)
                {
                    Vector2 toTarget = (Vector2)ai.Combat.CurrentTarget.transform.position - (Vector2)ai.HeadLimb.transform.position;
                    headLookAngle = Mathf.Clamp(Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f, -40f, 40f);
                }
                StabilizeLimb(ai.HeadRigidbody, headLookAngle, Head_P, Head_D);
            }

            // Ensure feet provide upright vertical ground support
            SupportGroundFeet();
        }

        private float CalculateTargetLeanAngle()
        {
            // Lean slightly forward into the direction of movement to simulate natural human locomotion balance
            float moveDir = ai.Navigation.MoveInput;
            float speedRatio = Mathf.Clamp(ai.TorsoRigidbody.velocity.x / 4f, -1f, 1f);
            return -moveDir * (6f * Mathf.Abs(speedRatio));
        }

        private void StabilizeLimb(Rigidbody2D rb, float targetAngle, float kP, float kD)
        {
            if (rb == null) return;

            float currentAngle = rb.rotation;
            float angleDelta = Mathf.DeltaAngle(currentAngle, targetAngle);
            float angularVelocity = rb.angularVelocity;

            // PD torque calculation: T = (Error * P) - (Velocity * D)
            float stabilizingTorque = (angleDelta * kP) - (angularVelocity * kD);
            rb.AddTorque(stabilizingTorque * Time.fixedDeltaTime, ForceMode2D.Force);
        }

        private void SupportGroundFeet()
        {
            // If standing near ground, apply corrective upward spring force to pelvis to prevent sagging
            RaycastHit2D groundHit = Physics2D.Raycast(ai.PelvisLimb.transform.position, Vector2.down, 1.8f, LayerMask.GetMask("Default", "Objects", "Debris"));
            if (groundHit.collider != null && groundHit.collider.transform.root != ai.transform.root)
            {
                float heightError = 1.35f - groundHit.distance;
                if (heightError > 0f && ai.PelvisRigidbody != null)
                {
                    float springForce = heightError * 220f;
                    ai.PelvisRigidbody.AddForce(Vector2.up * springForce * Time.fixedDeltaTime, ForceMode2D.Force);
                }
            }
        }
    }

    // =========================================================================================================
    // CORE NAVIGATION & ENVIRONMENTAL SCANNER
    // =========================================================================================================
    /// <summary>
    /// Handles obstacle raycasting, dynamic jumping over barriers and gaps, pathing towards weapons/targets,
    /// and multi-tier unstuck routines.
    /// </summary>
    public class AINavigationController
    {
        private readonly AdvancedSmartAI ai;

        public float MoveInput { get; private set; } = 0f;
        public float FacingDirection { get; private set; } = 1f;
        public StuckLevel CurrentStuckLevel { get; private set; } = StuckLevel.None;

        // Navigation & Jump state
        private float lastJumpTime = -999f;
        private float jumpCooldown = 1.2f;
        private bool isGrounded = true;

        // Unstuck tracking variables
        private Vector2 lastPosition;
        private float stuckTimer = 0f;
        private float unstuckActionTimer = 0f;
        private int unstuckAttemptCount = 0;

        public AINavigationController(AdvancedSmartAI ai)
        {
            this.ai = ai;
            if (ai.TorsoLimb != null)
            {
                lastPosition = ai.TorsoLimb.transform.position;
            }
        }

        public void UpdateNavigation(float deltaTime)
        {
            DetermineMovementGoal();
            CheckGroundedStatus();
            ScanEnvironmentAndObstacles();
            UpdateUnstuckRoutine(deltaTime);
        }

        public void FixedUpdateNavigation(float fixedDeltaTime)
        {
            ApplyLocomotionForces(fixedDeltaTime);
        }

        private void DetermineMovementGoal()
        {
            MoveInput = 0f;

            // If unstuck override is active, follow unstuck direction
            if (unstuckActionTimer > 0f) return;

            Vector2 currentPos = ai.TorsoLimb.transform.position;

            // Prioritize walking toward high-priority weapon if unarmed or holding empty weapon
            if (ai.Combat.NeedsWeapon && ai.Combat.NearestWeapon != null)
            {
                float weaponDeltaX = ai.Combat.NearestWeapon.transform.position.x - currentPos.x;
                if (Mathf.Abs(weaponDeltaX) > 0.6f)
                {
                    MoveInput = Mathf.Sign(weaponDeltaX);
                    FacingDirection = MoveInput;
                }
                return;
            }

            // Otherwise, navigate relative to Combat Target
            if (ai.Combat.CurrentTarget != null)
            {
                float targetDeltaX = ai.Combat.CurrentTarget.transform.position.x - currentPos.x;
                float targetDist = Mathf.Abs(targetDeltaX);
                FacingDirection = Mathf.Sign(targetDeltaX);

                if (ai.Config.Stance == AIStance.Aggressive)
                {
                    // If wielding firearm, keep tactical distance (4-8 units). If wielding melee/unarmed, close in.
                    float preferredDistance = (ai.Combat.CurrentWeaponType == WeaponType.Firearm) ? 6.0f : 1.2f;

                    if (targetDist > preferredDistance + 0.5f)
                    {
                        MoveInput = Mathf.Sign(targetDeltaX);
                    }
                    else if (targetDist < preferredDistance - 1.5f && ai.Combat.CurrentWeaponType == WeaponType.Firearm)
                    {
                        MoveInput = -Mathf.Sign(targetDeltaX); // Tactical backpedal while shooting
                    }
                }
                else if (ai.Config.Stance == AIStance.Defensive)
                {
                    if (targetDist < 4.0f)
                    {
                        MoveInput = -Mathf.Sign(targetDeltaX); // Back away from threat
                    }
                }
            }
        }

        private void CheckGroundedStatus()
        {
            if (ai.PelvisLimb == null) return;
            RaycastHit2D groundHit = Physics2D.Raycast(ai.PelvisLimb.transform.position, Vector2.down, 1.8f, LayerMask.GetMask("Default", "Objects", "Debris"));
            isGrounded = groundHit.collider != null && groundHit.collider.transform.root != ai.transform.root;
        }

        private void ScanEnvironmentAndObstacles()
        {
            if (!ai.Config.EnableJumpNavigation || !isGrounded || MoveInput == 0f) return;

            Vector2 originWaist = ai.TorsoLimb.transform.position;
            Vector2 originFeet = ai.PelvisLimb.transform.position + Vector3.down * 0.6f;
            Vector2 moveDir = new Vector2(FacingDirection, 0f);

            // 1. Low raycast: Low obstacles / steps (0.4m ahead)
            RaycastHit2D lowObstacle = Physics2D.Raycast(originFeet, moveDir, 1.4f, LayerMask.GetMask("Default", "Objects"));
            bool lowBlocked = lowObstacle.collider != null && lowObstacle.collider.transform.root != ai.transform.root;

            // 2. Mid raycast: Waist-high walls / barricades (1.2m ahead)
            RaycastHit2D midObstacle = Physics2D.Raycast(originWaist, moveDir, 1.8f, LayerMask.GetMask("Default", "Objects"));
            bool midBlocked = midObstacle.collider != null && midObstacle.collider.transform.root != ai.transform.root;

            // 3. High raycast: Overhead ceiling check
            RaycastHit2D ceilingCheck = Physics2D.Raycast(originWaist + Vector2.up * 0.8f, Vector2.up, 1.5f, LayerMask.GetMask("Default", "Objects"));
            bool ceilingBlocked = ceilingCheck.collider != null && ceilingCheck.collider.transform.root != ai.transform.root;

            // 4. Gap / Pit raycast: Cast downward 1.8m in front of feet
            Vector2 pitProbePos = originFeet + (moveDir * 1.5f);
            RaycastHit2D pitCheck = Physics2D.Raycast(pitProbePos, Vector2.down, 2.5f, LayerMask.GetMask("Default", "Objects"));
            bool gapDetected = (pitCheck.collider == null);

            // Execute jump if barrier or gap is detected and ceiling is clear
            if ((lowBlocked || midBlocked || gapDetected) && !ceilingBlocked)
            {
                ExecuteJump(highJump: midBlocked || gapDetected);
            }
        }

        public void ExecuteJump(bool highJump = false)
        {
            if (Time.time < lastJumpTime + jumpCooldown || !isGrounded) return;

            lastJumpTime = Time.time;

            float jumpForce = (ai.Config.BaseJumpForce * (highJump ? 1.35f : 1.0f));
            float forwardForce = ai.Config.BaseJumpForwardForce * FacingDirection;

            Vector2 impulse = new Vector2(forwardForce, jumpForce);

            if (ai.PelvisRigidbody != null) ai.PelvisRigidbody.AddForce(impulse, ForceMode2D.Impulse);
            if (ai.TorsoRigidbody != null) ai.TorsoRigidbody.AddForce(impulse * 0.8f, ForceMode2D.Impulse);

            // Slightly tuck legs upward during jump initiation
            if (ai.FrontLegLimb != null && ai.FrontLegLimb.PhysicalBehaviour.rigidbody != null)
                ai.FrontLegLimb.PhysicalBehaviour.rigidbody.AddForce(Vector2.up * (jumpForce * 0.4f), ForceMode2D.Impulse);
        }

        private void ApplyLocomotionForces(float fixedDeltaTime)
        {
            if (MoveInput == 0f || ai.TorsoRigidbody == null) return;

            float targetSpeed = ai.Config.BaseWalkForce * ai.Config.MovementSpeedMultiplier;
            Vector2 walkForce = new Vector2(MoveInput * targetSpeed, 0f);

            // Apply horizontal force to Pelvis and Torso
            ai.TorsoRigidbody.AddForce(walkForce * 0.6f, ForceMode2D.Force);
            if (ai.PelvisRigidbody != null)
            {
                ai.PelvisRigidbody.AddForce(walkForce * 0.8f, ForceMode2D.Force);
            }

            // Drive stepping limbs
            if (ai.FrontFootLimb != null && ai.FrontFootLimb.PhysicalBehaviour.rigidbody != null)
            {
                float stepPhase = Mathf.Sin(Time.time * 8f * ai.Config.MovementSpeedMultiplier);
                Vector2 footForce = new Vector2(MoveInput * targetSpeed * 0.3f, Mathf.Max(0f, stepPhase * 25f));
                ai.FrontFootLimb.PhysicalBehaviour.rigidbody.AddForce(footForce, ForceMode2D.Force);
            }
        }

        // =====================================================================================================
        // UNSTUCK ROUTINE (3-TIER ESCALATION)
        // =========================================================================================================
        private void UpdateUnstuckRoutine(float deltaTime)
        {
            if (ai.TorsoLimb == null) return;

            Vector2 currentPos = ai.TorsoLimb.transform.position;
            float displacement = (currentPos - lastPosition).magnitude;
            lastPosition = currentPos;

            if (unstuckActionTimer > 0f)
            {
                unstuckActionTimer -= deltaTime;
                return;
            }

            // If the AI wants to move (MoveInput != 0) but hasn't traveled at least 0.2 units over time
            if (Mathf.Abs(MoveInput) > 0.1f && displacement < 0.12f * deltaTime)
            {
                stuckTimer += deltaTime;
            }
            else
            {
                stuckTimer = Mathf.Max(0f, stuckTimer - (deltaTime * 2f));
            }

            // Determine escalation tier based on stuck duration
            StuckLevel newStuckLevel = StuckLevel.None;
            if (stuckTimer > 3.0f) newStuckLevel = StuckLevel.Severe;
            else if (stuckTimer > 1.6f) newStuckLevel = StuckLevel.Moderate;
            else if (stuckTimer > 0.75f) newStuckLevel = StuckLevel.Minor;

            if (newStuckLevel != CurrentStuckLevel)
            {
                CurrentStuckLevel = newStuckLevel;
                ai.NotifyStuckLevelChanged(CurrentStuckLevel);
            }

            // Execute unstuck maneuvers
            if (stuckTimer > 0.8f)
            {
                PerformUnstuckAction();
            }
        }

        private void PerformUnstuckAction()
        {
            unstuckAttemptCount++;
            unstuckActionTimer = 0.65f;

            switch (CurrentStuckLevel)
            {
                case StuckLevel.Minor:
                    // Tier 1: Jiggle & high-step / quick reverse burst
                    FacingDirection = -FacingDirection;
                    MoveInput = FacingDirection;
                    if (ai.PelvisRigidbody != null)
                    {
                        ai.PelvisRigidbody.AddForce(new Vector2(FacingDirection * 80f, 120f), ForceMode2D.Impulse);
                    }
                    break;

                case StuckLevel.Moderate:
                    // Tier 2: Wall push & power jump
                    ExecuteJump(highJump: true);
                    if (ai.TorsoRigidbody != null)
                    {
                        ai.TorsoRigidbody.AddForce(new Vector2(-FacingDirection * 140f, 180f), ForceMode2D.Impulse);
                    }
                    break;

                case StuckLevel.Severe:
                    // Tier 3: Explosive untangle leap & ragdoll slip impulse
                    if (ai.TorsoRigidbody != null)
                    {
                        // Burst upward and outward away from nearby geometry
                        Vector2 unstuckImpulse = new Vector2(UnityEngine.Random.Range(-1f, 1f) * 220f, 320f);
                        ai.TorsoRigidbody.AddForce(unstuckImpulse, ForceMode2D.Impulse);
                        if (ai.PelvisRigidbody != null) ai.PelvisRigidbody.AddForce(unstuckImpulse, ForceMode2D.Impulse);
                    }
                    stuckTimer = 0f;
                    break;
            }
        }
    }

    // =========================================================================================================
    // COMBAT & GENERIC WEAPON INTERACTION CONTROLLER
    // =========================================================================================================
    /// <summary>
    /// Manages target acquisition, weapon scanning/pickup, firearm aiming/shooting,
    /// melee swinging, and tactical weapon throwing when empty or out of range.
    /// Works generically with vanilla and modded custom weapons.
    /// </summary>
    public class AICombatController
    {
        private readonly AdvancedSmartAI ai;

        public GameObject CurrentTarget { get; private set; }
        public PhysicalBehaviour HeldWeapon { get; private set; }
        public WeaponType CurrentWeaponType { get; private set; } = WeaponType.None;
        public PhysicalBehaviour NearestWeapon { get; private set; }
        public float TargetDistance { get; private set; } = 999f;
        public bool NeedsWeapon => HeldWeapon == null || (CurrentWeaponType == WeaponType.Firearm && IsWeaponOutOfAmmo(HeldWeapon));

        // Combat Timers
        private float scanTimer = 0f;
        private float fireCooldownTimer = 0f;
        private float reactionDelayTimer = 0f;
        private float targetAcquiredTime = 0f;
        private int dryFireCount = 0;

        // Reflection cache for generic modded weapon detection
        private static readonly string[] FirearmTypeKeywords = new string[] { "firearm", "gun", "rifle", "pistol", "shotgun", "blaster", "laser", "cannon", "revolver", "weapon" };

        public AICombatController(AdvancedSmartAI ai)
        {
            this.ai = ai;
        }

        public void UpdateCombat(float deltaTime)
        {
            scanTimer += deltaTime;
            if (scanTimer >= 0.25f / ai.Config.ReactionTimeMultiplier)
            {
                scanTimer = 0f;
                ScanForTargets();
                ScanForWeapons();
            }

            InspectHeldWeapon();
            UpdateWeaponPickupLogic();
            HandleAimingAndAttack(deltaTime);
        }

        public void FixedUpdateCombat(float fixedDeltaTime)
        {
            // Physics torque aiming
            ApplyAimTorqueToArm(fixedDeltaTime);
        }

        // -----------------------------------------------------------------------------------------------------
        // Target Acquisition & Scanning
        // -----------------------------------------------------------------------------------------------------
        private void ScanForTargets()
        {
            if (ai.Config.Stance == AIStance.Neutral)
            {
                CurrentTarget = null;
                return;
            }

            Vector2 myPos = ai.TorsoLimb != null ? (Vector2)ai.TorsoLimb.transform.position : (Vector2)ai.transform.position;
            Collider2D[] candidates = Physics2D.OverlapCircleAll(myPos, ai.Config.VisionRange);

            GameObject bestTarget = null;
            float bestScore = -9999f;

            foreach (var col in candidates)
            {
                if (col == null || col.transform.root == ai.transform.root) continue;

                // Look for living organisms (PersonBehaviour / LimbBehaviour)
                LimbBehaviour limb = col.GetComponent<LimbBehaviour>();
                if (limb == null || limb.Person == null) continue;

                PersonBehaviour targetPerson = limb.Person;
                if (targetPerson.Consciousness <= 0.05f || targetPerson.Health <= 0f) continue;

                // Calculate distance
                float dist = Vector2.Distance(myPos, limb.transform.position);
                if (dist > ai.Config.VisionRange) continue;

                // Line of sight raycast
                RaycastHit2D los = Physics2D.Linecast(myPos, limb.transform.position, LayerMask.GetMask("Default", "Objects"));
                if (los.collider != null && los.collider.transform.root != targetPerson.transform.root) continue;

                // Score candidate (proximity + threat weighting)
                float score = 100f - dist;
                // Prefer targets holding weapons
                if (targetPerson.GetComponentInChildren<GripBehaviour>()?.Holding != null)
                {
                    score += 35f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = targetPerson.gameObject;
                }
            }

            if (bestTarget != CurrentTarget)
            {
                CurrentTarget = bestTarget;
                targetAcquiredTime = Time.time;
                reactionDelayTimer = 0.35f / ai.Config.ReactionTimeMultiplier;
                if (CurrentTarget != null)
                {
                    ai.NotifyTargetAcquired(CurrentTarget);
                }
            }

            if (CurrentTarget != null)
            {
                TargetDistance = Vector2.Distance(myPos, CurrentTarget.transform.position);
            }
            else
            {
                TargetDistance = 999f;
            }
        }

        // -----------------------------------------------------------------------------------------------------
        // Generic Weapon Detection & Pickup
        // -----------------------------------------------------------------------------------------------------
        private void ScanForWeapons()
        {
            if (!ai.Config.AllowWeaponPickup || !NeedsWeapon)
            {
                NearestWeapon = null;
                return;
            }

            Vector2 myPos = ai.TorsoLimb.transform.position;
            Collider2D[] items = Physics2D.OverlapCircleAll(myPos, ai.Config.WeaponSearchRadius);

            PhysicalBehaviour bestWeapon = null;
            float closestDist = float.MaxValue;

            foreach (var col in items)
            {
                if (col == null || col.transform.root == ai.transform.root) continue;

                PhysicalBehaviour pb = col.GetComponent<PhysicalBehaviour>();
                if (pb == null) continue;

                // Check if already held by someone else
                GripBehaviour otherGrip = col.GetComponentInParent<GripBehaviour>();
                if (otherGrip != null && otherGrip.Holding == pb) continue;

                WeaponType type = ClassifyWeapon(pb);
                if (type == WeaponType.None) continue;

                // If firearm, ensure it has ammo
                if (type == WeaponType.Firearm && IsWeaponOutOfAmmo(pb)) continue;

                float dist = Vector2.Distance(myPos, pb.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestWeapon = pb;
                }
            }

            NearestWeapon = bestWeapon;
        }

        private void UpdateWeaponPickupLogic()
        {
            if (NearestWeapon == null || ai.Grip == null || HeldWeapon != null) return;

            Vector2 handPos = ai.Grip.transform.position;
            Vector2 weaponPos = NearestWeapon.transform.position;
            float distToWeapon = Vector2.Distance(handPos, weaponPos);

            // If within grab range, attach to grip
            if (distToWeapon <= 1.2f)
            {
                EquipWeapon(NearestWeapon);
            }
        }

        public void EquipWeapon(PhysicalBehaviour weapon)
        {
            if (weapon == null || ai.Grip == null) return;

            // Attach weapon to hand grip
            try
            {
                ai.Grip.Attach(weapon);
            }
            catch
            {
                // Fallback direct connection
                weapon.transform.position = ai.Grip.transform.position;
            }

            HeldWeapon = weapon;
            CurrentWeaponType = ClassifyWeapon(weapon);
            dryFireCount = 0;
            NearestWeapon = null;

            ai.NotifyWeaponEquipped(weapon);
        }

        private void InspectHeldWeapon()
        {
            if (ai.Grip == null) return;

            // Check what the hand is actually holding
            PhysicalBehaviour currentlyHeld = ai.Grip.Holding ?? ai.Grip.GetComponent<PhysicalBehaviour>();
            if (currentlyHeld != HeldWeapon)
            {
                HeldWeapon = currentlyHeld;
                CurrentWeaponType = (HeldWeapon != null) ? ClassifyWeapon(HeldWeapon) : WeaponType.None;
                dryFireCount = 0;
            }
        }

        /// <summary>
        /// Classifies any vanilla or modded weapon generically using component inspection and reflection.
        /// </summary>
        public WeaponType ClassifyWeapon(PhysicalBehaviour pb)
        {
            if (pb == null || pb.gameObject == null) return WeaponType.None;

            Component[] components = pb.gameObject.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name.ToLower();

                // Check for standard firearm component names or modded firearm scripts
                if (FirearmTypeKeywords.Any(k => typeName.Contains(k)))
                {
                    return WeaponType.Firearm;
                }
            }

            // Check for melee characteristics (sharpness / stab / blunt damage properties)
            if (pb.Properties != null && (pb.Properties.Sharp || pb.Properties.Flammable || pb.Properties.Magnetic))
            {
                return WeaponType.Melee;
            }

            // Check object name heuristics
            string objName = pb.gameObject.name.ToLower();
            if (FirearmTypeKeywords.Any(k => objName.Contains(k))) return WeaponType.Firearm;
            if (objName.Contains("sword") || objName.Contains("knife") || objName.Contains("axe") || objName.Contains("baton") || objName.Contains("spear") || objName.Contains("blade") || objName.Contains("dagger"))
                return WeaponType.Melee;

            return WeaponType.None;
        }

        /// <summary>
        /// Checks whether a firearm is out of ammunition generically via reflection or dry-fire counter.
        /// </summary>
        private bool IsWeaponOutOfAmmo(PhysicalBehaviour weapon)
        {
            if (weapon == null) return true;
            if (dryFireCount >= 3) return true;

            Component[] components = weapon.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                Type t = comp.GetType();

                // Check HasAmmo / Ammo / CurrentAmmo properties or fields
                PropertyInfo hasAmmoProp = t.GetProperty("HasAmmo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (hasAmmoProp != null && hasAmmoProp.PropertyType == typeof(bool))
                {
                    bool hasAmmo = (bool)hasAmmoProp.GetValue(comp, null);
                    if (!hasAmmo) return true;
                }

                FieldInfo ammoField = t.GetField("Ammo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                   ?? t.GetField("CurrentAmmo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (ammoField != null && ammoField.FieldType == typeof(int))
                {
                    int ammo = (int)ammoField.GetValue(comp);
                    if (ammo <= 0) return true;
                }
            }

            return false;
        }

        // -----------------------------------------------------------------------------------------------------
        // Aiming, Shooting, Melee, and Throwing Execution
        // -----------------------------------------------------------------------------------------------------
        private void HandleAimingAndAttack(float deltaTime)
        {
            if (CurrentTarget == null || HeldWeapon == null) return;

            reactionDelayTimer -= deltaTime;
            fireCooldownTimer -= deltaTime;

            if (reactionDelayTimer > 0f) return;

            // Combat logic branch
            if (CurrentWeaponType == WeaponType.Firearm)
            {
                HandleFirearmCombat();
            }
            else if (CurrentWeaponType == WeaponType.Melee)
            {
                HandleMeleeCombat();
            }
        }

        private void HandleFirearmCombat()
        {
            // Check if gun ran out of ammo
            if (IsWeaponOutOfAmmo(HeldWeapon))
            {
                if (ai.Config.EnableWeaponThrowing)
                {
                    ThrowWeaponAtTarget();
                }
                return;
            }

            // Aim alignment check
            if (fireCooldownTimer <= 0f && IsAimAlignedWithTarget())
            {
                FireEquippedGun();
            }
        }

        private void HandleMeleeCombat()
        {
            if (TargetDistance <= ai.Config.MeleeRange)
            {
                // Melee strike: apply aggressive swing torque to arm
                if (ai.FrontArmRigidbody != null)
                {
                    float swingDir = Mathf.Sign(CurrentTarget.transform.position.x - ai.transform.position.x);
                    ai.FrontArmRigidbody.AddTorque(swingDir * 450f, ForceMode2D.Impulse);
                }
            }
            else if (TargetDistance >= ai.Config.ThrowThresholdDistance && ai.Config.EnableWeaponThrowing)
            {
                // Tactical Throw: Enemy is distant / unreachable -> Throw the melee weapon!
                ThrowWeaponAtTarget();
            }
        }

        private void FireEquippedGun()
        {
            if (HeldWeapon == null) return;

            // Trigger weapon actuation generically via SendMessage and direct Use invocation
            HeldWeapon.SendMessage("Use", SendMessageOptions.DontRequireReceiver);

            // Record fire cooldown
            fireCooldownTimer = UnityEngine.Random.Range(0.12f, 0.35f) / ai.Config.ReactionTimeMultiplier;

            // Verify if weapon fired or dry-fired
            if (IsWeaponOutOfAmmo(HeldWeapon))
            {
                dryFireCount++;
            }

            ai.NotifyWeaponFired(HeldWeapon);
        }

        /// <summary>
        /// Calculates parabolic ballistic arc and hurls the held weapon directly at the enemy.
        /// </summary>
        public void ThrowWeaponAtTarget()
        {
            if (HeldWeapon == null || ai.Grip == null || CurrentTarget == null) return;

            PhysicalBehaviour weaponToThrow = HeldWeapon;
            Rigidbody2D weaponRb = weaponToThrow.rigidbody;

            // Detach weapon from hand
            try
            {
                ai.Grip.Detach();
            }
            catch { }

            HeldWeapon = null;
            CurrentWeaponType = WeaponType.None;

            if (weaponRb != null)
            {
                Vector2 origin = weaponToThrow.transform.position;
                Vector2 targetPos = CurrentTarget.transform.position;
                Vector2 displacement = targetPos - origin;

                // Parabolic ballistic trajectory calculation
                float gravity = Mathf.Abs(Physics2D.gravity.y * weaponRb.gravityScale);
                float speed = 22f * ai.Config.ReactionTimeMultiplier;
                float angle = Mathf.Clamp(displacement.y / Mathf.Max(1f, Mathf.Abs(displacement.x)), -0.5f, 0.8f);

                Vector2 throwVelocity = new Vector2(Mathf.Sign(displacement.x) * speed, (angle * speed) + (gravity * 0.35f));

                weaponRb.velocity = throwVelocity;
                weaponRb.AddTorque(UnityEngine.Random.Range(-350f, 350f)); // Add tactical spin
            }

            ai.NotifyWeaponThrown(weaponToThrow, weaponRb != null ? weaponRb.velocity : Vector2.zero);

            // Immediately scan for next available weapon
            scanTimer = 999f;
        }

        private bool IsAimAlignedWithTarget()
        {
            if (ai.FrontArmLimb == null || CurrentTarget == null) return false;

            Vector2 toTarget = (Vector2)CurrentTarget.transform.position - (Vector2)ai.FrontArmLimb.transform.position;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float currentAngle = ai.FrontArmRigidbody != null ? ai.FrontArmRigidbody.rotation : ai.FrontArmLimb.transform.eulerAngles.z;

            return Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 25f;
        }

        private void ApplyAimTorqueToArm(float fixedDeltaTime)
        {
            if (ai.FrontArmRigidbody == null || CurrentTarget == null) return;

            Vector2 targetAimPoint = (Vector2)CurrentTarget.transform.position;
            Vector2 armPos = ai.FrontArmLimb.transform.position;
            Vector2 aimVector = targetAimPoint - armPos;

            float desiredAngle = Mathf.Atan2(aimVector.y, aimVector.x) * Mathf.Rad2Deg;

            // Add accuracy inaccuracy spread (inverse to AccuracyMultiplier)
            float spreadVariance = (1f - Mathf.Clamp01(ai.Config.AccuracyMultiplier)) * 18f;
            desiredAngle += Mathf.Sin(Time.time * 6f) * spreadVariance;

            float currentAngle = ai.FrontArmRigidbody.rotation;
            float angleDelta = Mathf.DeltaAngle(currentAngle, desiredAngle);

            // Apply torque to align arm towards target
            float aimTorque = (angleDelta * 320f) - (ai.FrontArmRigidbody.angularVelocity * 22f);
            ai.FrontArmRigidbody.AddTorque(aimTorque * fixedDeltaTime, ForceMode2D.Force);
        }
    }

    // =========================================================================================================
    // MODULAR ABILITY CONTROLLER
    // =========================================================================================================
    /// <summary>
    /// Manages the registry, condition checking, and invocation of all active abilities on this AI.
    /// </summary>
    public class AIAbilityController
    {
        private readonly AdvancedSmartAI ai;
        private readonly List<SmartAIAbility> abilities = new List<SmartAIAbility>();

        public IReadOnlyList<SmartAIAbility> Abilities => abilities.AsReadOnly();

        public AIAbilityController(AdvancedSmartAI ai)
        {
            this.ai = ai;
        }

        public void RegisterAbility(SmartAIAbility ability)
        {
            if (ability != null && !abilities.Contains(ability))
            {
                abilities.Add(ability);
            }
        }

        public void UnregisterAbility(SmartAIAbility ability)
        {
            if (ability != null && abilities.Contains(ability))
            {
                if (ability.IsActive) ability.OnDeactivate(ai.Context);
                abilities.Remove(ability);
            }
        }

        public void UpdateAbilities(float deltaTime)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                var ability = abilities[i];

                // Update active sustained abilities
                if (ability.IsActive)
                {
                    ability.OnUpdate(ai.Context, deltaTime);

                    // Check expiration
                    if (Time.time >= ability.LastExecutionTime + ability.Duration)
                    {
                        ability.OnDeactivate(ai.Context);
                    }
                }
                // Check if ready and should trigger
                else if (ability.IsReady && ability.ShouldTrigger(ai.Context))
                {
                    ability.Execute(ai.Context);
                    ai.NotifyAbilityTriggered(ability);
                }
            }
        }
    }
}
