using ContactMessages.Request;
using Library;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace ContactApiClient
{
    public interface IContactApiClient
    {
        [Get("/Contact/GetReport")]
        Task<Result<ReportDto>> GetReportDetail(string location );
    
    }
}