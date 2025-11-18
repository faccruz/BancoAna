using BancoAna.Account.Api.DTOs;
using BancoAna.Account.Api.Services;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Application.Services;
using BancoAna.Account.Domain.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace BancoAna.Account.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _repo;
    private readonly TransferService _transferService;
    private readonly IJwtService _jwt;

    public AccountController(
        IAccountRepository repo,
        TransferService transferService,
        IJwtService jwt)
    {
        _repo = repo;
        _transferService = transferService;
        _jwt = jwt;
    }

    // ------------------------------------------------------------
    // 1) CRIAÇÃO DE CONTA
    // ------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validação CPF
        if (!BancoAna.Account.Application.Utils.CpfValidator.IsValid(req.Cpf))
            return BadRequest(new { message = "CPF inválido." });

        // TRAVA 1 — verificar CPF duplicado
        var existeCpf = await _repo.ObterPorCpfAsync(req.Cpf);
        if (existeCpf != null)
            return BadRequest(new { message = "CPF já cadastrado no sistema." });

        // bcrypt gera salt automaticamente
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Senha);

        var numeroConta = new Random().Next(10000, 99999);

        var conta = new ContaCorrente
        {
            IdContaCorrente = Guid.NewGuid().ToString(),
            Numero = numeroConta,
            Nome = req.Nome,
            Senha = hash,
            Salt = "", // opcional
            Ativo = true,
            Cpf = req.Cpf
        };

        await _repo.CriarContaAsync(conta);

        return Created("", new
        {
            idcontacorrente = conta.IdContaCorrente,
            numero = conta.Numero
        });
    }

    // ------------------------------------------------------------
    // 2) LOGIN
    // ------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var conta = await _repo.ObterPorNumeroAsync(req.Numero);
        if (conta == null) return Unauthorized();

        if (!conta.Ativo)
            return Unauthorized(new { message = "Conta ainda não foi ativada." });

        var ok = BCrypt.Net.BCrypt.Verify(req.Senha, conta.Senha);
        if (!ok) return Unauthorized();

        var token = _jwt.GenerateToken(
            conta.IdContaCorrente,
            conta.Numero.ToString()
        );

        return Ok(new { token });
    }

    // ------------------------------------------------------------
    // 3) MOVIMENTO (CRÉDITO/ DÉBITO)
    // ------------------------------------------------------------
    [Authorize]
    [HttpPost("movimentos")]
    public async Task<IActionResult> CreateMovement([FromBody] MovementRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // idempotência: se chave já processada, retornar NoContent (idem)
        if (await _repo.IdempotenciaExisteAsync(req.RequisicaoId))
            return NoContent();

        // Resolver o id da conta:
        // 1) se o cliente passou NumeroConta, usar; senão tentar extrair do token (claim "numeroConta")
        ContaCorrente? conta = null;
        if (req.NumeroConta.HasValue)
        {
            conta = await _repo.ObterPorNumeroAsync(req.NumeroConta.Value);
        }
        else
        {
            var numeroClaim = User.FindFirst("numeroConta")?.Value;
            if (!string.IsNullOrEmpty(numeroClaim) && int.TryParse(numeroClaim, out var numeroFromToken))
            {
                conta = await _repo.ObterPorNumeroAsync(numeroFromToken);
            }
        }

        if (conta == null)
            return BadRequest(new { message = "Conta não informada ou não encontrada." });

        if (!conta.Ativo)
            return Unauthorized(new { message = "Conta ainda não foi ativada." });

        if (req.Tipo == "D")
        {
            var saldo = await _repo.ObterSaldoAsync(conta.IdContaCorrente);
            if (saldo < req.Valor)
                return BadRequest(new { message = "Saldo insuficiente." });
        }

        // montar movimento
        var mov = new Movimento
        {
            IdMovimento = Guid.NewGuid().ToString(),
            IdContaCorrente = conta.IdContaCorrente,
            DataMovimento = DateTime.Now.ToString("dd/MM/yyyy"),
            TipoMovimento = req.Tipo,
            Valor = req.Valor
        };

        await _repo.AdicionarMovimentoAsync(mov);

        // registrar idempotência para evitar reprocessar
        await _repo.RegistrarIdempotenciaAsync(new Idempotencia
        {
            ChaveIdempotencia = req.RequisicaoId,
            Requisicao = System.Text.Json.JsonSerializer.Serialize(req),
            Resultado = System.Text.Json.JsonSerializer.Serialize(new { status = "OK", idmovimento = mov.IdMovimento })
        });

        // retorno: 204 No Content é aceitável — ou podemos retornar 201 com id do movimento
        return NoContent();
    }

    // ------------------------------------------------------------
    // 4) TRANSFERÊNCIA ENTRE CONTAS
    // ------------------------------------------------------------
    [Authorize]
    [HttpPost("transferencia")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _transferService.TransferirAsync(
            req.RequisicaoId,
            req.ContaOrigem,
            req.ContaDestino,
            req.Valor
        );

        if (!result.Sucesso)
            return BadRequest(result);

        return Ok(result);
    }

    // ------------------------------------------------------------
    // 5) CONSULTAR SALDO (EXIGE TOKEN)
    // ------------------------------------------------------------
    [Authorize]
    [HttpGet("saldo")]
    public async Task<IActionResult> GetSaldo([FromQuery] int? numeroConta = null)
    {
        // 1) resolver conta: se veio numero via query, usar; se não, pegar do token
        string idConta = null;

        if (numeroConta.HasValue)
        {
            var conta = await _repo.ObterPorNumeroAsync(numeroConta.Value);
            if (conta == null) return NotFound(new { message = "Conta não encontrada para o número informado." });
            idConta = conta.IdContaCorrente;
        }
        else
        {
            // pegar id conta do token -> claim "sub" (ou "accountId", dependendo de como voce gerou o token)
            idConta = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(idConta))
            {
                // tentar extrair numero da claim "numeroConta" e resolver
                var numeroClaim = User.FindFirst("numeroConta")?.Value;
                if (!string.IsNullOrEmpty(numeroClaim) && int.TryParse(numeroClaim, out var numero))
                {
                    var conta = await _repo.ObterPorNumeroAsync(numero);
                    if (conta == null) return NotFound(new { message = "Conta do token não encontrada." });
                    idConta = conta.IdContaCorrente;
                }
            }
        }

        if (string.IsNullOrEmpty(idConta))
            return BadRequest(new { message = "Não foi possível determinar a conta para consultar o saldo." });

        var saldo = await _repo.ObterSaldoAsync(idConta);

        return Ok(new { idcontacorrente = idConta, saldo });
    }
}
