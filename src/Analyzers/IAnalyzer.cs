using GitRepoAnalyzer.Models;

namespace GitRepoAnalyzer.Analyzers;

/// <summary>
/// Contract every analyzer must satisfy.
///
/// LEARNING NOTE:
///   An interface defines *what* a type can do, not *how*.
///   The orchestrator works against this interface, so you can add, swap, or
///   mock any analyzer without touching AnalysisOrchestrator.
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Runs the analysis and populates the relevant section of <paramref name="report"/>.
    /// Throw on hard failures; the orchestrator will catch and continue.
    /// </summary>
    Task AnalyzeAsync(RepoReport report);
}
