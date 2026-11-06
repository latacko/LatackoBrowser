using System;

namespace InstanceFinderCore;

/// <summary>Marks a class to be created automatically at startup.
/// The class needs a public/internal parameterless constructor.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class InstanceAttribute : Attribute
{
    public int Priority { get; set; } = 0;
}