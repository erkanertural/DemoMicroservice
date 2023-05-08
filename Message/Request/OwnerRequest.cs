using BulKurumsal.Core.Message.Request;
using BulKurumsal.Entities;
using BulLibrary;

namespace Message.Request
{

    /// <summary>
    /// bla bla test
    /// </summary>

    public class OwnerRequest : BaseRequest, IOwner
    {
        public OwnerRequest()
        {
            this.SetAllPropertiesToDefaultValue();
        }

        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string OptionalPhone { get; set; }

        public int deneme { get; set; }

    }
}
