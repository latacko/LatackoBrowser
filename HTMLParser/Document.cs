using System.Text;
using HTMLParser.DocumentElements;
using HTMLParser.Enums;

namespace HTMLParser;

public class Document
{
    public readonly List<Element> Elements = new(2);
    public static Document Parse(string document, bool debug = false)
    {
        return new DocumentParser(document, debug).Get();
    }

    public override string ToString()
    {
        string str = "";

        for (int i = 0; i < Elements.Count; i++)
        {
            if (Elements[i].GetType() == typeof(DomElement))
                str += PrintTree((DomElement)Elements[i]) + "\n";
        }
        return str;
    }

    public static int elementCount = 0;

    static string PrintTree(Element node, string indent = "", bool isLast = true)
    {
        string connector = isLast ? "└── " : "├── ";

        string childIndent = indent + (isLast ? "    " : "│   ");

        var sb = new StringBuilder();
        sb.AppendLine(indent + connector + node.GetNode());
        elementCount++;
        if (node.GetType().BaseType == typeof(DomElement) || node.GetType() == typeof(DomElement))
        {
            DomElement element = (DomElement)node;
            if (element.Childrens != null)
            {
                for (int i = 0; i < element.Childrens.Count; i++)
                {
                    sb.Append(PrintTree(element.Childrens[i], childIndent, i == element.Childrens.Count - 1));
                }
            }
        }

        return sb.ToString();
    }
}