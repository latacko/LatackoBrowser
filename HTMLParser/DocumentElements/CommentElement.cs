using System;

namespace HTMLParser.DocumentElements;

public class CommentElement : Element
{
    public string Content { get; internal set; }

    public override string PrintView()
    {
        return "<string>" + Content.Replace("\n", "\\n") + "</string>";
    }

    public override string GetNode()
    {
        return "<comment>" + Content.Replace("\n", "\\n") + "</comment>";
    }
}
