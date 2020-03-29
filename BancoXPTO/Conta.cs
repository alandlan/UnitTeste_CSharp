namespace BancoXPTO
{
    public class Conta
    {
        public int Id { get; set; }
        public int AgenciaId { get; set; }
        public string NomeCliente { get; set; }
        public string CPFCliente { get; set; }
        public decimal Saldo { get; set; }
    }
}