using CloudShield.Commons;

namespace CloudShield.Entities.Operations;

// Dominios/Storage/Entities/Space.cs
public class SpaceCloud : BaseAbstract
{
    public required Guid UserId { get; set; } // Usuario o inquilino
    public long MaxBytes { get; set; } // Cuota asignada, en bytes
    public long UsedBytes { get; set; } // Bytes consumidos (campo calculado)
    public byte[] RowVersion { get; set; } = default!; // Concurrency token
    public ICollection<FileResourceCloud> FileResourcesCloud { get; set; } =
        new HashSet<FileResourceCloud>();
}
