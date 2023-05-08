using Bul.Core.UnitofWork;
using Bul.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Upsilon.Entities;
using Upsilon.Messages.ViewModels;

namespace Upsilon.Services.Services
{
    public class UserServiceOLD
    {
        /*
        private IUnitOfWork _unitOfWork;
        protected readonly IConfiguration Configuration;
        private IHttpContextAccessor _httpContextAccessor;
        private string Ip = "";
        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            var myEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{myEnv}.json", false)
                .Build();
        }


        public async Task<Result<UserViewModel>> Get(int id)
        {
            Result<UserViewModel> result = new Result<UserViewModel>();
            try
            {
                var data = await _unitOfWork.Users.GetByIdAsync(id);
                result.Data = ObjectMapper.Mapper.Map<UserViewModel>(data);
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }
        public async Task<Result<UserViewModel>> Get(string email)
        {
            Result<UserViewModel> result = new Result<UserViewModel>();
            try
            {
                var data = _unitOfWork.Users.Find(x => x.Email == email).FirstOrDefault();

                if (data == null)
                {
                    result.Success = false;
                    result.Message = "User not found !";
                    return result;
                }

                result.Data = ObjectMapper.Mapper.Map<UserViewModel>(data);
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }

        public async Task<Result<List<UserViewModel>>> GetAll()
        {
            Result<List<UserViewModel>> result = new Result<List<UserViewModel>>();
            try
            {
                var data = (await _unitOfWork.Users.GetAllAsync()).Select(x => ObjectMapper.Mapper.Map<UserViewModel>(x));
                result.Data = data.ToList();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }
        }
        public async Task<Result> Save(UserViewModel viewModel)
        {
            Result result = new Result();
            try
            {
                if (!viewModel.Email.Contains("@bul.com.tr"))
                {
                    result.Success = false;
                    result.Message = "Not a valid email";
                    return result;
                }
                if (await _unitOfWork.Users.SingleOrDefaultAsync(x => x.Email == viewModel.Email) != null)
                {
                    result.Success = false;
                    result.Message = "User already exist !";
                    return result;
                }

                string password = AesOperation.EncryptString(Configuration.GetSection("AesCryptKey").Value, viewModel.Email);
                var duckUser = CreateDuckUser(viewModel.Email.Replace("@bul.com.tr", ""), password, viewModel.FirstName + " " + viewModel.LastName);
                bool duckIsSuccess = duckUser["success"].Value<bool>();

                if (!duckIsSuccess)
                {
                    result.Success = false;
                    result.Message = "User not created";
                    return result;
                }
                else
                {
                    if (viewModel.Id == 0)
                    {


                        string duckId = duckUser["id"].Value<string>();

                        viewModel.WildDuckId = duckId;
                        viewModel.CreatedDate = DateTime.UtcNow;
                        viewModel.IsActive = true;
                        viewModel.Title = "Null";
                        viewModel.DomainId = GetDomainId(viewModel.Email);  // todo : check   user's domain then set domain id  
                                                                            //_unitOfWork.Domains.SingleOrDefaultAsync(x => x.Name == Configuration.GetSection("domain").Value).Result.Id;
                        await _unitOfWork.Users.AddAsync(ObjectMapper.Mapper.Map<User>(viewModel));

                    }
                    else
                    {
                        var model = _unitOfWork.Users.GetByIdAsync(viewModel.Id).Result;
                        model.FirstName = viewModel.FirstName;
                        model.LastName = viewModel.LastName;
                        model.Email = viewModel.Email;
                        model.IsActive = viewModel.IsActive;

                    }
                }
                // todo : repo.update needs
                _unitOfWork.CommitAsync();

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + viewModel.Email + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        private int GetDomainId(string email)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> Save(UserUpdateRequest viewModel)
        {
            Result result = new Result();
            try
            {
                if (!viewModel.email.Contains("@bul.com.tr"))
                {
                    result.Success = false;
                    result.Message = "Not a valid email";
                    return result;
                }

                var model = await _unitOfWork.Users.SingleOrDefaultAsync(x => x.Email == viewModel.email);

                if (model != null)
                {
                    var duckResponse = await UpdateDuckUser(viewModel.name + " " + viewModel.surname, model.WildDuckId);

                    if (duckResponse["success"].Value<bool>())
                    {
                        model.FirstName = viewModel.name;
                        model.LastName = viewModel.surname;
                        model.Email = viewModel.email;

                        // todo: repo.update
                        _unitOfWork.CommitAsync();
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "User not updated";
                        return result;
                    }


                }
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + viewModel.email + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        public Result Delete(int Id)
        {

            Result result = new Result();
            try
            {
                var model = _unitOfWork.Users.GetByIdAsync(Id).Result;

                var duckResponse = DeleteDuckUser(model.WildDuckId).Result;
                bool duckIsSuccess = duckResponse["success"].Value<bool>();

                if (duckIsSuccess)
                {
                    _unitOfWork.Users.Remove(model);
                    _unitOfWork.CommitAsync();
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }

        }
        public async Task<Result> Delete(string email)
        {
            Result result = new Result();
            try
            {
                var model = await _unitOfWork.Users.SingleOrDefaultAsync(x => x.Email == email);

                if (model != null)
                {
                    var duckResponse = await DeleteDuckUser(model.WildDuckId);

                    bool duckIsSuccess = duckResponse["success"].Value<bool>();
                    if (duckIsSuccess)
                    {
                        _unitOfWork.Users.Remove(model);
                        _unitOfWork.CommitAsync();
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "User not deleted";
                        return result;
                    }
                }
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return result;
            }

        }

        public JToken? CreateDuckUser(string username, string password, string nameSurname)
        {

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                UserCreate u = new UserCreate();
                u.username = username + "@" + Configuration.GetSection("domain").Value;
                u.password = password;
                u.name = nameSurname;
                u.address = username + "@" + Configuration.GetSection("domain").Value;
                u.uploadSentMessages = true;
                u.quota = Convert.ToInt32(Configuration.GetSection("quotaByte").Value);

                var json = JsonConvert.SerializeObject(u);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var result = client.PostAsync("/users?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value, data).Result;
                if (result.IsSuccessStatusCode)
                {
                    var resultContent = result.Content.ReadAsStringAsync().Result;
                    // convert to object
                    return JToken.Parse(resultContent);

                }

                return null;
            }
        }
        public async Task<JToken?> UpdateDuckUser(string name, string id)
        {

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "59fc66a03e54454869460e45a");

                object u = new
                {
                    name = name
                };

                var json = JsonConvert.SerializeObject(u);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var result = await client.DeleteAsync("/users/" + id + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);
                if (result.IsSuccessStatusCode)
                {
                    var resultContent = result.Content.ReadAsStringAsync().Result;
                    // convert to object
                    return JToken.Parse(resultContent);

                }

                return null;
            }
        }
        public async Task<JToken?> DeleteDuckUser(string id)
        {

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Configuration.GetSection("WildDuckUrl").Value);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "59fc66a03e54454869460e45a");


                //create json object
                var result = await client.DeleteAsync("/users/" + id + "?accessToken=" + Configuration.GetSection("WildDuckAccessToken").Value);

                if (result.IsSuccessStatusCode)
                {
                    var resultContent = result.Content.ReadAsStringAsync().Result;
                    // convert to object
                    return JToken.Parse(resultContent);

                }

                return null;
            }
        }
        */
    }
}