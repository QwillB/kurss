using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

namespace WarehouseVisualizer.Api.Controllers;

[ApiController]
[Route("mobile")]
public sealed class MobileController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public MobileController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public ContentResult Index()
    {
        return HtmlPage("QR Warehouse", Card("Сканирование QR", "Отсканируйте QR-код материала или ячейки, чтобы открыть мобильную карточку объекта.", string.Empty));
    }

    [HttpGet("material/{code}")]
    public async Task<ContentResult> Material(string code, CancellationToken cancellationToken)
    {
        code = WebUtility.UrlDecode(code).Trim();
        var materialId = TryGetMaterialId(code);

        var material = materialId.HasValue
            ? await _context.Materials.AsNoTracking().FirstOrDefaultAsync(m => m.Id == materialId.Value, cancellationToken)
            : await _context.Materials.AsNoTracking().FirstOrDefaultAsync(m => m.QrCode == code, cancellationToken);

        if (material is null)
        {
            return HtmlPage("Материал не найден", Card("Материал не найден", $"Код: {Html(code)}", "Проверьте QR-код или обновите данные склада."));
        }

        var cell = await _context.WarehouseCells
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.MaterialId == material.Id, cancellationToken);

        var lastHistory = await _context.OperationHistory
            .AsNoTracking()
            .Where(h => h.MaterialId == material.Id || h.MaterialName == material.Name)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var location = cell is null ? "Не размещён" : $"{cell.Row + 1}-{cell.Column + 1}";
        var lowStock = material.Quantity <= 5 ? "<span class=\"badge bad\">Низкий остаток</span>" : "<span class=\"badge ok\">Остаток в норме</span>";
        var historyText = lastHistory is null
            ? "Действий пока нет"
            : $"{Html(lastHistory.ActionType.ToString())}, {Html(lastHistory.Timestamp.ToString("dd.MM.yyyy HH:mm"))}, {Html(lastHistory.UserName)}";

        var body = $"""
            <div class="eyebrow">Материал</div>
            <h1>{Html(material.Name)}</h1>
            <div class="badges">{lowStock}<span class="badge">{Html(material.Status.ToString())}</span></div>
            <div class="grid">
                {Row("Код QR", code)}
                {Row("Категория", material.Type.ToString())}
                {Row("Количество", $"{material.Quantity} {material.Unit}")}
                {Row("Ячейка", location)}
                {Row("ID материала", material.Id.ToString())}
                {Row("Создан", material.CreatedAt.ToString("dd.MM.yyyy HH:mm"))}
            </div>
            <section>
                <h2>Последнее действие</h2>
                <p>{historyText}</p>
            </section>
            """;

        return HtmlPage($"Материал: {material.Name}", Card(string.Empty, string.Empty, body));
    }

    [HttpGet("cell/{code}")]
    public async Task<ContentResult> Cell(string code, CancellationToken cancellationToken)
    {
        code = WebUtility.UrlDecode(code).Trim();
        var location = code.StartsWith("CELL-", StringComparison.OrdinalIgnoreCase)
            ? code[5..]
            : code;

        var parts = location.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var rowNumber) || !int.TryParse(parts[1], out var columnNumber))
        {
            return HtmlPage("Ячейка не найдена", Card("Некорректный код ячейки", $"Код: {Html(code)}", "Ожидается формат CELL-ряд-колонка, например CELL-3-4."));
        }

        var row = rowNumber - 1;
        var column = columnNumber - 1;
        var cell = await _context.WarehouseCells
            .AsNoTracking()
            .Include(c => c.Material)
            .FirstOrDefaultAsync(c => c.Row == row && c.Column == column, cancellationToken);

        if (cell is null)
        {
            return HtmlPage("Ячейка не найдена", Card("Ячейка не найдена", $"Код: {Html(code)}", "Проверьте номер ячейки или сохраните склад в базе данных."));
        }

        var material = cell.Material;
        var status = material is null ? "<span class=\"badge ok\">Свободна</span>" : "<span class=\"badge warn\">Занята</span>";
        var lastHistory = await _context.OperationHistory
            .AsNoTracking()
            .Where(h => h.FromLocation == location || h.ToLocation == location || h.Location == location)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var historyText = lastHistory is null
            ? "Действий по ячейке пока нет"
            : $"{Html(lastHistory.ActionType.ToString())}, {Html(lastHistory.MaterialName)}, {Html(lastHistory.Timestamp.ToString("dd.MM.yyyy HH:mm"))}";

        var materialBlock = material is null
            ? "<p class=\"empty\">В ячейке нет материала.</p>"
            : $"""
                <div class="grid">
                    {Row("Материал", material.Name)}
                    {Row("Категория", material.Type.ToString())}
                    {Row("Количество", $"{material.Quantity} {material.Unit}")}
                    {Row("Статус", material.Status.ToString())}
                    {Row("Код QR", material.QrCode)}
                </div>
                """;

        var body = $"""
            <div class="eyebrow">Ячейка склада</div>
            <h1>{Html(location)}</h1>
            <div class="badges">{status}</div>
            {materialBlock}
            <section>
                <h2>Последнее действие</h2>
                <p>{historyText}</p>
            </section>
            """;

        return HtmlPage($"Ячейка {location}", Card(string.Empty, string.Empty, body));
    }

    private static int? TryGetMaterialId(string code)
    {
        if (code.StartsWith("MAT-", StringComparison.OrdinalIgnoreCase))
        {
            var idPart = code[4..].Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return int.TryParse(idPart, out var id) ? id : null;
        }

        return int.TryParse(code, out var numericId) ? numericId : null;
    }

    private static ContentResult HtmlPage(string title, string body)
    {
        var html = $$"""
            <!doctype html>
            <html lang="ru">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{{Html(title)}}</title>
                <style>
                    :root { color-scheme: dark; font-family: "Segoe UI", Arial, sans-serif; }
                    body { margin: 0; min-height: 100vh; background: #0b1220; color: #e5edf8; }
                    .shell { padding: 18px; max-width: 640px; margin: 0 auto; }
                    .brand { color: #38bdf8; font-weight: 800; letter-spacing: .04em; margin: 10px 0 18px; }
                    .card { background: #111827; border: 1px solid #27364f; border-radius: 18px; padding: 18px; box-shadow: 0 18px 45px rgba(0,0,0,.32); }
                    .eyebrow { color: #93a4b8; font-size: 13px; text-transform: uppercase; letter-spacing: .08em; }
                    h1 { margin: 8px 0 12px; font-size: 28px; line-height: 1.15; }
                    h2 { margin: 22px 0 8px; font-size: 16px; color: #cbd5e1; }
                    p { color: #cbd5e1; line-height: 1.45; }
                    .grid { display: grid; gap: 10px; margin-top: 16px; }
                    .row { display: flex; justify-content: space-between; gap: 14px; padding: 12px; background: #172033; border-radius: 12px; }
                    .label { color: #94a3b8; }
                    .value { color: #f8fafc; font-weight: 650; text-align: right; }
                    .badges { display: flex; flex-wrap: wrap; gap: 8px; margin: 8px 0 12px; }
                    .badge { display: inline-flex; padding: 6px 10px; border-radius: 999px; background: #22304a; color: #bfdbfe; font-size: 13px; font-weight: 700; }
                    .badge.ok { background: #12372a; color: #86efac; }
                    .badge.warn { background: #3a2a12; color: #facc15; }
                    .badge.bad { background: #3b1518; color: #fca5a5; }
                    .empty { padding: 14px; background: #172033; border-radius: 12px; }
                </style>
            </head>
            <body>
                <main class="shell">
                    <div class="brand">SMART WAREHOUSE</div>
                    {{body}}
                </main>
            </body>
            </html>
            """;

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }

    private static string Card(string title, string text, string content)
    {
        var header = string.IsNullOrWhiteSpace(title) ? string.Empty : $"<h1>{Html(title)}</h1>";
        var paragraph = string.IsNullOrWhiteSpace(text) ? string.Empty : $"<p>{Html(text)}</p>";
        return $"<section class=\"card\">{header}{paragraph}{content}</section>";
    }

    private static string Row(string label, string value)
    {
        return $"<div class=\"row\"><span class=\"label\">{Html(label)}</span><span class=\"value\">{Html(value)}</span></div>";
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
