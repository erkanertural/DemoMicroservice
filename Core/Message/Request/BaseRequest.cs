using Library;

namespace Core.Message.Request
{
    public class BaseRequest
    {

        public BaseRequest()
        {
           

        }
  
        public virtual long Id { get; set; }
   
        public string? Description { get; set; }

        public Pagination? Pagination { get; set; }

    }
}