using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class BreakableTile
    {
        private  Texture2D overlay0, overlay1;
        private Texture2D block0, block1;

        private Point position;

        public PowerState State { get; private set; } 

        public Rectangle BoundingRectangle
        {
            get
            {
                return new Rectangle(position.X - Tile.Width / 2, position.Y - Tile.Height * 2,
                    Tile.Width, Tile.Height * 2);
            }
        }

        public BreakableTile(Level level, Point basePosition)
        {
            this.position = basePosition;
            State = PowerState.Ground;

            this.block0 = level.Content.Load<Texture2D>("Tiles/" + level.LevelIndex.ToString() + "_BlockA0");
            this.block1 = level.Content.Load<Texture2D>("Tiles/" + level.LevelIndex.ToString() + "_BlockA1");

            this.overlay0 = level.Content.Load<Texture2D>("Tiles/BreakOverlay0");
            this.overlay1 = level.Content.Load<Texture2D>("Tiles/BreakOverlay1");
        }

        /// <summary>
        /// change state from Ground > Drop > Die
        /// </summary>
        public void ChangeState() { State += 2; }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (State == PowerState.Die) return;

            Rectangle bounds = BoundingRectangle;
            spriteBatch.Draw(block0, new Vector2(bounds.X, bounds.Y + Tile.Height), Color.White);
            spriteBatch.Draw(block1, new Vector2(bounds.X, bounds.Y), Color.White);

            if (State == PowerState.Ground)
                spriteBatch.Draw(overlay0, new Vector2(bounds.X, bounds.Y), Color.White);
            if(State == PowerState.Drop)
                spriteBatch.Draw(overlay1, new Vector2(bounds.X, bounds.Y), Color.White);
        }
    }
}
