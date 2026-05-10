using GitRepoAnalyzer.Models;
using Octokit;
using System.Linq;
// ─────────────────────────────────────────────────────────────────────────────
// Analyzer Stubs
//
// Each class below is a skeleton for one report section.
// Every class:
//   • Receives the GitHubClient + owner/repo in its constructor
//   • Implements IAnalyzer (single AnalyzeAsync method)
//   • Populates its matching section on the RepoReport
//
// HOW TO WORK THROUGH THESE:
//   Pick one class at a time. Ask for a step-by-step walkthrough of that
//   analyzer only — tackle them in top-to-bottom order for the smoothest curve.
// ─────────────────────────────────────────────────────────────────────────────

namespace GitRepoAnalyzer.Analyzers;

// ── 1 ── Project Structure ───────────────────────────────────────────────────

/// <summary>
/// Walks the repository root contents and builds a top-level directory listing.
///
/// GitHub API calls you'll use:
///   github.Repository.Content.GetAllContents(owner, repo)
///   github.Repository.Content.GetAllContentsByRef(owner, repo, path, branch)
///
/// What to populate: <see cref="RepoReport.ProjectStructure"/>
/// </summary>
public class ProjectStructureAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    public ProjectStructureAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner  = owner;
        _repo   = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        // TODO: implement
        //   1. Call GetAllContents to list root entries
        //   2. Separate files vs directories
        //   3. Look for notable files (.gitignore, Dockerfile, CI yamls, LICENSE)
        //   4. Populate report.ProjectStructure

        var contents = await _github.Repository.Content.GetAllContents(_owner, _repo);
        
        var topLevelEntries = contents.Select(c=>c.Type.Value == ContentType.Dir ? $"{c.Name}/" : $"{c.Name}").ToList();

        var notableFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".gitignore", "LICENSE", "LICENSE.md","LICENSE.txt" ,
            "Dockerfile", "docker-compose.yml",
            ".env.example", "Makefile",
            ".github",".editorconfig"
        };

        var ciPatterns = new string[] {".yml", ".yaml" };

        var notableFiles = contents
            .Where(c => notableFileNames.Contains(c.Name) || (ciPatterns.Any(ext => c.Name.EndsWith(ext,StringComparison.OrdinalIgnoreCase))))
            .Select(c => c.Name)
            .ToList();

        var totalFiles = contents.Count(c => c.Type.Value == ContentType.File);
        var totalDirs = contents.Count(c => c.Type.Value == ContentType.Dir);

        report.ProjectStructure = new ProjectStructureResult
        {
            TopLevelEntries = topLevelEntries,
            NotableFiles = notableFiles,
            TotalFileCount = totalFiles,
            TotalDirectoryCount = totalDirs
        };

        
    }
}

// ── 2 ── Language Breakdown ──────────────────────────────────────────────────

/// <summary>
/// Fetches the GitHub language breakdown (bytes per language).
///
/// GitHub API calls you'll use:
///   github.Repository.GetAllLanguages(owner, repo)
///
/// What to populate: <see cref="RepoReport.LanguageBreakdown"/>
/// </summary>
public class LanguageAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    public LanguageAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner  = owner;
        _repo   = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        var rawLanguages = await _github.Repository.GetAllLanguages(_owner, _repo);

        var languages = rawLanguages
            .OrderByDescending(l => l.NumberOfBytes)
            .ToDictionary(l => l.Name, l => l.NumberOfBytes);

        var primaryLanguage = languages.Keys.FirstOrDefault() ?? "Unknown";

        report.LanguageBreakdown = new LanguageBreakdownResult
        {
            Languages = languages,
            PrimaryLanguage = primaryLanguage
        };
    }
}

// ── 3 ── README Quality ──────────────────────────────────────────────────────

