using System;
 
namespace InstanceFinderCore;

public static class InstanceFinder
{
    static List<Type> instanceToCreate = [];
    static Dictionary<uint, Dictionary<Type, object>> instances = [];

    static byte newSiteThrad = 0;

    public static byte RegisterSiteThread()
    {
        var _siteId = newSiteThrad;
        newSiteThrad++;

        instances.Add(_siteId, new());

        foreach (var item in instanceToCreate)
        {
            instances[_siteId].Add(item, Activator.CreateInstance(item));
        }

        return _siteId;
    }

    public static void AddInstance<Type>()
    {
        instanceToCreate.Add(typeof(Type));
    }

    public static Type GetInstance<Type>(uint thread)
    {
        return (Type)instances[thread][typeof(Type)];
    }
}
