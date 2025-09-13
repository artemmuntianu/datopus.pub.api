using datopus.Api.DTOs.Subscriptions;

namespace datopus.Application.Services.Subscriptions;

public interface IPlanService
{
    Task<PlanResponse?> GetPlanByIdAsync(string productId);

    Task<IEnumerable<PlanResponse>?> GetPlansAsync();
}
