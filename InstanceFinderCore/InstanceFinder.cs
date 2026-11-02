using System;

namespace InstanceFinderCore;

public static class InstanceFinder
{
    static List<Type> instanceToCreate = [];
    static Dictionary<Type, object> instances = [];

    static List<IInstanceInit> initable = [];

    public static void CreateInstances()
    {
        foreach (var item in instanceToCreate)
        {
            object? _instance = Activator.CreateInstance(item);
            instances.Add(item, _instance!);
            
            if (_instance is IInstanceInit instanceInit)
                initable.Add(instanceInit);
        }
    }

    public static void InitInstances()
    {
        foreach (var item in initable)
        {
            item.Init();
        }
    }

    public static void AddInstance<Instance>()
    {
        instanceToCreate.Add(typeof(Instance));
    }

    public static Instance GetInstance<Instance>()
    {
        return (Instance)instances[typeof(Instance)];
    }
}
