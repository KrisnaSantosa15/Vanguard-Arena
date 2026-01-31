# GitHub Actions CI/CD Setup Instructions

## Overview
This document explains how to set up automated Unity testing with GitHub Actions for Vanguard Arena.

---

## Prerequisites
- GitHub repository: https://github.com/KrisnaSantosa15/Vanguard-Arena.git
- Unity 6000.0.23f1 (or update workflows to match your version)
- Unity account with valid license

---

## Step 1: Get Unity License for CI

### Option A: Personal License (Free)

1. **Generate Activation File**
   - Go to your GitHub repository
   - Click `Actions` tab
   - Select `Unity License Activation` workflow
   - Click `Run workflow` button
   - Wait for workflow to complete
   - Download the `.alf` file from artifacts

2. **Get License File**
   - Go to https://license.unity3d.com/manual
   - Upload the `.alf` file
   - Follow the manual activation steps
   - Download the `.ulf` license file

3. **Add License to GitHub Secrets**
   - Go to `Settings` → `Secrets and variables` → `Actions`
   - Click `New repository secret`
   - Name: `UNITY_LICENSE`
   - Value: Paste the **entire contents** of the `.ulf` file
   - Click `Add secret`

### Option B: Unity Plus/Pro License

If you have a Unity Plus or Pro license:

1. Find your Unity license file:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

2. Add to GitHub Secrets (same as Option A step 3)

---

## Step 2: Configure GitHub Secrets

Add the following secrets to your repository:

### Required Secrets

1. **UNITY_LICENSE**
   - Content of your `.ulf` license file
   - See Step 1 above

2. **UNITY_EMAIL**
   - Your Unity account email
   - Go to `Settings` → `Secrets and variables` → `Actions`
   - Click `New repository secret`
   - Name: `UNITY_EMAIL`
   - Value: `your-unity-email@example.com`

3. **UNITY_PASSWORD**
   - Your Unity account password
   - Go to `Settings` → `Secrets and variables` → `Actions`
   - Click `New repository secret`
   - Name: `UNITY_PASSWORD`
   - Value: `your-unity-password`

⚠️ **Security Note**: These secrets are encrypted and only accessible during workflow runs.

---

## Step 3: Enable GitHub Actions

1. Go to your repository on GitHub
2. Click `Settings` tab
3. Click `Actions` → `General` in left sidebar
4. Under "Actions permissions":
   - Select ✅ "Allow all actions and reusable workflows"
5. Click `Save`

---

## Step 4: Set Up Branch Protection (Optional but Recommended)

Prevent merging code that breaks tests:

1. Go to `Settings` → `Branches`
2. Click `Add branch protection rule`
3. Branch name pattern: `main` (or `develop`)
4. Enable the following:
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - Under "Status checks that are required":
     - Search and add: `EditMode Tests`
     - Search and add: `PlayMode Tests`
   - ✅ (Optional) Require approvals: `1` approval
5. Click `Create` or `Save changes`

---

## Step 5: Verify Setup

### Test the Workflow

1. **Commit and Push Changes**
   ```bash
   git add .github/workflows/
   git commit -m "Add GitHub Actions CI/CD workflow"
   git push origin main
   ```

2. **Monitor Workflow Execution**
   - Go to `Actions` tab on GitHub
   - You should see "Unity Tests" workflow running
   - Click on the workflow run to see details

3. **Check Test Results**
   - Wait for workflow to complete (10-15 minutes first time)
   - ✅ Green checkmark = All tests passed
   - ❌ Red X = Tests failed (click to see details)

### Expected First Run
- **EditMode Tests**: Should pass (151 tests)
- **PlayMode Tests**: Will fail until PlayMode tests are implemented
- **Coverage Report**: Will generate after both test suites complete

---

## Workflow Triggers

The Unity Tests workflow automatically runs on:

1. **Push to main branch**
   ```bash
   git push origin main
   ```

2. **Push to develop branch**
   ```bash
   git push origin develop
   ```

3. **Pull Requests to main or develop**
   - Tests run automatically on every PR commit
   - Results appear as checks in the PR

---

## Understanding the Workflow

### Jobs Breakdown

**1. EditMode Tests** (~5 minutes)
- Runs fast unit tests without Unity runtime
- Tests Domain layer logic
- 151 tests currently

**2. PlayMode Tests** (~10-20 minutes)
- Runs integration tests with Unity scene
- Tests BattleController, animations, UI
- Depends on EditMode tests passing

**3. Coverage Report** (~1 minute)
- Generates code coverage metrics
- Uploads to Codecov (optional)
- Depends on both test suites completing

