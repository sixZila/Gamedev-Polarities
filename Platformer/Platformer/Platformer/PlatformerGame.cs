#region File Description
//-----------------------------------------------------------------------------
// PlatformerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;


namespace Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {

        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private const int START_BUTTON = 0;
        private const int EXIT_BUTTON = 1;

        // Global content.
        private SpriteFont hudFont;
        private GameState gameState;

        private Texture2D startButton;
        private Texture2D startButtonHighlighted;
        private Texture2D exitButton;
        private Texture2D exitButtonHighlighted;
        private Texture2D whiteBackground; // For fade in fade out
        private Texture2D gameLogo;
        private bool nextLevel;
        private bool menuButtonPressed;

        private int menuState;

        private Vector2 center;
        private Vector2 upperCenter;
        private Vector2 lowerCenter;
        private Rectangle startButtonBounds;
        private Rectangle exitButtonBounds;

        private bool fadeIn;
        private bool fadeOut;
        private float alpha;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;
        
        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private const int numberOfLevels = 8;

        enum GameState
        {
            Menu = 0,
            InGame = 1,
        }

        public PlatformerGame()
        {
            //Set Window Title
            Window.Title = "Polarities: A Minimalist Platformer";

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 792;
            graphics.PreferredBackBufferHeight = 504;
            Content.RootDirectory = "Content";

#if WINDOWS_PHONE
            graphics.IsFullScreen = true;
            TargetElapsedTime = TimeSpan.FromTicks(333333);
#endif
            this.IsMouseVisible = true;
            gameState = GameState.Menu;

            Accelerometer.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            upperCenter = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         (titleSafeArea.Y + titleSafeArea.Height) / 6.0f);
            lowerCenter = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         (titleSafeArea.Y + titleSafeArea.Height) / 1.5f);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            //winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            //loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            //diedOverlay = Content.Load<Texture2D>("Overlays/you_died");
            startButton = Content.Load<Texture2D>("Sprites/Menu/StartButton");
            startButtonHighlighted = Content.Load<Texture2D>("Sprites/Menu/StartButtonHighlight");
            exitButton = Content.Load<Texture2D>("Sprites/Menu/ExitButton");
            exitButtonHighlighted = Content.Load<Texture2D>("Sprites/Menu/ExitButtonHighlight");
            whiteBackground = Content.Load<Texture2D>("Overlays/WhiteOverlay");
            gameLogo = Content.Load<Texture2D>("Sprites/Menu/Logo");

            startButtonBounds = new Rectangle((int)center.X - (startButton.Width / 2), (int)center.Y - (startButton.Height / 2), startButton.Width, startButton.Height);
            exitButtonBounds = new Rectangle((int)lowerCenter.X - (startButton.Width / 2), (int)lowerCenter.Y - (startButton.Height / 2), startButton.Width, startButton.Height);

            alpha = 1f;
            fadeIn = true;
            fadeOut = false;

            menuState = 0;

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away

            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            //Load Main Menu
            //LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (nextLevel && !fadeOut)
            {
                gameState = GameState.InGame;
                nextLevel = false;
                LoadNextLevel();
            }

            if (gameState == GameState.InGame)
            {
                HandleInput();
                // update our level, passing down the GameTime along with all of our input states
                level.Update(gameTime, keyboardState, gamePadState, touchState,
                         accelerometerState, Window.CurrentOrientation);
            }
            else
            {
                keyboardState = Keyboard.GetState();
                var mouseState = Mouse.GetState();
                var mousePosition = new Point(mouseState.X, mouseState.Y);

                if ((gamePadState.IsButtonDown(Buttons.DPadUp) || keyboardState.IsKeyDown(Keys.Up)))
                {
                    if (!menuButtonPressed)
                    {
                        menuState = menuState - 1 < 0 ? 1 : 0;
                        menuButtonPressed = true;
                    }
                }
                else if ((gamePadState.IsButtonDown(Buttons.DPadDown) || keyboardState.IsKeyDown(Keys.Down)))
                {
                    if (!menuButtonPressed)
                    {
                        menuState = (menuState + 1) % 2;
                        menuButtonPressed = true;
                    }
                }
                else
                {
                    menuButtonPressed = false;
                }

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (startButtonBounds.Contains(mousePosition))
                    {

                        fadeOut = true;
                        //LoadNextLevel();
                        nextLevel = true;
                    }
                    if (exitButtonBounds.Contains(mousePosition))
                    {
                        Exit();
                    }
                }

                if(gamePadState.IsButtonDown(Buttons.A) || keyboardState.IsKeyDown(Keys.Enter)) {
                    if (menuState == START_BUTTON)
                    {

                        fadeOut = true;
                        //LoadNextLevel();
                        nextLevel = true;
                    }
                    if (menuState == EXIT_BUTTON)
                    {
                        Exit();
                    }
                }
            }
    
            base.Update(gameTime);
        }


        private void HandleInput()
        {
            // get all of our input states
            keyboardState = Keyboard.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);
            touchState = TouchPanel.GetState();
            accelerometerState = Accelerometer.GetState();

            // Exit the game when back is pressed.
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                Exit();

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A) ||
                touchState.AnyTouch();

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive) {
                    level.StartNewLife();
                }
                //else if (level.TimeRemaining == TimeSpan.Zero)
                //{
                    if (level.ReachedExit)
                    {
                        //LoadNextLevel();
                        fadeOut = true;
                        nextLevel = true;
                    }
                        
                   // else
                       // ReloadCurrentLevel();
                //}
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);
            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin();
            if (gameState == GameState.InGame)
            {
                level.Draw(gameTime, spriteBatch);
            }
            else if (gameState == GameState.Menu)
            {
                Vector2 buttonSize = new Vector2(startButton.Width, startButton.Height);
                Vector2 logoSize = new Vector2(gameLogo.Width, gameLogo.Height);

                var mouseState = Mouse.GetState();
                var mousePosition = new Point(mouseState.X, mouseState.Y);

                spriteBatch.Draw(gameLogo, upperCenter - logoSize / 2, Color.White);

                if (exitButtonBounds.Contains(mousePosition) || menuState == EXIT_BUTTON)
                {
                    spriteBatch.Draw(exitButtonHighlighted, lowerCenter - buttonSize / 2, Color.White);
                    menuState = EXIT_BUTTON;
                }
                else
                    spriteBatch.Draw(exitButton, lowerCenter - buttonSize / 2, Color.White);

                if (startButtonBounds.Contains(mousePosition) || menuState == START_BUTTON)
                {
                    spriteBatch.Draw(startButtonHighlighted, center - buttonSize / 2, Color.White);
                    menuState = START_BUTTON;
                }
                else
                    spriteBatch.Draw(startButton, center - buttonSize / 2, Color.White);
            }

            if (fadeIn)
            {
                spriteBatch.Draw(whiteBackground, new Vector2(0, 0), Color.White * alpha);
                alpha -= 0.05f;

                if (alpha <= 0)
                {
                    fadeIn = false;
                }
            }
            if (fadeOut)
            {
                Vector2 vector = new Vector2(0, 0);
                spriteBatch.Draw(whiteBackground, new Vector2(0, 0), Color.White * alpha);
                alpha += 0.05f;

                if (alpha >= 1.0)
                {
                    fadeOut = false;
                    fadeIn = true;
                }
            }

            // spriteBatch.Draw(texture, position, Color.White * alpha);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
