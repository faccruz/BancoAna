using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
using BancoAna.Account.Api.DTOs;
using BancoAna.Account.Api.Services;
using BCrypt.Net;

namespace BancoAna.Account.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _repo;
    private readonly IJwtService _jwt;

    public AccountController(IAccountRepository repo, IJwtService jwt)
    {
        _repo = repo;
        _jwt = jwt;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Cpf) || string.IsNullOrWhiteSpace(req.Senha))
            return BadRequest(new { message = "CPF e senha são obrigatórios", errorType = "INVALID_DOCUMENT" });

        var existing = await _repo.GetByCpfAsync(req.Cpf);
        if (existing != null)
            return BadRequest(new { message = "CPF já cadastrado", errorType = "INVALID_DOCUMENT" });

        var numero = GenerateNumeroConta();
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Senha);

        var acc = new Domain.Models.Account
        {
            NumeroConta = numero,
            NomeTitular = req.Nome,
            Cpf = req.Cpf,
            SenhaHash = hash,
            Ativo = true
        };

        await _repo.CreateAsync(acc);
        return CreatedAtAction(nameof(GetBalance), new { numeroConta = numero }, new { numeroConta = numero });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NumeroConta) || string.IsNullOrWhiteSpace(req.Senha))
            return BadRequest(new { message = "Número da conta e senha são obrigatórios", errorType = "INVALID_REQUEST" });

        var acc = await _repo.GetByNumeroContaAsync(req.NumeroConta);
        if (acc == null) return Unauthorized(new { message = "Usuário não autorizado", errorType = "USER_UNAUTHORIZED" });

        if (!BCrypt.Net.BCrypt.Verify(req.Senha, acc.SenhaHash))
            return Unauthorized(new { message = "Usuário não autorizado", errorType = "USER_UNAUTHORIZED" });

        var token = _jwt.GenerateToken(acc.NumeroConta);
        return Ok(new { token });
    }

    [Authorize]
    [HttpGet("saldo")]
    public async Task<IActionResult> GetBalance([FromQuery] string? numeroConta)
    {
        var numero = numeroConta;
        if (string.IsNullOrEmpty(numero))
            numero = User.FindFirstValue("account") ?? User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(numero))
            return BadRequest(new { message = "Conta não informada", errorType = "INVALID_ACCOUNT" });

        var acc = await _repo.GetByNumeroContaAsync(numero);
        if (acc == null) return BadRequest(new { message = "Conta não encontrada", errorType = "INVALID_ACCOUNT" });
        if (!acc.Ativo) return BadRequest(new { message = "Conta inativa", errorType = "INACTIVE_ACCOUNT" });

        var saldo = await _repo.GetSaldoAsync(numero);
        var resp = new BalanceResponse { NumeroConta = numero, Saldo = saldo, DataHora = DateTime.UtcNow };
        return Ok(resp);
    }

    [Authorize]
    [HttpPost("movimentos")]
    public async Task<IActionResult> PostMovement([FromBody] MovementRequest req)
    {
        if (req == null) return BadRequest(new { message = "Request inválido", errorType = "INVALID_REQUEST" });
        if (req.Valor <= 0) return BadRequest(new { message = "Valor inválido", errorType = "INVALID_VALUE" });
        if (req.Tipo != 'C' && req.Tipo != 'D') return BadRequest(new { message = "Tipo inválido", errorType = "INVALID_TYPE" });
        if (string.IsNullOrWhiteSpace(req.RequisicaoId)) return BadRequest(new { message = "RequisicaoId obrigatório", errorType = "INVALID_REQUEST" });

        var numero = req.NumeroConta;
        if (string.IsNullOrWhiteSpace(numero))
            numero = User.FindFirstValue("account") ?? User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(numero)) return BadRequest(new { message = "Conta não informada", errorType = "INVALID_ACCOUNT" });

        var acc = await _repo.GetByNumeroContaAsync(numero);
        if (acc == null) return BadRequest(new { message = "Conta não encontrada", errorType = "INVALID_ACCOUNT" });
        if (!acc.Ativo) return BadRequest(new { message = "Conta inativa", errorType = "INACTIVE_ACCOUNT" });

        if (await _repo.RequisicaoExistsAsync(req.RequisicaoId))
            return NoContent();

        var mov = new Movement
        {
            RequisicaoId = req.RequisicaoId,
            ContaNumero = numero,
            Tipo = req.Tipo,
            Valor = req.Valor,
            CriadoEm = DateTime.UtcNow
        };

        await _repo.AddMovementAsync(mov);
        return NoContent();
    }

    private static string GenerateNumeroConta()
    {
        return DateTime.UtcNow.Ticks.ToString();
    }
}
