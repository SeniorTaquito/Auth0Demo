using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthWebApp.Model
{
    public class RefreshTokenRequest
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string refresh_token { get; set; }
    }
}
