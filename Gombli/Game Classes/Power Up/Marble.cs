using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class Marble : PowerUp
    {
        public Marble(Level level, Vector2 position, bool playPick)
            : base(level, position)
        { PowerIndex = 0; Damage = 0.4f; health = 0.1f; this.playPick = playPick; LoadContent(); }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (State == PowerState.Drop) { radius = 5; State = PowerState.Active; }

            if (State == PowerState.Active)
            {
                // Apply Physics
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                velocity.X = (int)direction * MoveSpeed;
                velocity.Y += MathHelper.Clamp(GravityAcceleration * elapsed, -MoveSpeed, MoveSpeed);

                position += velocity * elapsed;
                position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

                HandleCollision();

                if (IsOnGround) Died();

                else if (IsOnWall) direction = (FaceDirection)(-(int)direction);
            }
        }
    }
}
