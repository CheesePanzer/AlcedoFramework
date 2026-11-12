using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.FormulaEngine.Domain.IRepository;

public interface IFormulaRepository : IReferenceDataRepositoryBase<FormulaAggRoot>
{
    Task<FormulaAggRoot?> GetByCodeAsync(string code);

    Task<FormulaAggRoot?> GetByCodeAndVersionAsync(string code, int version);
}