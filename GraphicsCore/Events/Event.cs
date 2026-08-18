using System;
using System.Numerics;
using Silk.NET.Maths;

namespace GraphicsCore.Events;

public class Event
{
    public delegate void ElementEvent(RuntimeModelData element);
    readonly List<RuntimeModelData> elements = new();

    public void AddElement(RuntimeModelData element)
    {
        elements.Add(element);
    }

    public void Removelement(RuntimeModelData element)
    {
        elements.Remove(element);
    }

    public RuntimeModelData? Process(Vector2 pos)
    {
        for (int i = elements.Count-1; i >= 0; i--)
        {
            if (elements[i].GetBounds().Contains(pos))
            {
                return elements[i];
            }
        }
        return null;
    }
}
