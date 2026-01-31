using UnityEngine;

namespace Project.Domain
{
    public sealed class TeamEnergyState
    {
        public int Energy { get; private set; }
        public int MaxEnergy { get; }

        // 0..100 (can exceed temporarily before conversion)
        public float EnergyBarPct { get; private set; }

        public TeamEnergyState(int maxEnergy, int startEnergy, float startBarPct = 0f)
        {
            MaxEnergy = Mathf.Max(1, maxEnergy);
            Energy = Mathf.Clamp(startEnergy, 0, MaxEnergy);
            EnergyBarPct = Mathf.Clamp(startBarPct, 0f, 100f);
        }

        public void GainEnergy(int amount)
        {
            if (amount <= 0) return;
            int oldEnergy = Energy;
            Energy = Mathf.Min(MaxEnergy, Energy + amount);
            int actualGain = Energy - oldEnergy;
            
            if (actualGain > 0)
            {
                FileLogger.Log($"⚡ Energy gained: {oldEnergy} -> {Energy} (+{actualGain})", "ENERGY");
            }
            else
            {
                FileLogger.Log($"⚡ Energy gain blocked: {oldEnergy}/{MaxEnergy} (already at max)", "ENERGY");
            }
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount <= 0) return true;
            if (Energy < amount)
            {
                FileLogger.Log($"❌ Not enough energy: have {Energy}, need {amount}", "ENERGY");
                return false;
            }
            
            int oldEnergy = Energy;
            Energy -= amount;
            FileLogger.Log($"⚡ Energy spent: {oldEnergy} -> {Energy} (-{amount})", "ENERGY");
            return true;
        }

        /// <summary>
        /// Adds % to the energy bar; converts each full 100% into +1 Energy.
        /// Remainder is carried. Energy clamps at MaxEnergy (Option A).
        /// </summary>
        public int AddToBarAndConvert(float pctAdd)
        {
            if (pctAdd <= 0f) return 0;

            float oldBar = EnergyBarPct;
            EnergyBarPct += pctAdd;

            int converted = 0;
            if (EnergyBarPct >= 100f)
            {
                converted = Mathf.FloorToInt(EnergyBarPct / 100f);
                EnergyBarPct = EnergyBarPct % 100f;

                int before = Energy;
                GainEnergy(converted);
                converted = Energy - before; // respects clamp
                
                if (converted > 0)
                {
                    FileLogger.Log($"⚡ Energy bar filled: {oldBar:F1}% -> {EnergyBarPct:F1}% (converted {converted} energy)", "ENERGY-BAR");
                }
            }

            // Keep bar in [0, 100)
            if (EnergyBarPct >= 100f) EnergyBarPct = EnergyBarPct % 100f;
            if (EnergyBarPct < 0f) EnergyBarPct = 0f;

            return converted;
        }
        
        /// <summary>
        /// Reset energy to a specific value (for testing).
        /// </summary>
        public void Reset(int newEnergy)
        {
            Energy = Mathf.Clamp(newEnergy, 0, MaxEnergy);
            EnergyBarPct = 0f;
            FileLogger.Log($"⚡ Energy reset to {Energy}", "ENERGY");
        }
    }
}
