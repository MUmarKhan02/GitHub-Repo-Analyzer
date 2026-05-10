# GitRepoAnalyzer

A .NET 8 CLI tool that analyzes a GitHub repository and generates a detailed Markdown or HTML report.

## Features (planned)
- Project structure overview
- Language breakdown (via GitHub API)
- README quality scoring
- Code metrics (file count, size estimates)
- Dependency manifest detection
- Git activity & contributor stats
- Overall health summary

---

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| GitHub Personal Access Token (PAT) | Optional — public repos work without one |

---

## Setup

```bash
# 1. Clone this repo
git clone https://github.com/YOUR_USERNAME/GitRepoAnalyzer.git
cd GitRepoAnalyzer

# 2. Restore NuGet packages
dotnet restore

# 3. Build
dotnet build

# 4. Run (replace owner/repo as needed)
dotnet run -- --repo dotnet/runtime --output report.md
```

### With a GitHub PAT (avoids rate limiting)
```bash
# Option A: pass inline
dotnet run -- --repo dotnet/runtime --token ghp_XXXX

# Option B: set environment variable (recommended)
export GITHUB_TOKEN=ghp_XXXX
dotnet run -- --repo dotnet/runtime
```

---

## Usage

```
GitRepoAnalyzer [options]

Options:
  --repo    <owner/repo>   GitHub repository to analyze  [required]
  --token   <token>        GitHub PAT (or set GITHUB_TOKEN env var)
  --output  <path>         Output file path  [default: report.md]
  --format  <Markdown|Html> Output format  [default: Markdown]
  --help                   Show help
```

---

## Project Structure

```
GitRepoAnalyzer/
├── Program.cs                    ← CLI entry point (System.CommandLine)
├── GitRepoAnalyzer.csproj        ← NuGet dependencies
└── src/
    ├── Services/
    │   └── AnalysisOrchestrator.cs   ← Coordinates all analyzers
    ├── Models/
    │   └── RepoReport.cs             ← Central data model
    ├── Analyzers/
    │   ├── IAnalyzer.cs              ← Interface every analyzer implements
    │   └── Analyzers.cs              ← All 7 analyzer stubs
    └── Reports/
        └── ReportWriters.cs          ← Markdown + HTML writers
```

---

## Development Roadmap

Implement these one at a time (in order):

1. **LanguageAnalyzer** — simplest, one API call, good for learning Octokit
2. **ProjectStructureAnalyzer** — directory listing, filtering
3. **ReadmeAnalyzer** — fetch content, text analysis, scoring
4. **GitActivityAnalyzer** — paginated commit history, date math
5. **DependencyAnalyzer** — file detection + JSON/XML parsing
6. **CodeMetricsAnalyzer** — aggregate file sizes, estimations
7. **SummaryAnalyzer** — pure logic, no API calls, reads other sections
8. **MarkdownReportWriter** — fill in each TODO section
9. **HtmlReportWriter** — CSS polish, maybe charts

---

## Key NuGet Packages

| Package | Why |
|---------|-----|
| [Octokit](https://github.com/octokit/octokit.net) | Typed .NET client for the GitHub REST API |
| [Spectre.Console](https://spectreconsole.net) | Rich terminal output: spinners, tables, colors |
| [System.CommandLine](https://github.com/dotnet/command-line-api) | CLI argument parsing and help generation |
| [Markdig](https://github.com/xoofx/markdig) | Converts Markdown to HTML |
