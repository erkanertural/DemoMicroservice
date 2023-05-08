using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class JwtAuthViewModel
    {
        public string Email { get; set; }
        public string Nbf { get; set; }
        public string Exp { get; set; }
        public string Iat { get; set; }
    }
}
