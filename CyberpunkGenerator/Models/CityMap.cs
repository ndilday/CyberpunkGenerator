using CyberpunkGenerator.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkGenerator.Models
{
    public class CityMap
    {
        // The infinite grid. Keys are coordinate tuples (x, y).
        private readonly Dictionary<(int x, int y), CityBlock> _grid = new();
        private int _nextBlockId = 1;

        public IReadOnlyCollection<CityBlock> AllBlocks => _grid.Values;

        // Retrieves a block at the exact coordinates, or null if empty space
        public CityBlock GetBlockAt(int x, int y)
        {
            _grid.TryGetValue((x, y), out var block);
            return block;
        }

        // Creates and registers a new block on the grid
        public CityBlock CreateBlock(int x, int y, BlockType type)
        {
            if (_grid.ContainsKey((x, y)))
                throw new InvalidOperationException($"A block already exists at ({x}, {y}).");

            var newBlock = new CityBlock(_nextBlockId++, type, x, y);
            _grid[(x, y)] = newBlock;
            return newBlock;
        }

        // Calculates Manhattan Distance (the grid-based walking distance)
        public int GetDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        public int GetDistance(CityBlock a, CityBlock b)
        {
            return GetDistance(a.X, a.Y, b.X, b.Y);
        }

        // Useful for finding a place to build next to an existing block
        public IEnumerable<(int x, int y)> GetAdjacentEmptyCoordinates(int x, int y)
        {
            var directions = new (int dx, int dy)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };

            foreach (var (dx, dy) in directions)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                if (!_grid.ContainsKey((checkX, checkY)))
                {
                    yield return (checkX, checkY);
                }
            }
        }
    }
}