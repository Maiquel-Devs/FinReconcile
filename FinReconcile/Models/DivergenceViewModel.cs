namespace FinReconcile.Models;

/// <summary>
/// ViewModel responsável por agrupar e transportar os dados de transações 
/// pendentes e divergentes para a interface de conciliação manual.
/// </summary>
public class DivergenceViewModel
{
    /// <summary>
    /// Lista de transações do Livro-Razão (geradas internamente no sistema) 
    /// que ainda não encontraram uma correspondência exata no banco.
    /// </summary>
    public List<InternalTransaction> UnmatchedInternalTransactions { get; set; } = new();

    /// <summary>
    /// Lista de transações importadas do extrato bancário (CSV) 
    /// que não possuem paridade com os registros internos.
    /// </summary>
    public List<BankStatement> UnmatchedBankStatements { get; set; } = new();
}