using System;
using System.Collections.Generic;
using System.Threading;
using ModernFeatures;

// ============================================================
// C# 14 Feature Showcase — DeepSource analyzer regression testing
// ============================================================

// --- Extension Members (C# 14) ---
public static class Extensions
{
    extension(string s)
    {
        public bool IsNullOrEmpty => string.IsNullOrEmpty(s);
        public string Reversed()
        {
            var chars = s.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
    }

    extension(IEnumerable<int> source)
    {
        public int Sum()
        {
            int total = 0;
            foreach (var item in source)
                total += item;
            return total;
        }

        public static IEnumerable<int> Range(int count)
        {
            for (int i = 0; i < count; i++)
                yield return i;
        }
    }
}

// --- Null-Conditional Assignment (C# 14) ---
public class Order
{
    public string? Name { get; set; }
    public decimal Total { get; set; }
    public List<string>? Items { get; set; }
}

public class Customer
{
    public Order? Order { get; set; }

    public void UpdateOrder()
    {
        this.Order?.Name = "Updated Order";
        this.Order?.Items?.Add("New Item");
        this.Order?.Total += 10.0m;
    }
}

// --- field keyword (C# 14, stabilized) ---
public class Person
{
    public string Name
    {
        get => field;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int Age
    {
        get => field;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            field = value;
        }
    }

    public override string ToString() => $"{Name} (age {Age})";
}

// --- Partial Constructors (C# 14) ---
public partial class Widget
{
    public string Label { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public partial Widget(string label, int width, int height);

    public int Area => Width * Height;
}

public partial class Widget
{
    public partial Widget(string label, int width, int height)
    {
        Label = label;
        Width = width;
        Height = height;
    }
}

// --- Simple Lambda with Modifiers (C# 14) ---
public class LambdaExamples
{
    public delegate void RefAction(ref int x);
    public delegate bool TryParser(string input, out int result);

    public void Run()
    {
        RefAction doubler = (ref x) => x *= 2;
        TryParser parser = (string input, out result) =>
        {
            return int.TryParse(input, out result);
        };

        int val = 5;
        doubler(ref val);
        Console.WriteLine($"Doubled: {val}");

        parser("42", out var parsed);
        Console.WriteLine($"Parsed: {parsed}");
    }
}

// --- nameof with Unbound Generics (C# 14) ---
public class GenericUtils
{
    public static string ListTypeName => nameof(List<>);
    public static string DictTypeName => nameof(Dictionary<,>);

    public static void PrintTypeNames()
    {
        Console.WriteLine($"List type: {ListTypeName}");
        Console.WriteLine($"Dict type: {DictTypeName}");
    }
}

// --- Implicit Span Conversions (C# 14) ---
public class SpanExamples
{
    public static int SumSpan(ReadOnlySpan<int> data)
    {
        int sum = 0;
        foreach (var item in data)
            sum += item;
        return sum;
    }

    public static void Run()
    {
        int[] array = [1, 2, 3, 4, 5];
        // Implicit array -> ReadOnlySpan
        Console.WriteLine($"Sum: {SumSpan(array)}");

        Span<int> span = array;
        // Implicit Span -> ReadOnlySpan
        Console.WriteLine($"Sum via span: {SumSpan(span)}");
    }
}

// --- Partial Events (C# 14) ---
public partial class EventSource
{
    public partial event EventHandler? DataReceived;
}

public partial class EventSource
{
    private EventHandler? _dataReceived;
    public partial event EventHandler? DataReceived
    {
        add => _dataReceived += value;
        remove => _dataReceived -= value;
    }

    public void RaiseData() => _dataReceived?.Invoke(this, EventArgs.Empty);
}

// ============================================================
// CS-W1094: Lock on local variable tests
// ============================================================
public class LockPatterns
{
    private readonly object _syncRoot = new object();

    // GOOD: lock on field — should NOT trigger CS-W1094
    public void LockOnField()
    {
        lock (_syncRoot)
        {
            Console.WriteLine("Locked on field — correct");
        }
    }

    // BAD: lock on local object — SHOULD trigger CS-W1094
    public void LockOnLocalObject()
    {
        var localLock = new object();
        lock (localLock)
        {
            Console.WriteLine("Locked on local — bad!");
        }
    }

