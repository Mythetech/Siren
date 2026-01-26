namespace Siren.Components.Infrastructure;

public interface IAppAsyncInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
