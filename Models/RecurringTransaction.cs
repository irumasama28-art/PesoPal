using SQLite;

namespace CashLedgerAndroid.Models;

/// <summary>
/// Represents a recurring transaction template
/// </summary>
public class RecurringTransaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Display name for the recurring transaction (e.g., "Weekly payment", "Monthly fee")
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Customer name for the generated transactions
    /// </summary>
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Transaction type: "Cash-In" or "Cash-Out"
    /// </summary>
    public string Type { get; set; } = "Cash-In";

    /// <summary>
    /// Amount to be transacted
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional fee to be applied
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// Reference template (can include placeholders)
    /// </summary>
    public string ReferenceNumber { get; set; } = "";

    /// <summary>
    /// Notes for the transaction
    /// </summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// Recurrence frequency: "daily", "weekly", "biweekly", "monthly", "quarterly", "yearly"
    /// </summary>
    public string Frequency { get; set; } = "monthly";

    /// <summary>
    /// Day of week (0=Sunday, 6=Saturday) for weekly/biweekly recurrence
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Day of month (1-31) for monthly/quarterly/yearly recurrence
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Month (1-12) for yearly recurrence
    /// </summary>
    public int? MonthOfYear { get; set; }

    /// <summary>
    /// Date when this recurring transaction template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// When the recurring transaction should start generating transactions
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Optional end date; null means ongoing
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether this recurring transaction is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date of the last generated transaction
    /// </summary>
    public DateTime? LastProcessedDate { get; set; }

    /// <summary>
    /// Get the next due date for this recurring transaction
    /// </summary>
    public DateTime GetNextDueDate()
    {
        var baseDate = LastProcessedDate ?? StartDate;

        return Frequency switch
        {
            "daily" => baseDate.AddDays(1),
            "weekly" => baseDate.AddDays(7),
            "biweekly" => baseDate.AddDays(14),
            "monthly" => AddMonths(baseDate, 1),
            "quarterly" => AddMonths(baseDate, 3),
            "yearly" => AddMonths(baseDate, 12),
            _ => baseDate.AddDays(1)
        };
    }

    /// <summary>
    /// Check if this recurring transaction is due
    /// </summary>
    public bool IsDue() => DateTime.Now.Date >= GetNextDueDate().Date;

    /// <summary>
    /// Check if this recurring transaction is within its active range
    /// </summary>
    public bool IsWithinActiveRange()
    {
        var now = DateTime.Now.Date;
        if (now < StartDate.Date) return false;
        if (EndDate.HasValue && now > EndDate.Value.Date) return false;
        return IsActive;
    }

    /// <summary>
    /// Helper to add months accounting for day-of-month edge cases
    /// </summary>
    private static DateTime AddMonths(DateTime date, int months)
    {
        var result = date.AddMonths(months);
        // If DayOfMonth is set, use it; otherwise use the original day
        if (result.Day != date.Day && result.Month != date.AddMonths(months).Month)
        {
            // Day overflow happened, set to last day of month
            result = new DateTime(result.Year, result.Month, 
                DateTime.DaysInMonth(result.Year, result.Month));
        }
        return result;
    }
}