    // BAD: lock on another local — SHOULD trigger CS-W1094
    public void LockOnLocalString()
    {
        object temp = "lock-target";
        lock (temp)
        {
            Console.WriteLine("Locked on local string reference — bad!");
        }
    }

    // GOOD: lock on System.Threading.Lock (C# 13+) — should NOT trigger CS-W1094
    public void LockOnThreadingLock()
    {
        Lock myLock = new Lock();
        lock (myLock)
        {
            Console.WriteLine("Locked on System.Threading.Lock — correct, should be skipped");
        }
    }

    // GOOD: lock on System.Threading.Lock from field
    private readonly Lock _fieldLock = new Lock();
    public void LockOnFieldLock()
    {
        lock (_fieldLock)
        {
            Console.WriteLine("Field Lock — correct");
        }
    }
}

// ============================================================
// Deliberate anti-patterns for analyzer testing
// ============================================================
public class AntiPatterns
{
    // CS-R1137: could be readonly
    private string label = "default";
    private int maxRetries = 3;

    // CS-W1030: variable shadows field
    public void Process(string label)
    {
        Console.WriteLine($"Processing: {label}");
        Console.WriteLine($"Max retries: {maxRetries}");
    }

    // CS-R1048: use brace initializer
    public Order CreateOrder()
    {
        var order = new Order();
        order.Name = "Test";
        order.Total = 99.99m;
        return order;
    }

    // CS-R1123: dict add can be simplified
    public Dictionary<string, int> BuildLookup()
    {
        var dict = new Dictionary<string, int>();
        dict.Add("alpha", 1);
        dict.Add("beta", 2);
        dict.Add("gamma", 3);
        return dict;
    }

    // CS-R1037: use range index over Substring
    public string GetSuffix(string input)
    {
        return input.Substring(input.Length - 3);
    }

    // CS-W1100: unused variable
    public void DoWork()
    {
        var unused = 42;
        var alsoUnused = "hello";
        Console.WriteLine("Working...");
    }
}

// ============================================================
// Clean, well-written code (should produce no issues)
// ============================================================
public class CleanCalculator
{
    public static double Add(double a, double b) => a + b;
    public static double Multiply(double a, double b) => a * b;

    public static double Average(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        double sum = 0;
        foreach (var v in values)
            sum += v;
        return sum / values.Count;
    }
}

public sealed class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    private Result(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(string error) => new(default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> transform) =>
        IsSuccess ? Result<TOut>.Ok(transform(Value!)) : Result<TOut>.Fail(Error!);
}

// --- Main ---
class Program
{
    static void Main()
    {
        // Clean code
        var person = new Person { Name = "Alice", Age = 30 };
        Console.WriteLine(person);

        var widget = new Widget("Button", 100, 50);
        Console.WriteLine($"Widget: {widget.Label}, Area: {widget.Area}");

        var customer = new Customer
        {
            Order = new Order { Name = "Initial", Total = 50m, Items = new List<string>() }
        };
        customer.UpdateOrder();

        GenericUtils.PrintTypeNames();
        SpanExamples.Run();

        var lambdas = new LambdaExamples();
        lambdas.Run();

        // Lock patterns
        var locks = new LockPatterns();
        locks.LockOnField();
        locks.LockOnLocalObject();
        locks.LockOnLocalString();
        locks.LockOnThreadingLock();
        locks.LockOnFieldLock();

        // Anti-patterns
        var ap = new AntiPatterns();
        ap.CreateOrder();
        ap.BuildLookup();
        Console.WriteLine(ap.GetSuffix("testing"));
        ap.Process("test");
        ap.DoWork();

        // Clean calculator
        Console.WriteLine($"Sum: {CleanCalculator.Add(3, 4)}");
        Console.WriteLine($"Avg: {CleanCalculator.Average(new[] { 1.0, 2.0, 3.0 })}");

        var result = Result<int>.Ok(42).Map(x => x * 2);
        Console.WriteLine($"Result: {result.Value}");

        // Event source
        var src = new EventSource();
        src.DataReceived += (_, _) => Console.WriteLine("Data received!");
        src.RaiseData();
    }
}
