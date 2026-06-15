using GraphicCore;
using GraphicsCore;
using ObjectCore.Styles;
using ObjectCore.Textures;
using Silk.NET.Maths;
using Vulkan;

namespace ObjectCore;

public class RuntimeObject : RuntimeModelData<RuntimeObject, ObjectData, ObjectModelData<ushort>>
{
    public Style Style;
    ComputedStyle computedStyle;
    Vector2D<float> worldPosition;

    public List<RuntimeModelData> Children;
    public Texture texture;

    public RuntimeObject(ObjectModelData<ushort> modelData, uint objectIndex, Texture texture, RuntimeModelData? parent = null) : base(modelData, objectIndex, parent)
    {
        this.texture = texture;
    }

    public RuntimeObject SetStyle(Style style)
    {
        Style = style;
        StylesManager.AddInlineStyle(style);
        return this;
    }

    protected internal override void UpdateMySize(bool informChildren = false)
    {
        base.UpdateMySize(informChildren);

        var _prevSize = computedStyle.Size;
        computedStyle = Style.ComputeStyles(false, true, ParentSize, computedStyle.Size);

        if (_prevSize == computedStyle.Size) return;
        // Layout.UpdateChildrenLayout();


        if (!informChildren || Children == null) return;

        foreach (var children in Children)
        {
            children.UpdateMySize();
        }
    }

    protected internal override void UpdatePosition()
    {
        relativePos = (Parent != null ? Parent.relativePos : new Vector3D<float>()) + new Vector3D<float>(GetLayoutLeft(), GetLayoutTop(), 0);
        relativeRot = (Parent != null ? Parent.relativeRot : new Vector3D<float>()) + Style.Transform.Rotation;
        relativeTransformation = (Parent != null ? Parent.relativeTransformation : new Vector3D<float>()) + new Vector3D<float>(computedStyle.Translate.X, computedStyle.Translate.Y, 0);

        AddFlag(DirtyFlags.Matrix);
        // Layout.UpdateBoundsOffset();
        if (Children == null) return;

        foreach (var child in Children)
        {
            child.UpdatePosition();
        }
    }

    protected internal override void ConvertToPx()
    {
        // Layout.ConvertToPx(ParentSize);
        // Transform.ConvertToPx(Layout.GetSize());
        // Properties.ConvertToPx(Layout.GetSize());

        UpdatePosition();

        AddFlag(DirtyFlags.Matrix);
    }

    protected internal override float GetLayoutLeft() => computedStyle.Pos.X;
    protected internal override float GetLayoutTop() => computedStyle.Pos.Y;
    protected internal override Vector2D<float> GetLayoutSize() => computedStyle.Size;
    public override Bounds GetBounds() => new();
    public override CursorType GetCursorType() => Style.Properties.Cursor;

    protected internal override void UpdateChildrenLayout()
    {
        // Layout.UpdateChildrenLayout();
    }

    protected internal override void UpdateLayout(ref float cursorX, ref float cursorY, Action newLine, Action<float> updateSizeOfLine, ref float width)
    {
        updateSizeOfLine(GetLayoutSize().Y);

        switch (Style.Layout.Display)
        {
            case Layout.DisplayType.inline:
                if (cursorX + computedStyle.Size.X > width)
                    newLine.Invoke();

                // Layout.LayoutPos = new(cursorX, cursorY);
                cursorX += computedStyle.Size.X;

                break;
            case Layout.DisplayType.block:
                newLine.Invoke();

                // Layout.LayoutPos = new Vector2D<float>(cursorX, cursorY);
                // Layout.BaseSize = new(width, 0);
                break;
        }
    }

    public override void AddChild(RuntimeModelData runtimeModelData)
    {
        Children ??= new();
        Children.Add(runtimeModelData);
        // Layout.UpdateChildrenLayout();
        runtimeModelData.UpdatePosition();
    }

    public override bool TryGetObjectData(out ObjectData data, uint frame)
    {
        if (Swapchain.Instance.recreatedSwapChain)
        {
            UpdateMySize();
            AddFlag(DirtyFlags.Matrix);
        }

        if (dirty[frame] == DirtyFlags.None)
        {
            data = default;
            return false;
        }


        if (dirty[frame].HasFlag(DirtyFlags.Matrix))
        {
            // Task.Run(() =>
            // {
            // });
            cachedModel =
                Matrix4X4.CreateScale(computedStyle.Size.X, computedStyle.Size.Y, 1f) *
                Matrix4X4.CreateTranslation(-computedStyle.Translate.X, -computedStyle.Translate.Y, 0f) *
                Matrix4X4.CreateFromYawPitchRoll(Style.Transform.Rotation.X, Style.Transform.Rotation.Y, Style.Transform.Rotation.Z) *
                Matrix4X4.CreateTranslation(worldPosition.X, worldPosition.Y, 0f);

            if (Parent != null)
            {
                cachedModel *=
                    Matrix4X4.CreateFromYawPitchRoll(Parent.relativeRot.X, Parent.relativeRot.Y, Parent.relativeRot.Z) *
                    Matrix4X4.CreateTranslation(Parent.relativePos.X, Parent.relativePos.Y, Parent.relativePos.Z);
            }
            RemoveFlag(DirtyFlags.Matrix, frame);
        }

        data = new ObjectData
        {
            Model = cachedModel,
            Color = Style.Properties.BackgroundColor,

            pos = new Vector2D<float>(cachedModel.M41, cachedModel.M42),
            size = computedStyle.Size,

            TextureIndex = texture == null ? uint.MaxValue : texture.GetID(),
            borderRadiusTopLeft = computedStyle.BorderRadius.X,
            borderRadiusTopRight = computedStyle.BorderRadius.Y,
            borderRadiusBottomRight = computedStyle.BorderRadius.Z,
            borderRadiusBottomLeft = computedStyle.BorderRadius.W,
        };

        RemoveFlag(DirtyFlags.Data, frame);

        return true;
    }
}
