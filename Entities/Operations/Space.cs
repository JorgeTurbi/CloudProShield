

using CloudShield.Commons;

namespace CloudShield.Entities.Operations;
// Dominios/Storage/Entities/Space.cs
public class Space : BaseAbstract
{

    public required Guid CustomerId { get; set; }     // Usuario o inquilino
    public long MaxBytes { get; set; }       // Cuota asignada, en bytes
    public long UsedBytes { get; set; }      // Bytes consumidos (campo calculado)
    public byte[] RowVersion { get; set; } = default!;   // Concurrency token
    public ICollection<FileResource> FileResources { get; set; } = new HashSet<FileResource>();

}
