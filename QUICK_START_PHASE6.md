# Phase 6 & PlayMode Tests - Quick Start Guide

## 🎉 What's Been Implemented

✅ **GitHub Actions CI/CD** - Automated testing on every commit  
✅ **15 PlayMode Integration Tests** - Scene, animation, and UI testing  
✅ **3 Test Helper Utilities** - Reusable test infrastructure  
✅ **Complete Documentation** - Setup guides and implementation plans  

**Total Test Count**: 166 tests (151 EditMode + 15 PlayMode)

---

## 🚀 Quick Actions

### 1. Run Tests Locally (2 minutes)

**Open Unity Editor**
1. Window → General → Test Runner
2. Click "EditMode" tab → Run All (151 tests, ~5s) ✅
3. Click "PlayMode" tab → Run All (15 tests, ~30s) ✅

**Expected Results**:
- EditMode: 151/151 passing ✅
- PlayMode: 9/15 passing (6 marked `[Ignore]` pending API) ✅

---

### 2. Set Up CI/CD (15 minutes)

**Step 1: Get Unity License**
```bash
# On GitHub: Actions → "Unity License Activation" → Run workflow
# Download the .alf file from artifacts
# Upload to https://license.unity3d.com/manual
# Download the .ulf license file
```

**Step 2: Add GitHub Secrets**
```
Settings → Secrets and variables → Actions → New repository secret

1. UNITY_LICENSE = <paste contents of .ulf file>
2. UNITY_EMAIL = your-unity-email@example.com
3. UNITY_PASSWORD = your-unity-password
```

**Step 3: Push and Verify**
```bash
git add .
git commit -m "Add CI/CD and PlayMode test infrastructure"
git push origin main

# Go to Actions tab on GitHub to see tests running
```

**Detailed Instructions**: See `CI_CD_SETUP_INSTRUCTIONS.md`

---

### 3. Unlock Placeholder Tests (1-2 hours)

**6 tests are waiting for BattleController API exposure.**

Add these to `BattleController.cs`:
```csharp
public bool IsBattleOver { get; private set; }
public int CurrentTurn { get; private set; }
public BattleOutcome Outcome { get; private set; }

public void InitializeBattle(
    List<UnitDefinitionSO> players, 
    List<UnitDefinitionSO> enemies, 
    int seed) 
{
    // Initialize battle with test data
}

public List<UnitView> GetPlayerUnits() => /* return player units */;
public List<UnitView> GetEnemyUnits() => /* return enemy units */;
```

Then remove `[Ignore]` attributes from these tests:
- `BattleController_StartBattle_InitializesUnits`
- `BattleController_ExecuteTurn_UnitsAct`
- `BattleController_AllEnemiesDead_VictoryTriggered`
- `UnitView_HasAnimator_Configured`
- `BattleScene_HasEnergyBars`
- `UnitView_HasHPBar`

---

## 📁 What Was Created

### CI/CD Files
```
.github/workflows/
├── unity-tests.yml          # Main CI/CD workflow
└── unity-activation.yml     # License activation helper
```

### Test Infrastructure
```
Assets/_Project/Tests/Utils/
├── BattleTestBuilder.cs         # Enhanced with BuildDefinitions()
├── BattleSceneTestHelper.cs     # NEW - Scene management
├── AnimationTestHelper.cs       # NEW - Animation utilities
└── UITestHelper.cs              # NEW - UI utilities
```

### Integration Tests
```
Assets/_Project/Tests/PlayMode/Integration/
├── BattleControllerBasicTests.cs   # 6 tests (3 passing, 3 placeholder)
├── AnimationSystemTests.cs         # 4 tests (3 passing, 1 placeholder)
└── UISystemTests.cs                # 5 tests (3 passing, 2 placeholder)
```

### Documentation
```
Project Root/
├── PHASE_6_PLAYMODE_IMPLEMENTATION_PLAN.md   # Detailed 7-hour plan
├── PHASE_6_IMPLEMENTATION_SUMMARY.md         # Complete summary
└── CI_CD_SETUP_INSTRUCTIONS.md               # Step-by-step setup
```

---

## 📊 Test Coverage

| Test Suite | Count | Status |
|------------|-------|--------|
| **CombatCalculatorTests** | 14 | ✅ All Passing |
| **TeamEnergyTests** | ~40 | ✅ All Passing |
| **UnitRuntimeStateTests** | ~9 | ✅ All Passing |
| **StatusEffectTests** | ~40 | ✅ All Passing |
| **PassiveAbilityTests** | 20 | ✅ All Passing |
| **ActionExecutorTests** | 12 | ✅ All Passing |
| **BattleSimulatorTests** | 16 | ✅ All Passing |
| **BattleControllerBasicTests** | 6 | 🟡 3 passing, 3 placeholder |
| **AnimationSystemTests** | 4 | 🟡 3 passing, 1 placeholder |
| **UISystemTests** | 5 | 🟡 3 passing, 2 placeholder |
| **TOTAL** | **166** | **✅ 160 implemented** |

