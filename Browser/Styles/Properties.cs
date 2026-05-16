using Silk.NET.Maths;

public struct Properties
{
    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);
    public float Transition;

    public Properties()
    {
    }

    public Properties SetBackgroundColor255(float r, float g, float b, float a)
    {
        return SetBackgroundColor(r / 255, g / 255, b / 255, a / 255);
    }

    public Properties SetBackgroundColor(float r, float g, float b, float a)
    {
        BackgroundColor = new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a);
        return this;
    }

    public Properties SetBackgroundColorWithoutTransition(float r, float g, float b, float a)
    {
        BackgroundColor = new(SrgbToLinear(r), SrgbToLinear(g), SrgbToLinear(b), a);
        return this;
    }

    static float SrgbToLinear(float c)
    {
        return c <= 0.04045f
            ? c / 12.92f
            : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    public Properties SetTransition(float transition)
    {
        Transition = transition;
        return this;
    }
}