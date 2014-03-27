using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    class CreditsMenuScreen : GameScreen
    {
        Vector2 Position=new Vector2 (200);
        public CreditsMenuScreen() { }

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
            SpriteFont font = ScreenManager.TitleFont;

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "GOMBLI\n  by Team BitSits\n   --Shubhajit Saha\n   --Maya Agarwal", 
                Position, Color.White);
           // spriteBatch.DrawString(font, "Press Esc. to return to Main Menu", new Vector2(500, 200), Color.White);
            spriteBatch.End();
        }
    }
}
