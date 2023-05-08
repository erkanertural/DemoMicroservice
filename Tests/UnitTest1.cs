using ContactApiClient;
using ContactData;
using ContactEntities;
using ContactEntities.Mapping;
using ContactMessages.Request;
using ContactServices.Services;
using Core.DBContext;
using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using Library;
using Library.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Moq;
using Refit;
using ReportEntities;
using ReportServices.Services;
using static ContactLibrary.Enums;

namespace TestProjectMS
{

    public class UnitTest1
    {
        private readonly Mock<IRepository<ContactDetail>> _rcd;
        private readonly Mock<IRepository<Contact>> _r;
        private readonly ContactService c;
        private readonly Mock<IUnitOfWork> _unitOf;
        private readonly Mock<ContactContext> _ctx;
        public UnitTest1()
        {

            _ctx = new Mock<ContactContext>(new DbContextOptionsBuilder().Options, new ConfigurationService().GetConfiguration());
            _r = new Mock<IRepository<Contact>>();
            _rcd = new Mock<IRepository<ContactDetail>>(_ctx);
            _unitOf = new Mock<IUnitOfWork>();
            c = new ContactService(_r.Object, _unitOf.Object,_rcd.Object);
        }

        [TestMethod]
        public void CreateUser()
        {

            Assert.IsTrue(true == true);

        }
        [TestMethod]
        public void CreateContact()
        {
            //  _c.Setup(o => o);

            var r = c.Create(new Contact { Company = "g", Name = "erkan", SurName = "ff" }).Result;

            Assert.IsNotNull(r);


        }
    }

    [TestClass]
    public class UnitTest2
    {

        private readonly Repository<Contact> _r;
        private readonly Repository<ContactDetail> _rcd;
        private readonly Repository<Report> _rp;
        private readonly ContactService c;
        private readonly ReportService rs;
        private readonly IUnitOfWork _unitOf;
        private readonly IDBContext _ctx;
        public UnitTest2()
        {
            IContactApiClient api = RestService.For<IContactApiClient>("https://localhost:5046/api");
            _ctx = new ContactContext(new DbContextOptionsBuilder().Options, new ConfigurationService().GetConfiguration());
            _r = new Repository<Contact>(_ctx);
            _rcd = new Repository<ContactDetail>(_ctx);
            _unitOf = new UnitOfWork(_ctx);
            c = new ContactService(_r, _unitOf,_rcd);
            rs = new ReportService(_rp, _unitOf, new QueuePublisher(), api);
            ObjectMapper.Register<MappingProfile>();
        }

        [TestMethod]
        public void CreateContact()
        {


            var r = c.Create(new Contact { Company = "Firma", Name = "Erkan", SurName = "Ertural" }).Result;

            Assert.IsNotNull(r);


        }
        [TestMethod]
        public void AddContactDetail()
        {


            var r = c.AddContactDetail(new AddContactDetail { ContactId = 1, Context = "Gebze", Type = ContactType.Location }).Result;

            Assert.IsNotNull(r);


        }
        [TestMethod]
        public void RemoveContact()
        {
            var r = c.Remove(1).Result;
            Assert.IsNotNull(r);
        }
        [TestMethod]
        public void GetContacts()
        {
            var r = c.GetListDTO<ContactDetailsDto>(o => true, null).Result;
            Assert.IsTrue(r.Data.Count > 0);
        }
        [TestMethod]
        public void RemoveContactDetail()
        {
            var r = c.RemoveContactDetail(new RemoveContactDetail { ContactDetailId = 1, ContactId = 1 }).Result;
            Assert.IsNotNull(r);
        }

        [TestMethod]
        public void Publish()
        {
         
                var d = rs.PrepareReport("Gebze").Result;
         

        }
    }
}