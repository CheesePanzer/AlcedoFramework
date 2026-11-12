using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.JuiceStore.Backend.Domain;

public sealed class JuiceAggRoot : MasterDataAggregateRootBase
{
    public string Description { get; private set; }

    public string Flavor { get; private set; }

    public int VolumeMl { get; private set; }

    public decimal Price { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsFeatured { get; private set; }

    public int SortOrder { get; private set; }

#pragma warning disable CS8618
    private JuiceAggRoot()
    {
    }
#pragma warning restore CS8618

    public JuiceAggRoot(
        string code,
        string name,
        string description,
        string flavor,
        int volumeMl,
        decimal price,
        string? imageUrl,
        bool isFeatured,
        int sortOrder,
        OperatorRecord creator) : base(code, name, creator)
    {
        Validate(description, flavor, volumeMl, price);
        Description = description;
        Flavor = flavor;
        VolumeMl = volumeMl;
        Price = price;
        ImageUrl = imageUrl;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    public void Update(
        string name,
        string description,
        string flavor,
        int volumeMl,
        decimal price,
        string? imageUrl,
        bool isFeatured,
        int sortOrder,
        OperatorRecord updater)
    {
        Validate(description, flavor, volumeMl, price);
        base.Update(name, updater);
        Description = description;
        Flavor = flavor;
        VolumeMl = volumeMl;
        Price = price;
        ImageUrl = imageUrl;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
    }

    private static void Validate(string description, string flavor, int volumeMl, decimal price)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomErrRuleViolation("Juice description is required");
        }

        if (string.IsNullOrWhiteSpace(flavor))
        {
            throw new DomErrRuleViolation("Juice flavor is required");
        }

        if (volumeMl <= 0)
        {
            throw new DomErrRuleViolation("Juice volume must be greater than zero");
        }

        if (price < 0)
        {
            throw new DomErrRuleViolation("Juice price cannot be negative");
        }
    }
}
