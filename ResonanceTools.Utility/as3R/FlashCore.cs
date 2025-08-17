using System;

namespace ResonanceTools;

public class FlashCore
{
    public static AS3DynamicObject CreateObject(object[] args)
    {
        var obj = new AS3DynamicObject();

        if (args == null || args.Length == 0)
            return obj;

        if ((args.Length & 1) != 0)
            throw new InvalidDataException("createObject expects an even number of arguments (key/value pairs).");

        for (int i = 0; i < args.Length; i += 2)
        {
            var key = args[i]?.ToString() ?? string.Empty;
            var value = args[i + 1];
            obj.Set(key, value);
        }

        return obj;
    }
}
