using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ReleasePilot.Api.Auth;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(UserRecord user)
    {
        var secret        = _configuration["Jwt:Secret"]!;
        var issuer        = _configuration["Jwt:Issuer"]!;
        var audience      = _configuration["Jwt:Audience"]!;
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub,  user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(ClaimTypes.Role,              user.Role),
        ];

        var token = new JwtSecurityToken(issuer:             issuer,
                                         audience:           audience,
                                         claims:             claims,
                                         expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
                                         signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
