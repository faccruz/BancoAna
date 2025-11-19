using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BancoAna.Account.Application.Services
{
    public class TransferService
    {
        private readonly IAccountRepository _repo;
        private readonly decimal _tarifaValor;

        public TransferService(IAccountRepository repo, decimal tarifaValor)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tarifaValor = tarifaValor;
        }

        public async Task<TransferResult> TransferirAsync(string requisicaoId, int numeroOrigem, int numeroDestino, decimal valor)
        {
            if (string.IsNullOrWhiteSpace(requisicaoId))
                return new TransferResult { Sucesso = false, Mensagem = "RequisicaoId obrigatório." };

            // idempotência
            if (await _repo.IdempotenciaExisteAsync(requisicaoId))
                return new TransferResult { Sucesso = true, Mensagem = "Operação já processada (idempotência)." };

            if (numeroOrigem == numeroDestino)
                return new TransferResult { Sucesso = false, Mensagem = "Transferência para a mesma conta não é permitida." };

            // buscar contas
            var origem = await _repo.ObterPorNumeroAsync(numeroOrigem);
            if (origem is null)
                return new TransferResult { Sucesso = false, Mensagem = "Conta de origem não encontrada." };

            var destino = await _repo.ObterPorNumeroAsync(numeroDestino);
            if (destino is null)
                return new TransferResult { Sucesso = false, Mensagem = "Conta de destino não encontrada." };

            // verificar contas ativas
            if (!origem.Ativo)
                return new TransferResult { Sucesso = false, Mensagem = "Conta de origem está inativa." };
            if (!destino.Ativo)
                return new TransferResult { Sucesso = false, Mensagem = "Conta de destino está inativa." };

            // valor positivo
            if (valor <= 0m)
                return new TransferResult { Sucesso = false, Mensagem = "Valor inválido." };

            // calcular total a debitar da origem (valor + tarifa)
            var tarifa = Math.Round(_tarifaValor, 2);
            var totalDebito = valor + tarifa;

            // verificar saldo (inclui tarifa)
            var saldoOrigem = await _repo.ObterSaldoAsync(origem.IdContaCorrente);
            if (saldoOrigem < totalDebito)
                return new TransferResult { Sucesso = false, Mensagem = "Saldo insuficiente para a transferência incluindo tarifa." };

            // data
            var data = DateTime.Now.ToString("dd/MM/yyyy");



            // --- realiza operações ---
            // Débito da origem (valor)
            var movDebito = new Movimento
            {
                IdMovimento = Guid.NewGuid().ToString(),
                IdContaCorrente = origem.IdContaCorrente,
                DataMovimento = data,
                TipoMovimento = "D",
                Valor = valor
            };
            await _repo.AdicionarMovimentoAsync(movDebito);

            // Crédito no destino (valor)
            var movCredito = new Movimento
            {
                IdMovimento = Guid.NewGuid().ToString(),
                IdContaCorrente = destino.IdContaCorrente,
                DataMovimento = data,
                TipoMovimento = "C",
                Valor = valor
            };
            await _repo.AdicionarMovimentoAsync(movCredito);

            // Se tarifa > 0: registrar débito de tarifa na origem como movimento e registrar na tabela tarifa (metadado)
            if (tarifa > 0m)
            {
                var movTarifa = new Movimento
                {
                    IdMovimento = Guid.NewGuid().ToString(),
                    IdContaCorrente = origem.IdContaCorrente,
                    DataMovimento = data,
                    TipoMovimento = "D",
                    Valor = tarifa
                };

                // Debita efetivamente do saldo (movimento)
                await _repo.AdicionarMovimentoAsync(movTarifa);

                // registrar somente metadados na tabela tarifa (não deve duplicar movTarifa)
                var tarifaEntity = new Tarifa
                {
                    IdTarifa = Guid.NewGuid().ToString(),
                    IdContaCorrente = origem.IdContaCorrente,
                    DataMovimento = data,
                    Valor = tarifa
                };
                await _repo.RegistrarTarifaAsync(tarifaEntity);
            }

            // registrar a transferência (metadados)
            var transferencia = new Transferencia
            {
                IdTransferencia = Guid.NewGuid().ToString(),
                IdContaCorrenteOrigem = origem.IdContaCorrente,
                IdContaCorrenteDestino = destino.IdContaCorrente,
                DataMovimento = data,
                Valor = valor
            };
            await _repo.RegistrarTransferenciaAsync(transferencia);

            // registrar idempotência
            var resumoReq = new { numeroOrigem, numeroDestino, valor, tarifa, data };
            await _repo.RegistrarIdempotenciaAsync(new Idempotencia
            {
                ChaveIdempotencia = requisicaoId,
                Requisicao = System.Text.Json.JsonSerializer.Serialize(resumoReq),
                Resultado = "OK"
            });


            return new TransferResult
            {
                Sucesso = true,
                Mensagem = "Transferência realizada com sucesso.",
                ContaOrigem = numeroOrigem.ToString(CultureInfo.InvariantCulture),
                ContaDestino = numeroDestino.ToString(CultureInfo.InvariantCulture),
                Valor = valor
            };
        }
    }
}
public class TransferResult
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? ContaOrigem { get; set; }
    public string? ContaDestino { get; set; }
    public decimal Valor { get; set; }
}
