using System.Collections;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HTMLParser.DocumentElements;

namespace SvgLoader;

public static class Loader
{

    // Fist parser: 30187ms
    // Hange to switch 

    public static List<Vector2[]> Parse(string svgContent)
    {
        var _document = new HTMLParser.DocumentParser(svgContent, true).Get();
        Console.WriteLine("Size of my document: " + ObjectSizeEstimator.GetSize(_document));

        HtmlElement _htmlElement = (HtmlElement)_document.Elements[0];
        foreach (var item in _htmlElement.Attributes)
        {
            Console.WriteLine(item.Key + ": " + item.Value);
        }

        foreach (var item in _htmlElement.Childrens)
        {
            if (item is StringElement str)
            {
                Console.WriteLine("String " + str.Content.Replace("\n", "\\n"));
            }
            else if (item is CommentElement comment)
            {
                Console.WriteLine("Comment: " + comment.Content);
            }
            else if (item is HtmlElement htmlElement)
            {
                Console.WriteLine("Node: " + htmlElement.NodeName);
                foreach (var att in htmlElement.Attributes)
                {
                    Console.WriteLine(att.Key + ": " + att.Value);
                }
            }
        }

        return new();
    }
}


public static class ObjectSizeEstimator
{
    private static readonly HashSet<object> Visited =
        new HashSet<object>(ReferenceEqualityComparer.Instance);

    public static long GetSize(object obj)
    {
        Visited.Clear();
        return GetSizeRecursive(obj);
    }

    private static long GetSizeRecursive(object obj)
    {
        if (obj == null)
            return 0;

        if (Visited.Contains(obj))
            return 0;

        Visited.Add(obj);

        Type type = obj.GetType();

        // STRING
        if (obj is string str)
        {
            return
                24 +                 // object + string overhead
                (str.Length * 2);    // chars
        }

        // ARRAY
        if (type.IsArray)
        {
            long size = 24;

            foreach (var item in (Array)obj)
                size += GetSizeRecursive(item);

            return size;
        }

        // VALUE TYPE
        if (type.IsValueType)
        {
            try
            {
                return Marshal.SizeOf(type);
            }
            catch
            {
                return 16;
            }
        }

        // COLLECTIONS
        if (obj is IEnumerable enumerable)
        {
            long size = 32;

            foreach (var item in enumerable)
                size += GetSizeRecursive(item);

            return size;
        }

        // NORMAL OBJECT
        long total = 24;

        var fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            object value = field.GetValue(obj);

            // reference itself
            if (!field.FieldType.IsValueType)
                total += IntPtr.Size;

            if (value == null)
                continue;

            if (field.FieldType.IsValueType)
            {
                try
                {
                    total += Marshal.SizeOf(field.FieldType);
                }
                catch
                {
                    total += 16;
                }
            }
            else
            {
                total += GetSizeRecursive(value);
            }
        }

        return total;
    }

    private sealed class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public override bool Equals(object x, object y)
            => ReferenceEquals(x, y);

        public override int GetHashCode(object obj)
            => RuntimeHelpers.GetHashCode(obj);
    }
}