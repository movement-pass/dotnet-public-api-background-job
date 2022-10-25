namespace MovementPass.Public.Api.BackgroundJob.Infrastructure;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public interface ITokenValidator
{
    string Validate(string token);
}

public class TokenValidator : ITokenValidator
{
    private readonly TokenValidationParameters _parameters;
    private readonly ILogger<TokenValidator > _logger;

    public TokenValidator(
        IOptions<JwtOptions> options,
        ILogger<TokenValidator> logger)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        this._logger = logger ??
                       throw new ArgumentNullException(nameof(logger));

        this._parameters = new TokenValidationParameters
        {
            ValidIssuer = options.Value.Issuer,
            ValidAudiences = new[] { options.Value.Audience },
            IssuerSigningKey = options.Value.Key(),
            ValidateIssuerSigningKey = true
        };
    }

    public string Validate(string token)
    {
        if (token == null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal =
                handler.ValidateToken(token, this._parameters, out _);

            var idClaim =
                principal.Claims.FirstOrDefault(c => c.Type == "id");

            return idClaim?.Value;
        }
        catch(Exception e)
            when (e is SecurityTokenValidationException or ArgumentException)
        {
            this._logger.LogError("Invalid token: {@token}", token);
            return null;
        }
    }
}