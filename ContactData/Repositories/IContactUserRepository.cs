
using ContactEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories;

namespace ContactData.Repositories
{
    public interface IContactUserRepository : IRepository<Contact>
    {

    }
}
