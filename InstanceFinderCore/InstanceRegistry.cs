using System;

namespace InstanceFinderCore;

public static class InstanceRegistry
{
    private static readonly Dictionary<Type, object> instances = new();
    private static readonly List<IInstanceInit> initable = new();

    /// <summary>Called by generated code. Not meant to be called manually.</summary>
    public static void Register(object instance)
    {
        instances.Add(instance.GetType(), instance);

        if (instance is IInstanceInit init)
            initable.Add(init);
    }

    public static void InitInstances()
    {
        foreach (var item in initable)
        {
            item.Init();
        }
    }

    public static T Get<T>() where T : class
    {
        if (instances.TryGetValue(typeof(T), out var instance))
            return (T)instance;

        throw new InvalidOperationException($"No [Instance] registered for {typeof(T)}");
    }

    public static bool TryGet<T>(out T? value) where T : class
    {
        if (instances.TryGetValue(typeof(T), out var instance))
        {
            value = (T)instance;
            return true;
        }

        value = null;
        return false;
    }

    public static void Dispose()
    {
        foreach (var item in instances)
        {
            if (item.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

}
