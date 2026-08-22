using FinReconcile.DTOs;
using FinReconcile.Models;

namespace FinReconcile.Services;

public interface IReconciliationService
{
    Task<Guid> ProcessStatementCsvAsync(Stream csvStream);
    Task RunReconciliationAsync(Guid batchId);
}