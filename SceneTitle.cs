using Snake82;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;


namespace Atode
{
    enum TitlePhase
    {
        None = 0,
        Demo,
        Rush,
    }
    // タイトル画面の選択メニュー
    enum MenuType
    {
        Start = 0,
        Option,
        Quit,
        Speed,
        AutoPilot,
        FullScreen,
        Exit,
    }
    struct MenuInfo
    {
        public MenuType type;
        public bool visible;
        public string name;
        public int level;

        public MenuInfo(string name, MenuType type, bool visible,int level)
        {
            this.type = type;
            this.visible = visible;
            this.name = name;
            this.level = level;
        }
    }
    class SceneTitle : Scene
    {
        protected const int ENEMY_MAX = 20;
        private const int ITEM_NUM = 1;
        protected const int MAPOBJECT_HEROTOP = 1;                              // 先頭ITEMオブジェクト番号 1は先頭NULLオブジェクト
        protected const int MAPOBJECT_HEROPATTERN = Snake.MAPOBJECT_SNAKEPATTERN * 2; // Heroのオブジェクトバターンは（通常 虹）で2倍のオブジェクト
        protected const int MAPOBJECT_ITEMTOP = MAPOBJECT_HEROTOP + MAPOBJECT_HEROPATTERN;
        protected const int MAPOBJECT_ENEMYTOP = MAPOBJECT_ITEMTOP + ITEM_NUM;  // 先頭ENEMYオブジェクト番号
        protected const int MAPOBJECT_MAX = MAPOBJECT_ENEMYTOP + ENEMY_MAX * Snake.MAPOBJECT_SNAKEPATTERN;     // オブジェクトの最大数
        private SnakeDemo snake;
        private SnakeRush[] enemy;

        private Item item;
        private MenuInfo[] menu;
        private int menuSelect;
        private int menuLevel;
        private const int SUBMENU_LIFT = 2;
        private int MenuTypeLength() { return Enum.GetNames(typeof(MenuType)).Length; }
        private int[] speedRateList = { 7, 10, 15, 20 };
        private int appearCounter;
        private int appearNumber;
        private const int FULLSCREEN_REACTION = 150;    // フルスクリーン変更はハードウェア操作なので頻繁にされるとトラブルの元となる
        private int fullscreenCounter;
        // ゲーム中のフェーズ管理
        private TitlePhase phasenow = TitlePhase.Rush;


        private bool enableDeath = false; 

        public SceneTitle()
        {
            map = new CollisionMap(MAPOBJECT_MAX);

            snake = new SnakeDemo(MAPOBJECT_HEROTOP);
            map.SetObject(MAPOBJECT_HEROTOP, MapChip.SnakeHead, 0,snake);
            map.SetObject(MAPOBJECT_HEROTOP + 1, MapChip.SnakeBody, 0, snake);
            map.SetObject(MAPOBJECT_HEROTOP + 2, MapChip.SnakeTail, 0, snake);
            map.SetObject(MAPOBJECT_HEROTOP + 3, MapChip.RainbowHead, 0, snake);
            map.SetObject(MAPOBJECT_HEROTOP + 4, MapChip.RainbowBody, 0, snake);
            map.SetObject(MAPOBJECT_HEROTOP + 5, MapChip.RainbowTail, 0, snake);

            item = new Item(MAPOBJECT_ITEMTOP);
            map.SetObject(MAPOBJECT_ITEMTOP, MapChip.Item,0,null);

            // タイトル画面のメニュー作成
            menu = new MenuInfo[MenuTypeLength()];
            menu[0] = new MenuInfo("START", MenuType.Start, true,0);
            menu[1] = new MenuInfo("OPTION", MenuType.Option, true,0);
            menu[2] = new MenuInfo("QUIT", MenuType.Quit, true,0);
            menu[3] = new MenuInfo("SPEED", MenuType.Speed, true, 1);
            menu[4] = new MenuInfo("AUTO PILOT", MenuType.AutoPilot, false, 1);
            menu[5] = new MenuInfo("FULL SCREEN", MenuType.FullScreen, true, 1);
            menu[6] = new MenuInfo("EXIT", MenuType.Exit, true, 1);

        }

