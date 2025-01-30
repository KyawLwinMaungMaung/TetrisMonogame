using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Tetris
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont font;

        private const int GridWidth = 10;
        private const int GridHeight = 20;
        private const int TileSize = 30;
        private int[,] grid;
        private Texture2D blockTexture;
        private Color[] blockColors = new Color[]
        {
            Color.Cyan, Color.Blue, Color.Orange, Color.Yellow, Color.Green, Color.Purple, Color.Red
        };

        private int[][,] tetrominoes = new int[][,]
        {
            new int[,] { { 1, 1, 1, 1 } },
            new int[,] { { 1, 1, 1 }, { 0, 1, 0 } },
            new int[,] { { 1, 1 }, { 1, 1 } },
            new int[,] { { 0, 1, 1 }, { 1, 1, 0 } },
            new int[,] { { 1, 1, 0 }, { 0, 1, 1 } },
            new int[,] { { 1, 0, 0 }, { 1, 1, 1 } },
            new int[,] { { 0, 0, 1 }, { 1, 1, 1 } }
        };

        private int[,] currentPiece;
        private int currentX, currentY;
        private int currentColor;
        private int[,] nextPiece;
        private int nextColor;

        private int score;
        private int level;
        private int linesCleared;
        private double fallSpeed;
        private double fallTimer;

        private bool isGameOver;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = GridWidth * TileSize + 200;
            _graphics.PreferredBackBufferHeight = GridHeight * TileSize;
            _graphics.ApplyChanges();

            grid = new int[GridHeight, GridWidth];
            score = 0;
            level = 1;
            linesCleared = 0;
            fallSpeed = 1.0;
            fallTimer = 0;
            isGameOver = false;

            nextPiece = GetRandomPiece();
            nextColor = new Random().Next(blockColors.Length);

            SpawnPiece();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            blockTexture = new Texture2D(GraphicsDevice, 1, 1);
            blockTexture.SetData(new Color[] { Color.White });
            font = Content.Load<SpriteFont>("Font");
        }

        protected override void Update(GameTime gameTime)
        {
            if (isGameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    Initialize();
                }
                return;
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            HandleInput();

            fallTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (fallTimer >= fallSpeed)
            {
                if (IsPieceAtBottom())
                {
                    MergePiece();
                    ClearLines();
                    SpawnPiece();
                }
                else
                {
                    currentY++;
                }
                fallTimer = 0;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            DrawGrid();
            DrawPiece();
            DrawUI();

            if (isGameOver)
            {
                DrawGameOver();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void SpawnPiece()
        {
            currentPiece = nextPiece;
            currentColor = nextColor;

            nextPiece = GetRandomPiece();
            nextColor = new Random().Next(blockColors.Length);

            currentX = GridWidth / 2 - currentPiece.GetLength(1) / 2;
            currentY = 0;

            if (IsCollision(currentX, currentY, currentPiece))
            {
                isGameOver = true;
            }
        }

        private int[,] GetRandomPiece()
        {
            Random random = new Random();
            return tetrominoes[random.Next(tetrominoes.Length)];
        }

        private void HandleInput()
        {
            KeyboardState state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.Left) && CanMove(-1, 0))
                currentX--;
            if (state.IsKeyDown(Keys.Right) && CanMove(1, 0))
                currentX++;
            if (state.IsKeyDown(Keys.Down) && CanMove(0, 1))
            {
                currentY++;
                score++;
            }
            if (state.IsKeyDown(Keys.Up))
                RotatePiece();
            if (state.IsKeyDown(Keys.Space))
                HardDrop();
        }

        private void HardDrop()
        {
            int dropDistance = 0;
            while (CanMove(0, 1))
            {
                currentY++;
                dropDistance++;
            }
            MergePiece();
            ClearLines();
            SpawnPiece();
            score += dropDistance * 2;
        }

        private bool CanMove(int offsetX, int offsetY)
        {
            return !IsCollision(currentX + offsetX, currentY + offsetY, currentPiece);
        }

        private bool IsCollision(int x, int y, int[,] piece)
        {
            for (int py = 0; py < piece.GetLength(0); py++)
            {
                for (int px = 0; px < piece.GetLength(1); px++)
                {
                    if (piece[py, px] != 0)
                    {
                        int newX = x + px;
                        int newY = y + py;

                        if (newX < 0 || newX >= GridWidth || newY >= GridHeight || (newY >= 0 && grid[newY, newX] != 0))
                            return true;
                    }
                }
            }
            return false;
        }

        private void RotatePiece()
        {
            int[,] rotated = new int[currentPiece.GetLength(1), currentPiece.GetLength(0)];
            for (int y = 0; y < currentPiece.GetLength(0); y++)
            {
                for (int x = 0; x < currentPiece.GetLength(1); x++)
                {
                    rotated[x, currentPiece.GetLength(0) - y - 1] = currentPiece[y, x];
                }
            }

            if (!IsCollision(currentX, currentY, rotated))
                currentPiece = rotated;
        }

        private bool IsPieceAtBottom()
        {
            return IsCollision(currentX, currentY + 1, currentPiece);
        }

        private void MergePiece()
        {
            for (int y = 0; y < currentPiece.GetLength(0); y++)
            {
                for (int x = 0; x < currentPiece.GetLength(1); x++)
                {
                    if (currentPiece[y, x] != 0)
                    {
                        grid[currentY + y, currentX + x] = currentColor + 1;
                    }
                }
            }
        }

        private void ClearLines()
        {
            int linesRemoved = 0;

            for (int y = 0; y < GridHeight; y++)
            {
                bool isLineFull = true;
                for (int x = 0; x < GridWidth; x++)
                {
                    if (grid[y, x] == 0)
                    {
                        isLineFull = false;
                        break;
                    }
                }

                if (isLineFull)
                {
                    for (int yy = y; yy > 0; yy--)
                    {
                        for (int x = 0; x < GridWidth; x++)
                        {
                            grid[yy, x] = grid[yy - 1, x];
                        }
                    }

                    for (int x = 0; x < GridWidth; x++)
                    {
                        grid[0, x] = 0;
                    }

                    linesRemoved++;
                }
            }

            if (linesRemoved > 0)
            {
                switch (linesRemoved)
                {
                    case 1: score += 100 * level; break;
                    case 2: score += 300 * level; break;
                    case 3: score += 500 * level; break;
                    case 4: score += 800 * level; break;
                }

                linesCleared += linesRemoved;
                if (linesCleared >= level * 5)
                {
                    level++;
                    fallSpeed = Math.Max(0.1, fallSpeed - 0.1);
                }
            }
        }

        private void DrawGrid()
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (grid[y, x] != 0)
                    {
                        _spriteBatch.Draw(blockTexture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), blockColors[grid[y, x] - 1]);
                    }
                }
            }
        }

        private void DrawPiece()
        {
            if (currentPiece == null)
            {
                throw new InvalidOperationException("currentPiece is null. Did you call SpawnPiece()?");
            }

            for (int y = 0; y < currentPiece.GetLength(0); y++)
            {
                for (int x = 0; x < currentPiece.GetLength(1); x++)
                {
                    if (currentPiece[y, x] != 0)
                    {
                        _spriteBatch.Draw(blockTexture, new Rectangle((currentX + x) * TileSize, (currentY + y) * TileSize, TileSize, TileSize), blockColors[currentColor]);
                    }
                }
            }
        }

        private void DrawUI()
        {
            _spriteBatch.DrawString(font, $"Score: {score}", new Vector2(GridWidth * TileSize + 20, 20), Color.White);
            _spriteBatch.DrawString(font, $"Level: {level}", new Vector2(GridWidth * TileSize + 20, 60), Color.White);
            _spriteBatch.DrawString(font, "Next Piece:", new Vector2(GridWidth * TileSize + 20, 120), Color.White);
            DrawPieceAt(nextPiece, nextColor, GridWidth * TileSize + 20, 160);
        }

        private void DrawPieceAt(int[,] piece, int color, int x, int y)
        {
            for (int py = 0; py < piece.GetLength(0); py++)
            {
                for (int px = 0; px < piece.GetLength(1); px++)
                {
                    if (piece[py, px] != 0)
                    {
                        _spriteBatch.Draw(blockTexture, new Rectangle(x + px * TileSize, y + py * TileSize, TileSize, TileSize), blockColors[color]);
                    }
                }
            }
        }

        private void DrawGameOver()
        {
            string gameOverText = "Game Over!";
            string restartText = "Press Enter to Restart";
            Vector2 textSize = font.MeasureString(gameOverText);
            Vector2 restartSize = font.MeasureString(restartText);

            _spriteBatch.DrawString(font, gameOverText, new Vector2((_graphics.PreferredBackBufferWidth - textSize.X) / 2, (_graphics.PreferredBackBufferHeight - textSize.Y) / 2), Color.Red);
            _spriteBatch.DrawString(font, restartText, new Vector2((_graphics.PreferredBackBufferWidth - restartSize.X) / 2, (_graphics.PreferredBackBufferHeight - restartSize.Y) / 2 + 40), Color.White);
        }
    }
}
