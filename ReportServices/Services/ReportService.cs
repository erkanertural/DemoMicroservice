using ContactApiClient;
using ContactMessages.Request;
using Core.Message.Request;
using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using Library;
using Library.RabbitMQ;
using Microsoft.Extensions.Configuration;
using ReportEntities;

namespace ReportServices.Services
{
    public class ReportService : BaseService<Report>
    {
        protected readonly IConfiguration _configuration;
        private IContactApiClient _contactApiClient;
        private readonly IQueuePublisher _publisher;
        public ReportService(IRepository<Report> repo, IUnitOfWork unitOfWork, IQueuePublisher publisher, IContactApiClient contactApiClient) : base(unitOfWork, repo)
        {
            _contactApiClient = contactApiClient;
            _publisher = publisher;
            _publisher.InitRabbitMQ();
        }
        public override  async Task<Result<Report>> Create(Report ent)
        {
            Result<Report> result= await base.Create(ent);
            await UnitOfWork.SaveChangesAsync();
            return result.Successful();
        }
        public async Task<Result<bool>> PrepareReport(string location)
        {
            Result<ReportDto> resultReportDetail = await _contactApiClient.GetReportDetail(location);
            var resultCreate = await Create(new Report { ReportDate = DateTime.Now, FilePath = "", TaskStatus = Report.TaskStatusType.Preparing });
            resultReportDetail.Data.ReportId = resultCreate.Data.Id;
            _publisher.PublishMessage(resultReportDetail.Data);
            //_publisher.PublishMessage(new ReportDto { ContactCount=1, CountOfContactDetailTelephone=13, Location=location });
            return new Result<bool>(true);

        }
        public async Task<Result<Report>> GetReportDetail(long id)
        {

            Report report = await Repo.Get(id);
            return new Result<Report>(report);

        }


    }
}