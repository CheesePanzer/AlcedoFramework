using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.Domain.IRepository;
using Alcedo.Framework.Shared.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Repository;

[RegisterAs(typeof(IFormulaRepository))]
internal sealed class FormulaRepository : ReferenceDataRepositoryBase<FormulaAggRoot>, IFormulaRepository
{
    public FormulaRepository(AlcedoDbContext context) : base(context)
    {
    }

    public Task<FormulaAggRoot?> GetByCodeAsync(string code) 
        => DbSet.SingleOrDefaultAsync(f => f.Code == code);
    
    public Task<FormulaAggRoot?> GetByCodeAndVersionAsync(string code, int version)
        => DbSet.SingleOrDefaultAsync(f => f.Code == code && f.Version == version);
    
}