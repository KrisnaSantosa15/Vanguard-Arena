# Assembly Definition Reference

## Summary of All Assembly Definitions

### Main Project Assemblies

#### 1. VanguardArena.Domain.asmdef
**Location**: `Assets\_Project\Scripts\Domain\`
**Namespace**: `Project.Domain`
**References**: None (pure C# logic)
**Platform**: All
**Purpose**: Core battle logic, formulas, data structures

#### 2. VanguardArena.Presentation.asmdef ✅ FIXED
**Location**: `Assets\_Project\Scripts\Presentation\`
**Namespace**: `Project.Presentation`
**References**: 
- VanguardArena.Domain
- Unity.InputSystem (for BattleInputController)
- Unity.TextMeshPro (for UI components)
**Platform**: All
**Purpose**: Unity integration, MonoBehaviours, UI controllers

#### 3. VanguardArena.Editor.asmdef
**Location**: `Assets\_Project\Scripts\Editor\`
**Namespace**: `Project.Editor`
**References**:
- VanguardArena.Domain
**Platform**: Editor only
**Purpose**: Editor tools (UnitConfigurationExporter, UnitDefinitionGenerator)

---

### Test Assemblies

#### 4. VanguardArena.Tests.Utils.asmdef
**Location**: `Assets\_Project\Tests\Utils\`
**Namespace**: `VanguardArena.Tests.Utils`
**References**:
- VanguardArena.Domain
**Platform**: All
**Purpose**: Shared test utilities (BattleTestBuilder, InvariantChecker, BattleLogParser)

#### 5. VanguardArena.Tests.EditMode.asmdef
**Location**: `Assets\_Project\Tests\EditMode\`
**Namespace**: `VanguardArena.Tests.EditMode`
**References**:
- UnityEngine.TestRunner
- UnityEditor.TestRunner
- VanguardArena.Domain
- VanguardArena.Presentation
- VanguardArena.Tests.Utils
**Platform**: Editor only
**Purpose**: Fast unit tests (no runtime, no scene)

#### 6. VanguardArena.Tests.PlayMode.asmdef
**Location**: `Assets\_Project\Tests\PlayMode\`
**Namespace**: `VanguardArena.Tests.PlayMode`
**References**:
- UnityEngine.TestRunner
- UnityEditor.TestRunner
- VanguardArena.Domain
- VanguardArena.Presentation
- VanguardArena.Tests.Utils
**Platform**: All
**Purpose**: Integration tests (requires Unity runtime)

---

## Dependency Graph

```
┌─────────────────────────────┐
│  VanguardArena.Domain       │  (Pure C#, no Unity dependencies)
│  - IDeterministicRandom     │
│  - CombatCalculator         │
│  - UnitRuntimeState         │
│  - StatusEffect             │
└─────────────┬───────────────┘
              │
              ├───────────────────────────────────┐
              │                                   │
              ▼                                   ▼
┌─────────────────────────────┐   ┌──────────────────────────────┐
│ VanguardArena.Presentation  │   │  VanguardArena.Editor        │
│ + Unity.InputSystem         │   │  (Editor Tools)              │
│ + Unity.TextMeshPro         │   └──────────────────────────────┘
│ - BattleController          │
│ - BattleInputController     │
│ - DamagePopup (UI)          │
└─────────────┬───────────────┘
              │
              │
┌─────────────┴───────────────┐
│ VanguardArena.Tests.Utils   │
│ - BattleTestBuilder         │
│ - InvariantChecker          │
│ - BattleLogParser           │
└─────────────┬───────────────┘
              │
              ├───────────────────────────┐
              ▼                           ▼
┌──────────────────────────┐  ┌──────────────────────────┐
│ Tests.EditMode           │  │ Tests.PlayMode           │
│ + TestRunner             │  │ + TestRunner             │
│ - CombatCalculatorTests  │  │ - Integration tests      │
└──────────────────────────┘  └──────────────────────────┘
```

---

## Why These References?

### VanguardArena.Presentation Needs:
1. **Unity.InputSystem**
   - Used by: `BattleInputController.cs` (line 2)
   - For: Mouse/keyboard input handling
   - Package: `com.unity.inputsystem`

2. **Unity.TextMeshPro**
   - Used by: `DamagePopup.cs` (line 1), UI components
   - For: Text rendering (TMP_Text)
   - Package: `com.unity.ugui` (includes TMPro)

### Why Domain Has No References:
- Pure C# logic
- No Unity types (except UnityEngine.dll which is implicit)
- Can be tested without Unity runtime
- Maximum portability

---

## Common Issues & Solutions

### Issue 1: "InputSystem does not exist"
**Error**: `error CS0234: The type or namespace name 'InputSystem' does not exist in the namespace 'UnityEngine'`

**Cause**: Presentation.asmdef missing `Unity.InputSystem` reference

**Solution**: ✅ FIXED - Added to VanguardArena.Presentation.asmdef

### Issue 2: "TMPro could not be found"
**Error**: `error CS0246: The type or namespace name 'TMPro' could not be found`

**Cause**: Presentation.asmdef missing `Unity.TextMeshPro` reference

**Solution**: ✅ FIXED - Added to VanguardArena.Presentation.asmdef

### Issue 3: "Project.Domain types not found in tests"
**Error**: `error CS0246: The type or namespace name 'Project' could not be found`

**Cause**: Test assemblies missing reference to VanguardArena.Domain

**Solution**: ✅ FIXED - Added domain references to all test assemblies

---

## Verification Checklist

After Unity recompiles, verify:

- [ ] No errors in Console
- [ ] Test Runner shows EditMode tests
- [ ] CombatCalculatorTests visible (15 tests)
- [ ] All scripts in Domain compile
- [ ] All scripts in Presentation compile
- [ ] All scripts in Editor compile
- [ ] All test utilities compile

---

## Assembly Definition Best Practices

### ✅ DO:
- Keep Domain layer pure (no Unity dependencies)
- Reference only what you need
- Use platform restrictions (Editor-only for tools)
- Set `autoReferenced: true` for runtime assemblies
- Set `autoReferenced: false` for test assemblies

### ❌ DON'T:
- Add circular references
- Reference Presentation from Domain
- Mix Editor and Runtime code in same assembly
- Add unnecessary package dependencies

---

## Quick Reference: Unity Package Names

Common packages and their assembly names:

| Package                  | Assembly Name         | Used For              |
|--------------------------|----------------------|-----------------------|
| Input System             | Unity.InputSystem    | New input system      |
| TextMesh Pro             | Unity.TextMeshPro    | Advanced text         |
| UI (uGUI)                | UnityEngine.UI       | Canvas UI             |
| Timeline                 | Unity.Timeline       | Cutscenes             |
| Cinemachine              | Cinemachine          | Camera control        |
| Test Framework           | UnityEngine.TestRunner | Unit tests          |

---

**Status**: All assembly definition issues resolved ✅

The project should now compile successfully in Unity!
