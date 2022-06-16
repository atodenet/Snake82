using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;


namespace Snake82
{
    class SnakeHero : Snake
    {
        private int growstep;   // 成長から成長までに何マス進んだか
        private int growturn;   // 成長から成長までに何回曲がったか
        public SnakeHero()
        {
            headcolor = Color.White;
            bodycolor = Color.AliceBlue;
        }
        public new void Init(int startx, int starty)
        {
            base.Init(startx, starty);
            // 基底クラスで設定した値を上書き
            direction = 0; dirlast = 0; // 上向き
            dirratio = INTEGRAL_RANGE;
            growstep = 0;
            growturn = 0;
        }
        public new bool Update(Game1 g, int[,] map)
        {
            bool moved = base.Update(g, map);
            if (moved)
            {
                growstep++;
                if(direction != dirlast)
                {
                    growturn++;
                }
            }
            return moved;
        }


        // 成長する 敵を倒したり、アイテムを獲得したり
        private void Grow(int height)
        {
            AddBody();
            SetRainbow();
            int spup = 1;
            if (growstep <= height)
            {
                Debug.WriteLine("Growstep Speedup");
                spup++;
            }
            if(growturn <= 3)
            {
                Debug.WriteLine("Growturn Speedup");
                spup++;
            }
            Debug.WriteLine("Speedup "+spup);
            SpeedUp(spup);
            growstep = 0;
            growturn = 0;
        }
        // return: 1 アイテムを取った 2 敵を倒した
        public int CheckHit(int[,] map)
        {
            int rc = 0;
            int hitchip = GetHit(map);
            if (hitchip == (int)Chip.Item)
            {
                Grow(map.GetLength(1));
                rc = 1;
            }
            else if (hitchip == (int)Chip.Snake)
            {   // 頭が自分の体に衝突した
                SetDeath();
            }
            else if (hitchip == (int)Chip.Enemy)
            {   // 頭が敵に衝突した
                if( 0< rainbowcowntdown)
                {   // 無敵モード 敵を倒す
                    rc = 2;
                }
                else
                {
                    SetDeath();
                }
            }
            return rc;
        }
    }
}
