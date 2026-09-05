using System;
using System.Collections.Generic;
using Msdfgen;
using SharpFont;

namespace MsdfAtlasGen
{
    public class GlyphGeometry
    {
        public struct GlyphAttributes
        {
            public double Scale;
            public Msdfgen.Range Range;
            public Padding InnerPadding;
            public Padding OuterPadding;
            public double MiterLimit;
            public bool PxAlignOriginX;
            public bool PxAlignOriginY;
        }

        private char character;
        private double _geometryScale;
        private Shape? _shape;
        private Shape.Bounds _bounds; // scaled bounds in pixels at target font size
        private double _advance;      // scaled advance in pixels at target font size

        // Store original metrics before geometry scaling for FNT export
        private double _advanceUnscaled;   // advance in pixels at target font size (no extra scaling later)
        private Shape.Bounds _boundsUnscaled; // bounds in font-space (fontSize = 1.0) for reference

        private struct Box
        {
            public Rectangle Rect;
            public Msdfgen.Range Range;
            public double Scale;
            public Vector2 Translate;
            public Padding OuterPadding;
        }
        private Box _box;

        public GlyphGeometry()
        {
        }

        /// <summary>
        /// Loads glyph geometry from font by glyph index using FreeType (direct port of C++ implementation)
        /// </summary>
        public bool Load(Face face, double geometryScale, char char_index)
        {
            _shape = new Shape();
            if (face == null)
                return false;
            face.LoadChar(char_index, LoadFlags.NoScale | LoadFlags.NoBitmap, LoadTarget.Normal);

            BuildShape(face, char_index, 0, 0, false);

            _geometryScale = geometryScale;
            character = char_index;
            _advance = face.Glyph.Advance.X * geometryScale;
            _advanceUnscaled = _advance;

            _shape.Normalize();
            _bounds = _shape.GetBounds();
            _boundsUnscaled = _bounds;

            // Determine if shape is winded incorrectly and reverse it in that case
            // Match C++ behavior: check winding when Skia is not used
            Vector2 outerPoint = new Vector2(
                _bounds.L - (_bounds.R - _bounds.L) - 1,
                _bounds.B - (_bounds.T - _bounds.B) - 1
            );
            // Use ShapeDistanceFinder with SimpleTrueShapeDistanceFinder
            var combiner = new SimpleContourCombiner<TrueDistanceSelector>(_shape);
            var finder = new ShapeDistanceFinder<SimpleContourCombiner<TrueDistanceSelector>>(_shape, combiner);
            double distance = finder.Distance(outerPoint);
            if (distance > 0)
            {
                foreach (var contour in _shape.Contours)
                {
                    contour.Reverse();
                }
            }

            if (!_shape.Validate())
                return false;

            return true;
        }

        record struct PointInfo
        {
            public Vector2 Pos;
            public byte Tag;
        }

        Shape BuildShape(Face face, char c, float leftPadding, float topPadding, bool insideOut)
        {
            Vector2 GetPointCoords(FTVector pos)
            {
                // return new Vector2(pos.X.Value, pos.Y.Value);
                return new Vector2(pos.X.Value - leftPadding, pos.Y.Value + topPadding);
            }
            var outline = face.Glyph.Outline;
            var shape = new Shape();

            int contourStart = 0;
            List<PointInfo> _points = new();
            for (int i = 0; i < outline.Contours.Length; i++)
            {
                int contourEnd = outline.Contours[i];

                Contour contour = new();

                //? Iterating throught points

                _points.Clear();
                for (int j = contourStart; j <= contourEnd; j++)
                {
                    _points.Add(new()
                    {
                        Pos = GetPointCoords(outline.Points[j]),
                        Tag = outline.Tags[j],
                    });

                }

                if (insideOut)
                    _points.Reverse();

                int _outlineStart = 0;
                for (int j = 0; j < _points.Count; j++)
                {
                    if ((_points[j].Tag & 0b00000001) == 0)
                    {
                        if (j + 1 < _points.Count && (_points[j + 1].Tag & 0b00000001) == 0)
                        {
                            _points.Insert(j + 1, new()
                            {
                                Pos = Vector2Extensions.Lerp(_points[j].Pos, _points[j + 1].Pos, 0.5f),
                                Tag = 0b00000001,
                            });
                        }
                        continue;
                    }

                    if (j == 0)
                    {
                        continue;
                    }

                    int _outLineEnds = j;

                    Vector2 _startPoint = _points[_outlineStart].Pos;
                    Vector2 _endPoint = _points[_outLineEnds].Pos;


                    switch (_outLineEnds - _outlineStart)
                    {
                        case 1:
                            // Console.WriteLine(" linear");
                            contour.Edges.Add(new LinearSegment(_startPoint, _endPoint, EdgeColor.WHITE));
                            break;
                        case 2:
                            // Console.WriteLine(" quadratic");
                            Vector2 _controlPoint = _points[_outlineStart + 1].Pos;
                            contour.Edges.Add(new QuadraticSegment(_startPoint, _controlPoint, _endPoint, EdgeColor.WHITE));
                            break;
                        case 3:
                            _controlPoint = _points[_outlineStart + 1].Pos;
                            Vector2 _controlPoint2 = _points[_outlineStart + 2].Pos;

                            contour.Edges.Add(new CubicSegment(_startPoint, _controlPoint, _controlPoint2, _endPoint, EdgeColor.WHITE));
                            break;
                        default:
                            throw new Exception("Wrong contour for char: " + c);
                    }

                    _outlineStart = _outLineEnds;
                }
                Vector2 _endPoint2 = _points[0].Pos;
                if ((_points[_points.Count - 1].Tag & 0b00000001) == 0)
                {
                    Vector2 _startPos = _points[_points.Count - 2].Pos;
                    Vector2 _controlPoint = _points[_points.Count - 1].Pos;
                    // Console.WriteLine("Closing shape from " + (_points.Count - 2) + " to 0 is quadratic");
                    contour.Edges.Add(new QuadraticSegment(_startPos, _controlPoint, _endPoint2, EdgeColor.WHITE));
                }
                else
                {
                    // Console.WriteLine("Closing shape from " + (_points.Count - 1) + " to 0 is line");
                    Vector2 _startPos = _points[_points.Count - 1].Pos;
                    contour.Edges.Add(new LinearSegment(_startPos, _endPoint2, EdgeColor.WHITE));
                }


                contourStart = contourEnd + 1;

                shape.Contours.Add(contour);
            }

            shape.SetYAxisOrientation(YAxisOrientation.Upward);
            return shape;
        }

