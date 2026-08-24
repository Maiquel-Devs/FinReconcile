using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

/// <summary>
/// Representa o vínculo efetivado (conciliação) entre uma transação gerada 
/// internamente e um registro compensado no extrato bancário.
/// </summary>
public class ReconciliationMatch
{
    /// <summary>
    /// Identificador único do registro de conciliação.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Chave estrangeira para o lançamento no Livro-Razão.
    /// </summary>
    public int InternalTransactionId { get; set; }
    
    /// <summary>
    /// Propriedade de navegação para a transação interna.
    /// </summary>
    public InternalTransaction InternalTransaction { get; set; } = null!;

    /// <summary>
    /// Chave estrangeira para a transação importada via CSV.
    /// </summary>
    public int BankStatementId { get; set; }
    
    /// <summary>
    /// Propriedade de navegação para o registro do extrato bancário.
    /// </summary>
    public BankStatement BankStatement { get; set; } = null!;

    /// <summary>
    /// Estratégia ou tipo de regra utilizada para criar este vínculo 
    /// (ex: Exato, Tolerância de centavos ou Manual).
    /// </summary>
    public MatchType MatchType { get; set; }

    /// <summary>
    /// Diferença monetária identificada no momento da conciliação. 
    /// Geralmente reflete tarifas bancárias ocultas ou pequenos ajustes justificados.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// Justificativa ou observação opcional. Comumente preenchida 
    /// durante auditorias ou conciliações manuais por um operador.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Data e hora em que as transações foram vinculadas com sucesso.
    /// </summary>
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
}