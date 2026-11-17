using System;
using System.Text;

namespace ExpenseAnalytics.Core;

public static class FunctionalUtils
{
    public static Predicate<T> AllOf<T>(IReadOnlyList<Predicate<T>> predicates)
    {
        var predicatesCopy = predicates.ToList();

        return item =>
        {
            foreach (var predicate in predicatesCopy)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }
            return true;
        };
    }

    public static List<string> ValidateAll<T>(T value, IReadOnlyList<Func<T, string?>> rules)
    {
        var errors = new List<string>();
        foreach (var rule in rules)
        {
            var error = rule(value);
            if (error != null)
            {
                errors.Add(error);
            }
        }
        return errors;
    }

    public static (TState FinalState, List<TResult> Items) MapAccum<T, TState, TResult>(
        IEnumerable<T> source,
        TState initialState,
        Func<TState, T, (TState NextState, TResult Item)> step)
    {
        var currentState = initialState;
        var results = new List<TResult>();

        foreach (var item in source)
        {
            var (nextState, resultItem) = step(currentState, item);
            currentState = nextState;
            results.Add(resultItem);
        }

        return (currentState, results);
    }

    public static List<T> AppendImmutable<T>(IReadOnlyList<T> source, T item)
    {
        var newList = new List<T>(source.Count + 1);
        newList.AddRange(source);
        newList.Add(item);
        return newList;
    }
}
