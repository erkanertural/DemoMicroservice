using Core.Message;
using Library;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Core.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        public void Include(Type type);
        public DbSet<TEntity> Query { get; }
        Task<TEntity> Get(Expression<Func<TEntity, bool>> predicate, IQueryable queryable = null);
        Task<TEntity> Get(long id);
        Task<ListPagination<TEntity>> GetList(Expression<Func<TEntity, bool>> predicate, Pagination pagination);

        //   Task<ListPagination<TView>> GetListQuery(IQueryable queryable);


        Task Add(TEntity entity);
        Task AddRange(List<TEntity> entities);

        void Remove(TEntity entity);
        int Update(TEntity entity);
    }
}
