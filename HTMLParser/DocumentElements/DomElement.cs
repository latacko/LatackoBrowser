using System.Text;
using NodeBehaviour;

namespace HTMLParser.DocumentElements;

public class DomElement : Element
{
    public string NodeName { get; private protected set; }
    public Dictionary<string, string> Attributes { get; private protected set; }
    public List<Element> Childrens { get; private protected set; }

    internal void SetNodeName(string nodeName)
    {
        NodeName = nodeName;
    }

    internal void AddAttribute(string key, string value)
    {
        Attributes ??= new Dictionary<string, string>(PredefinedNodesBehaviour.Get(NodeName).InitialAttributesDictionarySize);
        // int beforeCapacity = Attributes.EnsureCapacity(Attributes.Count);
        Attributes.Add(key, value);
        // int afterCapacity = Attributes.EnsureCapacity(Attributes.Count);
        // if (afterCapacity > beforeCapacity)
        // {
        //     Console.WriteLine($"Resized for: "+NodeName+$" {beforeCapacity} → {afterCapacity}");
        // }
    }

    public override string ToString()
    {
        return PrintView();
    }

    public override string PrintView()
    {
        return "<" + NodeName + (Attributes != null && Attributes.Count > 0 ? " " + ParseAttributes() : " ") + "/>";
    }

    public override string GetNode()
    {
        return "<" + NodeName + (Attributes != null && Attributes.Count > 0 ? " " + ParseAttributes() : "") + ">";
    }

    private protected string ParseAttributes()
    {
        string _attributes = "";
        foreach (var item in Attributes)
        {
            _attributes += item.Key + "=\"" + item.Value + "\"" + " ";
        }
        return _attributes.Substring(0, _attributes.Length - 1);
    }

    internal void SetChildrenList()
    {
        Childrens = new List<Element>(PredefinedNodesBehaviour.Get(NodeName).InitialChildrenListSize);
    }

    internal void SetAttributes(Dictionary<string, string> attributes)
    {
        Attributes = attributes;
    }
}
