namespace Commons.Utils;
public interface ISaveServices
{
    Task<bool> Save(CancellationToken cancellationToken=default);
}