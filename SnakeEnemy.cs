using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snake82
{
    class SnakeEnemy : Snake
    {
        public SnakeEnemy()
        {
            headcolor = Color.LightPink;
            bodycolor = Color.Violet;
            chip = (int)Chip.Enemy;
            chiphead = (int)Chip.Enemy;

    }
}
}
