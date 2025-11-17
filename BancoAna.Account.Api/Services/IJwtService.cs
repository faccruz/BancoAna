namespace BancoAna.Account.Api.Services;

public interface IJwtService
{
    string GenerateToken(string numeroConta);
}
