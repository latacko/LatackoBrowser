using System;
using System.Numerics;
using Silk.NET.Maths;

namespace GraphicsCore.Events;

public class Event
{
    public delegate void ElementEvent(VisualElement element);
    readonly List<VisualElement> elements = new();
    readonly Dictionary<VisualElement, ElementEvent> elementEvents = [];

    public void AddElement(VisualElement element, ElementEvent elementEvent)
    {
        elements.Add(element);
        if (!elementEvents.TryAdd(element, elementEvent))
            elementEvents[element] += elementEvent;
    }

    public void Removelement(VisualElement element, ElementEvent elementEvent)
    {
        elements.Remove(element);
        if (elementEvents.TryGetValue(element, out var ev))
        {
            ev -= elementEvent;
            if (ev == null) elementEvents.Remove(element);
            else elementEvents[element] = ev;
        }
    }

    public VisualElement? Process(Vector2 pos)
    {
        for (int i = elements.Count - 1; i >= 0; i--)
        {
            if (elements[i].GetBounds().Contains(pos))
            {
                return elements[i];
            }
        }
        return null;
    }
}
