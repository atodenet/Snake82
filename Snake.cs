﻿using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Snake82
{
    enum Chip
    {   // ゲームマップ上の記号値
        None = 0,
        Snake,
        SnakeHead,
        Enemy,
        Item,
    }
    enum SnakeMode
    {
        Active,
        Death,
        Standby,
    }

    // ヘビの基底クラス。ここから自機、敵を派生する。
    class Snake
    {
        protected const int INTEGRAL_RANGE = 1000;    // Speedを積み重ねてこのrangeに到達したら次のマスへ
        private const int BUF_ONCE = 100;
        private const int BUF_ONCE_ADD = 10;
        private const int DEATH_TIME = 120;         // 体パーツが死ぬ間隔（フレーム数）
        private const int RAINBOW_BLINK = 50;        // レインボウモードの色変わり間隔（フレーム数） 3の倍数
        private const int RAINBOW_TIME = 60;        // レインボウモードの持続時間

        public int length;          // 体の長さ
        protected bool addbody;     // 体を伸ばすリクエスト
        protected int speed;        // 前進速度 0 - INTEGRAL_RANGE (INTEGRAL_RANGEは1フレームで1マス進む速度） ratioの加算に使用する
        protected int headno;       // 位置バッファ中の頭の位置 必ず1以上
        protected int posratio;     // 前のマスから次のマスへの遷移率 INTEGRAL_RANGEに達したら1マス
        public int direction;       // 次の頭の進行方向(0,1,2,3)上が0 反時計回り
        protected int dirnow;       // 今の進行方向
        protected int dirlast;      // 今の頭の方向(0,1,2,3)
        protected int dirratio;     // 頭の方向の遷移率 0 - INTEGRAL_RANGE、INTEGRAL_RANGEに達したあとはrotだけで頭の向きは決まる
        protected int buflength;    // 体位置メモリバッファ長
        protected Position[] body;  // 体のマップ上位置
        protected Color headcolor = Color.Black;
        protected Color bodycolor = Color.Black;
        protected int chip = (int)Chip.Snake;
        protected int chiphead = (int)Chip.SnakeHead;
        public int mode;            // enum snakemode
        protected int deathcounddown;
        protected int deathspeed;
        protected int rainbowcowntdown;



        public void Init(int startx, int starty)
        {
            buflength = BUF_ONCE;
            body = new Position[buflength + BUF_ONCE_ADD];
            length = 1;
            headno = 1;
            posratio = 0;
            body[0].x = body[1].x = startx;
            body[0].y = body[1].y = starty;
            direction = 3; dirlast = 0;
            dirratio = 0;
            speed = 40;
            rainbowcowntdown = 0;

            mode = (int)SnakeMode.Active;
        }
        private int DirectoryToRotation(int dir,int dirnext)
        {   // 角度を返す 0 90 180 270
            switch (dir)
            {
                case 1:
                    return 90;
                case 2:
                    return 180;
                case 3:
                    return 270;
                default:
                    break;
            }
            // 0の場合は0を返すが、隣が3であれば360を返す
            if(dirnext == 3)
            {
                return 360;
            }
            return 0;

        }
        protected float GetHeadRotation()
        {
            int rot1 = DirectoryToRotation(direction, dirlast);
            int rot2 = DirectoryToRotation(dirlast, direction);
            int rot = (rot1 * dirratio + rot2 * (INTEGRAL_RANGE - dirratio)) / INTEGRAL_RANGE;
            //Debug.WriteLine(-rot);
            return MathHelper.ToRadians(-rot);
        }

        public void Draw(Game1 g,int mapwidth,int mapheight,int upadding)
        {   // 蛇の描画
            Rectangle rec;
            
            for (int no=length-1; 0<=no; no--)
            {   // 蛇の体を１個ずつ描画 頭は最後
                Position pos1 = body[headno - no];
                Position pos2 = body[headno - no-1];
                bool mirrorX = false;
                bool mirrorY = false;
                Color color = (no == 0 ? headcolor : bodycolor);

                // 色の設定
                if ( 0< rainbowcowntdown)
                {   // レインボウモード
                    int red = 255 - (((rainbowcowntdown+no*RAINBOW_BLINK/10) % RAINBOW_BLINK) * 256 *2/ RAINBOW_BLINK);
                    int green = 255 - (((rainbowcowntdown + no * RAINBOW_BLINK / 10 + RAINBOW_BLINK/3) % RAINBOW_BLINK) * 256*2 / RAINBOW_BLINK);
                    int blue = 255 - (((rainbowcowntdown + no * RAINBOW_BLINK / 10 + RAINBOW_BLINK*2 / 3) % RAINBOW_BLINK) * 256*2 / RAINBOW_BLINK);
                    if (red < 70)
                    {
                        red = 70;
                    }
                    if (green < 70)
                    {
                        green = 70;
                    }
                    if (blue < 70)
                    {
                        blue = 70;
                    }
                    color = new Color(red, green, blue);
                }
                if (mode == (int)SnakeMode.Death)
                {   // 死んだら頭から黒くなる
                    int deathno = length - (deathcounddown / deathspeed);
                    if (no <= deathno)
                    {
                        color = Color.Black;
                    }
                }

                // 画面の上下端、左右端はループさせるため、端にいる場合は下側あるいは右側に寄せる
                if (pos1.x <= 1 && (pos2.x <= 1 || mapwidth - 2 < pos2.x))
                {
                    pos1.x += mapwidth;
                    mirrorX = true;
                }
                if (pos2.x <= 1 && (pos1.x <= 1 || mapwidth - 2 < pos1.x))
                {
                    pos2.x += mapwidth;
                    mirrorX = true;
                }
                if (pos1.y <= 1 && (pos2.y <= 1 || mapheight - 2 < pos2.y))
                {
                    pos1.y += mapheight;
                    mirrorY = true;
                }
                if (pos2.y <= 1 && (pos1.y <= 1 || mapheight - 2 < pos1.y))
                {
                    pos2.y += mapheight;
                    mirrorY = true;
                }

                rec = g.scr.TextBoxBetween(pos1.x, pos1.y + upadding, pos2.x, pos2.y + upadding, posratio, INTEGRAL_RANGE);

                if (no == 0)
                {   // 先頭はhead描画 頭だけ方向がある
                    Vector2 sft = new Vector2(8f, 8f); // テクスチャ上の中心位置（画面上ではない）
                    rec.X += rec.Width / 2;
                    rec.Y += rec.Height / 2;
                    float rot = GetHeadRotation();
                    g.spriteBatch.Draw(g.fonts, rec, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);

                    if (mirrorX)
                    {
                        rec = g.scr.TextBoxBetween( pos1.x - mapwidth, pos1.y + upadding,
                                                    pos2.x - mapwidth, pos2.y + upadding, posratio, INTEGRAL_RANGE);
                        rec.X += rec.Width / 2;
                        rec.Y += rec.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                    if (mirrorY)
                    {
                        rec = g.scr.TextBoxBetween( pos1.x, pos1.y-mapheight + upadding, 
                                                    pos2.x, pos2.y-mapheight + upadding, posratio, INTEGRAL_RANGE);
                        rec.X += rec.Width / 2;
                        rec.Y += rec.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                    if( mirrorX && mirrorY)
                    {
                        rec = g.scr.TextBoxBetween( pos1.x - mapwidth, pos1.y - mapheight + upadding, 
                                                    pos2.x - mapwidth, pos2.y - mapheight + upadding, posratio, INTEGRAL_RANGE);
                        rec.X += rec.Width / 2;
                        rec.Y += rec.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                }
                else
                {   // 体を描画
                    g.spriteBatch.Draw(g.fonts, rec, g.Font((char)5), color);
                    if (mirrorX)
                    {
                        rec = g.scr.TextBoxBetween(pos1.x - mapwidth, pos1.y + upadding,
                                                    pos2.x - mapwidth, pos2.y + upadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)5), color);
                    }
                    if (mirrorY)
                    {
                        rec = g.scr.TextBoxBetween(pos1.x, pos1.y - mapheight + upadding,
                                                    pos2.x, pos2.y - mapheight + upadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)5), color);
                    }
                    if (mirrorX && mirrorY)
                    {
                        rec = g.scr.TextBoxBetween(pos1.x - mapwidth, pos1.y - mapheight + upadding,
                                                    pos2.x - mapwidth, pos2.y - mapheight + upadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rec, g.Font((char)5), color);
                    }
                }

            }
        }

        // 頭が一マス進んだらtrueを返す
        public bool Update(Game1 g,int[,] map)
        {   // ゲームの進行
            bool rc = false;

            switch(mode)
            {
                case (int)SnakeMode.Active:
                    // 前に進む
                    posratio += speed;
                    if (INTEGRAL_RANGE <= posratio)
                    {
                        posratio = 0;
                        GoNext(g, map);
                        rc = true;
                    }
                    // 頭を回す
                    if (dirratio < INTEGRAL_RANGE)
                    {
                        dirratio += speed;
                        if (INTEGRAL_RANGE <= dirratio)
                        {
                            dirratio = INTEGRAL_RANGE;
                            dirlast = direction;
                        }
                    }
                    // 無敵モード
                    if (0 < rainbowcowntdown)
                    {
                        rainbowcowntdown--;
                    }
                    break;
                case (int)SnakeMode.Death:
                    // 死んだアニメーション
                    if( --deathcounddown <= -30)
                    {   // マイナスは最後の余韻
                        deathcounddown = 0;
                        mode = (int)SnakeMode.Standby;
                    }
                    break;
            }
            return rc;
        }
        protected void GoNext(Game1 g, int[,] map)
        {   // 次のマスへ移動する処理
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int x;
            int y;

            if (addbody)
            {   // 体を伸ばすのは、次のマスへ移動するときのみ
                addbody = false;
                length++;
                if ((buflength - length) < (BUF_ONCE / 2))
                {   // ボディバッファのサイズが大分窮屈になったので拡張
                    Position[] newbody = new Position[buflength + BUF_ONCE + BUF_ONCE_ADD];
                    for(int i=0; i<buflength; i++)
                    {
                        newbody[i] = body[i];
                    }
                    buflength += BUF_ONCE;
                    body = newbody; // バッファの差し替え
                }
            }
            headno++;  // 頭のバッファ位置を１マス進める
            if( buflength <= headno)
            {   // バッファ終点まで進んだので始点側へデータ移動
                int moves = headno - length;
                for(int i=0; i<length; i++)
                {
                    body[i] = body[i + moves];
                }
                headno -= moves;
            }
            // 頭のマップ位置を決める
            x = body[headno - 1].x;
            y = body[headno - 1].y;
            switch (direction)
            {
                case 0: // 上へ
                    if( --y < 0)
                    {
                        y = height - 1;
                    }
                    break;
                case 1: // 左へ
                    if( --x < 0)
                    {
                        x = width - 1;
                    }
                    break;
                case 2: // 下へ
                    if( height <= ++y)
                    {
                        y = 0;
                    }
                    break;
                case 3: // 右へ
                    if( width <= ++x)
                    {
                        x = 0;
                    }
                    break;
            }
            dirnow = direction; // 今回のマス進行時の頭の方向を覚えておく（逆方向移動防止用、回転アニメーション用）
            body[headno].x = x;
            body[headno].y = y;
            // map判定（衝突など）するならこの後
        }
        public void AddBody()
        {
            addbody = true;
        }
        // 頭の向きを指定する
        // 戻り値 設定後の新しい向き / 変更できなかった場合は-1
        public int SetDirection(int dir)
        {   // エラーチェック
            if (dir<0 || 3<dir)
            {
                return -1;
            }
            switch (dirnow) {   // 現在の進行方向と、反対側を指定することはできない
                case 0:
                    if (dir == 2)
                    {
                        return -1;
                    }
                    break;
                case 1:
                    if (dir == 3)
                    {
                        return -1;
                    }
                    break;
                case 2:
                    if (dir == 0)
                    {
                        return -1;
                    }
                    break;
                case 3:
                    if (dir == 1)
                    {
                        return -1;
                    }
                    break;
            }
            dirlast = dirnow;   // 頭の回転アニメーション用のパラメータ
            direction = dir;    // 頭の新しい向き
            dirratio = 0;
            return direction;
        }

        // ゲームマップに自身をプロットする（画面描画ではない）
        public void Plot(int[,] map)
        {
            // 死んだ蛇はmapに置かない
            if (mode == (int)SnakeMode.Active)
            {   
                int bodyno = headno;
                Position pos = body[bodyno--];

                // 最初に頭をプロット（体で上書きするため）
                map[pos.x, pos.y] = chiphead;

                for (int i = 1; i < length; i++)
                {   // 体をプロット （i=0は頭）
                    pos = body[bodyno--];
                    map[pos.x, pos.y] = chip;
                }
            }
        }

        // 頭の位置にあるchipを返す。EnemyやSnakeなら、頭が衝突している。
        public int GetHit(int[,] map)
        {   
            return map[body[headno].x, body[headno].y];
        }
        public Position GetHead()
        {
            return body[headno];
        }

        // 死んだ場合のステータス変更
        public void SetDeath()
        {
            mode = (int)SnakeMode.Death;
            if(length < DEATH_TIME/10)
            {
                deathspeed = 10;
            }
            else
            {
                deathspeed = DEATH_TIME / length;
                if(deathspeed <= 0)
                {
                    deathspeed = 1;
                }
            }
            deathcounddown = (length+1) * deathspeed;
        }

        // レインボーモードにする
        public void SetRainbow()
        {
            rainbowcowntdown = RAINBOW_TIME;
        }
        public int SpeedUp(int spd)
        {
            speed += spd;
            return speed;
        }
    }
}