using System;
using Microsoft.Xna.Framework;

namespace Gombli
{
    public static class RtAngledTriangle
    {
        public static float GetIntersectionDepthY(Vector2 position, Rectangle tileBounds, float slope)
        {
            if (position.X < tileBounds.Left)
                return slope == (int)TileCollision.SlopeMinus ? 0.0f : tileBounds.Top - position.Y;

            if (position.X > tileBounds.Right)
                return slope == (int)TileCollision.SlopePlus ? 0.0f : tileBounds.Top - position.Y;

            return GetIntersectionDepth(position, tileBounds, slope).Y;
        }

        public static bool IsAbove(Vector2 position, Rectangle tileBounds, float slope)
        {
            Vector2 left = new Vector2(tileBounds.Left, slope == (int)TileCollision.SlopeMinus ?
                                        tileBounds.Bottom : tileBounds.Top);
            Vector2 right = new Vector2(tileBounds.Right, slope == (int)TileCollision.SlopeMinus ?
                                        tileBounds.Top : tileBounds.Bottom);

            slope = (right.Y - left.Y) / (right.X - left.X);
            float c = left.Y - slope * left.X;

            float a = position.Y - position.X * slope - c;  // y - mx - c <= 0
            return a <= 0;
        }

        public static Vector2 GetIntersectionDepth(Vector2 position, Rectangle tileBounds, float slope)
        {
            Vector2 left = new Vector2(tileBounds.Left, slope == (int)TileCollision.SlopeMinus ?
                            tileBounds.Bottom : tileBounds.Top);
            Vector2 right = new Vector2(tileBounds.Right, slope == (int)TileCollision.SlopeMinus ?
                                        tileBounds.Top : tileBounds.Bottom);

            slope = (right.Y - left.Y) / (right.X - left.X);
            float c = left.Y - slope * left.X;

            Vector2 depth;
            depth.Y = (slope * position.X + c) - position.Y; // y = mx + c
            depth.X = (position.Y - c) / slope - position.X; // x = (y - c) / m

            return depth;
        }
    }
}
