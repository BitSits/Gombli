using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Gombli
{
    enum PowerState
    {
        Ground, Pick, Drop, Active, Die,
    }

    class PowerUp
    {
        public int PowerIndex { get; protected set; }
        
        // Animatoins
        private Animation groundAnimation;
        private Animation pickAnimation;
        private Animation dropAnimation;
        private Animation activeAnimation;
        private Animation dieAnimation;
        protected AnimationPlayer sprite;

        // Sound effects
        private SoundEffect hit;
        private SoundEffect pick;

        protected bool playPick;

        public int PointValue { get; private set; }

        public PowerState State { get; protected set; }

        protected float health = 1.0f;
        public float Damage { get; internal set; }

        public Level Level { get; private set; }

        protected FaceDirection direction = FaceDirection.Left;

        protected const float MoveSpeed = 200.0f;
        protected const float GravityAcceleration = 200.0f;

        public Vector2 Position { get { return position; } }
        protected Vector2 position, previousPosition, velocity = new Vector2(0, -75);

        public bool IsOnGround { get; private set; }
        public bool IsOnWall { get; private set; }

        protected bool skipGroundCheck = false;

        protected int radius;
        public Circle BoundingCircle { get { return new Circle(Position, radius); } }

        public PowerUp(Level level, Vector2 position)
        {
            this.Level = level;
            this.State = PowerState.Ground;
            this.position = position;
        }

        protected void LoadContent()
        {
            PointValue = 10 * (PowerIndex + 1) * (playPick ? 1 : 0);

            string powerUp = this.ToString().Remove(0, "Gombli.".Length);

            string path = "PowerUp/" + powerUp + "/";
            groundAnimation = new Animation(Level.Content.Load<Texture2D>(path + "Ground"), 0.2f, true, true);
            dropAnimation = new Animation(Level.Content.Load<Texture2D>(path + "Drop"), 0.1f, false, true);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>(path + "Die"), 0.1f, false, true);

            pickAnimation = new Animation(Level.Content.Load<Texture2D>("PowerUp/Pick"), 0.1f, false, true);

            activeAnimation = new Animation(Level.Content.Load<Texture2D>(path + "Active"), 0.1f,
                PowerIndex == 3 ? true : false, true);


            hit = Level.Content.Load<SoundEffect>(path + "Hit");
            pick = Level.Content.Load<SoundEffect>("PowerUp/power pick");

            radius = (int)(groundAnimation.FrameWidth / 2 * 0.67f);
            sprite.PlayAnimation(groundAnimation);

            if (Position == Level.InvalidPositionVector) Picked();
        }

        public virtual void Update(GameTime gameTime)
        {
            previousPosition = position;
        }

        public void Picked()
        {
            State = PowerState.Pick; sprite.PlayAnimation(pickAnimation);

            if (playPick) pick.Play(0.6f);
        }

        public void Droped()
        {
            State = PowerState.Drop; sprite.PlayAnimation(dropAnimation);

            // shif the position near to throwing position
            direction = Level.Player.Direction;
            position = Level.Player.Position - new Vector2(-(int)direction * Level.Player.BoundingRectangle.Width / 2 * 0.8f,
                                                            Level.Player.BoundingRectangle.Height * .61f);
        }

        protected void Activated() { State = PowerState.Active; sprite.PlayAnimation(activeAnimation); }

        protected void Died() { State = PowerState.Die; sprite.PlayAnimation(dieAnimation); }
 
        public void OnHurt(float damage)
        {
            hit.Play();

            health -= damage;
            if (health <= 0) Died();
        }


        protected void HandleCollision()
        {
            IsOnGround = IsOnWall = false;
            Rectangle bounds = new Rectangle(
                (int)position.X - radius, (int)position.Y - radius, 2 * radius, 2 * radius);

            #region For each potentially colliding tile

            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);

                    if (collision == TileCollision.SlopeMinus || collision == TileCollision.SlopePlus)
                    {
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        float depth = RtAngledTriangle.GetIntersectionDepthY(position, tileBounds, (int)collision);

                        if (depth < 0.0f && RtAngledTriangle.IsAbove(previousPosition, tileBounds, (int)collision))
                        {
                            IsOnGround = true;

                            position = new Vector2(Position.X, Position.Y + depth);

                            bounds = new Rectangle((int)position.X - radius, (int)position.Y - radius, 2 * radius, 2 * radius);
                        }
                    }

                    else if (collision == TileCollision.Platform || collision == TileCollision.Impassable)
                    {
                        Rectangle tileBounds = Level.GetBounds(x, y);

                        bounds = CollisionWithRectBlock(bounds, collision, tileBounds);
                    }
                        
                }
            }

            #endregion

            CollisionWithOther(bounds);
        }

        private Rectangle CollisionWithRectBlock(Rectangle bounds, TileCollision collision, Rectangle tileBounds)
        {
            bool wasOnGround = IsOnGround; IsOnGround = false;

            Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
            if (depth != Vector2.Zero)
            {
                float absDepthX = Math.Abs(depth.X);
                float absDepthY = Math.Abs(depth.Y);

                // Resolve the collision along the shallow axis.
                if (absDepthY < absDepthX || collision == TileCollision.Platform)
                {
                    // If we crossed the top of a tile, we are on the ground.
                    if ((previousPosition.Y + radius) <= tileBounds.Top)
                        IsOnGround = true;

                    // Ignore platforms, unless we are on the ground.
                    if (collision == TileCollision.Impassable || IsOnGround)
                    {
                        // Resolve the collision along the Y axis.
                        position = new Vector2(Position.X, Position.Y + depth.Y);

                        bounds = new Rectangle((int)position.X - radius, (int)position.Y - radius, 2 * radius, 2 * radius);
                    }
                }
                else if (collision == TileCollision.Impassable) // Ignore platforms.
                {
                    // Resolve the collision along the X axis.
                    position = new Vector2(Position.X + depth.X, Position.Y);

                    bounds = new Rectangle((int)position.X - radius, (int)position.Y - radius, 2 * radius, 2 * radius);

                    IsOnWall = true;
                }
            }

            IsOnGround = IsOnGround || wasOnGround;
            return bounds;
        }

        private Rectangle CollisionWithOther(Rectangle bounds)
        {
            foreach (OzoneTile ozoneTile in Level.OzoneTiles)
            {
                if (bounds.Intersects(ozoneTile.BoundingRectangle))
                    bounds = CollisionWithRectBlock(bounds, TileCollision.Platform, ozoneTile.BoundingRectangle);
            }

            foreach (BreakableTile breakableTile in Level.BreakableTiles)
            {
                if (bounds.Intersects(breakableTile.BoundingRectangle) && breakableTile.State != PowerState.Die
                    && !(this is Marble))
                    bounds = CollisionWithRectBlock(bounds, TileCollision.Impassable, breakableTile.BoundingRectangle);
            }

            foreach (MovingTile movingTile in Level.MovingTiles)
            {
                if (bounds.Intersects(movingTile.BoundingRectangle))
                    bounds = CollisionWithRectBlock(bounds, TileCollision.Platform, movingTile.BoundingRectangle);
            }

            return bounds;
        }


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None);
        }
    }
}
