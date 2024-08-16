using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace Movies.Api.Sdk.Consumer;

public class AuthTokenProvider
{
    private readonly HttpClient _httpClient;
    private string _cachedToken = string.Empty;

    // to handle thread safety in an async context which allows to only 1 request to come in
    // at a given time when I generate the token
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public AuthTokenProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // we need a System.IdentityModel.Tokens.Jwy package here to read JWT token
    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_cachedToken);
            var expiryTimeText = jwt.Claims.Single(claim => claim.Type == "exp").Value; // exp stands for expiring
            var expiryDateTime = UnixTimeStampToDateTime(int.Parse(expiryTimeText));

            if (expiryDateTime > DateTime.UtcNow)
            {
                return _cachedToken;
            }
        }

        await Lock.WaitAsync();

        var response = await _httpClient.PostAsJsonAsync("http://localhost:5002/token", new
        {
            userid = "d8566de3-b1a6-4a9b-b842-8e3887a82e42",
            email = "nick@nickchapsas.com",
            customClaims = new Dictionary<string, object>
            {
                { "admin", true },
                { "trusted_member", true }
            }
        });

        var newToken = await response.Content.ReadAsStringAsync();
        _cachedToken = newToken;

        Lock.Release();

        return newToken;
    }

    // because of the unix time I need to have an helper method that allows me to process it
    private static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}