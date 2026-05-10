# GitRepoAnalyzer

A .NET 10 CLI tool that analyzes a GitHub repository and generates a detailed Markdown or HTML report.

---

## Report Sections
- Repository Overview
- Project Structure
- Language Breakdown
- README Quality
- Code Metrics
- Dependency Analysis
- Git Activity
- Summary

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| GitHub Personal Access Token (PAT) | Optional — public repos work without one |

---

## Setup

```bash
# 1. Clone this repo
git clone https://github.com/MUmarKhan02/GitRepoAnalyzer.git
cd GitRepoAnalyzer

# 2. Restore NuGet packages
dotnet restore

# 3. Build
dotnet build

# 4. Run
dotnet run -- --repo owner/repo --output report.md
```

## Usage

```
GitRepoAnalyzer [options]

Options:
--repo    <owner/repo>    GitHub repository to analyze  [required]
--token   <token>         GitHub PAT (or set GITHUB_TOKEN env var)
--output  <path>          Output file path  [default: report.md]
--format  <Markdown|Html> Output format     [default: Markdown]
--help                    Show help                Show help
```

---

### Avoiding rate limits

GitHub allows 60 requests/hour unauthenticated. For best results use a PAT — no scopes needed for public repos.

```powershell
# Option A: pass inline
dotnet run -- --repo owner/repo --token ghp_XXXX

# Option B: set permanently (Windows)
[System.Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "ghp_XXXX", "User")
```

---

## Running as a standalone exe

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

Outputs to `bin\Release\net10.0\win-x64\publish\GitRepoAnalyzer.exe` — run from anywhere without needing `dotnet run`.

---
