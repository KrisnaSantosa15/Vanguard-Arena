using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Domain
{
    [Serializable]
    public sealed class UnitRuntimeState
    {
        public string UnitId;
        public string DisplayName;

        public bool IsEnemy;
        public UnitType Type; // Melee or Ranged

        // Positional data for formation targeting
        public int SlotIndex;     // 0-5 for players, 0-2 for enemies
        public int Column;        // 0, 1, or 2 (left, center, right)
        public int Row;           // 0 = front, 1 = back (players only)

        public int MaxHP;
        public int CurrentHP;

        public int ATK;
        public int DEF;
        public int SPD;

        public float CritRate;      // 0-100%
        public float CritDamage;    // 100-300% (e.g., 150 = 1.5x damage)

        public int BasicHitsMin;
        public int BasicHitsMax;
        public TargetPattern BasicTargetPattern;
        public string BasicSkillDescription { get; }

        public bool IsBoss { get; }
        public Sprite Portrait { get; }

        public Sprite BossTypeIcon { get; }

        public int UltimateEnergyCost;
        public int UltimateCooldownTurns;
        public TargetPattern UltimateTargetPattern;
        public string UltimateSkillDescription { get; }

        public string PassiveName { get; }
        public string PassiveDescription { get; }
        public PassiveAbility Passive { get; }

        // Runtime cooldown tracker: 0 means ready
        public int UltimateCooldownRemaining;

        // Per-unit energy for enemies (simplified system)
        public int CurrentEnergy;

        // For enemies: check per-unit energy. For players: only check alive, cooldown, and stun status (team energy checked separately)
        public bool CanUseUltimate => IsEnemy
            ? (IsAlive && UltimateCooldownRemaining <= 0 && !IsStunned && CurrentEnergy >= UltimateEnergyCost)
            : (IsAlive && UltimateCooldownRemaining <= 0 && !IsStunned);

        // Status effects tracking
        public List<StatusEffect> ActiveStatusEffects = new();

        // Status queries
        public bool IsStunned => ActiveStatusEffects.Any(s => s.Type == StatusEffectType.Stun && !s.IsExpired);
        public int ShieldAmount => ActiveStatusEffects.Where(s => s.Type == StatusEffectType.Shield && !s.IsExpired).Sum(s => s.Value);

        // Computed stats with status modifiers
        public int CurrentATK
        {
            get
            {
                float modifier = 1f;
                modifier += ActiveStatusEffects.Where(s => s.Type == StatusEffectType.ATKUp && !s.IsExpired).Sum(s => s.Modifier);
                modifier -= ActiveStatusEffects.Where(s => s.Type == StatusEffectType.ATKDown && !s.IsExpired).Sum(s => s.Modifier);
                return Mathf.Max(0, Mathf.RoundToInt(ATK * modifier));
            }
        }

        public int CurrentDEF
        {
            get
            {
                float modifier = 1f;
                modifier += ActiveStatusEffects.Where(s => s.Type == StatusEffectType.DEFUp && !s.IsExpired).Sum(s => s.Modifier);
                modifier -= ActiveStatusEffects.Where(s => s.Type == StatusEffectType.DEFDown && !s.IsExpired).Sum(s => s.Modifier);
                return Mathf.Max(0, Mathf.RoundToInt(DEF * modifier));
            }
        }

        public bool IsAlive => CurrentHP > 0;

        public UnitRuntimeState(UnitDefinitionSO def, bool isEnemy, int slotIndex = 0)
        {
            IsBoss = def.IsBoss;
            Portrait = def.Portrait;
            BossTypeIcon = def.BossTypeIcon;

            UnitId = def.Id;
            DisplayName = def.DisplayName;
            IsEnemy = isEnemy;
            Type = def.Type;

            // Assign positional data based on slot (SAME for both teams)
            // Slots 0-2 = Front Row (Row 0), Slots 3-5 = Back Row (Row 1)
            SlotIndex = slotIndex;
            Row = slotIndex < 3 ? 0 : 1;
            Column = slotIndex % 3; // Maps: 0→Col0, 1→Col1, 2→Col2, 3→Col0, 4→Col1, 5→Col2

            MaxHP = Mathf.Max(1, def.HP);
            CurrentHP = MaxHP;

            ATK = Mathf.Max(0, def.ATK);
            DEF = Mathf.Max(0, def.DEF);
            SPD = Mathf.Max(0, def.SPD);

            CritRate = Mathf.Clamp(def.CritRate, 0f, 100f);
            CritDamage = Mathf.Clamp(def.CritDamage, 100f, 300f);

            BasicHitsMin = Mathf.Clamp(def.BasicHitsMin, 1, 10);
            BasicHitsMax = Mathf.Clamp(def.BasicHitsMax, BasicHitsMin, 10);
            BasicTargetPattern = def.BasicTargetPattern;
            BasicSkillDescription = def.BasicSkillDescription;

            UltimateEnergyCost = Mathf.Clamp(def.UltimateEnergyCost, 0, 10);
            UltimateCooldownTurns = Mathf.Clamp(def.UltimateCooldownTurns, 0, 10);
            UltimateCooldownRemaining = 0;
            UltimateTargetPattern = def.UltimateTargetPattern;
            UltimateSkillDescription = def.UltimateSkillDescription;

            PassiveName = def.PassiveName;
            PassiveDescription = def.PassiveDescription;
            Passive = def.Passive;

            // Initialize enemy energy (simplified system: each enemy starts with 0, gains +1/turn)
            CurrentEnergy = isEnemy ? 0 : 0;
        }

        public void OnNewTurnTickCooldown()
        {
            if (UltimateCooldownRemaining > 0)
            {
                int oldCd = UltimateCooldownRemaining;
                UltimateCooldownRemaining--;
                FileLogger.Log($"⏱️ {DisplayName} ultimate cooldown: {oldCd} -> {UltimateCooldownRemaining}", "COOLDOWN");
            }

            // Tick status effect durations and track expiring effects
            var expiringEffects = new List<string>();
            
            foreach (var effect in ActiveStatusEffects)
            {
                int oldDuration = effect.DurationTurns;
                effect.TickDuration();
                
                if (effect.DurationTurns > 0 && oldDuration > 0)
                {
                    FileLogger.Log($"   {effect.Type} duration: {oldDuration} -> {effect.DurationTurns}", "EFFECT-TICK");
                }
                
                if (effect.IsExpired && oldDuration > 0)
                {
                    expiringEffects.Add($"{effect.Type}");
                }
            }

            // Remove expired effects
            int removed = ActiveStatusEffects.RemoveAll(s => s.IsExpired);
            
            if (removed > 0)
            {
                FileLogger.Log($"❌ {DisplayName} lost {removed} effect(s): {string.Join(", ", expiringEffects)}", "EFFECT-EXPIRE");
            }
        }

        public void StartUltimateCooldown()
        {
            UltimateCooldownRemaining = UltimateCooldownTurns;
        }

        /// <summary>
        /// Apply a status effect to this unit
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType type, int value, float modifier, int duration)
        {
            // Shields don't use modifiers - force to 0 for consistency
            if (type == StatusEffectType.Shield)
            {
                modifier = 0f;
            }
            
            FileLogger.Log($"✨ Applying {type} to {DisplayName}: value={value}, modifier={modifier}, duration={duration}", "EFFECT");
            
            var effect = new StatusEffect(type, value, modifier, duration);

            // Handle stacking rules (per design doc)
            if (type == StatusEffectType.Shield)
            {
                // Shields stack by value
                int currentShield = ActiveStatusEffects.Where(s => s.Type == StatusEffectType.Shield).Sum(s => s.Value);
                ActiveStatusEffects.Add(effect);
                FileLogger.Log($"   🛡️ Shield added: {currentShield} -> {currentShield + value} (stacked)", "EFFECT");
            }
            else if (type == StatusEffectType.Burn)
            {
                // Burn refreshes duration
                var existing = ActiveStatusEffects.FirstOrDefault(s => s.Type == StatusEffectType.Burn);
                if (existing != null)
                {
                    FileLogger.Log($"   🔥 Burn refreshed: duration={existing.DurationTurns} -> {duration}, damage={existing.Value} -> {value}", "EFFECT");
                    existing.DurationTurns = duration;
                    existing.Value = value; // Update damage if changed
                }
                else
                {
                    FileLogger.Log($"   🔥 Burn applied: {value} dmg/turn for {duration} turns", "EFFECT");
                    ActiveStatusEffects.Add(effect);
                }
            }
            else if (type == StatusEffectType.ATKUp || type == StatusEffectType.ATKDown ||
                     type == StatusEffectType.DEFUp || type == StatusEffectType.DEFDown)
            {
                // Stat modifiers: Same magnitude stacks, different magnitude uses "higher wins" rule
                var existingSameMagnitude = ActiveStatusEffects.FirstOrDefault(s => 
                    s.Type == type && 
                    !s.IsExpired && 
                    Mathf.Approximately(s.Modifier, modifier));
                
                if (existingSameMagnitude != null)
                {
                    // Same magnitude: Stack (enables ramping/snowball mechanics)
                    ActiveStatusEffects.Add(effect);
                    int stackCount = ActiveStatusEffects.Count(s => s.Type == type && !s.IsExpired);
                    FileLogger.Log($"   📊 {type} stacked: {modifier:P0} for {duration} turns (total: {stackCount} stacks)", "EFFECT");
                }
                else
                {
                    // Different magnitude: Check "higher wins" rule
                    var existingDifferent = ActiveStatusEffects.FirstOrDefault(s => s.Type == type && !s.IsExpired);
                    
                    if (existingDifferent != null)
                    {
                        if (Mathf.Abs(modifier) > Mathf.Abs(existingDifferent.Modifier))
                        {
                            // Replace with higher magnitude
                            FileLogger.Log($"   📊 {type} replaced: {existingDifferent.Modifier:P0} -> {modifier:P0}, duration={duration}", "EFFECT");
                            existingDifferent.Modifier = modifier;
                            existingDifferent.DurationTurns = duration;
                        }
                        else
                        {
                            // Ignore weaker buff
                            FileLogger.Log($"   📊 {type} ignored (existing {existingDifferent.Modifier:P0} is stronger)", "EFFECT");
                        }
                    }
                    else
                    {
                        // No existing buff: Add new
                        ActiveStatusEffects.Add(effect);
                        FileLogger.Log($"   📊 {type} applied: {modifier:P0} for {duration} turns", "EFFECT");
                    }
                }
            }
            else
            {
                // Default: add the effect
                FileLogger.Log($"   ✨ {type} applied (default)", "EFFECT");
                ActiveStatusEffects.Add(effect);
            }
        }

        /// <summary>
        /// Apply burn damage at turn start
        /// </summary>
        public int ProcessBurnDamage()
        {
            var burnEffects = ActiveStatusEffects.Where(s => s.Type == StatusEffectType.Burn && !s.IsExpired).ToList();
            
            if (burnEffects.Count == 0) return 0;
            
            int totalDamage = burnEffects.Sum(s => s.Value);
            
            FileLogger.Log($"🔥 {DisplayName} taking burn damage: {totalDamage} (from {burnEffects.Count} burn effect(s))", "BURN");
            
            int hpBefore = CurrentHP;
            CurrentHP = Mathf.Max(0, CurrentHP - totalDamage);
            
            FileLogger.Log($"   HP change: {hpBefore} -> {CurrentHP}", "BURN");

            return totalDamage;
        }

        /// <summary>
        /// Absorb damage with shield before applying to HP
        /// </summary>
        public int AbsorbDamageWithShield(int incomingDamage)
        {
            if (incomingDamage <= 0) return 0;

            int originalDamage = incomingDamage;
            int remainingDamage = incomingDamage;
            int shieldCount = ActiveStatusEffects.Count(s => s.Type == StatusEffectType.Shield && !s.IsExpired);
            
            if (shieldCount > 0)
            {
                FileLogger.Log($"🛡️ {DisplayName} has {shieldCount} active shield(s). Incoming damage: {incomingDamage}", "SHIELD");
            }

            // Process shields in order (FIFO)
            for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = ActiveStatusEffects[i];
                if (effect.Type != StatusEffectType.Shield || effect.IsExpired)
                    continue;

                int shieldBefore = effect.Value;
                
                if (effect.Value >= remainingDamage)
                {
                    // Shield absorbs all damage
                    effect.Value -= remainingDamage;
                    FileLogger.Log($"   Shield absorbed ALL damage: {shieldBefore} -> {effect.Value} (blocked {remainingDamage} dmg)", "SHIELD");
                    remainingDamage = 0;
                    break;
                }
                else
                {
                    // Shield breaks
                    remainingDamage -= effect.Value;
                    FileLogger.Log($"   Shield BROKEN: {shieldBefore} -> 0 (blocked {effect.Value} dmg, {remainingDamage} penetrates)", "SHIELD");
                    effect.Value = 0;
                    effect.DurationTurns = 0; // Mark for removal
                }
            }

            // Remove broken shields
            int brokenCount = ActiveStatusEffects.RemoveAll(s => s.Type == StatusEffectType.Shield && s.Value <= 0);
            if (brokenCount > 0)
            {
                FileLogger.Log($"   Removed {brokenCount} broken shield(s) from {DisplayName}", "SHIELD");
            }
            
            if (originalDamage != remainingDamage)
            {
                FileLogger.Log($"🛡️ Shield result: {originalDamage} dmg -> {remainingDamage} dmg (absorbed {originalDamage - remainingDamage})", "SHIELD");
            }

            return remainingDamage;
        }

        /// <summary>
        /// Take damage (apply mitigation/shield then reduce HP)
        /// </summary>
        public void TakeDamage(int rawDamage)
        {
            if (rawDamage <= 0) return;
            
            int damageAfterShields = AbsorbDamageWithShield(rawDamage);
            int oldHP = CurrentHP;
            
            // Apply damage to HP
            CurrentHP = Mathf.Max(0, CurrentHP - damageAfterShields);
            
            if (damageAfterShields > 0)
            {
                FileLogger.Log($"⚔️ {DisplayName} took damage: {rawDamage} (raw) -> {damageAfterShields} (unshielded). HP: {oldHP} -> {CurrentHP}", "DAMAGE");
            }
        }

        /// <summary>
        /// Check if passive should trigger based on condition
        /// </summary>
        public bool ShouldTriggerPassive(PassiveType triggerType, float hpPercent = 0f)
        {
            if (Passive == null || Passive.Type != triggerType)
                return false;

            if (triggerType == PassiveType.OnHPThreshold || triggerType == PassiveType.OnAllyLowHP)
            {
                return hpPercent <= Passive.TriggerThreshold;
            }

            return true;
        }

    }
}
