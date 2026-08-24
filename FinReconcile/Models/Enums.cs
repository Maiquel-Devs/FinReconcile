namespace FinReconcile.Models;

/// <summary>
/// Representa o status atual de uma transação financeira dentro do fluxo de conciliação.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// A transação foi registrada ou importada, mas ainda não passou pelo 
    /// motor de conciliação ou aguarda análise.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// A transação foi correspondida com sucesso (automática ou manualmente) 
    /// e a liquidação está confirmada.
    /// </summary>
    Reconciled = 1,

    /// <summary>
    /// O motor de conciliação detectou anomalias (ex: diferença de valor ou 
    /// ausência de par) e separou a transação para auditoria manual.
    /// </summary>
    Divergent = 2
}

/// <summary>
/// Define a estratégia ou método pelo qual a conciliação entre o Livro-Razão 
/// e o extrato bancário foi efetivada.
/// </summary>
public enum MatchType
{
    /// <summary>
    /// Os valores, datas e referências correspondem perfeitamente entre os registros.
    /// </summary>
    Exact = 1,

    /// <summary>
    /// A correspondência foi feita pelo motor automático, mas apresentou pequenas 
    /// diferenças de centavos compensadas pela margem de tolerância (ex: taxas não previstas).
    /// </summary>
    Tolerance = 2,

    /// <summary>
    /// A correspondência foi forçada manualmente por um operador no painel de divergências.
    /// </summary>
    Manual = 3
}