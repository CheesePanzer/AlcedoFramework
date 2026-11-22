using Alcedo.Framework.FormulaEngine.Contract.Abstraction;
using Alcedo.Framework.FormulaEngine.Domain;
using Alcedo.Framework.FormulaEngine.EngineCore;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.FormulaEngine.Test;

public class FormulaMetadataTests
{
    public sealed class Address { public decimal Cost { get; set; } }
    public sealed class Input
    {
        [Param("Price", "Unit price")] public decimal Amount { get; set; }
        public Address Shipping { get; set; } = new();
    }
    public sealed class InvalidInput { [Param("Flat")] public Address Shipping { get; set; } = new(); }

    [Fact]
    public void FlattenInput_PreservesAliasDescriptionAndNestedPath()
    {
        var parameters = FormulaParamFlattener.FlattenInput<Input>();
        var price = Assert.Single(parameters, p => p.VarName == "In_Price");
        Assert.Equal("Unit price", price.VarDescription);
        Assert.Equal("decimal", price.VarType);
        Assert.Contains(parameters, p => p.VarName == "In_Shipping_Cost");
    }

    [Fact]
    public void FlattenOutput_DoesNotAddInputPrefix() =>
        Assert.Equal("Cost", Assert.Single(FormulaParamFlattener.FlattenOutput<Address>()).VarName);

    [Fact]
    public void FlattenInput_RejectsAliasOnComplexObject() =>
        Assert.Throws<InvalidOperationException>(() => FormulaParamFlattener.FlattenInput<InvalidInput>());

    [Theory]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(decimal?), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(Address), false)]
    public void IsPrimitive_DistinguishesSupportedLeafTypes(Type type, bool expected) =>
        Assert.Equal(expected, FormulaParamFlattener.IsPrimitive(type));

    private static FormulaAggRoot Formula() => new("PRICE", "Price calculation",
        [new("In_Amount", "decimal", "Amount")], [new("Total", "decimal", "Total")]);
    private static OperatorRecord Oper() => new(Guid.NewGuid(), "TEST", "Tester");

    [Fact]
    public void UpdateInputParams_ChangesOnlySelectedParameterAndTracksOperator()
    {
        var formula = Formula();
        var input = Assert.Single(formula.InputParameters);
        var oper = Oper();
        formula.UpdateInputParams([(input.Id, "self >= 0", "1")], oper);
        Assert.Equal(formula.Id, input.FormulaId);
        Assert.Equal("self >= 0", input.ValidCondition);
        Assert.Equal("1", input.DefaultValue);
        Assert.Equal(oper, formula.Updater);
        Assert.Equal("In_Amount", input.VarName);
    }

    [Fact]
    public void UpdateInputParams_RejectsUnknownParameterId()
    {
        var formula = Formula();
        Assert.Throws<InvalidOperationException>(() => formula.UpdateInputParams([(Guid.NewGuid(), null, "1")], Oper()));
    }

    [Fact]
    public void ReplaceExpressions_RemovesOldVariablesInsteadOfAppending()
    {
        var formula = Formula();
        formula.CreateOrUpdateExpressions([(1, "Old", "1", null)], Oper());
        formula.CreateOrUpdateExpressions([(1, "New", "2", "replacement")], Oper());
        var expression = Assert.Single(formula.Expressions);
        Assert.Equal("New", expression.VarName);
        Assert.Equal(formula.Id, expression.FormulaId);
        Assert.Equal("replacement", expression.Note);
    }

    [Fact]
    public void UpdateOutputParams_ChangesExpressionAndTracksOperator()
    {
        var formula = Formula();
        var output = Assert.Single(formula.OutputParameters);
        var oper = Oper();
        formula.UpdateOutputParams([(output.Id, "In_Amount * 2")], oper);
        Assert.Equal("In_Amount * 2", output.Expression);
        Assert.Equal(oper, formula.Updater);
    }
}
