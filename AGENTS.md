# Repository Guidelines

This repository is an ASP.NET Core (.NET 10) application structured as a modular monolith under `Landscape.Tsi.slnx`:

- `src/Landscape.Tsi.Domain/` for domain models, invariants, and catalog/IAM entities.
- `src/Landscape.Tsi.Application/` for application use cases, contracts, validation, and services.
- `src/Landscape.Tsi.Infrastructure/` for EF Core contexts, SQL Server mappings, IAM, and external services.
- `src/Landscape.Tsi.Web/` for the ASP.NET Core MVC presentation layer (controllers, views, viewmodels).
- `tests/Landscape.Tsi.Tests/` for unit, integration, and architecture tests.
- `tools/Landscape.Tsi.PasswordReset/` for administrative security maintenance utilities.
- `docs/` for sanitized diagrams, architecture notes, and reconciliations.
- `scripts/` for sanitized SQL validation and reconciliation scripts.

## Build, Test, and Development Commands

Contributors should use the following standard commands from the solution root:

- `dotnet restore` — restore NuGet dependencies.
- `dotnet build --configuration Release` — compile all projects and surface warnings.
- `dotnet test --configuration Release` — run the complete test suite.
- `dotnet run --project src/Landscape.Tsi.Web` — start the web application locally.

Document any required SDK version in `global.json` and update this section when custom tooling is introduced.

## Coding Style & Naming Conventions

Use four spaces for C# indentation and UTF-8 files. Follow .NET conventions: `PascalCase` for types, methods, and public members; `camelCase` for parameters and locals; and `IName` for interfaces. Prefer file-scoped namespaces, nullable reference types, and one primary type per file. Add an `.editorconfig` with the first source project, then run `dotnet format` before submitting changes.

## Testing Guidelines

Place tests beside the corresponding project under `tests/`, mirror its namespaces, and name test files `<Subject>Tests.cs`. Use descriptive test methods such as `ConnectAsync_InvalidCredentials_ReturnsFailure`. Every behavior change should include tests for the normal path and relevant failure cases. No coverage threshold is established yet; avoid reducing coverage once reporting is configured.

## Commit & Pull Request Guidelines

The repository has no commit history from which to infer conventions. Use short, imperative subjects, optionally with Conventional Commit prefixes, for example `feat: add connection validation`. Keep commits focused. Pull requests should explain the change, list verification commands, link related issues, and include screenshots for visible UI changes.

## Security & Configuration

Never commit passwords, connection strings, private keys, or unredacted screenshots. Treat the current connection reference image as sensitive and remove or sanitize it before sharing the repository. Store local secrets in environment variables or .NET user-secrets; keep `.env` files untracked. Use placeholders in documentation and provide safe configuration templates such as `.env.example`.