### Workflow Optimizations

**Caching**
- Unity Library folder is cached to speed up builds
- First run: ~15 minutes
- Subsequent runs: ~5 minutes (with cache)

**Parallel Execution**
- EditMode tests run first (fast feedback)
- PlayMode tests only run if EditMode passes
- Coverage report runs after both complete

---

## Troubleshooting

### Issue: "Unity license verification failed"

**Solution**:
1. Check `UNITY_LICENSE` secret contains full `.ulf` content
2. Verify `UNITY_EMAIL` and `UNITY_PASSWORD` are correct
3. Re-run license activation workflow if license expired

### Issue: "Test runner timed out"

**Solution**:
1. Check `timeout-minutes` in workflow file
2. Increase timeout if tests legitimately take longer
3. Optimize tests if they're running too slow

### Issue: "Tests pass locally but fail in CI"

**Possible Causes**:
- Missing scene/asset files (check .gitignore)
- Platform-specific code (Linux CI vs Windows local)
- Timing-dependent tests (increase wait times)
- Non-deterministic RNG (ensure seeded random in tests)

**Solution**:
1. Run tests in batch mode locally:
   ```bash
   unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml
   ```
2. Compare local vs CI logs
3. Fix platform-specific issues

### Issue: "Artifacts not found"

**Solution**:
- Ensure test results are generated before upload
- Check `artifactsPath` matches upload path
- Verify tests actually ran (check logs)

---

## Viewing Test Results

### In GitHub Actions

1. Go to `Actions` tab
2. Click on a workflow run
3. Click on job (e.g., "EditMode Tests")
4. Scroll to "Publish Test Report" step
5. Click on the test report link

### Test Report Features
- ✅ Pass/Fail status for each test
- ⏱️ Execution time per test
- 📊 Test suite summary
- 🔍 Failed test details with stack traces

---

## Cost Considerations

### GitHub Actions Free Tier
- **Public repositories**: Unlimited minutes
- **Private repositories**: 2,000 minutes/month

### Typical Usage (Vanguard Arena)
- EditMode tests: ~5 minutes
- PlayMode tests: ~15 minutes
- Total per run: ~20 minutes
- Commits per day: ~10
- **Monthly estimate**: 200 minutes (within free tier)

⚠️ If you exceed free tier, upgrade to GitHub Pro or optimize workflows.

---

## Advanced Configuration

### Running Tests on Specific Paths

To only run tests when code changes:

```yaml
on:
  push:
    branches: [ main, develop ]
    paths:
      - 'Assets/_Project/Scripts/**'
      - 'Assets/_Project/Tests/**'
```

### Matrix Testing (Multiple Unity Versions)

To test against multiple Unity versions:

```yaml
jobs:
  test:
    strategy:
      matrix:
        unity-version: 
          - 6000.0.23f1
          - 6000.0.25f1
    steps:
      - uses: game-ci/unity-test-runner@v4
        with:
          unityVersion: ${{ matrix.unity-version }}
```

### Slack/Discord Notifications

Add notification step:

```yaml
- name: Notify Slack
  if: failure()
  uses: slackapi/slack-github-action@v1
  with:
    payload: |
      {
        "text": "Tests failed on ${{ github.ref }}"
      }
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Maintenance

### Updating Unity Version

When upgrading Unity:

1. Update `unityVersion` in both workflow files:
   ```yaml
   unityVersion: 6000.3.5f1  # New version
   ```

2. Re-run license activation workflow (if needed)

3. Test locally first before pushing to CI

### Monitoring CI Health

Check these regularly:
- ✅ Tests passing consistently
- ⏱️ Execution time not increasing
- 💾 Cache hit rate (should be >80%)
- 💰 GitHub Actions usage (Settings → Billing)

---

## Next Steps

After CI/CD is set up:

1. ✅ Implement PlayMode tests (see `PHASE_6_PLAYMODE_IMPLEMENTATION_PLAN.md`)
2. ✅ Monitor first few workflow runs
3. ✅ Adjust timeouts if needed
4. ✅ Set up branch protection rules
5. ✅ Document workflow for team members

---

## Resources

- [GameCI Documentation](https://game.ci/docs/)
- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [Codecov Integration](https://docs.codecov.com/docs)

---

## Support

For issues with this setup:
1. Check troubleshooting section above
2. Review GitHub Actions logs
3. Search GameCI discussions: https://github.com/game-ci/unity-test-runner/discussions
4. Check this project's issues on GitHub

---

**Last Updated**: January 30, 2026  
**Unity Version**: 6000.0.23f1  
**GitHub Actions**: v4
