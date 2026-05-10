using Octokit;

namespace GitRepoAnalyzer.Models;

// ─────────────────────────────────────────────────────────────────────────────
// RepoReport
//
// The central data model that flows through the entire pipeline.
// Each analyzer fills in its own section; the report writer reads it all.
//
// LEARNING NOTE:
//   Properties are marked nullable (?) because analyzers run independently —
//   if one fails, its section stays null and the writer can skip it gracefully.
// ─────────────────────────────────────────────────────────────────────────────

public class RepoReport
{
    // ── Core identity ────────────────────────────────────────────────────────
    public string Owner          { get; init; } = string.Empty;
    public string RepositoryName { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }

    // Raw Octokit repository object — contains stars, forks, topics, etc.
    public Repository? RepoMetadata { get; set; }

    // ── Analysis sections ────────────────────────────────────────────────────
    public ProjectStructureResult?  ProjectStructure  { get; set; }
    public LanguageBreakdownResult? LanguageBreakdown { get; set; }
    public ReadmeQualityResult?     ReadmeQuality     { get; set; }
    public CodeMetricsResult?       CodeMetrics       { get; set; }
    public DependencyResult?        Dependencies      { get; set; }
    public GitActivityResult?       GitActivity       { get; set; }
    public SummaryResult?           Summary           { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Section result models — one per analyzer.
// Start simple (just stubs); flesh out properties as you implement each one.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>File/folder tree, root-level breakdown, notable config files found.</summary>
public class ProjectStructureResult
{
    public List<string> TopLevelEntries    { get; set; } = [];
    public List<string> NotableFiles       { get; set; } = []; // .gitignore, CI configs, etc.
    public int          TotalFileCount     { get; set; }
    public int          TotalDirectoryCount { get; set; }
}

/// <summary>Byte counts per language returned directly by the GitHub API.</summary>
public class LanguageBreakdownResult
{
    /// <summary>Key = language name, Value = byte count.</summary>
    public Dictionary<string, long> Languages { get; set; } = [];
    public string PrimaryLanguage { get; set; } = string.Empty;
}

/// <summary>Heuristic quality score and checklist for the README.</summary>
public class ReadmeQualityResult
{
    public bool   Exists              { get; set; }
    public int    WordCount           { get; set; }
    public int    QualityScore        { get; set; }   // 0–100
    public string QualityGrade        { get; set; } = string.Empty; // A / B / C / F
    public List<string> MissingElements { get; set; } = []; // e.g. "No installation section"
    public List<string> Strengths       { get; set; } = [];
}

/// <summary>Surface-level code quality signals extractable via the API.</summary>
public class CodeMetricsResult
{
    public int TotalFiles { get; set; }
    public long TotalLinesEstimate { get; set; }
    public double AverageFileSizeBytes { get; set; }
    public List<string> LargestFiles { get; set; } = [];
}

/// <summary>Detected package manifests and key dependencies.</summary>
public class DependencyResult
{
    public List<string> ManifestFilesFound  { get; set; } = []; // package.json, *.csproj, etc.
    public List<string> DetectedDependencies { get; set; } = [];
    public string       EcosystemGuess       { get; set; } = string.Empty; // npm / nuget / pip …
}

/// <summary>Commit history stats and contributor signals.</summary>
public class GitActivityResult
{
    public int    TotalCommits           { get; set; }
    public int    CommitsLast30Days      { get; set; }
    public int    ContributorCount       { get; set; }
    public DateTimeOffset? LastCommitDate { get; set; }
    public List<string> TopContributors  { get; set; } = [];
}

/// <summary>AI-style overall verdict assembled from all other sections.</summary>
public class SummaryResult
{
    public string OverallHealth  { get; set; } = string.Empty; // Good / Fair / Needs Work
    public int    OverallScore   { get; set; }   // 0–100
    public List<string> Strengths     { get; set; } = [];
    public List<string> Improvements  { get; set; } = [];
    public string Verdict        { get; set; } = string.Empty;
}
