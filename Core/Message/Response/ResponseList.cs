using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Message.Response
{
    public class ResponseList<T>
    {
        public List<T> Data { get; set; }

        public Pagination Pagination { get; set; }
    }
}