        public void Init(Game1 g)
        {
            upcounter = 0;
            
            // 蛇が死ぬモードと死なないモードを繰り返す
            enableDeath = !enableDeath;

            // ゲームマップは画面サイズより狭い 画面の上１行はステータス行、右端と左端はミラー行
            map.AllocMap( g.celwidth() - 1, g.celheight() - 1);

            // 必ずAllocMapの後で（コンストラクタでは画面サイズ不明で実施不可）
            // 開幕ラッシュ用スネークのインスタンス作成
            if (enemy == null)
            {
                int mapObjectNo = MAPOBJECT_ENEMYTOP;
                enemy = new SnakeRush[g.celheight()];
                Color[] rushcolor = new Color[11]{  // 1個多め
                    new Color( 0xa7, 0xff, 0x00 ),
                    new Color( 0xff, 0xf5, 0x00 ),
                    new Color( 0xff, 0x93, 0x00 ),
                    new Color( 0xff, 0x31, 0x00 ),
                    new Color( 0xff, 0x00, 0x3c ),
                    new Color( 0xff, 0x00, 0x9e ),
                    new Color( 0xfe, 0x00, 0xff ),
                    new Color( 0x9c, 0x00, 0xff ),
                    new Color( 0x3a, 0x00, 0xff ),
                    new Color( 0x00, 0x28, 0xff ),
                    new Color( 0x00, 0x8a, 0xff )};

                for (int y = 0; y<g.celheight(); y++)
                {
                    SnakeRush newenemy = new SnakeRush(mapObjectNo,rushcolor[y%rushcolor.Length]);
                    enemy[y] = newenemy;
                    mapObjectNo += Snake.MAPOBJECT_SNAKEPATTERN;
                }
            }
            // ラッシュスネーク初期化(位置と方向の指定）
            int left = -1;
            int right = g.celwidth();
            for (int y = enemy.Length - 1; 0 <= y; y--)
            {
                int rushdirection;
                Point pos;
                pos.Y = y;
                if (y % 2 == 1)
                {
                    pos.X = left;
                    left--;
                    rushdirection = 3;
                }
                else
                {
                    pos.X = right;
                    right++;
                    rushdirection = 1;
                }
                enemy[y].Init(pos, rushdirection);
            }

            // 自機を初期化、位置を指定
            snake.Init(new Point(g.celwidth() / 2, g.celheight() / 2 + 2));
            // アイテムは最初存在しない
            item.SetDeath();

            // メニュー選択の初期化
            menuSelect = 0; // START
            menuLevel = 0;  // 最上位階層
            appearCounter = 0;
            appearNumber = 0;
            if (g.autopilotAppear)
            {   // 隠し機能が新規公開されたので
                menuLevel = 1;  // TOPメニューではなくOPTION画面を強制的に見せつける
                menuSelect = 4;
                appearCounter = INTEGRAL_RANGE;
                appearNumber = 1;
                g.autopilotUnlock = true;
                g.autopilotEnable = true;
                g.autopilotAppear = false;
            }
            // セーブデータでunlockされているケースもある
            if (g.autopilotUnlock)
            {
                menu[4].visible = true;
            }

            base.Init();
        }
        public new void Update(Game1 g)
        {
            // ゲームのメインロジック

            // 入力処理
            if (KEY_RELEASE_WAIT < upcounter)
            {   // ゲーム終了時にボタンが押されていると、またすぐゲームが始まってしまわないよう少し待つ
                if (g.inp.Get(Key.A))
                {   // 決定
                    switch (menu[menuSelect].type)
                    {
                        case MenuType.Start:
                            // ゲーム開始へ
                            nextScene = Scn.Game;
                            phasenow = TitlePhase.Demo; // タイトル画面に戻った時にまたラッシュの出番の必要はない。
                            break;
                        case MenuType.Option:
                            // オプション画面へ
                            menuLevel = 1;
                            menuSelect = 3; // アドホックな実装
                            break;
                        case MenuType.Quit:
                            // プログラム終了
                            nextScene = Scn.Quit;
                            break;
                        case MenuType.Exit:
                            // トップメニューへ
                            menuLevel = 0;
                            menuSelect = 0;
                            break;
                    }
                }
                if (g.inp.Get(Key.Up))
                {   // メニューを上へ
                    do
                    {
                        menuSelect--;
                        if (menuSelect < 0)
                        {
                            menuSelect = MenuTypeLength() - 1;
                        }
                    } while (menu[menuSelect].level != menuLevel || menu[menuSelect].visible == false);
                }
                if (g.inp.Get(Key.Down))
                {   // メニューを下へ
                    do
                    {
                        menuSelect++;
                        if (MenuTypeLength() <= menuSelect)
                        {
                            menuSelect = 0;
                        }
                    } while (menu[menuSelect].level != menuLevel || menu[menuSelect].visible == false);
                }
                if (g.inp.Get(Key.Left))
                {
                    //  メニューを左へ
                    if (menu[menuSelect].type == MenuType.Speed)
                    {   // スピードを下げる
                        // 最低スピードは対象外 1段階下げる処理
                        for (int n = 1; n < speedRateList.Length; n++)
                        {
                            if (speedRateList[n] == g.speedRate)
                            {
                                g.speedRate = speedRateList[n - 1];
                                break;
                            }
                        }
                    }
                    if (menu[menuSelect].type == MenuType.AutoPilot)
                    {   // 衝突防止機能on
                        g.autopilotEnable = true;
                    }
                    if (menu[menuSelect].type == MenuType.FullScreen)
                    {   // フルスクリーンモードへ
                        if(g.graphics.IsFullScreen == false)
                        {
                            if (fullscreenCounter + FULLSCREEN_REACTION < upcounter)
                            {
                                fullscreenCounter = upcounter;
                                g.scr.ToggleFull(g);
                            }
                        }
                    }
                }
                if (g.inp.Get(Key.Right))
                {
                    //  メニューを右へ
                    if (menu[menuSelect].type == MenuType.Speed)
                    {   // スピードを上げる
                        // 最高スピードは対象外 1段階上げる処理
                        for (int n = 0; n < speedRateList.Length-1; n++)
                        {
                            if (speedRateList[n] == g.speedRate)
                            {
                                g.speedRate = speedRateList[n + 1];
                                break;
                            }
                        }
                    }
                    if (menu[menuSelect].type == MenuType.AutoPilot)
                    {   // 衝突防止機能off
                        g.autopilotEnable = false;
                    }
                    if (menu[menuSelect].type == MenuType.FullScreen)
                    {   // ウィンドウモードへ
                        if (g.graphics.IsFullScreen == true)
                        {
                            if (fullscreenCounter + FULLSCREEN_REACTION < upcounter)
                            {
                                fullscreenCounter = upcounter;
                                g.scr.ToggleFull(g);
                            }
                        }
                    }
                }
            }

            if(phasenow == TitlePhase.Rush)
            {
                bool isactive = false;
                for(int i = 0; i < enemy.Length; i++)
                {
                    bool newstep = enemy[i].Update(g,map);
#if TITLESNAP
                    if (newstep)
                    {
                        g.inp.bPause = true;
                    }
#endif
                    if(enemy[i].modenow == SnakeMode.Active)
                    {
                        isactive = true;
                    }
                }
                if (isactive == false)
                {
                    phasenow = TitlePhase.Demo;
                }
            }
            else
            {
                // マップ描画
                map.ClearMap();
                snake.Plot(map);
                item.Plot(map);
                // 自機を動かす
                bool eatItemMode = (item.modenow == SnakeMode.Active);  // アイテムがあればまっすぐ取りに行く
                if (snake.Update(g, map, eatItemMode) == 1)
                {
                    item.SetDeath();
                }
                // アイテムを動かす
                item.Update();

                if (enableDeath)
                {   // 無敵モードではない 衝突判定を行う
                    MapObject mo = snake.GetHit(g, map);
                    if (map.IsSnakeChip(mo.chip))
                    {   // 頭が蛇に衝突した
                        if (mo.chip == MapChip.SnakeHead || mo.chip == MapChip.RainbowHead)
                        {
                            // 自分の頭であれば何もしない
                        }
                        else
                        {
                            snake.SetDeath(true);
                            item.SetDeath();    // アイテムも消す
                        }
                    }

                    if (snake.modenow == SnakeMode.Standby)
                    {
                        snake.Reborn(new Point(g.celwidth() / 2, g.celheight() / 2 + 2));
                    }
                }
                // アイテムが無ければ
                if (item.modenow == SnakeMode.Death && snake.modenow == SnakeMode.Active)
                {   // アイテムを配置する
                    if (g.rand.Next(20) < 1)
                    {
                        // 場所を決める
                        const int ITEM_FORWARD = 2; // 進行方向の何マス先にアイテムを置くか
                        Point po = snake.GetHeadPoint();
                        switch (snake.direction)
                        {
                            case 0:
                                po.Y -= ITEM_FORWARD;
                                break;
                            case 1:
                                po.X -= ITEM_FORWARD;
                                break;
                            case 2:
                                po.Y += ITEM_FORWARD;
                                break;
                            case 3:
                                po.X += ITEM_FORWARD;
                                break;
                        }
                        // 座標がマップ内であること＋ミラー行（上端＆左端）でないこと
                        if (0 < po.X && po.X < mapwidth() && 0 < po.Y && po.Y < mapheight())
                        {
                            int score = map.ScoreMap(po, MapType.Item);
                            if (score <= CollisionMap.SCORE_TITLEMIN || enableDeath == false)
                            {
                                item.Reborn(po);
                            }
                        }
                    }
                }
            }

            // その他処理
            // 隠し機能告知
            if (0 < appearCounter)
            {
                appearCounter -= 15;
                if( appearCounter <= 0)
                {
                    switch (appearNumber)
                    {
                        case 1:
                        case 2:
                            appearNumber++;
                            appearCounter = INTEGRAL_RANGE;
                            break;
                        default:    // 終了
                            appearNumber = 0;
                            appearCounter = 0;
                            break;
                    }
                }
            }

            // 音楽が演奏中なら停止
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Volume -= 0.02f;
                if (MediaPlayer.Volume <= 0f)
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Volume = 1f;
                }
            }

