using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Abstraction.BackednInterface;

public interface IReferenceController<TDto, in TSearch> : IPagedQueryController<TDto, TSearch>
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{
}