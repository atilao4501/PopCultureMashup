using PopCultureMashup.Domain.Entities;

namespace PopCultureMashup.Domain.Abstractions;

public interface IFeedbackRepository
{
    Task AddAsync(Feedback feedback, CancellationToken ct = default);
}