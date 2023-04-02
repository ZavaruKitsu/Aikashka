#region

using JWT.Algorithms;
using JWT.Builder;

#endregion

namespace Tinkoff.VoiceKit;

public sealed class Auth
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _endpoint;
    private DateTimeOffset _expTime;
    private string _jwt;

    public string Token
    {
        get
        {
            if (_expTime == null || _expTime < DateTimeOffset.UtcNow)
            {
                CreateJWT();
            }

            return _jwt;
        }
    }

    public Auth(string apiKey, string secretKey, string endpoint)
    {
        _apiKey = apiKey;
        _secretKey = secretKey;
        _endpoint = endpoint;

        CreateJWT();
    }

    private void CreateJWT()
    {
        _expTime = DateTimeOffset.UtcNow.AddMinutes(5);

        _jwt = new JwtBuilder()
#pragma warning disable CS0618
               .WithAlgorithm(new HMACSHA256Algorithm())
#pragma warning restore CS0618
               .WithSecret(Convert.FromBase64String(_secretKey))
               .AddClaim("aud", _endpoint)
               .AddClaim("exp", _expTime.ToUnixTimeSeconds())
               .AddHeader(HeaderName.KeyId, _apiKey)
               .Encode();
    }
}
