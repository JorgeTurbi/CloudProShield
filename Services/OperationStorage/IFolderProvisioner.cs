namespace CloudShield.Services.OperationStorage;

public interface IFolderProvisioner
{
    Task EnsureStructureAsync(
        Guid customerId,
        IEnumerable<string> folders,
        CancellationToken ct = default
    );
}
