namespace EventScanner.Models;

/// <summary>
/// A single heuristic value pulled from raw event text for quick manual pivots (CTF / triage).
/// </summary>
public sealed record ExtractedPivot(string Category, string Value);
