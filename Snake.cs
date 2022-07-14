using Atode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Snake82
{
    enum SnakeMode
    {
        Active,
        Death,      // 死にアニメーション中
        Standby,    // 待機状態、使われない
        Corpse,     // 死体を表示
    }

    // ヘビの基底クラス。ここから自機、敵を派生する。
    class Snake
    {
        protected const int INTEGRAL_RANGE = 1000;    // Speedを積み重ねてこのrangeに到達したら次のマスへ
        private const int BUF_ONCE = 100;
        private const int BUF_ONCE_ADD = 10;
        private const int DEATH_TIME = 120;         // 体パーツが死ぬアニメーション時間（フレーム数）
        private const int DEATH_TIME_PIKA = 12;     // 死ぬ瞬間の感電表現時間
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
        protected Point[] body;     // 体のマップ上位置
        protected Color headcolor = Color.Black;
        protected Color bodycolor = Color.Black;
        protected int growstep;   // 成長から成長までに何マス進んだか
        protected int growturn;   // 成長から成長までに何回曲がったか

        protected int objno;        // ゲームマップ上に配置する番号
        public SnakeMode modenow;            // esnakemode
        protected int deathCounddown;
        protected int deathPika;
        protected int deathspeed;
        protected int rainbowCowntdown;
        protected bool IsRainbow() { return 0 < rainbowCowntdown; }
        protected bool IsDeathPika() { return 0 < deathPika; }

        public Snake(int mapobjectno)
        {
            objno = mapobjectno;
        }

        public void Init(Point startPos)
        {
            buflength = BUF_ONCE;
            body = new Point[buflength + BUF_ONCE_ADD];
            Reborn(startPos);
        }
        public void Reborn(Point startPos)
        {
            length = 1;
            headno = 1;
            posratio = 0;
            body[0] = body[1] = startPos;
            direction = 3; dirlast = 0;
            dirratio = 0;
            speed = 40;
            rainbowCowntdown = 0;
            growstep = 0;
            growturn = 0;
            modenow = SnakeMode.Active;
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

        // 蛇を画面に描画する
        public void Draw(Game1 g,int mapwidth,int mapheight,int uppadding)
        {   
            Rectangle rect;
            
            for (int no=length-1; 0<=no; no--)
            {   // 蛇の体を１個ずつ描画 頭は最後
                Point pos1 = body[headno - no];
                Point pos2 = body[headno - no-1];
                bool mirrorX = false;
                bool mirrorY = false;
                Color color = (no == 0 ? headcolor : bodycolor);

                // 色の設定
                if ( IsRainbow() )
                {   // レインボウモード
                    int red = 255 - (((rainbowCowntdown+no*RAINBOW_BLINK/10) % RAINBOW_BLINK) * 256 *2/ RAINBOW_BLINK);
                    int green = 255 - (((rainbowCowntdown + no * RAINBOW_BLINK / 10 + RAINBOW_BLINK/3) % RAINBOW_BLINK) * 256*2 / RAINBOW_BLINK);
                    int blue = 255 - (((rainbowCowntdown + no * RAINBOW_BLINK / 10 + RAINBOW_BLINK*2 / 3) % RAINBOW_BLINK) * 256*2 / RAINBOW_BLINK);
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
                if (modenow == SnakeMode.Death || modenow == SnakeMode.Corpse)
                {   
                    if( IsDeathPika() )
                    {   // 感電中は体は黒い
                        if(deathPika % 6 < 3)
                        {
                            color = Color.Black;
                        }
                        else
                        {
                            color = Color.Goldenrod;
                        }
                    }
                    else
                    {
                        // 死んだら頭から黒くなる
                        int deathno = length - (deathCounddown / deathspeed);
                        if (no <= deathno)
                        {
                            color = Color.Black;
                        }
                    }
                }

                // 画面の上下端、左右端はループさせるため、端にいる場合は下側あるいは右側に寄せる
                if (pos1.X == 0 || (pos1.X==1 && pos2.X ==0))
                {
                    pos1.X += mapwidth;
                    mirrorX = true;
                }
                if (pos2.X == 0 || (pos2.X == 1 && mirrorX))
                {
                    pos2.X += mapwidth;
                    mirrorX = true;
                }
                if (pos1.Y ==0 || (pos1.Y == 1 && pos2.Y==0))
                {
                    pos1.Y += mapheight;
                    mirrorY = true;
                }
                if (pos2.Y ==0 ||(pos2.Y == 1 && mirrorY))
                {
                    pos2.Y += mapheight;
                    mirrorY = true;
                }

                rect = g.scr.TextBoxBetween(pos1.X, pos1.Y + uppadding, pos2.X, pos2.Y + uppadding, posratio, INTEGRAL_RANGE);

                if (IsDeathPika())
                {   // 感電ビリビリの瞬間、背景は黄色
                    Rectangle wall = rect;
                    Primitive.FillRectangle(g.spriteBatch, wall, Color.Yellow);
                    if (mirrorX)
                    {
                        wall = g.scr.TextBoxBetween(pos1.X - mapwidth, pos1.Y + uppadding,
                                                    pos2.X - mapwidth, pos2.Y + uppadding, posratio, INTEGRAL_RANGE);
                        Primitive.FillRectangle(g.spriteBatch, wall, Color.Yellow);
                    }
                    if (mirrorY)
                    {
                        wall = g.scr.TextBoxBetween(pos1.X, pos1.Y - mapheight + uppadding,
                                                    pos2.X, pos2.Y - mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        Primitive.FillRectangle(g.spriteBatch, wall, Color.Yellow);
                    }
                    if (mirrorX && mirrorY)
                    {
                        wall = g.scr.TextBoxBetween(pos1.X - mapwidth, pos1.Y - mapheight + uppadding,
                                                    pos2.X - mapwidth, pos2.Y - mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        Primitive.FillRectangle(g.spriteBatch, wall, Color.Yellow);
                    }
                }

                if (no == 0)
                {   // 先頭はhead描画 頭だけ方向がある
                    Vector2 sft = new Vector2(8f, 8f); // テクスチャ上の中心位置（画面上ではない）
                    rect.X += rect.Width / 2;
                    rect.Y += rect.Height / 2;
                    float rot = GetHeadRotation();

                    g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);

                    if (mirrorX)
                    {
                        rect = g.scr.TextBoxBetween( pos1.X - mapwidth, pos1.Y + uppadding,
                                                    pos2.X - mapwidth, pos2.Y + uppadding, posratio, INTEGRAL_RANGE);
                        rect.X += rect.Width / 2;
                        rect.Y += rect.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                    if (mirrorY)
                    {
                        rect = g.scr.TextBoxBetween( pos1.X, pos1.Y-mapheight + uppadding, 
                                                    pos2.X, pos2.Y-mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        rect.X += rect.Width / 2;
                        rect.Y += rect.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                    if( mirrorX && mirrorY)
                    {
                        rect = g.scr.TextBoxBetween( pos1.X - mapwidth, pos1.Y - mapheight + uppadding, 
                                                    pos2.X - mapwidth, pos2.Y - mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        rect.X += rect.Width / 2;
                        rect.Y += rect.Height / 2;
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)4), color, rot, sft, SpriteEffects.None, 0f);
                    }
                }
                else
                {   // 体を描画
                    g.spriteBatch.Draw(g.fonts, rect, g.Font((char)5), color);
                    if (mirrorX)
                    {
                        rect = g.scr.TextBoxBetween(pos1.X - mapwidth, pos1.Y + uppadding,
                                                    pos2.X - mapwidth, pos2.Y + uppadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)5), color);
                    }
                    if (mirrorY)
                    {
                        rect = g.scr.TextBoxBetween(pos1.X, pos1.Y - mapheight + uppadding,
                                                    pos2.X, pos2.Y - mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)5), color);
                    }
                    if (mirrorX && mirrorY)
                    {
                        rect = g.scr.TextBoxBetween(pos1.X - mapwidth, pos1.Y - mapheight + uppadding,
                                                    pos2.X - mapwidth, pos2.Y - mapheight + uppadding, posratio, INTEGRAL_RANGE);
                        g.spriteBatch.Draw(g.fonts, rect, g.Font((char)5), color);
                    }
                }

            }
        }

        // 静止した世界でも次のマスまでは進める
        public bool UpdateStopWorld(Game1 g, CollisionMap map)
        {   // ゲームの進行
            bool rc = false;

            switch (modenow)
            {
                case SnakeMode.Active:
                    // 前に進む
                    posratio += Speed(g);
                    if (INTEGRAL_RANGE <= posratio)
                    {
                        posratio = INTEGRAL_RANGE;
                    }
                    // 頭を回す
                    if (dirratio < INTEGRAL_RANGE)
                    {
                        dirratio += Speed(g);
                        if (INTEGRAL_RANGE <= dirratio)
                        {
                            dirratio = INTEGRAL_RANGE;
                            dirlast = direction;
                        }
                    }
                    // 無敵モード
                    if (0 < rainbowCowntdown)
                    {
                        rainbowCowntdown--;
                    }
                    break;
            }
            return rc;
        }

        // 頭が次のマスへ進んだらtrueを返す
        public bool Update(Game1 g,CollisionMap map)
        {   // ゲームの進行
            bool rc = false;

            switch(modenow)
            {
                case SnakeMode.Active:
                    // 前に進む
                    posratio += Speed(g);
                    if (INTEGRAL_RANGE <= posratio)
                    {   // 次のマスへ進む
                        posratio = 0;
                        GoNext(g, map);
                        // マスを進んだ場合、のちのちの難易度自動調整のために記録をつける
                        growstep++; // 進んだ距離
                        if (direction != dirlast)
                        {   // 回頭中であれば
                            growturn++; // 曲がった回数
                        }
                        rc = true;
                    }
                    // 頭を回す
                    if (dirratio < INTEGRAL_RANGE)
                    {
                        dirratio += Speed(g);
                        if (INTEGRAL_RANGE <= dirratio)
                        {   // 回頭終了
                            dirratio = INTEGRAL_RANGE;
                            dirlast = direction;
                        }
                    }
                    // 無敵モード
                    if (0 < rainbowCowntdown)
                    {
                        rainbowCowntdown--;
                    }
                    break;
                case SnakeMode.Death:
                    // 死んだアニメーション
                    if(0 < deathPika)
                    {
                        deathPika--;
                    }
                    else
                    {
                        if (--deathCounddown <= -30)
                        {   // マイナスは最後の余韻
                            deathCounddown = 0;
                            SetAfterDeath();
                        }
                    }
                    break;
            }
            return rc;
        }

        // 死にアニメが終わった蛇は待機状態になる（何にも使われない）
        protected virtual void SetAfterDeath()
        {
            modenow = SnakeMode.Standby;
        }
        protected void GoNext(Game1 g, CollisionMap map)
        {   // 次のマスへ移動する処理

            // 体を伸ばす
            if (addbody)
            {   // 体を伸ばすのは、次のマスへ移動するときのみ
                addbody = false;
                length++;
                if ((buflength - length) < (BUF_ONCE / 2))
                {   // ボディバッファのサイズが大分窮屈になったので拡張
                    Point[] newbody = new Point[buflength + BUF_ONCE + BUF_ONCE_ADD];
                    for(int i=0; i<buflength; i++)
                    {
                        newbody[i] = body[i];
                    }
                    buflength += BUF_ONCE;
                    body = newbody; // バッファの差し替え
                }
            }

            // 頭を次のマスへ移動する
            headno++;  // 頭のバッファ位置を１マス進める
            // バッファサイズの管理
            if( buflength <= headno)
            {   // バッファ終点まで進んだので始点側へデータ移動
                int moves = headno - length;
                for(int i=0; i<length; i++)
                {
                    body[i] = body[i + moves];
                }
                headno -= moves;
            }
            // 移動後の頭のマップ位置をセット
            body[headno] = GetNextPoint(map, body[headno - 1],direction);
            // 今回のマス進行時の頭の方向を覚えておく（逆方向移動防止用、回転アニメーション用）
            dirnow = direction; 
            // map判定（衝突など）するならこの後
        }

        // 指定地点から、指定された方向に1マス進んだ地点を返す
        public Point GetNextPoint(CollisionMap map,Point po,int direc)
        {
            switch (direc)
            {
                case 0: // 上へ
                    if (--po.Y < 0)
                    {
                        po.Y = map.mapheight() - 1;
                    }
                    break;
                case 1: // 左へ
                    if (--po.X < 0)
                    {
                        po.X = map.mapwidth() - 1;
                    }
                    break;
                case 2: // 下へ
                    if (map.mapheight() <= ++po.Y)
                    {
                        po.Y = 0;
                    }
                    break;
                case 3: // 右へ
                    if (map.mapwidth() <= ++po.X)
                    {
                        po.X = 0;
                    }
                    break;
            }
            return po;
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
        // 注意！ 蛇のMapObjectは頭と体で２個連続で登録されていること。（LogicalMapに対して）
        public void Plot(CollisionMap map)
        {
            // 死んだ蛇はmapに置かない
            if (modenow == SnakeMode.Active)
            {
                int headobj;
                int bodyobj;
                int bodyno = headno;
                Point pos = body[bodyno--];
                Point lastpos;

                headobj = objno;
                bodyobj = objno + 1;    // LogicalMapに1蛇あたり2オブジェクト登録されていることが前提

                // 最初に頭をプロット（体で上書きするため）
                map.Plot(headobj, pos);

                // 体をプロット （i=0は頭）
                for (int i = 1; i < length; i++)
                {
                    pos = body[bodyno--];
                    map.Plot(bodyobj, pos);
                }
                // 頭から最後端まではプロットし終わった。

                // まだ次のマスへ十分移行していなければ、最後端のさらに一つ先をプロットする
                // 見た目ではまだそこに蛇がいるから、当たり判定も必要
                if(posratio*2 < INTEGRAL_RANGE)
                {
                    // 自機は、一番最初は0と1が同じ位置になっている。その時だけは頭を上書きしない。
                    lastpos = body[bodyno];
                    if (lastpos != pos)
                    {
                        map.Plot(bodyobj, lastpos);
                    }
                }
            }
        }

        // 頭の位置にあるchipを返す。EnemyやSnakeなら、頭が衝突している。
        public MapObject GetHit(CollisionMap map)
        {
            return map.GetHit(body[headno]);
        }
        public Point GetHeadPoint()
        {
            return body[headno];
        }

        // 死んだ場合のステータス変更
        public void SetDeath(bool pika)
        {
            if(modenow != SnakeMode.Death)
            {
                modenow = SnakeMode.Death;
                deathspeed = DEATH_TIME / length;
                if (deathspeed > 8)
                {
                    deathspeed = 8;
                }
                else if (deathspeed <= 0)
                {
                    deathspeed = 1;
                }
                deathCounddown = (length + 1) * deathspeed;

                if (pika)
                {
                    deathPika = DEATH_TIME_PIKA;
                }
            }
        }

        // レインボーモードにする
        public void SetRainbow()
        {
            rainbowCowntdown = RAINBOW_TIME;
        }
        public int SpeedUp(int spd)
        {
            speed += spd;
            return speed;
        }
        protected int Speed(Game1 g)
        {
            return speed * g.speedRate / Game1.SPEED_DEFAULT;
        }

        // 画面（ゲームマップ）のサイズ拡張に対応して、体の位置を調節する
        // 体の長さは伸ばさない。新しく生まれたマップ位置に中間パーツを挿入し、その分最後端は一個前へ進む
        public void ExpandMap(int xadd)
        {
            int cnt;
            int bodyno;
            int copyno;

            // 右側への画面拡張に対応する
            bodyno = headno;    // 頭から始まって番号は減っていく 頭が最も大きい番号
            for (cnt = 0; cnt < length; cnt++,bodyno--)
            {
                // 隣なのにXが2以上離れていれば左右端を跨いだと判断
                if(2<= Math.Abs(body[bodyno].X - body[bodyno - 1].X))
                {   // bodynoとbodyno-1の間に一個挿入する
                    // まず最後尾へ１個ずつずらす(後ろから順番にコピー）
                    copyno = headno - length + 1; // 最後尾のno
                    for(; copyno <= bodyno-1; copyno++)
                    {
                        body[copyno - 1] = body[copyno];
                    }
                    // 新しい中間パーツのX座標は拡張後右端なので+1
                    body[bodyno - 1].X = Math.Max(body[bodyno].X, body[bodyno - 1].X) + 1;
                    // 全体1個後ろずらししたので、このチェックルーチンも一つ進める
                    cnt++;
                    bodyno--;
                }
            }

            // 下側への画面拡張に対応する
            bodyno = headno;    // 頭から始まって番号は減っていく 頭が最も大きい番号
            for (cnt = 0; cnt < length; cnt++, bodyno--)
            {
                // 隣なのにYが2以上離れていれば上下端を跨いだと判断
                if (2 <= Math.Abs(body[bodyno].Y - body[bodyno - 1].Y))
                {   // bodynoとbodyno-1の間に一個挿入する
                    // まず最後尾へ１個ずつずらす(後ろから順番にコピー）
                    copyno = headno - length + 1; // 最後尾のno
                    for (; copyno <= bodyno - 1; copyno++)
                    {
                        body[copyno - 1] = body[copyno];
                    }
                    // 新しい中間パーツのX座標は拡張後右端なので+1
                    body[bodyno - 1].Y = Math.Max(body[bodyno].Y, body[bodyno - 1].Y) + 1;
                    // 全体1個後ろずらししたので、このチェックルーチンも一つ進める
                    cnt++;
                    bodyno--;
                }
            }

            if (0<xadd) {
                // 左側への画面拡張に対応する
                // まず、全パーツを1ずつ右へずらす
                bodyno = headno;
                for (int i = 0; i <= length; i++, bodyno--)
                {
                    body[bodyno].X += xadd;
                }
                bodyno = headno;    // 頭から始まって番号は減っていく 頭が最も大きい番号
                for (cnt = 0; cnt < length; cnt++, bodyno--)
                {
                    // 隣なのにXが2以上離れていれば左右端を跨いだと判断
                    if (2 <= Math.Abs(body[bodyno].X - body[bodyno - 1].X))
                    {   // bodynoとbodyno-1の間に一個挿入する
                        // まず最後尾へ１個ずつずらす(後ろから順番にコピー）
                        copyno = headno - length + 1; // 最後尾のno
                        for (; copyno <= bodyno - 1; copyno++)
                        {
                            body[copyno - 1] = body[copyno];
                        }
                        // 新しい中間パーツのX座標は拡張後左端なので0
                        body[bodyno - 1].X = 0;
                        // 全体1個後ろずらししたので、このチェックルーチンも一つ進める
                        cnt++;
                        bodyno--;
                    }
                }
            }
        }
    }
}
