using System.Globalization;
using ExpenseAnalytics.Core;

namespace ExpenseAnalytics.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        List<Expense> expenses = ExpenseService.GetInitialExpenses();

        bool exit = false;
        while (!exit)
        {
            PrintMenu();
            string? choice = Console.ReadLine()?.Trim();

            Console.WriteLine();
            switch (choice)
            {
                case "1":
                    ShowAllExpenses(expenses);
                    break;
                case "2":
                    expenses = AddExpenseInteractive(expenses);
                    break;
                case "3":
                    FilterExpensesInteractive(expenses);
                    break;
                case "4":
                    GenerateReportInteractive(expenses);
                    break;
                case "0":
                    exit = true;
                    Console.WriteLine("Завершение работы приложения.");
                    break;
                default:
                    Console.WriteLine("Неизвестная команда. Пожалуйста, выберите пункт от 0 до 4.");
                    break;
            }

            if (!exit)
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    private static void PrintMenu()
    {
        Console.WriteLine("=== АНАЛИЗАТОР РАСХОДОВ ===");
        Console.WriteLine("1 - Показать все расходы");
        Console.WriteLine("2 - Добавить расход");
        Console.WriteLine("3 - Отобрать расходы");
        Console.WriteLine("4 - Сформировать отчёт");
        Console.WriteLine("0 - Завершить работу");
        Console.Write("Выберите пункт меню: ");
    }

    private static void ShowAllExpenses(IReadOnlyList<Expense> expenses)
    {
        Console.WriteLine("--- ВСЕ РАСХОДЫ ---");
        var sorted = ExpenseService.OrderExpenses(expenses);
        PrintExpensesTable(sorted);
    }

    private static List<Expense> AddExpenseInteractive(IReadOnlyList<Expense> currentExpenses)
    {
        Console.WriteLine("--- ДОБАВЛЕНИЕ НОВОГО РАСХОДА ---");

        DateOnly date = default;
        bool validDate = false;
        while (!validDate)
        {
            Console.Write("Введите дату (в формате дд.мм.гггг): ");
            string? dateStr = Console.ReadLine();
            if (DateOnly.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                validDate = true;
            }
            else
            {
                Console.WriteLine("Ошибка: Неверный формат даты. Используйте формат дд.мм.гггг (например, 01.10.2026).");
            }
        }

        Console.Write("Введите категорию: ");
        string category = Console.ReadLine() ?? string.Empty;

        Console.Write("Введите описание: ");
        string description = Console.ReadLine() ?? string.Empty;

        decimal amount = 0;
        bool validAmount = false;
        while (!validAmount)
        {
            Console.Write("Введите сумму: ");
            string? amountStr = Console.ReadLine();
            if (decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ||
                decimal.TryParse(amountStr, out amount))
            {
                validAmount = true;
            }
            else
            {
                Console.WriteLine("Ошибка: Неверный формат суммы. Введите числовое значение.");
            }
        }

        var categoryErrors = FunctionalUtils.ValidateAll(category, new[] { ExpenseService.CategoryNotEmptyRule });
        var descriptionErrors = FunctionalUtils.ValidateAll(description, new[] { ExpenseService.DescriptionNotEmptyRule });
        var amountErrors = FunctionalUtils.ValidateAll(amount, new[] { ExpenseService.AmountPositiveRule });

        var allErrors = categoryErrors.Concat(descriptionErrors).Concat(amountErrors).ToList();

        if (allErrors.Count > 0)
        {
            Console.WriteLine("\nОбнаружены ошибки при проверке данных:");
            foreach (var err in allErrors)
            {
                Console.WriteLine($" - {err}");
            }
            Console.WriteLine("Расход не был добавлен.");
            return currentExpenses.ToList();
        }

        int newId = currentExpenses.Count > 0 ? currentExpenses.Max(e => e.Id) + 1 : 1;
        var newExpense = new Expense(newId, date, category, description, amount);

        var updatedList = FunctionalUtils.AppendImmutable(currentExpenses, newExpense);
        Console.WriteLine($"\nРасход успешно добавлен с Id = {newId}!");
        return updatedList;
    }

    private static void FilterExpensesInteractive(IReadOnlyList<Expense> expenses)
    {
        Console.WriteLine("--- ОТБОР РАСХОДОВ ---");
        Console.WriteLine("Оставьте поле пустым, если критерий не требуется.");

        DateOnly? startDate = null;
        DateOnly? endDate = null;

        while (true)
        {
            Console.Write("Дата начала (дд.мм.гггг): ");
            string? startStr = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(startStr))
            {
                if (DateOnly.TryParseExact(startStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sd))
                {
                    startDate = sd;
                }
                else
                {
                    Console.WriteLine("Ошибка формата даты. Повторите ввод критериев.");
                    continue;
                }
            }

            Console.Write("Дата окончания (дд.мм.гггг): ");
            string? endStr = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(endStr))
            {
                if (DateOnly.TryParseExact(endStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ed))
                {
                    endDate = ed;
                }
                else
                {
                    Console.WriteLine("Ошибка формата даты. Повторите ввод критериев.");
                    continue;
                }
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                Console.WriteLine("Ошибка: Дата начала позже даты окончания. Повторите ввод критериев.");
                startDate = null;
                endDate = null;
                continue;
            }
            break;
        }

        Console.Write("Категория: ");
        string? categoryInput = Console.ReadLine();
        string? category = string.IsNullOrWhiteSpace(categoryInput) ? null : categoryInput;

        decimal? minAmount = null;
        Console.Write("Минимальная сумма: ");
        string? minAmountStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(minAmountStr))
        {
            if (decimal.TryParse(minAmountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var ma) ||
                decimal.TryParse(minAmountStr, out ma))
            {
                minAmount = ma;
            }
            else
            {
                Console.WriteLine("Некорректная сумма, критерий минимальной суммы проигнорирован.");
            }
        }

        var filter = new ExpenseFilter(startDate, endDate, category, minAmount);
        var filteredExpenses = ExpenseService.FilterExpenses(expenses, filter);

        Console.WriteLine($"\nРезультаты отбора (найдено записей: {filteredExpenses.Count}):");
        PrintExpensesTable(filteredExpenses);
    }

    private static void GenerateReportInteractive(IReadOnlyList<Expense> expenses)
    {
        Console.WriteLine("--- ФОРМИРОВАНИЕ ОТЧЁТА ---");
        Console.WriteLine("Хотите применить критерии отбора перед формированием отчёта? (y/n): ");
        string? choice = Console.ReadLine()?.Trim().ToLower();

        IEnumerable<Expense> targetExpenses = expenses;
        if (choice == "y")
        {
            Console.Write("Введите категорию для отчёта (или пусто для всех): ");
            string? cat = Console.ReadLine();
            cat = string.IsNullOrWhiteSpace(cat) ? null : cat;

            var filter = new ExpenseFilter(null, null, cat, null);
            targetExpenses = ExpenseService.FilterExpenses(expenses, filter);
        }

        var report = ExpenseService.GenerateReport(targetExpenses);

        Console.WriteLine("\n================ ОТЧЁТ ================");
        Console.WriteLine($"Количество расходов: {report.Count}");
        Console.WriteLine($"Общая сумма:       {report.TotalAmount}");
        Console.WriteLine($"Средняя сумма:     {(report.AverageAmount.HasValue ? report.AverageAmount.Value.ToString("0.00", CultureInfo.InvariantCulture) : "нет данных")}");

        Console.WriteLine("\nСуммы по категориям:");
        if (report.CategorySummaries.Count == 0)
        {
            Console.WriteLine(" (нет данных)");
        }
        else
        {
            foreach (var cs in report.CategorySummaries)
            {
                Console.WriteLine($" - {cs.Category}: {cs.TotalAmount}");
            }
        }

        Console.WriteLine("\nТри крупнейших расхода:");
        if (report.TopExpenses.Count == 0)
        {
            Console.WriteLine(" (нет данных)");
        }
        else
        {
            foreach (var e in report.TopExpenses)
            {
                Console.WriteLine($" [Id: {e.Id}] {e.Date:dd.MM.yyyy} | {e.Category} | {e.Description} — {e.Amount}");
            }
        }

        Console.WriteLine("\nДневные суммы и накопленные итоги:");
        if (report.DailySummaries.Count == 0)
        {
            Console.WriteLine(" (нет данных)");
        }
        else
        {
            Console.WriteLine($"{"Дата",-12} | {"Сумма за день",-15} | {"Накопленный итог",-15}");
            Console.WriteLine(new string('-', 48));
            foreach (var ds in report.DailySummaries)
            {
                Console.WriteLine($"{ds.Date:dd.MM.yyyy,-12} | {ds.DailyTotal,-15} | {ds.RunningTotal,-15}");
            }
        }
        Console.WriteLine("=======================================");
    }

    private static void PrintExpensesTable(IReadOnlyList<Expense> expenses)
    {
        if (expenses.Count == 0)
        {
            Console.WriteLine("Список расходов пуст.");
            return;
        }

        Console.WriteLine($"{"Id",-4} | {"Дата",-10} | {"Категория",-15} | {"Описание",-25} | {"Сумма",-8}");
        Console.WriteLine(new string('-', 74));
        foreach (var e in expenses)
        {
            Console.WriteLine($"{e.Id,-4} | {e.Date:dd.MM.yyyy,-10} | {e.Category,-15} | {e.Description,-25} | {e.Amount,-8}");
        }
    }
}
