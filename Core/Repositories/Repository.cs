using Core.DBContext;
using Core.Entities;
using Core.Message;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;

namespace Core.Repositories
{
    public class Repository
    {
        private readonly DbContext Context;

        public Repository(IDBContext context)
        {
            Context = context as DbContext;

            // dbset instance 
        }
        public DbSet<T> Set<T>() where T : BaseEntity
        {
            return Context.Set<T>();
        }


    }
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity, new()
    {
        protected readonly DbContext Context;

        public Repository(IDBContext context)
        {
            Context = context as DbContext;
            // dbset instance 
        }


        public async Task Add(TEntity entity)
        {
        
            if (entity.Id != 0)
                throw new Exception("Entity Id must be 0");
            await Context.Set<TEntity>().AddAsync(entity);
        }

        public async Task AddRange(List<TEntity> data)
        {
            foreach (var item in data)
            {
             
                item.Id = 0;
            }
            await Context.Set<TEntity>().AddRangeAsync(data);
        }

        public async Task<ListPagination<TEntity>> GetList(Expression<Func<TEntity, bool>> predicate, Pagination pagination = null)
        {
            if (pagination == null)
            {
                List<TEntity> list = await Context.Set<TEntity>().Where(predicate).ToListAsync();
                return new ListPagination<TEntity>(list);
            }
            else
            {
                List<TEntity> list = await Context.Set<TEntity>().Where(predicate).Skip(pagination.PageSize * ((pagination.PageNo + 1) - 1)).Take(pagination.PageSize).ToListAsync();
              
                pagination.TotalPage = list.Count / pagination.PageSize;

                return new ListPagination<TEntity>(list);
                //todo: 1000 kayıttan fazlasına müsade edilmeyecek 
            }
        }


        public async Task<TEntity> Get(long id)
        {
            TEntity ent = await Context.Set<TEntity>().FindAsync(id);
     
            return ent;
        }
        public void Include(Type type)
        {


        }
        public async Task<TEntity> Get(Expression<Func<TEntity, bool>> predicate, IQueryable queryable = null)
        {
            return await Context.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }
        public void Remove(TEntity entity)
        {
            // await Task.Run (()=> { Context.Set<TEntity>().Remove(entity); });      // if you need async you can use , try it :))
            Context.Set<TEntity>().Remove(entity);
        }
        public int Update(TEntity entity)
        {

            if (entity.Id == 0)
                throw new Exception("Id cannot be 0 (zero)");
            EntityEntry<TEntity> ent = Context.Set<TEntity>().Update(entity);
            return 1; // fake affected row


            // todo:  topic of affectedRows will be searching
        }
        public DbSet<TEntity> Query => Context.Set<TEntity>();

    }
}
