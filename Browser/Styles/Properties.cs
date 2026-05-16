using Silk.NET.Maths;

public struct Properties
{
    public Vector4D<float> BackgroundColor = new(1, 1, 1, 1);

    public Properties()
    {
    }

    public Properties SetBackgroundColor255(float r, float g, float b, float a)
    {
        BackgroundColor = new(r / 255, g / 255, b / 255, a/255);
        return this;
    }

    public Properties SetBackgroundColor(float r, float g, float b, float a)
    {
        BackgroundColor = new(r, g, b, a);
        return this;
    }
}