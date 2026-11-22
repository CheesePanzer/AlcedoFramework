using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.Helpers;

namespace Alcedo.Frontend.Abstraction.Test;

public class AutoValidatorTests
{
    public sealed class RequiredModel { public string Name { get; set; } = ""; public Guid Id { get; set; } }
    public sealed class OptionalModel { public string? Note { get; set; } public int? Count { get; set; } }
    public sealed class IgnoredModel { [AutoValidateIgnore] public string Name { get; set; } = ""; }
    public sealed class ContactModel
    {
        [AutoValidateEmail("email-invalid")] public string Email { get; set; } = "";
        [AutoValidatePhone("phone-invalid")] public string Phone { get; set; } = "";
    }
    public sealed class RangeModel { [AutoValidateRange(1, 10, "out-of-range")] public int Count { get; set; } }
    public sealed class LengthModel { [AutoValidateStringLength(2, 4, "invalid-length")] public string Name { get; set; } = ""; }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ImplicitRequiredStringRejectsBlank(string name)
    {
        var errors = AutoValidator.Validate(new RequiredModel { Name = name, Id = Guid.NewGuid() });
        Assert.Equal("Name cannot be empty.", Assert.Single(errors));
    }

    [Fact]
    public void Validate_ImplicitRequiredGuidRejectsEmpty()
    {
        var errors = AutoValidator.Validate(new RequiredModel { Name = "valid" });
        Assert.Equal("Id is required.", Assert.Single(errors));
    }

    [Fact]
    public void Validate_ValidRequiredFieldsHaveNoErrors() =>
        Assert.Empty(AutoValidator.Validate(new RequiredModel { Name = "valid", Id = Guid.NewGuid() }));

    [Fact]
    public void Validate_OptionalNullFieldsHaveNoErrors() => Assert.Empty(AutoValidator.Validate(new OptionalModel()));

    [Fact]
    public void Validate_IgnoreSkipsImplicitRequiredRule() => Assert.Empty(AutoValidator.Validate(new IgnoredModel()));

    [Fact]
    public void Validate_ReportsAllInvalidContactFields()
    {
        var errors = AutoValidator.Validate(new ContactModel { Email = "invalid", Phone = "123" });
        Assert.Equal(2, errors.Count);
        Assert.Contains("email-invalid", errors);
        Assert.Contains("phone-invalid", errors);
    }

    [Fact]
    public void Validate_ValidContactHasNoErrors() => Assert.Empty(AutoValidator.Validate(
        new ContactModel { Email = "tester@example.com", Phone = "+8613812345678" }));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Validate_NumericRangeIncludesBothBoundaries(int count, bool valid) =>
        Assert.Equal(valid, AutoValidator.Validate(new RangeModel { Count = count }).Count == 0);

    [Theory]
    [InlineData("a", false)]
    [InlineData("ab", true)]
    [InlineData("abcd", true)]
    [InlineData("abcde", false)]
    public void Validate_StringLengthIncludesBothBoundaries(string name, bool valid) =>
        Assert.Equal(valid, AutoValidator.Validate(new LengthModel { Name = name }).Count == 0);
}
