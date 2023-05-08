using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactEntities;

namespace ContactServices.Services
{
    public class NoEntityService : BaseService<NoEntity>
    {
        public NoEntityService(IUnitOfWork unitOfWork, IRepository<NoEntity> repo) : base(unitOfWork, repo)
        {
        }
    }
}
