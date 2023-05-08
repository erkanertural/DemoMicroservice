namespace Core.Services
{
    public interface IUserInfo
    { 
        public long InternalUserId { get; set; }
    //    public long ExternalUserId { get; set; }
        public string Email { get; set; }
    }
}