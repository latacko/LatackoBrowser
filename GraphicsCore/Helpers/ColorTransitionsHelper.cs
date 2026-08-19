using Silk.NET.Maths;

namespace GraphicsCore;

public static class ColorTransitionsHelper
{

    class TransitionData
    {
        public float Duration;
        public Vector4D<float> OriginalColor;
        public Vector4D<float> TargetColor;
        public float Elapsed;
        public required Action<Vector4D<float>> UpdateColor;
    }
    static Dictionary<VisualElement, TransitionData> elements = new();

    public static void StartTransition(VisualElement element, Vector4D<float> originalColor, Vector4D<float> targetColor, float duration, Action<Vector4D<float>> updateColor = null)
    {
        if (elements.TryGetValue(element, out var value))
        {
            value.OriginalColor = originalColor;
            value.TargetColor = targetColor;
            value.Duration = duration;
            value.Elapsed = 0;
        }
        else
        {
            elements.Add(element, new()
            {
                OriginalColor = originalColor,
                TargetColor = targetColor,
                Duration = duration,
                UpdateColor = updateColor,
            });
        }
    }

    public static void Update(float deltaTime)
    {
        foreach (var item in new Dictionary<VisualElement, TransitionData>(elements))
        {
            item.Value.Elapsed += deltaTime;
            float t = Math.Clamp(item.Value.Elapsed / item.Value.Duration, 0f, 1f);

            t = t * t * (3f - 2f * t);

            if (t >= 0.998f)
            {
                item.Value.UpdateColor.Invoke(item.Value.TargetColor);
                elements.Remove(item.Key);
            }
            else
            {
                var _currentColor = Vector4D.Lerp(item.Value.OriginalColor, item.Value.TargetColor, t);
                item.Value.UpdateColor.Invoke(_currentColor);
            }
        }
    }


}