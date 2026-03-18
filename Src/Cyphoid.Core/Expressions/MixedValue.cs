using System.ComponentModel.DataAnnotations;

namespace Cyphoid.Core.Expressions
{
  public readonly struct MixedValue : IEquatable<MixedValue>
  {
    public enum Kind : byte
    {
      Null = 0,
      Bool = 1,
      Int = 2,
      String = 3
    }

    private readonly Kind _kind;
    private readonly bool _bool;
    private readonly long _int;
    private readonly string? _string;


    public MixedValue()
    {
      _kind = Kind.Null;
      _bool = default;
      _int = default;
      _string = default;
    }

    private MixedValue(bool value)
    {
      _kind = Kind.Bool;
      _bool = value;
      _int = default;
      _string = default;
    }

    private MixedValue(long value)
    {
      _kind = Kind.Int;
      _bool = default;
      _int = value;
      _string = default;
    }

    private MixedValue(string value)
    {
      ArgumentNullException.ThrowIfNull(value);
      _kind = Kind.String;
      _bool = default;
      _int = default;
      _string = value;
    }

    public static MixedValue Null() => new();

    public static MixedValue Bool(bool value) => new(value);
    
    public static MixedValue Int(long value) => new(value);
    
    public static MixedValue String(string value) => new(value);

    public static MixedValue FromObject(object? value) =>
      value is bool b ? Bool(b)
      : value is int i ? Int(i)
      : value is long l ? Int(l)
      : value is string s ? String(s)
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
        _kind == Kind.String ? _string! : throw new InvalidOperationException($"Value is not a string. Got {_kind} {ToString()}.");

    public object? AsObject() => Match<object?>(
      () => null,
      b => b,
      i => i,
      s => s);

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
      value = _string;
      return _kind == Kind.String;
    }

    public T Match<T>(
      Func<T> onNull,
      Func<bool, T> onBool,
      Func<long, T> onInt,
      Func<string, T> onString)
    {
      return _kind switch
      {
        Kind.Null => onNull(),
        Kind.Bool => onBool(_bool),
        Kind.Int => onInt(_int),
        Kind.String => onString(_string!),
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
      _string == other._string;

    
    public override bool Equals(object? obj) =>
      obj is MixedValue other && Equals(other)
      || obj is bool b && _kind == Kind.Bool && _bool == b
      || obj is int i && _kind == Kind.Int && _int == i
      || obj is long l && _kind == Kind.Int && _int == l
      || obj is string s && _kind == Kind.String && _string == s;

    
    public override int GetHashCode() =>
      _kind switch
      {
        Kind.Bool => HashCode.Combine(_kind, _bool),
        Kind.Int => HashCode.Combine(_kind, _int),
        Kind.String => HashCode.Combine(_kind, _string),
        _ => (int)_kind
      };

    
    public override string ToString() =>
      _kind switch
      {
        Kind.Null => "null",
        Kind.Bool => _bool.ToString(),
        Kind.Int => _int.ToString(),
        Kind.String => _string!,
        _ => "<undefined>"
      };
  }
}
