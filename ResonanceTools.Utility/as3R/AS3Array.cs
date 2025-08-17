using System;
using System.Collections.Generic;

namespace ResonanceTools.Utility;

public class AS3Array
{
    private int _endIndex;
    private Dictionary<int, object> dic;

    public AS3Array()
    {
        _endIndex = -1;
        dic = new Dictionary<int, object>();
    }

    public void Push(object item)
    {
        _endIndex++;
        dic[_endIndex] = item;
    }
}
