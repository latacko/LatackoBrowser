using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace HTMLParser.DocumentElements;

public class Element
{
    public override string ToString()
    {
        return PrintView();
    }

    public virtual string PrintView()
    {
        return "<empty/>";
    }

    public virtual string GetNode()
    {
        return "<empty>";
    }
}
