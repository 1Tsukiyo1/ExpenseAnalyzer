using System;
using System.Text;

namespace ExpenseAnalytics.Core;

public record Expense(int Id, DateOnly Date, string Category, string Description, decimal Amount);

public record ExpenseFilter(
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Category,
    decimal? MinAmount
);

public record CategorySummary(string Category, decimal TotalAmount);

public record DailySummary(DateOnly Date, decimal DailyTotal, decimal RunningTotal);

public record ExpenseReport(
    int Count,
    decimal TotalAmount,
    decimal? AverageAmount,
    IReadOnlyList<CategorySummary> CategorySummaries,
    IReadOnlyList<Expense> TopExpenses,
    IReadOnlyList<DailySummary> DailySummaries
);
