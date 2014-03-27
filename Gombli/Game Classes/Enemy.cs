using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    /// <summary>
    /// Facing direction along the X or Y axis.
    /// </summary>
    enum FaceDirection { Left = -1, Right = 1, Up = -1, Down = 1, }

    enum Orientation { Horizontal = -1, Vertical = 1, }

    /// <summary>
    /// A monster who is impeding the progress of our fearless adventurer.
    /// </summary>
    class Enemy
    {
        public Level Level { get { return level; } }
        Level level;

        public float PowerUpDamage { get; private set; }

        public int PlayerDamage { get; private set; }

        public int PointValue { get; private set; }

        /// <summary>
        /// Position in world space of the bottom center of this enemy.
        /// </summary>
        public Vector2 Position { get { return position; } }
        Vector2 position;

        private int radius;
        /// <summary>
        /// Gets a circle which bounds this Enemy in world space.
        /// </summary>
        public Circle BoundingCircle { get { return new Circle(Position, radius); } }

        public bool IsAlive { get { return health > 0.0f; } }
        private float health;

        public bool IsPollutant { get; private set; }

        private Texture2D BubbleOverlay;
        private const float MaxHurtTime = 4.0f;

        public bool IsHurt { get { return hurtTime > 0.0f; } }
        private float hurtTime;

        // Animations
        private Animation runAnimation;
        private Animation idleAnimation;
        private Animation dieAnimation;
        private AnimationPlayer sprite;

        /// <summary>
        /// The direction this enemy is facing and moving along the X axis.
        /// </summary>
        private FaceDirection direction = FaceDirection.Right;
        private Orientation orientation;

        /// <summary>
        /// How long this enemy has been waiting before turning around.
        /// </summary>
        private float waitTime;

        /// <summary>
        /// How long to wait before turning around.
        /// </summary>
        private const float MaxWaitTime = 0.5f;

        /// <summary>
        /// The speed at which this enemy moves along the X axis.
        /// </summary>
        private const float MoveSpeed = 130.0f;

        /// <summary>
        /// Constructs a new Enemy.
        /// </summary>
        public Enemy(Level level, Vector2 position, string spriteSet, Orientation orientation,
            bool isPollutant, int contactDamage)
        {
            this.level = level;
            this.position = position;
            this.orientation = orientation;
            hurtTime = 0.0f;
            IsPollutant = isPollutant;

            PlayerDamage = contactDamage;

            PowerUpDamage = health = (float)contactDamage / 10;

            PointValue = contactDamage * 5;

            LoadContent(spriteSet);
        }

        /// <summary>
        /// Loads a particular enemy sprite sheet and sounds.
        /// </summary>
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Enemy/" + spriteSet + "/";
            runAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_Run"), 0.12f, true, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_Idle"), 0.1f, true, true);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_Die"), 0.1f, false, true);

            sprite.PlayAnimation(idleAnimation);

            if (IsPollutant)
                BubbleOverlay = Level.Content.Load<Texture2D>(spriteSet + Level.LevelIndex.ToString() + "_BubOverlay");

            radius = (int)(idleAnimation.FrameWidth / 2 * 0.75);
        }

        /// <summary>
        /// Paces back and forth along a platform, waiting at either end.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!IsAlive) return;

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (hurtTime > 0.0f) hurtTime = Math.Max(0.0f, hurtTime - elapsed);

            // Calculate tile position based on the side we are walking towards.
            float posX = Position.X + (orientation == Orientation.Horizontal ? BoundingCircle.Radius * (int)direction : 0);
            int tileX = (int)Math.Floor(posX / Tile.Width) - (orientation == Orientation.Horizontal ? (int)direction : 0);

            float posY = Position.Y + (orientation == Orientation.Vertical ? BoundingCircle.Radius * (int)direction : 0);
            int tileY = (int)Math.Floor(posY / Tile.Height);

            if (waitTime > 0)
            {
                // Wait for some amount of time.
                waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            else
            {
                // If we are about to run into a wall or off a cliff, start waiting.
                TileCollision collision = Level.GetCollision(tileX + (orientation == Orientation.Horizontal ? (int)direction : 0), tileY);

                if (collision == TileCollision.Impassable || collision == TileCollision.Reverse
                    || collision == TileCollision.Left || collision == TileCollision.Up
                    || collision == TileCollision.Right || collision == TileCollision.Down
                    || collision == TileCollision.SlopeMinus || collision == TileCollision.SlopePlus)
                {
                    waitTime = MaxWaitTime;
                    Orientation previousType = orientation;

                    if (collision == TileCollision.Up || collision == TileCollision.Down)
                    {
                        orientation = Orientation.Vertical;
                        direction = collision == TileCollision.Up ? FaceDirection.Down : FaceDirection.Up;
                    }
                    if (collision == TileCollision.Left || collision == TileCollision.Right)
                    {
                        orientation = Orientation.Horizontal;
                        direction = collision == TileCollision.Left ? FaceDirection.Right : FaceDirection.Left;
                    }

                    // Then turn around.
                    direction = (FaceDirection)(-(int)direction);

                    if (previousType != orientation) waitTime = 0.0f;
                }
                else
                {
                    // Move in the current direction.
                    Vector2 velocity = new Vector2(orientation == Orientation.Horizontal ? (int)direction : 0.0f,
                        orientation == Orientation.Vertical ? (int)direction : 0.0f);
                    position = position + velocity * MoveSpeed * elapsed;

                    position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));
                }
            }
        }

        public void OnHurt(float damage)
        {
            if (damage == -1)
                if (IsPollutant)
                { hurtTime = MaxHurtTime; return; }
                else
                { direction = (FaceDirection)(-(int)direction); return; }

            health -= damage;
            if (health <= 0.0f) sprite.PlayAnimation(dieAnimation);
        }

        /// <summary>
        /// Draws the animated enemy.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!IsAlive)
                sprite.PlayAnimation(dieAnimation);

            // Stop running when the game is paused or before turning around.
            else if (!Level.Player.IsAlive ||
                Level.ReachedExit ||
                waitTime > 0)
            {
                sprite.PlayAnimation(idleAnimation);
            }
            else
            {
                sprite.PlayAnimation(runAnimation);
            }


            // Draw facing the way the enemy is moving.
            SpriteEffects flip = direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip);

            if (IsPollutant && hurtTime > 0.0f)
                spriteBatch.Draw(BubbleOverlay, Position, null, Color.White, 0, new Vector2(BubbleOverlay.Width) / 2, 1, flip, 1);
        }
    }
}
