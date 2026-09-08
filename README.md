# Advanced Smart AI NPC - People Playground Mod

An advanced, highly intelligent autonomous NPC mod for **People Playground**. Designed from the ground up to replace vanilla floppy ragdoll behaviors with active balance stabilization, dynamic obstacle navigation, advanced combat tactics (both ranged firearms and melee weapon throwing), customizable in-game Context Menu parameters, and a fully modular Ability Hook API for 3rd-party mod integration.

---

## 🌟 Key Features

### 1. Core Intelligence, Navigation & Active Balance
- **Active Proportional-Derivative (PD) Balance Stabilization**: Keeps the ragdoll standing upright with realistic muscle tension and torque stabilization on the Torso, Pelvis, and Head. Prevents premature collapsing or ragdoll tripping.
- **Dynamic 4-Ray Environmental Scanner**:
  - **Low Ray**: Detects small ground obstacles, steps, and curbs.
  - **Mid Ray**: Identifies waist-to-chest-height barricades, crates, and walls.
  - **Overhead Ray**: Checks ceiling clearance before executing jumps.
  - **Pit / Ledge Ray**: Casts downward ahead of movement to detect drop-offs, acid pits, or gaps.
- **Dynamic Jumping & Vaulting**: Automatically vaults over obstacles and leaps across chasms with coordinated leg contraction/extension physics.
- **Multi-Tier Unstuck Routine**:
  - **Tier 1 (Minor)**: Jitters leg torque, executes high-knees stepping, and reverses direction.
  - **Tier 2 (Moderate)**: Pushes against contacting geometry and executes a power jump.
  - **Tier 3 (Severe)**: Delivers an explosive untangle leap to break free from tight corners or heavy debris.

### 2. Combat & Generic Weapon Interaction
- **Target Acquisition & Threat Scoring**: Scans within line-of-sight, prioritizing living armed threats, closing distances based on equipped weapon archetype.
- **Weapon Foraging & Pickup**: Actively seeks out nearby firearms or melee weapons when unarmed or out of ammo.
- **Smooth Arm Aiming with Accuracy Variance**: Rotates arm with proportional torque towards target center-of-mass, incorporating spread/sway scaled by the configured accuracy multiplier.
- **Ranged Gunplay & Reaction Delay**: Actuates firearm triggers via generic `Use` calls, respecting fire rates and reaction times.
- **Tactical Weapon Throwing**:
  - If a firearm **runs out of ammo**, the AI throws the empty gun at the enemy's head and immediately looks for a new weapon.
  - If holding a **melee weapon** and the enemy is at a distance (> 5.5m) or elevated out of reach, the AI computes a parabolic ballistic trajectory and throws the melee weapon at the enemy!
- **Universal Modded Weapon Compatibility**: Uses generic reflection and component inspection to identify and operate custom firearms and melee weapons from any other People Playground mod.

### 3. In-Game Context Menu Configuration
Right-click on the Smart AI (or convert any vanilla human into a Smart AI via the context menu):
- **Preset Selector**:
  - `Godlike`: 5.0x Health, 2.0x Speed, 100% Accuracy, 2.5x Reaction Time.
  - `Elite Soldier`: 2.5x Health, 1.4x Speed, 90% Accuracy, 1.8x Reaction Time.
  - `Standard AI`: 1.5x Health, 1.15x Speed, 75% Accuracy, 1.2x Reaction Time.
  - `Grunt / Weakling`: 0.8x Health, 0.85x Speed, 45% Accuracy, 0.7x Reaction Time.
  - `Civilian`: 0.5x Health, 0.75x Speed, 25% Accuracy, 0.5x Reaction Time (Neutral/Cowardly).
- **Attribute Multipliers**: Live cycling of `HealthMultiplier`, `SpeedMultiplier`, `AccuracyMultiplier`, and `ReactionTimeMultiplier`.
- **Stance Selector**: `Aggressive`, `Defensive`, `Follower`, `Neutral`.
- **Diagnostics**: Displays real-time health %, target name, held weapon, stuck status, and active abilities.

### 4. Modular Ability System & API Hook
- **Event Hooks**: Subscribe to `OnHealthChanged`, `OnTargetAcquired`, `OnWeaponEquipped`, `OnWeaponFired`, `OnWeaponThrown`, `OnAbilityTriggered`, `OnStuckLevelChanged`, and `OnAnyAISpawned`.
- **Built-in Abilities**:
  - `TacticalDashAbility`: Evasive high-speed leap when target is far or aiming.
  - `AdrenalineSurgeAbility`: Activates when Health < 40%, regenerating health and granting a 1.75x speed boost.
  - `KineticShockwaveAbility`: Triggers on severe stuck state or when crowded by multiple enemies, releasing a radial physics shockwave.

