# Repository Guidelines

## Project Structure & Module Organization

This repository is currently a project seed: the root contains `.gitignore` and a database-connection reference image, but no solution, source, or test project yet. When implementation begins, keep the root uncluttered and use the conventional .NET layout:

- `src/<ProjectName>/` for application code and `.csproj` files.
- `tests/<ProjectName>.Tests/` for automated tests.
- `docs/` for sanitized diagrams and operational notes.
- `assets/` for non-sensitive images used by the application or documentation.

Add the solution file at the root so commands can be run consistently from this directory.

## Build, Test, and Development Commands

There are no runnable projects or repository-defined scripts yet. After adding a solution, contributors should support these standard commands:

- `dotnet restore` — restore NuGet dependencies.
- `dotnet build --configuration Release` — compile all projects and surface warnings.
- `dotnet test --configuration Release` — run the complete test suite.
- `dotnet run --project src/<ProjectName>` — start the application locally.

Document any required SDK version in `global.json` and update this section when custom tooling is introduced.

## Coding Style & Naming Conventions

Use four spaces for C# indentation and UTF-8 files. Follow .NET conventions: `PascalCase` for types, methods, and public members; `camelCase` for parameters and locals; and `IName` for interfaces. Prefer file-scoped namespaces, nullable reference types, and one primary type per file. Add an `.editorconfig` with the first source project, then run `dotnet format` before submitting changes.

## Testing Guidelines

Place tests beside the corresponding project under `tests/`, mirror its namespaces, and name test files `<Subject>Tests.cs`. Use descriptive test methods such as `ConnectAsync_InvalidCredentials_ReturnsFailure`. Every behavior change should include tests for the normal path and relevant failure cases. No coverage threshold is established yet; avoid reducing coverage once reporting is configured.

## Commit & Pull Request Guidelines

The repository has no commit history from which to infer conventions. Use short, imperative subjects, optionally with Conventional Commit prefixes, for example `feat: add connection validation`. Keep commits focused. Pull requests should explain the change, list verification commands, link related issues, and include screenshots for visible UI changes.

## Security & Configuration

Never commit passwords, connection strings, private keys, or unredacted screenshots. Treat the current connection reference image as sensitive and remove or sanitize it before sharing the repository. Store local secrets in environment variables or .NET user-secrets; keep `.env` files untracked. Use placeholders in documentation and provide safe configuration templates such as `.env.example`.
