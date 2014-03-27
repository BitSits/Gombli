using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Gombli
{
    class RecycleBall : PowerUp
    {
        private const float ActiveMoveSpeed = 7.0f;
        private const int MaxDistance = 2000;

        Rectangle attackRect;

        public RecycleBall(Level level, Vector2 position, bool playPick)
            : base(level, position)
        { PowerIndex = 3; Damage = 1.5f; health = 20000f; this.playPick = playPick; LoadContent(); }

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

                if (IsOnGround || !BoundingCircle.Intersects(Level.VisibleRectangle)) 
                { Activated(); attackRect = Level.VisibleRectangle; }

                else if (IsOnWall) direction = (FaceDirection)(-(int)direction);
            }
            else if (State == PowerState.Active)
            {
                Attack();
            }
        }

        private void Attack()
        {
            Vector2 nearestEnemyPos = Vector2.Zero;
            float minDist = MaxDistance;

            foreach (Enemy enemy in Level.Enemies)
            {
                // search for enemy
                if (enemy.BoundingCircle.Intersects(attackRect) && enemy.IsAlive)
                {
                    float dist = Vector2.Distance(enemy.Position, Position);
                    if (dist < minDist)
                    {
                        nearestEnemyPos = enemy.Position;
                        minDist = dist;
                    }
                }
            }

            // There is no enemy on the screen
            if (minDist == MaxDistance) { Died(); return; }

            // Move in the direction of the nearest enemy
            position = BoundingCircle.LineCircle(nearestEnemyPos, ActiveMoveSpeed);
        }
    }
}