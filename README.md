# Advanced Smart AI NPC - People Playground Mod
*(Featuring Automatic Compatibility for **Human Tiers: Reforged** & **Beyond Nowhere**)*

An advanced, highly intelligent autonomous NPC mod for **People Playground**. Designed from the ground up to replace vanilla floppy ragdoll behaviors with active balance stabilization, dynamic obstacle navigation, advanced combat tactics (firearms, melee, and tactical weapon throwing), customizable in-game Context Menu parameters, and a zero-dependency **Cross-Mod Compatibility Engine** for **Human Tiers: Reforged** and **Beyond Nowhere**.

---

## 🌟 Key Features & Improvements

### 1. Active Mass-Adaptive Balance & Locomotion
- **Mass-Adaptive PD Balance Controller**: Dynamically samples the total ragdoll mass and moment of inertia. Scales proportional-derivative torque gains automatically for vanilla humans, armored characters, giant mutants, and god-tier entities.
- **Knockdown Recovery & Kip-Up**: Detects when fallen over or pinned, applying rotational spring impulses to immediately return to a stable fighting stance.
- **Dynamic 5-Ray Environmental Scanner**:
  - **Low Ray**: Detects small steps and ground obstacles.
  - **Mid Ray**: Identifies waist-to-chest barricades and walls.
  - **High Ray & Ledge Mantle**: Detects tall walls (up to 3.2m) and executes an athletic pull-up / mantle over obstacles.
  - **Overhead Ray**: Verifies vertical clearance before vaulting.
  - **Pit / Ledge Ray**: Probes downward in front of movement to leap across chasms and hazard zones.
- **Multi-Tier Unstuck Routine**:
  - **Tier 1 (Minor)**: Jitters leg torques, reverses direction, and executes high-knees stepping.
  - **Tier 2 (Moderate)**: Pushes against contacting geometry and executes a power jump.
  - **Tier 3 (Severe)**: Unleashes an explosive untangle leap or triggers modded teleportation/kinetic shockwaves to break free.

### 2. Combat & Universal Weapon Interaction
- **Target Threat & Tier Scoring**: Scans line-of-sight, factoring in distance, weapon threat, and enemy **Human Tiers / Beyond Nowhere Tier Level** to prioritize dangerous superhumans.
- **Dynamic Ballistic Aiming**: Rotates arm with proportional torque towards target center-of-mass, incorporating spread/sway scaled by the configured accuracy multiplier.
- **Tactical Weapon Throwing**:
  - If a firearm **runs out of ammo**, the AI throws the empty gun at the enemy's head and immediately looks for a new weapon.
  - If holding a **melee weapon** and the enemy is at a distance (> 5.5m) or elevated out of reach, the AI computes a parabolic ballistic trajectory and throws the melee weapon at the enemy!
- **Universal Modded Weapon Compatibility**: Dynamically identifies and operates custom firearms, melee weapons, and **Ability Weapons / Relics** from any mod.

### 3. Automatic Human Tiers & Beyond Nowhere Compatibility
- **Zero Hard Dependencies**: Operates seamlessly in vanilla People Playground and automatically unlocks cross-mod capabilities if **Human Tiers (Reforged)** or **Beyond Nowhere** are present in your game.
- **Tier Detection & Auto-Scaling**: Automatically detects if the spawned or converted entity is a Tier 1–8 entity, Awakened, or God-level entity, dynamically scaling its health, speed, and reaction times to match its lore power.
- **Energy & Superpower Autonomous Triggering**: The AI reads the entity's `Energy` pool and autonomously casts native superpowers (elemental blasts, kinetic forces, teleportation, domain bursts, awakenings) during combat and emergencies.
- **Ability Weapon Recognition**: Prioritizes supernatural relics, anomaly data banks, and energy weapons from both mods.

### 4. In-Game Context Menu Configuration
Right-click on the Smart AI (or convert any vanilla or modded human into a Smart AI via the context menu):
- **Preset Selector**:
  - `Godlike`: 5.0x HP, 2.0x Speed, 100% Accuracy, 2.5x Reaction Time.
  - `Elite Soldier`: 2.5x HP, 1.4x Speed, 90% Accuracy, 1.8x Reaction Time.
  - `Standard AI`: 1.5x HP, 1.15x Speed, 75% Accuracy, 1.2x Reaction Time.
  - `Grunt / Weakling`: 0.8x HP, 0.85x Speed, 45% Accuracy, 0.7x Reaction Time.
  - `Civilian`: 0.5x HP, 0.75x Speed, 25% Accuracy, 0.5x Reaction Time (Neutral/Cowardly).
- **Attribute Multipliers**: Live cycling of `HealthMultiplier`, `SpeedMultiplier`, `AccuracyMultiplier`, and `ReactionTimeMultiplier`.
- **Stance Selector**: `Aggressive`, `Defensive`, `Follower`, `Neutral`.
- **Extended Diagnostics**: Displays real-time health %, target name, enemy tier, equipped weapon, Human Tiers Tier & Energy, and active abilities.

---

## 📁 Project Structure

```
running-people/
├── mod.json                     # Mod manifest metadata for People Playground
├── AdvancedSmartAI.cs           # Main mod source code (Controller, Cross-Mod Adapter, Nav, Combat, Abilities)
├── ExampleAbilityExtension.cs   # Example extension showing 3rd-party mod custom abilities & hooks
└── README.md                    # Documentation & API reference
```

---

## 🚀 Installation

1. Copy the mod folder into your People Playground `Mods` directory:
   ```
   People Playground/Mods/AdvancedSmartAI/
   ├── mod.json
   └── AdvancedSmartAI.cs
   ```
2. Launch **People Playground**.
3. Enable **Advanced Smart AI NPC** in the **Mods** menu.
4. Spawn **Advanced Smart AI** from the **Entities** tab, or right-click any vanilla human, **Human Tiers** character, or **Beyond Nowhere** entity and select **"Convert to Smart AI"**.

---

## 🛠️ Ability Mod Compatibility (Developer Guide)

```csharp
using UnityEngine;
using AdvancedSmartAIMod;

public class MyCustomMod
{
    public static void Main()
    {
        // Register custom ability to all Smart AIs
        AdvancedSmartAI.RegisterGlobalAbilityFactory(ai => new CustomVoidAbility());

        // Listen to AI events
        AdvancedSmartAI.OnAnyAISpawned += (smartAI) =>
        {
            smartAI.OnTargetAcquired += (context, target) =>
            {
                int enemyTier = CrossModAdapterEngine.DetectEntityTier(target);
                Debug.Log($"AI locked onto target {target.name} (Tier: {enemyTier})");
            };
        };
    }
}
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
| `EnableActiveBalance` | bool | `true` | Enables adaptive PD torque stabilization on Torso, Pelvis, and Head |
| `EnableJumpNavigation` | bool | `true` | Enables automatic obstacle and chasm jumping |
| `EnableLedgeMantling` | bool | `true` | Enables high wall climbing and ledge pull-ups |
| `EnableWeaponThrowing` | bool | `true` | Enables throwing empty guns and distant melee weapons |
| `EnableModdedSuperpowers`| bool | `true` | Enables autonomous superpower usage for Human Tiers & Beyond Nowhere |

---

## 📜 License & Credits
Built for the **People Playground** modding community. Fully compatible with vanilla People Playground, **Human Tiers (Reforged)** by Batrix Studios / Alibarda, and **Beyond Nowhere**.