/// <summary>
/// Downloads the README and scores it against a quality checklist.
///
/// GitHub API calls you'll use:
///   github.Repository.Content.GetReadme(owner, repo)
///
/// Scoring ideas (implement however you like):
///   • Existence check
///   • Word count (too short = low score)
///   • Contains Installation / Usage / Contributing sections?
///   • Has code blocks?
///   • Has badges?
///
/// What to populate: <see cref="RepoReport.ReadmeQuality"/>
/// </summary>
public class ReadmeAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    public ReadmeAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner  = owner;
        _repo   = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        // TODO: implement

        Readme readme;

        try
        {
            readme = await _github.Repository.Content.GetReadme(_owner, _repo);

        }
        catch (NotFoundException)
        { // No README found
            report.ReadmeQuality = new ReadmeQualityResult { Exists = false };
            return;
        }

        var content = readme.Content; // Base64-encoded content

        var wordCount = content.Split(' ','\n','\r').Count(w => !string.IsNullOrWhiteSpace(w));

        int score = 0;
        var strengths = new List<string>();
        var missing = new List<string>();

        if (wordCount >= 300) { 
            score += 20;
            strengths.Add("Good word count (300+)");
        }
        else if (wordCount >= 100) {
            score += 10;
            strengths.Add("Decent word count (100+)");
        }
        else missing.Add("Low word count (<100)");


        var sectionChecks = new Dictionary<string, (string[] Keywords, int Points, string StrengthLabel, string MissingLabel)>
        {
            ["installation"] = (["## install", "## getting started", "## setup"], 15, "Has installation section", "No installation / getting started section"),
            ["usage"] = (["## usage", "## how to", "## quickstart"], 15, "Has usage section", "No usage section"),
            ["contributing"] = (["## contribut", "## development"], 10, "Has contributing section", "No contributing section"),
            ["license"] = (["## license", "license"], 10, "Has license information", "No license information"),
            ["badges"] = (["![", "[!["], 5, "Has badges", "No badges"),
            ["codeblocks"] = (["```"], 10, "Has code examples", "No code examples"),
            ["description"] = (["## about", "## overview", "## description"], 15, "Has project description", "No project description / about section"),
        };

        var lowerContent = content.ToLower();

        foreach (var check in sectionChecks)
        {
            var (keywords, points, strengthLabel, missingLabel) = check.Value;

            if (keywords.Any(k => lowerContent.Contains(k)))
            {
                score += points;
                strengths.Add(strengthLabel);
            }
            else missing.Add(missingLabel);
        }

        var grade = score switch         {
            >= 80 => "A",
            >= 60 => "B",
            >= 40 => "C",
            >= 20 => "D",
            _ => "F"
        };

        report.ReadmeQuality = new ReadmeQualityResult
        {
            Exists = true,
            WordCount = wordCount,
            QualityScore = score,
            QualityGrade = grade,
            Strengths = strengths,
            MissingElements = missing
        };

    }
}

// ── 4 ── Code Metrics ────────────────────────────────────────────────────────

/// <summary>
/// Derives surface-level code metrics from information already available via API.
///
/// Note: deep metrics (cyclomatic complexity, test coverage) require cloning the
/// repo locally — flag that as a future enhancement for now.
///
/// GitHub API calls you'll use:
///   github.Repository.Content.GetAllContents — for file list + sizes
///
/// What to populate: <see cref="RepoReport.CodeMetrics"/>
/// </summary>
public class CodeMetricsAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
        ".pdf", ".zip", ".tar", ".gz", ".exe", ".dll",
        ".lock", ".sum"
    };

    public CodeMetricsAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner = owner;
        _repo = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        var contents = await _github.Repository.Content.GetAllContents(_owner, _repo);

        var codeFiles = contents
            .Where(c => c.Type.Value == ContentType.File)
            .Where(c => !IsIgnored(c.Name))
            .ToList();

        var totalFiles = codeFiles.Count;
        var totalBytes = codeFiles.Sum(c => c.Size);
        var avgFileSizeBytes = totalFiles > 0 ? (double)totalBytes / totalFiles : 0;

        var estimatedLines = totalBytes / 40;

        var largestFiles = codeFiles
            .OrderByDescending(c => c.Size)
            .Take(5)
            .Select(c => $"{c.Path} ({c.Size:N0} bytes)")
            .ToList();

        report.CodeMetrics = new CodeMetricsResult
        {
            TotalFiles = totalFiles,
            TotalLinesEstimate = estimatedLines,
            AverageFileSizeBytes = avgFileSizeBytes,
            LargestFiles = largestFiles
        };
    }

    private static bool IsIgnored(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && IgnoredExtensions.Contains(ext);
    }
}

// ── 5 ── Dependency Analysis ─────────────────────────────────────────────────

