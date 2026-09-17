namespace Landscape.Tsi.Domain.Identity;

public sealed class IamEventoAutenticacion
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? UserIdentifier { get; set; }
    public required string Mechanism { get; set; }
    public required string Result { get; set; }
    public required string EventType { get; set; }
    public string? CorrelationId { get; set; }
}