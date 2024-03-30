namespace AuthenticationService.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}