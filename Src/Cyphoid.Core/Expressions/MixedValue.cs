using Cyphoid.Core.Exceptions;
using Cyphoid.Core.Execution;

namespace Cyphoid.Core.Expressions
{
  public readonly struct MixedValue : IEquatable<MixedValue>, IComparable<MixedValue>
  {
    public enum ValueType : byte
    {
      Null = 0,
      Bool = 1,
      Int = 2,
      Double = 3,
      String = 4,
      Node = 5,
      Edge = 6
    }

    private readonly ValueType _kind;
    private readonly bool _bool;
    private readonly long _int;
    private readonly double _double;
    private readonly object? _ref;


    public MixedValue()
    {
      _kind = ValueType.Null;
      _bool = default;
      _int = default;
      _double = default;
      _ref = default;
    }

    private MixedValue(bool value)
    {
      _kind = ValueType.Bool;
      _bool = value;
      _int = default;
      _double= default;
      _ref = default;
    }

    private MixedValue(long value)
    {
      _kind = ValueType.Int;
      _bool = default;
      _int = value;
      _double = default;
      _ref = default;
    }

    private MixedValue(double value)
    {
      _kind = ValueType.Double;
      _bool = default;
      _int = default;
      _double = value;
      _ref = default;
    }

    private MixedValue(string value)
    {
      ArgumentNullException.ThrowIfNull(value);
      _kind = ValueType.String;
      _bool = default;
      _int = default;
      _double = default;
      _ref = value;
    }

    private MixedValue(IGraphNode value)
    {
      ArgumentNullException.ThrowIfNull(value);
      _kind = ValueType.Node;
      _bool = default;
      _int = default;
      _double = default;
      _ref = value;
    }

    public static MixedValue Null() => new();

    public static MixedValue Bool(bool value) => new(value);

    public static MixedValue Int(long value) => new(value);

    public static MixedValue Double(double value) => new(value);

    public static MixedValue String(string? value) => value == null ? Null() : new(value);

    public static MixedValue GraphNode(IGraphNode? value) => value == null ? Null() : new(value);

    public static MixedValue FromObject(object? value) =>
      value is bool b ? Bool(b)
      : value is int i ? Int(i)
      : value is double d ? Double(d)
      : value is long l ? Int(l)
      : value is string s ? String(s)
      : value is IGraphNode g ? GraphNode(g)
      : value == null ? new()
      : throw new RuntimeException($"Unhandled value '{value}' of type '{value.GetType()}'.");

    public ValueType Type => _kind;

    public bool IsNull() =>
      _kind == ValueType.Null;

    public bool AsBool() =>
        _kind == ValueType.Bool ? _bool : throw new RuntimeException($"Value is not a bool. Got {_kind} {ToString()}.");

    public bool IsAnythingButTrue() =>
        _kind == ValueType.Bool ? !_bool : true;

    public long AsInt() =>
        _kind == ValueType.Int ? _int : throw new RuntimeException($"Value is not an int Got {_kind} {ToString()}.");

    public double AsDouble() =>
        _kind == ValueType.Double ? _double : throw new RuntimeException($"Value is not a double Got {_kind} {ToString()}.");

    public string AsString() =>
        _kind == ValueType.String ? (string)_ref! : throw new RuntimeException($"Value is not a string. Got {_kind} {ToString()}.");

    public IGraphNode<TId> AsGraphNode<TId>() where TId : IEquatable<TId> =>
        _kind == ValueType.Node ? (IGraphNode<TId>)_ref! : throw new RuntimeException($"Value is not a node. Got {_kind} {ToString()}.");

    public object? AsObject() => Match<object?>(
      () => null,
      b => b,
      i => i,
      d => d,
      s => s,
      g => g);

    public bool TryGetBool(out bool value)
    {
      value = _bool;
      return _kind == ValueType.Bool;
    }

    public bool TryGetInt(out long value)
    {
      value = _int;
      return _kind == ValueType.Int;
    }

    public bool TryGetDouble(out double value)
    {
      value = _double;
      return _kind == ValueType.Double;
    }

    public bool TryGetString(out string? value)
    {
      value = _ref as string;
      return _kind == ValueType.String;
    }

    public bool TryGetGraphNode(out IGraphNode? value)
    {
      value = _ref as IGraphNode;
      return _kind == ValueType.String;
    }

    public T Match<T>(
      Func<T> onNull,
      Func<bool, T> onBool,
      Func<long, T> onInt,
      Func<double, T> onDouble,
      Func<string, T> onString,
      Func<IGraphNode, T> onGraphNode)
    {
      return _kind switch
      {
        ValueType.Null => onNull(),
        ValueType.Bool => onBool(_bool),
        ValueType.Int => onInt(_int),
        ValueType.Double => onDouble(_double),
        ValueType.String => onString((string)_ref!),
        ValueType.Node => onGraphNode((IGraphNode)_ref!),
        _ => throw new RuntimeException($"Unhandled value type '{_kind}'.")
      };
    }


    public bool Equals(MixedValue other) =>
      _kind == other._kind &&
      _bool == other._bool &&
      _int == other._int &&
      _double == other._double &&
      _kind switch
      {
        ValueType.String => (string?)_ref == (string?)other._ref,
        ValueType.Null => _ref == other._ref,
        ValueType.Node => ((IGraphNode)_ref!).HasSameIdAs((IGraphNode?)other._ref),
        _ => true
      };


    public override bool Equals(object? obj) =>
      obj is MixedValue other && Equals(other)
      || obj is bool b && _kind == ValueType.Bool && _bool == b
      || obj is int i && _kind == ValueType.Int && _int == i
      || obj is long l && _kind == ValueType.Int && _int == l
      || obj is double d && _kind == ValueType.Double && _double == d
      || obj is string s && _kind == ValueType.String && (string?)_ref == s
      || obj is IGraphNode g && _kind == ValueType.Node && (IGraphNode?)_ref == g;


    public override int GetHashCode() =>
      _kind switch
      {
        ValueType.Bool => HashCode.Combine(_kind, _bool),
        ValueType.Int => HashCode.Combine(_kind, _int),
        ValueType.Double => HashCode.Combine(_kind, _double),
        ValueType.String => HashCode.Combine(_kind, _ref),
        ValueType.Node => HashCode.Combine(_kind, _ref),
        _ => (int)_kind
      };


    public override string ToString() =>
      _kind switch
      {
        ValueType.Null => "null",
        ValueType.Bool => _bool.ToString(),
        ValueType.Int => _int.ToString(),
        ValueType.Double => _double.ToString(),
        ValueType.String => _ref!.ToString() ?? "",
        ValueType.Node => "-node-",
        _ => "<undefined>"
      };


    int IComparable<MixedValue>.CompareTo(MixedValue other)
    {
      if (this._kind == ValueType.Null && other._kind == ValueType.Null)
      {
        return 0;
      }
      else if (this._kind == ValueType.Null && other._kind != ValueType.Null)
      {
        return -1;
      }
      else if (this._kind != ValueType.Null && other._kind == ValueType.Null)
      {
        return 1;
      }
      else if (this._kind == other._kind)
      {
        return _kind switch
        {
          ValueType.Bool => throw new RuntimeException($"Cannot compare booleans '{this}' and '{other}'."),
          ValueType.Int => _int.CompareTo(other._int),
          ValueType.Double => _double.CompareTo(other._double),
          ValueType.String => (_ref!.ToString() ?? "").CompareTo(other._ref!.ToString() ?? ""),
          ValueType.Node => throw new RuntimeException($"Cannot compare nodes '{this}' and '{other}'."),
          _ => throw new RuntimeException($"Cannot compare values '{this}' and '{other}'."),
        };
      }
      else
        throw new RuntimeException($"Cannot compare '{this}' and '{other}'.");
    }

    
    public static MixedValue operator <=(MixedValue left, MixedValue right)
    {
      return MixedValue.Bool(((IComparable<MixedValue>)left).CompareTo(right) <= 0);
    }


    public static MixedValue operator <(MixedValue left, MixedValue right)
    {
      return MixedValue.Bool(((IComparable<MixedValue>)left).CompareTo(right) < 0);
    }


    public static MixedValue operator >=(MixedValue left, MixedValue right)
    {
      return MixedValue.Bool(((IComparable<MixedValue>)left).CompareTo(right) >= 0);
    }


    public static MixedValue operator >(MixedValue left, MixedValue right)
    {
      return MixedValue.Bool(((IComparable<MixedValue>)left).CompareTo(right) > 0);
    }


    public static MixedValue operator +(MixedValue left, MixedValue right)
    {
      if (left.IsNull() || right.IsNull())
        return MixedValue.Null();

      if (left.TryGetInt(out var li))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Int(li + ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(li + rd);
        }
      }
      else if (left.TryGetDouble(out var ld))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Double(ld + ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(ld + rd);
        }
      }

