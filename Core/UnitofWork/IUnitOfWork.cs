namespace Core.UnitofWork
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
         void RollBack();

    }
}
