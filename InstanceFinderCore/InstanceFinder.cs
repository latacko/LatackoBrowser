using System;
 
namespace InstanceFinderCore;

public static class InstanceFinder
{
    static List<Type> instanceToCreate = [];
    static Dictionary<Type, object> instances = [];

    public static void CreateInstances()
    {
        foreach (var item in instanceToCreate)
        {
            instances.Add(item, Activator.CreateInstance(item));
        }
    }

    public static void AddInstance<Type>()
    {
        instanceToCreate.Add(typeof(Type));
    }

    public static Type GetInstance<Type>()
    {
        return (Type)instances[typeof(Type)];
    }
}
