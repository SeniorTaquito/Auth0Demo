# Using an Access\Refresh Token in a Web Application when calling an API

This sample will show you how to use an access token to make calls to an API. 
It will also demonstrate how to use a refresh token to get a new access token when it expires.

## Requirements 
* .[NET Core 2.2 SDK]( https://dotnet.microsoft.com/download/dotnet-core/2.2)

## To run this project

1. Replace the Domain, ClientId, and ClientSecret values in the appsettings.json with your own
	```json
	{
	  "Auth0": {
	    "Domain": "Your Auth0 domain",
	    "ClientId": "Your Auth0 Client Id",
		"ClientSecret": "Your Auth0 client secret"
	  } 
	}
	```

2. Add the application to IIS or open it in visual studio and run it in IIS express.

   **NOTE:** When running this service alone you will not be able to make calls to the API.           
             To test fully, run both this service and the AuthWebApiCore together. 

## Configuring the middleware
In Startup.cs we need to setup the authentication middleware

### 1. Register the Cookie and OIDC Authentication handlers
Set Auth0 as the claims handler and configure Open ID Connect

Configure the following scopes: <br/>
**openid:** required for id token <br/>
**profile:** required for claim with user data i.e. nickname, avatar etc. <br/>
**offline_access:** required for refresh token

```csharp
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    // Add authentication services
    services.AddAuthentication(options => {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect("Auth0", options => {
        // Set the authority to your Auth0 domain
        options.Authority = $"https://{Configuration["Auth0:Domain"]}";

        // Configure the Auth0 Client ID and Client Secret
        options.ClientId = Configuration["Auth0:ClientId"];
        options.ClientSecret = Configuration["Auth0:ClientSecret"];

        // Set response type to code
        options.ResponseType = "code";

        // Configure the scope
        options.Scope.Clear();
        options.Scope.Add("openid"); //required for id_token
        options.Scope.Add("profile"); //required to get claim with user data i.e. nickname, avatar etc.
        options.Scope.Add("offline_access"); //required for refresh token

        // Set the callback path, so Auth0 will call back to http://localhost:5000/signin-auth0 
        // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
        options.CallbackPath = new PathString("/signin-auth0");

        // Configure the Claims Issuer to be Auth0
        options.ClaimsIssuer = "Auth0";  
		
		options.SaveTokens = true;
    });

    // Add framework services.
    services.AddMvc();
}
```
### 1.1 Configuring OIDC Authentication handlers continued
Set the RoleClaimType to the URL used in the **Set roles to a user** rule.
Add the OnRedirectToIdentityProvider event and set the audience to the **ApiIdentifier** for the AuthWebApiCore project. 

**NOTE:** If the audience is not set or configured correctly an access_token will be returned but it will not be a legitimate JWT and      	    cannot be used to access the API. 

```csharp
// Startup.cs

.AddOpenIdConnect("Auth0", options => {
	// Code omitted for brevity
		
	options.SaveTokens = true;

	options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/roles"
        };

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                context.ProtocolMessage.SetParameter("audience", "authWebApi.com");  //set the audience to get acess_token for webapi              
                return Task.FromResult(0);
            },

            // handle the logout redirection 
            OnRedirectToIdentityProviderForSignOut = (context) =>
            {
                var logoutUri = $"https://{Configuration["Auth0:Domain"]}/v2/logout?client_id={Configuration["Auth0:ClientId"]}";

                var postLogoutUri = context.Properties.RedirectUri;
                if (!string.IsNullOrEmpty(postLogoutUri))
                {
                    if (postLogoutUri.StartsWith("/"))
                    {
                        // transform to absolute
                        var request = context.Request;
                        postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                    }
                    logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                }
                
                context.Response.Redirect(logoutUri);
                context.HandleResponse();

                return Task.CompletedTask;
            }
        };   
    });
 });
```

### 2. Register the Authentication middleware

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseStaticFiles();
	app.UseCookiePolicy();
    
	// Register the Authentication middleware
    app.UseAuthentication();

    app.UseMvc(routes =>
    {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
    });
}
```

## Using JWT Tokens to Access the API
### 1. Retrieving the Access Token 
The access_token is required when making an http request to the API. While building that request we need to check if the access_token is expired. 
If it is, the refresh_token is required to get a new one. 

```csharp
//HomeController.cs

 private HttpClient client = new HttpClient();

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
             var refreshToken = RenewToken();
             accessToken = refreshToken.access_token;
         }

         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
     }
 }
```

### 2. Renewing the Access Token
To renew an access_token, make a call to https://"Auth0:Domain"/oauth/token

```csharp
//HomeController.cs

private async Task<RefreshToken> RenewToken()
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
```
