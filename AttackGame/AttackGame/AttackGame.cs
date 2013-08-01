using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace AttackGame
{
    #region enums used by other classes

    public enum GameDifficulty { easy, medium, hard }

    #endregion

    /// <summary>
    /// The AttackGame class is the main game class, and handles the logistics of the game
    /// </summary>
    public class AttackGame : Microsoft.Xna.Framework.Game
    {
        #region Fields: camera, graphics managers, object lists, important objects, keyboard state

        #region enums that are private
        private enum OptionSelectedSplashWinLoss { play, controls, difficulty, quit }

        public enum GameState { splash, controls, difficultyselect, running, upgradeMenu, winLoss }
        #endregion

        #region constant strings
        public const string StrTimeAlive = "Time Alive: ";
        public const string StrHealth = "Hull Strength: ";
        public const string StrShield = "Shield Strength: ";

        public const string diffeasy = "easy";
        public const string diffmed = "medium";
        public const string diffhard = "hard";

        public const string menuPlay = "play";
        public const string menuDifficulty = "difficulty";
        public const string menuControls = "instructions";
        public const string menuQuit = "quit";
        public const string menuInstructions = "Enter: Select";

#endregion

        #region Game Constants
        public const float BoundaryWall = 2000.0f;
        public const float BoundaryCeiling = 2000.0f;
        #endregion

        #region State management and menus
        private GameState state;
        private OptionSelectedSplashWinLoss splashSelection;

        private int upgradeSelection; //the actual stat selected for upgrade

        private int upgradeMenuTop; //the stat at the top of the upgrade menu display, used for scrolling
        private int upgradeMenuSelection;//the location on the upgrademenu of the selector

        private GameDifficulty difficulty;
        public GameDifficulty Difficulty
        {
            get { return difficulty; }
        }

        private bool controlsSeen;
        private int controlScreenNumber;
        #endregion

        //Output
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private bool playSounds;
        public bool PlaySounds
        {
            get { return playSounds; }
        }

        //HUD
        Texture2D HUDHSBack;
        Texture2D HUDTop;
        Texture2D red;
        Texture2D blue;

        //Camera
        private Camera gameCamera;

        //Objects
        private List<GameObject> objects;
        public List<GameObject> Objects
        {
            get { return objects; }
        }

        private List<MovingGameObject> updateableObjects;
        public List<MovingGameObject> UpdateableObjects
        {
            get { return updateableObjects; }
        }

        private List<MovingGameObject> nextUpdateableObjects;

        //All the spaceships categorised
        private List<Enemy> enemies;
        public List<Enemy> Enemies
        {
            get { return enemies; }
        }


        private Craft avatar;
        public Craft Avatar
        {
            get { return avatar; }
        }

        private GameObject ground;
        private List<GameObject> loadList; //create a list containing all possible objects to be loaded.;

        //Input
        KeyboardState currentState;
        KeyboardState prevState;

        //Scorekeeping
        private float score;
        private float multiplier;
        private TimeSpan timeAlive;

        private PlayerStats playerstats;
        public PlayerStats PlayerStats
        {
            get { return playerstats; }
        }

        //Enemy spawn timer
        private float spawnDelay;

        private TimeSpan timeSinceSpawn;

        #endregion


        public AttackGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 853;
            graphics.PreferredBackBufferHeight = 480;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            objects = new List<GameObject>();
            updateableObjects = new List<MovingGameObject>();
            nextUpdateableObjects = new List<MovingGameObject>();
            enemies = new List<Enemy>();
            loadList = new List<GameObject>();

            difficulty = GameDifficulty.medium;

            // Create the chase camera
            gameCamera = new Camera();

            // Set the camera offsets
            gameCamera.DesiredPositionOffset = new Vector3(0.0f, 40.0f, 70.0f);
            gameCamera.LookAtOffset = new Vector3(0.0f, 0.0f, -100.0f);

            // Set camera perspective
            gameCamera.NearPlaneDistance = 10.0f;
            gameCamera.FarPlaneDistance = 10000.0f;

            loadList.Add(new Mine("Models/mineModel", this, null));
            loadList.Add(new Orbiter("Models/stationary", this, null));
            loadList.Add(new Craft("Models/spaceship", this));
            loadList.Add(new Bullet("Models/bulletCube", this, null, 10.0f));

            loadList.Add(new GameObject("Models/cube10uR", this));

            foreach (GameObject thing in loadList)
            {
                thing.IsActive = false;
            }

            //Initialise keystates
            currentState = new KeyboardState();
            prevState = new KeyboardState();

            upgradeMenuSelection = 0;
            upgradeMenuTop = 0;
            upgradeSelection = 0;

            controlsSeen = false;
            controlScreenNumber = 0;

            playSounds = true;

            reset();

            base.Initialize();

        }

        public void reset()
        {
            //Set up lists
            objects = new List<GameObject>();
            updateableObjects = new List<MovingGameObject>();
            nextUpdateableObjects = new List<MovingGameObject>();
            enemies = new List<Enemy>();

            //Set up the abstract game variables, stats, etc.
            playerstats = new PlayerStats();
            if (!controlsSeen)
            {
                splashSelection = OptionSelectedSplashWinLoss.controls;
            }
            else
            {
                splashSelection = OptionSelectedSplashWinLoss.play;
            }

            upgradeSelection = 0;
            score = 0;

            //Initialise important objects
            avatar = new Craft("Models/cube10uR", this);
            avatar.Position = new Vector3(0.0f, 500.0f, 0.0f);

            ground = new Terrain("Models/ground", this);
            ground.World = Matrix.Identity;

            timeSinceSpawn = new TimeSpan(0, 0, 0);
            timeAlive = new TimeSpan(0, 0, 0);
            state = GameState.splash;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("gameFont");
            ground.Model = Content.Load<Model>(ground.modelName);
            foreach (GameObject thing in loadList)
            {
                Model m = Content.Load<Model>(thing.modelName);
                thing.Model = m;
                BoundingSphere bs = CalculateBoundingSphere(m);
                thing.BoundingSphere = bs;
                thing.destroy();
            }

            //Sound effects obtained from http://www.freesfx.co.uk 
            Bullet b = new Bullet(null, this, null, 1);
            b.HitEffect = Content.Load<SoundEffect>("Sounds/hitEffect");
            b.ShootEffect = Content.Load<SoundEffect>("Sounds/fireEffect");
            b.destroy();

            HUDHSBack = Content.Load<Texture2D>("Textures/HUDLR");
            HUDTop = Content.Load<Texture2D>("Textures/HUDT");
            red = Content.Load<Texture2D>("Textures/red");
            blue = Content.Load<Texture2D>("Textures/blue");
        }
        
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            //Get input
            prevState = currentState;
            currentState = Keyboard.GetState();

            switch (state)
            {
                #region Splash screen controls
                case GameState.splash:
                    if (currentState.IsKeyDown(Keys.Down) && prevState.IsKeyUp(Keys.Down))
                    {
                        switch (splashSelection)
                        {
                            case OptionSelectedSplashWinLoss.play:
                                splashSelection = OptionSelectedSplashWinLoss.controls;
                                break;
                            case OptionSelectedSplashWinLoss.controls:
                                splashSelection = OptionSelectedSplashWinLoss.difficulty;
                                break;
                            case OptionSelectedSplashWinLoss.difficulty:
                                splashSelection = OptionSelectedSplashWinLoss.quit;
                                break;
                        }
                    }

                    if (currentState.IsKeyDown(Keys.Up) && prevState.IsKeyUp(Keys.Up))
                    {
                        switch (splashSelection)
                        {
                            case OptionSelectedSplashWinLoss.controls:
                                splashSelection = OptionSelectedSplashWinLoss.play;
                                break;
                            case OptionSelectedSplashWinLoss.difficulty:
                                splashSelection = OptionSelectedSplashWinLoss.controls;
                                break;
                            case OptionSelectedSplashWinLoss.quit:
                                splashSelection = OptionSelectedSplashWinLoss.difficulty;
                                break;
                        }
                    }

                    if (currentState.IsKeyDown(Keys.Enter) && prevState.IsKeyUp(Keys.Enter))
                    {
                        switch (splashSelection)
                        {
                            case OptionSelectedSplashWinLoss.play:
                                state = GameState.running;
                                controlsSeen = true;
                                break;

                            case OptionSelectedSplashWinLoss.controls:
                                state = GameState.controls;
                                break;

                            case OptionSelectedSplashWinLoss.difficulty:
                                state = GameState.difficultyselect;
                                break;

                            case OptionSelectedSplashWinLoss.quit:
                                base.Exit();
                                break;
                        }
                    }

                    break;
                #endregion

                #region Difficulty select screen logic, up / down to select difficulty, enter to return to splash
                case GameState.difficultyselect:
                    switch (Difficulty)
                    {
                        case GameDifficulty.easy:
                            if (currentState.IsKeyDown(Keys.Down) && prevState.IsKeyUp(Keys.Down))
                            {
                                difficulty = GameDifficulty.medium;
                            }
                            break;

                        case GameDifficulty.medium:
                            if (currentState.IsKeyDown(Keys.Up) && prevState.IsKeyUp(Keys.Up))
                            {
                                difficulty = GameDifficulty.easy;
                            }
                            if (currentState.IsKeyDown(Keys.Down) && prevState.IsKeyUp(Keys.Down))
                            {
                                difficulty = GameDifficulty.hard;
                            }
                            break;

                        case GameDifficulty.hard:
                            if (currentState.IsKeyDown(Keys.Up) && prevState.IsKeyUp(Keys.Up))
                            {
                                difficulty = GameDifficulty.medium;
                            }
                            break;
                    }
                    if (currentState.IsKeyDown(Keys.Enter) && prevState.IsKeyUp(Keys.Enter))
                    {
                        state = GameState.splash;
                    }
                    break;
                #endregion

                #region Game running logic
                case GameState.running:
                    //Should be only called once, to spawn the first enemy
                    if (timeAlive.TotalSeconds == 0)
                    {
                        spawnRandomEnemy();
                    }

                    #region dealing with the camera
                    gameCamera.updateCameraPosition(avatar.Position, avatar.Direction, avatar.Up);
                    gameCamera.Update();
                    #endregion
                    for (int i = 0; i < updateableObjects.Count; i++)
                    {
                        updateableObjects[i].Update(gameTime);
                    }

                    timeSinceSpawn += gameTime.ElapsedGameTime;

                    timeAlive += gameTime.ElapsedGameTime;
                    multiplier = 1.0f + ((int)timeAlive.TotalMinutes) / 10.0f;

                    spawnDelay = 3.0f - ((int)timeAlive.TotalMinutes) / 10.0f;

                    if (timeSinceSpawn.TotalSeconds > spawnDelay)
                    {
                        spawnRandomEnemy();
                        timeSinceSpawn = new TimeSpan(0, 0, 0);
                    }

                    #region Dealing with any input not captured by the craft class
                    if (currentState.IsKeyDown(Keys.Escape) && prevState.IsKeyUp(Keys.Escape))
                    {
                        state = GameState.upgradeMenu;
                    }

                    if (currentState.IsKeyDown(Keys.M) && prevState.IsKeyUp(Keys.M))
                    {
                        playSounds = !playSounds;
                    }

                    #endregion

                    updateableObjects = nextUpdateableObjects; //This is done to prevent modifying the list as we iterate through it as that could produce problems.
                    break;
                #endregion

                #region Control screen logic
                case GameState.controls:
                    if (currentState.IsKeyDown(Keys.Enter) && prevState.IsKeyUp(Keys.Enter))
                    {
                        state = GameState.splash;
                    }
                    else if (currentState.IsKeyDown(Keys.Right) && prevState.IsKeyUp(Keys.Right))
                    {
                        if (controlScreenNumber < 3)
                        {
                            controlScreenNumber++;
                        }
                    }
                    else if (currentState.IsKeyDown(Keys.Left) && prevState.IsKeyUp(Keys.Left))
                    {
                        if(controlScreenNumber > 0)
                        {
                            controlScreenNumber--;
                        }
                    }
                    controlsSeen = true;
                    break;
                #endregion

                #region Loss screen logic
                case GameState.winLoss:
                    if (currentState.IsKeyDown(Keys.Enter) && prevState.IsKeyUp(Keys.Enter))
                    {
                        reset();
                    }
                    if(currentState.IsKeyDown(Keys.Escape) && prevState.IsKeyUp(Keys.Escape))
                    {
                        base.Exit();
                    }
                    break;
                #endregion

                #region Upgrade screen logic
                case GameState.upgradeMenu:
                    if (currentState.IsKeyDown(Keys.Down) && prevState.IsKeyUp(Keys.Down) && upgradeSelection != PlayerStats.statList.Count)
                    {
                        if (upgradeSelection < PlayerStats.statList.Count - 1)
                        {
                            upgradeSelection++;
                            upgradeMenuSelection++;
                        }
                        if (upgradeMenuSelection == 5 && upgradeSelection < PlayerStats.statList.Count - 1)
                        {
                            upgradeMenuTop++;
                            upgradeMenuSelection = 4;
                        }
                    }

                    if (currentState.IsKeyDown(Keys.Up) && prevState.IsKeyUp(Keys.Up))
                    {
                        if (!(upgradeSelection == 0))
                        {
                            upgradeSelection--;
                            upgradeMenuSelection--;
                        }
                        if (upgradeMenuSelection == 0 && upgradeSelection != 0)
                        {
                            upgradeMenuTop--;
                            upgradeMenuSelection = 1;
                        }
                    }

                    if (currentState.IsKeyDown(Keys.Enter) && prevState.IsKeyUp(Keys.Enter))
                    {
                        if (PlayerStats.statList[upgradeSelection].getNextLevelCost() <= score
                            && PlayerStats.statList[upgradeSelection].CurrentLevel < PlayerStats.statList[upgradeSelection].MaxLevel)
                        {
                            score -= PlayerStats.statList[upgradeSelection].getNextLevelCost();
                            PlayerStats.statList[upgradeSelection].upgradeStat();
                        }
                    }

                    if (currentState.IsKeyDown(Keys.Escape) && prevState.IsKeyUp(Keys.Escape))
                    {
                        state = GameState.running;
                    }
                    if (currentState.IsKeyDown(Keys.Q) && prevState.IsKeyUp(Keys.Q))
                    {
                        state = GameState.winLoss;
                    }
                    break;
                #endregion
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// Adds a number of points to the players score multiplied by the multiplier the player has and the difficulty modifier.
        /// </summary>
        /// <param name="points"></param>
        public void addPoints(float points)
        {
            float diffMult = 1.0f;
            switch (difficulty)
            {
                case GameDifficulty.easy:
                    diffMult = 2.0f;
                    break;
                case GameDifficulty.hard:
                    diffMult = 0.3f;
                    break;
            }
            score += points * multiplier * diffMult;
        }

        /// <summary>
        /// Spawns a random enemy type at a random location.
        /// </summary>
        public void spawnRandomEnemy()
        {
            Random rand = new Random();
            Vector3 pos = new Vector3();
            pos.X = rand.Next((int)(BoundaryWall * -1), (int)BoundaryWall);
            pos.Y = rand.Next(10, (int)BoundaryCeiling);
            pos.Z = rand.Next((int)(BoundaryWall * -1), (int)BoundaryWall);

            Enemy e = null;

            int r = rand.Next(2);
            switch (r)
            {
                case 0:
                    e = new Mine("", this, Avatar);
                    break;
                case 1:
                    e = new Orbiter("", this, Avatar);
                    break;
            }

            e.Position = pos;
            e.Direction = Vector3.Forward;
            e.Up = Vector3.Up;
            e.Right = Vector3.Right;
        }

        /// <summary>
        /// Public method to allow other objects to set the game to lost, called when the craft destroys itself
        /// </summary>
        public void loseGame()
        {
            state = GameState.winLoss;
        }

        #region Drawing
        //Taken from chasecam
        /// <summary>
        /// Simple model drawing method. The interesting part here is that
        /// the view and projection matrices are taken from the camera object.
        /// </summary>        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = graphics.GraphicsDevice;

            device.Clear(Color.Black);

            switch (state)
            {
                case GameState.running:
                    GraphicsDevice.BlendState = BlendState.Opaque;
                    GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

                    foreach (GameObject thing in objects)
                    {
                        if (thing.IsActive)
                        {
                            thing.updateWorld();
                            thing.DrawModel(gameCamera.ProjectionMatrix, gameCamera.ViewMatrix);
                        }
                    }

                    drawHUD();

                    break;

                case GameState.splash:
                    drawSplash();
                    break;

                case GameState.difficultyselect:
                    drawDifficultyMenu();
                    break;

                case GameState.winLoss:
                    drawGameOver();
                    break;

                case GameState.upgradeMenu:
                    drawUpgradeMenu();
                    break;

                case GameState.controls:
                    drawControlScreen();
                    break;
            }

            base.Draw(gameTime);
        }

        //All code until the end of this region based on code for displaying splash screens in FuelCell
        private void drawHUD()
        {
            String scoreHUD = "Score: " + score;
            String multiHUD = "Multiplier: " + multiplier + "x  ";
            String health = "" + (int)avatar.Hull;
            String shield = "" + (int)avatar.CurrentShield;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            Vector2 stringSize = spriteFont.MeasureString("Score: ");

            spriteBatch.Begin();


            #region Draw Background
            int w = HUDHSBack.Width;
            int h = HUDHSBack.Height;

            Rectangle r = new Rectangle((int)viewportSize.X - w, (int)viewportSize.Y - h, w, h);
            spriteBatch.Draw(HUDHSBack, r, Color.White);

            r = new Rectangle(0, (int)stringSize.Y - 2, (int)viewportSize.X, HUDTop.Height);
            spriteBatch.Draw(HUDTop, r, Color.White);
            #endregion

            #region Draw Strings
            spriteBatch.DrawString(spriteFont, scoreHUD, new Vector2(viewportSize.X / 2 - stringSize.X, stringSize.Y), Color.White);

            stringSize = spriteFont.MeasureString(multiHUD);

            spriteBatch.DrawString(spriteFont, multiHUD, new Vector2(viewportSize.X - stringSize.X, stringSize.Y), Color.Gold);

            stringSize = spriteFont.MeasureString("000");

            float sWid = (avatar.CurrentShield / PlayerStats.MaxShield.getValue()) * 100.0f;
            int shieldWidth = (int)sWid;
            float hWid = (avatar.Hull / PlayerStats.Hull.getValue()) * 100.0f;
            int healthWidth = (int)hWid;

            Rectangle shieldr = new Rectangle(((int)viewportSize.X - (int)stringSize.X - shieldWidth - 2), (int)viewportSize.Y - ((int)stringSize.Y * 2) + 2, shieldWidth, (int)stringSize.Y);
            Rectangle healthr = new Rectangle(((int)viewportSize.X - (int)stringSize.X - healthWidth - 2), (int)viewportSize.Y - ((int)stringSize.Y), healthWidth, (int)stringSize.Y);

            spriteBatch.Draw(blue, shieldr, Color.Blue);
            spriteBatch.Draw(red, healthr, Color.Red);

            spriteBatch.DrawString(spriteFont, shield, new Vector2(viewportSize.X - stringSize.X, viewportSize.Y - (stringSize.Y * 2) + 2), Color.Blue);
            spriteBatch.DrawString(spriteFont, health, new Vector2(viewportSize.X - stringSize.X, viewportSize.Y - stringSize.Y), Color.Red);
            #endregion

            spriteBatch.End();
        }

        private void drawSplash()
        {
            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 strCenter;

            graphics.GraphicsDevice.Clear(Color.Black);

            Vector2 stringSize =
                spriteFont.MeasureString(menuPlay);
            strCenter = new Vector2(stringSize.X, stringSize.Y);

            yOffsetText = (viewportSize.Y / 2 - strCenter.Y);
            xOffsetText = (viewportSize.X / 2 - strCenter.X);

            Vector2 strPlayPosition = new Vector2((int)xOffsetText, (int)yOffsetText - ((stringSize.Y*2) + 2));
            Vector2 strControlPosition = new Vector2((int)xOffsetText, (int)yOffsetText - (stringSize.Y + 2));
            Vector2 strDifficultyPosition = new Vector2((int)xOffsetText, (int)yOffsetText);
            Vector2 strQuitPosition = new Vector2 ((int)xOffsetText, (int)yOffsetText + (stringSize.Y + 2));

            Color notControlColor = Color.White;
            
            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, menuPlay, strPlayPosition, Color.White);
            spriteBatch.DrawString(spriteFont, menuControls, strControlPosition, Color.White);
            spriteBatch.DrawString(spriteFont, menuDifficulty, strDifficultyPosition, Color.White);
            spriteBatch.DrawString(spriteFont, menuQuit, strQuitPosition, Color.White);

            switch (splashSelection)
            {
                case OptionSelectedSplashWinLoss.play:
                    spriteBatch.DrawString(spriteFont, menuPlay, strPlayPosition, Color.Yellow);
                    break;

                case OptionSelectedSplashWinLoss.controls:
                    spriteBatch.DrawString(spriteFont, menuControls, strControlPosition, Color.Yellow);
                    break;

                case OptionSelectedSplashWinLoss.difficulty:
                    spriteBatch.DrawString(spriteFont, menuDifficulty, strDifficultyPosition, Color.Yellow);
                    break;

                case OptionSelectedSplashWinLoss.quit:
                    spriteBatch.DrawString(spriteFont, menuQuit, strQuitPosition, Color.Yellow);
                    break;

            }

            xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString(menuInstructions).X / 2);
            spriteBatch.DrawString(spriteFont, menuInstructions, new Vector2(xOffsetText, viewportSize.Y - (strCenter.Y + 5)), Color.DarkGray);

            spriteBatch.End();
        }

        private void drawDifficultyMenu()
        {
            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 strCenter;

            graphics.GraphicsDevice.Clear(Color.Black);

            xOffsetText = yOffsetText = 0;
            Vector2 stringSize =
                spriteFont.MeasureString(menuPlay);
            strCenter = new Vector2(stringSize.X, stringSize.Y);

            yOffsetText = (viewportSize.Y / 2 - strCenter.Y);
            xOffsetText = (viewportSize.X / 2 - strCenter.X);

            Vector2 strEasyPosition = new Vector2((int)xOffsetText, (int)yOffsetText - (stringSize.Y + 2));
            Vector2 strMedPosition = new Vector2((int)xOffsetText, (int)yOffsetText);
            Vector2 strHardPosition = new Vector2((int)xOffsetText, (int)yOffsetText + (stringSize.Y + 2));

            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, diffeasy, strEasyPosition, Color.White);
            spriteBatch.DrawString(spriteFont, diffmed, strMedPosition, Color.White);
            spriteBatch.DrawString(spriteFont, diffhard, strHardPosition, Color.White);


            String diffDesc = "";
            Color c = Color.White;
            switch (difficulty)
            {
                case GameDifficulty.easy:
                    diffDesc = "Most enemies die in 1 hit and do half as much damage. Point gain is doubled.";
                    c = Color.LightGreen;
                    spriteBatch.DrawString(spriteFont, diffeasy, strEasyPosition, Color.Yellow);
                    break;

                case GameDifficulty.medium:
                    diffDesc = "Enemies die in the normal amount of hits and do regular damage. Point gain is normal.";
                    c = Color.LightYellow;
                    spriteBatch.DrawString(spriteFont, diffmed, strMedPosition, Color.Yellow);
                    break;

                case GameDifficulty.hard:
                    diffDesc = "Enemies are tougher and more dangerous. Point gain is slow.";
                    c = Color.Red;
                    spriteBatch.DrawString(spriteFont, diffhard, strHardPosition, Color.Yellow);
                    break;
            }

            spriteBatch.DrawString(spriteFont, "Difficulty: ", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Difficulty: ").X / 2), strCenter.Y), Color.White);

            xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString(diffDesc).X / 2);
            spriteBatch.DrawString(spriteFont, diffDesc, new Vector2(xOffsetText, viewportSize.Y - (strCenter.Y + 5) * 2), c);

            xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString(menuInstructions).X/2);
            spriteBatch.DrawString(spriteFont, menuInstructions, new Vector2(xOffsetText, viewportSize.Y - (strCenter.Y + 5)), Color.DarkGray);

            spriteBatch.End();
        }

        private void drawControlScreen()
        {
            spriteBatch.Begin();

            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 strCenter;

            graphics.GraphicsDevice.Clear(Color.Black);

            Vector2 stringSize =
                spriteFont.MeasureString(menuPlay);
            strCenter = new Vector2(stringSize.X, stringSize.Y);

            yOffsetText = (viewportSize.Y / 2 - strCenter.Y);

            String str = "WASD: Lateral Movement";
            xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString(str).X / 2);

            switch(controlScreenNumber)
            {
                case 0:
                    spriteBatch.DrawString(spriteFont, str, new Vector2(xOffsetText, yOffsetText - strCenter.Y * 3), Color.White);
                    spriteBatch.DrawString(spriteFont, "Arrow Keys: Rotation", new Vector2(xOffsetText, yOffsetText - strCenter.Y * 2), Color.White);
                    spriteBatch.DrawString(spriteFont, "LShift + LControl: Thrust", new Vector2(xOffsetText, yOffsetText - strCenter.Y), Color.White);
                    spriteBatch.DrawString(spriteFont, "Space: Shoot", new Vector2(xOffsetText, yOffsetText), Color.White);
                    spriteBatch.DrawString(spriteFont, "M: Toggle Sounds", new Vector2(xOffsetText, yOffsetText + strCenter.Y), Color.White);
                    spriteBatch.DrawString(spriteFont, "ESC: Access Upgrade Menu", new Vector2(xOffsetText, yOffsetText + strCenter.Y*2), Color.White);

                    spriteBatch.DrawString(spriteFont, "Controls: ", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Controls: ").X / 2), strCenter.Y), Color.White);
                    break;

                case 1:
                    xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString("Upgrades can improve every facet of your ship, however they cost points").X / 2);

                    spriteBatch.DrawString(spriteFont, "Playing: ", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Controls: ").X / 2), strCenter.Y), Color.White);

                    String output = "Score by destroying enemies";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y * 3), Color.White);
                    output = "The number of points each enemy is worth increases each minute";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y), Color.White);
                    output = "but so does the rate they spawn. To survive, try upgrading.";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText), Color.White);
                    output = "Upgrades improve your ship but cost you points. Good luck!";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y), Color.White);

                    break;
                case 2:
                    spriteBatch.DrawString(spriteFont, "Death: ", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Controls: ").X / 2), strCenter.Y), Color.White);
                    output = "Eventually you will die.";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y * 3), Color.White);
                    output = "You die when your hull strength (red bar) is reduced to 0";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y), Color.White);
                    output = "but to damage your health they have to get through your shields (blue bar)";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText), Color.White);
                    output = "shields recharge, health doesn't.";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y), Color.White);
                    break;
                case 3:
                    spriteBatch.DrawString(spriteFont, "Enemies: ", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Controls: ").X / 2), strCenter.Y), Color.White);
                    output = "There are different types of enemies";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y * 3), Color.White);
                    output = "Mines:";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText - strCenter.Y), Color.White);
                    output = "Will be draw towards you and collide with you if";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText), Color.White);
                    output = "you get too close. Beware when they turn red!";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y), Color.White);


                    output = "Orbiters:";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y*3), Color.White);
                    output = "Will circle you and fire, try and keep up with them";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y*4), Color.White);
                    output = "as they move. That is when they are most vulnerable.";
                    spriteBatch.DrawString(spriteFont, output, new Vector2((viewportSize.X / 2 - spriteFont.MeasureString(output).X / 2), yOffsetText + strCenter.Y*5), Color.White);
                    break;

            }

            xOffsetText = (viewportSize.X / 2 - spriteFont.MeasureString(menuInstructions).X/2);
            spriteBatch.DrawString(spriteFont, "Arrow Keys: Change Page", new Vector2((viewportSize.X / 2 - spriteFont.MeasureString("Arrow Keys: Change Page").X / 2), viewportSize.Y - (strCenter.Y + 5)*3), Color.DarkGray);
            spriteBatch.DrawString(spriteFont, "Enter: Return to title", new Vector2(xOffsetText, viewportSize.Y - (strCenter.Y + 5)), Color.DarkGray);

            

            spriteBatch.End();
        }

        private void drawUpgradeMenu()
        {
            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 strCenter;

            graphics.GraphicsDevice.Clear(Color.Black);

            Vector2 stringSize =
                spriteFont.MeasureString("upgrade,       current level:  /  (pts)");
            strCenter = new Vector2(stringSize.X / 2, stringSize.Y / 2);

            xOffsetText = (viewportSize.X / 2 - strCenter.X);

            spriteBatch.Begin();

            for (int i = 0; i <= 5; i++)
            {
                
                Stat s = PlayerStats.statList[upgradeMenuTop + i];

                String cost = "";

                if(s.MaxLevel > s.CurrentLevel)
                {
                    cost = " (" + s.getNextLevelCost() + " pts)";
                }

                String write = "upgrade " + s.Name + ", current level: " + s.CurrentLevel + "/" + s.MaxLevel + cost;
                yOffsetText = (viewportSize.Y / 2 - (stringSize.Y * 3) + ((stringSize.Y + 2) * i));
                Color c = Color.White;
                if (upgradeMenuSelection == i)
                {
                    if (s.getNextLevelCost() > score)
                    {
                        c = Color.DarkRed;
                    }
                    else if (s.CurrentLevel == s.MaxLevel)
                    {
                        c = Color.DarkGoldenrod;
                    }
                    else
                    {
                        c = Color.Green;
                    }
                }
                else if (s.getNextLevelCost() > score)
                {
                    c = Color.Gray;
                }
                else if (s.CurrentLevel == s.MaxLevel)
                {
                    c = Color.Goldenrod;
                }

                spriteBatch.DrawString(spriteFont, write, new Vector2(xOffsetText, yOffsetText), c);
            }

            String str = "[more]";
            if (upgradeMenuTop > 0)
            {
                spriteBatch.DrawString(spriteFont, str, new Vector2(xOffsetText, (viewportSize.Y / 2 - (stringSize.Y * 3) - ((stringSize.Y + 2)))), Color.Gray);
            }
            if (upgradeMenuTop + 6 < PlayerStats.statList.Count())
            {
                spriteBatch.DrawString(spriteFont, str, new Vector2(xOffsetText, (viewportSize.Y / 2 - (stringSize.Y * 3) + ((stringSize.Y + 2) * 6))), Color.Gray);
            }

            str = "Upgrades:";
            spriteBatch.DrawString(spriteFont, str, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(str).X / 2), stringSize.Y + 2), Color.White);

            str = "ESC: Return to game, Q: End Game";
            spriteBatch.DrawString(spriteFont, str, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(str).X / 2), viewportSize.Y - (stringSize.Y + 2)), Color.DarkGray);

            str = "Current Score: " + score + "pts";
            spriteBatch.DrawString(spriteFont, str, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(str).X / 2), stringSize.Y * 2 + 4), Color.DarkGray);

            spriteBatch.End();
        }

        private void drawGameOver()
        {
            float xOffsetText, yOffsetText;
            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);

            graphics.GraphicsDevice.Clear(Color.Black);

            Vector2 stringSize =
                spriteFont.MeasureString("You Died");
            Vector2 strCenter = new Vector2(stringSize.X / 2, stringSize.Y / 2);

            yOffsetText = (viewportSize.Y / 2);
            xOffsetText = (viewportSize.X / 2);

            String youDied = "You Died. Final Score: " + score;
            String instructions = "Enter: Play Again";
            String instructions2 = "Esc: Quit";

            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, youDied, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(youDied).X / 2), stringSize.Y + 2), Color.Red);
            spriteBatch.DrawString(spriteFont, instructions, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(instructions).X / 2), yOffsetText - stringSize.Y), Color.Gray);
            spriteBatch.DrawString(spriteFont, instructions2, new Vector2(viewportSize.X / 2 - (spriteFont.MeasureString(instructions2).X / 2), yOffsetText), Color.Gray);
            spriteBatch.End();
        }

        #endregion drawing region

        #region Collision detection
        //Taken from FuelCell
        /// <summary>
        /// Calculates a merged boundng 
        /// </summary>
        /// <returns>The merged bounding sphere of a given model</returns>
        protected BoundingSphere CalculateBoundingSphere(Model model)
        {
            BoundingSphere mergedSphere = new BoundingSphere();
            BoundingSphere[] boundingSpheres;
            int index = 0;
            int meshCount = model.Meshes.Count;

            boundingSpheres = new BoundingSphere[meshCount];
            foreach (ModelMesh mesh in model.Meshes)
            {
                boundingSpheres[index++] = mesh.BoundingSphere;
            }

            mergedSphere = boundingSpheres[0];
            if ((model.Meshes.Count) > 1)
            {
                index = 1;
                do
                {
                    mergedSphere = BoundingSphere.CreateMerged(mergedSphere,
                        boundingSpheres[index]);
                    index++;
                } while (index < model.Meshes.Count);
            }
            mergedSphere.Center.Y = 0;
            return mergedSphere;
        }

        public bool collides(GameObject objectA, GameObject objectB)
        {
            BoundingSphere A = objectA.InstanceBoundingSphere;
            BoundingSphere B = objectB.InstanceBoundingSphere;
            {
                if(A.Intersects(B))
                {
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
        }

        /// <summary>
        /// Returns a list of objects the given collider collides with, from a specified larger list
        /// </summary>
        /// <param name="collider">the object to collide with</param>
        /// <param name="objects">the list of objects to collide</param>
        /// <returns></returns>
        public List<MovingGameObject> listCollides(MovingGameObject collider, List<MovingGameObject> objects)
        {
            List<MovingGameObject> collisionList = new List<MovingGameObject>();
            foreach(MovingGameObject thing in objects)
            {
                if (collides(thing, collider))
                {
                    collisionList.Add(thing);
                }
            }
            collisionList.Remove(collider);//It will always be colliding with itself.
            return collisionList;
        }

        /// <summary>
        /// Extension of listCollides to check the enemy list only.
        /// </summary>
        public List<Enemy> listCollides(Enemy collider, List<Enemy> objects)
        {
            List<Enemy> collisionList = new List<Enemy>();
            foreach (Enemy thing in objects)
            {
                if (collides(thing, collider))
                {
                    collisionList.Add(thing);
                }
            }
            collisionList.Remove(collider);//It will always be colliding with itself.
            return collisionList;
        }

        /// <summary>
        /// Finds the nearest object in a list of objects to a given object. Used in collision detection to see which object another
        /// is most likely to be colliding with if it is colliding with 2 at a time.
        /// </summary>
        /// <param name="collider">The collider object</param>
        /// <param name="collidingObjects">The list of objects it could be colliding with</param>
        /// <returns>The closest colliding object</returns>
        public MovingGameObject findNearest(MovingGameObject collider, List<MovingGameObject> collidingObjects)
        {
            if (collidingObjects.Count() < 1)
            {
                return null;
            }
            else if (collidingObjects.Count == 1)
            {
                return collidingObjects[0];
            }
            else
            {
                MovingGameObject o = null;
                float minDist = float.MaxValue;
                foreach(MovingGameObject thing in collidingObjects)
                {
                    float dist = Vector3.Distance(thing.Position, collider.Position);
                    if (dist < minDist)
                    {
                        o = thing;
                        minDist = dist;
                    }
                }
                return o;
            }
        }

        #endregion

        #region list management methods
        /// <summary>
        /// Adds an object to the list of GameObjects (used by the drawing code)
        /// </summary>
        /// <param name="thing">The object to add to the list</param>
        public void addObject(GameObject thing)
        {
            objects.Add(thing);
        }

        /// <summary>
        /// Adds an object to the list of game objects with an update() method. Adds to a list that
        /// is later assigned to be the current list so as to prevent problems that could be caused by adding
        /// or removing while the list is still being iterated over.
        /// </summary>
        /// <param name="thing">The object to add to the list</param>
        public void addUpdatingObject(MovingGameObject movingThing)
        {
            nextUpdateableObjects.Add(movingThing);
        }

        /// <summary>
        /// Removes an object to the list of GameObjects (used by the drawing code)
        /// </summary>
        /// <param name="thing">The object to remove from the list</param>
        public void removeObject(GameObject thing)
        {
            objects.Remove(thing);
        }

        /// <summary>
        /// Removes an object from the list of next updating objects to prevent problems caused
        /// by altering the current updating objects while they are still being iterated over
        /// </summary>
        /// <param name="movingThing"></param>
        public void removeUpdatingObject(MovingGameObject movingThing)
        {
            nextUpdateableObjects.Remove(movingThing);
        }

        /// <summary>
        /// Adds an enemy to the list of enemies
        /// </summary>
        /// <param name="e">The enemy to add to the list</param>
        public void addEnemy(Enemy e)
        {
            enemies.Add(e);
        }

        /// <summary>
        /// Removes an enemy from the list of enemies
        /// </summary>
        /// <param name="e">The enemy to remove from the list</param>
        public void enemyDestroyed(Enemy e)
        {
            enemies.Remove(e);
        }
        #endregion
    }
}