/// <summary>
/// Detects manifest files and extracts dependency names.
///
/// Strategy: look for well-known manifest file names in the root content list,
/// then fetch and parse each one found.
///
/// Manifests to handle (start with 1–2):
///   • package.json  (npm)
///   • *.csproj      (.NET / NuGet)
///   • requirements.txt (Python / pip)
///   • Gemfile       (Ruby)
///   • pom.xml       (Java / Maven)
///   • go.mod        (Go)
///
/// What to populate: <see cref="RepoReport.Dependencies"/>
/// </summary>
public class DependencyAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    private static readonly Dictionary<string, string> ManifestEcoSystems = new()
    {
        ["package.json"] = "npm",
        ["requirements.txt"] = "pip",
        ["Pipfile"] = "pip",
        ["*.csproj"] = "nuget",
        ["packages.config"] = "nuget",
        ["pom.xml"] = "maven",
        ["build.gradle"] = "gradle",
        ["go.mod"] = "go",
        ["Gemfile"] = "rubygems",
        ["composer.json"] = "composer",
        ["Cargo.toml"] = "cargo",
        ["pyproject.toml"] = "pip",
    };


    public DependencyAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner = owner;
        _repo = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        var contents = await _github.Repository.Content.GetAllContents(_owner, _repo);
        var manifestsFound = new List<string>();
        var allDependencies = new List<string>();
        var ecosystems = new HashSet<string>();

        foreach (var file in contents.Where(c => c.Type.Value == ContentType.File))
        {
            var matchedEcosystem = GetEcosystem(file.Name);
            if (matchedEcosystem is null) continue;

            manifestsFound.Add(file.Name);
            ecosystems.Add(matchedEcosystem);

            var deps = await ExtractDependenciesAsync(file.Name, matchedEcosystem);
            allDependencies.AddRange(deps);
        }

        report.Dependencies = new DependencyResult
        {
            ManifestFilesFound = manifestsFound,
            DetectedDependencies = allDependencies.Distinct().ToList(),
            EcosystemGuess = string.Join(", ", ecosystems)
        };

    }

    private static string? GetEcosystem(string fileName)
    {
        if (ManifestEcoSystems.TryGetValue(fileName, out var ecosystem))
            return ecosystem;
        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return "nuget";

        return null;
    }

    private async Task<List<string>> ExtractDependenciesAsync(string fileName, string ecosystem)
    {
        var deps = new List<string>();

        try
        {
            var fileContents = await _github.Repository.Content.GetAllContents(_owner, _repo, fileName);
            var raw = fileContents.FirstOrDefault()?.Content ?? string.Empty;

            deps = ecosystem switch
            {
                "npm" => ParseNpm(raw),
                "pip" => ParsePip(raw),
                "nuget" => ParseNuget(raw),
                _ => deps
            };

        }
        catch (Exception) { }
        return deps;

    }


    private static List<string> ParseNpm(string content)
    {
        var deps = new List<string>();
        var lines = content.Split('\n');

        bool inDepsBlock = false;

        foreach (var line in lines) {
            if (line.Contains("\"dependencies\"") || line.Contains("\"devDependencies\""))
            { inDepsBlock = true; continue; }

            if (inDepsBlock && line.Contains('}'))
            { inDepsBlock = false; continue; }

            if (inDepsBlock)
            {
                var trimmed = line.Trim().TrimEnd(',');
                var parts = trimmed.Split(':');
                if (parts.Length >= 2) deps.Add(parts[0].Trim('"', ' '));

            }
        }
        return deps;
    }

    private static List<string> ParsePip(string content)
    {
        return content
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            .Select(l => l.Split(['=', '>', '<', '!', '~'])[0].Trim())
            .ToList();
    }

    private static List<string> ParseNuget(string content)
    {
        var deps = new List<string>();
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (!line.Contains("PackageReference")) continue;

            var includeStart = line.IndexOf("Include=\"") + "Include=\"".Length;
            var includeEnd = line.IndexOf('"', includeStart);

            if (includeStart > "Include=\"".Length - 1 && includeEnd > includeStart) deps.Add(line[includeStart..includeEnd]);
        }

        return deps;
    }

}

// ── 6 ── Git Activity ────────────────────────────────────────────────────────

/// <summary>
/// Queries commit history and contributor stats.
///
/// GitHub API calls you'll use:
///   github.Repository.Commit.GetAll(owner, repo, commitRequest)
///   github.Repository.Statistics.GetContributors(owner, repo)
///
/// What to populate: <see cref="RepoReport.GitActivity"/>
/// </summary>
public class GitActivityAnalyzer : IAnalyzer
{
    private readonly GitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;

    public GitActivityAnalyzer(GitHubClient github, string owner, string repo)
    {
        _github = github;
        _owner = owner;
        _repo = repo;
    }

    public async Task AnalyzeAsync(RepoReport report)
    {
        var commitRequest = new CommitRequest
        {
            Since = DateTimeOffset.UtcNow.AddDays(-365), // last year
        };

        var commits = await _github.Repository.Commit.GetAll(_owner, _repo, commitRequest, new ApiOptions { PageSize = 100, PageCount = 10 });

        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

        var commitsLast30Days = commits.Count(c => c.Commit.Author.Date >= thirtyDaysAgo);

        var lastCommitDate = commits
            .OrderByDescending(c => c.Commit.Author.Date)
            .FirstOrDefault()?.Commit.Author.Date;

        var topContributors = new List<string>();
        int contributorCount = 0;

        try
        {
            var contributorTask = _github.Repository.Statistics.GetContributors(_owner, _repo);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(8));

            var winner = await Task.WhenAny(contributorTask, timeoutTask);

            if (winner == contributorTask)
            {
                var contributors = await contributorTask;
                contributorCount = contributors.Count;
                topContributors = contributors
                    .OrderByDescending(c => c.Total)
                    .Take(5)
                    .Select(c => $"{c.Author.Login} ({c.Total} commits)")
                    .ToList();
            }
        }
        catch (Exception)
        {
            // skip silently
        }

