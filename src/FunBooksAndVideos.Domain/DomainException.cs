namespace FunBooksAndVideos.Domain;

/// <summary>
/// Thrown when a domain invariant would be broken. The API reports it as a client error.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
