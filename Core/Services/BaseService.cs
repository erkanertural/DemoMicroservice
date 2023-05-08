using Core.Entities;
using Core.Entities.IEntities;
using Core.Message;
using Core.Message.Request;
using Core.Repositories;
using Core.UnitofWork;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Core.Services
{
    public class BaseService<TEntity> : IBaseService<TEntity> where TEntity : BaseEntity, new()
    {
        private readonly IRepository<TEntity> _repo;
        private readonly IUnitOfWork _unitOfWork;


        public BaseService(IUnitOfWork unitOfWork, IRepository<TEntity> repo)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;

        }

        public IRepository<TEntity> Repo => _repo;

        public IUnitOfWork UnitOfWork => _unitOfWork;






        public async virtual Task<Result<bool>> Remove(long Id)
        {

            try
            {
                TEntity model = await Repo.Get(Id);
                Repo.Remove(model);
                await UnitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Error(ex.Message);
            }
        }
        public async virtual Task<Result<bool>> Remove(BaseRequestT<TEntity> deleteRequest)
        {

            try
            {
                TEntity model = await Repo.Get(deleteRequest.Id);
                Repo.Remove(model);
                await UnitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Error(ex.Message);
            }
        }


        public async virtual Task<Result<TEntity>> Get(long id)
        {
            return await Get(x => x.Id == id);
        }

        public async virtual Task<Result<TEntity>> Get(Expression<Func<TEntity, bool>> exp)
        {
            Result<TEntity> result = new Result<TEntity>();

            if (exp != null)
            {

                if (new TEntity() is ISoftDelete)
                {
                    exp = exp.AndAlso(o => ((ISoftDelete)o).IsDeleted == false);
                }
                TEntity allData = await Repo.Get(exp);

                //allData.ExtaData
                if (allData == null)
                    return result.NoContent();
                return result.Successful(allData);
            }
            return result.NoContent();

            // todo : exception handler / middleware - logging  

        }


        public async virtual Task<Result<ListPagination<T>>> GetListDTO<T>(Expression<Func<TEntity, bool>> exp, Pagination pagination)
        {
             throw new NotImplementedException();
         
        }


        public async virtual Task<Result<ListPagination<TEntity>>> GetList(Expression<Func<TEntity, bool>> exp, IQueryable<TEntity> q, Pagination pagination)
        {
            //todo: pagination veya expression null verip hiçkimse bir tablonun tamamını alamaz...  senaryo : 1 milyon kayıt var
            //  count tablo < 1000 kabul ediyorum... 1000 tane 

            if (pagination == null)
                pagination = new Pagination();
            if (pagination.PageNo < 0 || pagination.PageSize > 1000)
                throw new Exception("Invalid Pagination values. MinPageNo: 0 , MaxPageSize=1000");
            Result<ListPagination<TEntity>> result = new Result<ListPagination<TEntity>>();
            try
            {
                Expression<Func<TEntity, bool>> expression = exp;
                if (exp == null)
                {
                    expression = x => true;

                }

                if (new TEntity() is ISoftDelete)
                {
                    expression = expression.AndAlso(o => ((ISoftDelete)o).IsDeleted == false);
                }
                if (q == null)
                {
                    ListPagination<TEntity> allData = await Repo.GetList(expression, pagination);
                    return result.Successful(allData);
                }
                else
                    return result.Successful(new ListPagination<TEntity>(q.Where(exp).Skip((pagination.PageNo ) * pagination.PageSize).Take(pagination.PageSize).ToList()));

            }
            catch (Exception ex)
            {
                return result.Error(ex.Message);
            }
        }

        public async virtual Task<Result<TEntity>> Create(BaseRequestT<TEntity> createRequest)
        {
            return await Create(ObjectMapper.Mapper.Map<TEntity>(createRequest.Data));
        }
        public async virtual Task<Result<TEntity>> Create(TEntity ent)
        {
            Result<TEntity> result = new Result<TEntity>();
            await Repo.Add(ent);
            return result.Successful(ent);
        }

        public async Task<Result<bool>> CreateBulk(List<TEntity> data)
        {
            try
            {
                await Repo.AddRange(data);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Error(ex.Message);
            }
        }


        public async Task<long> Edit(TEntity ent)
        {
            Result<int> result = new Result<int>();
            TEntity orj = await Repo.Get(ent.Id);
            if (orj != null)
            {
                TEntity last = ent.CloneGeneric(orj, true);
                return Repo.Update(last);
            }
            return -1;
        }

        public async virtual Task<long> Edit(BaseRequestT<TEntity> editRequest)
        {
            // isim = erkan  , erkan ="" -> 
            // yaş = 30      , yas = 0  ->   dk -10     dk 2 lira 
            // trigger faturayı yeniden hesapla 
            // adı değişti / adres  / cep telefonu  ->  kargolanacak ürün 
            return await Edit(editRequest.Data.CloneGeneric<TEntity>(true));

        }


    }
}

