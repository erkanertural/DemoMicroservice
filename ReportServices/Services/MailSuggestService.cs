using BulKurumsal.Entities;
using Bulmail.Core;
using Bulmail.Core.Services;
using Bulmail.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Reflection;

namespace Upsilon.Services.Services
{
    public class MailSuggestService : IMailSuggestService
    {
        private IUnitOfWork _unitOfWork;
        private IHttpContextAccessor _httpContextAccessor;
        private string Ip = "";

        public MailSuggestService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public Result Save(MailSuggestViewModel viewModel)
        {
            Result result = new Result();
            var model = ObjectMapper.Mapper.Map<MailSuggest>(viewModel);
            try
            {
                var dbData = _unitOfWork.MailSuggests.SingleOrDefaultAsync(x => x.Sender == viewModel.Sender && x.Receiver == viewModel.Receiver).Result;

                if (dbData == null)
                {
                    viewModel.LastSentDate = DateTime.UtcNow;
                    viewModel.Count = 1;
                    model = ObjectMapper.Mapper.Map<MailSuggest>(viewModel);
                    _unitOfWork.MailSuggests.AddAsync(model).Wait();
                }
                else
                {
                    dbData.IsActive = dbData.IsActive;
                    dbData.Count++;
                    dbData.LastSentDate = DateTime.UtcNow;
                }
                _unitOfWork.CommitAsync();

                result.Message = "Saved successfully!";
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

        public async Task<Result<IEnumerable<object>>> GetEmails(string email)
        {
            Result<IEnumerable<object>> result = new Result<IEnumerable<object>>();
            try
            {
                var emails = _unitOfWork.MailSuggests.Find(x => x.Sender == email).Select(x => new { x.Id, x.IsActive, x.Receiver, x.Count, x.LastSentDate }).OrderByDescending(x => x.Count).ThenBy(x => x.Receiver);
                result.Data = emails;
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
                Log.Error("User= " + email + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }

        public async Task<Result> Delete(int Id)
        {

            Result result = new Result();
            try
            {
                var model = await _unitOfWork.MailSuggests.GetByIdAsync(Id);
                _unitOfWork.MailSuggests.Remove(model);
                _unitOfWork.CommitAsync();

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
    }
}
