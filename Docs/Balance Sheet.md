# Balance Sheet / Formulas
## Project: Vanguard Arena

> **Note**: This document defines the mathematical models used for combat and progression. In production, these formulas would be implemented in Excel/Google Sheets with variable cells for simulation.

## 1. Combat Formulas

### 1.1 Damage Calculation
The standard formula for calculating damage dealt by an attacker to a defender.

```text
Final_Damage = (Base_DMG * Crit_Multi * DMG_Bonus_Multi) * Mitigation_Multi * Element_Multi * Variance
```

*   **Base_DMG**: `(Atk_Stat * Skill_Multiplier)`
*   **Mitigation_Multi**: `1000 / (1000 + Defense_Stat)`
    *   *Note: Standard "Diminishing Returns" defense formula. 1000 Def = 50% reduction.*
*   **Crit_Multi**: If Crit: `(1 + Crit_Damage_%)`, else `1`.
*   **Element_Multi**:
    *   Advantage: `1.2`
    *   Disadvantage: `0.8`
    *   Neutral: `1.0`
*   **Variance**: Random float between `0.95` and `1.05`.

### 1.2 Effective HP (EHP)
Used to balance unit durability regardless of HP/Def split.

```text
EHP = HP_Stat * (1 + (Defense_Stat / 1000))
```

### 1.3 Speed & Turn Order
Turn-based system using an Action Value (AV) accumulator.

```text
AV_Target = 10000 / Speed_Stat
```
*   Units run down their AV. When AV reaches 0, it is their turn.
*   Faster units have lower AV targets, thus acting more frequently.

## 2. Progression Curves

### 2.1 Unit Leveling (XP Requirements)
Exponential growth curve to prevent trivial max-leveling.

*   **Formula**: `XP_Required = Base_XP * (Level ^ Power_Factor)`
*   **Constants**:
    *   `Base_XP`: 100
    *   `Power_Factor`: 2.1

| Level | XP to Next | Cumulative XP |
| :--- | :--- | :--- |
| 1 | 100 | 0 |
| 10 | 12,589 | 45,000 (Approx) |
| 20 | 54,000 | 350,000 (Approx) |
| 60 | 540,000 | 12,000,000 (Approx) |

### 2.2 Stat Growth
Stats grow linearly per level, but are heavily influenced by "Star Rating" multipliers.

```text
Stat_Val = Base_Stat + (Growth_Rate * (Level - 1))
```

### 2.3 Gold Cost for Gear Enhancement
Costs increase strictly exponentially to serve as the primary Gold Sink.

```text
Cost = Base_Cost * (Multiplier ^ Enhancement_Level)
```

## 3. Drop Rates & Weights

### 3.1 Loot Table Logic
Loot is determined by "Weight" rather than raw percentage to allow easier addition of items.

`Probability = Item_Weight / Sum(All_Weights)`

### 3.2 Gacha Weights (Example)
| Rarity | Weight | Probability |
| :--- | :--- | :--- |
| SSR | 100 | 1.0% |
| SR | 1,000 | 10.0% |
| R | 8,900 | 89.0% |
| **Total** | **10,000** | **100%** |

## 4. Action Economy (Stamina)
*   **Regen**: 1 Stamina / 5 Minutes.
*   **Daily Cap**: 288 Stamina (Natural Regen).
*   **Stage Costs**:
    *   Story Normal: 6 Stamina
    *   Story Hard: 12 Stamina
    *   Dungeon (Materials): 20 Stamina
    *   Raid Boss: 30 Stamina

*   **Value Peg**:
    *   1 Gem ~= 1.5 Stamina (based on refill rates).
    *   10,000 Gold ~= 20 Stamina (based on Gold Dungeon yields).