        /// <summary>
        /// Performs edge coloring for the glyph's shape.
        /// </summary>
        public void EdgeColoring(EdgeColoringDelegate fn, double angleThreshold, ulong seed)
        {
            fn(_shape!, angleThreshold, seed);
        }

        /// <summary>
        /// Calculates the glyph's bounding box and internal transformation based on the provided attributes.
        /// </summary>
        public void WrapBox(GlyphAttributes glyphAttributes)
        {
            if (_shape == null || _shape.Contours.Count == 0)
            {
                _box.Rect.W = 0; _box.Rect.H = 0;
                _box.Translate = new Vector2(0, 0);
                return;
            }

            // glyphAttributes.Scale is already pixels-per-font-unit for this glyph.
            double scale = glyphAttributes.Scale;
            Msdfgen.Range range = glyphAttributes.Range;
            Padding fullPadding = glyphAttributes.InnerPadding + glyphAttributes.OuterPadding;

            _box.Range = range;
            _box.Scale = scale;

            if (_bounds.L < _bounds.R && _bounds.B < _bounds.T)
            {
                double l = _bounds.L, b = _bounds.B, r = _bounds.R, t = _bounds.T;
                l += range.Lower; b += range.Lower;
                r -= range.Lower; t -= range.Lower;

                if (glyphAttributes.MiterLimit > 0)
                    _shape!.BoundMiters(ref l, ref b, ref r, ref t, -range.Lower, glyphAttributes.MiterLimit, 1);

                l -= fullPadding.L; b -= fullPadding.B;
                r += fullPadding.R; t += fullPadding.T;

                if (glyphAttributes.PxAlignOriginX)
                {
                    int sl = (int)Math.Floor(scale * l - 0.5);
                    int sr = (int)Math.Ceiling(scale * r + 0.5);
                    _box.Rect.W = sr - sl;
                    _box.Translate.X = -sl / scale;
                }
                else
                {
                    double w = scale * (r - l);
                    _box.Rect.W = (int)Math.Ceiling(w);
                    _box.Translate.X = -l + 0.5 * (_box.Rect.W - w) / scale;
                }

                if (glyphAttributes.PxAlignOriginY)
                {
                    int sb = (int)Math.Floor(scale * b - 0.5);
                    int st = (int)Math.Ceiling(scale * t + 0.5);
                    _box.Rect.H = st - sb;
                    _box.Translate.Y = -sb / scale;
                }
                else
                {
                    double h = scale * (t - b);
                    _box.Rect.H = (int)Math.Ceiling(h);
                    _box.Translate.Y = -b + 0.5 * (_box.Rect.H - h) / scale;
                }
                _box.OuterPadding = glyphAttributes.Scale * glyphAttributes.OuterPadding;

                // If after alignment we ended up with non-positive size, zero the rect to avoid blit bleed.
                if (_box.Rect.W <= 0 || _box.Rect.H <= 0)
                {
                    _box.Rect.W = 0; _box.Rect.H = 0;
                    _box.Translate = new Vector2(0, 0);
                }
            }
            else
            {
                _box.Rect.W = 0; _box.Rect.H = 0;
                _box.Translate = new Vector2(0, 0);
            }
        }

        /// <summary>
        /// Sets the position of the glyph's box in the atlas.
        /// </summary>
        public void PlaceBox(int x, int y)
        {
            _box.Rect.X = x;
            _box.Rect.Y = y;
        }

        /// <summary>
        /// Returns the Unicode codepoint.
        /// </summary>
        public char GetCharacter() => character;

