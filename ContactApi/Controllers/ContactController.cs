using ContactEntities;
using ContactMessages.Request;
using ContactServices.Services;
using Core.Message.Request;
using Library;
using Microsoft.AspNetCore.Mvc;

namespace ContactAPI.Controllers
{

    public class ContactController : BaseController<Contact>
    {
        private ContactService _contactService;
        public ContactController(ContactService contactService) : base(contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// This method get blockUserId
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        ///     
        ///       {
        ///         "id": 1
        ///         }
        ///     
        ///     
        /// </remarks>
        /// <returns> </returns>        
        /// 
        [HttpPost]
        [Route("Get")]
        public override async Task<Result<Contact>> Get(BaseRequest request)
        {
            return await _contactService.Get(request.Id);
        }



        [HttpPost]
        [Route("AddAddressToContact")]
        public Task<Result<bool>> AddAddressToContact(AddContactDetail request)
        {

            return _contactService.AddContactDetail(request);
        }

        [HttpGet]
        [Route("GetReport")]
        public async Task<Result<ReportDto>> GetReport([FromQuery] string location )
        {

            return  await _contactService.GetReport(location);
        }


        /// <summary>
        ///  This method updates blockUser
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        ///     {
        ///       "data": {
        ///         "id": 0,
        ///         "userMail": "example@armaara.com",
        ///         "userId": 0,
        ///         "isDeleted": false
        ///       }
        ///     }
        ///     
        /// </remarks>
        /// <returns> </returns>
        [HttpPost]
        [Route("Edit")]
        public override Task<IActionResult> Edit(BaseRequestT<Contact> editRequest)
        {
            return base.Edit(editRequest);
        }
        /// <summary>
        /// This method get blockUserId
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        ///     
        ///       {
        ///         "id": 1
        ///         }
        ///     
        ///     
        /// </remarks>
        /// <returns> </returns>  
        /// 

        [HttpPost]
        [Route("Remove")]
        public override Task<IActionResult> Remove(BaseRequest request)
        {
            return base.Remove(request);
        }
    }
}
