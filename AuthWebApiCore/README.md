# Authorizing Endpoints based on Role

This sample will show you how to allow access to an endpoint based on the user's role stored in the access token.

## Requirements 
* .[NET Core 2.2 SDK]( https://dotnet.microsoft.com/download/dotnet-core/2.2)

## To run this project

1. Replace the Domain and ApiIdentifier values in the appsettings.json with your own
	```json
	{
	  "Auth0": {
	    "Domain": "Your Auth0 domain",
	    "ApiIdentifier": "Your Auth0 api identifier"
	  } 
	}
	```
2. Add the application to IIS or open it in visual studio and run in IIS express.

   **NOTE:** When running this service alone you will recieve a 401 unauthorized error. 
             This is because the endpoints are expecting an access token with the user's role.
             To test fully, run both this service and the AuthWebapp together. 

## Configuring the middleware
In Startup.cs we need to setup the authentication middleware

### 1. Register the Authentication handler 
Configure authentication to use JWT bearer tokens

```csharp
 public void ConfigureServices(IServiceCollection services)
 {
     services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);          
 
     services.AddAuthentication(options =>
     {
         options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
         options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
     }).AddJwtBearer(options =>
     {
         options.Authority = $"https://{Configuration["Auth0:Domain"]}/"; ;
         options.Audience = Configuration["Auth0:ApiIdentifier"];
         options.TokenValidationParameters = new TokenValidationParameters
         {
		     //Retrieving the role we set in the Set roles to a user rule
             RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/roles",

             //required for checking access_token expiration date
             RequireExpirationTime = true,
             ValidateLifetime = true,                    
             ClockSkew = TimeSpan.Zero
         };
     });
 }
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

    app.UseAuthentication();
    
    app.UseMvc();
}
```

## Securing the API method using Roles
These endpoints will use the roles from the user app_metadata that were added to the access token in the **Set roles to a user** rule

#### Read Role
```csharp
  [HttpGet]
  [Authorize(Roles = "read")]
  public ActionResult Get()
  {
      return Ok();
  }
```

#### Write Role
```csharp
   [HttpPost]        
   [Authorize(Roles = "write")]
   public ActionResult Post([FromBody] Game game)
   {            
       return Ok();
   }
```

#### Delete Role
```csharp
  [HttpDelete("{id}")]        
  [Authorize(Roles = "delete")]
  public ActionResult Delete(int id)
  {
      return Ok();
  }
```
