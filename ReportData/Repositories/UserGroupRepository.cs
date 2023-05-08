using BulKurumsal.Core.Models;
using BulKurumsal.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulKurumsal.Data.Repositories
{
    public class UserGroupRepository : Repository<UserGroup>, IUserGroupRepository
    {
        public UserGroupRepository(ApplicationContext context)
          : base(context)
        { }

        private ApplicationContext ApplicationContext => Context as ApplicationContext;

    }
}
