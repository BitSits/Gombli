using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class OzoneTile
    {
        private Texture2D block, blockOFF, blockON, blockSTATIC;

        public bool OriginalGlow { get; private set; }

        public bool IsGlowing { get; private set; }
        private bool isStatic;

        public bool PlayerIsOn { get; set; }

        private Vector2 position;

        public Rectangle BoundingRectangle
        {
            get { return new Rectangle((int)position.X, (int)position.Y, Tile.Width, Tile.Height); }
        }

        public OzoneTile(Level level, Vector2 position, char type)
        {
            this.position = position;
            this.OriginalGlow = this.IsGlowing = type == '0' ? false : true;

            this.isStatic = type == '2' ? true : false;

            blockOFF = level.Content.Load<Texture2D>("Tiles/Ozone0");
            blockON = level.Content.Load<Texture2D>("Tiles/Ozone1");
            blockSTATIC = level.Content.Load<Texture2D>("Tiles/Ozone2");
        }

        public void SwitchGlow() { if (isStatic)return; IsGlowing = IsGlowing ? false : true; }

        public void ResetGlow() { IsGlowing = OriginalGlow; }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            block = isStatic ? blockSTATIC : IsGlowing ? blockON : blockOFF;
            spriteBatch.Draw(block, position, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
        }
    }
}
