using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class Bubble : PowerUp
    {
        private const float MaxActivationTime = 5.0f;
        private const float MaxDropTime = 0.5f;
        private float activationTime = MaxActivationTime, dropTime = MaxDropTime;

        // Bounce control constants
        const float BounceHeight = 0.38f;
        const float BounceRate = 3.0f;
        const float BounceSync = -0.01f;

        private bool onTop; // 0 - Down and Back 1 - Top and Front

        public Bubble(Level level, Vector2 position, bool onTop, bool playPick)
            : base(level, position)
        {
            PowerIndex = 2; this.onTop = onTop;
            velocity = Vector2.Zero;

            this.playPick = playPick;
            LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (State == PowerState.Drop)
            {
                velocity.X = (int)direction * (MoveSpeed / 2.5f);

                if (!onTop) // Shifting it back and down
                { velocity.X = velocity.X * 1.0f; velocity.Y = Math.Abs(velocity.X); }
                else
                    velocity.X = velocity.X * 1.5f;

                position += velocity * elapsed;
                position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

                dropTime -= elapsed;

                if (dropTime <= 0.0f)
                {
                    Activated();
                    radius = (int)(sprite.Animation.FrameWidth / 2 * 0.68);
                }
            }
            if (State == PowerState.Active)
            {
                activationTime -= elapsed;
                if (activationTime < 0.0f || health <= 0.0f) Died();

                // Apply Physics
                velocity.X = (int)direction * (MoveSpeed / 2.5f); velocity.Y = 0;

                position += velocity * elapsed;

                // Bounce along a sine curve over time.
                // Include the X coordinate so that neighboring gems bounce in a nice wave pattern.            
                double t = gameTime.TotalGameTime.TotalSeconds * BounceRate + Position.X * BounceSync;
                float bounce = (float)Math.Sin(t) * BounceHeight;

                position.Y += bounce;

                HandleCollision();

                if (IsOnWall) Died(); // Bursts if it Touches the Wall
            }
        }
    }
}
