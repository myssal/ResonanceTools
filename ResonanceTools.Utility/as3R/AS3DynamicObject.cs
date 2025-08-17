using System;

namespace ResonanceTools;

public class AS3DynamicObject
{
    private readonly Dictionary<string, object> _props = new();

    // Optional: an AS3-style "undefined" placeholder (the IL2CPP has one in FlashCore.static_fields.undefined)
    public static readonly object Undefined = new object();

    public object this[string key]
    {
        get => _props.TryGetValue(key, out var v) ? v : Undefined;
        set => _props[key] = value;
    }

    public IReadOnlyDictionary<string, object> Properties => _props;

    public void Set(string key, object value) => _props[key] = value;
}
