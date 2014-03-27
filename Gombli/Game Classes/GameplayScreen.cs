using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Media; 

namespace Gombli
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont smallFont, bigFont;

        private Texture2D winOverlay;
        private Texture2D gameOverOverlay;

        private Rectangle titleSafeArea;
        private Vector2 hudLocation, center;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);
        private const float MaxStartTime = 0.3f;
        private float startTime;

        private const float MaxExitTime = 1.8f;
        private float exitTime;

        // Saves
        private int totalScore = 0;
        private int[] powerUpPicked = new int[4];

        private float tempHealth, tempScore;
        private Texture2D healthBar;


        private const Buttons ContinueButton = Buttons.A;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.2);
            TransitionOffTime = TimeSpan.FromSeconds(0.2);
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            // Load fonts
            smallFont = content.Load<SpriteFont>("Fonts/Header");
            bigFont = content.Load<SpriteFont>("Fonts/HeaderBig");

            // Load overlay textures
            winOverlay = content.Load<Texture2D>("Overlays/NextLevel");
            gameOverOverlay = content.Load<Texture2D>("Overlays/TryAgain");

            healthBar = content.Load<Texture2D>("Overlays/HealthBar");

            titleSafeArea = ScreenManager.GraphicsDevice.Viewport.TitleSafeArea;
            hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            LoadNextLevel(true);

            // A real game would probably have more content than this sample, so
            // it would take longer to load. We simulate that by delaying for a
            // while, giving you a chance to admire the beautiful loading screen.
            //Thread.Sleep(1000);

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (IsActive)
            {
                if(startTime == MaxStartTime)
                    ScreenManager.AddScreen(new IntroScreen(levelIndex), ControllingPlayer);

                if (startTime > 0.0f)
                {
                    // wait to pass control to level
                    startTime = Math.Max(0.0f, startTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                    return;
                }

                level.Update(gameTime);
            }
        }


        private void LoadNextLevel(bool newLoading)
        {
            // Find the path of the next level.
            string levelPath;


            // Loop here so we can try again when we can't find a level.
            while (true)
            {
                // Try to find the next level. They are sequentially numbered txt files.
                levelPath = String.Format("Levels/{0}.txt", ++levelIndex);
                levelPath = Path.Combine(StorageContainer.TitleLocation, "Content/" + levelPath);
                if (File.Exists(levelPath))
                    break;

                // If there isn't even a level 0, something has gone wrong.
                if (levelIndex == 0)
                    throw new Exception("No levels found.");

                // Load main menu screen
                LoadingScreen.Load(ScreenManager, false, null, new BackgroundScreen(), new MainMenuScreen());

                return;
            }

            // Unloads the content for the current level before loading the next one.
            if (level != null)
            {
                // save score and powerUps before loading Level is Disposed
                if (newLoading)
                {
                    totalScore = level.Score;

                    for (int i = 0; i < 4; i++)
                    { level.selectedPowerUpIndex = i; powerUpPicked[i] = level.SelectedPowerUpPicked; }
                }

                level.Dispose();
            }

            // Load the level.            
            level = new Level(ScreenManager.Game.Services, levelPath, levelIndex, totalScore, powerUpPicked);

            exitTime = MaxExitTime;
            startTime = MaxStartTime;

            tempHealth = level.Player.CurrentHealth;
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel(false);
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                bool continuePressed =
                    keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadState.IsButtonDown(ContinueButton);

                // Perform the appropriate action to advance the game and
                // to get the player back to playing.
                if (!wasContinuePressed && continuePressed && exitTime < 0.0f)
                {
                    if (!level.Player.IsAlive) ReloadCurrentLevel();

                    else if (level.ReachedExit) LoadNextLevel(true);
                }

                wasContinuePressed = continuePressed;
            }
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Our player and enemy are both actually just text strings.
            spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            level.Draw(gameTime, spriteBatch);

            DrawOverlay((float)gameTime.ElapsedGameTime.TotalSeconds);

            DrawPowerupHealthScore((float)gameTime.ElapsedGameTime.TotalSeconds);

            float fps = (1000.0f / (float)gameTime.ElapsedRealTime.TotalMilliseconds);
            spriteBatch.DrawString(smallFont, "fps : " + fps.ToString("00.0"), new Vector2(10, 50), Color.BlueViolet);


            spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0) ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }

        private void DrawOverlay(float elapsed)
        {
            // Determine the status overlay message to show.
            Texture2D status = null;

            if (level.ReachedExit) { status = winOverlay; exitTime -= elapsed; }

            else if (!level.Player.IsAlive) { status = gameOverOverlay; exitTime -= elapsed; }


            if (status != null && exitTime < 0.0f)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);

                spriteBatch.DrawString(smallFont, level.Score.ToString("000"), center + new Vector2(125, 0), Color.White);
            }
        }

        private void DrawPowerupHealthScore(float elapsed)
        {
            string text; Vector2 position, stringSize; SpriteFont font;
            int topClearance = 8;

            string stringPowerUpPicked = level.SelectedPowerUpPicked.ToString("00");
            position = new Vector2(10, 20);

            switch (level.selectedPowerUpIndex)
            {
                case 0:
                    spriteBatch.DrawString(smallFont, "Marble : " + stringPowerUpPicked, position, Color.DarkMagenta); break;
                case 1:
                    spriteBatch.DrawString(smallFont, "Bean Seed : " + stringPowerUpPicked, position, Color.ForestGreen); break;
                case 2:
                    spriteBatch.DrawString(smallFont, "Soap Bubble : " + stringPowerUpPicked, position, Color.SteelBlue); break;
                case 3:
                    spriteBatch.DrawString(smallFont, "Recycle Ball : " + stringPowerUpPicked, position, Color.DimGray); break;
            }


            // Health
            text = "Health"; font = smallFont; stringSize = font.MeasureString(text);
            position = new Vector2(center.X, stringSize.Y / 2 + topClearance);
            spriteBatch.DrawString(font, text, position - stringSize / 2, Color.White);

            tempHealth = Math.Max(level.Player.CurrentHealth, tempHealth - elapsed * 10);
            text = tempHealth.ToString("00"); font = bigFont; stringSize = font.MeasureString(text);

            Vector2 helthBarSize = new Vector2(healthBar.Width, healthBar.Height);
            Rectangle source = new Rectangle(0, 0, (int)(tempHealth * helthBarSize.X) / level.Player.MaxHealth,
                (int)helthBarSize.Y);
            position.Y += stringSize.Y / 2 + 5;
            spriteBatch.Draw(healthBar, position - helthBarSize / 2, source, Color.WhiteSmoke);

            position.X += -helthBarSize.X / 2 + source.Width + stringSize.X / 2 + 5;
            spriteBatch.DrawString(font, text, position - stringSize / 2, Color.WhiteSmoke);


            // Score
            text = "Score"; font = smallFont; stringSize = font.MeasureString(text);
            position = new Vector2(center.X + 320, stringSize.Y / 2 + topClearance);
            spriteBatch.DrawString(font, text, position - font.MeasureString(text) / 2, Color.White);

            tempScore = Math.Min(level.Score, tempScore + elapsed * 20);
            text = tempScore.ToString("0000"); font = bigFont; stringSize = font.MeasureString(text);
            position.Y += stringSize.Y / 2 + 5;
            spriteBatch.DrawString(font, text, position - stringSize / 2, Color.Yellow);
        }


        #endregion
    }
}
