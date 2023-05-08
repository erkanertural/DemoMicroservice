namespace Core.Message.Request
{


    public class BaseRequestT<T> : BaseRequest where T : class, new()
    {
        public BaseRequestT(BaseRequest request)
        {

            Id = request.Id;

        }
        public BaseRequestT()
        {

        }
        /// <summary>
        /// Model , View , Entity nesnesi controller'a gönderilebilir.
        /// </summary>
        public T Data { get; set; }

    }
}