using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.EngineCore;
using Alcedo.Framework.FormulaEngine.FormulaCenter;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.FormulaEngine.Infrastructure.Seeder;

internal static class FormulaSeeder
{
    public static void Seed(AlcedoDbContext context)
    {
        var dbSet = context.AggRoot<FormulaAggRoot>();
        var metaDataList = FormulaStoreCenter.GetAll();

        CheckOrphanedCodes(dbSet, metaDataList);

        foreach (var metaData in metaDataList)
        {
            if (metaData.IsObsolete)
            {
                ArchiveIfExists(context, dbSet, metaData);
                continue;
            }

            SyncActiveFormula(context, dbSet, metaData);
        }

        context.SaveChanges();
    }

    private static void CheckOrphanedCodes(DbSet<FormulaAggRoot> dbSet,
        IReadOnlyCollection<FormulaMetaData> metaDataList)
    {
        var activeCodes = metaDataList.Where(m => !m.IsObsolete).Select(m => m.Code).ToHashSet();

        var orphanedCodes = dbSet
            .Select(f => f.Code)
            .AsEnumerable()
            .Where(code => !activeCodes.Contains(code))
            .ToList();

        if (orphanedCodes.Count != 0)
        {
            throw new InvalidOperationException(
                $"Formula classes missing without [Obsolete]: {string.Join(", ", orphanedCodes)}. " +
                $"Check git history and add [Obsolete] to the corresponding Formula subclass.");
        }
    }

    private static void ArchiveIfExists(AlcedoDbContext context, DbSet<FormulaAggRoot> dbSet, FormulaMetaData metaData)
    {
        var ent = dbSet.SingleOrDefault(f => f.Code == metaData.Code);

        if (ent is null) return;

        context.RawEntity<HistoryFormulaEntity>().Add(new HistoryFormulaEntity(ent));
        dbSet.Remove(ent);
    }

    private static void SyncActiveFormula(AlcedoDbContext context, DbSet<FormulaAggRoot> dbSet,
        FormulaMetaData metaData)
    {
        var existing = dbSet
            .Include(f => f.InputParameters)
            .Include(f => f.OutputParameters)
            .SingleOrDefault(f => f.Code == metaData.Code);

        if (existing is null)
        {
            AddNewFormula(context, dbSet, metaData);
        }
        else
        {
            ValidateStructure(metaData, existing);
        }
    }

    private static void AddNewFormula(AlcedoDbContext context, DbSet<FormulaAggRoot> dbSet, FormulaMetaData metaData)
    {
        var hasArchive = context.RawEntity<HistoryFormulaEntity>().Any(h => h.Code == metaData.Code);

        if (hasArchive)
        {
            throw new InvalidOperationException(
                $"Formula '{metaData.Code}' has archived history. " +
                $"Reusing this code would create version ambiguity — define a new Formula subclass with a different code.");
        }

        var flattenedInput = FormulaParamFlattener.FlattenInput(metaData.InputType);
        var flattenedOutput = FormulaParamFlattener.FlattenOutput(metaData.OutputType);
        dbSet.Add(new FormulaAggRoot(metaData.Code, metaData.Description, flattenedInput, flattenedOutput));
    }

    private static void ValidateStructure(FormulaMetaData metaData, FormulaAggRoot existing)
    {
        var flattenedInput = FormulaParamFlattener.FlattenInput(metaData.InputType);
        var flattenedOutput = FormulaParamFlattener.FlattenOutput(metaData.OutputType);

        var inputMismatch = !flattenedInput.OrderBy(p => p.VarName).Select(p => (p.VarName, p.VarType))
            .SequenceEqual(existing.InputParameters.OrderBy(p => p.VarName).Select(p => (p.VarName, p.VarType)));

        var outputMismatch = !flattenedOutput.OrderBy(p => p.VarName).Select(p => (p.VarName, p.VarType))
            .SequenceEqual(existing.OutputParameters.OrderBy(p => p.VarName).Select(p => (p.VarName, p.VarType)));

        if (inputMismatch || outputMismatch)
        {
            throw new InvalidOperationException(
                $"Formula '{metaData.Code}' structure mismatch. " +
                $"TInput/TOutput has changed — create a new Formula subclass instead.");
        }
    }
}