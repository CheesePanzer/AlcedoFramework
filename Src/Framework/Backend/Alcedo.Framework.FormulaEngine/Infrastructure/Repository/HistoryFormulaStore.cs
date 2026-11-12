using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.RepositoryBase;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.Domain.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Repository;

[RegisterAs(typeof(IHistoryFormulaStore))]
internal sealed class HistoryFormulaStore : EntityStoreBase<HistoryFormulaEntity>, IHistoryFormulaStore
{
    public HistoryFormulaStore(AlcedoDbContext context) : base(context)
    {
    }

    public Task<HistoryFormulaEntity?> GetByCodeAndVersion(string code, int version)
        => DbSet.SingleOrDefaultAsync(f => f.Code == code && f.OriginalVersion == version);
}