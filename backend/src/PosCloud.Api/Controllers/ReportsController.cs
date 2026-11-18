using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.Reports;
using PosCloud.Application.Features.Reports.Dtos;

namespace PosCloud.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HasPermission(Permissions.ReportsView)]
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var f = (from ?? BusinessTime.TodayLocal).Date;
        var t = (to   ?? BusinessTime.TodayLocal).Date;
        return Ok(await _reports.GetSalesReportAsync(f, t, branchId, ct));
    }

    [HttpGet("sales/export")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var f    = (from ?? BusinessTime.TodayLocal).Date;
        var t    = (to   ?? BusinessTime.TodayLocal).Date;
        var bytes = await _reports.ExportSalesReportAsync(f, t, branchId, ct);
        var name  = $"savdolar-hisoboti-{f:yyyy-MM-dd}-{t:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    /// <summary>Sotilgan tovarlar logi (har SaleItem — qator). source: Pos|Online|All, type: Sale|Refund|All.</summary>
    [HttpGet("sold-items")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<ActionResult<SoldItemsPageDto>> GetSoldItems([FromQuery] SoldItemsQuery query, CancellationToken ct)
        => Ok(await _reports.GetSoldItemsAsync(query, ct));

    [HttpGet("sold-items/export")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> ExportSoldItems([FromQuery] SoldItemsQuery query, CancellationToken ct)
    {
        var bytes = await _reports.ExportSoldItemsAsync(query, ct);
        var f = (query.From ?? BusinessTime.TodayLocal).Date;
        var t = (query.To ?? query.From ?? BusinessTime.TodayLocal).Date;
        var name = $"sotuvlar-logi-{f:yyyy-MM-dd}-{t:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet("stock")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> GetStockReport([FromQuery] Guid? branchId, CancellationToken ct)
        => Ok(await _reports.GetStockReportAsync(branchId, ct));

    [HttpGet("stock/export")]
    [HasPermission(Permissions.ReportsView)]
    public async Task<IActionResult> ExportStockReport([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var bytes = await _reports.ExportStockReportAsync(branchId, ct);
        var name  = $"qoldiqlar-{BusinessTime.TodayLocal:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
}