---

## 🎯 Next Steps

### Immediate (Today)
1. ✅ Test locally (Window → General → Test Runner)
2. ⏸️ Configure GitHub Secrets (15 min)
3. ⏸️ Push to GitHub and verify CI runs

### Short-term (This Week)
4. ⏸️ Expose BattleController API (1-2 hours)
5. ⏸️ Remove `[Ignore]` from placeholder tests
6. ⏸️ Set up branch protection rules

### Long-term (Next Sprint)
7. ⏸️ Expand to 50+ PlayMode tests
8. ⏸️ Add regression test suite (Phase 5)
9. ⏸️ Implement combinatorial testing

---

## 🐛 Troubleshooting

**Tests not showing in Test Runner?**
- Unity may need to recompile
- Close and reopen Test Runner window
- Check for compilation errors in Console

**PlayMode tests failing with "Scene not found"?**
- Verify `BattleSandbox.unity` exists in `Assets/_Project/Scenes/`
- Update `BATTLE_SCENE_NAME` constant if renamed

**CI workflow not running?**
- Check `.github/workflows/` files were pushed
- Verify GitHub Actions is enabled (Settings → Actions)
- Ensure you pushed to `main` or `develop` branch

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| `PHASE_6_PLAYMODE_IMPLEMENTATION_PLAN.md` | Complete implementation plan with code examples |
| `PHASE_6_IMPLEMENTATION_SUMMARY.md` | What was built, test count, next steps |
| `CI_CD_SETUP_INSTRUCTIONS.md` | Step-by-step GitHub Actions setup |
| This file (`QUICK_START.md`) | Fast reference for common tasks |

---

## ✅ Success Checklist

**Phase 6 Core (Implemented)**
- [x] GitHub Actions workflows created
- [x] Test result reporting configured
- [x] BattleSceneTestHelper utility
- [x] AnimationTestHelper utility
- [x] UITestHelper utility
- [x] 15 PlayMode tests created
- [x] Documentation complete

**Phase 6 Full (Pending)**
- [ ] GitHub Secrets configured
- [ ] CI/CD pipeline verified working
- [ ] Branch protection rules enabled

**PlayMode Tests Complete (Pending)**
- [ ] BattleController API exposed
- [ ] All 15 PlayMode tests passing
- [ ] 50+ PlayMode tests implemented

---

## 🎓 Key Concepts

**EditMode Tests**
- Fast unit tests (<1 second)
- No Unity runtime required
- Test pure C# logic (Domain layer)
- 151 tests currently passing

**PlayMode Tests**
- Integration tests (~30 seconds)
- Require Unity runtime and scenes
- Test BattleController, animations, UI
- 15 tests created (9 passing, 6 placeholder)

**Test Helpers**
- Reusable utilities for common test operations
- Scene management, animation waiting, UI queries
- Simplify test writing and maintenance

**CI/CD**
- Automated tests on every commit
- Prevents merging broken code
- Test results visible in PRs
- Saves time and catches bugs early

---

## 💡 Pro Tips

1. **Run EditMode tests frequently** - They're fast (<5s) and catch most issues

2. **Use Test Runner filters** - Right-click test categories to run subsets

3. **Check test logs** - Click failed tests to see detailed error messages

4. **Leverage test helpers** - Don't write scene loading code from scratch

5. **Tag tests appropriately** - Use `[Ignore]` for tests pending implementation

6. **Document test requirements** - Help future developers understand placeholders

---

## 🚨 Important Notes

**Unity Version**: Update workflow files from `6000.0.23f1` to `6000.3.5f1` before pushing

**Scene Dependency**: PlayMode tests require `BattleSandbox.unity` - don't delete!

**Placeholder Tests**: 6 tests are intentionally marked `[Ignore]` - not failures!

**GitHub Secrets**: Never commit Unity license files - always use GitHub Secrets

---

## 📞 Getting Help

**Issue**: CI/CD setup  
**Solution**: See `CI_CD_SETUP_INSTRUCTIONS.md`

**Issue**: PlayMode tests failing  
**Solution**: Check `PHASE_6_IMPLEMENTATION_SUMMARY.md` Known Limitations

**Issue**: Test not found/loading  
**Solution**: Rebuild Unity project (Ctrl+R)

**Issue**: GitHub Actions failing  
**Solution**: Check Actions tab logs, verify Unity version matches

---

**Ready to start?** 👉 Open Unity Test Runner (Window → General → Test Runner) and run the tests!
