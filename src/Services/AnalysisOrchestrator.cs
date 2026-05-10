using GitRepoAnalyzer.Analyzers;
using GitRepoAnalyzer.Models;
using GitRepoAnalyzer.Reports;
using Octokit;
using Spectre.Console;

namespace GitRepoAnalyzer.Services;

/// <summary>
/// Orchestrates the full analysis pipeline.
///
/// Responsibilities:
///   1. Initialize the GitHub client
///   2. Run each analyzer in sequence, collecting results into a <see cref="RepoReport"/>
///   3. Pass the completed report to the correct <see cref="IReportWriter"/>
///
/// Each analyzer is independent — you can run or skip them individually.
/// </summary>
public class AnalysisOrchestrator
{
    private readonly string _owner;
    private readonly string _repoName;
    private readonly string? _token;
    private readonly string _outputPath;
    private readonly ReportFormat _format;

    public AnalysisOrchestrator(
        string repo,
        string? token,
        string outputPath,
        ReportFormat format)
    {
        // Expect "owner/repo" — split and validate
        var parts = repo.Split('/', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ArgumentException("Repository must be in 'owner/repo' format.", nameof(repo));

        _owner      = parts[0].Trim();
        _repoName   = parts[1].Trim();
        _token      = token;
        _outputPath = outputPath;
        _format     = format;
    }

    /// <summary>
    /// Entry point called from Program.cs. Runs all analysis steps, then writes the report.
    /// </summary>
    public async Task RunAsync()
    {
        // ── Step 1: Initialize GitHub client ────────────────────────────────
        var github = BuildGitHubClient();

        // ── Step 2: Fetch the base repository metadata ───────────────────── 
        Repository repo = await AnsiConsole
            .Status()
            .StartAsync("Fetching repository metadata...", _ =>
                github.Repository.Get(_owner, _repoName));

        // ── Step 3: Build the report container ──────────────────────────────
        var report = new RepoReport
        {
            Owner        = _owner,
            RepositoryName = _repoName,
            GeneratedAt  = DateTimeOffset.UtcNow,
            RepoMetadata = repo
        };

        // ── Step 4: Run each analyzer ────────────────────────────────────────
        //
        // LEARNING NOTE:
        //   Each analyzer receives the GitHub client + partial report, does its
        //   work, then populates its own section on the report object.
        //   You'll implement each of these one by one.
        //
        await RunAnalyzerAsync("Project Structure",    new ProjectStructureAnalyzer(github, _owner, _repoName), report);
        await RunAnalyzerAsync("Language Breakdown",   new LanguageAnalyzer(github, _owner, _repoName),         report);
        await RunAnalyzerAsync("README Quality",       new ReadmeAnalyzer(github, _owner, _repoName),           report);
        await RunAnalyzerAsync("Code Metrics",         new CodeMetricsAnalyzer(github, _owner, _repoName),      report);
        await RunAnalyzerAsync("Dependency Analysis",  new DependencyAnalyzer(github, _owner, _repoName),       report);
        await RunAnalyzerAsync("Git Activity",         new GitActivityAnalyzer(github, _owner, _repoName),      report);
        await RunAnalyzerAsync("General Summary",      new SummaryAnalyzer(),                                   report);

        // ── Step 5: Write the report ─────────────────────────────────────────
        IReportWriter writer = _format switch
        {
            ReportFormat.Html     => new HtmlReportWriter(),
            ReportFormat.Markdown => new MarkdownReportWriter(),
            _                     => new MarkdownReportWriter()
        };

        await writer.WriteAsync(report, _outputPath);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps each analyzer call with a Spectre.Console progress spinner and
    /// basic error handling so one failing analyzer doesn't kill the whole run.
    /// </summary>
    private static async Task RunAnalyzerAsync(string label, IAnalyzer analyzer, RepoReport report)
    {
        await AnsiConsole.Status().StartAsync($"[grey]Analyzing:[/] {label}...", async _ =>
        {
            try
            {
                await analyzer.AnalyzeAsync(report);
                AnsiConsole.MarkupLine($"  [green]✓[/] {label}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [yellow]⚠[/] {label} [grey]skipped — {ex.Message}[/]");
                // Individual failures are non-fatal; the section will be empty in the report.
            }
        });
    }

    /// <summary>Builds the Octokit GitHub client with optional PAT authentication.</summary>
    private GitHubClient BuildGitHubClient()
    {
        var client = new GitHubClient(new ProductHeaderValue("GitRepoAnalyzer"));

        if (_token is not null)
            client.Credentials = new Credentials(_token);

        return client;
    }
}
