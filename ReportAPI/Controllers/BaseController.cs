using Core.Entities;
using Core.Message.Request;
using Core.Repositories;
using Core.Services;
using Library;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace ContactAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("MyPolicy")]
    public class BaseController<TEntity> : Controller where TEntity : BaseEntity, new()
    {
        IBaseService<TEntity> _service;
        Repository _repo;

        public string UserWildduckId { get; private set; }


        public BaseController(IBaseService<TEntity> service)
        {
       
            _service = service;
            
         
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
       
            base.OnActionExecuting(context);
        }

        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            return base.OnActionExecutionAsync(context, next);
        }

        /// <summary>
        ///  This method returns the data corresponding to the id you gave.
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        /// 
        ///     {
        ///     "id": 10
        ///     }
        ///     
        /// </remarks>
        /// <returns> </returns>
        [HttpPost]
        [Route("Get")]
        public virtual async Task<Result<TEntity>> Get(BaseRequest request)
        {
            return (await _service.Get(request.Id));
        }

        /// <summary>
        ///  This method returns a paginated list of the data you want
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        ///     {
        ///       "pagination": {
        ///         "pageNo": 0,
        ///         "pageSize": 20,
        ///         "hasMoreData": true,
        ///         "orderByDescending": true
        ///       }
        ///     }
        ///     
        /// </remarks>
        /// <returns> </returns>
        [HttpPost]
        [Route("GetList")]
        public async virtual Task<Result<ListPagination<TEntity>>> GetList(BaseRequest request)
        {
            return (await _service.GetList(o=> true,null, request.Pagination));
        }


        [HttpPost]
        [Route("Create")]
        public virtual async Task<IActionResult> Create(BaseRequestT<TEntity> request)
        {
           
            request.Data.Id = 0; // fix to mistakenly  given
            Result<TEntity> resp = await _service.Create(request);
            if(resp != null)
                return resp?.ToActionResult();
            return BadRequest();
        }
        [HttpPut]
        [Route("Edit")]
        public virtual async Task<IActionResult> Edit(BaseRequestT<TEntity> editRequest)
        {
            long entityResult = await _service.Edit(editRequest);

            return entityResult > -1 ? new Result<long>().Successful(entityResult).ToActionResult() : new Result<int>().NoContent().ToActionResult();

        }
        /// <summary>
        /// This method deletes the data corresponding to the id you gave
        /// </summary>
           /// <remarks>
        /// Sample of usage
        /// 
        /// 
        ///     {
        ///     "id": 10
        ///     }
        ///     
        /// </remarks>
        /// <returns> </returns>
        [HttpDelete]
        [Route("Remove")]
        public virtual async Task<IActionResult> Remove(BaseRequest request)
        {
            var result = await _service.Remove(request.Id);
            return result.Success ? Ok() : BadRequest(result);
        }
    }


}
