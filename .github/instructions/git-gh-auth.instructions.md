---
description: "Use when running git or gh commands in this repository, troubleshooting GitHub authentication errors, or preparing issue and PR automation. Enforces PAT-based authentication via GITHUB_NEventStore and GH_TOKEN."
---

# Git And gh Authentication Rules

- Use PAT-based authentication from the environment variable `GITHUB_NEventStore` for this repository.
- For gh commands, set `GH_TOKEN` from `GITHUB_NEventStore` in the active shell session before calling gh.
- Never echo, print, log, or persist token values.
- Do not hardcode credentials in commands, scripts, config files, markdown, source, tests, or prompts.
- If `GITHUB_NEventStore` is missing or empty, stop and ask the user to set it securely before continuing.
- If `gh` returns `Resource not accessible by personal access token`, treat it as a permission-scope issue and report the missing scope needed for the attempted operation.
- Prefer session-scoped auth over permanent credential storage.

## Quick Session Setup
PowerShell:

```powershell
$env:GH_TOKEN = $env:GITHUB_NEventStore
if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { throw "GITHUB_NEventStore is not set" }
gh auth status
```

Bash:

```bash
export GH_TOKEN="$GITHUB_NEventStore"
if [ -z "$GH_TOKEN" ]; then echo "GITHUB_NEventStore is not set"; exit 1; fi
gh auth status
```