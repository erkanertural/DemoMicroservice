using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public interface IPaginationList
    {

        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNo { get; set; }

        public int TotalPage { get; set; }
        public bool HasMoreData { get; set; }
    }
    public class ListPagination<T> : List<T>, IPaginationList
    {
        public ListPagination()
        {

        }
        public ListPagination(IEnumerable<T> collection):base(collection)
        {

        }

        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNo { get; set; }
        public bool HasMoreData { get; set; }
        public int TotalPage { get; set; }
    }
}
