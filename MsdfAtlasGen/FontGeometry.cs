using System;
using System.Collections.Generic;
using System.Linq;
using Msdfgen;
using SharpFont;

namespace MsdfAtlasGen
{
    public class FontGeometry
    {
        public class GlyphRange
        {
            private readonly List<GlyphGeometry> _glyphs;
            private readonly int _rangeStart;
            private readonly int _rangeEnd;

            /// <summary>
            /// Initializes an empty glyph range.
            /// </summary>
            public GlyphRange()
            {
                _glyphs = new List<GlyphGeometry>();
            }

            /// <summary>
            /// Initializes a glyph range from a list of glyphs and a specified range.
            /// </summary>
            public GlyphRange(List<GlyphGeometry> glyphs, int rangeStart, int rangeEnd)
            {
                _glyphs = glyphs;
                _rangeStart = rangeStart;
                _rangeEnd = rangeEnd;
            }

            public int Size => _rangeEnd - _rangeStart;
            public bool Empty => _rangeStart == _rangeEnd;
            public IEnumerable<GlyphGeometry> Glyphs => _glyphs.Skip(_rangeStart).Take(Size);
        }

        private const double DefaultFontUnitsPerEm = 2048.0;

        private double _geometryScale = 1;
        private FontMetrics _metrics = new();
        private List<GlyphGeometry> _glyphs = new();
        private string loaded_charset;
        private readonly Dictionary<char, int> _glyphsByCharacter = new();
        private readonly Dictionary<(int, int), double> _kerning = new();
        private string _name = string.Empty;

        /// <summary>
        /// Initializes a new font geometry instance with no glyphs.
        /// </summary>
        public FontGeometry()
        {
        }


        /// <summary>
        /// Loads a set of glyphs by their Unicode codepoints from the specified font.
        /// </summary>
        public int LoadCharset(Face font, double fontScale, string charset, bool enableKerning = true)
        {
            string toLoadCharacters = "";
            for (int i = 0; i < charset.Length; i++)
            {
                if (loaded_charset.Contains(charset[i])) continue;

                loaded_charset += charset[i];
                toLoadCharacters += charset[i];
            }
            if (!LoadMetrics(font, fontScale))
                return -1;

            int loaded = 0;
            foreach (char cp in toLoadCharacters)
            {
                var glyph = new GlyphGeometry();
                if (glyph.Load(font, _geometryScale, cp))
                {
                    AddGlyph(glyph);
                    ++loaded;
                }
            }
            if (enableKerning)
                LoadKerning(font);
            return loaded;
        }

        /// <summary>
        /// Loads font-wide metrics (ascender, descender, line height) from the specified font.
        /// </summary>
        public bool LoadMetrics(Face face, double fontScale)
        {
            if (face == null) return false;

            _metrics = new FontMetrics
            {
                EmSize = face.UnitsPerEM,
                AscenderY = face.Ascender,
                DescenderY = face.Descender,
                LineHeight = face.Height,
                // UnderlineY = ftMetrics.UnderlineY,
                // UnderlineThickness = ftMetrics.UnderlineThickness
            };

            if (_metrics.EmSize <= 0)
                _metrics.EmSize = DefaultFontUnitsPerEm;

            _geometryScale = fontScale / _metrics.EmSize;

            _metrics.EmSize *= _geometryScale;
            _metrics.AscenderY *= _geometryScale;
            _metrics.DescenderY *= _geometryScale;
            _metrics.LineHeight *= _geometryScale;
            _metrics.UnderlineY *= _geometryScale;
            _metrics.UnderlineThickness *= _geometryScale;

            return true;
        }

        /// <summary>
        /// Manually adds a single glyph to the geometry.
        /// </summary>
        bool AddGlyph(GlyphGeometry glyph)
        {
            _glyphs.Add(glyph);
            _glyphsByCharacter.Add(glyph.GetCharacter(), _glyphs.Count-1);
            return true;
        }

        /// <summary>
        /// Loads kerning information for all glyphs currently in the geometry.
        /// </summary>
        public int LoadKerning(Face font)
        {
            _kerning.Clear();
            int loaded = 0;
            for (int i = 0; i < loaded_charset.Length; ++i)
            {
                for (int j = 0; j < loaded_charset.Length; ++j)
                {
                    var _kearingVector = font.GetKerning(loaded_charset[i], loaded_charset[j], KerningMode.Unscaled);
                    if (_kearingVector.X == 0)
                        continue;

                    _kerning[(loaded_charset[i], loaded_charset[j])] = _geometryScale * _kearingVector.X;
                    ++loaded;
                }
            }
            return loaded;
        }

        /// <summary>
        /// Retrieves the kerning dictionary.
        /// </summary>
        public Dictionary<(int, int), double> GetKernings() => _kerning;

        /// <summary>
        /// Sets the name of the font.
        /// </summary>
        public void SetName(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Returns the scale from font units to geometry pixels.
        /// </summary>
        public double GetGeometryScale() => _geometryScale;

        /// <summary>
        /// Returns the font-wide metrics.
        /// </summary>
        public FontMetrics GetMetrics() => _metrics;

        /// <summary>
        /// Returns the range of glyphs managed by this instance.
        /// </summary>
        public List<GlyphGeometry> GetGlyphs() => _glyphs;

        /// <summary>
        /// Retrieves a glyph by its index.
        /// </summary>
        public GlyphGeometry? GetGlyph(char character)
        {
            if (_glyphsByCharacter.TryGetValue(character, out int pos))
                return _glyphs[pos];
            return null;
        }

        /// <summary>
        /// Returns the font name.
        /// </summary>
        public string GetName() => _name;

        /// <summary>
        /// Returns the original Units Per Em before geometry scaling.
        /// </summary>
        public double GetUnitsPerEm() => (_metrics.EmSize > 0) ? _metrics.EmSize / _geometryScale : DefaultFontUnitsPerEm;
    }
}
