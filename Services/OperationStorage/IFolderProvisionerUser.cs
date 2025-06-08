namespace CloudShield.Services.OperationStorage;

public interface IFolderProvisionerUser
{
    Task EnsureStructureAsync(
        Guid UserId,
        IEnumerable<string> folders,
        CancellationToken ct = default
    );
}
