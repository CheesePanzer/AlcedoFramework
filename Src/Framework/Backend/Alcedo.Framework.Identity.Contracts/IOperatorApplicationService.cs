using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Contracts;

public interface IOperatorModuleService
{
    /// <summary>
    /// Used To Get Current Enabled Operator, If the oper is disabled or doesn't exist, this will throw an exception
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<OperatorDto> GetCurrentOperatorAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Used To Get Current Enabled Operator, If the oper is disabled or doesn't exist, this will return null
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<OperatorDto?> TryGetCurrentOperatorAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Used To Get Current Enabled Operator, If the oper is disabled or doesn't exist, this will throw an exception
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<OperatorDtoWithRoles> GetCurrentOperatorWithRolesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Used To Get Current Enabled Operator, If the oper is disabled or doesn't exist, this will return null
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<OperatorDtoWithRoles?> TryGetCurrentOperatorWithRolesAsync(Guid userId, CancellationToken ct = default);
}