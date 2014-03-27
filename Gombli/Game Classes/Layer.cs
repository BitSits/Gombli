using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;

namespace Gombli
{
    class Layer
    {
        public Texture2D[] Textures { get; private set; }
        public float ScrollRate { get; private set; }

        public Layer(ContentManager Content, string basePath, float scrollRate)
        {
            // Assumes each layer only has 3 segments.
            int numberOfTex = 2;
            Textures = new Texture2D[numberOfTex];
            for (int i = 0; i < numberOfTex; i++)
                Textures[i] = Content.Load<Texture2D>(basePath + "_" + i);

            ScrollRate = scrollRate;
        }

        public void Draw(SpriteBatch spriteBatch, float cameraPositionX)
        {
            // Assume each segment is the same width.
            int segmentWidth = Textures[0].Width;

            // Calculate which segments to draw and how much to offset them.
            float x = cameraPositionX * ScrollRate;
            int leftSegment = (int)Math.Floor(x / segmentWidth);
            int rightSegment = leftSegment + 1;

            x = (x / segmentWidth - leftSegment) * -segmentWidth;
            spriteBatch.Draw(Textures[leftSegment % Textures.Length], new Vector2(x, 0.0f), Color.White);
            spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segmentWidth, 0.0f), Color.White);
        }
    }
}
