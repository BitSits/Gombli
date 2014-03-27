using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Gombli
{
    /// <summary>
    /// A popup screen, used to display the level intro
    /// </summary>
    class IntroScreen : GameScreen
    {
        private int levelIndex;

        private Texture2D storyText;

        public IntroScreen(int levelIndex)
        {
            this.levelIndex = levelIndex;

            IsPopup = true;

            TransitionOnTime = TimeSpan.FromSeconds(2);
            TransitionOffTime = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Loads graphics content for this screen. This uses the shared ContentManager
        /// provided by the Game class, so the content will remain loaded forever.
        /// Whenever a subsequent MessageBoxScreen tries to load this same content,
        /// it will just get back another reference to the already loaded data.
        /// </summary>
        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;

            storyText = content.Load<Texture2D>("Levels/" + levelIndex.ToString() + "_Story");
        }

        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;

            if (input.IsMenuSelect(ControllingPlayer, out playerIndex))
            {
                ExitScreen();
            }
        }

        /// <summary>
        /// Draws the Story intro.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            // Darken down any other screens that were drawn beneath the popup.
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            // Center the message text in the viewport.
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);
            Vector2 storyTextureSize = new Vector2(storyText.Width, storyText.Height);
            // Fade the popup alpha during transitions.
            Color color = new Color(255, 255, 255, TransitionAlpha);

            spriteBatch.Begin();

            // Draw the story Texture.
            spriteBatch.Draw(storyText, (viewportSize - storyTextureSize) / 2, color);

            spriteBatch.End();
        }
    }
}
