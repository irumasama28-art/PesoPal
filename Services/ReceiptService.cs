using System.Net;
using System.Text;
using CashLedgerAndroid.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace CashLedgerAndroid.Services;

public static class ReceiptService
{
    private const string Css =
        "*{box-sizing:border-box}body{font-family:Arial,Helvetica,sans-serif;background:#eef3fa;color:#0b2a5b;margin:0;padding:16px}" +
        ".receipt{width:100%;max-width:440px;margin:0 auto;background:#fff;border:1px solid #d6dfec;border-radius:18px;overflow:hidden}" +
        ".top{background:#0066f0;color:#fff;padding:24px 20px;text-align:center}" +
        "h1{font-size:23px;line-height:1.25;margin:0 0 7px;overflow-wrap:anywhere}" +
        ".subtitle{font-size:11px;letter-spacing:1px;color:#e3efff}" +
        ".body{padding:20px}" +
        ".badge{display:inline-block;border-radius:20px;padding:8px 14px;font-weight:bold;font-size:12px;letter-spacing:.5px;margin:0 0 14px}" +
        ".line{display:flex;justify-content:space-between;align-items:flex-start;gap:14px;padding:11px 0;border-bottom:1px dashed #d3dcea;font-size:13px}" +
        ".line span{color:#5b6b84;flex:1}.line strong{text-align:right;max-width:58%;overflow-wrap:anywhere}" +
        ".section{font-size:11px;font-weight:bold;color:#5b6b84;text-transform:uppercase;letter-spacing:.8px;margin-top:18px}" +
        ".footer{text-align:center;margin-top:22px;font-size:11px;line-height:1.6;color:#5b6b84}" +
        ".actions{display:flex;justify-content:center;margin:16px auto;max-width:440px}" +
        "button{padding:13px 20px;border:0;border-radius:10px;background:#0066f0;color:#fff;font-size:15px;font-weight:bold;min-height:46px}" +
        "@media(max-width:360px){body{padding:8px}.body{padding:14px}.line{font-size:12px;gap:8px}h1{font-size:20px}}" +
        "@media print{@page{size:auto;margin:10mm}body{background:#fff;padding:0;-webkit-print-color-adjust:exact;print-color-adjust:exact}" +
        ".receipt{max-width:none;border:0;border-radius:0}.top{background:#fff!important;color:#0b2a5b;border-bottom:2px solid #0066f0;padding:14px}" +
        ".subtitle{color:#5b6b84}.actions{display:none}}";

    private static string H(string? value) => WebNetEncode(value ?? "");
    private static string WebNetEncode(string value) => WebUtility.HtmlEncode(value);

    private static string Line(string label, string value) =>
        "<div class=\"line\"><span>" + H(label) + "</span><strong>" + H(value) + "</strong></div>";

    public static string BuildHtml(AppSettings settings, TransactionRecord t)
    {
        var isIn = t.IsCashIn;
        var badgeColors = t.IsVoided
            ? "color:#c62828;background:#fdecec"
            : (isIn ? "color:#087f49;background:#e3f5ec" : "color:#0a47be;background:#e3efff");
        var status = t.IsVoided ? "VOIDED" : "COMPLETED";
        var customer = string.IsNullOrWhiteSpace(t.CustomerName) ? "Walk-in customer" : t.CustomerName;
        var reference = string.IsNullOrWhiteSpace(t.ReferenceNumber) ? "—" : t.ReferenceNumber;
        var notes = string.IsNullOrWhiteSpace(t.Notes) ? "—" : t.Notes;

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta name=\"color-scheme\" content=\"light\">");
        sb.Append("<title>Cash Ledger Receipt TX-").Append(t.Id.ToString("000000")).Append("</title><style>").Append(Css).Append("</style></head><body>");
        sb.Append("<article class=\"receipt\"><header class=\"top\"><h1>PesoPal</h1>");
        sb.Append("<div class=\"subtitle\">CASH TRANSACTION RECEIPT</div></header><main class=\"body\">");
        sb.Append("<div style=\"text-align:center\"><div class=\"badge\" style=\"").Append(badgeColors).Append("\">");
        sb.Append(H(t.Type.ToUpperInvariant())).Append(" · ").Append(status).Append("</div></div>");
        sb.Append(Line("Receipt number", "TX-" + t.Id.ToString("000000")));
        sb.Append(Line("Date and time", t.CreatedAt.ToString("MMM d, yyyy h:mm tt", Fmt.Inv)));
        sb.Append(Line("Customer", customer));
        sb.Append(Line("Reference number", reference));
        sb.Append("<div class=\"section\">Transaction details</div>");
        sb.Append(Line("Transaction amount", Fmt.Money(t.Amount)));
        sb.Append(Line("Service fee", Fmt.Money(t.Fee)));
        if (isIn)
        {
            sb.Append(Line("Cash received from customer", Fmt.Money(t.Amount + t.Fee)));
            sb.Append(Line("E-wallet amount sent", Fmt.Money(t.Amount)));
        }
        else
        {
            sb.Append(Line("Cash paid to customer", Fmt.Money(t.Amount)));
            sb.Append(Line("Fee collected separately", Fmt.Money(t.Fee)));
        }
        sb.Append(Line("Notes", notes));
        if (t.IsVoided)
        {
            var reason = string.IsNullOrWhiteSpace(t.VoidReason) ? "—" : t.VoidReason;
            sb.Append("<div class=\"section\">Void</div>");
            sb.Append(Line("Voided on", (t.VoidedAt ?? DateTime.Now).ToString("MMM d, yyyy h:mm tt", Fmt.Inv)));
            sb.Append(Line("Reason", reason));
        }
        sb.Append("<div class=\"footer\">Thank you for your transaction.<br>Keep this receipt for your records.<br>");
        sb.Append("Generated offline by Cash Ledger on this device.</div></main></article>");
        sb.Append("<div class=\"actions\"><button onclick=\"window.print()\">Print / Save as PDF</button></div></body></html>");
        return sb.ToString();
    }

    public static async Task ShareAsync(AppSettings settings, TransactionRecord t)
    {
        try
        {
            var path = Path.Combine(FileSystem.CacheDirectory, "receipt-TX-" + t.Id.ToString("000000") + ".html");
            await File.WriteAllTextAsync(path, BuildHtml(settings, t), new UTF8Encoding(false));
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Cash Ledger receipt TX-" + t.Id.ToString("000000"),
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("Receipt unavailable", "The receipt could not be prepared or shared. " + ex.Message, "OK");
        }
    }
}
