# RestCall


[![NuGet](https://img.shields.io/nuget/dt/Vapolia.UserInteraction?label=Get%20It%20On%20Nuget&style=for-the-badge)](https://www.nuget.org/packages/Softlion.FluentRest/)  


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
```
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
