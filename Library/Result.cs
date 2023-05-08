using System.Net;

namespace Library
{
    public class Result
    {
        public static Result<bool> Success(string message = "")
        {
            Result<bool> result = new Result<bool>()
            {

                HttpStatus = HttpStatusCode.OK,
                Success = true,
                Data = true
            };
            result.Messages.Add(message);
            return result;
        }


        public static Result<bool> Error(string message = "")
        {
            Result<bool> result = new Result<bool>()
            {

                HttpStatus = HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
            result.Messages.Add(message);
            return result;
        }

    }
    public class Result<T>
    {
        public Result(T data)
        {
            Data = data;
        }
        public Result()
        {
            Messages = new List<string>();
        }
     
        public bool Success { get; set; }
        public List<string> Messages { get; set; }

        public HttpStatusCode HttpStatus { get; set; }

        public T Data { get; set; }

       
            
                        
        public Result<T> Error(string message = "")
        {

            HttpStatus = HttpStatusCode.InternalServerError;
            Success = false;
            Data = default(T);

            Messages.Add(message);
            return this;
        }

        public Result<T> Successful(T data)
        {
            Data = data;
            HttpStatus = HttpStatusCode.OK;
            Success = true;
            return this;
        }
        public Result<T> NoContent(string message = "")
        {

            Data = default;
            HttpStatus = HttpStatusCode.NoContent; 
            Success = false;
            return this;

        }
    }




}
