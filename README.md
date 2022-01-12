# RestCall

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
