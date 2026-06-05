using PosPlatform.Application.Features.Labels.Dtos;

namespace PosPlatform.Application.Features.Labels;

public interface ILabelService
{
    Task<LabelPrintJobDto> CreateJobAsync(CreateLabelJobRequest request, CancellationToken cancellationToken = default);
    Task<LabelPrintJobDto?> GetPendingJobAsync(CancellationToken cancellationToken = default);
    Task MarkPrintedAsync(Guid jobId, CancellationToken cancellationToken = default);
}
