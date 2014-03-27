using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    enum AnimalType
    {
        Ground = 0, FLoating = 1, Lazy = 2,
    }

    class Animal
    {
        public Level Level { get { return level; } }
        Level level;

        public int PlayerDamage { get; private set; }

        /// <summary>
        /// Position in world space of the bottom center of this animal.
        /// </summary>
        public Vector2 Position { get { return position; } }
        Vector2 position;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this animal in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        // Animations
        private Animation runAnimation;
        private Animation idleAnimation;
        private AnimationPlayer sprite;

        /// <summary>
        /// The direction this animal is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Left;

        /// <summary>
        /// How long this animal has been waiting before turning around.
        /// </summary>
        private float waitTime;

        private bool isFloating, isLazy;

        private bool isOnSlope;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 2.5f;

        /// <summary>
        /// The speed at which this animal moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 30.5f;

        /// <summary>
        /// Constructs a new Animal.
        /// </summary>
        public Animal(Level level, Vector2 position, string spriteSet, 
            bool isFloating, bool isLazy, int playerDamage)
        {
            this.level = level;
            this.position = position;
            this.isFloating = isFloating;
            this.isLazy = isLazy;
            PlayerDamage = playerDamage;

            LoadContent(spriteSet);
        }

        /// <summary>
        /// Loads a particular animal sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Animal/" + spriteSet + "/";
            if (!isLazy)
                runAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_Run"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_Idle"), 0.5f, true);
            sprite.PlayAnimation(idleAnimation);

            Vector2 fraction;
            if (isLazy) fraction = new Vector2(0.59f, 0.92f);
            else if (isFloating) fraction = new Vector2();
            else fraction = new Vector2(0.25f, 0.65f);

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * fraction.X);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * fraction.Y);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public void OnAnimalHurt()
        {
            waitTime = MaxWaitTime;
        }

        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (isLazy) return;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Calculate tile position based on the side we are walking towards.
            float posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);

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
                if (Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable ||
                   (!isFloating && !isOnSlope && (Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable ||
                   Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Water)))
                {
                    waitTime = MaxWaitTime;
                }
                else
                {
                    // Move in the current direction.
                    Vector2 velocity = new Vector2((int)direction * MoveSpeed * elapsed, 0.0f);
                    position = position + velocity;

                    position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

                    HandleCollision();
                }
            }
        }

        private void HandleCollision()
        {
            int x = (int)Math.Floor(Position.X / Tile.Width);
            int y = (int)Math.Ceiling(Position.Y / Tile.Height);

            TileCollision collisionBottom = Level.GetCollision(x, y - 1);
            TileCollision collisionBelow = Level.GetCollision(x, y);

            isOnSlope = false;

            if (collisionBottom == TileCollision.SlopePlus || collisionBottom == TileCollision.SlopeMinus)
            {
                // Going Up
                position.Y += RtAngledTriangle.GetIntersectionDepthY(Position, Level.GetBounds(x, y - 1), (int)collisionBottom);
                isOnSlope = true;
            }
            else if (collisionBelow == TileCollision.SlopePlus || collisionBelow == TileCollision.SlopeMinus)
            {
                // Coming Down
                position.Y += RtAngledTriangle.GetIntersectionDepthY(Position, Level.GetBounds(x, y), (int)collisionBelow);
                isOnSlope = true;
            }
            else
            {
                // On Ground
                Rectangle tileBound = Level.GetBounds(x, y - 1);
                position.Y = position.Y < (tileBound.Top + tileBound.Bottom) / 2 ? tileBound.Top : tileBound.Bottom;
            }
        }

        /// <summary>
        /// Draws the animated animal.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Stop running when the game is paused or before turning around.
            if (isLazy || !Level.Player.IsAlive ||
                Level.ReachedExit ||
                waitTime > 0)
            {
                sprite.PlayAnimation(idleAnimation);
            }
            else
            {
                sprite.PlayAnimation(runAnimation);
            }

            // Draw facing the way the animal is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip);
        }
    }
}
