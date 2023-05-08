using BulKurumsal.Entities;
using Bulmail.Core;
using Bulmail.Core.Helpers;
using Bulmail.Core.Services;
using Bulmail.Core.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities.Encoders;
using Serilog;
using System.Reflection;

namespace Upsilon.Services.Services
{
    public class FeedbackService : IFeedbackService
    {
        private IUnitOfWork _unitOfWork;
        private IFeedbackFileService _feedbackFileService;
        private IHttpContextAccessor _httpContextAccessor;
        protected readonly IConfiguration Configuration;
        private string Ip = "";

        public FeedbackService(IUnitOfWork unitOfWork, IFeedbackFileService feedbackFileService, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _feedbackFileService = feedbackFileService;
            _httpContextAccessor = httpContextAccessor;
            var myEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{myEnv}.json", false)
                .Build();
        }

        public async Task<Result<FeedbackResultViewModel>> Get(int id)
        {
            Result<FeedbackResultViewModel> result = new Result<FeedbackResultViewModel>();
            try
            {
                var data = await _unitOfWork.Feedbacks.GetByIdAsync(id);
                var file = await _unitOfWork.FeedbackFiles.SingleOrDefaultAsync(x => x.FeedbackId == id);
                result.Data = new FeedbackResultViewModel
                {
                    Id = data.Id,
                    CreatedDate = data.CreatedDate,
                    Message = data.Message,
                    Rate = data.Rate,
                    User = await _unitOfWork.Users.GetByIdAsync(data.UserId),
                    File = file != null ? ByteArrayToBase64(file.File) : null
                };
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

        public async Task<Result<string>> GetAll(int pageNumber)
        {

            Result<string> result = new Result<string>();
            try
            {
                int pageSize = 20;
                var all = await _unitOfWork.Feedbacks.GetAllAsync();
                var data = all.Skip(pageSize * (pageNumber - 1)).Take(pageSize).Select(async x => new FeedbackResultViewModel
                {
                    Id = x.Id,
                    CreatedDate = x.CreatedDate,
                    Message = x.Message,
                    Rate = x.Rate,
                    User = _unitOfWork.Users.GetByIdAsync(x.UserId).Result
                });

                int totalPages = (int)Math.Ceiling(all.Count() / (double)pageSize);
                var paginateData = new JObject();

                paginateData["PageNumber"] = pageNumber;
                paginateData["TotalPages"] = totalPages;
                paginateData["PageSize"] = pageSize;
                paginateData["TotalCount"] = data.Count();

                result.Data = JsonConvert.SerializeObject(new JObject { ["PaginateData"] = paginateData, ["Feedbacks"] = JToken.Parse(JsonConvert.SerializeObject(data)) });
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

        public async Task<Result> Save(FeedbackSaveViewModel viewModel, string email)
        {
            Result result = new Result();
            Feedback model = new Feedback();
            try
            {
                byte[]? fileBytes = null;
                if (viewModel.File != null)
                {
                    var validPathList = new string[] { ".png", ".jpg", ".jpeg" };
                    if (viewModel.File.Length > 5000000)
                    {
                        result.Success = false;
                        result.Message = "File is greater than 5 MB!";
                        return result;
                    }

                    if (!validPathList.Contains(Path.GetExtension(viewModel.File.FileName).ToLower()))
                    {
                        result.Success = false;
                        result.Message = "File extension does not supported!";
                        return result;
                    }
                }

                var user = _unitOfWork.Users.Find(x => x.Email == email).FirstOrDefault();

                if (user == null)
                {
                    result.Success = false;
                    result.Message = "User not found!";
                    return result;
                }


                if (viewModel.Id == 0)
                {

                    model = new Feedback
                    {
                        Id = (int)viewModel.Id,
                        Message = viewModel.Message,
                        UserId = (int)user.Id,
                        Rate = viewModel.Rate
                    };

                    model.CreatedDate = DateTime.UtcNow;
                    await _unitOfWork.Feedbacks.AddAsync(model);

                }
                else
                {
                    model = await _unitOfWork.Feedbacks.GetByIdAsync((int)viewModel.Id);
                    model.Rate = viewModel.Rate;
                    model.Message = viewModel.Message;

                }
                if (viewModel.File != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        await viewModel.File.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }
                }

                _unitOfWork.CommitAsync();
                await _feedbackFileService.Save(new FeedbackFileSaveViewModel { Id = 0, File = viewModel.File, FeedbackId = model.Id });
                await FeedbackSendMail(new FeedbackSendViewModel { FileName = viewModel.File != null ? viewModel.File.FileName : "FeedBackFile.png", Body = model.Message, Subject = "Mail Feedback: " + email + " | " + viewModel.Rate + " Yıldız", File = fileBytes });
                result.Message = "Feedback saved successfully!";
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

        public async Task<Result> Delete(int Id)
        {

            Result result = new Result();
            try
            {
                var model = await _unitOfWork.Feedbacks.GetByIdAsync(Id);
                _unitOfWork.Feedbacks.Remove(model);
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

        public async Task<Result> FeedbackSendMail(FeedbackSendViewModel vm)
        {
            var result = new Result();
            try
            {
                var hostname = _httpContextAccessor.HttpContext.Request.Host.Value;
                string sender = "";
                string receiver = "";

                string test = '"' + "Mail Feedback" + '"' + "<" + Configuration.GetSection("FeedbackMail:email").Value + ">";
                sender = Configuration.GetSection("FeedbackMail:email").Value;
                receiver = Configuration.GetSection("FeedbackMail:email").Value;

                string password = GetPassword(Configuration.GetSection("FeedbackMail:email").Value.Replace("armaara.com", "bul.com.tr"));
                string smtpServer = Configuration.GetSection("MailSettings:smtpServer").Value;
                int smtpPort = Convert.ToInt32(Configuration.GetSection("MailSettings:smtpPort").Value);

                // create email message
                var mail = new MimeMessage();
                mail.From.Add(MailboxAddress.Parse(sender));

                // add receivers
                mail.To.Add(MailboxAddress.Parse(receiver));

                mail.Subject = vm.Subject;
                var builder = new BodyBuilder();
                builder.HtmlBody = vm.Body;

                if (vm.File != null)
                {
                    builder.Attachments.Add(vm.FileName, vm.File);
                    var contentId = MimeKit.Utils.MimeUtils.GenerateMessageId();
                    builder.Attachments[0].ContentId = contentId;
                }

                mail.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(Configuration.GetSection("FeedbackMail:email").Value, password);
                await smtp.SendAsync(mail);
                await smtp.DisconnectAsync(true);

                result.Success = true;
                result.Message = "Mail sent successfuly";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    Ip = _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"];
                Log.Error("User= " + Configuration.GetSection("DriveMail:email").Value + " | " + " Method= " + GetType().Name + "/" + MethodBase.GetCurrentMethod().Name + " | " + " Log= " + ex.Message + " | " + " Ip= " + Ip);
                return result;
            }
        }


        public string ByteArrayToBase64(byte[] byteArray)
        {
            return "data:image/jpeg;base64," + Base64.ToBase64String(byteArray);

        }

        public string GetPassword(string email)
        {
            return AesOperation.EncryptString(Configuration.GetSection("AesCryptKey").Value, email);
        }
    }
}