            base.Update(g);
        }

        // ON OFF の選択メニューを描画
        private void DrawMenuOnoff(Game1 g,int cy,Color clr,bool onis)
        {
            string[] menuOnoff = { "on", "off" };
            int cx = g.celwidth() / 2 - 3;
            int selected = onis ? 0 : 1;
            for (int n = 0; n < menuOnoff.Length; n++)
            {
                Color strcolor = clr;
                if (n == selected)
                {
                    Rectangle grnd = g.scr.TextBox(cx - 1, cy, menuOnoff[n].Length + 2, 1);
                    Primitive.FillRectangle(g.spriteBatch, grnd, clr);
                    strcolor = Color.White;
                }
                g.DrawString(menuOnoff[n], cx, cy, strcolor);
                cx += menuOnoff[n].Length + 1;
            }

        }
        // メニュー描画
        private void DrawMenu(Game1 g)
        {
            int cx;
            int cy = g.celheight() / 2;
            int blinking;

            if (0 < menuLevel )
            {
                cy -= SUBMENU_LIFT;
                if (g.autopilotUnlock)
                {
                    cy -= SUBMENU_LIFT;
                }
            }

            // 選択メニューを表示
            for (int i = 0; i < MenuTypeLength(); i++)
            {
                if (menu[i].level == menuLevel && menu[i].visible)
                {
                    Color clr = Color.DarkOliveGreen;
                    if (i == menuSelect)
                    {
                        // 選択中のメニューの色は、周期的に光る
                        blinking = (upcounter + 30) % 180;
                        if (90 <= blinking)
                        {
                            blinking = 180 - blinking;
                        }
                        blinking -= 60;
                        if (blinking < 0)
                        {
                            blinking = 0;
                        }
                        // blinking は 0-30 になる
                        clr = new Color(255, blinking * 6 + 70, blinking * 6 + 70);
                    }
                    string menustr = menu[i].name;
                    cx = g.celwidth() / 2 - (menustr.Length + 1) / 2;
                    g.DrawString(menustr, cx, cy++, clr);

                    // スピード選択メニュー
                    if (menu[i].type == MenuType.Speed)
                    {
                        // メニューの横幅を計算して横位置を決める
                        int menuwidth = 0;
                        int n;
                        for (n = 0; n < speedRateList.Length; n++)
                        {
                            menuwidth += speedRateList[n].ToString().Length + 1;
                        }
                        cx = g.celwidth() / 2 - menuwidth / 2;

                        for (n = 0; n < speedRateList.Length; n++)
                        {
                            string speedMenu = speedRateList[n].ToString();
                            Color strcolor = clr;
                            if (speedRateList[n] == g.speedRate)
                            {
                                Rectangle grnd = g.scr.TextBox(cx-1, cy,speedMenu.Length+2,1);
                                Primitive.FillRectangle(g.spriteBatch, grnd, clr);
                                strcolor = Color.White;
                            }
                            g.DrawString(speedMenu, cx, cy, strcolor);
                            cx += speedMenu.Length + 1;
                        }
                        cy++;
                    }
                    // オートパイロットon/offメニュー
                    if (menu[i].type == MenuType.AutoPilot)
                    {
                        DrawMenuOnoff(g, cy++, clr, g.autopilotEnable);
                    }

                    // フルスクリーンon/offメニュー
                    if (menu[i].type == MenuType.FullScreen)
                    {
                        DrawMenuOnoff(g, cy++, clr, g.graphics.IsFullScreen);
                    }
                }
            }

            // メニューの下に、どのキーを押せばよいかボタンガイドを表示
            const int GUIDE_PERIOD = 240;
            float alpha = 1f;
            // キーボード操作とゲームパッド操作を定期的に入れ替える
            string guidestr = "Enter Key";
            blinking = upcounter % GUIDE_PERIOD;
            if (GUIDE_PERIOD / 2 <= blinking)
            {   // \a = ベル制御コード = ボタンAのアイコン
                blinking -= GUIDE_PERIOD / 2;
                guidestr = "\a Button";
            }
#if TITLESNAP
            guidestr = "\a Button"; // 画面撮影用には見栄えがいい
#endif
            if (GUIDE_PERIOD / 4 < blinking)
            {
                blinking = GUIDE_PERIOD / 2 - blinking;
            }
            if (blinking < GUIDE_PERIOD / 8)
            {
                alpha = (float)blinking / (float)(GUIDE_PERIOD / 8);
            }
            if (menuLevel == 0)
            {   // トップ階層の場合は、決定ボタンを表示
                cx = g.celwidth() / 2 - (guidestr.Length + 1) / 2;
                g.DrawString(guidestr, cx, cy, Color.LightGray * alpha);
            }
            else
            {   // OPTION画面の場合は、上下左右で操作することを表示
                const int ARROW_LENGTH = 4;
                cx = g.celwidth() / 2 - (ARROW_LENGTH + 1) / 2;
                Rectangle rect;
                rect = g.scr.TextBox(cx++, cy);
                g.spriteBatch.Draw(g.fonts, rect, g.Font((char)0), Color.LightGray * alpha);
                rect = g.scr.TextBox(cx++, cy);
                g.spriteBatch.Draw(g.fonts, rect, g.Font((char)1), Color.LightGray * alpha);
                rect = g.scr.TextBox(cx++, cy);
                g.spriteBatch.Draw(g.fonts, rect, g.Font((char)2), Color.LightGray * alpha);
                rect = g.scr.TextBox(cx, cy);
                g.spriteBatch.Draw(g.fonts, rect, g.Font((char)3), Color.LightGray * alpha);
            }
        }

        public new void Draw(Game1 g)
        {
            int cx;
            int cy;
            // フレーム描画
            // spriteのドット絵をボケない指定
            g.spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            base.DrawGround(g, 0);

            if (phasenow == TitlePhase.Rush)
            {   // 開幕ラッシュスネークを描画
                for (int i = 0; i < enemy.Length; i++)
                {
                    enemy[i].Draw(g, mapwidth(), mapheight(), 0);
                }
            }
            else
            {
                // 蛇を描画
                snake.Draw(g, mapwidth(), mapheight(), 0);
                item.Draw(g, 0);
            }


            // オプションメニューでauto pilotメニュー表示ありの場合はスペースがないのでタイトル表示をカット
            if (menuLevel==0 || g.autopilotUnlock == false)
            {
                // タイトル文字表示 色は明滅させる
                string titlestr = "Snake82";
                cx = g.celwidth() / 2 - (titlestr.Length + 1) / 2;
                cy = g.celheight() / 2 - 2;
                if (0 < menuLevel)
                {
                    cy -= SUBMENU_LIFT;
                }
                int blinking;
                for (int i = 0; i < titlestr.Length; i++)
                {   // 一文字ごとに明るさを変える
                    // 明るさを計算
                    blinking = (upcounter - i * 10) % 180;
                    if (90 <= blinking)
                    {
                        blinking = 180 - blinking;
                    }
                    blinking -= 30;
                    if (blinking < 0)
                    {
                        blinking = 0;
                    }
                    // blinking は 0-60 になる
                    Color titlecolor = new Color(255, blinking * 2 + 130, blinking * 2 + 130);
                    g.DrawString(titlestr.Substring(i, 1), cx++, cy, titlecolor);
                }
            }

            if (g.totalPlay % 20 == 0)
            {   // ときどきコピーライト表示（プレイ回数の区切り目）
                cx = g.celwidth() - ((upcounter / 20) % (g.celwidth()+ Game1.copyright.Length));
                g.DrawString(Game1.copyright, cx, g.celheight() - 1, Color.White * (float)0.1);
            }

            // ユーザー選択メニューを描画
            DrawMenu(g);

            if(0 < appearCounter)
            {   // 隠し機能オープン告知
                string[] appearMessage = { "AUTO", "PILOT", "OPEN" };
                string msg = appearMessage[appearNumber-1];

                // 文字の位置
                int logoScale = mapwidth() / 5; // PILOTの文字長で決める
                cx = mapwidth() / 2 - msg.Length * logoScale / 2;
                cy = mapheight() / 2 - logoScale / 2;
                float alpha = (float)appearCounter/(float)INTEGRAL_RANGE; // 透明度
                // 文字表示
                g.DrawString(msg, cx, cy, Color.Red * alpha, logoScale);
            }

            g.spriteBatch.End();

            base.Draw(g);
        }
    }
}
