using CloudShield.Commons;

namespace CloudShield.Entities.Operations;
// Dominios/Storage/Entities/FileResource.cs
public class FileResourceCloud : BaseAbstract
{
    public Guid SpaceId { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string RelativePath { get; set; } = default!; // p. ej. "images/logo.png"
    public long SizeBytes { get; set; }
    public virtual SpaceCloud? SpaceCloud { get; set; }
}
