using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace Atode
{
    enum Phase
    {   // ゲームのフェーズはスタートアップアニメーション＞ゲーム＞ゲームオーバーアニメーション＞ランキングの順に進む
        None = 0,
        Startup,
        Play,
        Death,
        Gameover,
        Ranking,
    }
    partial class SceneGame : Scene
    {
        private const int UPPER_PADDING = 1;        // ゲームメイン画面の上部にステータス1行有り
        private const int ITEM_NUM = 3;             // アイテム個数は固定
        private Item[] apple;
        private SnakeHero hero;
        protected const int ENEMY_MAX = 1000;
        private SnakeEnemy[] enemy;
        private int enemynum;

        private const int CEL_CHANGE_RATE = 3;      // キャラクター画面拡大スピード
        private int celchangerate;                  // キャラクター画面拡大中はスピードを示す 普段は0
        private int cellastxshift;                  // キャラクター画面が左側に拡大された量 これが変わると全てのオブジェクトのmap座標がずれる
        private int herolastlength;
        private int initialmapheight;
        private int GetStage() { return mapheight() - initialmapheight + 1; }

        // ゲーム中のフェーズ管理
        private int gamephase;
        // startup animation phase
        private const int STARTUP_SPEED = 2;
        private const int STARTUP_DEGREE_STAY = 15;                     // スタートアップアニメーションの余韻
        private const int STARTUP_DEGREE = (90+ STARTUP_DEGREE_STAY);   // カウントダウン初期値
        private int startupdeg;                     // カウントダウンして0になったらPlayPhaseへ移行

        // gameover animation phase
        private const int GAMEOVER_SPEED = 334;
        private const int GAMEOVER_STAY = 15;       // ゲームオーバーアニメーションが終わった後のタメ、余韻
        private int gameoverpos;
        private int gameoverzoom;
        private Color[] gameoverpixel;



        public SceneGame()
        {
            hero = new SnakeHero();
            apple = new Item[ITEM_NUM];

            for (int no = 0; no < ITEM_NUM; no++)
            {
                apple[no] = new Item();
            }
        }

        public void Init(Game1 g)
        {
            // 始まりのフェーズを指定
            gamephase = (int)Phase.Startup;
            startupdeg = STARTUP_DEGREE;

            // ゲームマップは画面サイズより狭い 画面の上１行はステータス行
            // 右端と左端、上端と下端はミラー行なので、２行で１行なので-1
            AllocMap(g.celwidth() - 1, g.celheight() - 1 - UPPER_PADDING);
            initialmapheight = mapheight();

            celchangerate = 0;      // キャラクター画面拡大中のみ正数
            cellastxshift = 0;      // キャラクター画面サイズは初期値

            // 自機を初期化、位置を指定
            hero.Init(g.celwidth()/2,g.celheight()/2);
            herolastlength = hero.length;   // 画面拡大のキー情報

            // 敵を初期化
            enemy = new SnakeEnemy[ENEMY_MAX];
            enemynum = 0;

            // 自機マップ配置
            hero.Plot(map);
            // 自機マップ配置の後でないとアイテムを配置できない
            // アイテムを初期化、配置
            InitItem(g);

            // ゲームオーバー表示処理用
            if (gameoverpixel == null)
            {
                gameoverpixel = new Color[g.txgameover.Width * g.txgameover.Height];
                g.txgameover.GetData<Color>(gameoverpixel);
            }

            base.Init();
        }
        public new void Update(Game1 g)
        {
            int no;
            // ゲームのメインロジック

            //--------------------------------------------------------------------------------
            // ユーザーによる操作
            if (g.inp.Get((int)Key.Back))
            {  // Backボタンでゲーム終了、タイトルへ
                nextScene = (int)Scn.Title;
            }

            if (g.inp.Get((int)Key.Up))
            {
                hero.SetDirection(0);
            }
            if (g.inp.Get((int)Key.Down))
            {
                hero.SetDirection(2);
            }
            if (g.inp.Get((int)Key.Left))
            {
                hero.SetDirection(1);
            }
            if (g.inp.Get((int)Key.Right))
            {
                hero.SetDirection(3);
            }
            if (g.inp.Get((int)Key.A))
            {   // スキップボタン
                if(gamephase == (int)Phase.Gameover)
                {   // ゲームオーバーアニメーションをスキップ
                    gameoverpos = -GAMEOVER_STAY * INTEGRAL_RANGE;
                }
            }

            //--------------------------------------------------------------------------------
            // キャラクター画面拡大処理
            if (0 < celchangerate)
            {   // ただいま画面サイズ遷移中
                if (g.scr.CelNextAdd(celchangerate) == 1)
                {   // キャラクター画面サイズ変更が完了した時の、画面拡大処理
                    celchangerate = 0;
                    // ゲームマップを広げる
                    AllocMap(g.celwidth() - 1, g.celheight() - 1 - UPPER_PADDING);
                    // 画面左端が拡張されたかどうか
                    int expandleft = g.scr.celxshift() - cellastxshift;
                    if (0 < expandleft)
                    {
                        cellastxshift = g.scr.celxshift();
                    }
                    // 蛇は画面が拡大したら必ず体を再配置する。右端下端をまたぐケースに対応するため。
                    hero.ExpandMap(expandleft);     // 自機
                    // 敵

                    // アイテムの再配置は、マップが左側に拡張された場合のみ
                    if (0<expandleft)
                    {   // アイテム位置移動 オブジェクトを右へずらして再配置
                        for (no = 0; no < ITEM_NUM; no++)
                        {
                            apple[no].ExpandMap(expandleft);
                        }

                    }
                }
            }

            //--------------------------------------------------------------------------------
            // マップ描画
            ClearMap();
            for (no = 0; no < ITEM_NUM; no++)
            {
                apple[no].Plot(map);
            }
            hero.Plot(map);
            for (no = 0; no < enemynum; no++)
            {
                enemy[no].Plot(map);
            }

            //--------------------------------------------------------------------------------
            // 各オブジェクトのUpdate
            if ( gamephase != (int)Phase.Startup)
            {   // startupの最中に自機が動いてしまうと動きがバグる
                // 自機を動かす
                hero.Update(g, map);
                // プレイフェーズ中のみ自機ヒット判定
                if(gamephase == (int)Phase.Play)
                {
                    Point headpos;
                    int rc = hero.CheckHit(map);
                    switch (rc)
                    {
                        case 1:
                            // アイテムを取った
                            headpos = hero.GetHead();
                            // どのアイテムか探す
                            for (no = 0; no < ITEM_NUM; no++)
                            {
                                if (apple[no].CheckHit(headpos))
                                {   // このアイテムが取られたので、アイテムを消す
                                    apple[no].SetDeath();
                                    break;
                                }
                            }
                            break;
                        case 2:
                            // 敵を倒した
                            headpos = hero.GetHead();
                            // どの敵か探す
                            break;
                    }

                }
                // デスアニメ中は静止した世界
                if (gamephase == (int)Phase.Death)
                {   // 敵は限定的に動く
                    for (no = 0; no < enemynum; no++)
                    {
                        enemy[no].UpdateStopWorld(g, map);
                    }
                }
                else
                {
                    // 敵を動かす
                    for (no = 0; no < enemynum; no++)
                    {
                        enemy[no].Update(g, map);
                    }


                    // アイテムを動かす
                    for (no = 0; no < ITEM_NUM; no++)
                    {
                        if (apple[no].Update())
                        {   // 消えていたアイテムが再誕生
                            apple[no].Reborn(SearchMapSpace(g));
                        }
                    }
                }
            }

            //--------------------------------------------------------------------------------
            // 各フェーズ処理
            switch (gamephase)
            {
                case (int)Phase.Play:
                    // ゲームプレイ中、自機が死んだらデスアニメモードへ遷移
                    if (hero.mode == (int)SnakeMode.Death)
                    {
                        gamephase = (int)Phase.Death;
                    }
                    break;
                case (int)Phase.Death:
                    // デスアニメ中、自機が死体になったらゲームオーバーへ
                    if (hero.mode == (int)SnakeMode.Corpse)
                    {
                        // ゲームオーバーアニメーションのフェーズへ遷移
                        gamephase = (int)Phase.Gameover;
                        gameoverzoom = (g.celheight() - UPPER_PADDING) / g.txgameover.Height;   // 1以上、boardの表示倍率
                        gameoverzoom = Math.Max(gameoverzoom, 1);
                        // X方向移動距離（CEL単位、1CEL移動=INTEGRAL_RANGE）右に隠れてスタート、左に隠れきるまでの距離 最後の+は余韻
                        gameoverpos = (g.celwidth() + g.txgameover.Width * gameoverzoom) * INTEGRAL_RANGE;
                    }
                    break;
                case (int)Phase.Gameover:
                    // ゲームオーバーアニメーション処理
                    gameoverpos -= GAMEOVER_SPEED * gameoverzoom;
                    if (gameoverpos <= -GAMEOVER_STAY * INTEGRAL_RANGE)
                    {   // アニメーション完了なので次のシーンへ
                        nextScene = (int)Scn.Title;
                    }
                    break;
                case (int)Phase.Startup:
                    startupdeg -= STARTUP_SPEED;
                    if (startupdeg <= 0)
                    {
                        startupdeg = 0;
                        gamephase = (int)Phase.Play;    // ゲームプレイ開始
                    }
                    break;
            }

            //--------------------------------------------------------------------------------
            // 自機が伸びたら画面拡大する処理 
            // 自機を動かした後で、これはUpdateの最後で判定する
            if ( herolastlength < hero.length)
            {
                herolastlength = hero.length;
                // 体の長さが4の倍数で画面拡大
                if((herolastlength % (ITEM_NUM + 1)) == 0)
                {
                    g.scr.CelResizeNext(g.celheight() + 1, CEL_CHANGE_RATE);
                    celchangerate = CEL_CHANGE_RATE;
                    // 画面が拡大すると敵も生まれる
                    if(enemynum < ENEMY_MAX)
                    {
                        Point pos = SearchMapSpace(g);
                        SnakeEnemy en = new SnakeEnemy();
                        en.Init(pos.X, pos.Y);
                        enemy[enemynum++] = en;
                    }
                }
            }
            base.Update(g);
        }

        private void DrawGameover(Game1 g)
        {
            // GAMEOVERの一枚絵をboardと呼ぶ
            int boardwidth = g.txgameover.Width;
            int boardheight = g.txgameover.Height;
            int xpos = gameoverpos / INTEGRAL_RANGE - g.txgameover.Width * gameoverzoom;
            int ypos = ((g.celheight() - UPPER_PADDING) - boardheight * gameoverzoom) / 2 + UPPER_PADDING;

            for(int yc=0; yc<boardheight; yc++)
            {
                for(int xc=0; xc<boardwidth; xc++)
                {
                    int x = xpos + xc * gameoverzoom;
                    int y = ypos + yc * gameoverzoom;
                    if(0<=x && x < g.celwidth()) {
                        Color color = gameoverpixel[xc + yc * boardwidth];
                        if (0 < color.R)
                        {
                            for(int zy = 0; zy < gameoverzoom; zy++){
                                for(int zx = 0; zx < gameoverzoom; zx++)
                                {
                                    Primitive.FillRectangle(g.spriteBatch, g.scr.TextBox(x+zx, y+zy), Color.Yellow);
                                }
                            }
                        }
                    }
                }
            }
        }
        public new void Draw(Game1 g)
        {
            int no;
            // フレーム描画
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // フィールド描画
            base.DrawGround(g,1);
            if (gamephase == (int)Phase.Gameover)
            {
                DrawGameover(g);
            }

            if(gamephase == (int)Phase.Startup)
            {
                int ratio;
                double stayval = Math.Cos(MathHelper.ToRadians(STARTUP_DEGREE_STAY));   // 最後にstartupdegが0になった時のcos値
                double nowval = Math.Cos(MathHelper.ToRadians(startupdeg - STARTUP_DEGREE_STAY));
                ratio = (int)(nowval * (double)INTEGRAL_RANGE / stayval);
                // アイテムを描画
                for (no = 0; no < ITEM_NUM; no++)
                {
                    apple[no].DrawStartup(g, UPPER_PADDING,ratio,INTEGRAL_RANGE);
                }
                // 蛇を描画
                hero.DrawStartup(g, mapwidth(), mapheight(), UPPER_PADDING, ratio, INTEGRAL_RANGE);
            }
            else
            {
                // アイテムを描画
                for (no = 0; no < ITEM_NUM; no++)
                {
                    apple[no].Draw(g, UPPER_PADDING);
                }
                // 蛇を描画
                hero.Draw(g, mapwidth(), mapheight(), UPPER_PADDING);
                // 敵を描画
                for (no = 0; no < enemynum; no++)
                {
                    enemy[no].Draw(g, mapwidth(), mapheight(), UPPER_PADDING);
                }
            }

            // 上部ステータス行
            Rectangle starea;
            starea = g.scr.TextBox(0, 0);
            starea.Width += g.scr.TextWidth(g.celwidth() - 1, 1);
            Primitive.FillRectangle(g.spriteBatch, starea, Color.Honeydew);
            // 体の長さ
            int xpos = (g.celwidth() - 1) / 2 - hero.length.ToString().Length - 4;
            if( xpos < 1)
            {
                xpos = 1;
            }
            g.DrawString("Size", xpos, 0, Color.LightBlue);
            g.DrawString(hero.length.ToString(), xpos+4, 0, Color.MidnightBlue);
            // ステージ表示
            int stage = GetStage();
            if(1 < stage)
            {
                xpos = (g.celwidth() - 1) / 2 + 1;
                g.DrawString("Stage", xpos, 0, Color.LightBlue);
                g.DrawString(stage.ToString(), xpos + 5, 0, Color.MidnightBlue);
            }

            g.spriteBatch.End();

            base.Draw(g);
        }
    }
}
