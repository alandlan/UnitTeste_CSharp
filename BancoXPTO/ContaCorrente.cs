using System;
using System.Collections.Generic;
using System.Transactions;

namespace BancoXPTO
{
    public class ContaCorrente : IContaCorrente
    {
        public ContaCorrente(IAgenciaRepository agenciaRepository, IContaRepository contaRepository, IExtratoRepository extratoRepository)
        {
            AgenciaRepository = agenciaRepository;
            ContaRepository = contaRepository;
            ExtratoRepository = extratoRepository;
        }

        public IAgenciaRepository AgenciaRepository { get; set; }
        public IContaRepository ContaRepository { get; set; }
        public IExtratoRepository ExtratoRepository { get; set; }

        public bool Deposito(int agencia, int conta, decimal valor, out string mensagemErro)
        {
            mensagemErro = "";
            var ag = AgenciaRepository.GetById(agencia);

            if (ag == null)
            {
                mensagemErro = "Agência Invalida";
                return false;
            }

            var cc = ContaRepository.GetById(agencia, conta);

            if (cc == null)
            {
                mensagemErro = "Conta Invalida";
                return false;
            }

            if (valor <= 0)
            {
                mensagemErro = "O valor do deposito for maior que zero!";
                return false;
            }

            cc.Saldo = cc.Saldo + valor;

            var extrato = new Extrato()
            {
                DataRegistro = DateTime.Now,
                AgenciaId = agencia,
                ContaId = conta,
                Valor = valor,
                Saldo = cc.Saldo,
                Descricao = "Deposito"
            };

            try
            {
                using (var t = new TransactionScope())
                {
                    ContaRepository.Save(cc);
                    ExtratoRepository.Save(extrato);
                    t.Complete();
                }
            }
            catch (Exception ex)
            {
                mensagemErro = "Ocorreu um problema ao fazer o depósito!";
                return false;
            }

            return true;
        }

        public IList<Extrato> Extrato(int agencia, int conta, DateTime dataInicio, DateTime dataFim, out string mensagemErro)
        {
            mensagemErro = "";
            var ag = AgenciaRepository.GetById(agencia);

            if (ag == null)
            {
                mensagemErro = "Agencia Invalida";
                return null;
            }

            var cc = ContaRepository.GetById(agencia, conta);

            if (cc == null)
            {
                mensagemErro = "Conta Invalida";
                return null;
            }

            if(dataInicio > dataFim)
            {
                mensagemErro = "Data inicio deve ser menor que a data de fim!";
                return null;
            }

            if ((dataFim - dataInicio).Days > 120)
            {
                mensagemErro = "O perido nao deve ser superior a 120 dias!";
                return null;
            }

            try
            {
                var extrato = ExtratoRepository.GetByPeriodo(agencia, conta, dataInicio, dataFim);

                var linhaSaldo = new Extrato()
                {
                    Descricao = "Saldo anterior",
                    Saldo = ExtratoRepository.GetSaldoAnterior(agencia, conta, dataInicio, dataFim)
                };

                extrato.Insert(0, linhaSaldo);

                return extrato;
            }
            catch (Exception ex)
            {
                mensagemErro = "Ocorreu um problema ao obter o extrato!";
                return null;
            }
        }

        public decimal Saldo(int agencia, int conta, out string mensagemErro)
        {
            mensagemErro = "";
            var ag = AgenciaRepository.GetById(agencia);

            if (ag == null)
            {
                mensagemErro = "Agencia Invalida";
                return 0;
            }

            var cc = ContaRepository.GetById(agencia, conta);

            if (cc == null)
            {
                mensagemErro = "Conta Invalida";
                return 0;
            }

            return cc.Saldo;
        }

        public bool Saque(int agencia, int conta, decimal valor, out string mensagemErro)
        {
            mensagemErro = "";
            var ag = AgenciaRepository.GetById(agencia);

            if (ag == null)
            {
                mensagemErro = "Agencia Invalida";
                return false;
            }

            var cc = ContaRepository.GetById(agencia, conta);

            if (cc == null)
            {
                mensagemErro = "Conta Invalida";
                return false;
            }

            if (valor <= 0)
            {
                mensagemErro = "O valor do saque precisa ser maior que zero";
                return false;
            }

            if (valor > cc.Saldo)
            {
                mensagemErro = "O valor do saque precisa ser menor ou igual ao saldo da conta";
                return false;
            }

            cc.Saldo = cc.Saldo - valor;

            var extrato = new Extrato()
            {
                DataRegistro = DateTime.Now,
                AgenciaId = agencia,
                ContaId = conta,
                Valor = valor * -1,
                Saldo = cc.Saldo,
                Descricao = "Saque"
            };

            try
            {
                using (var t = new TransactionScope())
                {
                    ContaRepository.Save(cc);
                    ExtratoRepository.Save(extrato);
                    t.Complete();
                }
            }
            catch (Exception ex)
            {
                mensagemErro = "Ocorreu um problema ao fazer o saque!";
                return false;
            }



            return true;
        }

        public bool Transferencia(int agenciaOrigem, int contaOrigem, decimal valor, int agenciaDestino, int contaDestino, out string mensagemErro)
        {
            mensagemErro = "";
            var ag = AgenciaRepository.GetById(agenciaOrigem);

            if (ag == null)
            {
                mensagemErro = "Agencia de origem Invalida";
                return false;
            }

            var cc = ContaRepository.GetById(agenciaOrigem, contaOrigem);

            if (cc == null)
            {
                mensagemErro = "Conta de Origem Invalida";
                return false;
            }

            if (valor <= 0)
            {
                mensagemErro = "O valor deve ser maior que zero!";
                return false;
            }

            if (valor > cc.Saldo)
            {
                mensagemErro = "O valor deve ser menor ou igual ao saldo da conta de origem!";
                return false;
            }

            var ag2 = AgenciaRepository.GetById(agenciaDestino);

            if (ag2 == null)
            {
                mensagemErro = "Agencia de destino invalida";
                return false;
            }

            var cc2 = ContaRepository.GetById(agenciaDestino, contaDestino);

            if (cc2 == null)
            {
                mensagemErro = "Conta de destino inválida!";
                return false;
            }

            

            cc.Saldo = cc.Saldo - valor;

            var extratoOrigem = new Extrato()
            {
                DataRegistro = DateTime.Now,
                AgenciaId = agenciaOrigem,
                ContaId = contaOrigem,
                Valor = valor * -1,
                Saldo = cc.Saldo,
                Descricao = $"Transferencia para AG {agenciaDestino} CC {contaDestino}"
            };

            cc2.Saldo = cc2.Saldo + valor;

            var extratoDestino = new Extrato()
            {
                DataRegistro = DateTime.Now,
                AgenciaId = agenciaDestino,
                ContaId = contaDestino,
                Valor = valor,
                Saldo = cc2.Saldo,
                Descricao = $"Transferencia de AG {agenciaOrigem} CC {contaOrigem}"
            };

            try
            {
                using (var t = new TransactionScope())
                {
                    ContaRepository.Save(cc);
                    ContaRepository.Save(cc2);
                    ExtratoRepository.Save(extratoOrigem);
                    ExtratoRepository.Save(extratoDestino);
                    t.Complete();
                }
            }
            catch (Exception ex)
            {
                mensagemErro = "Ocorreu um problema ao fazer o transferencia!";
                return false;
            }

            return true;
        }
    }
}
