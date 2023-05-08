using BulKurumsal.Entities;
using Bulmail.Core;
using Bulmail.Core.Services;
using Bulmail.Core.ViewModels;

namespace Upsilon.Services.Services
{
    public class FeedbackFileService : IFeedbackFileService
    {
        private IUnitOfWork _unitOfWork;

        public FeedbackFileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Result<FeedbackFileResultViewModel> Get(long id)
        {
            Result<FeedbackFileResultViewModel> result = new Result<FeedbackFileResultViewModel>();
            try
            {
                var data = _unitOfWork.FeedbackFiles.GetByIdAsync(id).Result;

                result.Data = new FeedbackFileResultViewModel
                {
                    Id = data.Id,
                    FeedbackId = data.FeedbackId,
                    File = data.File
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

        public async Task<Result> Save(FeedbackFileSaveViewModel viewModel)
        {
            Result result = new Result();

            try
            {
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

                if (viewModel.Id == 0)
                {

                    byte[]? fileBytes = null;
                    if (viewModel.File != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await viewModel.File.CopyToAsync(ms);
                            fileBytes = ms.ToArray();
                        }
                    }
                    var model = new FeedbackFile
                    {
                        Id = viewModel.Id,
                        FeedbackId = viewModel.FeedbackId,
                        File = fileBytes

                    };

                    await _unitOfWork.FeedbackFiles.AddAsync(model);
                }

                _unitOfWork.CommitAsync();

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

        public Result Delete(long Id)
        {

            Result result = new Result();
            try
            {
                var model = _unitOfWork.FeedbackFiles.GetByIdAsync(Id).Result;
                _unitOfWork.FeedbackFiles.Remove(model);
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
