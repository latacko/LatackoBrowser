
using System;
using HTMLParser.DocumentElements;

namespace NodeBehaviour;

public static class PredefinedNodesBehaviour
{
    static readonly NodeBehaviourInfo defaultBahaviour = new();
    static readonly Dictionary<string, NodeBehaviourInfo> behaviours = new()
    {
        ["!DOCTYPE"] = new NodeBehaviourInfo()
        {
            RequiresClosingTag = false,
            AutoClose = true,
            Factory = DomElementFactory,
        },
        ["meta"] = new NodeBehaviourInfo()
        {
            RequiresClosingTag = false,
            CanHaveChildren = false,
            Factory = DomElementFactory,
        },
        ["html"] = new NodeBehaviourInfo()
        {
            Factory = DomElementFactory,
        },
        ["area"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["base"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["br"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["col"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["embed"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["hr"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["img"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
            InitialAttributesDictionarySize = 16,
        },
        ["input"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
        },
        ["link"] = new NodeBehaviourInfo()
        {
            CanHaveChildren = false,
            Factory = DomElementFactory,
        },
        ["body"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=32,
        },
        ["select"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=8,
        },
        ["div"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=8,
        },
        ["head"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=8,
        },
        ["tr"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=8,
        },
        ["ul"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=16,
        },
        ["ol"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=16,
        },
        ["tbody"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=16,
        },
        ["table"] = new NodeBehaviourInfo()
        {
            InitialChildrenListSize=16,
        },
    };

    static Dictionary<string, NodeBehaviourInfo>.AlternateLookup<ReadOnlySpan<char>> behavioursLookup = behaviours.GetAlternateLookup<ReadOnlySpan<char>>();

    static DomElement DomElementFactory()=> new DomElement();

    public static NodeBehaviourInfo Get(ReadOnlySpan<char> nodeName)
    {
        if (behavioursLookup.TryGetValue(nodeName, out var behaviour))
            return behaviour;
        else
            return defaultBahaviour;
    }
}
