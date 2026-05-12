using System;

namespace HTMLParser.DocumentElements;

public class HtmlElement : DomElement
{
    public override string PrintView()
    {
        return "<" + NodeName + (Attributes != null && Attributes.Count > 0 ? " " + ParseAttributes() : " ") + ">" + StringContent() + "</" + NodeName + ">";
    }

    public string StringContent()
    {
        string str = "";
        if (Childrens != null)
        {
            foreach (var item in Childrens)
            {
                if (item.GetType() == typeof(StringElement))
                {
                    str += ((StringElement)item).Content.Replace("\n", "\\n");
                }
            }
        }

        return str;
    }
}
