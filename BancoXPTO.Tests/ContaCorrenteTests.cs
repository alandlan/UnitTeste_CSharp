using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;


namespace BancoXPTO.Tests
{
    [TestClass]
    public class ContaCorrenteTests
    {
        private ContaCorrente GetContaCorrente()
        {
            var cc = new ContaCorrente(
                Mock.Of<IAgenciaRepository>(),
                Mock.Of<IContaRepository>(),
                Mock.Of<IExtratoRepository>()
            );

            var agencia = new Agencia() { Id = 100, Nome = "Agencia teste" };
            var agencia2 = new Agencia() { Id = 200, Nome = "Agencia teste 2" };
            var conta = new Conta() { Id = 555, AgenciaId = 100, NomeCliente = "Chuck", CPFCliente = "1312334123", Saldo = 100m };
            var conta2 = new Conta() { Id = 700, AgenciaId = 200, NomeCliente = "Michael", CPFCliente = "06645045404", Saldo = 200m };
            

            Mock.Get(cc.AgenciaRepository).Setup(r => r.GetById(100)).Returns(agencia);
            Mock.Get(cc.AgenciaRepository).Setup(r => r.GetById(200)).Returns(agencia2);
            Mock.Get(cc.ContaRepository).Setup(c => c.GetById(100, 555)).Returns(conta);
            Mock.Get(cc.ContaRepository).Setup(c => c.GetById(200, 700)).Returns(conta2);


            return cc;
        }

        [TestMethod]
        public void Deposito_retorna_true_se_realizado_com_sucesso()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Deposito(100, 555, 50m, out erro);

            // asserts
            Assert.IsTrue(result);
            Mock.Get(cc.ContaRepository).Verify(r => r.Save(It.Is<Conta>(c => c.AgenciaId == 100 && c.Id == 555 && c.Saldo == 150m)));
            Mock.Get(cc.ExtratoRepository).Verify(r => r.Save(It.Is<Extrato>(e => e.AgenciaId == 100 && e.ContaId == 555 && e.Descricao == "Deposito" && e.DataRegistro.Date == DateTime.Today && e.Valor == 50m && e.Saldo==150m)));

            
        }

        [TestMethod]
        public void Deposito_erro_se_agencia_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Deposito(666, 100, 100m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Agência Invalida", erro ); 
        }

        [TestMethod]
        public void Deposito_erro_se_conta_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Deposito(100, 666, 100m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Conta Invalida", erro);
        }

        [TestMethod]
        public void Deposito_erro_se_valor_menor_ou_igual_zero()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Deposito(100, 555, 0m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("O valor do deposito for maior que zero!", erro);
        }

        [TestMethod]
        public void Deposito_erro_se_ocorrer_uma_exception_na_hora_de_salvar_dados()
        {
            // arrange
            var cc = GetContaCorrente();

            Mock.Get(cc.ContaRepository).Setup(r => r.Save(It.IsAny<Conta>())).Throws(new Exception("Deu ruim!"));

            // act 
            string erro;
            var result = cc.Deposito(100, 555, 100m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Ocorreu um problema ao fazer o depósito!", erro);
        }

        [TestMethod]
        public void Saque_retorna_true_se_realizado_com_sucesso()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saque(100, 555, 50m, out erro);

            // asserts
            Assert.IsTrue(result);
            Mock.Get(cc.ContaRepository).Verify(r => r.Save(It.Is<Conta>(c => c.AgenciaId == 100 && c.Id == 555 && c.Saldo == 50m)));
            Mock.Get(cc.ExtratoRepository).Verify(r => r.Save(It.Is<Extrato>(e => e.AgenciaId == 100 && e.ContaId == 555 && e.Descricao == "Saque" && e.DataRegistro.Date == DateTime.Today && e.Valor == -50m && e.Saldo == 50m)));
        }

        [TestMethod]
        public void Saque_erro_se_agencia_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();
            
            // act 
            string erro;
            var result = cc.Saque(666, 555, 50m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Agencia Invalida", erro);
        }

        [TestMethod]
        public void Saque_erro_se_conta_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saque(100, 666, 50m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Conta Invalida", erro);
        }

        [TestMethod]
        public void Saque_erro_se_valor_menor_ou_igual_zero()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saque(100, 555, -1m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("O valor do saque precisa ser maior que zero", erro);
        }

        [TestMethod]
        public void Saque_erro_se_valor_maior_que_saldo_conta()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saque(100, 555, 110m, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("O valor do saque precisa ser menor ou igual ao saldo da conta", erro);
        }

