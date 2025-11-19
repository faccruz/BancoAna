using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using BancoAna.Account.Application.Services;
using BancoAna.Account.Application.Interfaces;
using BancoAna.Account.Domain.Models;
namespace BancoAna.Account.Tests.Unit
{
    public class TransferServiceTests
    {
        [Fact]
        public async Task TransferirAsync_Succeeds_And_Applies_Tariff()
        {
            // arrange
            var repoMock = new Mock<IAccountRepository>();
            var requisicaoId = "req-1";
            int numOrig = 11111, numDest = 22222;
            var idOrig = "id-orig";
            var idDest = "id-dest";

            // idempotencia false
            repoMock.Setup(r => r.IdempotenciaExisteAsync(requisicaoId)).ReturnsAsync(false);

            // contas
            repoMock.Setup(r => r.ObterPorNumeroAsync(numOrig))
                .ReturnsAsync(new ContaCorrente { IdContaCorrente = idOrig, Numero = numOrig, Ativo = true });
            repoMock.Setup(r => r.ObterPorNumeroAsync(numDest))
                .ReturnsAsync(new ContaCorrente { IdContaCorrente = idDest, Numero = numDest, Ativo = true });

            // saldo suficiente: retorna 100
            repoMock.Setup(r => r.ObterSaldoAsync(idOrig)).ReturnsAsync(100m);

            // operações de persistência: permitir
            repoMock.Setup(r => r.AdicionarMovimentoAsync(It.IsAny<Movimento>())).Returns(Task.CompletedTask);
            repoMock.Setup(r => r.RegistrarTarifaAsync(It.IsAny<Tarifa>())).Returns(Task.CompletedTask);
            repoMock.Setup(r => r.RegistrarTransferenciaAsync(It.IsAny<Transferencia>())).Returns(Task.CompletedTask);
            repoMock.Setup(r => r.RegistrarIdempotenciaAsync(It.IsAny<Idempotencia>())).Returns(Task.CompletedTask);

            var tarifaValor = 1.00m;
            var service = new TransferService(repoMock.Object, tarifaValor);


            // act
            var result = await service.TransferirAsync(requisicaoId, numOrig, numDest, 10m);

            // assert
            result.Sucesso.Should().BeTrue();
            repoMock.Verify(r => r.AdicionarMovimentoAsync(It.Is<Movimento>(m => m.IdContaCorrente == idOrig && m.TipoMovimento == "D" && m.Valor == 10m)), Times.Once);
            repoMock.Verify(r => r.AdicionarMovimentoAsync(It.Is<Movimento>(m => m.IdContaCorrente == idDest && m.TipoMovimento == "C" && m.Valor == 10m)), Times.Once);
            repoMock.Verify(r => r.AdicionarMovimentoAsync(It.Is<Movimento>(m => m.IdContaCorrente == idOrig && m.TipoMovimento == "D" && m.Valor == tarifaValor)), Times.Once);
            repoMock.Verify(r => r.RegistrarTarifaAsync(It.IsAny<Tarifa>()), Times.Once);
            repoMock.Verify(r => r.RegistrarTransferenciaAsync(It.IsAny<Transferencia>()), Times.Once);
            repoMock.Verify(r => r.RegistrarIdempotenciaAsync(It.IsAny<Idempotencia>()), Times.Once);
        }

        [Fact]
        public async Task TransferirAsync_Fails_When_InsufficientBalance_Including_Tariff()
        {
            // arrange
            var repoMock = new Mock<IAccountRepository>();
            var requisicaoId = "req-2";
            int numOrig = 11111, numDest = 22222;
            var idOrig = "id-orig";

            repoMock.Setup(r => r.IdempotenciaExisteAsync(requisicaoId)).ReturnsAsync(false);
            repoMock.Setup(r => r.ObterPorNumeroAsync(numOrig)).ReturnsAsync(new ContaCorrente { IdContaCorrente = idOrig, Numero = numOrig, Ativo = true });
            repoMock.Setup(r => r.ObterPorNumeroAsync(numDest)).ReturnsAsync(new ContaCorrente { IdContaCorrente = "id-dest", Numero = numDest, Ativo = true });

            // saldo insuficiente (5) para valor 10 + tarifa 1
            repoMock.Setup(r => r.ObterSaldoAsync(idOrig)).ReturnsAsync(5m);

            var service = new TransferService(repoMock.Object, 1.00m);

            // act
            var result = await service.TransferirAsync(requisicaoId, numOrig, numDest, 10m);

            // assert
            result.Sucesso.Should().BeFalse();
            result.Mensagem.Should().Contain("Saldo insuficiente");
        }

        [Fact]
        public async Task TransferirAsync_Is_Idempotent_If_Already_Processed()
        {
            var repoMock = new Mock<IAccountRepository>();
            var requisicaoId = "req-xx";
            repoMock.Setup(r => r.IdempotenciaExisteAsync(requisicaoId)).ReturnsAsync(true);

            var service = new TransferService(repoMock.Object, 0m);

            var result = await service.TransferirAsync(requisicaoId, 1, 2, 10m);

            result.Sucesso.Should().BeTrue();
            result.Mensagem!.ToLower().Should().Contain("idempotência");

            // no write operations must be called
            repoMock.Verify(r => r.AdicionarMovimentoAsync(It.IsAny<Movimento>()), Times.Never);
        }
    }
}
