using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Atode
{
    enum PlayPhase
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
        protected const int MAPOBJECT_HEROTOP = 1;                              // 先頭ITEMオブジェクト番号 1は先頭NULLオブジェクト
        protected const int MAPOBJECT_HEROPATTERN = Snake.MAPOBJECT_SNAKEPATTERN * 2; // Heroのオブジェクトバターンは（通常 虹）で2倍のオブジェクト
        protected const int MAPOBJECT_ITEMTOP = MAPOBJECT_HEROTOP + MAPOBJECT_HEROPATTERN;
        protected const int MAPOBJECT_ENEMYTOP = MAPOBJECT_ITEMTOP + ITEM_NUM;  // 先頭ENEMYオブジェクト番号
        protected const int MAPOBJECT_MAX = MAPOBJECT_ENEMYTOP + ENEMY_MAX * Snake.MAPOBJECT_SNAKEPATTERN;     // オブジェクトの最大数


        private const int CEL_CHANGE_RATE = 3;      // キャラクター画面拡大スピード
        private int celchangerate;                  // キャラクター画面拡大中はスピードを示す 普段は0
        private int cellastxshift;                  // キャラクター画面が左側に拡大された量 これが変わると全てのオブジェクトのmap座標がずれる
        private int herolastlength;
        private int initialMapheight;
        private int GetStage() { return mapheight() - initialMapheight + 1; }

        // ゲーム中のフェーズ管理
        private PlayPhase phasenow;
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

        // ランキング画面用 Ranking phase
        private int rankingCounter;                 // 最低表示期間用カウンタ
        private int rankNow;                        // 今回のランク
        List<PlayRecord> ranking;


        public SceneGame()
        {
            map = new CollisionMap(MAPOBJECT_MAX);

            hero = new SnakeHero(MAPOBJECT_HEROTOP);
            map.SetObject(MAPOBJECT_HEROTOP, MapChip.SnakeHead, 0, hero);
            map.SetObject(MAPOBJECT_HEROTOP + 1, MapChip.SnakeBody, 0, hero);
            map.SetObject(MAPOBJECT_HEROTOP + 2, MapChip.SnakeTail, 0, hero);
            map.SetObject(MAPOBJECT_HEROTOP + 3, MapChip.RainbowHead, 0, hero);
            map.SetObject(MAPOBJECT_HEROTOP + 4, MapChip.RainbowBody, 0, hero);
            map.SetObject(MAPOBJECT_HEROTOP + 5, MapChip.RainbowTail, 0, hero);

            apple = new Item[ITEM_NUM];
            for (int no = 0; no < ITEM_NUM; no++)
            {
                apple[no] = new Item(MAPOBJECT_ITEMTOP + no);
                map.SetObject(MAPOBJECT_ITEMTOP + no, MapChip.Item, no,null);
            }
        }

        public void Init(Game1 g)
        {
            // 始まりのフェーズを指定
            phasenow = PlayPhase.Startup;
            startupdeg = STARTUP_DEGREE;

            // ゲームマップ関係変数初期化
            // ゲームマップは画面サイズより狭い 画面の上１行はステータス行
            // 右端と左端、上端と下端はミラー行なので、２行で１行なので-1
            map.AllocMap(g.celwidth() - 1, g.celheight() - 1 - UPPER_PADDING);
            initialMapheight = mapheight();

            celchangerate = 0;      // キャラクター画面拡大中のみ正数
            cellastxshift = 0;      // キャラクター画面サイズは初期値

            // 自機を初期化、位置を指定
            hero.Init(new Point(g.celwidth()/2,g.celheight()/2));
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
            // ランキング表示用
            rankingCounter = 0;

            base.Init();
        }
        public new void Update(Game1 g)
        {
            int no;
            // ゲームのメインロジック

            //--------------------------------------------------------------------------------
            // ユーザーによる操作
            // 戻るボタン処理はここではなくGame1で（ポーズ中も有効にするため）
            if (g.inp.Get(Key.Up))
            {
                hero.SetDirection(0);
            }
            if (g.inp.Get(Key.Down))
            {
                hero.SetDirection(2);
            }
            if (g.inp.Get(Key.Left))
            {
                hero.SetDirection(1);
            }
            if (g.inp.Get(Key.Right))
            {
                hero.SetDirection(3);
            }
            if (g.inp.Get(Key.A))
            {   // スキップボタン
                if(phasenow == PlayPhase.Gameover)
                {   // ゲームオーバーアニメーションをスキップ
                    gameoverpos = -GAMEOVER_STAY * INTEGRAL_RANGE;
                }
            }

            if (phasenow == PlayPhase.Ranking)
            {
                if (KEY_RELEASE_WAIT < rankingCounter)
                {   // ランキング表示を終了してタイトル画面へ戻る
                    if (g.inp.AnyKey())
                    {
                        // 何かキーが押されたら
                        nextScene = Scn.Title;
                    }
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
                    map.AllocMap(g.celwidth() - 1, g.celheight() - 1 - UPPER_PADDING);
                    // 画面左端が拡張されたかどうか
                    int expandleft = g.scr.celxshift() - cellastxshift;
                    if (0 < expandleft)
                    {
                        cellastxshift = g.scr.celxshift();
                    }
                    // 蛇は画面が拡大したら必ず体を再配置する。右端下端をまたぐケースに対応するため。
                    // 自機
                    hero.ExpandMap(expandleft);
                    // 敵を画面拡大にあわせて再配置
                    for (no = 0; no < enemynum; no++)
                    {
                        enemy[no].ExpandMap(expandleft);
                    }

                    // アイテムの再配置は、マップが左側に拡張された場合のみ
                    if (0<expandleft)
                    {   // アイテム位置移動 オブジェクトを右へずらして再配置
                        for (no = 0; no < ITEM_NUM; no++)
                        {
                            apple[no].ExpandMap(expandleft);
                        }

                    }
                    // 画面が拡大すると敵も生まれる
                    if (enemynum < ENEMY_MAX)
                    {
                        Point pos = SearchMapSpace(g);
                        int mapObjectNo = MAPOBJECT_ENEMYTOP + enemynum * Snake.MAPOBJECT_SNAKEPATTERN;
                        SnakeEnemy newenemy = new SnakeEnemy(mapObjectNo);
                        newenemy.Init(pos);
                        enemy[enemynum] = newenemy;
                        map.SetObject(mapObjectNo, MapChip.EnemyHead, enemynum, newenemy);
                        map.SetObject(mapObjectNo + 1, MapChip.EnemyBody, enemynum, newenemy);
                        map.SetObject(mapObjectNo + 2, MapChip.EnemyTail, enemynum, newenemy);
                        enemynum++;
                    }
                }
            }

            //--------------------------------------------------------------------------------
            // コリジョンマップ描画
            map.ClearMap();
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

            // startupの最中は何も動かしてはダメ。自機が動いてしまうと動きがバグる
            if ( phasenow != PlayPhase.Startup)
            {   
                // 自機を動かす
                hero.Update(g, map);
                // プレイフェーズ中のみ自機ヒット判定
                if(phasenow == PlayPhase.Play)
                {
                    MapObject mo = hero.CheckHit(g,map);
                    switch (mo.chip)
                    {
                        case MapChip.Item:
                            // アイテムを取った
                            // 取られたアイテムを除去
                            apple[mo.localNo].SetDeath();
                            break;
                        case MapChip.EnemyHead:
                        case MapChip.EnemyBody:
                        case MapChip.EnemyTail:
                            // 敵を倒した
                            // 倒された敵を殺す
                            enemy[mo.localNo].SetDeath(true);
                            break;
                    }
                }

                // 敵とアイテムを動かす
                // デスアニメ中は静止した世界
                if (phasenow == PlayPhase.Death)
                {   // 敵は限定的に動く  もともとは、衝突時に敵と自機にまだ隙間があるのを誤魔化すため（衝突判定を変えたので必要なくなった）
                    // 止め絵はぴったりマスにハマった状態が美しいから残す
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
                        if(enemy[no].Update(g, map) == 1)
                        {
                            enemy[no].Reborn(SearchMapSpace(g));
                        }
                        if(enemy[no].CheckHit(g,map) == 1)
                        {
                            hero.Grow(map);
                        }
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
            switch (phasenow)
            {
                case PlayPhase.Play:
                    // ゲームプレイ中、自機が死んだらデスアニメモードへ遷移
                    if (hero.modenow == SnakeMode.Death)
                    {
                        phasenow = PlayPhase.Death;
                        // ゲーム結果確定タイミングで行うべき処理
                        // ランキング登録など
                        GameResultProcess(g);
                    }
                    break;
                case PlayPhase.Death:
                    // デスアニメ中、自機が死体になったらゲームオーバーへ
                    if (hero.modenow == SnakeMode.Corpse)
                    {
                        // ゲームオーバーアニメーションのフェーズへ遷移
                        phasenow = PlayPhase.Gameover;
                        gameoverzoom = (g.celheight() - UPPER_PADDING) / g.txgameover.Height;   // 1以上、boardの表示倍率
                        gameoverzoom = Math.Max(gameoverzoom, 1);
                        // X方向移動距離（CEL単位、1CEL移動=INTEGRAL_RANGE）右に隠れてスタート、左に隠れきるまでの距離 最後の+は余韻
                        gameoverpos = (g.celwidth() + g.txgameover.Width * gameoverzoom) * INTEGRAL_RANGE;
                    }
                    break;
                case PlayPhase.Gameover:
                    // ゲームオーバーアニメーション処理
                    gameoverpos -= GAMEOVER_SPEED * gameoverzoom;
                    if (gameoverpos <= -GAMEOVER_STAY * INTEGRAL_RANGE)
                    {   // アニメーション完了なのでランキングフェーズへ
                        phasenow = PlayPhase.Ranking;
                        StartGrountWave();
                    }
                    break;
                case PlayPhase.Ranking:
                    // ランキング表示
                    rankingCounter++;
                    break;
                case PlayPhase.Startup:
                    startupdeg -= STARTUP_SPEED;
                    if (startupdeg <= 0)
                    {
                        startupdeg = 0;
                        phasenow = PlayPhase.Play;    // ゲームプレイ開始
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
                }
            }
            base.Update(g);
        }

        // ゲームオーバー時の背景アニメーション表示
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

        // ランキング画面を表示
        private void DrawRanking(Game1 g)
        {
            int xleft;
            int cx;
            int cy = UPPER_PADDING;
            bool fullinfo;
            string title;
            String str;
            const string titleLong = "Rank       Date Size Stage";
            const string titleMini = "Rank       Date Size";
            const string nodata = "No Record.";
            const string autopilotMessage = "Autopilot ranking";

            // ヘッダー行
            if (g.celwidth() < titleLong.Length)
            {
                fullinfo = false;
                title = titleMini;
            }
            else
            {
                fullinfo = true;
                title = titleLong;
            }
            xleft = (g.celwidth() - title.Length) / 2;
            g.DrawString(title, xleft, cy, Color.LightBlue);

            // 標準スピード以外であればヘッダー行にスピード表示
            // ランキングはスピード毎に分かれているということを気づかせるため
            if (g.speedRate != Game1.SPEED_DEFAULT)
            {
                cx = xleft + 4; // "Rank"の文字列直後の位置
                g.DrawString("speed"+g.speedRate.ToString(), cx, cy, Color.DarkSeaGreen);
            }
            cy++;

            // ランキング行
            if (ranking == null)
            {
                xleft = (g.celwidth() - nodata.Length) / 2;
                g.DrawString(nodata, xleft, cy++, Color.White);
            }
            else
            {
                int ymax = g.celheight()-1;
                if (g.autopilotEnable)
                {
                    ymax--;
                }
                for(int no=0; no < ranking.Count; no++)
                {
                    const int RRANK = 4;
                    const int RDATE = RRANK + 1 + 10;
                    const int RSIZE = RDATE + 1 + 4;
                    const int RSTAGE = RSIZE + 1 + 5;
                    PlayRecord play = ranking[no];
                    Color color = Color.White;
                    if(no == rankNow)
                    {
                        color = Color.Yellow;
                    }

                    // rank
                    int rank = no + 1;
                    switch (rank)
                    {
                        case 1:
                            str = "1st";
                            break;
                        case 2:
                            str = "2nd";
                            break;
                        case 3:
                            str = "3rd";
                            break;
                        default:
                            str = rank.ToString();
                            if(rank <= 10)
                            {
                                str += "th";
                            }
                            break;
                    }
                    cx = xleft + RRANK - str.Length;
                    g.DrawString(str, cx, cy, color);
                    // date
                    str = play.playdate.ToString("d");
                    cx = xleft + RDATE - str.Length;
                    g.DrawString(str, cx, cy, color);
                    // size
                    str = play.size.ToString();
                    cx = xleft + RSIZE - str.Length;
                    g.DrawString(str, cx, cy, color);
                    if(fullinfo)
                    {
                        // stage
                        str = play.stage.ToString();
                        cx = xleft + RSTAGE - str.Length;
                        g.DrawString(str, cx, cy, color);
                    }

                    cy++;
                    if (ymax < cy)
                    {
                        break;
                    }
                }
            }

            // オートパイロット注意書き
            if (g.autopilotEnable)
            {
                if (g.celwidth() < autopilotMessage.Length)
                {
                    xleft = g.celwidth() - ((upcounter / 20) % (autopilotMessage.Length + g.celwidth()));
                }
                else
                {
                    xleft = (g.celwidth() - autopilotMessage.Length) / 2;
                }
                g.DrawString(autopilotMessage, xleft, cy, Color.DeepPink * (float)0.5);
            }
        }

        // ゲーム画面描画
        public new void Draw(Game1 g)
        {
            int no;
            // フレーム描画
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // フィールド描画
            base.DrawGround(g,1);
            if (phasenow == PlayPhase.Gameover)
            {
                DrawGameover(g);
            }

            if(phasenow == PlayPhase.Startup)
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
            starea = g.scr.TextBox(0, 0, g.celwidth(), 1);
            //starea.Width += g.scr.TextWidth(g.celwidth() - 1, 1);
            Primitive.FillRectangle(g.spriteBatch, starea, Color.Honeydew);
            // 体の長さを表示
            // 中央から左側に表示 -4は"Size"
            int xpos = (g.celwidth() - 1) / 2 - hero.length.ToString().Length - 4;
            if( xpos < 1)
            {
                xpos = 1;
            }
            g.DrawString("Size", xpos, 0, Color.LightBlue);
            g.DrawString(hero.length.ToString(), xpos+4, 0, Color.MidnightBlue);
            // ステージ番号表示
            int stage = GetStage();
            if(1 < stage)
            {   // 中央の右側に表示
                xpos = (g.celwidth() - 1) / 2 + 1;
                g.DrawString("Stage", xpos, 0, Color.LightBlue);
                g.DrawString(stage.ToString(), xpos + 5, 0, Color.MidnightBlue);
            }
            // 右端から
            xpos = g.celwidth();
            // スピード表示
            if (g.speedRate != Game1.SPEED_DEFAULT)
            {   // 標準スピードの場合は表示なし
                String ratestr = g.speedRate.ToString();
                xpos -= ratestr.Length;
                g.DrawString(ratestr, xpos, 0, Color.DarkSeaGreen);
            }
            // オートパイロット表示
            if (g.autopilotEnable)
            {
                String autostr = "AT";
                xpos -= autostr.Length;
                g.DrawString(autostr, xpos, 0, Color.DeepPink);
            }


            if (phasenow == PlayPhase.Ranking)
            {   // ランキング表示
                DrawRanking(g);
            }

                g.spriteBatch.End();

            base.Draw(g);
        }
    }
}