        /// <summary>
        /// Returns the scale factor.
        /// </summary>
        public double GetGeometryScale() => _geometryScale;

        /// <summary>
        /// Returns the loaded vector shape.
        /// </summary>
        public Shape? GetShape() => _shape;

        /// <summary>
        /// Returns the shape's original bounds.
        /// </summary>
        public Shape.Bounds GetShapeBounds() => _bounds;

        /// <summary>
        /// Returns the glyph's advance in pixels.
        /// </summary>
        public double GetAdvance() => _advance;

        /// <summary>
        /// Returns the calculated rectangle for the glyph's box.
        /// </summary>
        public Rectangle GetBoxRect() => _box.Rect;

        /// <summary>
        /// Outputs the calculated box rectangle components.
        /// </summary>
        public void GetBoxRect(out int x, out int y, out int w, out int h)
        {
            x = _box.Rect.X; y = _box.Rect.Y;
            w = _box.Rect.W; h = _box.Rect.H;
        }

        /// <summary>
        /// Returns the distance field range for the box.
        /// </summary>
        public Msdfgen.Range GetBoxRange() => _box.Range;

        /// <summary>
        /// Returns the projection information for the box.
        /// </summary>
        public Projection GetBoxProjection() => new Projection(new Vector2(_box.Scale, _box.Scale), _box.Translate);

        /// <summary>
        /// Returns the scale used for the box.
        /// </summary>
        public double GetBoxScale() => _box.Scale;

        /// <summary>
        /// Returns the translation used for the box.
        /// </summary>
        public Vector2 GetBoxTranslate() => _box.Translate;

        /// <summary>
        /// Returns the bounds of the quad in plane space relative to the glyph origin.
        /// </summary>
        public void GetQuadPlaneBounds(out double l, out double b, out double r, out double t)
        {
            if (_box.Rect.W > 0 && _box.Rect.H > 0)
            {
                // Convert back to pixel space: translate is stored in font units, Scale is pixels-per-unit.
                double s = _box.Scale;
                l = -_box.Translate.X * s + _box.OuterPadding.L + 0.5;
                b = -_box.Translate.Y * s + _box.OuterPadding.B + 0.5;
                r = -_box.Translate.X * s + (-_box.OuterPadding.R + _box.Rect.W - 0.5);
                t = -_box.Translate.Y * s + (-_box.OuterPadding.T + _box.Rect.H - 0.5);
            }
            else
            {
                l = b = r = t = 0;
            }
        }

        /// <summary>
        /// Returns the bounds of the glyph box in plane space relative to the glyph origin.
        /// </summary>
        public void GetBoxPlaneBounds(out double l, out double b, out double r, out double t)
        {
            if (_box.Rect.W > 0 && _box.Rect.H > 0)
            {
                double s = _box.Scale;
                l = -_box.Translate.X * s;
                b = -_box.Translate.Y * s;
                r = l + _box.Rect.W;
                t = b + _box.Rect.H;
            }
            else
            {
                l = b = r = t = 0;
            }
        }

        /// <summary>
        /// Returns the quad's bounds within the atlas texture.
        /// </summary>
        public void GetQuadAtlasBounds(out double l, out double b, out double r, out double t)
        {
            if (_box.Rect.W > 0 && _box.Rect.H > 0)
            {
                l = _box.Rect.X + _box.OuterPadding.L + 0.5;
                b = _box.Rect.Y + _box.OuterPadding.B + 0.5;
                r = _box.Rect.X - _box.OuterPadding.R + _box.Rect.W - 0.5;
                t = _box.Rect.Y - _box.OuterPadding.T + _box.Rect.H - 0.5;
            }
            else
            {
                l = b = r = t = 0;
            }
        }

        /// <summary>
        /// Determines if the glyph is whitespace (has no contours).
        /// </summary>
        public bool IsWhitespace() => _shape?.Contours.Count == 0;

        /// <summary>
        /// Returns the pixel size of the glyph's box.
        /// </summary>
        public void GetBoxSize(out int w, out int h)
        {
            w = _box.Rect.W;
            h = _box.Rect.H;
        }

        /// <summary>
        /// Converts the glyph geometry to a lightweight GlyphBox representation.
        /// </summary>
        public GlyphBox ToGlyphBox()
        {
            var box = new GlyphBox();
            box.Advance = _advance;
            box.Bounds = new GlyphBox.GlyphBounds();
            GetQuadPlaneBounds(out box.Bounds.L, out box.Bounds.B, out box.Bounds.R, out box.Bounds.T);
            box.Rect = _box.Rect;
            return box;
        }

        /// <summary>
        /// Returns the unscaled glyph advance.
        /// </summary>
        public double GetAdvanceUnscaled() => _advanceUnscaled;

        /// <summary>
        /// Returns the unscaled shape bounds.
        /// </summary>
        public Shape.Bounds GetBoundsUnscaled() => _boundsUnscaled;

        /// <summary>
        /// Returns the scaled shape bounds.
        /// </summary>
        public Shape.Bounds GetBoundsScaled() => _bounds;
    }
}
