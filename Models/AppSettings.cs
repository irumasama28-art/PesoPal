using SQLite;

namespace CashLedgerAndroid.Models;

public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public string StoreName { get; set; } = "My Cash Store";

    // Theme preference: "light", "dark", or "system"
    public string ThemeMode { get; set; } = "light";

    // Editable default fee bands; values are defaults and can be changed in Settings > Fee table.
    public decimal FeeUpTo500 { get; set; } = 10;
    public decimal FeeUpTo1000 { get; set; } = 20;
    public decimal FeeUpTo1500 { get; set; } = 30;
    public decimal FeeUpTo2000 { get; set; } = 40;
    public decimal FeeUpTo3000 { get; set; } = 60;
    public decimal FeeAbove3000 { get; set; } = 60;

    public decimal SuggestFee(decimal amount)
    {
        if (amount <= 500) return FeeUpTo500;
        if (amount <= 1000) return FeeUpTo1000;
        if (amount <= 1500) return FeeUpTo1500;
        if (amount <= 2000) return FeeUpTo2000;
        if (amount <= 3000) return FeeUpTo3000;
        return FeeAbove3000;
    }
}
