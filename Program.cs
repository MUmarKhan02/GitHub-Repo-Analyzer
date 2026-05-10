using System.CommandLine;
using GitRepoAnalyzer.Services;
using Spectre.Console;

// ─────────────────────────────────────────────────────────────────────────────
// GitRepoAnalyzer — Entry Point
//
// Wires together the CLI layer (System.CommandLine) with the core
// AnalysisOrchestrator that drives each analysis step in order.
// ─────────────────────────────────────────────────────────────────────────────

// ── 1. Define CLI options ────────────────────────────────────────────────────

var repoOption = new Option<string>(
    name: "--repo",
    description: "GitHub repository in 'owner/repo' format (e.g. dotnet/runtime)")
{
    IsRequired = true
};

var tokenOption = new Option<string?>(
    name: "--token",
    description: "GitHub Personal Access Token (PAT). Falls back to GITHUB_TOKEN env var.");

var outputOption = new Option<string>(
    name: "--output",
    description: "Output file path for the generated report.",
    getDefaultValue: () => "report.md");

var formatOption = new Option<ReportFormat>(
    name: "--format",
    description: "Output format: Markdown or Html.",
    getDefaultValue: () => ReportFormat.Markdown);

// ── 2. Build root command ────────────────────────────────────────────────────

var rootCommand = new RootCommand("Analyze a GitHub repository and generate a detailed report.")
{
    repoOption,
    tokenOption,
    outputOption,
    formatOption
};

rootCommand.SetHandler(async (repo, token, output, format) =>
{
    // Resolve token: CLI flag → env var → null (public repos only)
    var resolvedToken = token
        ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    AnsiConsole.Write(
        new FigletText("RepoAnalyzer")
            .LeftJustified()
            .Color(Color.DodgerBlue1));

    AnsiConsole.MarkupLine($"[bold]Repository:[/] [green]{repo}[/]");
    AnsiConsole.MarkupLine($"[bold]Output:[/]     [yellow]{output}[/] ([grey]{format}[/])");
    AnsiConsole.MarkupLine($"[bold]Token:[/]      {(resolvedToken is null ? "[red]none (public only)[/]" : "[green]provided[/]")}");
    AnsiConsole.WriteLine();

    // ── 3. Build and run orchestrator ───────────────────────────────────────
    var orchestrator = new AnalysisOrchestrator(repo, resolvedToken, output, format);

    try
    {
        await orchestrator.RunAsync();
        AnsiConsole.MarkupLine($"\n[bold green]✓ Report written to:[/] {output}");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[bold red]✗ Analysis failed:[/] {ex.Message}");
        Environment.Exit(1);
    }

}, repoOption, tokenOption, outputOption, formatOption);

// ── 4. Execute ───────────────────────────────────────────────────────────────
return await rootCommand.InvokeAsync(args);

// ─────────────────────────────────────────────────────────────────────────────
// Enum lives here for simplicity — move to Models/ as the project grows.
// ─────────────────────────────────────────────────────────────────────────────
public enum ReportFormat { Markdown, Html }
