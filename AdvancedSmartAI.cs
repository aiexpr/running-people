using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AdvancedSmartAIMod
{
    // =========================================================================================================
    // MOD ENTRY POINT & INITIALIZATION
    // =========================================================================================================
    public class Mod
    {
        public static void Main()
        {
            ModAPI.Register(new Modification
            {
                OriginalItem = ModAPI.FindSpawnable("Human"),
                NameOverride = "Advanced Smart AI",
                DescriptionOverride = "An advanced autonomous NPC featuring dynamic obstacle navigation, realistic firearm & melee combat, authentic two-handed weapon gripping, context menu settings, and an open ability API for mod creators.",
                CategoryOverride = ModAPI.FindCategory("Entities"),
                AfterSpawn = (instance) =>
                {
                    AdvancedSmartAI ai = instance.AddComponent<AdvancedSmartAI>();
                    ai.InitializeAI();
                }
            });

            ModAPI.Notify("Advanced Smart AI Mod initialized!");
        }
    }

    // =========================================================================================================
    // ENUMS & DATA STRUCTURES
    // =========================================================================================================

    public enum AIStance
    {
        Aggressive, // Actively seeks weapons and attacks enemies
        Defensive,  // Arms itself, attacks only when approached or fired upon
        Follower,   // Follows nearest friendly humanoid
        Neutral     // Wanders peacefully unless attacked
    }

    public enum AIPreset
    {
        Godlike,       // 5.0x HP, 1.8x Speed, 100% Accuracy, 2.2x Reaction Time
        EliteSoldier,  // 2.5x HP, 1.35x Speed, 90% Accuracy, 1.6x Reaction Time
        StandardAI,    // 1.5x HP, 1.15x Speed, 75% Accuracy, 1.2x Reaction Time
        GruntWeakling, // 0.8x HP, 0.85x Speed, 45% Accuracy, 0.7x Reaction Time
        Civilian       // 0.5x HP, 0.75x Speed, 25% Accuracy, 0.5x Reaction Time (Neutral)
    }

    public enum StuckLevel
    {
        None,
        Minor,
        Moderate,
        Severe
    }

    public enum WeaponType
    {
        None,
        Firearm,
        Melee,
        AbilityWeapon,
        Throwable
    }

    public struct EnergyData
    {
        public float Current;
        public float Max;

        public EnergyData(float current, float max)
        {
            Current = current;
            Max = max;
        }
    }

    // =========================================================================================================
    // CROSS-MOD COMPATIBILITY ENGINE (HUMAN TIERS & BEYOND NOWHERE)
    // =========================================================================================================
    public static class CrossModAdapterEngine
    {
        public static int DetectEntityTier(GameObject root)
        {
            if (root == null) return -1;

            string objName = root.name.ToLower();
            for (int k = 8; k >= 1; k--)
            {
                if (objName.Contains("tier " + k) || objName.Contains("tier" + k) || objName.Contains("[tier " + k + "]") || objName.Contains("[t" + k + "]"))
                {
                    return k;
                }
            }

            if (objName.Contains("god") || objName.Contains("monarch") || objName.Contains("z'othra") || objName.Contains("true void"))
                return 8;

            Component[] components = root.GetComponentsInChildren<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component comp = components[i];
                if (comp == null) continue;
                Type t = comp.GetType();
                string tName = t.Name.ToLower();

                if (tName.Contains("tier") || tName.Contains("gifted") || tName.Contains("power") || tName.Contains("anomaly"))
                {
                    try
                    {
                        var prop = t.GetProperty("Tier") ?? t.GetProperty("TierLevel");
                        if (prop != null && (prop.PropertyType == typeof(int) || prop.PropertyType.IsEnum))
                        {
                            return Convert.ToInt32(prop.GetValue(comp, null));
                        }

                        var field = t.GetField("Tier") ?? t.GetField("TierLevel") ?? t.GetField("tier");
                        if (field != null && (field.FieldType == typeof(int) || field.FieldType.IsEnum))
                        {
                            return Convert.ToInt32(field.GetValue(comp));
                        }
                    }
                    catch { }
                }
            }

            return -1;
        }

        public static EnergyData GetEnergyStatus(GameObject root)
        {
            if (root == null) return new EnergyData(0f, 0f);

            Component[] components = root.GetComponentsInChildren<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component comp = components[i];
                if (comp == null) continue;
                Type t = comp.GetType();
                string tName = t.Name.ToLower();

                if (tName.Contains("energy") || tName.Contains("power") || tName.Contains("gifted") || tName.Contains("main"))
                {
                    try
                    {
                        var curField = t.GetField("Energy") ?? t.GetField("currentEnergy") ?? t.GetField("energy");
                        var maxField = t.GetField("MaxEnergy") ?? t.GetField("maxEnergy");

                        if (curField != null)
                        {
                            float cur = Convert.ToSingle(curField.GetValue(comp));
                            float max = maxField != null ? Convert.ToSingle(maxField.GetValue(comp)) : Mathf.Max(cur, 100f);
                            return new EnergyData(cur, max);
                        }
                    }
                    catch { }
                }
            }

            return new EnergyData(0f, 0f);
        }

        public static bool IsModdedAbilityWeapon(GameObject obj)
        {
            if (obj == null) return false;
            if (obj.GetComponent<LimbBehaviour>() != null || obj.GetComponentInParent<PersonBehaviour>() != null) return false;

            string objName = obj.name.ToLower();
            if (objName.Contains("ability") || objName.Contains("z'othra") || objName.Contains("anomaly") ||
                objName.Contains("soul") || objName.Contains("data bank") || objName.Contains("analyser") ||
                objName.Contains("whisperer") || objName.Contains("relic") || objName.Contains("void"))
            {
                return true;
            }

            Component[] comps = obj.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null) continue;
                string cName = c.GetType().Name.ToLower();
                if (cName.Contains("powerweapon") || cName.Contains("abilityweapon") || cName.Contains("anomaly") || cName.Contains("voidweapon"))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // =========================================================================================================
    // AI CONFIGURATION COMPONENT
    // =========================================================================================================
    [Serializable]
    public class AIConfig
    {
        [Header("Attribute Multipliers")]
        [Range(0.1f, 10.0f)] public float HealthMultiplier = 1.8f;
        [Range(0.2f, 3.0f)]  public float MovementSpeedMultiplier = 1.15f;
        [Range(0.1f, 1.0f)]  public float AccuracyMultiplier = 0.85f;
        [Range(0.1f, 5.0f)]  public float ReactionTimeMultiplier = 1.3f;

        [Header("Behavioral Settings")]
        public AIStance Stance = AIStance.Aggressive;
        public AIPreset CurrentPreset = AIPreset.StandardAI;
        public bool AllowWeaponPickup = true;
        public bool EnableJumpNavigation = true;
        public bool EnableWeaponThrowing = true;
        public bool EnableCustomAbilities = true;

        [Header("Tuning Parameters")]
        public float VisionRange = 25f;
        public float WeaponSearchRadius = 10f;
        public float MeleeRange = 2.0f;
        public float ThrowThresholdDistance = 6.0f;

        public void ApplyPreset(AIPreset preset, AdvancedSmartAI ai)
        {
            CurrentPreset = preset;
            switch (preset)
            {
                case AIPreset.Godlike:
                    HealthMultiplier = 5.0f;
                    MovementSpeedMultiplier = 1.6f;
                    AccuracyMultiplier = 1.0f;
                    ReactionTimeMultiplier = 2.2f;
                    Stance = AIStance.Aggressive;
                    break;
                case AIPreset.EliteSoldier:
                    HealthMultiplier = 2.5f;
                    MovementSpeedMultiplier = 1.3f;
                    AccuracyMultiplier = 0.90f;
                    ReactionTimeMultiplier = 1.6f;
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
                ModAPI.Notify("Applied Preset: " + preset + " (HP: " + HealthMultiplier + "x, Spd: " + MovementSpeedMultiplier + "x, Acc: " + (int)(AccuracyMultiplier * 100) + "%, React: " + ReactionTimeMultiplier + "x)");
            }
        }
    }

    // =========================================================================================================
    // SMART AI EXECUTION CONTEXT (FOR 3RD-PARTY ABILITY EXTENSIONS)
    // =========================================================================================================
    public class SmartAIContext
    {
        public AdvancedSmartAI AI { get; private set; }
        public PersonBehaviour Person { get { return AI.Person; } }
        public AINavigationController Navigation { get { return AI.Navigation; } }
        public AICombatController Combat { get { return AI.Combat; } }
        public AIConfig Config { get { return AI.Config; } }

        public Transform Transform { get { return AI.transform; } }
        public Vector2 Position { get { return AI.TorsoLimb != null ? (Vector2)AI.TorsoLimb.transform.position : (Vector2)AI.transform.position; } }
        public Vector2 Velocity { get { return AI.TorsoRigidbody != null ? AI.TorsoRigidbody.velocity : Vector2.zero; } }

        public float HealthPercent { get { return AI.GetAverageHealthPercent(); } }
        public bool IsAlive { get { return AI.IsAlive; } }
        public bool IsConscious { get { return AI.IsConscious; } }
        public bool IsArmed { get { return AI.Combat.HeldWeapon != null; } }
        public PhysicalBehaviour HeldWeapon { get { return AI.Combat.HeldWeapon; } }
        public GameObject TargetEnemy { get { return AI.Combat.CurrentTarget; } }
        public float TargetDistance { get { return AI.Combat.TargetDistance; } }
        public StuckLevel CurrentStuckLevel { get { return AI.Navigation.CurrentStuckLevel; } }

        public int ModdedTier { get { return AI.DetectedTier; } }
        public float ModdedEnergy { get { return AI.CurrentEnergy; } }
        public float ModdedMaxEnergy { get { return AI.MaxEnergy; } }
        public bool IsHumanTiersEntity { get { return AI.DetectedTier >= 0; } }

        public SmartAIContext(AdvancedSmartAI ai)
        {
            AI = ai;
        }
    }

    // =========================================================================================================
    // MODULAR ABILITY SYSTEM BASE CLASS (FOR EXTERNAL MOD CREATORS)
    // =========================================================================================================
    public abstract class SmartAIAbility
    {
        public string AbilityName { get; protected set; }
        public float Cooldown { get; protected set; }
        public float Duration { get; protected set; }
        public float LastExecutionTime { get; protected set; }
        public bool IsActive { get; protected set; }

        public float RemainingCooldown { get { return Mathf.Max(0f, (LastExecutionTime + Cooldown) - Time.time); } }
        public bool IsReady { get { return Time.time >= LastExecutionTime + Cooldown; } }

        protected SmartAIAbility(string name, float cooldown, float duration)
        {
            AbilityName = name;
            Cooldown = cooldown;
            Duration = duration;
            LastExecutionTime = -999f;
            IsActive = false;
        }

        protected SmartAIAbility(string name, float cooldown) : this(name, cooldown, 0f) { }

        public abstract bool ShouldTrigger(SmartAIContext context);
        public abstract void OnActivate(SmartAIContext context);
        public virtual void OnUpdate(SmartAIContext context, float deltaTime) { }
        public virtual void OnDeactivate(SmartAIContext context) { IsActive = false; }

        public void Execute(SmartAIContext context)
        {
            LastExecutionTime = Time.time;
            IsActive = Duration > 0f;
            OnActivate(context);
        }
    }

    // =========================================================================================================
    // MAIN AI CONTROLLER COMPONENT
    // =========================================================================================================
    public class AdvancedSmartAI : MonoBehaviour
    {
        public static event Action<AdvancedSmartAI> OnAnyAISpawned;
        public static readonly List<Func<AdvancedSmartAI, SmartAIAbility>> GlobalAbilityFactories = new List<Func<AdvancedSmartAI, SmartAIAbility>>();

        public event Action<SmartAIContext> OnAIInitialized;
        public event Action<SmartAIContext, float, float> OnHealthChanged;
        public event Action<SmartAIContext, GameObject> OnTargetAcquired;
        public event Action<SmartAIContext, PhysicalBehaviour> OnWeaponEquipped;
        public event Action<SmartAIContext, PhysicalBehaviour> OnWeaponFired;
        public event Action<SmartAIContext, PhysicalBehaviour, Vector2> OnWeaponThrown;
        public event Action<SmartAIContext, SmartAIAbility> OnAbilityTriggered;
        public event Action<SmartAIContext, StuckLevel> OnStuckLevelChanged;

        public PersonBehaviour Person { get; private set; }
        public LimbBehaviour HeadLimb { get; private set; }
        public LimbBehaviour TorsoLimb { get; private set; }
        public LimbBehaviour PelvisLimb { get; private set; }
        public LimbBehaviour UpperArmFrontLimb { get; private set; }
        public LimbBehaviour LowerArmFrontLimb { get; private set; }
        public LimbBehaviour UpperArmBackLimb { get; private set; }
        public LimbBehaviour LowerArmBackLimb { get; private set; }
        public LimbBehaviour FrontLegLimb { get; private set; }
        public LimbBehaviour BackLegLimb { get; private set; }
        public LimbBehaviour FrontFootLimb { get; private set; }
        public LimbBehaviour BackFootLimb { get; private set; }

        public LimbBehaviour FrontArmLimb { get { return LowerArmFrontLimb != null ? LowerArmFrontLimb : UpperArmFrontLimb; } }
        public LimbBehaviour BackArmLimb { get { return LowerArmBackLimb != null ? LowerArmBackLimb : UpperArmBackLimb; } }

        public Rigidbody2D TorsoRigidbody { get { return TorsoLimb != null && TorsoLimb.PhysicalBehaviour != null ? TorsoLimb.PhysicalBehaviour.rigidbody : null; } }
        public Rigidbody2D PelvisRigidbody { get { return PelvisLimb != null && PelvisLimb.PhysicalBehaviour != null ? PelvisLimb.PhysicalBehaviour.rigidbody : null; } }
        public Rigidbody2D HeadRigidbody { get { return HeadLimb != null && HeadLimb.PhysicalBehaviour != null ? HeadLimb.PhysicalBehaviour.rigidbody : null; } }
        public Rigidbody2D FrontArmRigidbody { get { return FrontArmLimb != null && FrontArmLimb.PhysicalBehaviour != null ? FrontArmLimb.PhysicalBehaviour.rigidbody : null; } }

        public int DetectedTier { get; private set; }
        public float CurrentEnergy { get; private set; }
        public float MaxEnergy { get; private set; }
        public string ModdedTierInfo { get { return DetectedTier >= 0 ? "Tier " + DetectedTier : ""; } }

        public AIConfig Config { get; private set; }
        public SmartAIContext Context { get; private set; }
        public AINavigationController Navigation { get; private set; }
        public AICombatController Combat { get; private set; }
        public AIAbilityController AbilitySystem { get; private set; }

        private float lastKnownHealthPercent = 1f;
        private float healthCheckTimer = 0f;
        private float crossModSyncTimer = 0f;
        private bool isInitialized = false;

        public bool IsAlive { get { return Person != null && Person.Consciousness > 0.05f && HeadLimb != null && HeadLimb.Health > 1f && TorsoLimb != null && TorsoLimb.Health > 1f; } }
        public bool IsConscious { get { return Person != null && Person.Consciousness > 0.2f && Person.ShockLevel < 0.9f; } }

        private void Awake()
        {
            Person = GetComponent<PersonBehaviour>();
            Config = new AIConfig();
            Context = new SmartAIContext(this);
            DetectedTier = -1;
            CurrentEnergy = 0f;
            MaxEnergy = 0f;
        }

        public void InitializeAI()
        {
            if (isInitialized) return;

            CacheLimbReferences();
            DetectCrossModAttributes();
            SetupSubsystems();
            ApplyHealthMultiplier();
            RegisterContextMenuOptions();

            // Load any 3rd-party custom abilities registered by other mods via GlobalAbilityFactories
            for (int i = 0; i < GlobalAbilityFactories.Count; i++)
            {
                try
                {
                    Func<AdvancedSmartAI, SmartAIAbility> factory = GlobalAbilityFactories[i];
                    SmartAIAbility customAbility = factory != null ? factory(this) : null;
                    if (customAbility != null)
                    {
                        AbilitySystem.RegisterAbility(customAbility);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[AdvancedSmartAI] Error registering 3rd-party ability: " + ex.Message);
                }
            }

            isInitialized = true;
            if (OnAIInitialized != null) OnAIInitialized(Context);
            if (OnAnyAISpawned != null) OnAnyAISpawned(this);
        }

        private void CacheLimbReferences()
        {
            if (Person == null || Person.Limbs == null) return;

            LimbBehaviour[] limbs = Person.Limbs;
            for (int i = 0; i < limbs.Length; i++)
            {
                LimbBehaviour limb = limbs[i];
                if (limb == null) continue;
                string limbName = limb.gameObject.name.ToLower();

                if (limbName.Contains("head")) HeadLimb = limb;
                else if (limbName.Contains("upperbody") || limbName.Contains("torso")) TorsoLimb = limb;
                else if (limbName.Contains("lowerbody") || limbName.Contains("middlebody")) PelvisLimb = limb;
                else if (limbName.Contains("upperarmfront") || (limbName.Contains("upperarm") && !limbName.Contains("back"))) UpperArmFrontLimb = limb;
                else if (limbName.Contains("lowerarmfront") || limbName.Contains("forearmfront") || (limbName.Contains("lowerarm") && !limbName.Contains("back"))) LowerArmFrontLimb = limb;
                else if (limbName.Contains("upperarmback")) UpperArmBackLimb = limb;
                else if (limbName.Contains("lowerarmback") || limbName.Contains("forearmback")) LowerArmBackLimb = limb;
                else if (limbName.Contains("legfront") || limbName.Contains("lowerlegfront")) FrontLegLimb = limb;
                else if (limbName.Contains("legback") || limbName.Contains("lowerlegback")) BackLegLimb = limb;
                else if (limbName.Contains("footfront")) FrontFootLimb = limb;
                else if (limbName.Contains("footback")) BackFootLimb = limb;

                limb.BreakingThreshold = Mathf.Max(limb.BreakingThreshold, 300f);
            }
        }

        public bool IsOwnLimb(Collider2D col)
        {
            if (col == null) return false;
            if (col.transform.root == transform.root) return true;

            LimbBehaviour limb = col.GetComponent<LimbBehaviour>();
            if (limb != null)
            {
                if (limb.Person == Person) return true;
                if (Person != null && Person.Limbs != null)
                {
                    for (int i = 0; i < Person.Limbs.Length; i++)
                    {
                        if (Person.Limbs[i] == limb) return true;
                    }
                }
            }
            return false;
        }

        private void DetectCrossModAttributes()
        {
            DetectedTier = CrossModAdapterEngine.DetectEntityTier(gameObject);
            EnergyData eData = CrossModAdapterEngine.GetEnergyStatus(gameObject);
            CurrentEnergy = eData.Current;
            MaxEnergy = eData.Max;

            if (DetectedTier >= 1)
            {
                float tierMultiplier = 1f + (DetectedTier * 0.45f);
                Config.HealthMultiplier = Mathf.Max(Config.HealthMultiplier, tierMultiplier);
                Config.MovementSpeedMultiplier = Mathf.Max(Config.MovementSpeedMultiplier, 1.0f + (DetectedTier * 0.10f));
                Config.ReactionTimeMultiplier = Mathf.Max(Config.ReactionTimeMultiplier, 1.0f + (DetectedTier * 0.15f));
                Config.AccuracyMultiplier = Mathf.Clamp(0.7f + (DetectedTier * 0.05f), 0.7f, 1.0f);
            }
        }

        private void SetupSubsystems()
        {
            Navigation = new AINavigationController(this);
            Combat = new AICombatController(this);
            AbilitySystem = new AIAbilityController(this);
        }

        public void ApplyHealthMultiplier()
        {
            if (Person == null || Person.Limbs == null) return;

            float targetMaxHealth = 100f * Config.HealthMultiplier;
            LimbBehaviour[] limbs = Person.Limbs;
            for (int i = 0; i < limbs.Length; i++)
            {
                LimbBehaviour limb = limbs[i];
                if (limb == null) continue;
                limb.Health = targetMaxHealth;
                limb.InitialHealth = targetMaxHealth;
                limb.Numbness = Mathf.Clamp(limb.Numbness * 0.5f, 0f, 0.2f);
            }

            Person.Consciousness = 1.0f;
            Person.ShockLevel = 0.0f;
            Person.PainLevel = 0.0f;
        }

        private void RegisterContextMenuOptions()
        {
            if (TorsoLimb == null || TorsoLimb.PhysicalBehaviour == null) return;

            PhysicalBehaviour pb = TorsoLimb.PhysicalBehaviour;
            if (pb.ContextMenuOptions == null || pb.ContextMenuOptions.Buttons == null) return;

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_preset",
                "AI Preset [" + Config.CurrentPreset + "]",
                "Cycle through AI difficulty/behavior presets.",
                new UnityAction[] { () =>
                {
                    int nextPreset = ((int)Config.CurrentPreset + 1) % Enum.GetValues(typeof(AIPreset)).Length;
                    Config.ApplyPreset((AIPreset)nextPreset, this);
                }}
            ));

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_stance",
                "AI Stance [" + Config.Stance + "]",
                "Cycle stance (Aggressive, Defensive, Follower, Neutral).",
                new UnityAction[] { () =>
                {
                    int nextStance = ((int)Config.Stance + 1) % Enum.GetValues(typeof(AIStance)).Length;
                    Config.Stance = (AIStance)nextStance;
                    ModAPI.Notify("AI Stance set to: " + Config.Stance);
                }}
            ));

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_health",
                "Cycle Health [" + Config.HealthMultiplier + "x]",
                "Scale AI health (0.5x, 1.0x, 1.8x, 3.0x, 5.0x).",
                new UnityAction[] { () =>
                {
                    float[] healthSteps = new float[] { 0.5f, 1.0f, 1.8f, 3.0f, 5.0f };
                    int currentIndex = Array.FindIndex(healthSteps, h => Mathf.Approximately(h, Config.HealthMultiplier));
                    int nextIndex = (currentIndex + 1) % healthSteps.Length;
                    Config.HealthMultiplier = healthSteps[nextIndex];
                    ApplyHealthMultiplier();
                    ModAPI.Notify("AI Health Multiplier set to " + Config.HealthMultiplier + "x");
                }}
            ));

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_speed",
                "Cycle Speed [" + Config.MovementSpeedMultiplier + "x]",
                "Scale AI movement speed (0.75x, 1.0x, 1.15x, 1.4x, 1.8x).",
                new UnityAction[] { () =>
                {
                    float[] speedSteps = new float[] { 0.75f, 1.0f, 1.15f, 1.4f, 1.8f };
                    int currentIndex = Array.FindIndex(speedSteps, s => Mathf.Approximately(s, Config.MovementSpeedMultiplier));
                    int nextIndex = (currentIndex + 1) % speedSteps.Length;
                    Config.MovementSpeedMultiplier = speedSteps[nextIndex];
                    ModAPI.Notify("AI Speed Multiplier set to " + Config.MovementSpeedMultiplier + "x");
                }}
            ));

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_accuracy",
                "Cycle Accuracy [" + (int)(Config.AccuracyMultiplier * 100) + "%]",
                "Scale weapon aiming accuracy (30%, 60%, 85%, 100%).",
                new UnityAction[] { () =>
                {
                    float[] accSteps = new float[] { 0.30f, 0.60f, 0.85f, 1.0f };
                    int currentIndex = Array.FindIndex(accSteps, a => Mathf.Approximately(a, Config.AccuracyMultiplier));
                    int nextIndex = (currentIndex + 1) % accSteps.Length;
                    Config.AccuracyMultiplier = accSteps[nextIndex];
                    ModAPI.Notify("AI Accuracy set to " + (int)(Config.AccuracyMultiplier * 100) + "%");
                }}
            ));

            pb.ContextMenuOptions.Buttons.Add(new ContextMenuButton(
                "smart_ai_stats",
                "Inspect AI Diagnostics",
                "Display current health, combat target, equipped weapon, tier stats.",
                new UnityAction[] { () =>
                {
                    string targetName = Combat.CurrentTarget != null ? Combat.CurrentTarget.name : "None";
                    string weaponName = Combat.HeldWeapon != null ? Combat.HeldWeapon.name : "Unarmed";
                    string tierStr = DetectedTier >= 0 ? " | Tier: " + DetectedTier + " (Energy: " + CurrentEnergy.ToString("F0") + "/" + MaxEnergy.ToString("F0") + ")" : "";
                    ModAPI.Notify("[Smart AI Diagnostics]\nHP: " + (int)(GetAverageHealthPercent() * 100) + "% | Target: " + targetName + "\nWeapon: " + weaponName + " | Stuck: " + Navigation.CurrentStuckLevel + "\nPreset: " + Config.CurrentPreset + " | Stance: " + Config.Stance + tierStr);
                }}
            ));
        }

        private void Update()
        {
            if (!IsAlive || !IsConscious) return;

            float dt = Time.deltaTime;

            healthCheckTimer += dt;
            if (healthCheckTimer >= 0.2f)
            {
                healthCheckTimer = 0f;
                float currentHp = GetAverageHealthPercent();
                if (Mathf.Abs(currentHp - lastKnownHealthPercent) > 0.05f)
                {
                    if (OnHealthChanged != null) OnHealthChanged(Context, lastKnownHealthPercent, currentHp);
                    lastKnownHealthPercent = currentHp;
                }
            }

            crossModSyncTimer += dt;
            if (crossModSyncTimer >= 0.5f)
            {
                crossModSyncTimer = 0f;
                EnergyData eData = CrossModAdapterEngine.GetEnergyStatus(gameObject);
                CurrentEnergy = eData.Current;
                MaxEnergy = eData.Max;
            }

            Navigation.UpdateNavigation(dt);
            Combat.UpdateCombat(dt);
            if (Config.EnableCustomAbilities)
            {
                AbilitySystem.UpdateAbilities(dt);
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive || !IsConscious) return;

            float fixedDt = Time.fixedDeltaTime;
            Navigation.FixedUpdateNavigation(fixedDt);
            Combat.FixedUpdateCombat(fixedDt);
        }

        public float GetAverageHealthPercent()
        {
            if (Person == null || Person.Limbs == null || Person.Limbs.Length == 0) return 0f;
            float total = 0f;
            int count = 0;
            LimbBehaviour[] limbs = Person.Limbs;
            for (int i = 0; i < limbs.Length; i++)
            {
                LimbBehaviour limb = limbs[i];
                if (limb != null)
                {
                    total += Mathf.Clamp01(limb.Health / (100f * Config.HealthMultiplier));
                    count++;
                }
            }
            return count > 0 ? (total / count) : 0f;
        }

        public void NotifyWeaponEquipped(PhysicalBehaviour weapon) { if (OnWeaponEquipped != null) OnWeaponEquipped(Context, weapon); }
        public void NotifyWeaponFired(PhysicalBehaviour weapon) { if (OnWeaponFired != null) OnWeaponFired(Context, weapon); }
        public void NotifyWeaponThrown(PhysicalBehaviour weapon, Vector2 velocity) { if (OnWeaponThrown != null) OnWeaponThrown(Context, weapon, velocity); }
        public void NotifyTargetAcquired(GameObject target) { if (OnTargetAcquired != null) OnTargetAcquired(Context, target); }
        public void NotifyAbilityTriggered(SmartAIAbility ability) { if (OnAbilityTriggered != null) OnAbilityTriggered(Context, ability); }
        public void NotifyStuckLevelChanged(StuckLevel level) { if (OnStuckLevelChanged != null) OnStuckLevelChanged(Context, level); }

        public void RegisterAbility(SmartAIAbility ability) { AbilitySystem.RegisterAbility(ability); }

        public static void RegisterGlobalAbilityFactory(Func<AdvancedSmartAI, SmartAIAbility> factory)
        {
            if (factory != null && !GlobalAbilityFactories.Contains(factory))
            {
                GlobalAbilityFactories.Add(factory);
            }
        }
    }

    // =========================================================================================================
    // CORE NAVIGATION & LOCOMOTION
    // =========================================================================================================
    public class AINavigationController
    {
        private readonly AdvancedSmartAI ai;

        public float MoveInput { get; private set; }
        public float FacingDirection { get; private set; }
        public StuckLevel CurrentStuckLevel { get; private set; }

        private float lastJumpTime = -999f;
        private float jumpCooldown = 1.5f;
        private bool isGrounded = true;

        private Vector2 lastPosition;
        private float stuckTimer = 0f;
        private float unstuckActionTimer = 0f;

        public AINavigationController(AdvancedSmartAI ai)
        {
            this.ai = ai;
            FacingDirection = 1f;
            CurrentStuckLevel = StuckLevel.None;
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

            if (ai.Person != null)
            {
                ai.Person.DesiredWalkingDirection = MoveInput * Mathf.Clamp(ai.Config.MovementSpeedMultiplier, 0.5f, 2.0f);
            }
        }

        public void FixedUpdateNavigation(float fixedDeltaTime)
        {
        }

        private void DetermineMovementGoal()
        {
            MoveInput = 0f;
            if (unstuckActionTimer > 0f) return;

            Vector2 currentPos = ai.TorsoLimb != null ? (Vector2)ai.TorsoLimb.transform.position : (Vector2)ai.transform.position;

            if (ai.Config.AllowWeaponPickup && ai.Combat.NeedsWeapon && ai.Combat.NearestWeapon != null)
            {
                float weaponDeltaX = ai.Combat.NearestWeapon.transform.position.x - currentPos.x;
                if (Mathf.Abs(weaponDeltaX) > 0.4f)
                {
                    MoveInput = Mathf.Sign(weaponDeltaX);
                    FacingDirection = MoveInput;
                }
                return;
            }

            if (ai.Combat.CurrentTarget != null)
            {
                float targetDeltaX = ai.Combat.CurrentTarget.transform.position.x - currentPos.x;
                float targetDist = Mathf.Abs(targetDeltaX);
                FacingDirection = Mathf.Sign(targetDeltaX);

                if (ai.Config.Stance == AIStance.Aggressive)
                {
                    float preferredDistance = (ai.Combat.CurrentWeaponType == WeaponType.Firearm) ? 5.5f : 1.2f;

                    if (targetDist > preferredDistance + 0.4f)
                    {
                        MoveInput = Mathf.Sign(targetDeltaX);
                    }
                    else if (targetDist < preferredDistance - 1.2f && ai.Combat.CurrentWeaponType == WeaponType.Firearm)
                    {
                        MoveInput = -Mathf.Sign(targetDeltaX);
                    }
                }
                else if (ai.Config.Stance == AIStance.Defensive)
                {
                    if (targetDist < 4.0f)
                    {
                        MoveInput = -Mathf.Sign(targetDeltaX);
                    }
                }
            }
        }

        private void CheckGroundedStatus()
        {
            if (ai.PelvisLimb == null) return;
            RaycastHit2D groundHit = Physics2D.Raycast(ai.PelvisLimb.transform.position, Vector2.down, 1.8f, LayerMask.GetMask("Default", "Objects", "Debris"));
            isGrounded = groundHit.collider != null && !ai.IsOwnLimb(groundHit.collider);
        }

        private void ScanEnvironmentAndObstacles()
        {
            if (!ai.Config.EnableJumpNavigation || !isGrounded || MoveInput == 0f || ai.TorsoLimb == null) return;

            Vector2 originWaist = ai.TorsoLimb.transform.position;
            Vector2 originFeet = ai.PelvisLimb != null ? (Vector2)ai.PelvisLimb.transform.position + (Vector2.down * 0.6f) : originWaist + (Vector2.down * 0.6f);
            Vector2 moveDir = new Vector2(FacingDirection, 0f);

            RaycastHit2D lowObstacle = Physics2D.Raycast(originFeet, moveDir, 1.3f, LayerMask.GetMask("Default", "Objects"));
            bool lowBlocked = lowObstacle.collider != null && !ai.IsOwnLimb(lowObstacle.collider);

            RaycastHit2D midObstacle = Physics2D.Raycast(originWaist, moveDir, 1.6f, LayerMask.GetMask("Default", "Objects"));
            bool midBlocked = midObstacle.collider != null && !ai.IsOwnLimb(midObstacle.collider);

            RaycastHit2D ceilingCheck = Physics2D.Raycast(originWaist + (Vector2.up * 0.8f), Vector2.up, 1.5f, LayerMask.GetMask("Default", "Objects"));
            bool ceilingBlocked = ceilingCheck.collider != null && !ai.IsOwnLimb(ceilingCheck.collider);

            Vector2 pitProbePos = originFeet + (moveDir * 1.5f);
            RaycastHit2D pitCheck = Physics2D.Raycast(pitProbePos, Vector2.down, 2.5f, LayerMask.GetMask("Default", "Objects"));
            bool gapDetected = (pitCheck.collider == null);

            if ((lowBlocked || midBlocked || gapDetected) && !ceilingBlocked)
            {
                ExecuteJump(midBlocked || gapDetected);
            }
        }

        public void ExecuteJump(bool highJump)
        {
            if (Time.time < lastJumpTime + jumpCooldown || !isGrounded || ai.TorsoRigidbody == null) return;

            lastJumpTime = Time.time;

            float jumpForce = highJump ? 6.0f : 4.5f;
            float forwardForce = 1.5f * FacingDirection;

            Vector2 impulse = new Vector2(forwardForce, jumpForce);
            ai.TorsoRigidbody.AddForce(impulse, ForceMode2D.Impulse);
        }

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

            if (Mathf.Abs(MoveInput) > 0.1f && displacement < 0.10f * deltaTime)
            {
                stuckTimer += deltaTime;
            }
            else
            {
                stuckTimer = Mathf.Max(0f, stuckTimer - (deltaTime * 2f));
            }

            StuckLevel newStuckLevel = StuckLevel.None;
            if (stuckTimer > 3.0f) newStuckLevel = StuckLevel.Severe;
            else if (stuckTimer > 1.5f) newStuckLevel = StuckLevel.Moderate;
            else if (stuckTimer > 0.7f) newStuckLevel = StuckLevel.Minor;

            if (newStuckLevel != CurrentStuckLevel)
            {
                CurrentStuckLevel = newStuckLevel;
                ai.NotifyStuckLevelChanged(CurrentStuckLevel);
            }

            if (stuckTimer > 0.75f)
            {
                PerformUnstuckAction();
            }
        }

        private void PerformUnstuckAction()
        {
            unstuckActionTimer = 0.6f;

            switch (CurrentStuckLevel)
            {
                case StuckLevel.Minor:
                    FacingDirection = -FacingDirection;
                    MoveInput = FacingDirection;
                    break;

                case StuckLevel.Moderate:
                    ExecuteJump(true);
                    break;

                case StuckLevel.Severe:
                    FacingDirection = -FacingDirection;
                    MoveInput = FacingDirection;
                    ExecuteJump(true);
                    stuckTimer = 0f;
                    break;
            }
        }
    }

    // =========================================================================================================
    // COMBAT & WEAPON CONTROLLER (AUTHENTIC TWO-HANDED GRIP & NATURAL ARM POSING)
    // =========================================================================================================
    public class AICombatController
    {
        private readonly AdvancedSmartAI ai;

        public GameObject CurrentTarget { get; private set; }
        public PhysicalBehaviour HeldWeapon { get; private set; }
        public WeaponType CurrentWeaponType { get; private set; }
        public PhysicalBehaviour NearestWeapon { get; private set; }
        public float TargetDistance { get; private set; }
        public int TargetTier { get; private set; }
        public bool NeedsWeapon { get { return HeldWeapon == null || (CurrentWeaponType == WeaponType.Firearm && IsWeaponOutOfAmmo(HeldWeapon)); } }

        private FixedJoint2D primaryHandJoint;
        private FixedJoint2D secondaryHandJoint;
        private float scanTimer = 0f;
        private float fireCooldownTimer = 0f;
        private float reactionDelayTimer = 0f;
        private int dryFireCount = 0;

        private static readonly string[] FirearmTypeKeywords = new string[] {
            "firearm", "gun", "rifle", "pistol", "shotgun", "blaster", "laser", "cannon", "revolver", "weapon", "projectile", "ak", "m4", "sniper", "smg"
        };

        public AICombatController(AdvancedSmartAI ai)
        {
            this.ai = ai;
            CurrentWeaponType = WeaponType.None;
            TargetDistance = 999f;
            TargetTier = -1;
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
            AnimateArmAndAim(fixedDeltaTime);
        }

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
            int detectedEnemyTier = -1;

            for (int i = 0; i < candidates.Length; i++)
            {
                Collider2D col = candidates[i];
                if (col == null || ai.IsOwnLimb(col)) continue;

                LimbBehaviour limb = col.GetComponent<LimbBehaviour>();
                if (limb == null || limb.Person == null || limb.Person == ai.Person) continue;

                PersonBehaviour targetPerson = limb.Person;
                if (targetPerson.Consciousness <= 0.05f || limb.Health <= 0f) continue;

                float dist = Vector2.Distance(myPos, limb.transform.position);
                if (dist > ai.Config.VisionRange) continue;

                RaycastHit2D los = Physics2D.Linecast(myPos, limb.transform.position, LayerMask.GetMask("Default", "Objects"));
                if (los.collider != null && los.collider.transform.root != targetPerson.transform.root && !ai.IsOwnLimb(los.collider)) continue;

                float score = 100f - dist;

                PhysicalBehaviour pb = col.GetComponent<PhysicalBehaviour>();
                if (pb != null && pb.beingHeldByGripper)
                {
                    score += 30f;
                }

                int enemyTier = CrossModAdapterEngine.DetectEntityTier(targetPerson.gameObject);
                if (enemyTier > 0)
                {
                    score += enemyTier * 10f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = targetPerson.gameObject;
                    detectedEnemyTier = enemyTier;
                }
            }

            if (bestTarget != CurrentTarget)
            {
                CurrentTarget = bestTarget;
                TargetTier = detectedEnemyTier;
                reactionDelayTimer = 0.25f / ai.Config.ReactionTimeMultiplier;
                if (CurrentTarget != null)
                {
                    ai.NotifyTargetAcquired(CurrentTarget);
                }
            }

            TargetDistance = CurrentTarget != null ? Vector2.Distance(myPos, CurrentTarget.transform.position) : 999f;
        }

        private void ScanForWeapons()
        {
            if (!ai.Config.AllowWeaponPickup || !NeedsWeapon || ai.TorsoLimb == null)
            {
                NearestWeapon = null;
                return;
            }

            Vector2 myPos = ai.TorsoLimb.transform.position;
            Collider2D[] items = Physics2D.OverlapCircleAll(myPos, ai.Config.WeaponSearchRadius);

            PhysicalBehaviour bestWeapon = null;
            float bestScore = -999f;

            for (int i = 0; i < items.Length; i++)
            {
                Collider2D col = items[i];
                if (col == null || ai.IsOwnLimb(col)) continue;

                // STRICT FILTER: reject any limb, person, or living entity collider
                if (col.GetComponent<LimbBehaviour>() != null || col.GetComponentInParent<PersonBehaviour>() != null) continue;

                PhysicalBehaviour pb = col.GetComponent<PhysicalBehaviour>();
                if (pb == null || pb.beingHeldByGripper) continue;
                if (pb.GetComponent<LimbBehaviour>() != null || pb.GetComponentInParent<PersonBehaviour>() != null) continue;

                WeaponType type = ClassifyWeapon(pb);
                if (type == WeaponType.None) continue;
                if (type == WeaponType.Firearm && IsWeaponOutOfAmmo(pb)) continue;

                float dist = Vector2.Distance(myPos, pb.transform.position);

                float score = 50f - dist;
                if (type == WeaponType.AbilityWeapon) score += 25f;
                else if (type == WeaponType.Firearm) score += 20f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWeapon = pb;
                }
            }

            NearestWeapon = bestWeapon;
        }

        private void UpdateWeaponPickupLogic()
        {
            if (NearestWeapon == null || HeldWeapon != null || ai.FrontArmLimb == null) return;

            Vector2 handPos = ai.FrontArmLimb.transform.position;
            Vector2 weaponPos = NearestWeapon.transform.position;
            float distToWeapon = Vector2.Distance(handPos, weaponPos);

            if (distToWeapon <= 1.5f)
            {
                EquipWeapon(NearestWeapon);
            }
        }

        public void EquipWeapon(PhysicalBehaviour weapon)
        {
            if (weapon == null || ai.FrontArmLimb == null) return;

            // STRICT FILTER: NEVER grab humans or limbs
            if (weapon.GetComponent<LimbBehaviour>() != null || weapon.GetComponentInParent<PersonBehaviour>() != null)
            {
                return;
            }

            // 1. Ignore collisions between weapon and all limbs of this AI
            IgnoreCollisionsWithAI(weapon, true);

            // 2. Identify holding positions on the weapon (cyan grip points)
            Vector2 primaryGripLocal = Vector2.zero;
            Vector2 secondaryGripLocal = Vector2.zero;
            bool hasSecondaryGrip = false;

            if (weapon.HoldingPositions != null && weapon.HoldingPositions.Length > 0)
            {
                primaryGripLocal = (Vector2)weapon.HoldingPositions[0];
                if (weapon.HoldingPositions.Length > 1)
                {
                    secondaryGripLocal = (Vector2)weapon.HoldingPositions[1];
                    hasSecondaryGrip = true;
                }
            }

            float facingDir = ai.Navigation.FacingDirection >= 0 ? 1f : -1f;
            float baseAngle = (facingDir >= 0) ? 0f : 180f;

            // 3. Orient and position weapon cleanly at the front hand
            weapon.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);
            Vector2 handPos = ai.FrontArmLimb.transform.position;
            Vector2 worldPrimaryOffset = (Vector2)weapon.transform.TransformVector(primaryGripLocal);
            weapon.transform.position = handPos - worldPrimaryOffset;

            Rigidbody2D weaponRb = weapon.rigidbody;
            if (weaponRb != null)
            {
                weaponRb.velocity = Vector2.zero;
                weaponRb.angularVelocity = 0f;
            }

            // 4. Attach primary hand joint (Front Arm / Hand)
            if (primaryHandJoint != null) UnityEngine.Object.Destroy(primaryHandJoint);
            primaryHandJoint = ai.FrontArmLimb.gameObject.AddComponent<FixedJoint2D>();
            primaryHandJoint.connectedBody = weaponRb;
            primaryHandJoint.autoConfigureConnectedAnchor = false;
            primaryHandJoint.anchor = Vector2.zero;
            primaryHandJoint.connectedAnchor = primaryGripLocal;
            primaryHandJoint.dampingRatio = 1f;
            primaryHandJoint.frequency = 0f;

            // 5. Attach secondary hand joint if two-handed weapon
            if (secondaryHandJoint != null) UnityEngine.Object.Destroy(secondaryHandJoint);
            if (hasSecondaryGrip && ai.BackArmLimb != null)
            {
                secondaryHandJoint = ai.BackArmLimb.gameObject.AddComponent<FixedJoint2D>();
                secondaryHandJoint.connectedBody = weaponRb;
                secondaryHandJoint.autoConfigureConnectedAnchor = false;
                secondaryHandJoint.anchor = Vector2.zero;
                secondaryHandJoint.connectedAnchor = secondaryGripLocal;
                secondaryHandJoint.dampingRatio = 1f;
                secondaryHandJoint.frequency = 0f;
            }

            weapon.beingHeldByGripper = true;
            HeldWeapon = weapon;
            CurrentWeaponType = ClassifyWeapon(weapon);
            dryFireCount = 0;
            NearestWeapon = null;

            ai.NotifyWeaponEquipped(weapon);
        }

        private void IgnoreCollisionsWithAI(PhysicalBehaviour weapon, bool ignore)
        {
            if (weapon == null || ai.Person == null || ai.Person.Limbs == null) return;

            Collider2D[] weaponCols = weapon.GetComponentsInChildren<Collider2D>();
            LimbBehaviour[] limbs = ai.Person.Limbs;

            for (int w = 0; w < weaponCols.Length; w++)
            {
                Collider2D wCol = weaponCols[w];
                if (wCol == null) continue;

                for (int l = 0; l < limbs.Length; l++)
                {
                    LimbBehaviour limb = limbs[l];
                    if (limb == null) continue;

                    Collider2D lCol = limb.GetComponent<Collider2D>();
                    if (lCol != null)
                    {
                        Physics2D.IgnoreCollision(wCol, lCol, ignore);
                    }
                }
            }
        }

        private void AnimateArmAndAim(float fixedDeltaTime)
        {
            if (HeldWeapon == null || ai.FrontArmLimb == null) return;

            float facing = (ai.Navigation.FacingDirection >= 0) ? 1f : -1f;
            float desiredAngle = (facing >= 0) ? 0f : 180f;

            if (CurrentTarget != null && ai.TorsoLimb != null)
            {
                Vector2 diff = (Vector2)CurrentTarget.transform.position - (Vector2)ai.FrontArmLimb.transform.position;
                desiredAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            }

            // Restrict aiming to natural forward humanoid angles
            if (facing >= 0)
            {
                desiredAngle = Mathf.Clamp(desiredAngle, -40f, 50f);
            }
            else
            {
                if (desiredAngle < 0) desiredAngle += 360f;
                desiredAngle = Mathf.Clamp(desiredAngle, 130f, 220f);
            }

            // Smooth, gentle arm lifting torque (never rips joints or snaps limbs)
            float currentAngle = ai.FrontArmLimb.transform.eulerAngles.z;
            float angleDelta = Mathf.DeltaAngle(currentAngle, desiredAngle);
            float armTorque = Mathf.Clamp(angleDelta * 0.35f, -2.5f, 2.5f);

            if (ai.FrontArmRigidbody != null)
            {
                ai.FrontArmRigidbody.AddTorque(armTorque - (ai.FrontArmRigidbody.angularVelocity * 0.05f), ForceMode2D.Force);
            }

            if (ai.UpperArmFrontLimb != null && ai.UpperArmFrontLimb.PhysicalBehaviour != null && ai.UpperArmFrontLimb.PhysicalBehaviour.rigidbody != null)
            {
                ai.UpperArmFrontLimb.PhysicalBehaviour.rigidbody.AddTorque(armTorque * 0.6f, ForceMode2D.Force);
            }
        }

        private void InspectHeldWeapon()
        {
            if (HeldWeapon != null && primaryHandJoint == null)
            {
                if (secondaryHandJoint != null)
                {
                    UnityEngine.Object.Destroy(secondaryHandJoint);
                    secondaryHandJoint = null;
                }
                HeldWeapon.beingHeldByGripper = false;
                HeldWeapon = null;
                CurrentWeaponType = WeaponType.None;
                dryFireCount = 0;
            }
        }

        public WeaponType ClassifyWeapon(PhysicalBehaviour pb)
        {
            if (pb == null || pb.gameObject == null) return WeaponType.None;
            if (pb.GetComponent<LimbBehaviour>() != null || pb.GetComponentInParent<PersonBehaviour>() != null) return WeaponType.None;

            if (CrossModAdapterEngine.IsModdedAbilityWeapon(pb.gameObject))
            {
                return WeaponType.AbilityWeapon;
            }

            Component[] components = pb.gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component comp = components[i];
                if (comp == null) continue;
                string typeName = comp.GetType().Name.ToLower();

                for (int k = 0; k < FirearmTypeKeywords.Length; k++)
                {
                    if (typeName.Contains(FirearmTypeKeywords[k]))
                    {
                        return WeaponType.Firearm;
                    }
                }
            }

            if (pb.Properties != null && pb.Properties.Sharp)
            {
                return WeaponType.Melee;
            }

            string objName = pb.gameObject.name.ToLower();
            for (int k = 0; k < FirearmTypeKeywords.Length; k++)
            {
                if (objName.Contains(FirearmTypeKeywords[k])) return WeaponType.Firearm;
            }

            if (objName.Contains("sword") || objName.Contains("knife") || objName.Contains("axe") || objName.Contains("baton") || objName.Contains("spear") || objName.Contains("blade") || objName.Contains("dagger") || objName.Contains("hammer") || objName.Contains("mace"))
                return WeaponType.Melee;

            return WeaponType.None;
        }

        private bool IsWeaponOutOfAmmo(PhysicalBehaviour weapon)
        {
            if (weapon == null) return true;
            if (dryFireCount >= 3) return true;

            Component[] components = weapon.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component comp = components[i];
                if (comp == null) continue;
                Type t = comp.GetType();

                try
                {
                    var hasAmmoProp = t.GetProperty("HasAmmo");
                    if (hasAmmoProp != null && hasAmmoProp.PropertyType == typeof(bool))
                    {
                        bool hasAmmo = (bool)hasAmmoProp.GetValue(comp, null);
                        if (!hasAmmo) return true;
                    }

                    var ammoField = t.GetField("Ammo") ?? t.GetField("CurrentAmmo");
                    if (ammoField != null && ammoField.FieldType == typeof(int))
                    {
                        int ammo = (int)ammoField.GetValue(comp);
                        if (ammo <= 0) return true;
                    }
                }
                catch { }
            }

            return false;
        }

        private void HandleAimingAndAttack(float deltaTime)
        {
            if (CurrentTarget == null || HeldWeapon == null) return;

            reactionDelayTimer -= deltaTime;
            fireCooldownTimer -= deltaTime;

            if (reactionDelayTimer > 0f) return;

            if (CurrentWeaponType == WeaponType.Firearm || CurrentWeaponType == WeaponType.AbilityWeapon)
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
            if (IsWeaponOutOfAmmo(HeldWeapon))
            {
                if (ai.Config.EnableWeaponThrowing)
                {
                    ThrowWeaponAtTarget();
                }
                return;
            }

            if (fireCooldownTimer <= 0f)
            {
                FireEquippedGun();
            }
        }

        private void HandleMeleeCombat()
        {
            if (TargetDistance <= ai.Config.MeleeRange)
            {
                HeldWeapon.SendMessage("Use", SendMessageOptions.DontRequireReceiver);

                // Gentle natural melee swing motion
                if (ai.FrontArmRigidbody != null)
                {
                    float facing = ai.Navigation.FacingDirection >= 0 ? 1f : -1f;
                    float swing = Mathf.Sin(Time.time * 9f) * 2.5f * facing;
                    ai.FrontArmRigidbody.AddTorque(swing, ForceMode2D.Force);
                }
            }
            else if (TargetDistance >= ai.Config.ThrowThresholdDistance && ai.Config.EnableWeaponThrowing)
            {
                ThrowWeaponAtTarget();
            }
        }

        private void FireEquippedGun()
        {
            if (HeldWeapon == null) return;

            HeldWeapon.SendMessage("Use", SendMessageOptions.DontRequireReceiver);
            HeldWeapon.SendMessage("UsePower", SendMessageOptions.DontRequireReceiver);

            fireCooldownTimer = UnityEngine.Random.Range(0.15f, 0.35f) / ai.Config.ReactionTimeMultiplier;

            if (IsWeaponOutOfAmmo(HeldWeapon))
            {
                dryFireCount++;
            }

            ai.NotifyWeaponFired(HeldWeapon);
        }

        public void ThrowWeaponAtTarget()
        {
            if (HeldWeapon == null || CurrentTarget == null) return;

            PhysicalBehaviour weaponToThrow = HeldWeapon;
            Rigidbody2D weaponRb = weaponToThrow.rigidbody;

            if (primaryHandJoint != null)
            {
                UnityEngine.Object.Destroy(primaryHandJoint);
                primaryHandJoint = null;
            }

            if (secondaryHandJoint != null)
            {
                UnityEngine.Object.Destroy(secondaryHandJoint);
                secondaryHandJoint = null;
            }

            weaponToThrow.beingHeldByGripper = false;
            HeldWeapon = null;
            CurrentWeaponType = WeaponType.None;

            if (weaponRb != null)
            {
                Vector2 origin = weaponToThrow.transform.position;
                Vector2 targetPos = CurrentTarget.transform.position;
                Vector2 displacement = targetPos - origin;

                float speed = 12f * ai.Config.ReactionTimeMultiplier;
                float angle = Mathf.Clamp(displacement.y / Mathf.Max(1f, Mathf.Abs(displacement.x)), -0.3f, 0.5f);

                Vector2 throwVelocity = new Vector2(Mathf.Sign(displacement.x) * speed, (angle * speed) + 2f);

                weaponRb.velocity = throwVelocity;
                weaponRb.AddTorque(UnityEngine.Random.Range(-100f, 100f));
            }

            ai.NotifyWeaponThrown(weaponToThrow, weaponRb != null ? weaponRb.velocity : Vector2.zero);
            scanTimer = 999f;
        }
    }

    // =========================================================================================================
    // MODULAR ABILITY CONTROLLER (OPEN API FOR 3RD-PARTY MODS)
    // =========================================================================================================
    public class AIAbilityController
    {
        private readonly AdvancedSmartAI ai;
        private readonly List<SmartAIAbility> abilities = new List<SmartAIAbility>();

        public IReadOnlyList<SmartAIAbility> Abilities { get { return abilities.AsReadOnly(); } }

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
                SmartAIAbility ability = abilities[i];

                if (ability.IsActive)
                {
                    ability.OnUpdate(ai.Context, deltaTime);

                    if (Time.time >= ability.LastExecutionTime + ability.Duration)
                    {
                        ability.OnDeactivate(ai.Context);
                    }
                }
                else if (ability.IsReady && ability.ShouldTrigger(ai.Context))
                {
                    ability.Execute(ai.Context);
                    ai.NotifyAbilityTriggered(ability);
                }
            }
        }
    }
}
