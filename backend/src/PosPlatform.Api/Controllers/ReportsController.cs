using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Reports;

namespace PosPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to   ?? DateTime.Today).Date;
        return Ok(await _reports.GetSalesReportAsync(f, t, branchId, ct));
    }

    [HttpGet("sales/export")]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var f    = (from ?? DateTime.Today).Date;
        var t    = (to   ?? DateTime.Today).Date;
        var bytes = await _reports.ExportSalesReportAsync(f, t, branchId, ct);
        var name  = $"savdolar-hisoboti-{f:yyyy-MM-dd}-{t:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport(CancellationToken ct)
        => Ok(await _reports.GetStockReportAsync(ct));

    [HttpGet("stock/export")]
    public async Task<IActionResult> ExportStockReport(CancellationToken ct)
    {
        var bytes = await _reports.ExportStockReportAsync(ct);
        var name  = $"qoldiqlar-{DateTime.Today:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
}
