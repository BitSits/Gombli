using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace Gombli
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        #region Initialization


        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base("Main Menu")
        {
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry("Play Game");
           // MenuEntry controlsMenuEntry = new MenuEntry("Controls");
            MenuEntry creditsMenuEntry = new MenuEntry("Credits");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers.
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            //controlsMenuEntry.Selected += ControlsMenuEntrySelected;
            creditsMenuEntry.Selected += CreditsMenuEntrySelected;

            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(playGameMenuEntry);
            //MenuEntries.Add(controlsMenuEntry);
            MenuEntries.Add(creditsMenuEntry);

            MenuEntries.Add(exitMenuEntry);
        }


        public override void LoadContent()
        {
            base.LoadContent();

            MediaPlayer.Play(ScreenManager.Game.Content.Load<Song>("Music/02 - traiin"));
            MediaPlayer.IsRepeating = true;
        }
        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            //LoadingScreen.Load(ScreenManager, true, e.PlayerIndex, new GameplayScreen());
            ScreenManager.AddScreen(new GameplayScreen(), e.PlayerIndex);
        }


        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        /*void ControlsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new ControlsMenuScreen(), e.PlayerIndex);
        }*/

        void CreditsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new CreditsMenuScreen(), e.PlayerIndex);
        }


        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }

        #endregion
    }
}
