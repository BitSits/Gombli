using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class BeanSeed : PowerUp
    {
        private const float MaxActivationTime = 10.0f;
        private const float ActivationDelay = 0.3f; // Just the time need the plant to Grow up
        private float activationTime = MaxActivationTime;

        // bool onGround, onWall, onStableGround;

        /// <summary>
        /// there will be a shift in center of bounding circle when 
        /// it grows into a full size plant from seed
        /// </summary>
        private float bottomShift;

        public BeanSeed(Level level, Vector2 position, bool playPick)
            : base(level, position)
        { PowerIndex = 1; Damage = 1f; this.playPick = playPick; LoadContent(); }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (State == PowerState.Drop)
            {
                radius = 5;

                // Apply Physics
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                velocity.X = (int)direction * MoveSpeed;
                velocity.Y += MathHelper.Clamp(GravityAcceleration * elapsed, -MoveSpeed, MoveSpeed);

                position += velocity * elapsed;
                position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

                HandleCollision();

                if (IsOnGround)
                {
                    Activated();

                    // Shifted Up and bounding Circle is increased Depending on the current Animation
                    bottomShift = sprite.Animation.FrameHeight / 2 - radius;
                    radius = (int)(sprite.Animation.FrameWidth / 2 * 0.6);
                    position.Y -= bottomShift;
                }
                else if (IsOnWall) direction = (FaceDirection)(-(int)direction);
            }
            else if (State == PowerState.Active)
            {
                activationTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (activationTime < 0.0f) Died();
            }
        }
    }
}
