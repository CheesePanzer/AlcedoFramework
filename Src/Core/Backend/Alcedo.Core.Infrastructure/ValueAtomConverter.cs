using Alcedo.Core.Abstractions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alcedo.Core.Infrastructure;

public class ValueAtomConverter<TImpl> : ValueConverter<TImpl, string>
    where TImpl : ValueAtom
{
    public ValueAtomConverter()
        : base(
            v => v.Value,
            v => (TImpl)Activator.CreateInstance(typeof(TImpl), v)!)
    {
    }
}