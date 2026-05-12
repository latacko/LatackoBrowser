using System;
using System.Web;
using HTMLParser.DocumentElements;

namespace NodeBehaviour;

public class NodeBehaviourInfo
{
    public bool RequiresClosingTag = true;
    public bool CanHaveChildren = true;
    public bool AutoClose = false;
    public int InitialChildrenListSize = 4;
    public int InitialAttributesDictionarySize = 4;
    public Func<DomElement> Factory { get; init; } = static () => new HtmlElement();

    public DomElement GetElement()
    {
        return Factory();
    }
}
