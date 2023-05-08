using Core.Entities;
using Core.Entities.IEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactMessages.Request
{
    public class AddContact 
    {
        public string Name { get; set; }
        public string SurName { get; set; }
        public string Company { get; set; }

    }
}