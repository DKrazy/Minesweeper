using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Minesweeper
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        SpriteFont _font;

        bool clicked = false;
        bool keyPressed = false;

        static int windowWidth = 1200;
        static int windowHeight = 800;

        Texture2D[] _tiles = new Texture2D[12];

        static int[] tileLengths = { 65, 35, 35 };
        int tileLength;

        static int[] gridWidths = { 9, 16, 30 };
        static int[] gridHeights = { 9, 16, 16 };
        int gridWidth, gridHeight;

        static int[] mineCounts = { 10, 40, 99 };
        int numberOfMines;

        MouseState mState;
        Point mouseGridPosition;

        KeyboardState kState;

        // Game variables

        int gameState = PLAYING;
        int gameDifficulty = 1;

        int[,] grid;
        int[,] tileStates;

        bool gridInitialized = false;

        // Constants

        const int UNCOVERED = 8;
        const int MINE = 9;
        const int COVERED = 10;
        const int FLAGGED = 11;

        const int PLAYING = 0;
        const int WIN = 1;
        const int LOSE = 2;

        readonly string[] MESSAGES = { "", "YOU WIN", "YOU LOSE" };
        readonly int[] MESSAGE_WIDTHS = { 0, 141, 167 };

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = windowWidth;
            _graphics.PreferredBackBufferHeight = windowHeight;
        }

        protected override void Initialize()
        {
            ResetGrid();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _font = Content.Load<SpriteFont>("Font");

            _tiles[0] = Content.Load<Texture2D>("Tiles/Empty");

            for (int i = 1; i <= 8; i++)
            {
                _tiles[i] = Content.Load<Texture2D>($"Tiles/{i}");
            }

            _tiles[9] = Content.Load<Texture2D>("Tiles/Mine");
            _tiles[10] = Content.Load<Texture2D>("Tiles/Covered");
            _tiles[11] = Content.Load<Texture2D>("Tiles/Flag");

            // Used this code just to find the values for MESSAGE_WIDTHS. Just keeping it around for archival purposes.
            /*foreach (string m in MESSAGES)
            {
                int width = 0;

                foreach (char c in m)
                {
                    width += (int)_font.GetGlyphs()[c].Width;
                }

                Debug.WriteLine(width);
            }*/
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            mState = Mouse.GetState();
            kState = Keyboard.GetState();

            mouseGridPosition = MousedOverTile(mState.Position);

            if (!clicked && MouseIsInGrid(mState.Position) && gameState == PLAYING)
            {
                int x = mouseGridPosition.X;
                int y = mouseGridPosition.Y;

                if (mState.LeftButton == ButtonState.Pressed && tileStates[x, y] != FLAGGED)
                {
                    tileStates[x, y] = UNCOVERED;

                    if (!gridInitialized) InitializeGrid(new Point(x, y));

                    if (grid[x, y] == 0) Zerospread(new Point(x, y));
                    if (grid[x, y] == MINE) GameLose();

                    if (gameState != LOSE && CheckWin()) gameState = WIN;
                }

                if (mState.RightButton == ButtonState.Pressed && (tileStates[x, y] == COVERED || tileStates[x, y] == FLAGGED))
                {
                    tileStates[x, y] = tileStates[x, y] == COVERED ? FLAGGED : COVERED;
                }
            }

            if (!keyPressed)
            {
                if (kState.IsKeyDown(Keys.R)) ResetGrid();

                if (kState.IsKeyDown(Keys.D1)) { gameDifficulty = 0; ResetGrid(); }
                if (kState.IsKeyDown(Keys.D2)) { gameDifficulty = 1; ResetGrid(); }
                if (kState.IsKeyDown(Keys.D3)) { gameDifficulty = 2; ResetGrid(); }
            }

            if (mState.LeftButton == ButtonState.Released && mState.RightButton == ButtonState.Released) clicked = false;
            else clicked = true;

            if (kState.GetPressedKeys().Length > 0) keyPressed = true;
            else keyPressed = false;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            DrawGrid();

            // Debug text
            /*_spriteBatch.DrawString(_font, mState.Position.ToString(), new Vector2(30, 30), Color.White);
            _spriteBatch.DrawString(_font, mouseGridPosition.ToString(), new Vector2(30, 60), Color.White);
            _spriteBatch.DrawString(_font, MouseIsInGrid(mState.Position).ToString(), new Vector2(30, 90), Color.White);
            _spriteBatch.DrawString(_font, gameState.ToString(), new Vector2(30, 120), Color.White);
            _spriteBatch.DrawString(_font, (new Point(gridWidth, gridHeight)).ToString(), new Vector2(30, 150), Color.White);*/

            _spriteBatch.DrawString(_font, MESSAGES[gameState], new Vector2((windowWidth - MESSAGE_WIDTHS[gameState]) / 2, 30), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        void ResetGrid()
        {
            gridInitialized = false;

            tileLength = tileLengths[gameDifficulty];
            gridWidth = gridWidths[gameDifficulty];
            gridHeight = gridHeights[gameDifficulty];
            numberOfMines = mineCounts[gameDifficulty];

            grid = new int[gridWidth, gridHeight];
            tileStates = new int[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = 0;
                    tileStates[x, y] = COVERED;
                }
            }

            gameState = PLAYING;
        }

        void InitializeGrid(Point mGridPos)
        {
            Random rand = new Random();

            for (int i = 0; i < numberOfMines; i++)
            {
                int x, y;

                do
                {
                    x = (int)(rand.NextSingle() * gridWidth);
                    y = (int)(rand.NextSingle() * gridHeight);
                } while (grid[x, y] == MINE || mGridPos.Equals(new Point(x, y)));

                grid[x, y] = MINE;
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != MINE) grid[x, y] = NumberOfMineNeighbours(x, y);
                }
            }

            gridInitialized = true;
        }

        void DrawGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    int tileX = (windowWidth - gridWidth * tileLength) / 2 + x * tileLength;
                    int tileY = (windowHeight - gridHeight * tileLength) / 2 + y * tileLength;

                    int textureID = tileStates[x, y] == UNCOVERED ? grid[x, y] : tileStates[x, y];
                    _spriteBatch.Draw(_tiles[textureID], new Rectangle(tileX, tileY, tileLength, tileLength), Color.White);
                }
            }
        }

        bool MouseIsInGrid(Point mPos)
        {
            return !(mPos.X < (windowWidth - gridWidth * tileLength) / 2
                || mPos.X > (windowWidth + gridWidth * tileLength) / 2
                || mPos.Y < (windowHeight - gridHeight * tileLength) / 2
                || mPos.Y > (windowHeight + gridHeight * tileLength) / 2);
        }

        Point MousedOverTile(Point mPos)
        {
            int x = (mPos.X - (windowWidth - gridWidth*tileLength)/2) / tileLength;
            int y = (mPos.Y - (windowHeight - gridHeight*tileLength)/2) / tileLength;

            return new Point(x, y);
        }

        // Terrible, horrible, no-good, very bad code. Do I care? Hell no, I'm making Minesweeper, not GTA VI.
        int NumberOfMineNeighbours(int x, int y)
        {
            int mines = 0;

            for (int dx = x > 0 ? -1 : 0; dx <= (x < gridWidth - 1 ? 1 : 0); dx++)
            {
                for (int dy = y > 0 ? -1 : 0; dy <= (y < gridHeight - 1 ? 1 : 0); dy++)
                {
                    mines += grid[x + dx, y + dy] == MINE ? 1 : 0;
                }
            }

            return mines;
        }

        void GameLose()
        {
            // Uncovering all tiles
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    tileStates[x, y] = UNCOVERED;
                }
            }

            gameState = LOSE;
        }

        void Zerospread(Point seed)
        {
            List<Point> seeds = new List<Point>() { seed };

            do
            {
                List<Point> newSeeds = new List<Point>();

                foreach (Point s in seeds)
                {
                    // Terrible, horrible, no-good, very bad code copied over from NumberOfMineNeighbours, and then made even worse. Oh well.
                    for (int dx = s.X > 0 ? -1 : 0; dx <= (s.X < gridWidth - 1 ? 1 : 0); dx++)
                    {
                        for (int dy = s.Y > 0 ? -1 : 0; dy <= (s.Y < gridHeight - 1 ? 1 : 0); dy++)
                        {
                            int x = s.X + dx;
                            int y = s.Y + dy;

                            if (grid[x, y] <= 8 && tileStates[x, y] == COVERED)
                            {
                                tileStates[x, y] = UNCOVERED;

                                if (grid[x, y] == 0) newSeeds.Add(new Point(x, y));
                            }
                        }
                    }
                }

                seeds = newSeeds;
            } while (seeds.Count > 0);
        }

        bool CheckWin()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (tileStates[x, y] != UNCOVERED && grid[x, y] != MINE) return false;
                }
            }

            return true;
        }
    }
}