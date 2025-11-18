namespace BancoAna.Account.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string idConta, string numeroConta);
    }
}
