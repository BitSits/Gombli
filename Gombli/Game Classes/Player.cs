using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Gombli
{
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player
    {
        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private Animation throwAnimation;
        private Animation climbAnimation;
        private Animation swimAnimation;
        private AnimationPlayer sprite;

        public FaceDirection Direction { get; private set; }

        // Sounds
        private SoundEffect jumpSound;
        //private SoundEffect fallSound;
        //private SoundEffect killedSound;

        public Level Level { get { return level; } }
        Level level;

        // Physics state
        public Vector2 Position { get { return position; } private set { position = value; } }
        Vector2 position;

        private float previousBottom;
        private Vector2 previousPosition;

        private KeyboardState prevKeyboardState;

        public Vector2 Velocity { get { return velocity; } set { velocity = value; } }
        Vector2 velocity;

        public int MaxHealth { get { return 20; } }
        public bool IsAlive { get { return CurrentHealth > 0.0f; } }
        public int CurrentHealth { get; private set; }

        public bool CanGetHurt { get { return hurtTime == 0.0f; } }
        private const float MaxHurtTime = 2.0f;
        private float hurtTime;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 14000.0f;
        private const float MaxMoveSpeed = 2000.0f;
        private const float GroundDragFactor = 0.58f;
        private const float AirDragFactor = 0.65f;
        private const float WaterDragFactor = 0.52f;

        // Constants for controlling vertical movement
        private const float MaxJumpTimeInOzone = 0.14f;
        private const float MaxJumpTimeInPlat = 0.42f;
        private const float JumpLaunchVelocity = -4000.0f;
        private const float GravityAcceleration = 3500.0f;
        private const float MaxFallSpeed = 600.0f;
        private const float JumpControlPower = 0.14f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const Buttons JumpButton = Buttons.A;

        private float MaxJumpTime = MaxJumpTimeInPlat;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround { get { return isOnGround; } }
        bool isOnGround;

        /// <summary>
        /// Gets whether or not the player's feet are on the Ladder.
        /// </summary>
        public bool IsOnLadder { get; private set; }
        bool isOnTopOfLadder;

        public bool IsUnderWater { get; private set; }

        public bool IsOnWaterTop { get; private set; }

        bool isOnSlope;

        public bool IsOnMovingTile { get; private set; }

        public bool IsOnOzoneTile { get; private set; }

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private Vector2 movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        // Throw state
        private bool wasThrowing, isThrowing;
        private const float MaxThrowTime = 0.60f;
        private float throwTime = 0.0f;
        public bool Throw { get { return throwTime == MaxThrowTime; } }

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
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

        /// <summary>
        /// Constructors a new 
        /// </summary>
        public Player(Level level, Vector2 position)
        {
            this.level = level;
            CurrentHealth = MaxHealth;
            Direction = FaceDirection.Left;

            LoadContent();

            Reset(position);
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        private void LoadContent()
        {
            // Load animated textures.
            idleAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Idle"), 0.1f, false);
            runAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Run"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Jump"), 0.1f, false);
            celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Celebrate"), 0.18f, true);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Die"), 0.1f, false);
            throwAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Throw"), 0.08f, false);
            climbAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Climb"), 0.2f, true);
            swimAnimation = new Animation(Level.Content.Load<Texture2D>("Gombli/Swim"), 0.2f, true);

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.28);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.82);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            //killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = Level.Content.Load<SoundEffect>("Gombli/PlayerJump");
            //fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerFall");
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            CurrentHealth = MaxHealth;
            hurtTime = 0.0f;

            sprite.PlayAnimation(idleAnimation);
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            GetInput();

            ApplyPhysics(gameTime);

            DoThrow(gameTime);

            if (hurtTime > 0.0f)
                hurtTime = Math.Max(0.0f, hurtTime - (float)gameTime.ElapsedGameTime.TotalSeconds);

            // Clear input.
            movement = Vector2.Zero;
            isJumping = isThrowing = false;
        }

        private void DoThrow(GameTime gameTime)
        {
            if (throwTime > 0.0f)
                throwTime = Math.Max(throwTime - (float)gameTime.ElapsedGameTime.TotalSeconds, 0.0f);

            else if (isThrowing && !wasThrowing && throwTime == 0.0f && Level.SelectedPowerUpPicked != 0)
            {
                throwTime = MaxThrowTime;
                sprite.PlayAnimation(throwAnimation);
            }

            wasThrowing = isThrowing;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput()
        {
            // Get input state.
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            KeyboardState keyboardState = Keyboard.GetState();

            isThrowing = keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Enter);
            if (throwTime > 0.0f) return;

            // Get analog horizontal movement.
            movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement.X) < 0.5f)
                movement.X = 0.0f;

            // If any digital horizontal movement input is found, override the analog movement.
            if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A))
            {
                movement.X = -1.0f;
            }
            else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D))
            {
                movement.X = 1.0f;
            }

            if (keyboardState.IsKeyDown(Keys.Up) ||
                keyboardState.IsKeyDown(Keys.W))
            {
                movement.Y = -1.0f;
            }
            else if (keyboardState.IsKeyDown(Keys.Down) ||
                keyboardState.IsKeyDown(Keys.S))
            {
                movement.Y = 1.0f;
            }

            // Check if the player wants to jump.
            isJumping =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.W) ||
                keyboardState.IsKeyDown(Keys.Up);


            if (!prevKeyboardState.IsKeyDown(Keys.Tab))
                if (keyboardState.IsKeyDown(Keys.Tab) &&
                    keyboardState.IsKeyUp(Keys.RightShift) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    level.selectedPowerUpIndex++; level.selectedPowerUpIndex %= 4;
                }
                else if (keyboardState.IsKeyDown(Keys.Tab) &&
                    (keyboardState.IsKeyDown(Keys.RightShift) || keyboardState.IsKeyDown(Keys.LeftShift)))
                {
                    level.selectedPowerUpIndex--; if (level.selectedPowerUpIndex < 0) level.selectedPowerUpIndex += 4;
                }
            
            prevKeyboardState = keyboardState;
        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Save the new bounds bottom.
            previousBottom = BoundingRectangle.Bottom;
            previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement.X * MoveAcceleration * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            velocity.Y = DoJump(velocity.Y, gameTime);

            // fully overrides the Y velocity when in ladder
            if (IsOnLadder) velocity.Y = movement.Y * MoveAcceleration * elapsed;

            // is on top of ladder and wanna go DOWN
            if (isOnTopOfLadder && movement.Y > 0.0) velocity.Y = movement.Y * MoveAcceleration * elapsed;

            // overrides vel.Y if wanna go UP and not jumping
            if (IsUnderWater && movement.Y < 0 && jumpTime == 0) velocity.Y = movement.Y * MoveAcceleration * elapsed * 0.98f;

            // Apply pseudo-drag horizontally.
            if (isOnSlope) velocity.X *= GroundDragFactor * 0.7f;
            else if (IsOnGround || IsOnLadder) velocity.X *= GroundDragFactor;
            else velocity.X *= AirDragFactor;

            if (IsUnderWater) velocity *= WaterDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;

            if (Velocity.X > 0) Direction = FaceDirection.Right;
            else if (Velocity.X < 0) Direction = FaceDirection.Left;
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if (!wasJumping && (isOnGround || isOnSlope || isOnTopOfLadder) || jumpTime > 0.0f)
                {
                    if (jumpTime == 0.0f) jumpSound.Play(0.25f);

                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    sprite.PlayAnimation(jumpAnimation);
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;

            return velocityY;
        }



        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on Direction of movement.
        /// </summary>
        private void HandleCollisions()
        {
            // Get the player's bounding rectangle.
            Rectangle bounds = BoundingRectangle;
            Rectangle tempBounds = bounds;

            bool isAboveSlope = false; isOnSlope = false;

            #region Slope Tile checking,

            // find neighboring tiles
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    TileCollision collision = Level.GetCollision(x, y);

                    if (collision == TileCollision.SlopePlus || collision == TileCollision.SlopeMinus)
                    {
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        bounds = CollisionWithSlope(bounds, collision, tileBounds);

                        isAboveSlope = isAboveSlope || isOnSlope ||
                            RtAngledTriangle.IsAbove(position, tileBounds, (int)collision);
                    }
                }
            }

            #endregion

            // Reset flag to search for ground collision.
            IsOnWaterTop = IsUnderWater = isOnGround = IsOnLadder = isOnTopOfLadder = false;

            #region Ladder Checking,

            // Ladder top collision detection when falling under gravity
            if (TileCollisionBottom(0) == TileCollision.Ladder && previousBottom <= TileBoundsBottom().Top
                && movement.Y <= 0)
            {
                // Resolve the collision along the Y axis.
                Position = new Vector2(Position.X, (int)(Position.Y / Tile.Height) * Tile.Height);
                bounds = BoundingRectangle;
                isOnTopOfLadder = true;
            }

            if (TileCollisionBottom(0) == TileCollision.Ladder && jumpTime == 0)
            {
                IsOnLadder = true;

                // Sticking it to the Center of the Ladder
                if (previousBottom != bounds.Bottom && movement.Y != 0)
                {
                    position.X = (int)(Position.X / Tile.Width) * Tile.Width + Tile.Width / 2;
                    bounds = BoundingRectangle;
                }
            }

            #endregion

            #region and For each potentially colliding tile

            //leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            //rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            //topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            //bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // Skiping tile Checking when on Slope
                    if (y == bottomTile && leftTile != rightTile && isAboveSlope)
                    {
                        TileCollision collisionSlope = TileCollisionBottom(0);
                        if (x == leftTile && collisionSlope == TileCollision.SlopePlus)
                            continue;
                        if (x == rightTile && collisionSlope == TileCollision.SlopeMinus)
                            continue;
                    }

                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);

                    if (collision != TileCollision.Passable && collision != TileCollision.Ladder)
                    {
                        // Determine collision depth (with Direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        if (collision == TileCollision.Platform || collision == TileCollision.Impassable)
                            bounds = CollisionWithRectBlock(bounds, collision, tileBounds);
                    }
                }
            }

            #endregion

            bounds = CollisionWithMovingTiles(bounds);

            bounds = CollisionWithBreakableTile(bounds);

            if (tempBounds.Bottom > bounds.Bottom) MaxJumpTime = MaxJumpTimeInPlat;

            #region Water and water surface

            if (TileCollisionBottom(0) == TileCollision.Water)
            {
                TileCollision tileCollisionAboveBottom = TileCollisionBottom(1);
                Rectangle boundsBottom = TileBoundsBottom();

                if (tileCollisionAboveBottom == TileCollision.Water) IsUnderWater = true;
                if (tileCollisionAboveBottom == TileCollision.Passable && jumpTime == 0.0f &&
                    previousBottom >= boundsBottom.Bottom)
                {
                    position.Y = boundsBottom.Bottom;
                    bounds = BoundingRectangle;
                    IsUnderWater = true;
                }
            }

            #endregion

            tempBounds = bounds;
            bounds = CollisionWithOzoneTiles(bounds);
            if (tempBounds.Bottom > bounds.Bottom) { IsOnOzoneTile = true; MaxJumpTime = MaxJumpTimeInOzone; }

            if (TileCollisionBottom(0) == TileCollision.Ladder && IsOnGround) IsOnLadder = true;
        }

        private Rectangle CollisionWithRectBlock(Rectangle bounds, TileCollision collision, Rectangle tileBounds)
        {
            bool wasOnGround = isOnGround; isOnGround = false;

            Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
            if (depth != Vector2.Zero)
            {
                float absDepthX = Math.Abs(depth.X);
                float absDepthY = Math.Abs(depth.Y);

                // Resolve the collision along the shallow axis.
                if (absDepthY < absDepthX || collision == TileCollision.Platform)
                {
                    // If we crossed the top of a tile, we are on the ground.
                    if (previousBottom <= tileBounds.Top)
                        isOnGround = true;

                    // Ignore platforms, unless we are on the ground.
                    if (collision == TileCollision.Impassable || IsOnGround)
                    {
                        // Resolve the collision along the Y axis.
                        Position = new Vector2(Position.X, Position.Y + depth.Y);

                        // Perform further collisions with the new bounds.
                        bounds = BoundingRectangle;
                    }
                }
                else if (collision == TileCollision.Impassable) // Ignore platforms.
                {
                    // Resolve the collision along the X axis.
                    Position = new Vector2(Position.X + depth.X, Position.Y);

                    // Perform further collisions with the new bounds.
                    bounds = BoundingRectangle;
                }
            }

            isOnGround = isOnGround || wasOnGround;
            return bounds;
        }

        private Rectangle CollisionWithSlope(Rectangle bounds, TileCollision collision, Rectangle tileBounds)
        {
            float depth = RtAngledTriangle.GetIntersectionDepthY(position, tileBounds, (int)collision);
            if (depth < 0 && (((isOnGround || isOnTopOfLadder) && previousBottom == tileBounds.Top) ||
                RtAngledTriangle.IsAbove(previousPosition, tileBounds, (int)collision)))
            {
                isOnSlope = true;
                position.Y += depth;
                bounds = BoundingRectangle;
            }
            return bounds;
        }

        /// <summary>
        /// get the tile collision
        /// </summary>
        /// <param name="at">0 = Bottom  1 = above bottom</param>
        /// <returns></returns>
        private TileCollision TileCollisionBottom(int at)
        {
            int x = (int)(position.X / Tile.Width);
            int y = (int)Math.Ceiling((position.Y / Tile.Height)) - 1 - at;
            return Level.GetCollision(x, y);
        }

        private Rectangle TileBoundsBottom()
        {
                int x = (int)(position.X / Tile.Width);
                int y = (int)Math.Ceiling((position.Y / Tile.Height)) - 1;
                return Level.GetBounds(x, y);
        }

        private Rectangle CollisionWithMovingTiles(Rectangle bounds)
        {
            bool isOn = false;

            Point center = new Point((int)Position.X, (int)Position.Y);
            Point left = new Point((int)(Position.X - bounds.Width / 2), (int)Position.Y);
            Point right = new Point((int)(Position.X + bounds.Width / 2), (int)Position.Y);

            bool wasNotOnGround = !IsOnGround;

            foreach (MovingTile movingTile in Level.MovingTiles)
            {
                if (movingTile.BoundingRectangle.Contains(left) || movingTile.BoundingRectangle.Contains(right))
                {
                    if (wasNotOnGround)
                        bounds = CollisionWithRectBlock(BoundingRectangle, TileCollision.Platform, movingTile.BoundingRectangle);

                    if (movingTile.BoundingRectangle.Top == bounds.Bottom)
                    {
                        isOn = true;
                        if (!wasNotOnGround && movingTile.Velocity == Vector2.Zero)
                            IsOnMovingTile = false;
                    }
                }
                else continue;
                

                if (// Completly on a moving tile
                    (wasNotOnGround && IsOnGround) ||

                    // Continue moving on if its on a moving tile
                    (IsOnMovingTile && bounds.Bottom == movingTile.BoundingRectangle.Top &&
                    (movingTile.BoundingRectangle.Contains(left) || movingTile.BoundingRectangle.Contains(right))) ||

                    // Partially on ground and on moving tile so the motion begins
                    (!wasNotOnGround && movingTile.BoundingRectangle.Contains(center) &&
                    bounds.Bottom == movingTile.BoundingRectangle.Top &&
                    movingTile.BoundingRectangle.Contains(movingTile.Velocity.X > 0 ? right : left)))
                {
                    IsOnMovingTile = true;
                }
            }

            if (!isOn) IsOnMovingTile = false;

            return bounds;
        }

        private Rectangle CollisionWithOzoneTiles(Rectangle bounds)
        {
            IsOnOzoneTile = false;

            foreach (OzoneTile ozoneTile in Level.OzoneTiles)
            {
                if (bounds.Intersects(ozoneTile.BoundingRectangle))
                    bounds = CollisionWithRectBlock(bounds, TileCollision.Platform, ozoneTile.BoundingRectangle);
            }

            return bounds;
        }

        private Rectangle CollisionWithBreakableTile(Rectangle bounds)
        {
            foreach (BreakableTile breakableTile in Level.BreakableTiles)
            {
                if (bounds.Intersects(breakableTile.BoundingRectangle) && breakableTile.State != PowerState.Die)
                    bounds = CollisionWithRectBlock(bounds, TileCollision.Impassable, breakableTile.BoundingRectangle);
            }

            return bounds;
        }


        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// hurt value, -1 = fall thru the level dies instantly 
        /// </param>
        public void OnHurt(int hurtBy)
        {
            hurtTime = MaxHurtTime;

            if (hurtBy == -1)   // fall thru the Level
            {
                // fallSound.Play();
                CurrentHealth = 0;
            }
            else
            {
                //killedSound.Play();
                CurrentHealth = Math.Max(0, CurrentHealth - hurtBy);
            }
                
            if (!IsAlive) sprite.PlayAnimation(dieAnimation);
        }

        public void OnMovingTile(MovingTile movingTile)
        {
            position += movingTile.Velocity;
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit()
        {
            sprite.PlayAnimation(celebrateAnimation);
        }

        /// <summary>
        /// Draws the animated 
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (IsAlive && !Level.ReachedExit && throwTime == 0)
            {
                if (isOnGround || isOnSlope || isOnTopOfLadder)
                {
                    if (Math.Abs(Velocity.X) - 0.02f > 0)
                        sprite.PlayAnimation(runAnimation);
                    else
                        sprite.PlayAnimation(idleAnimation);
                }
                else if (IsOnLadder)
                {
                    if (Math.Abs(velocity.Y) - 0.02f > 0)
                        sprite.PlayAnimation(climbAnimation);
                    else if (Math.Abs(Velocity.X) - 0.02f > 0)
                        sprite.PlayAnimation(runAnimation);
                    else
                        sprite.PlayAnimation(idleAnimation);
                }
                else if ((IsUnderWater || IsOnWaterTop) && jumpTime == 0.0f) { sprite.PlayAnimation(swimAnimation); }

                else sprite.PlayAnimation(jumpAnimation);
            }

            SpriteEffects flip = Direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Blink
            Color colour = Color.White;
            if (hurtTime > 0.0 && (int)(hurtTime * 10) % 2 == 0) colour = new Color(Color.White, 0);
            if (Level.ReachedExit || !IsAlive) colour = Color.White;

            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip, colour);
        }
    }
}
