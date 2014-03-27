using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class MovingTile
    {
        private Texture2D texture;
        private Vector2 origin;

        public Level Level { get { return level; } }
        Level level;

        Vector2 position;

        Vector2 velocity;
        public Vector2 Velocity { get { return velocity; } }

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this enemy in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(position.X - origin.X) + localBounds.X;
                int top = (int)Math.Round(position.Y - origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        public Rectangle PrevBoundingRectangle { get; private set; }

        /// <summary>
        /// The direction this enemy is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Left;
        private Orientation dirType = Orientation.Horizontal;

        /// <summary>
        /// How long this enemy has been waiting before turning around.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 0.92f;

        /// <summary>
        /// The speed at which this enemy moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 150.0f;

        public MovingTile(Level level, int index, Vector2 basePosition, Orientation type)
        {
            this.level = level;
            this.position = basePosition;
            this.dirType = type;

            LoadContent(index);
        }

        public void LoadContent(int index)
        {
            texture = Level.Content.Load<Texture2D>("Tiles//Cloud" + index);
            origin = new Vector2(texture.Width / 2, texture.Height);

            localBounds = new Rectangle(0, 0, texture.Width, texture.Height);
        }

        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position based on the side we are walking towards.
            float posX = position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(position.Y / Tile.Height);

            PrevBoundingRectangle = BoundingRectangle;

            if (waitTime > 0)
            {
                // Wait for some amount of time.
                waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (waitTime <= 0.0f)
                {
                    // Then turn around.
                    direction = (FaceDirection)(-(int)direction);
                }
            }
            else
            {
                // If we are about to run into a wall or off a cliff, start waiting.
                TileCollision collision = Level.GetCollision(tileX + (int)direction, tileY - 1);
                if (collision != TileCollision.Passable)
                {
                    waitTime = MaxWaitTime;
                    velocity = Vector2.Zero;
                }
                else
                {
                    // Move in the current direction.
                    velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                    position = position + velocity;

                    position = new Vector2((float)Math.Round(position.X), (float)Math.Round(position.Y));
                }
            }
        }

        /// <summary>
        /// Draws the moving tile in appropriate position.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
        }
    }
}
