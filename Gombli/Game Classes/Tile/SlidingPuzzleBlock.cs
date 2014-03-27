using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class SlidingPuzzleBlock
    {
        public static Vector2 puzzlePosition;

        public static bool IsActive { get;private set; }

        private Texture2D texture;
        private Vector2 position, destPositon, maxSlidePosition, oriPosition, prevPosition, direction;
        private int blockNumber;

        public PowerState State { get; private set; }

        public bool IsSolved { get { return position == destPositon; } }

        public Rectangle BoundingRectangle
        {
            get
            { return new Rectangle((int)position.X, (int)position.Y, Tile.Width, Tile.Height); }
        }

        public SlidingPuzzleBlock(Level level,Vector2 position, char number)
        {
            this.position = oriPosition = position;
            this.blockNumber = number - '0';

            this.destPositon = Level.InvalidPositionVector;
            State = PowerState.Ground;

            //if (this.blockNumber == 0) { puzzlePosition = position; State = PowerState.Pick; return; }

            texture = level.Content.Load<Texture2D>("SlidingPuzzle/" + number);
        }

        public void Activate()
        {
            IsActive = true;

            State = PowerState.Active;

            if (destPositon == Level.InvalidPositionVector)
            {
                Point blockIndex = new Point((blockNumber - 1) % 3, (blockNumber - 1) / 3);
                destPositon = puzzlePosition + new Vector2(blockIndex.X * Tile.Width, blockIndex.Y * Tile.Height);

                direction = new Vector2(destPositon.X != position.X ? destPositon.X < position.X ? -1 : 1 : 0,
                    destPositon.Y != position.Y ? destPositon.Y < position.Y ? -1 : 1 : 0);

                if (direction.X != 0) { blockIndex = new Point(direction.X < 0 ? 0 : 2, blockIndex.Y); }
                else { blockIndex = new Point(blockIndex.X, direction.Y < 0 ? 0 : 2); }

                maxSlidePosition = puzzlePosition + new Vector2(blockIndex.X * Tile.Width, blockIndex.Y * Tile.Height);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (State == PowerState.Active)
            {
                Move(direction, maxSlidePosition);

                if (position == maxSlidePosition) State = PowerState.Die;
            }
            else if (State == PowerState.Pick) 
            {
                Vector2 oppDirection = direction * -1;

                Move(oppDirection, oriPosition);

                if (position == oriPosition) State = PowerState.Ground;
            }
        }

        private void Move(Vector2 direction, Vector2 destinationVector)
        {
            IsActive = true;

            prevPosition = position;

            position += direction * 3;

            float mid, small, big;
            if (direction.X != 0)
            {
                mid = destinationVector.X; small = position.X; big = prevPosition.X;
                if (direction.X > 0) { small = prevPosition.X; big = position.X; }
            }
            else
            {
                mid = destinationVector.Y; small = position.Y; big = prevPosition.Y;
                if (direction.Y > 0) { small = prevPosition.Y; big = position.Y; }
            }

            if (small <= mid && mid <= big) { position = destinationVector; IsActive = false; } 
        }

        public void Collision(Rectangle bounds)
        {
            State = PowerState.Die; IsActive = false;

            if (direction.X != 0) { position.X = bounds.Left - direction.X * Tile.Width; }
            else { position.Y = bounds.Top - direction.Y * Tile.Height; }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            //if (texture != null)
                spriteBatch.Draw(texture, position, Color.White);
        }
    }
}
