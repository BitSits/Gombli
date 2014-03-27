using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Gombli
{
    class Info
    {
        // Animations
        private Animation ground;
        private Animation pick;
        private AnimationPlayer sprite;

        public string Message { get; private set; }

        private Level level;

        private Vector2 position;
        private float radius;
        public Circle BoundingCircle { get { return new Circle(position, radius); } }

        public PowerState State { get; private set; }

        private const float MaxWaitTime = 5.0f;
        private float time;

        public Info(Level level, string message, Vector2 position)
        {
            this.level = level;
            this.Message = message;
            this.position = position;
            this.radius = 20;

            State = PowerState.Ground;

            LoadContent();
        }

        private void LoadContent()
        {
            ground = new Animation(level.Content.Load<Texture2D>("InfoBox/Ground"), 0.1f, true, true);
            pick = new Animation(level.Content.Load<Texture2D>("InfoBox/Pick"), 0.1f, true, true);

            sprite.PlayAnimation(ground);
        }

        public void Picked() { State = PowerState.Pick; sprite.PlayAnimation(pick); time = MaxWaitTime; }

        public void Draw(GameTime gameTime, SpriteBatch sptiteBatch)
        {
            if (time > 0.0f) time = Math.Max(0.0f, time - (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (time == 0.0f) { State = PowerState.Ground; sprite.PlayAnimation(ground); }

            sprite.Draw(gameTime, sptiteBatch, position, SpriteEffects.None);
        }
    }

    class InformationBox
    {
        private Texture2D box;
        private AnimationPlayer sprite;

        private SpriteFont font;
        private Vector2 origin;

        List<Info> infos;

        public string Message { get; private set; }

        private const float MaxDisplayTime = 2.0f;
        private float time;

        public InformationBox(Texture2D boxTexture, SpriteFont font)
        {
            this.box = boxTexture;
            this.font = font;

            origin = new Vector2(box.Width / 2, box.Height / 2);
        }
        
        public void DisplayText(string message)
        {
            if (Message == message) return;

            this.Message = message;
            this.time = MaxDisplayTime;
            sprite.PlayAnimation(null);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Process passing time.
            if (time > 0.0f) time = Math.Max(0.0f, time - (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (time == 0.0f) { Message = null; return; }

            /*
            spriteBatch.Draw(box,
                new Vector2(spriteBatch.GraphicsDevice.Viewport.TitleSafeArea.Width / 2, origin.Y + 15) - origin,
                Color.White);
            */

            // TODO : Draw the string letter by letter
            spriteBatch.DrawString(font, Message, new Vector2(100, 150), Color.White);
        }
    }
}
