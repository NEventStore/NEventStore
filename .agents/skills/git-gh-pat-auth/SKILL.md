---
name: git-gh-pat-auth
description: "Use when: authenticating git and GitHub CLI for NEventStore tasks, fixing gh auth errors, setting PAT environment variables, preparing a shell session for git push and gh issue or pr commands."
argument-hint: "Goal, for example: create issue, push branch, open PR"
user-invocable: false
---

# Git And gh Authentication With PAT

## Outcome
Prepare the current shell session so both git remote operations and gh CLI commands authenticate using a personal access token from the environment variable GITHUB_NEventStore.

## When To Use
- gh returns authentication or permission errors.
- git push, fetch, or remote operations fail due to missing credentials.
- You need a repeatable login flow without interactive prompts.
- You are preparing automation that must avoid hardcoded secrets.

## Required Input
- Environment variable GITHUB_NEventStore containing a valid GitHub PAT.
- Repository owner and name, when command scoping is needed.

## Procedure
1. Validate the token variable exists in the current shell session.
2. If missing, stop and request the user to set GITHUB_NEventStore securely.
3. Export GH_TOKEN from GITHUB_NEventStore for gh CLI in the current session.
4. Confirm gh authentication status.
5. Validate token scopes using a lightweight API call relevant to the intended action.
6. Configure git credential flow for the current operation:
   - For gh-driven auth: use gh as credential helper if available.
   - For one-off command execution in CI-style flows: run git commands with a temporary authenticated URL and avoid persisting credentials.
7. Run the target git or gh command.
8. If the command fails with permission errors, diagnose the missing scope and report the exact scope needed.

## Decision Points
- Missing GITHUB_NEventStore:
  - Stop and ask user to set it.
- gh auth status shows not logged in:
  - Re-export GH_TOKEN and re-check.
- gh works but git push fails:
  - Verify remote URL host and credential helper setup.
- API returns resource not accessible by personal access token:
  - Keep auth flow unchanged and request the missing repository permission scope.

## Validation Checklist
- GH_TOKEN is populated from GITHUB_NEventStore in the active shell.
- gh auth status is successful.
- A read API check succeeds for the target repository.
- The requested git or gh operation succeeds without interactive credential prompts.

## Security Rules
- Never print token values.
- Never commit token values to files.
- Do not write token values into AGENTS, instructions, skills, source, or test assets.
- Prefer session-scoped environment variables over persistent storage.

## Common Commands
PowerShell session setup:

```powershell
$env:GH_TOKEN = $env:GITHUB_NEventStore
if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { throw "GITHUB_NEventStore is not set" }
gh auth status
```

Bash session setup:

```bash
export GH_TOKEN="$GITHUB_NEventStore"
if [ -z "$GH_TOKEN" ]; then echo "GITHUB_NEventStore is not set"; exit 1; fi
gh auth status
```

Repository access check:

```
gh api repos/NEventStore/NEventStore > /dev/null
```

## Completion Criteria
- The target git or gh command is completed successfully.
- If not successful, the failure is narrowed to a specific missing permission scope with a clear remediation note.
