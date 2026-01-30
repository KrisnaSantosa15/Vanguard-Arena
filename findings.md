# Findings: Vanguard Arena

## Research Topics
1. One Punch Man: The Strongest
2. Unity 6.3 LTS 2.5D Development
3. Game Documentation Best Practices

## 1. One Punch Man: The Strongest (Mobile Game Analysis)

### Core Loop
*   **Genre**: Turn-Based Strategy RPG (Gacha).
*   **Combat**:
    *   Teams of up to 6 characters arranged in a grid (Front/Back rows).
    *   **Turn Order**: Determined by speed stats.
    *   **Action Economy**: Basic attacks generate "Energy". Ultimates consume Energy.
    *   **Auto-Battle**: Heavy focus on team composition and auto-play efficiency for farming.
*   **Progression**: Character leveling, skill upgrades, "Limit Breaking" (stars), and gear collection.
*   **Modes**: Story Campaign, Elite Dungeons, PvP Arena, Guild Bosses.

### Art Style
*   **Character Representation**:
    *   **In-Combat**: "Chibi" (Super Deformed) 2D rigged sprites (Spine/Live2D). Big heads, small bodies, expressive animations.
    *   **UI/Menus**: Full-proportion, high-fidelity anime illustrations. Often animated (Live2D) for high-rarity characters.
*   **Environment**:
    *   **2.5D/2D Parallax**: Side-scrolling city streets or static battle backdrops with depth layers.
    *   **Perspective**: Flat side view for combat, slight isometric or flat 2D for world map navigation.
*   **Aesthetic**:
    *   **Faithful Adaptation**: Stick strictly to the anime's line work and color palette.
    *   **Contrast**: High contrast, bright colors (Saitama's Yellow/Red), dark outlines.

### UI/UX
*   **Mood**: High-energy, modern, "Hero Association" tech interface.
*   **Visual Language**:
    *   **Shapes**: Slanted rectangles, dynamic angles, tech-overlays.
    *   **Typography**: Bold, sans-serif, impactful numbers.
    *   **Feedback**: "Juicy" interactions. Buttons scale/flash on press. Rewards pop out with fanfare.
*   **Navigation**: Hub-based (City map with icons) rather than complex menu diving.

### FX (Special Effects)
*   **Combat FX**:
    *   **Particles**: Heavy use of impact frames, speed lines, and glows.
    *   **Ultimates**: The signature feature. When a character uses an Ultimate, the game often cuts to a **full-screen 2D animation** (ripped from or recreating the anime), then snaps back to the Chibi view for the damage impact. This bridges the gap between the Chibi style and the "cool" factor of the IP.

## 2. Unity 6.3 LTS for 2.5D Development

### 2.5D Approaches in Unity
*   **3D World + 2D Sprites**: The most common "Paper Mario" or "Octopath" style.
    *   **Pros**: Dynamic shadows, perspective changes, easier level design with 3D primitives.
    *   **Cons**: Lighting 2D sprites requires normal maps or specific shaders.
*   **2D World + 3D Characters**: (e.g., Metroid Dread).
    *   **Pros**: Fluid animation blending, equipment changes are easier.
    *   **Cons**: Needs high-quality 3D assets.
*   **Pure 2D with Parallax**: (Likely closest to OPM mobile).
    *   **Setup**: Orthographic camera. Layers sorted by Z-depth.

### Rendering (URP)
*   **Universal Render Pipeline (URP)** is the standard.
*   **2D Renderer**:
    *   Optimized for sprites.
    *   **2D Lights**: Point, Global, Spot, Freeform. Very performant.
    *   *limitation*: 2D lights do not affect 3D objects by default.
*   **Forward+ (3D) Renderer**:
    *   Better if you have significant 3D elements (background buildings, ground mesh).
    *   **Lighting Strategy**: Use 3D lights. Give Sprites a "Lit" shader and **Normal Maps** to let them react to the 3D lights.

### Relevant Unity 6 Features
*   **Render Graph**: Allows for efficient custom rendering passes (e.g., custom outlines, stylized post-processing) without overhead.
*   **Sprite Atlas v2**: Improved workflow for packing chibi sprites and UI elements to reduce draw calls.
*   **VFX Graph for 2D**: Full support for high-end particles in 2D space (crucial for those OPM-style explosions).

## 3. Game Documentation Standards

### Art Bible Essentials
An Art Bible serves as the single source of truth for the visual direction.
1.  **Pillars**: 3-5 keywords defining the "vibe" (e.g., "Punchy," "Anime-Faithful," "Urban-Tech").
2.  **Character Guidelines**:
    *   **Proportions**: Head-to-body ratio (e.g., 2-3 heads for Chibi).
    *   **Line Art**: Thickness, color (black vs colored), tapering rules.
    *   **Color**: Palette restrictions, saturation levels.
    *   **Shading**: Cell-shading steps (1 shadow tone, 1 highlight).
3.  **Environment**:
    *   Mood boards for different biomes.
    *   Object density and composition rules.
4.  **UI/UX Guide**:
    *   Font hierarchy.
    *   Color codes (Hex/RGB) for UI states (Normal, Hover, Pressed, Disabled).
    *   Iconography style (Flat vs Skeuomorphic).
5.  **Technical Art**:
    *   Resolution standards (e.g., "Assets authored at 4k, exported at 1080p").
    *   Naming conventions (`T_Char_Saitama_D.png`).
    *   Pivot point rules.

### Documentation Folder Structure
Organize `/Docs` to keep the project sane.
*   `00_Admin/`: Contact lists, logins (keep secure), meeting notes.
*   `01_Design/`:
    *   `GDD_Master.md`: The living Game Design Document.
    *   `Systems/`: Individual specs for Combat, Inventory, Gacha.
    *   `Narrative/`: Scripts, character bios.
*   `02_Art/`:
    *   `Art_Bible.md`: The visual guide.
    *   `Style_Guides/`: Specific guides for outsourced artists.
    *   `Asset_Lists/`: Spreadsheets tracking sprite completion.
*   `03_Tech/`:
    *   `TDD.md`: Technical Design Document (Architecture).
    *   `API_Docs/`: Backend integration if needed.
    *   `Standards.md`: Coding conventions.
*   `04_Production/`:
    *   `Milestones.md`: Roadmap.
    *   `Budgets/`: Time/Money tracking.
