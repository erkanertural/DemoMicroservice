using Core.DBContext;
using Microsoft.EntityFrameworkCore;

namespace Core.UnitofWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        public UnitOfWork(IDBContext context)
        {
            _context = context as DbContext;
        }


        public void RollBack()
        {
             _context.Database.RollbackTransaction();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        
    }
}
