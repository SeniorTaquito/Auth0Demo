using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using GameLib.Models;
using AuthWebApp.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthWebApp.Controllers
{
    public class HomeController : Controller
    {
        private HttpClient client = new HttpClient();
        private readonly IConfiguration _configuration;
        UserInfo userInfo = new UserInfo();

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // If the user is authenticated, then this is how you can get the access_token and id_token
            if (User.Identity.IsAuthenticated)
            {
                userInfo = GetUserData();
            }

            return View(userInfo);
        }

        public async Task<IActionResult> GetApi()
        {
            await AddTokenToClient();
            string url = "https://localhost:44374/api/game";
            var result = await client.GetAsync(url);                       
            userInfo = GetUserData(result);
            return View("Index", userInfo);
        }

        public async Task<IActionResult> PostApi()
        {
            await AddTokenToClient();
            string url = "https://localhost:44374/api/game";
            var result = await client.PostAsJsonAsync(url, new Game { Title = "Test Game", Cost = 59.99, Genre = "puzzle", Id = 6 });
            userInfo = GetUserData(result);            

            return View("Index", userInfo);
        }

        public async Task<IActionResult> DeleteApi(int id)
        {
            await AddTokenToClient();
            string url = $"https://localhost:44374/api/game/{id}";
            var result = await client.DeleteAsync(url);
            userInfo = GetUserData(result);

            return View("Index", userInfo);            
        }

        private string GetResponseMessage(HttpResponseMessage responseMsg)
        {
            string resultStatus = "Success";

            if (!responseMsg.IsSuccessStatusCode)
                resultStatus = responseMsg.ReasonPhrase;

            return resultStatus.ToString();
        }


        public IActionResult Error()
        {           
            return View();
        }


        private async Task AddTokenToClient()
        {
            if (User.Identity.IsAuthenticated)
            {
                //get access token
                string accessToken = await HttpContext.GetTokenAsync("access_token");

                //check if access_token is expired
                DateTime accessTokenExpiresAt = DateTime.Parse(
                await HttpContext.GetTokenAsync("expires_at"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);

                //if expired use refresh_token to get a new access_token
                if (accessTokenExpiresAt < DateTime.Now)
                {
                    var refreshToken = await RenewTokenAsync();
                    accessToken = refreshToken.access_token;
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        //call auth0 and send refresh_troken for new access_token
        private async Task<RefreshToken> RenewTokenAsync()
        {
            string refreshToken = await HttpContext.GetTokenAsync("refresh_token");

            var tokenPayload = new RefreshTokenRequest
            {
                grant_type = "refresh_token",
                client_id = _configuration["Auth0:ClientId"],
                client_secret = _configuration["Auth0:ClientSecret"],
                refresh_token = refreshToken
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var result = await client.PostAsJsonAsync($"https://{_configuration["Auth0:Domain"]}/oauth/token", tokenPayload);
            return result.Content.ReadAsAsync<RefreshToken>().Result;
        }

        //populate UserInfo view model with user claims
        private UserInfo GetUserData(HttpResponseMessage responseMsg = null)
        {
            var claimDictionary = User.Claims.ToDictionary(x => x.Type, x => x.Value);
            var userinfo = new UserInfo()
            {
                NickName = claimDictionary["nickname"],
                Picture = claimDictionary["picture"],
                Name = claimDictionary["name"],
                NameIdentifier = claimDictionary["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
                Role = claimDictionary["http://schemas.microsoft.com/ws/2008/06/identity/claims/roles"],
                RoleDescription = claimDictionary["https://challengeApp/jobDescription"],
                ApiCallStatus = responseMsg != null ? GetResponseMessage(responseMsg) : string.Empty
            };

            return userinfo;
        }
    }
}
