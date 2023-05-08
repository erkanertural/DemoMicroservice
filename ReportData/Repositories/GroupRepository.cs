using BulKurumsal.Core.Models;
using BulKurumsal.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulKurumsal.Data.Repositories
{
    public class GroupRepository : Repository<Group>, IGroupRepository
    {
        public GroupRepository(ApplicationContext context)
          : base(context)
        { }

        private ApplicationContext ApplicationContext => Context as ApplicationContext;

    }
}
