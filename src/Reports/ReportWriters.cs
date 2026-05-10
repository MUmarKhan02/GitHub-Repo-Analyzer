using GitRepoAnalyzer.Models;
using System.Text;

namespace GitRepoAnalyzer.Reports;

// ─────────────────────────────────────────────────────────────────────────────
// IReportWriter
//
// Abstracts how the finished report is serialized to disk.
// The orchestrator picks the right implementation at runtime via the --format flag.
// ─────────────────────────────────────────────────────────────────────────────

public interface IReportWriter
{
    /// <summary>Serialize <paramref name="report"/> and write it to <paramref name="outputPath"/>.</summary>
    Task WriteAsync(RepoReport report, string outputPath);
}

// ─────────────────────────────────────────────────────────────────────────────
// MarkdownReportWriter
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Writes the report as a Markdown (.md) file.
///
/// LEARNING NOTE:
///   StringBuilder is more efficient than string concatenation in a loop.
///   File.WriteAllTextAsync writes the whole string to disk asynchronously.
/// </summary>
public class MarkdownReportWriter : IReportWriter
{
    public async Task WriteAsync(RepoReport report, string outputPath)
    {
        var sb = new StringBuilder();

        WriteHeader(sb, report);
        WriteRepositoryOverview(sb, report);
        WriteProjectStructure(sb, report);
        WriteLanguageBreakdown(sb, report);
        WriteReadmeQuality(sb, report);
        WriteCodeMetrics(sb, report);
        WriteDependencies(sb, report);
        WriteGitActivity(sb, report);
        WriteSummary(sb, report);

        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private static void WriteHeader(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine($"# Repository Analysis Report");
        sb.AppendLine($"**Repository:** `{report.Owner}/{report.RepositoryName}`  ");
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC  ");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Repository Overview ───────────────────────────────────────────────────

    private static void WriteRepositoryOverview(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Repository Overview");

        if (report.RepoMetadata is null)
        {
            sb.AppendLine("_Metadata not available._");
            sb.AppendLine();
            return;
        }

        var meta = report.RepoMetadata;

        sb.AppendLine($"- **Description:** {meta.Description ?? "_none_"}");
        sb.AppendLine($"- **Primary Language:** {meta.Language ?? "Unknown"}");
        sb.AppendLine($"- **Stars:** {meta.StargazersCount:N0}");
        sb.AppendLine($"- **Forks:** {meta.ForksCount:N0}");
        sb.AppendLine($"- **Open Issues:** {meta.OpenIssuesCount:N0}");
        sb.AppendLine($"- **Default Branch:** `{meta.DefaultBranch}`");
        sb.AppendLine($"- **Visibility:** {(meta.Private ? "Private" : "Public")}");

        if (meta.Topics is { Count: > 0 })
            sb.AppendLine($"- **Topics:** {string.Join(", ", meta.Topics)}");

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Project Structure ─────────────────────────────────────────────────────

    private static void WriteProjectStructure(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Project Structure");

        if (report.ProjectStructure is not { } ps)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Root files:** {ps.TotalFileCount}");
        sb.AppendLine($"- **Root directories:** {ps.TotalDirectoryCount}");
        sb.AppendLine();

        if (ps.TopLevelEntries.Count > 0)
        {
            sb.AppendLine("**Top-level entries:**");
            sb.AppendLine("```");
            foreach (var entry in ps.TopLevelEntries)
                sb.AppendLine(entry);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (ps.NotableFiles.Count > 0)
        {
            sb.AppendLine("**Notable files detected:**");
            foreach (var file in ps.NotableFiles)
                sb.AppendLine($"- `{file}`");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Language Breakdown ────────────────────────────────────────────────────

    private static void WriteLanguageBreakdown(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Language Breakdown");

        if (report.LanguageBreakdown is not { } lb || lb.Languages.Count == 0)
        {
            sb.AppendLine("_No language data available._");
            sb.AppendLine();
            return;
        }

        var totalBytes = lb.Languages.Values.Sum();

        sb.AppendLine($"| Language | Bytes | Percentage |");
        sb.AppendLine($"|----------|-------|------------|");

        foreach (var (language, bytes) in lb.Languages)
        {
            var percentage = totalBytes > 0 ? (double)bytes / totalBytes * 100 : 0;
            sb.AppendLine($"| {language} | {bytes:N0} | {percentage:F1}% |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── README Quality ────────────────────────────────────────────────────────

    private static void WriteReadmeQuality(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## README Quality");

        if (report.ReadmeQuality is not { } rq)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        if (!rq.Exists)
        {
            sb.AppendLine("⚠️ **No README file found.**");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Score:** {rq.QualityScore}/100");
        sb.AppendLine($"- **Grade:** {rq.QualityGrade}");
        sb.AppendLine($"- **Word Count:** {rq.WordCount:N0}");
        sb.AppendLine();

        if (rq.Strengths.Count > 0)
        {
            sb.AppendLine("**Strengths:**");
            foreach (var s in rq.Strengths)
                sb.AppendLine($"- ✅ {s}");
            sb.AppendLine();
        }

        if (rq.MissingElements.Count > 0)
        {
            sb.AppendLine("**Missing / Needs Improvement:**");
            foreach (var m in rq.MissingElements)
                sb.AppendLine($"- ❌ {m}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Code Metrics ──────────────────────────────────────────────────────────

    private static void WriteCodeMetrics(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Code Metrics");

        if (report.CodeMetrics is not { } cm)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Total files analyzed:** {cm.TotalFiles:N0}");
        sb.AppendLine($"- **Estimated lines of code:** {cm.TotalLinesEstimate:N0}");
        sb.AppendLine($"- **Average file size:** {cm.AverageFileSizeBytes:N0} bytes");
        sb.AppendLine();

        if (cm.LargestFiles.Count > 0)
        {
            sb.AppendLine("**Largest files:**");
            foreach (var file in cm.LargestFiles)
                sb.AppendLine($"- `{file}`");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Dependencies ──────────────────────────────────────────────────────────

    private static void WriteDependencies(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Dependency Analysis");

        if (report.Dependencies is not { } dep)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        if (dep.ManifestFilesFound.Count == 0)
        {
            sb.AppendLine("_No dependency manifests detected at root level._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Ecosystem:** {dep.EcosystemGuess}");
        sb.AppendLine($"- **Manifests found:** {string.Join(", ", dep.ManifestFilesFound.Select(f => $"`{f}`"))}");
        sb.AppendLine($"- **Dependencies detected:** {dep.DetectedDependencies.Count}");
        sb.AppendLine();

        if (dep.DetectedDependencies.Count > 0)
        {
            sb.AppendLine("**Detected dependencies:**");
            foreach (var d in dep.DetectedDependencies)
                sb.AppendLine($"- `{d}`");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Git Activity ──────────────────────────────────────────────────────────

    private static void WriteGitActivity(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Git Activity");

        if (report.GitActivity is not { } ga)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Total commits (last 12 months):** {ga.TotalCommits:N0}");
        sb.AppendLine($"- **Commits (last 30 days):** {ga.CommitsLast30Days:N0}");
        sb.AppendLine($"- **Contributors:** {ga.ContributorCount:N0}");
        sb.AppendLine($"- **Last commit:** {ga.LastCommitDate?.ToString("yyyy-MM-dd") ?? "unknown"}");
        sb.AppendLine();

        if (ga.TopContributors.Count > 0)
        {
            sb.AppendLine("**Top contributors:**");
            foreach (var contributor in ga.TopContributors)
                sb.AppendLine($"- {contributor}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    private static void WriteSummary(StringBuilder sb, RepoReport report)
    {
        sb.AppendLine("## Summary");

        if (report.Summary is not { } sum)
        {
            sb.AppendLine("_Analysis not available._");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- **Overall Score:** {sum.OverallScore}/100");
        sb.AppendLine($"- **Overall Health:** {sum.OverallHealth}");
        sb.AppendLine();
        sb.AppendLine($"> {sum.Verdict}");
        sb.AppendLine();

        if (sum.Strengths.Count > 0)
        {
            sb.AppendLine("**Strengths:**");
            foreach (var s in sum.Strengths)
                sb.AppendLine($"- ✅ {s}");
            sb.AppendLine();
        }

        if (sum.Improvements.Count > 0)
        {
            sb.AppendLine("**Areas for improvement:**");
            foreach (var i in sum.Improvements)
                sb.AppendLine($"- 🔧 {i}");
            sb.AppendLine();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// HtmlReportWriter
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Writes the report as a styled HTML file.
///
/// Strategy: generate the same Markdown string via <see cref="MarkdownReportWriter"/>,
/// then convert it to HTML using the Markdig library.
/// This keeps the two writers in sync — you only maintain Markdown logic.
///
/// LEARNING NOTE:
///   Dependency injection would let you pass a MarkdownReportWriter in here.
///   For now, we just new it up — fine for a CLI tool of this size.
/// </summary>
public class HtmlReportWriter : IReportWriter
{
    public async Task WriteAsync(RepoReport report, string outputPath)
    {
        // Step 1: generate Markdown string (reuse existing writer)
        var tempMdPath = Path.GetTempFileName();
        await new MarkdownReportWriter().WriteAsync(report, tempMdPath);
        var markdown = await File.ReadAllTextAsync(tempMdPath);
        File.Delete(tempMdPath);

        // Step 2: convert to HTML via Markdig
        var htmlBody = Markdig.Markdown.ToHtml(markdown);

        // Step 3: wrap in a minimal HTML shell
        // TODO: add CSS for nicer styling
        var html = $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>{{report.Owner}}/{{report.RepositoryName}} — Analysis Report</title>
      <style>
        body { font-family: system-ui, sans-serif; max-width: 860px; margin: 2rem auto; padding: 0 1rem; }
        code { background: #f4f4f4; padding: 0.1em 0.3em; border-radius: 3px; }
        pre  { background: #f4f4f4; padding: 1rem; overflow-x: auto; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 0.5rem; }
      </style>
    </head>
    <body>
    {{htmlBody}}
    </body>
    </html>
    """;

        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8);
    }
}
