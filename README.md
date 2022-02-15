# FluentRest


[![NuGet](https://img.shields.io/nuget/dt/Softlion.FluentRest?label=Get%20It%20On%20Nuget&style=for-the-badge)](https://www.nuget.org/packages/Softlion.FluentRest/)  


A small, simple and powerful .net6, maui compatible, and System.Text.Json only HTTP client library (also compatible with xamarin and .net core).  
Based on the amazing work from Todd Menier.  
It quickly replaces heavy/old libs like RestSharp.

```c#
var result = await "https://api.mysite.com"
    .AppendPathSegment("person")
    .SetQueryParams(new { api_key = "xyz" })
    .WithOAuthBearerToken("my_oauth_token")
    .PostJsonAsync(new { first_name = firstName, last_name = lastName })
    .ReceiveJson<T>();
```

When using xxxJson methods, the mapping into a C# object is done by `System.Text.Json`.

## Business Use Cases

Common code:
```c#
private const string Endpoint = "https://my.api.com/";
```

### Use case insensitive JSON mapping globally

```c#
FluentRestHttp.Configure(settings =>
{
    settings.JsonSerializer = new SystemTextJsonSerializer(new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        PropertyNameCaseInsensitive = true
    });
});
```

### Post to an API and get the json result

```C#
var response = await Endpoint.AppendPathSegment("signin").AllowAnyHttpStatus()
                             .PostJsonAsync(new { login = email });
if (response.StatusCode is not (>=200 and <300) && response.StatusCode != (int)HttpStatusCode.Conflict)
    return null;
var data = await response.GetJsonAsync<ApiSigninResponse>();
...
```

### Get an API with an optional parameter
```c#
var query = Endpoint.AppendPathSegment("1.0/account/find")
                .AllowAnyHttpStatus()
                .SetQueryParam("someParameter", text1)
                .SetQueryParam("otherParameter", text2)
                .WithOAuthBearerToken(token);

if (userLocation != null)
{
    query.SetQueryParam("latitude", userLocation.Latitude.ToString(CultureInfo.InvariantCulture));
}

var response = await query.GetAsync();

if (response.StatusCode is not (>= 200 and < 300))
    return null;

return await response.GetJsonAsync<List<ApiSomeResult>>();
```

### Extract a Bearer token from the response header
```c#
var response = await Endpoint.AppendPathSegment("signin").AllowAnyHttpStatus()
                             .PostJsonAsync(new { username = email, password });
if (response.StatusCode is not (>= 200 and < 300))
    return false;
if (!response.Headers.TryGetFirst("Authorization", out var authorization)
    || authorization?.StartsWith("Bearer ") != true)
    return false;
var sessionToken = authorization.Split(" ")[1];
```

### Add data in the header of each request globally
```c#
FluentRestHttp.Configure(settings =>
{
    //Mobile xamarin app
    var platform = Xamarin.Essentials.DeviceInfo.Platform.ToString();
    var version = Xamarin.Essentials.DeviceInfo.Version.ToString();
    var build = Xamarin.Essentials.AppInfo.BuildString;

    settings.BeforeCall = call =>
    {
        call.Request.Headers.Add("x-app-platform", platform);
        call.Request.Headers.Add("x-app-platform-version", version);
        call.Request.Headers.Add("x-app-version", build);
    };
});
```

### Prevent throwing an exception if the HTTP call fails (when internet is offline)
```c#
FluentRestHttp.Configure(settings =>
{
    settings.OnError = call =>
    {
        //If the call fails with an exception, return notfound instead of throwing
        if (call.Exception != null)
        {
            call.Response = new FluentRestResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
            call.ExceptionHandled = true;
        }
    };
}
```

### Refresh a JWT automatically
This snippet checks the JWT for expiration, and refreshes it before any api call.  
The check happens only for api calls having an Authorization header, so obviously requiring a valid JWT.  
`ApiSignIn()` must not have an Authorization header as this would create an infinite loop.

```c#
//In a class
private readonly SemaphoreSlim sync = new (1,1);
public string? AuthorizationToken { get; private set; }

//In the class constructor
settings.BeforeCallAsync = async call =>
{
    if (call.Request.Headers.Contains("Authorization"))
    {
        //Make sure the token is still valid. If we can't validate it, disconnect and go back to login screen.
        if (AuthorizationToken != null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateActor = false,
                ValidateTokenReplay = false,
                ValidateIssuerSigningKey = false,
                //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) // The same key as the one that generated the token
            };

            try
            {
                tokenHandler.ValidateToken(AuthorizationToken, validationParameters, out var _);
            }
            catch (Exception)
            {
                //Invalid JWT: try relogin once
                var old = AuthorizationToken;
                //sync required as this can be called by multiple threads simultaneously; and we want to refresh only once.
                await sync.WaitAsync();

                try
                {
                    if (old != AuthorizationToken)
                    {
                        if(AuthorizationToken != null)
                            call.Request.WithOAuthBearerToken(AuthorizationToken);
                    }
                    else if (userEmail != null && userPassword != null)
                    {
                        //we use the last auth info stored locally. You have to provide pour own login code, as this vary from service to service.
                        AuthorizationToken = await ApiSignIn(userEmail, userPassword);
                        if (AuthorizationToken != null)
                            call.Request.WithOAuthBearerToken(AuthorizationToken);
                    }
                    else
                        AuthorizationToken = null;
                        
                    if (AuthorizationToken == null)
                        await Logout();
                }
                finally
                {
                    sync.Release();
                }
            }
        }
    }
};


//Example use
const string Endpoint = "https://your.api.endpoint";
public async Task<ApiCallResultModel?> Hotspot_Status(CancellationToken cancel)
{
    var response = await Endpoint.AppendPathSegment("/some/api").AllowAnyHttpStatus()
        .WithOAuthBearerToken(AuthorizationToken)
        .GetAsync(cancel);

    if (response.StatusCode is not (>= 200 and < 300))
        return null;

    return await response.GetJsonAsync<ApiCallResultModel>();
}
```

### Disable https certificate validation
```c#
public class UntrustedCertClientFactory : DefaultHttpClientFactory
{
    public override HttpMessageHandler CreateMessageHandler() 
      => new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true }; 
}

FluentRestHttp.ConfigureClient(Endpoint, client => client.Settings.HttpClientFactory = new UntrustedCertClientFactory());
```

### Delete an item
```c#
public async Task<bool> Remove(string itemId)
{
    var response = await Endpoint.AppendPathSegment($"remove/{itemId}/").AllowAnyHttpStatus()
        .WithOAuthBearerToken(userInfo!.Authorization)
        .DeleteAsync();

    return response.StatusCode is >= 200 and < 300;
}
```

## Handling errors

When an http or json error occurs, the global custom handlers `OnError` and `OnErrorAsync` are both called in this order respectively.

If you set `ExceptionHandled` to true in the object received by one of these handlers, the exception is ignored. Then for http errors, you should set the `Response` property and it will be returned to the original caller. For json parsing errors, `default(T)` is always returned, you can not change this value.

If you don't set `ExceptionHandled` to true, the original call will throw one of the exception below.

* `FluentRestParsingException` when json parsing fails (for json methods like `GetJsonAsync<T>`)
* `FluentRestHttpTimeoutException` when a timeout occurs
* `FluentRestHttpException` when a http call fails directly (ie: domain not found, connection failed, ...)
