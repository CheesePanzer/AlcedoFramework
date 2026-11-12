using Alcedo.Core.Domain.IRepositoryBase;

namespace Alcedo.Framework.FormulaEngine.Domain.IRepository;

public interface IHistoryFormulaStore : IEntityStoreBase<HistoryFormulaEntity>
{
    Task<HistoryFormulaEntity?> GetByCodeAndVersion(string code, int version);
}