      throw new RuntimeException($"Cannot add two values of type '{left.Type}' ({left}) and '{right.Type}' ({right}).");
    }


    public static MixedValue operator -(MixedValue left, MixedValue right)
    {
      if (left.IsNull() || right.IsNull())
        return MixedValue.Null();

      if (left.TryGetInt(out var li))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Int(li - ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(li - rd);
        }
      }
      else if (left.TryGetDouble(out var ld))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Double(ld - ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(ld - rd);
        }
      }

      throw new RuntimeException($"Cannot substract two values of type '{left.Type}' ({left}) and '{right.Type}' ({right}).");
    }


    public static MixedValue operator *(MixedValue left, MixedValue right)
    {
      if (left.IsNull() || right.IsNull())
        return MixedValue.Null();

      if (left.TryGetInt(out var li))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Int(li * ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(li * rd);
        }
      }
      else if (left.TryGetDouble(out var ld))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Double(ld * ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(ld * rd);
        }
      }

      throw new RuntimeException($"Cannot multiply two values of type '{left.Type}' ({left}) and '{right.Type}' ({right}).");
    }


    public static MixedValue operator /(MixedValue left, MixedValue right)
    {
      if (left.IsNull() || right.IsNull())
        return MixedValue.Null();

      if (left.TryGetInt(out var li))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Int(li / ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(li / rd);
        }
      }
      else if (left.TryGetDouble(out var ld))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Double(ld / ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(ld / rd);
        }
      }

      throw new RuntimeException($"Cannot divide two values of type '{left.Type}' ({left}) and '{right.Type}' ({right}).");
    }


    public static MixedValue operator %(MixedValue left, MixedValue right)
    {
      if (left.IsNull() || right.IsNull())
        return MixedValue.Null();

      if (left.TryGetInt(out var li))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Int(li % ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(li % rd);
        }
      }
      else if (left.TryGetDouble(out var ld))
      {
        if (right.TryGetInt(out var ri))
        {
          return MixedValue.Double(ld % ri);
        }
        else if (right.TryGetDouble(out var rd))
        {
          return MixedValue.Double(ld % rd);
        }
      }

      throw new RuntimeException($"Cannot calculate modula of two values of type '{left.Type}' ({left}) and '{right.Type}' ({right}).");
    }
  }
}
