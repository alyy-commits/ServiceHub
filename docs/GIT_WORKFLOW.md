# Git workflow

## Branches

```
main                  always stable, tagged releases (v1.0.0)
develop               integration branch – features are merged here
feature/solution-setup
feature/auth
feature/services
feature/appointments
feature/hangfire
feature/swagger
feature/docs
```

## Setup

```bash
git init
git add .
git commit -m "chore: initial solution structure"
git branch -M main
git remote add origin https://github.com/<your-user>/ServiceHub.git
git push -u origin main
git checkout -b develop
git push -u origin develop
```

## Daily flow

```bash
git checkout develop
git pull
git checkout -b feature/auth

# work, then commit small logical steps
git add .
git commit -m "feat(auth): add ASP.NET Core Identity and JWT generator"
git push -u origin feature/auth

# open a Pull Request feature/auth -> develop, review, squash or merge
git checkout develop && git pull
git branch -d feature/auth
```

Release: open a PR `develop -> main`, merge, then `git tag v1.0.0 && git push --tags`.

## Suggested commit messages (Conventional Commits)

```
chore: create Clean Architecture solution and project references
feat(domain): add Service and Appointment entities with status rules
feat(infra): configure EF Core DbContext, Identity and entity configurations
feat(infra): seed roles, demo users and services
feat(auth): add register and login commands with JWT
feat(services): add service CRUD commands and queries
feat(appointments): add create, cancel and status update commands
feat(appointments): prevent overlapping appointments
feat(validation): add FluentValidation pipeline behavior
feat(api): add global exception middleware and error response model
feat(hangfire): add reminder and status update recurring jobs
feat(swagger): add JWT security definition and XML comments
fix(appointments): return 409 for duplicate slot
docs: add README, testing guide and interview notes
```

Rules: never commit secrets or `bin/obj`; one feature per branch; commit often; write messages in the imperative mood.
