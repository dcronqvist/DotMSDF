using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotMSDF;

public class Charset : IEnumerable<uint>
{
    private readonly HashSet<uint> _codepoints = [];

    public Charset Add(uint codepoint) { _codepoints.Add(codepoint); return this; }
    public Charset Remove(uint codepoint) { _codepoints.Remove(codepoint); return this; }

    public IEnumerator<uint> GetEnumerator() => _codepoints.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static Charset FromRange(uint start, uint end)
    {
        var charset = new Charset();
        for (uint i = start; i <= end; i++)
            charset.Add(i);
        return charset;
    }

    public static Charset ASCII => FromRange(0x20, 0x7E);
}