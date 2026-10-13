namespace CashLedgerAndroid.Services;

/// <summary>
/// Service to process recurring transactions and generate automatic entries
/// </summary>
public class RecurringTransactionService
{
    private readonly LedgerDatabase _db;
    private DateTime _lastProcessed = DateTime.MinValue;

    public RecurringTransactionService(LedgerDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// Process due recurring transactions (safe to call frequently, checks if already processed today)
    /// </summary>
    public async Task TryProcessAsync()
    {
        // Only process once per day to avoid unnecessary database queries
        if ((DateTime.Now - _lastProcessed).TotalHours < 1)
            return;

        try
        {
            var count = await _db.ProcessDueRecurringTransactionsAsync();
            if (count > 0)
            {
                _lastProcessed = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Recurring transaction processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Force immediate processing regardless of last processed time
    /// </summary>
    public async Task ForceProcessAsync()
    {
        try
        {
            await _db.ProcessDueRecurringTransactionsAsync();
            _lastProcessed = DateTime.Now;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Recurring transaction processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get upcoming recurring transaction preview
    /// </summary>
    public async Task<List<(Models.RecurringTransaction recurring, DateTime nextDue)>> GetUpcomingAsync(int daysLookahead = 30)
    {
        try
        {
            var recurring = await _db.GetRecurringTransactionsAsync();
            var upcoming = new List<(Models.RecurringTransaction, DateTime)>();

            var today = DateTime.Now.Date;
            var lookahead = today.AddDays(daysLookahead);

            foreach (var item in recurring)
            {
                if (!item.IsWithinActiveRange()) continue;

                var nextDue = item.GetNextDueDate();
                if (nextDue.Date >= today && nextDue.Date <= lookahead)
                {
                    upcoming.Add((item, nextDue));
                }
            }

            return upcoming.OrderBy(x => x.Item2).ToList();
        }
        catch { return new List<(Models.RecurringTransaction, DateTime)>(); }
    }
}
