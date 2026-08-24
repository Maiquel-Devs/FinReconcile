namespace FinReconcile.Services;

/// <summary>
/// Contrato que define as operações do motor de conciliação bancária, 
/// incluindo processamento de arquivos, rotinas automáticas e auditoria manual.
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// Lê e processa um arquivo CSV de extrato bancário, convertendo as linhas 
    /// em entidades de banco de dados e agrupando-as sob um mesmo lote (Batch).
    /// </summary>
    /// <param name="csvStream">Stream contendo os dados do arquivo CSV em memória.</param>
    /// <returns>O identificador único (BatchId) gerado para este lote de importação.</returns>
    Task<Guid> ProcessStatementCsvAsync(Stream csvStream);

    /// <summary>
    /// Executa o motor de conciliação automática para um lote específico de extratos, 
    /// cruzando os dados importados com o Livro-Razão interno.
    /// </summary>
    /// <param name="batchId">Identificador do lote de importação retornado pelo processamento do CSV.</param>
    Task RunReconciliationAsync(Guid batchId);

    /// <summary>
    /// Efetiva manualmente uma conciliação entre um registro interno e um registro do extrato, 
    /// ignorando as regras estritas do motor automático.
    /// </summary>
    /// <param name="internalTransactionId">ID da transação pendente no Livro-Razão.</param>
    /// <param name="bankStatementId">ID da transação pendente no extrato bancário.</param>
    /// <param name="note">Justificativa ou anotação para auditoria da operação manual.</param>
    Task ManualMatchAsync(int internalTransactionId, int bankStatementId, string note);
}