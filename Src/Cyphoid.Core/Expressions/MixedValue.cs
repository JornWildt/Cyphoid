using Cyphoid.Core.Execution;
using System.ComponentModel.DataAnnotations;
using System.Windows.Markup;

namespace Cyphoid.Core.Expressions
{
  public readonly struct MixedValue : IEquatable<MixedValue>
  {
    public enum Kind : byte
    {
      Null = 0,
      Bool = 1,
      Int = 2,
      String = 3,
      Node = 4
    }

    private readonly Kind _kind;
    private readonly bool _bool;
    private readonly long _int;
    private readonly object? _ref;


    public MixedValue()
    {
      _kind = Kind.Null;
      _bool = default;
      _int = default;
      _ref = default;
    }

    private MixedValue(bool value)
    {
      _kind = Kind.Bool;
      _bool = value;
      _int = default;
      _ref = default;
    }

    private MixedValue(long value)
    {
      _kind = Kind.Int;
      _bool = default;
      _int = value;
      _ref = default;
    }

    private MixedValue(string value)
    {
      ArgumentNullException.ThrowIfNull(value);
      _kind = Kind.String;
      _bool = default;
      _int = default;
      _ref = value;
    }

    private MixedValue(IGraphNode value)
    {
      ArgumentNullException.ThrowIfNull(value);
      _kind = Kind.Node;
      _bool = default;
      _int = default;
      _ref = value;
    }

    public static MixedValue Null() => new();

    public static MixedValue Bool(bool value) => new(value);

    public static MixedValue Int(long value) => new(value);

    public static MixedValue String(string? value) => value == null ? Null() : new(value);

    public static MixedValue GraphNode(IGraphNode? value) => value == null ? Null() : new(value);

    public static MixedValue FromObject(object? value) =>
      value is bool b ? Bool(b)
      : value is int i ? Int(i)
      : value is long l ? Int(l)
      : value is string s ? String(s)
      : value is IGraphNode g ? GraphNode(g)
      : value == null ? new()
      : throw new NotImplementedException();

    public bool IsNull() =>
      _kind == Kind.Null;

    public bool AsBool() =>
        _kind == Kind.Bool ? _bool : throw new InvalidOperationException($"Value is not a bool. Got {_kind} {ToString()}.");

    public bool IsAnythingButTrue() =>
        _kind == Kind.Bool ? !_bool : true;

    public long AsInt() =>
        _kind == Kind.Int ? _int : throw new InvalidOperationException($"Value is not an int Got {_kind} {ToString()}.");

    public string AsString() =>
        _kind == Kind.String ? (string)_ref! : throw new InvalidOperationException($"Value is not a string. Got {_kind} {ToString()}.");

    public IGraphNode AsGraphNode() =>
        _kind == Kind.Node ? (IGraphNode)_ref! : throw new InvalidOperationException($"Value is not a node. Got {_kind} {ToString()}.");

    public object? AsObject() => Match<object?>(
      () => null,
      b => b,
      i => i,
      s => s,
      g => g);

    public bool TryGetBool(out bool value)
    {
      value = _bool;
      return _kind == Kind.Bool;
    }

    public bool TryGetInt(out long value)
    {
      value = _int;
      return _kind == Kind.Int;
    }

    public bool TryGetString(out string? value)
    {
      value = _ref as string;
      return _kind == Kind.String;
    }

    public bool TryGetGraphNode(out IGraphNode? value)
    {
      value = _ref as IGraphNode;
      return _kind == Kind.String;
    }

    public T Match<T>(
      Func<T> onNull,
      Func<bool, T> onBool,
      Func<long, T> onInt,
      Func<string, T> onString,
      Func<IGraphNode, T> onGraphNode)
    {
      return _kind switch
      {
        Kind.Null => onNull(),
        Kind.Bool => onBool(_bool),
        Kind.Int => onInt(_int),
        Kind.String => onString((string)_ref!),
        Kind.Node => onGraphNode((IGraphNode)_ref!),
        _ => throw new NotImplementedException()
      };
    }

    //public static implicit operator MixedValue(bool value) => new(value);
    //public static implicit operator MixedValue(long value) => new(value);
    //public static implicit operator MixedValue(string value) => new(value);

    //public static explicit operator bool(MixedValue value) => value.AsBool;
    //public static explicit operator long(MixedValue value) => value.AsInt;
    //public static explicit operator string(MixedValue value) => value.AsString;

    public bool Equals(MixedValue other) =>
      _kind == other._kind &&
      _bool == other._bool &&
      _int == other._int &&
      _kind switch
      {
        Kind.String => (string?)_ref == (string?)other._ref,
        Kind.Null => _ref == other._ref,
        _ => true
      };

    
    public override bool Equals(object? obj) =>
      obj is MixedValue other && Equals(other)
      || obj is bool b && _kind == Kind.Bool && _bool == b
      || obj is int i && _kind == Kind.Int && _int == i
      || obj is long l && _kind == Kind.Int && _int == l
      || obj is string s && _kind == Kind.String && (string?)_ref == s
      || obj is IGraphNode g && _kind == Kind.Node && (IGraphNode?)_ref == g;

    
    public override int GetHashCode() =>
      _kind switch
      {
        Kind.Bool => HashCode.Combine(_kind, _bool),
        Kind.Int => HashCode.Combine(_kind, _int),
        Kind.String => HashCode.Combine(_kind, _ref),
        Kind.Node => HashCode.Combine(_kind, _ref),
        _ => (int)_kind
      };

    
    public override string ToString() =>
      _kind switch
      {
        Kind.Null => "null",
        Kind.Bool => _bool.ToString(),
        Kind.Int => _int.ToString(),
        Kind.String => _ref?.ToString() ?? "",
        Kind.Node => "-node-",
        _ => "<undefined>"
      };
  }
}
