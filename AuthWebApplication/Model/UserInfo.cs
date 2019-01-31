using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthWebApp.Model
{
    public class UserInfo
    {        
        public string NameIdentifier { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public string NickName{ get; set; }        

        public string Role { get; set; }
        public string RoleDescription { get; set; }

        public string ApiCallStatus { get; set; }
    }
}
