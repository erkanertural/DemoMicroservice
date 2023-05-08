using ContactMessages.Request;
using Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ReportEntities;
using ReportServices.Services;

namespace ReportAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController
    {
        private readonly ReportService _reportService;
        public ReportController(ReportService report)
        {
            _reportService = report;
        }

        [HttpGet]
        [Route("GetReport/{id}")]
        public async Task<Result<Report>> GetReportDetail([FromRoute] long id)
        {

            return await _reportService.GetReportDetail(id);
        }
        [HttpGet]
        [Route("Create")]
        public async Task<Result<bool>> PrepareReport([FromQuery] string location)
        {

            return await _reportService.PrepareReport(location);
        }
    }
}