        [TestMethod]
        public void Transferencia_retorna_true_se_realizado_com_sucesso()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 555, 50m, 200, 700, out erro);

            // asserts
            Assert.IsTrue(result);
            //Conta Origem
            Mock.Get(cc.ContaRepository).Verify(r => r.Save(It.Is<Conta>(c => c.AgenciaId == 100 && c.Id == 555 && c.Saldo == 50m)));
            Mock.Get(cc.ExtratoRepository).Verify(r => r.Save(It.Is<Extrato>(e => e.AgenciaId == 100 && e.ContaId == 555 && e.Descricao == "Transferencia para AG 200 CC 700" && e.DataRegistro.Date == DateTime.Today && e.Valor == -50m && e.Saldo == 50m)));

            //Conta Destino
            Mock.Get(cc.ContaRepository).Verify(r => r.Save(It.Is<Conta>(c => c.AgenciaId == 200 && c.Id == 700 && c.Saldo == 250m)));
            Mock.Get(cc.ExtratoRepository).Verify(r => r.Save(It.Is<Extrato>(e => e.AgenciaId == 200 && e.ContaId == 700 && e.Descricao == "Transferencia de AG 100 CC 555" && e.DataRegistro.Date == DateTime.Today && e.Valor == 50m && e.Saldo == 250m)));
        }

        [TestMethod]
        public void Transferencia_erro_se_agencia_origem_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(666, 555, 50m, 200, 700, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Agencia de origem Invalida", erro);
        }

        [TestMethod]
        public void Transferencia_erro_se_conta_origem_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 666, 50m, 200, 700, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Conta de Origem Invalida", erro);
        }

        [TestMethod]
        public void Transferencia_erro_se_agencia_destino_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 555, 50m,666,700, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Agencia de destino invalida", erro);
        }

        [TestMethod]
        public void Transferencia_erro_se_conta_destino_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 555, 50m, 200, 666, out erro);


            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("Conta de destino inválida!", erro);
        }

        [TestMethod]
        public void Transferencia_erro_se_valor_menor_ou_igual_zero()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 555, 0m, 200, 700, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("O valor deve ser maior que zero!", erro);
        }

        [TestMethod]
        public void Transferencia_erro_se_valor_maior_que_saldo_conta_origem()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Transferencia(100, 555, 200m, 200, 700, out erro);

            // asserts
            Assert.IsFalse(result);
            Assert.AreEqual("O valor deve ser menor ou igual ao saldo da conta de origem!", erro);
        }

        [TestMethod]
        public void Saldo_retorna_saldo_da_conta()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saldo(100, 555, out erro);

            // asserts
            Assert.AreEqual(100m, result);
        }

        [TestMethod]
        public void Saldo_erro_se_agencia_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saldo(666, 555, out erro);

            // asserts
            Assert.AreEqual(0m, result);
            Assert.AreEqual("Agencia Invalida", erro);
        }

        [TestMethod]
        public void Saldo_erro_se_conta_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Saldo(100, 666, out erro);

            // asserts
            Assert.AreEqual(0m, result);
            Assert.AreEqual("Conta Invalida", erro);
        }

        [TestMethod]
        public void Extrato_retorna_registros_do_extrato()
        {
            // arrange
            var cc = GetContaCorrente();
            var dataInicio = new DateTime(2020, 01, 01);
            var dataFim = new DateTime(2020, 01, 15);


            var extrato = Builder<Extrato>.CreateListOfSize(10)
                .All()
                .With(x=>x.AgenciaId=100)
                .With(x=>x.ContaId=555)
                .Build();

            Mock.Get(cc.ExtratoRepository).Setup(r => r.GetByPeriodo(100, 555, dataInicio, dataFim)).Returns(extrato);

            // act 
            string erro;
            var result = cc.Extrato(100, 555, dataInicio, dataFim, out erro);

            // asserts
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Count);
            Assert.AreEqual(extrato.Sum(e => e.Valor), result.Sum(r => r.Valor));
        }

        [TestMethod]
        public void Extrato_erro_se_agencia_nao_existir()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Extrato(666, 555, new DateTime(2020,01,01),new DateTime(2020,01,15), out erro);

            // asserts
            Assert.IsNull(result);
            Assert.AreEqual("Agencia Invalida", erro);
        }

        [TestMethod]
        public void Extrato_erro_se_conta_nao_existir_na_agencia()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Extrato(100, 666, new DateTime(2020, 01, 01), new DateTime(2020, 01, 15), out erro);

            // asserts
            Assert.IsNull(result);
            Assert.AreEqual("Conta Invalida", erro);
        }

        [TestMethod]
        public void Extrato_erro_se_data_inicio_maior_data_fim()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Extrato(100, 555, new DateTime(2021, 01, 01), new DateTime(2020, 01, 15), out erro);

            // asserts
            Assert.IsNull(result);
            Assert.AreEqual("Data inicio deve ser menor que a data de fim!", erro);
        }

        [TestMethod]
        public void Extrato_erro_se_periodo_maior_120_dias()
        {
            // arrange
            var cc = GetContaCorrente();

            // act 
            string erro;
            var result = cc.Extrato(100, 555, new DateTime(2020, 01, 01), (new DateTime(2020, 01, 15).AddDays(121)), out erro);

            // asserts
            Assert.IsNull(result);
            Assert.AreEqual("O perido nao deve ser superior a 120 dias!", erro);
        }

        [TestMethod]
        public void Extrato_primeira_linha_contem_saldo_anterior()
        {
            // arrange
            var cc = GetContaCorrente();
            var dataInicio = new DateTime(2020, 01, 01);
            var dataFim = new DateTime(2020, 01, 15);


            var extrato = Builder<Extrato>.CreateListOfSize(10)
                .All()
                .With(x => x.AgenciaId = 100)
                .With(x => x.ContaId = 555)
                .Build();

            Mock.Get(cc.ExtratoRepository).Setup(r => r.GetByPeriodo(100, 555, dataInicio, dataFim)).Returns(extrato);
            Mock.Get(cc.ExtratoRepository).Setup(r => r.GetSaldoAnterior(100, 555, dataInicio, dataFim)).Returns(30m);

            // act 
            string erro;
            var result = cc.Extrato(100, 555, dataInicio, dataFim, out erro);

            // asserts
            Assert.AreEqual("Saldo anterior", result.First().Descricao);
            Assert.AreEqual(30m, result.First().Saldo);
        }


    }
}