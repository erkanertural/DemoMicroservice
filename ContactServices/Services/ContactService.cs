using ContactEntities;
using ContactMessages.Request;
using ContactServices.Validations;
using Core.Message;
using Core.Message.Request;
using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using FluentValidation.Results;
using Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace ContactServices.Services
{
    public class ContactService : BaseService<Contact>
    {
        protected readonly IConfiguration _configuration;
        private readonly IRepository<Contact> _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ContactDetail> _repoContactDetail;
        public ContactService(IRepository<Contact> repo, IUnitOfWork unitOfWork, IRepository<ContactDetail> repoContactDetail) : base(unitOfWork, repo)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _repoContactDetail = repoContactDetail;
        }

        public override Task<Result<ListPagination<Contact>>> GetList(Expression<Func<Contact, bool>> exp, IQueryable<Contact> q, Pagination pagination)
        {
            var d = base.Repo.Query.Include(o => o.ContactDetails).AsQueryable();
            return base.GetList(exp, d, pagination);
        }

        public override async Task<Result<ListPagination<T>>> GetListDTO<T>(Expression<Func<Contact, bool>> exp, Pagination pagination)
        {
            var d = base.Repo.Query.Include(o => o.ContactDetails).AsQueryable();
            var result = await GetList(exp, d, pagination);
            ListPagination<T> l = new ListPagination<T>();
            foreach (var item in result.Data)
            {
                l.Add(ObjectMapper.Mapper.Map<T>(item));
            }
            return await Task.FromResult(new Result<ListPagination<T>>(l));
        }
        public override async Task<Result<Contact>> Create(Contact request)
        {
            AddContactValidator validations = new AddContactValidator();
            ValidationResult resultV = validations.Validate(request);
            if (resultV.IsValid)
            {
                Result<Contact> result = await base.Create(request);
                await UnitOfWork.SaveChangesAsync();
                return result;
            }
            string allMessages = resultV.ToString("; ");
            throw new Exception(allMessages);

        }
        public async Task<Result<bool>> AddContactDetail(AddContactDetail request)
        {
            AddContactAddressValidator validations = new AddContactAddressValidator();
            ValidationResult resultV = validations.Validate(request);
            if (resultV.IsValid)
            {
                var d = await _repo.Get(request.ContactId);
                var m = ObjectMapper.Mapper.Map<ContactDetail>(request);

                d.AddContactDetail(m);

                await UnitOfWork.SaveChangesAsync();
                return await Task.FromResult(new Result<bool>());
            }
            string allMessages = resultV.ToString("; ");
            throw new Exception(allMessages);

        }
        public override async Task<long> Edit(BaseRequestT<Contact> editRequest)
        {
            UpdateBlockUserValidator v = new UpdateBlockUserValidator();
            ValidationResult resultVal = v.Validate(editRequest.Data);
            if (resultVal.IsValid)
            {
                long result = await base.Edit(editRequest);
                await UnitOfWork.SaveChangesAsync();

                return 1;
            }
            string allMessages = resultVal.ToString("; ");
            throw new Exception(allMessages);
        }



        public async Task<Result<bool>> RemoveContactDetail(RemoveContactDetail removeContactDetail)
        {
            //AddContactAddressValidator v = new AddContactAddressValidator();
            //ValidationResult resultVal = v.Validate(deleteRequest);
            if (removeContactDetail.ContactId > 0)
            {
                Contact model = Repo.Query.Include(o => o.ContactDetails).FirstOrDefault(o => o.Id == removeContactDetail.ContactId);
                model.RemoveContactDetail(removeContactDetail.ContactDetailId);
                await UnitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            return Result.Success();
            //  string allMessages = resultVal.ToString(";");
            //  throw new Exception(allMessages);
        }

        public async Task<Result<ReportDto>> GetReport(string location)
        {
            List<ContactDetail> locList = _repoContactDetail.Query.Where(o => o.Type == ContactLibrary.Enums.ContactType.Location && o.Context == location).ToList();
            var contacts = locList.Select(o => o.ContactId).Distinct().ToList();
            int locCountTel = _repoContactDetail.Query.Count(o => o.Type == ContactLibrary.Enums.ContactType.Telephone && contacts.Contains(o.ContactId));
            return await Task.FromResult(new Result<ReportDto>(new ReportDto { ContactCount = contacts.Count, Location = location, CountOfContactDetailTelephone = locCountTel }));
        }
    }
}