using Core.Entities;
using Core.Message;
using Core.Message.Request;
using Core.Repositories;
using Core.UnitofWork;
using Library;
using System.Linq.Expressions;

namespace Core.Services
{
    public interface IBaseService<TEntity> where TEntity : BaseEntity, new()
    {

        public IRepository<TEntity> Repo { get; }
        public IUnitOfWork UnitOfWork { get; }
 
        public Task<Result<TEntity>> Get(Expression<Func<TEntity, bool>> predicate);
        public Task<Result<TEntity>> Get(long id);

        public Task<Result<ListPagination<T>>> GetListDTO<T>(Expression<Func<TEntity, bool>> exp,  Pagination pagination);
        public Task<Result<ListPagination<TEntity>>> GetList(Expression<Func<TEntity, bool>> predicate,IQueryable<TEntity> q, Pagination pagination = null);
        public Task<Result<TEntity>> Create(TEntity entity);
        public Task<Result<TEntity>> Create(BaseRequestT<TEntity> createRequest);
        public Task<Result<bool>> CreateBulk(List<TEntity> data);
        public Task<long> Edit(TEntity entity);
        public Task<long> Edit(BaseRequestT<TEntity> createRequest);
        public Task<Result<bool>> Remove(long Id);
    }
}
