# Calling Protected API endpoints from a Web Application

This demo covers the following topics
1. Logging into a web application 
2. Getting and using a refresh token 
3. Custom Auth0 Rules
4. Protecting and accessing API endpoints based on a user's role

The following will walk you through configuring the applications in the Auth0 dashboard.
For more application specific information including code snippets, look through the readme file for each application.

* AuthWebApiCore/readme.md
* AuthWebApplication/readme.md

## Requirements 
* .[NET Core 2.2 SDK]( https://dotnet.microsoft.com/download/dotnet-core/2.2)
* [Visual Studio 2017]( https://visualstudio.microsoft.com/downloads/) (or another IDE of your choice)
* [Auth0 Account]( https://auth0.com/)

## Auth0 Dashboard Configuration
This demo contains a web application and API that needs to be created and configured in the Auth0 dashboard.

### Web Application 
To configure the web application

1. Login to Auth0 and access your dashboard
2. Click on the orange button located in the top Right of the screen that says "+ NEW APPLICATION" 
3. Add a name, select "Regular Web App" and click "Create"
4. Take note of the Domain, Client ID, and Client Secret. These will be required when configuring the web application.
5. Add callback and logout URLs which will be called after authentication and when logging out. When running locally the callback URL will be http://localhost:portnumber/signin-auth0 and the logout URL will be http://localhost:portnumber. 
6. Click on "Save changes"

### API
To configure the API

1. Login to Auth0 and access your dashboard
2. On the left navbar click on "APIs" then the orange button located in the top right of the screen that says "+ Create API" 
3. Add a name and identifier for your app. Leave the signing algorithm as RS256 and click "Create"
4. Scroll down till you see "Allow Offline Access" and turn it on. This will allow the web appication to request a refresh token when the access token expires.
6. Click on "Save" 

### Database
Create database connection

1. Login to Auth0 and access your dashboard
2. On the left navbar click on "Connections" > "Database" > then the orange button located in the top right of the screen that says "+ Create DB Connection"
3. Add a connection name and click "Create"

### Users
Configure user roles for endpoint authorization

1. Login to Auth0 and access your dashboard
2. On the left navbar click on "Users" then the orange button located in the top right corner of the screen that says "+ Create User"
3. Fill out the user details and set the connection to the name of the database that was just created and save
4. Click on the user name to view the user's settings
5. Scroll down to "app_metadata" and add a javascript object with the below property and save
6. Create 2 more users, with the jobTitle being different for each. The available job titles are "read", "write", and "delete"

```json
{
  "jobTitle": "read"  
}
```

### Rules
Create two Rules which are called when a user is authenticated 

#### Set roles to a user
This rule will get the jobTitle from the app_metadata and add that role to the id token and access token

```javascript
function (user, context, callback) {
  user.app_metadata = user.app_metadata || {};
    
  //check if app_metadata has jobTitle property and it is not empty. If empty default jobTitle to unemployed
  var jobTitle = user.app_metadata.hasOwnProperty('jobTitle') && user.app_metadata.jobTitle !== '' ? user.app_metadata.jobTitle: 'unemployed';
  console.log(jobTitle);
  
  //id token is for app
  context.idToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/roles"] = [jobTitle];
  
  //access token is for the api
  context.accessToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/roles"] = [jobTitle];  
 
  callback(null, user, context);
}
```

#### Set job description
This rule will get the jobTitle from the app_metadata and add a job description to the id token based on the job title.
In a real app the job description would be pulled from a database.

```javascript
function (user, context, callback) {
  user.appmetadata = user.app_metadata || {};  
  
  var jobDescription = null;
  
  var jobTitle = user.app_metadata.jobTitle;
  console.log(jobTitle);
  
  switch(jobTitle){
    case 'read':
      jobDescription = 'can perform GET actions on the api';
      break;
    case 'write':
      jobDescription = 'can perform POST actions on the api';
      break;
    case 'delete':
      jobDescription = 'can perform DELETE actions on the api';
      break;    
  } 
   context.idToken[ 'https://challengeApp/jobDescription'] = jobDescription;  
  
  callback(null, user, context);
}
```

