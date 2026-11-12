using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Alcedo.Core.Presentation.ValueAtomModel;

public class ValueAtomModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (typeof(ValueAtom).IsAssignableFrom(context.Metadata.ModelType))
        {
            return new ValueAtomModelBinder();
        }

        return null;
    }
}