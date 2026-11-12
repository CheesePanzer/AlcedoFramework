using Alcedo.Core.Abstractions;
using Alcedo.Core.Presentation.ValueAtomModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Alcedo.Core.Presentation.Test;

public record TestAtomValue : ValueAtom
{
    public TestAtomValue(string value) : base(value)
    {
    }
}

public class ValueAtomModelBinderTests
{
    private static readonly EmptyModelMetadataProvider MetadataProvider = new();

    private static DefaultModelBindingContext CreateContext(string modelName, Type modelType, string? value)
    {
        var provider = new TestValueProvider(modelName, value);
        return new DefaultModelBindingContext
        {
            ModelName = modelName,
            ModelMetadata = MetadataProvider.GetMetadataForType(modelType),
            ValueProvider = provider,
            ModelState = new ModelStateDictionary()
        };
    }

    [Fact]
    public async Task BindModelAsync_ValuePresent_CreatesInstance()
    {
        var binder = new ValueAtomModelBinder();
        var context = CreateContext("status", typeof(TestAtomValue), "Active");

        await binder.BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        var model = Assert.IsType<TestAtomValue>(context.Result.Model);
        Assert.Equal("Active", model.Value);
    }

    [Fact]
    public async Task BindModelAsync_EmptyValue_DoesNotSetModel()
    {
        var binder = new ValueAtomModelBinder();
        var context = CreateContext("status", typeof(TestAtomValue), "");

        await binder.BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModelAsync_NullValue_DoesNotSetModel()
    {
        var binder = new ValueAtomModelBinder();
        var context = CreateContext("status", typeof(TestAtomValue), null);

        await binder.BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModelAsync_MissingKey_DoesNotSetModel()
    {
        var binder = new ValueAtomModelBinder();
        var provider = new TestValueProvider("other", "Active");
        var context = new DefaultModelBindingContext
        {
            ModelName = "status",
            ModelMetadata = MetadataProvider.GetMetadataForType(typeof(TestAtomValue)),
            ValueProvider = provider,
            ModelState = new ModelStateDictionary()
        };

        await binder.BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModelAsync_InvalidConstructor_AddsModelError()
    {
        var binder = new ValueAtomModelBinder();
        var context = CreateContext("status", typeof(AtomWithStrictValidation), "Active");

        await binder.BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
        Assert.True(context.ModelState.ContainsKey("status"));
        Assert.NotEmpty(context.ModelState["status"]!.Errors);
    }

    private class TestValueProvider : IValueProvider
    {
        private readonly string _key;
        private readonly string? _value;

        public TestValueProvider(string key, string? value)
        {
            _key = key;
            _value = value;
        }

        public bool ContainsPrefix(string prefix) => _key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        public ValueProviderResult GetValue(string key)
            => string.Equals(key, _key, StringComparison.OrdinalIgnoreCase)
                ? new ValueProviderResult(_value)
                : ValueProviderResult.None;
    }

    public record AtomWithStrictValidation : ValueAtom
    {
        public AtomWithStrictValidation(string value) : base(value)
        {
            if (value != "Valid")
            {
                throw new InvalidOperationException("Only Valid is allowed");
            }
        }
    }
}