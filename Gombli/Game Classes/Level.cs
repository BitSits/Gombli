using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.IO;

namespace Gombli
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        public int LevelIndex { get; private set; }

        // Physical structure of the level.
        private Tile[,] tiles;

        // The layer which entities are drawn on top of.
        private const int EntityLayer = 1;
        private Layer[] layers;
        private Vector2 cameraPosition = Vector2.Zero;

        private const int InflationAmount = 150;

        /// <summary>
        /// visible rectangle inflated by 150 on all sides
        /// </summary>
        public static Rectangle VisibleRectangleInflated { get; private set; }
        public static Rectangle VisibleRectangle { get; private set; }

        // Entities in the level.
        public Player Player { get { return player; } }
        Player player;

        private List<Gem> gems = new List<Gem>();

        private List<Info> infos = new List<Info>();
        private List<string> infoStrings = new List<string>();
        private InformationBox infoBox;
        private int infoCounter = 0;


        // Enemies and Animals
        public Enemy[] Enemies { get { return enemies.ToArray(); } }
        private List<Enemy> enemies = new List<Enemy>();

        private List<Enemy> germs = new List<Enemy>();

        private List<Animal> animals = new List<Animal>();


        // PowerUp Things   0-Marble 1-Bean Seed 2-Bubble 3-Recycle Ball
        public int selectedPowerUpIndex = 0;
        public int SelectedPowerUpPicked
        { get { return powerUpPicked[selectedPowerUpIndex] / powerUpDrop[selectedPowerUpIndex]; } }

        private int[] powerUpPicked = new int[4];
        private int[] powerUpLoad = { 10, 5, 5, 1 };
        private int[] powerUpDrop = { 1, 1, 2, 1 };
        private List<PowerUp> powerUps = new List<PowerUp>();

        // Tiles
        public MovingTile[] MovingTiles { get { return movingTiles.ToArray(); } }
        private List<MovingTile> movingTiles = new List<MovingTile>();

        public BreakableTile[] BreakableTiles { get { return breakableTiles.ToArray(); } }
        private List<BreakableTile> breakableTiles = new List<BreakableTile>();

        public OzoneTile[] OzoneTiles { get { return ozoneTiles.ToArray(); } }
        private List<OzoneTile> ozoneTiles = new List<OzoneTile>();
        private const int maxMove = 9; int moves = maxMove;

        private List<SlidingPuzzleBlock> slidingPuzzleBlocks = new List<SlidingPuzzleBlock>();

        private Point reset = InvalidPositionPoint;


        // Key locations in the level.        
        private Vector2 start;
        private Point exit = InvalidPositionPoint;
        public static readonly Point InvalidPositionPoint = new Point(-1, -1);
        public static readonly Vector2 InvalidPositionVector = new Vector2(-100);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed

        public int Score { get { return score; } }
        int score;

        public bool ReachedExit { get { return reachedExit; } }
        bool reachedExit;

        // Level content.        
        public ContentManager Content { get { return content; } }
        ContentManager content;

        private SoundEffect exitReachedSound;

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        public Level(IServiceProvider serviceProvider, string path, int levelIndex, 
            int score, int[] powerUpPickedSaved)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            LevelIndex = levelIndex;

            this.score = score;
            powerUpPickedSaved.CopyTo(this.powerUpPicked, 0);

            LoadMusic();

            LoadPowerUpSaved();

            LoadLevelInfo(path);

            LoadTiles(path);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Layer[2];
            layers[0] = new Layer(content, "Backgrounds/" + LevelIndex + "_Layer0", 0.2f);
            layers[1] = new Layer(content, "Backgrounds/" + LevelIndex + "_Layer1", 0.5f);

            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Levels/ExitReached");
        }

        private void LoadMusic()
        {
            switch (LevelIndex)
            {
                case 0: { MediaPlayer.Play(content.Load<Song>("Music/06 - hi ha ho")); break; }
                case 1: { MediaPlayer.Play(content.Load<Song>("Music/04 - pialopiano")); break; }
            }

            MediaPlayer.IsRepeating = true;
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        private void LoadTiles(string path)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPositionPoint)
                throw new NotSupportedException("A level must have an exit.");

            if(ozoneTiles.Count !=0 && reset == InvalidPositionPoint)
                throw new NotSupportedException("A level must have an Ozone Reseter if Ozone Tile is Present.");
        }

        /// <summary>
        /// Load level informations
        /// </summary>
        /// <param name="path"></param>
        private void LoadLevelInfo(string path)
        {
            path = path.Insert(path.Length - ".txt".Length, "_Info");

            using (StreamReader reader = new StreamReader(path))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    infoStrings.Add(line);
                    line = reader.ReadLine();
                }
            }

            infoBox = new InformationBox(Content.Load<Texture2D>("InfoBox/Box"), Content.Load<SpriteFont>("Fonts/Small"));
        }

        /// <summary>
        /// Load saved power ups from the last level
        /// </summary>
        private void LoadPowerUpSaved()
        {
            powerUpPicked[0] *= powerUpDrop[0];
            for (int i = 0; i < powerUpPicked[0]; i++)
                powerUps.Add(new Marble(this, InvalidPositionVector, false));

            powerUpPicked[1] *= powerUpDrop[1];
            for (int i = 0; i < powerUpPicked[1]; i++)
                powerUps.Add(new BeanSeed(this, InvalidPositionVector, false));

            powerUpPicked[2] *= powerUpDrop[2];
            for (int i = 0; i < powerUpPicked[2]; i++)
                powerUps.Add(new Bubble(this, InvalidPositionVector, i % 2 == 0 ? true : false, false));

            powerUpPicked[3] *= powerUpDrop[3];
            for (int i = 0; i < powerUpPicked[3]; i++)
                powerUps.Add(new RecycleBall(this, InvalidPositionVector, false));
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.': return new Tile(null, TileCollision.Passable);

                // Player 1 start point
                case 'A': return LoadStartTile(x, y);

                // Various enemies
                case 'B': return LoadEnemyTile(x, y, "Basic", Orientation.Horizontal, true, 5);
                case 'b': return LoadEnemyTile(x, y, "Basic", Orientation.Vertical, true, 5);

                case 'C': return LoadEnemyTile(x, y, "Tough", Orientation.Horizontal, true, 10);
                case 'c': return LoadEnemyTile(x, y, "Tough", Orientation.Vertical, true, 10);

                // Various Animals
                case 'D': return LoadAnimalTile(x, y, "Ground", false, false, 1);
                case 'E': return LoadAnimalTile(x, y, "Lazy", false, true, 2);
                case 'F': return LoadAnimalTile(x, y, "Floating", true, false, 1);

                // Exit
                case 'Z': return LoadExitTile(x, y);

                // Information
                case '!': return LoadInfoTile(x, y);

                // Gem
                case '$': return LoadGemTile(x, y);

                // Floating platform
                case '-': return LoadTile(LevelIndex.ToString() + "_Platform", TileCollision.Platform);

                // Water block
                case '~': return LoadWaterTile(x, y);

                // Impassable block
                case '#': return LoadImpassableTile(LevelIndex.ToString() + "_BlockA", 9, TileCollision.Impassable);

                case 'L': return LoadTile("Ladder", TileCollision.Ladder);

                case 'm': return LoadMovingTile(x, y, 3);

                case '|': return LoadBreakableTile(x, y);

                case '/': return LoadTile(LevelIndex.ToString() + "_BlockL", TileCollision.SlopeMinus);

                case '\\': return LoadTile(LevelIndex.ToString() + "_BlockR", TileCollision.SlopePlus);

                // Puzzles
                case '0': 
                case '1': 
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': return LoadPuzzleTile(x, y, tileType);
                case 'R': return LoadResetTile(x, y);

                // Load PowerUps
                case '@': return LoadRecycleBall(x, y);
                case '&': return LoadBeanSeed(x, y);
                case '%': return LoadBubble(x, y);
                case '*': return LoadMarble(x, y);

                // Load Direction Tile
                case 'l': return new Tile(null, TileCollision.Left);
                case 'u': return new Tile(null, TileCollision.Up);
                case 'r': return new Tile(null, TileCollision.Right);
                case 'd': return new Tile(null, TileCollision.Down);
                case 'x': return new Tile(null, TileCollision.Reverse);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////


        
        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadImpassableTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }

        private Tile LoadEmptyTile(int x,int y)
        {
            if (LevelIndex == 1) return LoadWaterTile(x, y);
            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadWaterTile(int x, int y)
        {
            TileCollision topCollision = GetCollision(x, y - 1);

            if (topCollision == TileCollision.Passable) return LoadTile("WaterTop", TileCollision.Water);

            return LoadTile("Water", TileCollision.Water);
        }

        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);

            return LoadEmptyTile(x,y);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPositionPoint)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            if (LevelIndex == 1) return LoadTile("Exit", TileCollision.Water);
            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet, Orientation orientation, 
            bool isPollutant, int contactDamage)
        {
            Point position = GetBounds(x, y).Center;
            enemies.Add(new Enemy(this, new Vector2(position.X, position.Y), spriteSet, orientation, isPollutant, contactDamage));

            return LoadEmptyTile(x,y);
        }

        private Tile LoadInfoTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            infos.Add(new Info(this, infoStrings[infoCounter++], new Vector2(position.X, position.Y)));

            return LoadEmptyTile(x, y);
        }

        private Tile LoadAnimalTile(int x, int y, string spriteSet, bool isFloating, bool isLazy, 
            int playerDamage)
        {
            Vector2 position = GetBounds(x, y).GetBottomCenter();
            animals.Add(new Animal(this, position, spriteSet, isFloating, isLazy, playerDamage));

            return LoadEmptyTile(x, y);
        }

        private Tile LoadPuzzleTile(int x, int y, char type)
        {
            Point position = GetBounds(x, y).Location;

            if (LevelIndex == 0)
            {
                if (type == '0') SlidingPuzzleBlock.puzzlePosition = new Vector2(position.X, position.Y);
                else
                    slidingPuzzleBlocks.Add(new SlidingPuzzleBlock(this, new Vector2(position.X, position.Y), type));
            }

            if (LevelIndex == 1)
                ozoneTiles.Add(new OzoneTile(this, new Vector2(position.X, position.Y), type));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadResetTile(int x, int y)
        {
            reset = GetBounds(x, y).Center;

            return LoadTile("Reset", TileCollision.Passable);
        }

        private Tile LoadRecycleBall(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            for (int i = 0; i < powerUpLoad[3] * powerUpDrop[3]; i++)
                powerUps.Add(new RecycleBall(this, new Vector2(position.X, position.Y), i == 0 ? true : false));

            return LoadEmptyTile(x, y);
        }

        private Tile LoadBeanSeed(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            for (int i = 0; i < powerUpLoad[1] * powerUpDrop[1]; i++)
                powerUps.Add(new BeanSeed(this, new Vector2(position.X, position.Y), i == 0 ? true : false));

            return LoadEmptyTile(x, y);
        }

        private Tile LoadBubble(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            for (int i = 0; i < powerUpLoad[2] * powerUpDrop[2]; i++)
            {
                powerUps.Add(new Bubble(this, new Vector2(position.X, position.Y), i % 2 == 0 ? true : false, 
                    i == 0 ? true : false));
            }

            return LoadEmptyTile(x, y);
        }

        private Tile LoadMarble(int x, int y)
        {
            Point position = GetBounds(x, y).Center;

            for (int i = 0; i < powerUpLoad[0] * powerUpDrop[0]; i++)
                powerUps.Add(new Marble(this, new Vector2(position.X, position.Y), i == 0 ? true : false));

            return LoadEmptyTile(x, y);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y)));

            return LoadEmptyTile(x, y);
        }

        /// <summary>
        /// Instantiates a horizontally moving tile and puts it in the level.
        /// </summary>
        private Tile LoadMovingTile(int x, int y, int variationCount)
        {
            int index = random.Next(variationCount);

            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            movingTiles.Add(new MovingTile(this, index, new Vector2(position.X, position.Y), Orientation.Horizontal));

            return LoadEmptyTile(x, y);
        }

        private Tile LoadBreakableTile(int x, int y)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            breakableTiles.Add(new BreakableTile(this, new Point((int)position.X, (int)position.Y)));

            return LoadEmptyTile(x, y);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive  && !reachedExit)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                //int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                //seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                //timeRemaining -= TimeSpan.FromSeconds(seconds);
                //score += seconds * PointsPerSecond;
            }
            else
            {
                UpdateMovingTilesAndAddVelocity(gameTime);

                Player.Update(gameTime);

                UpdateGems(gameTime);

                UpdateOzoneTiles(gameTime);

                UpdateSlidingPuzzle(gameTime);

                UpdateBreakableTiles(gameTime);

                UpdateInfo();

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height) OnPlayerHurt(-1);

                UpdatePowerUps(gameTime);

                UpdateEnemies(gameTime);

                UpdateAnimals(gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    bool allOzone = true;
                    foreach (OzoneTile oz in ozoneTiles) if (!oz.IsGlowing) allOzone = false;

                    if (allOzone)
                        OnExitReached();
                }
            }
        }

        private void UpdateMovingTilesAndAddVelocity(GameTime gameTime)
        {
            int playerHalfWidth = Player.BoundingRectangle.Width/2;
            Point playerL = new Point((int)Player.Position.X - playerHalfWidth, (int)Player.Position.Y);
            Point playerR = new Point((int)Player.Position.X + playerHalfWidth, (int)Player.Position.Y);

            foreach (MovingTile movingTile in movingTiles)
            {
                movingTile.Update(gameTime);

                if (Player.IsOnMovingTile && (movingTile.BoundingRectangle.Contains(playerL) 
                    || movingTile.BoundingRectangle.Contains(playerR)))
                    Player.OnMovingTile(movingTile);
            }
        }

        private void UpdateOzoneTiles(GameTime gameTime)
        {
            Point playerPos = new Point((int)Player.Position.X, (int)Player.Position.Y);

            if (Player.IsOnOzoneTile)
                foreach (OzoneTile ozoneTile in ozoneTiles)
                {
                    if (ozoneTile.BoundingRectangle.Contains(playerPos))
                    {
                        if (!ozoneTile.PlayerIsOn && moves > 0) { ozoneTile.SwitchGlow(); moves -= 1; }
                        ozoneTile.PlayerIsOn = true;
                    }
                    else ozoneTile.PlayerIsOn = false;
                }

            if (Player.BoundingRectangle.Contains(reset) && player.IsOnOzoneTile)
            {
                moves = maxMove;
                foreach (OzoneTile ozoneTile in ozoneTiles) ozoneTile.ResetGlow();
            }
        }

        private void UpdateSlidingPuzzle(GameTime gameTime)
        {
            foreach (SlidingPuzzleBlock slidingBlock in slidingPuzzleBlocks)
            {
                slidingBlock.Update(gameTime);

                if (slidingBlock.State == PowerState.Ground && !SlidingPuzzleBlock.IsActive
                    && Player.BoundingRectangle.Intersects(slidingBlock.BoundingRectangle))
                    slidingBlock.Activate();

                foreach (SlidingPuzzleBlock block in slidingPuzzleBlocks)
                {
                    if (slidingBlock != block && slidingBlock.BoundingRectangle.Intersects(block.BoundingRectangle)
                        && block.State == PowerState.Die)
                    {
                        slidingBlock.Collision(block.BoundingRectangle);
                    }
                }
            }
        }

        private void UpdateBreakableTiles(GameTime gameTime)
        {
            foreach (BreakableTile breakableTile in breakableTiles)
            foreach (PowerUp power in powerUps)
            {
                if (power is Marble && power.State == PowerState.Active && breakableTile.State != PowerState.Die
                    && power.BoundingCircle.Intersects(breakableTile.BoundingRectangle))
                {
                    power.OnHurt(10);
                    breakableTile.ChangeState();
                }
            }
        }

        /// <summary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player);
                }
            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.Update(gameTime);

                if (enemy.BoundingCircle.Intersects(Player.BoundingRectangle) && Player.CanGetHurt 
                    && enemy.IsAlive && !enemy.IsHurt)
                    OnPlayerHurt(enemy.PlayerDamage);
            }
        }

        private void UpdateAnimals(GameTime gameTime)
        {
            foreach (Animal animal in animals)
            {
                animal.Update(gameTime);

                if (animal.BoundingRectangle.Intersects(Player.BoundingRectangle) && Player.CanGetHurt)
                    OnPlayerHurt(animal.PlayerDamage);
            }
        }

        private void UpdateInfo()
        {
            foreach (Info info in infos)
            {
                if(info.State == PowerState.Ground && info.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    info.Picked();
                    infoBox.DisplayText(info.Message);
                }
            }
        }



        private void UpdatePowerUps(GameTime gameTime)
        {
            bool fire = Player.Throw;

            for (int i = 0; i< powerUps.Count; i++)
            {
                PowerUp power = powerUps[i];
                if (fire && selectedPowerUpIndex == power.PowerIndex && power.State == PowerState.Pick)
                {
                    fire = false;
                    for (int j = 0; j < powerUpDrop[selectedPowerUpIndex]; j++)
                    { powerUps[i + j].Droped(); DecreasePowerCount(power); }
                }

                power.Update(gameTime);

                if (power.State == PowerState.Ground && power.BoundingCircle.Intersects(Player.BoundingRectangle))
                { power.Picked(); InccreasePowerCount(power); }

                foreach (var enemy in enemies)
                {
                    if (enemy.BoundingCircle.Intersects(power.BoundingCircle) && enemy.IsAlive && 
                        power.State == PowerState.Active)
                    {
                        if ((power is Bubble && enemy.IsPollutant) || (power is BeanSeed && !enemy.IsPollutant))
                        {
                            enemy.OnHurt(-1);
                            power.OnHurt(10);
                            continue;
                        }

                        bool wasAlive = enemy.IsAlive;

                        enemy.OnHurt(power.Damage);
                        power.OnHurt(enemy.PowerUpDamage);

                        if (wasAlive && !enemy.IsAlive) score += enemy.PointValue;
                    }
                }
            }
        }

        private void InccreasePowerCount(PowerUp power)
        {
            ++powerUpPicked[power.PowerIndex]; score += power.PointValue;
        }

        private void DecreasePowerCount(PowerUp power)
        {
            --powerUpPicked[power.PowerIndex];
        }
        


        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += Gem.PointValue;

            gem.OnCollected(collectedBy);
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerHurt(int hurtBy)
        {
            Player.OnHurt(hurtBy);
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player.OnReachedExit();
            exitReachedSound.Play();
            reachedExit = true;
        }

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }
        
        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch, cameraPosition.X);
            spriteBatch.End();

            ScrollRate(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0.0f);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, cameraTransform);
            DrawTiles(spriteBatch);


            foreach (Gem gem in gems) gem.Draw(gameTime, spriteBatch);

            foreach (MovingTile movingTile in movingTiles) movingTile.Draw(gameTime, spriteBatch);
            foreach (OzoneTile ozoneTile in ozoneTiles) ozoneTile.Draw(gameTime, spriteBatch);
            foreach (BreakableTile breakableTile in breakableTiles) breakableTile.Draw(gameTime, spriteBatch);

            foreach (SlidingPuzzleBlock slidingBlock in slidingPuzzleBlocks) slidingBlock.Draw(gameTime, spriteBatch);

            foreach (Animal animal in animals) animal.Draw(gameTime, spriteBatch);

            foreach (Info info in infos) info.Draw(gameTime, spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (PowerUp power in powerUps) power.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies) enemy.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch, cameraPosition.X);

            infoBox.Draw(gameTime, spriteBatch);
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition.X / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width + 1;
            right = Math.Min(right, Width - 1);

            int top = (int)Math.Floor(cameraPosition.Y / Tile.Height);
            int bottom = top + spriteBatch.GraphicsDevice.Viewport.Height / Tile.Height + 1;
            bottom = Math.Min(bottom, Height - 1);

            // For each tile position
            for (int y = top; y <= bottom; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        private void ScrollRate(Viewport viewport)
        {
            const float ViewMargin = 0.35f;

            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition.X + marginWidth;
            float marginRight = cameraPosition.X + viewport.Width - marginWidth;

            float marginHeight = viewport.Height * ViewMargin;
            float marginTop = cameraPosition.Y + marginHeight;
            float marginBottom = cameraPosition.Y + viewport.Height - marginHeight;

            // Calculate how far to scroll when the player is near the edges of the screen.
            Vector2 cameraMovement = Vector2.Zero;
            if (Player.Position.X < marginLeft)
                cameraMovement.X = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovement.X = Player.Position.X - marginRight;

            if (Player.Position.Y < marginTop)
                cameraMovement.Y = Player.Position.Y - marginTop;
            else if (Player.Position.Y > marginBottom)
                cameraMovement.Y = Player.Position.Y - marginBottom;

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPosition = Tile.Width * Width - viewport.Width;
            cameraPosition.X = MathHelper.Clamp(cameraPosition.X + cameraMovement.X, 0.0f, maxCameraPosition);

            maxCameraPosition = Tile.Height * Height - viewport.Height;
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y + cameraMovement.Y, 0.0f, maxCameraPosition);

            //Update Expanded viewport Rectangle
            Rectangle visiRect = new Rectangle((int)cameraPosition.X, (int)cameraPosition.Y, (int)viewport.Width, (int)viewport.Height);
            VisibleRectangle = visiRect;
            visiRect.Inflate(InflationAmount, InflationAmount);
            VisibleRectangleInflated = visiRect;
        }

        #endregion
    }
}
