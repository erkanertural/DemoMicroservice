using ContactEntities;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace ContactEntities
{
    //  [SwaggerSchema(ReadOnly = true)]

    [NotMapped]
    public class NoEntity : BaseEntity
    {
       

    }
}
