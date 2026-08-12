using System;
using GraphicCore;

namespace GraphicsCore;

public static class ShadersRegistry
{
    static readonly List<Type> registeredShaders = []; 
    public static void RegisterShader(Type shader)
    {
        if (shader != typeof(BaseShader) && !shader.IsSubclassOf(typeof(BaseShader)))
            throw new Exception("You can only register shaders with base class of BaseShader");
        registeredShaders.Add(shader);
    }

    public static BaseShader[] GetShaders()
    {
        BaseShader[] _shaders = new BaseShader[registeredShaders.Count];

        for (int i = 0; i < registeredShaders.Count; i++)
        {
            _shaders[i] = (BaseShader) Activator.CreateInstance(registeredShaders[i])!;
        }

        return _shaders;
    }
}