        report.GitActivity = new GitActivityResult
        {
            TotalCommits = commits.Count,
            CommitsLast30Days = commitsLast30Days,
            ContributorCount = contributorCount,
            LastCommitDate = lastCommitDate,
            TopContributors = topContributors
        };
    }
}

// ── 7 ── Summary ─────────────────────────────────────────────────────────────

/// <summary>
/// Reads all other completed sections and produces an overall verdict.
///
/// This analyzer has NO GitHub API calls — it only reads the RepoReport
/// that the previous analyzers already populated.
///
/// Scoring ideas:
///   • README score      → 0–30 pts
///   • Activity score    → 0–20 pts
///   • Language presence → 0–10 pts
///   • Has CI/CD files   → 0–10 pts
///   • Dependency health → 0–10 pts
///   • Structure clarity → 0–20 pts
///
/// What to populate: <see cref="RepoReport.Summary"/>
/// </summary>
public class SummaryAnalyzer : IAnalyzer
{
    public Task AnalyzeAsync(RepoReport report)
    {
        int score = 0;
        var strengths = new List<string>();
        var improvements = new List<string>();

        if (report.ReadmeQuality is { Exists: true } readme)
        {
            var readmeContribution = (int)(readme.QualityScore * 0.3);
            score += readmeContribution;

            if (readme.QualityScore >= 80) strengths.Add("High-quality README");
            else if (readme.QualityScore >= 60) strengths.Add("Decent README");
            else if (readme.QualityScore >= 40) improvements.Add("README could be more comprehensive");
            else improvements.Add("README needs significant improvement");

        }
        else improvements.Add("No README found - Add one");

        //----Activity scoring----
        if (report.GitActivity is { } activity)
        {
            if (activity.CommitsLast30Days >= 10) { score += 20; strengths.Add("Actively maintained (10+ commits/month)"); }
            else if (activity.CommitsLast30Days >= 3) { score += 12; strengths.Add("Moderately maintained"); }
            else if (activity.CommitsLast30Days >= 1) { score += 5; improvements.Add("Low recent commit activity"); }
            else { improvements.Add("No commits in the last 30 days"); }

            if (activity.ContributorCount > 5)
                strengths.Add($"Healthy contributor base ({activity.ContributorCount} contributors)");
        }

        //----Language presence----
        if (report.LanguageBreakdown is { } lang && lang.Languages.Count > 0)
        {
            score += 10;
            strengths.Add($"Primary Language: {lang.PrimaryLanguage}");

        }

        //----CI/CD presence----
        if (report.ProjectStructure is { } structure)
        {
            bool hasCi = structure.NotableFiles
                .Any(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));

            if (hasCi) {  score += 10; strengths.Add("CI/CD configuration detected"); }
            else improvements.Add("No CI/CD configuration files found");

            bool hasLicense = structure.NotableFiles
                .Any(f=>f.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase));

            if (hasLicense) { score += 5; strengths.Add("License file detected"); }
            else improvements.Add("No license file found");

        }

        //----Dependency health (basic heuristic)----
        if(report.Dependencies is { } deps && deps.ManifestFilesFound.Count > 0)
        {
            score += 10;
            strengths.Add($"Dependency manifest found ({deps.EcosystemGuess})");
        }

        score = Math.Min(score, 100);

        var overallHealth = score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            >= 20 => "Poor",
            _ => "Critical"
        };

        //Verdict Paragraph

        var repoName = $"{report.Owner}/{report.RepositoryName}";
        var lastActive = report.GitActivity?.LastCommitDate?.ToString("yyyy-MM-dd") ?? "unknown";

        var verdict = $"{repoName} scores {score}/100 ({overallHealth}). " +
                      $"Last commit: {lastActive}. " +
                      $"Identified {strengths.Count} strength(s) and {improvements.Count} area(s) for improvement.";

        report.Summary = new SummaryResult
        {
           OverallHealth = overallHealth,
           OverallScore = score,
           Strengths = strengths,
           Improvements = improvements,
           Verdict = verdict
        };

        return Task.CompletedTask;


    }
}
