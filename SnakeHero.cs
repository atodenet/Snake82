using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        // 蛇が死んだあと普通は待機だが、自機は死体描画モードになる
        protected override void SetAfterDeath()
        {
            mode = (int)SnakeMode.Corpse;   // 自機は死んでも消えない、死体を表示する
        }


        // 成長する 敵を倒したり、アイテムを獲得したり
        private void Grow(int height)
        {
            AddBody();
            SetRainbow();
            // スピードアップ
            int spup = 0;   // 1;   // デフォルト値
            if (growstep <= height) // 移動距離が高さ未満なら1up
            {
                Debug.WriteLine("Growstep Speedup");
                spup++;
            }
            if(growturn <= 3)       // 方向転換が3回以下なら1up
            {
                Debug.WriteLine("Growturn Speedup");
                spup++;
            }
            Debug.WriteLine("Speedup "+spup);
            SpeedUp(spup);      // スピードアップ完了
            growstep = 0;       // 移動距離カウンタ
            growturn = 0;       // 方向転換回数カウンタ
        }

        // 頭の衝突判定と衝突時処理
        // return: 0変化なし 1 アイテムを取った 2 敵を倒した
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
                    Grow(map.GetLength(1));
                    rc = 2;
                }
                else
                {
                    SetDeath();
                }
            }
            return rc;
        }
        public void DrawStartup(Game1 g, int mapwidth, int mapheight, int uppadding, int ratio1, int range)
        {
            Color color = headcolor;
            Point pos = body[headno];
            Point startup;

            startup.X = pos.X;
            startup.Y = mapheight+1;    // 画面の高さはマップの高さ＋１（ミラー行があるので）
            Rectangle rect = g.scr.TextBoxBetween(pos.X, pos.Y + uppadding, startup.X, startup.Y + uppadding, ratio1, range);
            Vector2 sft = new Vector2(8f, 8f); // テクスチャ上の中心位置（画面上ではない）
            rect.X += rect.Width / 2;
            rect.Y += rect.Height / 2;
            float rot = GetHeadRotation();
            g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
        }
    }
}
