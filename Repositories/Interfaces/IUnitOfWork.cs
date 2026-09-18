namespace StudentPortalAPI.Repositories;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
