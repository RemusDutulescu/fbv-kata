namespace FunBooksAndVideos.Domain;

/// <summary>
/// Thrown when a domain invariant would be broken. Contract validation should reject bad input first,
/// so reaching this means input slipped past it; the API still reports it as a client error.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
