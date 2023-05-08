using Library;

namespace Core.Message.Request
{
    public class RequestPagination:BaseRequest
    {

        public RequestPagination()
        {
           

        }
  
      
        public Pagination? Pagination { get; set; }

    }
}