# Copilot Instructions — TenantCore.App

Use `CLAUDE.md` in the repository root as the primary project instruction reference.

Also use the Claude command definitions in `.claude/commands/`:
- `plan-clinic-feature.md`
- `execute-clinic-feature.md`
- `bugfix.md`

When working from a newly created issue, follow this workflow:
1. Run the **plan feature** command (`/plan-clinic-feature`) first to produce the implementation plan.
2. After the plan is ready and reviewed, run the **execute feature** command (`/execute-clinic-feature`) to implement the feature.

Keep the plan-first, execute-second workflow as the default behavior for feature work in this repository.
