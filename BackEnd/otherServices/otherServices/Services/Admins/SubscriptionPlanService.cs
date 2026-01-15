using otherServices.Models;
using otherServices.Models.DTOs.Subscriptions;
using otherServices.Repositories;
using otherServices.Services.Interfaces.Admins;

namespace otherServices.Services.Admins
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SubscriptionPlanService> _logger;

        public SubscriptionPlanService(IUnitOfWork uow, ILogger<SubscriptionPlanService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<SubscriptionPlanResponseDto?> GetSubscriptionPlanByIdAsync(long id)
        {
            try
            {
                var plan = await _uow.SubscriptionPlans.GetByIdAsync(id);
                if (plan == null) return null;

                return MapToDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription plan {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<SubscriptionPlanResponseDto>> GetAllSubscriptionPlansAsync()
        {
            try
            {
                var plans = await _uow.SubscriptionPlans.GetAllAsync();
                return plans.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all subscription plans");
                throw;
            }
        }

        public async Task<SubscriptionPlanResponseDto> AddSubscriptionPlanAsync(AddSubscriptionPlanDto dto)
        {
            try
            {
                if (dto.Price <= 0)
                    throw new ArgumentException("Price must be greater than zero.");

                var plan = new SubscriptionPlan
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    DurationInMonths = (int)dto.Duration,
                    Price = dto.Price,
                    IsActive = true
                };

                await _uow.SubscriptionPlans.AddAsync(plan);
                await _uow.CompleteAsync();

                _logger.LogInformation("Subscription plan {Name} added successfully", plan.Name);

                return MapToDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding subscription plan");
                throw;
            }
        }

        public async Task<SubscriptionPlanResponseDto?> UpdateSubscriptionPlanAsync(long id, UpdateSubscriptionPlanDto dto)
        {
            try
            {
                var plan = await _uow.SubscriptionPlans.GetByIdAsync(id);
                if (plan == null) return null;

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    plan.Name = dto.Name;

                if (!string.IsNullOrWhiteSpace(dto.Description))
                    plan.Description = dto.Description;

                if (dto.Duration.HasValue)
                    plan.DurationInMonths = (int)dto.Duration.Value;

                if (dto.Price.HasValue)
                {
                    if (dto.Price <= 0)
                        throw new ArgumentException("Price must be greater than zero.");

                    plan.Price = dto.Price.Value;
                }

                await _uow.CompleteAsync();

                _logger.LogInformation("Subscription plan {Id} updated successfully", id);

                return MapToDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating subscription plan {Id}", id);
                throw;
            }
        }

        private static SubscriptionPlanResponseDto MapToDto(SubscriptionPlan plan)
        {
            return new SubscriptionPlanResponseDto
            {
                Id = plan.SubscriptionPlanId,
                Name = plan.Name,
                Description = plan.Description,
                DurationInMonths = plan.DurationInMonths,
                Price = plan.Price
            };
        }
    }
}
