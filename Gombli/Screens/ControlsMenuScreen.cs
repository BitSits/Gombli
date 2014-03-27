using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class ControlsMenuScreen : GameScreen
    {

        ContentManager content;
        Texture2D backgroundTexture;

        public ControlsMenuScreen() 
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            backgroundTexture = content.Load<Texture2D>("Backgrounds/ControlBackground");
        }

        public override void UnloadContent()
        {
            content.Unload();
        }


        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;
            if (input.IsMenuCancel(ControllingPlayer, out playerIndex))
                ExitScreen();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Rectangle fullscreen = new Rectangle(0, 0, viewport.Width, viewport.Height);
            byte fade = TransitionAlpha;

            spriteBatch.Begin(SpriteBlendMode.None);

            spriteBatch.Draw(backgroundTexture, fullscreen,
                             new Color(fade, fade, fade));

            SpriteFont font = ScreenManager.TitleFont;
            
            spriteBatch.DrawString(font, "Press Esc. to return to Main Menu", new Vector2(100,500), Color.White);
            spriteBatch.End();
        }
    }
}
