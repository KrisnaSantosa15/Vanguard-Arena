# Art Bible — Project: Vanguard Arena

> **Version:** 1.0
> **Date:** January 27, 2026
> **Engine:** Unity 6.3 LTS (URP)

---

## 1. Vision Statement
**"3D tactical depth with 2D anime punch."**

Vanguard Arena bridges the gap between tactical clarity and high-octane anime action. We use 3D assets to create a "living illustration" where every frame feels composed, readable, and energetic. The player should feel like the director of a high-budget anime battle sequence, with a modern, tech-forward interface wrapper.

---

## 2. Art Pillars

### **Readable**
*   **Instant Recognition:** A player must identify a unit's role, health status, and active effects within 0.5 seconds.
*   **Clarity Over Noise:** VFX and environment details never compete with gameplay information. If it doesn't serve the tactic, it doesn't stay in the frame.
*   **Fixed Framing:** The orthographic camera is our canvas. Composition is locked and curated.

### **Energetic**
*   **Juiciness:** Every interaction has weight. Buttons click with a punch; hits pause the frame (hitstop); crits shake the screen.
*   **Anime Timing:** Motion is snappy, not floaty. Use "smears" and "impact frames" to convey power rather than realistic physics.
*   **Modern Tech:** The UI is diegetic to a futuristic command center—slick, holographic, and responsive.

### **Modular**
*   **Efficient Pipeline:** Assets are built to be reused. Environment pieces snap to a grid.
*   **Scalable:** Characters share rigs where possible. VFX share master shaders.
*   **Clean Geometry:** Mid-to-low poly counts with high-quality shading, optimized for PC but ready for mobile.

---

## 3. Style Guide

### 3.1 Color Palette
Our world is vibrant and high-contrast, avoiding muddy textures or realistic grit.

*   **Primary (UI & Gameplay)**:
    *   **Action Blue:** `#00AEEF` (Player interaction, friendly units, mana/energy)
    *   **Hostile Red:** `#FF4444` (Enemy health, danger zones, attack indicators)
    *   **Neutral Dark:** `#1A1A24` (Backgrounds, UI panels to make colors pop)
    *   **Highlight White:** `#FFFFFF` (Text, borders, max contrast elements)

*   **Secondary (Accents)**:
    *   **Victory Gold:** `#FFD700` (Rare items, critical hits, level up)
    *   **Tech Cyan:** `#00FFCC` (Holographic elements, speed lines)
    *   **Toxic Green / Magic Purple:** Specific status effects or biome accents.

*   **Readability Rules**:
    *   Backgrounds uses lower saturation (20-40%) and value.
    *   Characters use mid-to-high saturation (60-80%).
    *   VFX/UI use max saturation (90-100%) and emission.

### 3.2 Lighting & Shading
*   **Pipeline:** URP Lit.
*   **Toon Shading:**
    *   **Banding:** 1 distinct shadow band (2-tone shading). Hard edge between light and dark.
    *   **Ramp:** Use a global light ramp texture to control the "warmth" of shadows.
    *   **Specular:** Sharp, stylized specular highlights on metallic surfaces; matte on cloth/skin.
*   **Environment Lighting:** Baked lighting for base ambience + 1 Real-time Directional Light for casting dynamic character shadows.

### 3.3 Outlines
*   **Technique:** Inverted Hull method or high-quality Post-Process edge detection.
*   **Color:** Dark Blue/Purple (almost black), never pure black.
*   **Thickness:**
    *   **Characters:** Constant screen-space thickness (bold).
    *   **Environment:** Thinner or no outlines to push them back in depth.

---

## 4. Character Anatomy
Our style is **Heroic Stylized**. We reject realistic proportions in favor of readability and personality.

*   **Proportions**: 6.5 - 7 heads tall.
    *   **NOT Chibi**: Characters must look cool and dangerous, not cute/infantilized.
    *   **Readability Tweaks**: Hands and Feet are ~1.2x larger than realistic to clarify punches, kicks, and weapon grips. Heads are slightly larger to show facial expressions.
*   **Silhouettes**:
    *   Push distinct shapes for roles (e.g., Tank = Square/Bulky, Rogue = Triangle/Sharp).
    *   Remove small noise (pouches, straps) that breaks the silhouette at a distance.
*   **Texturing**:
    *   Flat color diffuse maps.
    *   **Baked AO**: Bake ambient occlusion directly into the texture to ground the geometry, as we don't rely on real-time AO.
    *   Internal detail lines painted into texture.

---

## 5. Environment
The stage is a theatrical set, not a simulation of a real world.

### 5.1 Stage Construction
*   **Orthographic Camera**: Fixed angle (approx 30-45 deg), fixed distance.
*   **Lanes**: Clear visual delineation between the "Player Side" and "Enemy Side".
*   **Density Rule**:
    *   **Playable Area**: 0% clutter. Flat ground with subtle texture variation.
    *   **Foreground**: Minimal framing elements (blurred, pushed aside).
    *   **Background**: 70% detail density. Set the scene but don't distract.

### 5.2 Biomes (MVP)
1.  **Neo-City**: Cyberpunk aesthetic. Neon signs, asphalt, rain puddles (reflections), concrete barriers.
2.  **Traditional Dojo**: Tatami mats, wooden beams, paper lanterns, cherry blossom petals (particle).
3.  **Ancient Ruin**: Overgrown stone, glowing runes, moss, floating geometric artifacts.

---

## 6. UI / UX
Referencing "OPM The Strongest" energy — slick, fast, snappy.

*   **Framework**: Unity UI Toolkit.
*   **Typography**:
    *   **Headlines**: Bold, condensed Sans-Serif (e.g., *Rajdhani*, *Teko*). Italicized for movement.
    *   **Body**: Clean, legible Sans-Serif (e.g., *Roboto*, *Inter*). High x-height.
*   **Iconography**:
    *   Flat, 2-tone vector style.
    *   Thick outlines to match character art.
    *   Instant readability (Heart = HP, Sword = ATK).
*   **Motion**:
    *   UI panels slide in with "speed lines".
    *   Buttons have a press-down animation state.
    *   Numbers "pop" and scale up on critical hits.

---

## 7. VFX Guidelines
VFX bridge the gap between turn-based inputs and visceral impact.

### 7.1 Timing & Shape
*   **The Arc**: Anticipation (gather energy) -> **Impact** (Flash frame + Burst) -> Dissolve (Quick fade).
*   **Shape Language**: Sharp spikes for damage, circles/bubbles for defense, flowing lines for healing.
*   **Anime FX**:
    *   **Impact Frames**: For heavy hits, invert screen colors or flash white for 1 frame.
    *   **Speed Lines**: Directional blur meshes during dashes or projectiles.
    *   **2D Elements**: Hand-drawn flipbooks (sprites) mixed with 3D particles.

### 7.2 Priority System (Visual Hierarchy)
To prevent "particle soup," VFX strictly adhere to priority layers:

1.  **Ultimate Abilities**: (Highest) Dims background, pauses time, creates a full-screen "Cut-in" of the character's face.
2.  **Critical Hits / Finishers**: Large scale, screen shake.
3.  **Standard Hits / Projectiles**: Readable path and contact point.
4.  **Buffs / Status Effects**: (Lowest) Subtle floating particles or ground decals. *Must not obscure character face.*
