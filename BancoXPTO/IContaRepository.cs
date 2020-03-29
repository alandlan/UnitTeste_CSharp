namespace BancoXPTO
{
    public interface IContaRepository
    {
        Conta GetById(int agencia, int contaId);
        void Save(Conta conta);
    }
}