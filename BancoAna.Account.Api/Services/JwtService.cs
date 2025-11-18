using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using BancoAna.Account.Application.Interfaces;

namespace BancoAna.Account.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(string idConta, string numeroConta)
    {
        var key = _config["Jwt:Key"] ?? throw new Exception("JWT Key não configurada!");
        var issuer = _config["Jwt:Issuer"] ?? "BancoAnaAPI";
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "30");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, idConta),           // ID da conta
            new Claim("numeroConta", numeroConta),                     // número da conta
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
