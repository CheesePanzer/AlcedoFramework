using Alcedo.Framework.FormulaEngine.EngineCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alcedo.Framework.FormulaEngine.Test;

public class EngineCoreTests
{
    public sealed class Input { public decimal? Amount { get; set; } }
    public sealed class Output { public decimal Total { get; set; } public Detail? Detail { get; set; } }
    public sealed class Detail { public decimal Total { get; set; } }

    private static EngineCore<Input, Output> Create(Input input,
        IReadOnlyCollection<ExpressionParameter>? expressions = null,
        IReadOnlyCollection<OutputParameter>? outputs = null,
        IReadOnlyCollection<InputParameter>? inputs = null) => new(input, "PRICE", 7,
        inputs ?? [new() { VarName = "In_Amount", VarType = "decimal" }],
        expressions ?? [], outputs ?? [Out("Total", "In_Amount * 2")],
        NullLogger<EngineCore<Input, Output>>.Instance);

    private static OutputParameter Out(string name, string expression) =>
        new() { VarName = name, VarType = "decimal", Expression = expression };

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, 24)]
    [InlineData(-3, -6)]
    public async Task ParseAsync_ComputesOutputAndReturnsFormulaIdentity(int amount, int expected)
    {
        var result = await Create(new() { Amount = amount }).ParseAsync();
        Assert.Equal((decimal)expected, result.Output.Total);
        Assert.Equal("PRICE", result.FormulaCode);
        Assert.Equal(7, result.FormulaVersion);
        Assert.NotEqual(Guid.Empty, result.TaskId);
    }

    [Fact]
    public async Task ParseAsync_SortsIntermediateExpressionsBeforeEvaluatingDependencies()
    {
        var engine = Create(new() { Amount = 5 },
            [new() { Order = 2, VarName = "Final", Expression = "Base + 3" },
             new() { Order = 1, VarName = "Base", Expression = "In_Amount * 2" }],
            [Out("Total", "Final")]);
        Assert.Equal(13m, (await engine.ParseAsync()).Output.Total);
    }

    [Fact]
    public async Task ParseAsync_NullInputUsesConfiguredDefault()
    {
        var engine = Create(new(), inputs:
            [new() { VarName = "In_Amount", VarType = "decimal", DefaultValue = "5" }]);
        Assert.Equal(10m, (await engine.ParseAsync()).Output.Total);
    }

    [Fact]
    public async Task ParseAsync_ProvidedValueTakesPrecedenceOverDefault()
    {
        var engine = Create(new() { Amount = 3 }, inputs:
            [new() { VarName = "In_Amount", VarType = "decimal", DefaultValue = "5" }]);
        Assert.Equal(6m, (await engine.ParseAsync()).Output.Total);
    }

    [Fact]
    public async Task ParseAsync_RejectsInputThatFailsValidationCondition()
    {
        var engine = Create(new() { Amount = -1 }, inputs:
            [new() { VarName = "In_Amount", VarType = "decimal", ValidCondition = "self >= 0" }]);
        await Assert.ThrowsAsync<FormulaValidationException>(() => engine.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_AcceptsInputThatPassesValidationCondition()
    {
        var engine = Create(new() { Amount = 2 }, inputs:
            [new() { VarName = "In_Amount", VarType = "decimal", ValidCondition = "self >= 0" }]);
        Assert.Equal(4m, (await engine.ParseAsync()).Output.Total);
    }

    [Fact]
    public async Task ParseAsync_RejectsInputMetadataMissingFromDto()
    {
        var engine = Create(new(), inputs: [new() { VarName = "In_Missing", VarType = "decimal" }]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_RejectsIntermediateVariableThatShadowsInput()
    {
        var engine = Create(new() { Amount = 1 },
            [new() { Order = 1, VarName = "In_Amount", Expression = "1" }]);
        await Assert.ThrowsAsync<FormulaExecutionException>(() => engine.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_RejectsDuplicateOutputVariables()
    {
        var engine = Create(new() { Amount = 1 }, outputs: [Out("Total", "1"), Out("Total", "2")]);
        await Assert.ThrowsAsync<FormulaExecutionException>(() => engine.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_ConstructsMissingNestedOutputObjects()
    {
        var result = await Create(new() { Amount = 4 }, outputs: [Out("Detail_Total", "In_Amount")]).ParseAsync();
        Assert.NotNull(result.Output.Detail);
        Assert.Equal(4m, result.Output.Detail.Total);
    }

    [Fact]
    public async Task ParseAsync_RejectsOutputWithoutMatchingProperty()
    {
        var engine = Create(new() { Amount = 1 }, outputs: [Out("Missing", "1")]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ParseAsync());
    }
}
