# Economy Design Document
## Project: Vanguard Arena (3v3 Turn-Based Squad RPG)

## 1. Executive Summary
The economy of Vanguard Arena is designed to drive long-term player retention through a multi-layered progression system. It utilizes a dual-currency model (Soft/Hard) with energy gating to balance content consumption. The core loop revolves around acquiring units via Gacha, upgrading them through gameplay (Stamina expenditure), and overcoming increasingly difficult PvE/PvP challenges to earn prestige and more resources.

## 2. Core Loop
1.  **Prepare**: Manage squad, upgrade units, equip gear.
2.  **Battle**: Spend **Stamina** to enter stages (Campaign, Dungeons).
3.  **Reward**: Earn **Gold**, **XP Materials**, **Gear**, and **Account XP**.
4.  **Upgrade**: Sink **Gold** and **Materials** to strengthen units.
5.  **Gacha**: Spend **Gems** (from achievements/purchases) to acquire new units/duplicates.
6.  **Repeat**.

## 3. Currencies & Resources

### 3.1 Primary Currencies
| Currency | Type | Source (Faucet) | Sink (Use) | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Gold** | Soft | Campaign stages, Gold Dungeon, Daily Quests, Selling Gear | Unit Leveling, Skill Upgrades, Gear Enhancement, Shop purchases | The primary bottleneck for mid-to-late game progression. Inflation controlled by exponential upgrade costs. |
| **Gems** | Hard | IAP, First-clear rewards, PvP Weekly, Monthly Pass, Achievements | **Gacha Summons**, Stamina Refills, Shop Resets, Cosmetic Skins | Premium currency. Highly scarce for F2P players. |
| **Stamina** | Energy | Recharges over time (1 per 5m), Level Up, Gem Refills, Friend Gifts | Entering Battle Stages | Limits session time and prevents content burnout/hyper-inflation. |

### 3.2 Secondary Resources
*   **Unit XP Materials (Codex)**: Dropped from XP Dungeons. Used to level units faster than combat XP.
*   **Skill Modules**: Rare drops or purchased in Arena Shop. Used to upgrade active/passive skills.
*   **Evolution Shards**: Class-specific materials (e.g., Tank Shard, Striker Shard) required to increase Star Rating/Level Cap.
*   **Arena Points**: Earned in PvP. Spent in the Arena Shop for exclusive units or high-tier Skill Modules.

## 4. Systems Design

### 4.1 Gacha System (The Summoning)
*   **Standard Banner**: Permanent pool of all non-limited units.
*   **Featured Banner**: Rate-up for specific units. Rotates every 2 weeks.
*   **Cost**: 160 Gems = 1 Pull. 1600 Gems = 10 Pulls.
*   **Rates**:
    *   **SSR (5★)**: 1.0% (Includes pity)
    *   **SR (4★)**: 10.0%
    *   **R (3★)**: 89.0%
*   **Pity System**:
    *   **Soft Pity**: Rates increase starting at pull 75.
    *   **Hard Pity**: Guaranteed SSR at 90 pulls.
    *   **50/50 Rule**: If the first SSR pulled on a Featured Banner is not the Featured unit, the next SSR is guaranteed to be the Featured unit.

### 4.2 Unit Progression (Sinks)
*   **Leveling**: Linear stat growth. Costs **Gold** + **XP Materials**.
*   **Limit Break**: Unlocks level caps every 10 levels. Costs **Evolution Shards** + **Gold**.
*   **Skills**: Upgrading abilities increases multipliers or reduces cooldowns. Costs **Skill Modules** + **Gold**. Costs scale exponentially.
*   **Gear**: Units have 4 gear slots (Weapon, Armor, Accessory, Boots).
    *   Gear has a Main Stat (Fixed based on slot/type) and Sub-stats (Random).
    *   **Enhancement**: Sinks **Gold** and **Fodder Gear** to raise gear level (+0 to +15).

### 4.3 The Shop
*   **General Store**: Rotates daily. Sells low-tier materials for Gold, high-tier for Gems.
*   **Cash Shop**: Direct purchase of Gems, Monthly Cards (high value over time), and Battle Pass.

## 5. Economic Balance Goals
*   **Gold Scarcity**: Players should feel "Gold Poor" starting at mid-game (Level 30+), forcing prioritization of which units to upgrade.
*   **Stamina Economy**: F2P players should have enough stamina for ~1 hour of active play per day (excluding claimed bonus stamina).
*   **Gacha Acquisition**: F2P players should be able to reach Hard Pity on a banner approximately once every 1.5 - 2 months through active play.
