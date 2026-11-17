using System;
using System.Text;

namespace ExpenseAnalytics.Core;

public static class ExpenseService
{
    public static readonly Func<string, string?> CategoryNotEmptyRule = cat =>
        string.IsNullOrWhiteSpace(cat) ? "Категория не должна быть пустой или состоять только из пробелов." : null;

    public static readonly Func<string, string?> DescriptionNotEmptyRule = desc =>
        string.IsNullOrWhiteSpace(desc) ? "Описание не должно быть пустым или состоять только из пробелов." : null;

    public static readonly Func<decimal, string?> AmountPositiveRule = amount =>
        amount <= 0 ? "Сумма должна быть больше нуля." : null;

    public static List<Expense> GetInitialExpenses() => new()
    {
        new(1, new DateOnly(2026, 10, 01), "Продукты", "Покупки в магазине", 1800m),
        new(2, new DateOnly(2026, 10, 02), "Транспорт", "Проезд", 150m),
        new(3, new DateOnly(2026, 10, 03), "Развлечения", "Кино", 700m),
        new(4, new DateOnly(2026, 10, 04), "Продукты", "Покупки на рынке", 1200m),
        new(5, new DateOnly(2026, 10, 05), "Образование", "Книга", 1600m),
        new(6, new DateOnly(2026, 10, 06), "Транспорт", "Такси", 600m),
        new(7, new DateOnly(2026, 10, 07), "Развлечения", "Концерт", 2500m),
        new(8, new DateOnly(2026, 10, 08), "Продукты", "Кофе", 250m)
    };

    public static List<Expense> OrderExpenses(IEnumerable<Expense> expenses) =>
        expenses.OrderBy(e => e.Date).ThenBy(e => e.Id).ToList();

    public static Predicate<Expense> CreateFilterPredicate(ExpenseFilter filter)
    {
        var predicates = new List<Predicate<Expense>>();

        if (filter.StartDate.HasValue)
        {
            predicates.Add(e => e.Date >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            predicates.Add(e => e.Date <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var normalizedSearchCategory = filter.Category.Trim();
            predicates.Add(e => string.Equals(e.Category.Trim(), normalizedSearchCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.MinAmount.HasValue)
        {
            predicates.Add(e => e.Amount >= filter.MinAmount.Value);
        }

        return FunctionalUtils.AllOf(predicates);
    }

    public static List<Expense> FilterExpenses(IEnumerable<Expense> expenses, ExpenseFilter filter)
    {
        var predicate = CreateFilterPredicate(filter);
        return OrderExpenses(expenses.Where(e => predicate(e)));
    }

    public static ExpenseReport GenerateReport(IEnumerable<Expense> expenses)
    {
        var expenseList = expenses.ToList();
        int count = expenseList.Count;

        if (count == 0)
        {
            return new ExpenseReport(
                Count: 0,
                TotalAmount: 0m,
                AverageAmount: null,
                CategorySummaries: new List<CategorySummary>(),
                TopExpenses: new List<Expense>(),
                DailySummaries: new List<DailySummary>()
            );
        }

        decimal totalAmount = expenseList.Sum(e => e.Amount);
        decimal averageAmount = totalAmount / count;

        var categorySummaries = expenseList
            .GroupBy(e => e.Category.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new CategorySummary(g.Key, g.Sum(e => e.Amount)))
            .OrderByDescending(cs => cs.TotalAmount)
            .ToList();

        var topExpenses = expenseList
            .OrderByDescending(e => e.Amount)
            .ThenBy(e => e.Id)
            .Take(3)
            .ToList();

        var dailyGroups = expenseList
            .GroupBy(e => e.Date)
            .Select(g => new { Date = g.Key, DailyTotal = g.Sum(e => e.Amount) })
            .OrderBy(d => d.Date)
            .ToList();

        (_, var dailySummaries) = FunctionalUtils.MapAccum(
            dailyGroups,
            initialState: 0m,
            step: (runningSum, dayGroup) =>
            {
                var nextRunningSum = runningSum + dayGroup.DailyTotal;
                var summary = new DailySummary(dayGroup.Date, dayGroup.DailyTotal, nextRunningSum);
                return (nextRunningSum, summary);
            }
        );

        return new ExpenseReport(
            Count: count,
            TotalAmount: totalAmount,
            AverageAmount: averageAmount,
            CategorySummaries: categorySummaries,
            TopExpenses: topExpenses,
            DailySummaries: dailySummaries
        );
    }
}
