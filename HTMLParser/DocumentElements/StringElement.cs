using System;

namespace HTMLParser.DocumentElements;

public class StringElement : Element
{
    public string Content { get; internal set; }

    public override string PrintView()
    {
        return "<string>" + Content.Replace("\n", "\\n") + "</string>";
    }

    public override string GetNode()
    {
        return "<string>" + Content.Replace("\n", "\\n") + "</string>";
    }
}
