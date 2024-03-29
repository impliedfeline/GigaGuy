﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GigaGuy
{
    class Level
    {
        public Player Player { get; private set; } // Does the player have to be a property?

        private Tile[,] tileMap;
        private Camera camera;

        private const int gridCellWidth = 32;
        private const int gridCellHeight = 32;

        private string levelPath;

        public Level(string levelPath)
        {
            this.levelPath = levelPath;
            tileMap = new Tile[100, 100];

        }

        /// <summary>
        /// Loads textures to game objects and creates tilemap.
        /// Reads a .txt file and assigns the rows as strings in an array.
        /// The strings are then read as char-arrays and the tiles are given texture and position based on the value of the char and the position in the string array.
        /// These tiles are then added on the tileMap-list.
        /// </summary>
        public void LoadContent(ContentManager Content)
        {
            string[] levelInText = File.ReadAllLines(levelPath);
            Texture2D tileTexture = Content.Load<Texture2D>("Tiles/tile");

            Texture2D _45RSTexture = Content.Load<Texture2D>("Tiles/Slopes/45RightSlope");
            Texture2D _45LSTexture = Content.Load<Texture2D>("Tiles/Slopes/45LeftSlope");

            Texture2D _2251RSTexture = Content.Load<Texture2D>("Tiles/Slopes/2251RightSlope");
            Texture2D _2252RSTexture = Content.Load<Texture2D>("Tiles/Slopes/2252RightSlope");
            Texture2D _2251LSTexture = Content.Load<Texture2D>("Tiles/Slopes/2251LeftSlope");
            Texture2D _2252LSTexture = Content.Load<Texture2D>("Tiles/Slopes/2252LeftSlope");

            Texture2D _11251RSTexture = Content.Load<Texture2D>("Tiles/Slopes/11251RightSlope");
            Texture2D _11252RSTexture = Content.Load<Texture2D>("Tiles/Slopes/11252RightSlope");
            Texture2D _11253RSTexture = Content.Load<Texture2D>("Tiles/Slopes/11253RightSlope");
            Texture2D _11254RSTexture = Content.Load<Texture2D>("Tiles/Slopes/11254RightSlope");
            Texture2D _11251LSTexture = Content.Load<Texture2D>("Tiles/Slopes/11251LeftSlope");
            Texture2D _11252LSTexture = Content.Load<Texture2D>("Tiles/Slopes/11252LeftSlope");
            Texture2D _11253LSTexture = Content.Load<Texture2D>("Tiles/Slopes/11253LeftSlope");
            Texture2D _11254LSTexture = Content.Load<Texture2D>("Tiles/Slopes/11254LeftSlope");

            string row;

            for (int i = 0; i < levelInText.Length; i++)
            {
                row = levelInText[i];

                for (int j = 0; j < row.Length; j++)
                {
                    Tile tile = null;
                    RectangleF hitbox = new RectangleF(j * gridCellWidth, i * gridCellHeight, gridCellWidth, gridCellHeight);

                    if (row[j].Equals('T'))
                        tile = new Tile(tileTexture, hitbox);
                    else if (row[j].Equals('R'))
                        tile = new Slope(_45RSTexture, hitbox, SlopeType._45R);
                    else if (row[j].Equals('L'))
                        tile = new Slope(_45LSTexture, hitbox, SlopeType._45L);
                    else if (row[j].Equals('s'))
                        tile = new Slope(_2251RSTexture, hitbox, SlopeType._2251R);
                    else if (row[j].Equals('l'))
                        tile = new Slope(_2252RSTexture, hitbox, SlopeType._2252R);
                    else if (row[j].Equals('o'))
                        tile = new Slope(_2252LSTexture, hitbox, SlopeType._2252L);
                    else if (row[j].Equals('p'))
                        tile = new Slope(_2251LSTexture, hitbox, SlopeType._2251L);
                    else if (row[j].Equals('1'))
                        tile = new Slope(_11251RSTexture, hitbox, SlopeType._11251R);
                    else if (row[j].Equals('2'))
                        tile = new Slope(_11252RSTexture, hitbox, SlopeType._11252R);
                    else if (row[j].Equals('3'))
                        tile = new Slope(_11253RSTexture, hitbox, SlopeType._11253R);
                    else if (row[j].Equals('4'))
                        tile = new Slope(_11254RSTexture, hitbox, SlopeType._11254R);
                    else if (row[j].Equals('!'))
                        tile = new Slope(_11254LSTexture, hitbox, SlopeType._11254L);
                    else if (row[j].Equals('@'))
                        tile = new Slope(_11253LSTexture, hitbox, SlopeType._11253L);
                    else if (row[j].Equals('#'))
                        tile = new Slope(_11252LSTexture, hitbox, SlopeType._11252L);
                    else if (row[j].Equals('$'))
                        tile = new Slope(_11251LSTexture, hitbox, SlopeType._11251L);
                    else
                        continue;
                    tileMap[i, j] = tile;
                }
            }
            Player = new Player();
            Player.LoadContent(Content);
            camera = new Camera();
        }

        public void Update(GameTime gameTime)
        {
            Player.Update(gameTime);
            HandleCollisions();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 offSet = camera.CalculateOffSet(Player);
            foreach (Tile tile in tileMap)
            {
                if (tile == null)
                    continue;
                tile.Draw(spriteBatch, offSet);
            }
            Player.Draw(spriteBatch, offSet);
        }

        /// <summary>
        /// Collision in GigaGuy(WorkingTitle) works by having collision points around the player character:
        /// The amount and position of these points is calculated based on the shape and size of the player collision hitbox.
        /// There are currently three types of points: side points, end points and dedicated slope points for checking for collision with slopes.
        /// If any of the points enter a cell in the tilemap occupied by a tile, a corresponding action depending on what kind of point entered the cell will be performed.
        /// The points also have a hierarchy, where the collision logic is performed in order with the slopes first, sides second and ends last.
        /// NOTE: The characters maximum terminal velocity can't exceed the height of the center points,
        /// otherwise their logic will be performed with the would-be floor tiles, causing the player to tunnel through the tilemap.
        /// </summary>
        private void HandleCollisions()
        {
            float sideOffSet;
            float sideOffSetDefault = 3;
            float endOffSet;
            float endOffSetDefault = 7;
            int tileColumn;
            int tileRow;
            int tilesWide = (int)Math.Ceiling(Player.Hitbox.Width / gridCellWidth);
            int tilesHigh = (int)Math.Ceiling(Player.Hitbox.Height / gridCellHeight);
            Tile[] collidingSlopeTiles = new Tile[3];
            Tile[] collidingSideTiles = new Tile[tilesHigh * 2]; // The maximum amount of tiles a character of given height and width can collide with.
            Tile[] collidingEndTiles = new Tile[(1 + tilesWide) * 2];

            // Slopes
            tileColumn = FindTileIndex(Player.Hitbox.Center.X, true);
            tileRow = FindTileIndex(Player.Hitbox.Center.Y, false);
            collidingSlopeTiles[0] = CheckForCollision(tileRow, tileColumn);
            tileRow = FindTileIndex(Player.Hitbox.Bottom - 2, false);
            collidingSlopeTiles[1] = CheckForCollision(tileRow, tileColumn);
            tileRow = FindTileIndex(Player.Hitbox.Bottom, false);
            collidingSlopeTiles[2] = CheckForCollision(tileRow, tileColumn);

            Player.IsOnSlope = false;

            foreach (Tile tile in collidingSlopeTiles)
            {
                if (tile != null && tile is Slope)
                {
                    Player.IsOnSlope = true;
                    OnSlopeCollision(tile);
                }
            }

            // NOTE: The collision points have a slight offset, with all points being closer to the center.
            // This allows for higher max speed on the y-axis as the player is less likely to tunnel through the tilemap.
            // The end points also have an offset, but this is due to the collision otherwise favoring the end points excessively when moving up 1 tile high "steps"

            // Sides
            for (float i = 0; i <= tilesHigh; i += 2)
            {
                if ((1 + i) / (tilesHigh * 2) < 0.5f)
                    sideOffSet = 0;
                else if ((1 + i) / (tilesHigh * 2) > 0.5f)
                    sideOffSet = -sideOffSetDefault;
                else
                    sideOffSet = -sideOffSetDefault; // Compromise for 1x1 characters
                tileRow = FindTileIndex(Player.Position.Y + ((1 + i) / (tilesHigh * 2)) * Player.Hitbox.Height + sideOffSet, false);
                for (float j = 0; j <= 1; j++)
                {
                    tileColumn = FindTileIndex(Player.Position.X + j * Player.Hitbox.Width, true);
                    for (int k = 0; k < collidingSideTiles.Length; k++) // Maybe make a method for this
                    {
                        if (collidingSideTiles[k] == null)
                        {
                            collidingSideTiles[k] = CheckForCollision(tileRow, tileColumn);
                            break;
                        }
                    }
                }
            }
            CheckArrayForCollidingTiles(collidingSideTiles, true);

            // Ends
            for (float i = 0; i <= tilesWide; i++)
            {
                if ((i / tilesWide) < 0.5f)
                    endOffSet = endOffSetDefault;
                else if ((i / tilesWide) > 0.5f)
                    endOffSet = -endOffSetDefault;
                else
                    endOffSet = 0;

                tileColumn = FindTileIndex(Player.Position.X + endOffSet + (i / tilesWide) * Player.Hitbox.Width, true);
                for (float j = 0; j <= 1; j++)
                {
                    tileRow = FindTileIndex(Player.Position.Y + j * Player.Hitbox.Height, false);
                    for (int k = 0; k < collidingEndTiles.Length; k++)
                    {
                        if (collidingEndTiles[k] == null)
                        {
                            collidingEndTiles[k] = CheckForCollision(tileRow, tileColumn);
                            break;
                        }
                    }
                }
            }
            CheckArrayForCollidingTiles(collidingEndTiles, false);
        }

        // Checks whether the tileMap has a tile with the exact position specified. Returns tile if one is found.
        private Tile CheckForCollision(int tileRow, int tileColumn)
        {
            if (tileMap[tileRow, tileColumn] != null && tileMap[tileRow, tileColumn].Hitbox.Intersects(Player.Hitbox))
            {
                return tileMap[tileRow, tileColumn];
            }
            return null;
        }

        // Gives the X and Y coordinate of the tile occupied by the collision point.
        private int FindTileIndex(float coordinate, bool isX)
        {
            if (isX)
                return (int)coordinate / gridCellWidth;
            else
                return (int)coordinate / gridCellHeight;
        }

        // Performs collision with tile with a logic specified with the isSide bool (true for sides, false for ends).
        // The player is then assigned a new position and velocity.
        private void OnCollision(Tile tile, bool isSide)
        {
            Vector2 playerNewPosition;
            Vector2 playerNewVelocity;
            if (isSide)
            {
                Player.IsOnWall = true;
                if (Player.Hitbox.X < tile.Hitbox.X)
                {
                    playerNewPosition = new Vector2(tile.Hitbox.Left - Player.Hitbox.Width, Player.Position.Y);
                    playerNewVelocity = new Vector2(0, Player.Velocity.Y);
                    Player.IsOnRightWall = true;
                }
                else
                {
                    playerNewPosition = new Vector2(tile.Hitbox.Right, Player.Position.Y);
                    playerNewVelocity = new Vector2(0, Player.Velocity.Y);
                    Player.IsOnRightWall = false;
                }
            }
            else
            {
                if (Player.Hitbox.Y < tile.Hitbox.Y)
                {
                    // If the player is standing on a slope, ignore the bottom end points as they are used for the "floor" tiles.
                    // Otherwise, movement on slopes will appear "bumpy."
                    if (Player.IsOnSlope)
                        return;
                    playerNewPosition = new Vector2(Player.Position.X, tile.Hitbox.Top - Player.Hitbox.Height);
                    playerNewVelocity = new Vector2(Player.Velocity.X, 0);
                    Player.IsOnGround = true;
                    if (Player.IsArc)
                        Player.IsJumping = false;
                }
                else
                {
                    playerNewPosition = new Vector2(Player.Position.X, tile.Hitbox.Bottom);
                    playerNewVelocity = new Vector2(Player.Velocity.X, 0);
                    Player.IsJumping = false;
                }
            }
            Player.Position = playerNewPosition;
            Player.Velocity = playerNewVelocity;
        }

        private void CheckArrayForCollidingTiles(Array collidingTiles, bool isSide)
        {
            foreach (Tile tile in collidingTiles)
            {
                if (tile != null && !(tile is Slope))
                {
                    OnCollision(tile, isSide);
                }
            }
        }

        // Separate collision logic used with slopes.
        // The slopes height at the center of the player is calculated based on the player's position on the tile.
        // If the player is lower than the slope height, collision logic is performed.
        // NOTE: The player is handed the type of the slope as separate logic has to performed for changing Y position based on the X velocity on the player.
        private void OnSlopeCollision(Tile tile)
        {
            float slopeHeight = 0;
            Slope slope = (Slope)tile;
            Player.SlopeType = slope.SlopeType;
            // 45 Degrees
            if (slope.SlopeType == SlopeType._45R)
                slopeHeight = Player.Hitbox.Center.X - slope.Hitbox.X;
            else if (slope.SlopeType == SlopeType._45L)
                slopeHeight = slope.Hitbox.Height - (Player.Hitbox.Center.X - slope.Hitbox.X);
            // 22.5 Degrees
            else if (slope.SlopeType == SlopeType._2251R)
                slopeHeight = (Player.Hitbox.Center.X - slope.Hitbox.X) / 2;
            else if (slope.SlopeType == SlopeType._2252R)
                slopeHeight = (slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 2;
            else if (slope.SlopeType == SlopeType._2251L)
                slopeHeight = slope.Hitbox.Height - (slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 2;
            else if (slope.SlopeType == SlopeType._2252L)
                slopeHeight = slope.Hitbox.Height - (Player.Hitbox.Center.X - slope.Hitbox.X) / 2;
            // 11.25 Degrees
            else if (slope.SlopeType == SlopeType._11251R)
                slopeHeight = (Player.Hitbox.Center.X - slope.Hitbox.X) / 4;
            else if (slope.SlopeType == SlopeType._11252R)
                slopeHeight = (slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 4;
            else if (slope.SlopeType == SlopeType._11253R)
                slopeHeight = (Player.Hitbox.Center.X - slope.Hitbox.X) / 4 + slope.Hitbox.Height / 2;
            else if (slope.SlopeType == SlopeType._11254R)
                slopeHeight = (slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 4 + slope.Hitbox.Height / 2;
            else if (slope.SlopeType == SlopeType._11251L)
                slopeHeight = slope.Hitbox.Height - ((slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 4 + slope.Hitbox.Height / 2);
            else if (slope.SlopeType == SlopeType._11252L)
                slopeHeight = slope.Hitbox.Height - ((Player.Hitbox.Center.X - slope.Hitbox.X) / 4 + slope.Hitbox.Height / 2);
            else if (slope.SlopeType == SlopeType._11253L)
                slopeHeight = slope.Hitbox.Height - (slope.Hitbox.Height + Player.Hitbox.Center.X - slope.Hitbox.X) / 4;
            else if (slope.SlopeType == SlopeType._11254L)
                slopeHeight = slope.Hitbox.Height - (Player.Hitbox.Center.X - slope.Hitbox.X) / 4;
            if (Player.Hitbox.Bottom > tile.Hitbox.Bottom - slopeHeight)
            {
                Player.Position = new Vector2(Player.Position.X, slope.Hitbox.Bottom - slopeHeight - Player.Hitbox.Height);
                Player.Velocity = new Vector2(Player.Velocity.X, 0);
                Player.IsOnGround = true;
            }
        }
    }
}