---

## 📁 Project Structure

```
running-people/
├── mod.json                     # Mod manifest metadata for People Playground
├── AdvancedSmartAI.cs           # Main mod source code (AI Controller, Balance, Nav, Combat, Abilities, Config)
├── ExampleAbilityExtension.cs   # Example extension showing how 3rd-party mods register custom abilities
└── README.md                    # Documentation & API reference
```

---

## 🚀 Installation

1. Copy the mod folder (`running-people` or your custom folder name) into your People Playground `Mods` directory:
   ```
   People Playground/Mods/AdvancedSmartAI/
   ├── mod.json
   └── AdvancedSmartAI.cs
   ```
2. Launch **People Playground**.
3. Open the **Mods** menu on the title screen and ensure **Advanced Smart AI NPC** is enabled (`Active: true`).
4. Enter any map:
   - Spawn **Advanced Smart AI** directly from the **Entities** tab.
   - Or right-click any vanilla human and select **"Convert to Smart AI"**.

---

## 🛠️ Ability Mod Compatibility (Developer Guide)

Other modders can easily create custom abilities or listen to AI events in their own C# mods:

### 1. Creating a Custom Ability
Inherit from `SmartAIAbility` and implement `ShouldTrigger` and `OnActivate`:

```csharp
using UnityEngine;
using AdvancedSmartAIMod;

public class LaserEyeAbility : SmartAIAbility
{
    // Name, Cooldown = 8s, Duration = 1.5s
    public LaserEyeAbility() : base("Laser Eyes", cooldown: 8.0f, duration: 1.5f) { }

    public override bool ShouldTrigger(SmartAIContext context)
    {
        // Condition: Trigger when enemy is within 10 units and AI has line of sight
        return context.TargetEnemy != null && context.TargetDistance < 10f;
    }

    public override void OnActivate(SmartAIContext context)
    {
        ModAPI.Notify($"{context.AI.name} FIRED LASER BEAMS!");
        // Custom laser projectile / damage logic here
    }
}
```

### 2. Injecting Custom Abilities Globally
In your mod's `Main()` method, register your ability factory:

```csharp
public class MyCustomMod
{
    public static void Main()
    {
        // Automatically attach LaserEyeAbility to EVERY Smart AI spawned in game!
        AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new LaserEyeAbility());
    }
}
```

### 3. Subscribing to AI Events
```csharp
AdvancedSmartAI.OnAnyAISpawned += (smartAI) =>
{
    // Hook into health changes (e.g., trigger custom effect when HP < 30%)
    smartAI.OnHealthChanged += (context, oldHp, newHp) =>
    {
        if (newHp < 0.30f)
        {
            Debug.Log("AI entered critical damage state!");
        }
    };

    // Hook into weapon throwing
    smartAI.OnWeaponThrown += (context, weapon, velocity) =>
    {
        Debug.Log($"AI threw {weapon.name} with velocity {velocity}");
    };
};
```

---

## ⚙️ Configuration Parameters Reference

| Parameter | Type | Default | Description |
|---|---|---|---|
| `HealthMultiplier` | float | `1.8f` | Scales max limb health and healing rate |
| `MovementSpeedMultiplier` | float | `1.25f` | Scales walking force, stepping frequency, and agility |
| `AccuracyMultiplier` | float | `0.85f` | Controls weapon aiming spread and angular precision |
| `ReactionTimeMultiplier` | float | `1.4f` | Modulates scan intervals, trigger actuation, and throw velocity |
| `Stance` | `AIStance` | `Aggressive` | `Aggressive`, `Defensive`, `Follower`, `Neutral` |
| `EnableActiveBalance` | bool | `true` | Enables PD torque stabilization on Torso, Pelvis, and Head |
| `EnableJumpNavigation` | bool | `true` | Enables automatic obstacle and chasm jumping |
| `EnableWeaponThrowing` | bool | `true` | Enables throwing empty guns and distant melee weapons |
| `EnableAbilities` | bool | `true` | Enables automated evaluation and activation of abilities |

---

## 📜 License & Credits
Built for the **People Playground** modding community. Free to use, modify, and extend in custom mods and scenario maps.
