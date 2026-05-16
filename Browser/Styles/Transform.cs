
using Silk.NET.Maths;
using Silk.NET.SDL;
using Units;

public struct Transform
{
    public enum BoxE : byte
    {
        Content,
        Border,
        Fill,
        Stroke,
        View,
    }
    public BoxE Box;
    public Vector2 Translate;
    public Vector3D<float> Rotation;
    public Vector3D<float> Scale;

    public Transform SetBox(BoxE box)
    {
        Box = box;
        return this;
    }

    public Transform SetTranslate(Vector2 origin)
    {
        Translate = origin;
        return this;
    }

    #region Rotation
    public Transform SetRotation(Vector3D<float> rotation)
    {
        Rotation = rotation;
        return this;
    }

    public Transform SetRotationX(float x)
    {
        Rotation = new(x, Rotation.Y, Rotation.Z);
        return this;
    }

    public Transform SetRotationY(float y)
    {
        Rotation = new(Rotation.X, y, Rotation.Z);
        return this;
    }

    public Transform SetRotationZ(float z)
    {
        Rotation = new(Rotation.X, Rotation.Y, z);
        return this;
    }
    #endregion

    #region Scale
    public Transform SetScale(float scale)
    {
        Scale = new(scale, scale, scale);
        return this;
    }
    public Transform SetScale(Vector3D<float> scale)
    {
        Scale = scale;
        return this;
    }

    public Transform SetScaleX(float x)
    {
        Scale = new(x, Scale.Y, Scale.Z);
        return this;
    }

    public Transform SetScaleY(float y)
    {
        Scale = new(Scale.X, y, Scale.Z);
        return this;
    }

    public Transform SetScaleZ(float z)
    {
        Scale = new(Scale.X, Scale.Y, z);
        return this;
    }
    #endregion

    internal Transform ConvertToPx()
    {
        Translate.ConvertToPx();

        return this;
    }
}