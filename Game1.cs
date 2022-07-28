using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Atode;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Snake82
{
    public class Game1 : Game
    {
        public const string copyright = "Copyright(c)2022 metys";
        private const string _titleName = "Snake82";
        private const int CEL_WIDTH_START = 18;       // 横キャラクター数の初期値
        private const int CEL_HEIGHT_START = 10;      // 縦キャラクター数の初期値
        private const int PAUSE_INVISIVLE_TIME = 120;   // PAUSE表示を消す期間
        private int _pauseinvisible;
        private int _pauselogo;
        public const int SPEED_DEFAULT = 10;        // ゲームのスピード標準値
        public int speedRate = SPEED_DEFAULT;       // ゲームのスピード
        public int speedZoom = 1;                   // 速度倍率

        public GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        public Input inp;
        public Vga scr;     // Screen情報（ゲーム画面の解像度、キャラクタ画面の情報）
        public RecordData rec;
        public Texture2D fonts = null;
        public Texture2D txgameover = null;
        public Random rand;
        
        private bool _resized = false;
        private Viewport _viewedge;
        private BasicEffect effect;
        private VertexPositionColor[] vertexpFullscreen;


        // オートパイロット関係
        public bool autopilotVisible = false;       // ユーザーにこの機能を公開するか
        public bool autopilotEnable = false;        // ユーザーによるオートパイロットのON/OFF
        public bool autopilotAppear = false;        // ユーザー告知

        // シーン関係
        private Scn _scheneNo = Scn.None;
        private SceneBoot _sceneboot;
        private SceneTitle _scenetitle;
        private SceneGame _scenegame;

        delegate void DeleUpdate(Game1 game);
        private DeleUpdate UpdateScene;
        delegate void DeleDraw(Game1 game);
        private DeleDraw DrawScene;
        delegate Scn DeleNext();
        private DeleNext NextScene;
        delegate Color DeleBackColor();
        private DeleBackColor BackColor;

        // デバッグ用
        public bool collisionVisible = false;       // コリジョン可視化フラグ
        public List<Rectangle> hitRect;

        public int celwidth() { return scr.celwidth(); }
        public int celheight() { return scr.celheight(); }

        void WindowSizeChanged(object sender, EventArgs e)
        {   // イベントによって呼び出される（イベントハンドラー）
            _resized = true;
            Debug.WriteLine("Window Size Changed ");
        }
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            rand = new Random();
            inp = new Input();
            scr = new Vga();
            rec = new RecordData(_titleName);

            _sceneboot = new SceneBoot();
            _scenetitle = new SceneTitle();
            _scenegame = new SceneGame();
            SetScene(Scn.Boot);

            Window.AllowUserResizing = true;
            // _scrインスタンス生成後にイベントハンドラーを登録する ユーザーによるウィンドウサイズ変更
            Window.ClientSizeChanged += WindowSizeChanged;

            //_graphics.IsFullScreen = true;

            // 画面全体を覆うサイズの黒い四角形を定義
            vertexpFullscreen = new VertexPositionColor[] {
            new VertexPositionColor(new Vector3(-1F, 1F, 0), Color.Black),
            new VertexPositionColor(new Vector3(1F, 1F, 0), Color.Black),
            new VertexPositionColor(new Vector3(-1F, -1F, 0), Color.Black),
            new VertexPositionColor(new Vector3(1F, -1F, 0), Color.Black),
            };
            // デバッグ用
            hitRect = new List<Rectangle>();
        }
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            // セーブデータをロードする Screen初期化より前に
            rec.Load();
            Debug.WriteLine("SaveData " + rec.width + " x " + rec.height);
            // Screen初期化 （ウィンドウサイズを指定）
            scr.Init(this, rec.width, rec.height, rec.fullscreen);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            fonts = Content.Load<Texture2D>("font16snake");
            txgameover = Content.Load<Texture2D>("gameover9");
            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;
        }

        protected override void Update(GameTime gameTime)
        {   // ゲーム進行はすべてここで行われる（画面描画を除く）
            // TODO: Add your update logic here
            if (IsActive)
            {   // アクティブウィンドウでなければゲーム停止
                // ユーザー操作をここで入力
                inp.Update(graphics.IsFullScreen);

                if (inp.Get(Key.Back))
                {
                    if(_scheneNo == Scn.Title || _scheneNo == Scn.Boot)
                    {   // タイトル画面でBackボタンを押されたらプログラム終了
                        SetScene(Scn.Quit);
                    }
                    if (_scheneNo == Scn.Game)
                    {  // Backボタンでゲーム終了、タイトルへ（どのフェーズでも）
                        SetScene( Scn.Title);
                        if (inp.bPause == true)
                        {   // ポーズ中だとUpdateをしないから画面が切り替わらない。ここでUpdate
                            UpdateScene(this);
                        }
                    }
                }
                if (inp.bFullscreen)
                {   // 全画面化
                    inp.bFullscreen = false;
                    scr.ToggleFull(this);

                }
                if (inp.bScreensize)
                {   // 画面サイズ変更
                    inp.bScreensize = false;
                    scr.ToggleSize(this);
                }
                // システム系操作完了

                if (inp.Get(Key.Pup))
                {   // デバッグ表示用
                    collisionVisible = !collisionVisible;
                }

                if ( inp.bPause == false)
                {   // シーンに関係なく、ポーズ中でない場合のみゲームは進行する。
                    UpdateScene(this);
                }

                // ポーズ中の表示変更（二段階）ShiftキーとゲームパッドLBボタン
                if(inp.Get(Key.LB) == true){
                    if (_pauselogo == 0)
                    {
                        _pauselogo = PAUSE_INVISIVLE_TIME;
                        _pauseinvisible = 0;
                    }
                    else if ( _pauseinvisible == 0)
                    {
                        _pauseinvisible = PAUSE_INVISIVLE_TIME;
                        _pauselogo = 0;
                    }
                }

                if (inp.Sense(Key.A))
                {   // Aボタン/Z/Enterで一時的スピードアップ
                    speedZoom = 2;
                }
                else
                {
                    speedZoom = 1;
                }

                // 時間カウンタ
                if (0 < _pauselogo)
                {
                    _pauselogo--;
                }
                if (0 < _pauseinvisible)
                {
                    _pauseinvisible--;
                }
            }

            // シーンの切り替えが発生したのであればシーン切り替え
            SetScene(NextScene());

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {   // 画面描画（マイフレーム必ず呼ばれるわけではない。描画は時間がかかるのでDraw()はスキップされることもある）
            if( _resized)
            {// ユーザーによってウィンドウサイズが変更された
                _resized = false;
                scr.Resize(this);
            }
            // 背景塗りつぶし
            GraphicsDevice.Clear(BackColor());

            // 画面の縦横サイズはここでセット
            // フルスクリーン時、ゲーム画面の縦x横比率と合っていなければ余白が発生する（余白は除いたビュー座標系をセット）
            scr.SetViewport(this);

            // ドット絵をボケなくしたいが、これは直接用であってspritebatch用ではない
            //GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            // シーン描画
            DrawScene(this);

            // ポーズ中表示
            if( inp.bPause )
            {
                if( 0 < _pauseinvisible)
                {
                    // 何も描画しない;
                }
                else if (0 < _pauselogo)
                {   // ポーズ中のゲーム名オーバーレイ
                    const string promoLogo = "Snake82";
                    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    // 文字の位置
                    int logoScale = celwidth() / promoLogo.Length;
                    int cx = celwidth() / 2 - promoLogo.Length * logoScale/2;
                    int cy = celheight() / 2 - logoScale/2;
                    float alpha = 0.7f; // 透明度
                    // 文字表示
                    DrawString(promoLogo, cx, cy, Color.HotPink*alpha,logoScale);
                    spriteBatch.End();
                }
                else
                {   // 通常のポーズ中表示
                    const string pauseLogo = " PAUSE ";
                    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    // 文字の位置
                    int cx = celwidth() / 2 - pauseLogo.Length/2;
                    int cy = celheight() / 2;
                    // 背景の黒箱描画
                    Rectangle rect;
                    rect = scr.TextBox(cx, cy, pauseLogo.Length, 1);
                    Primitive.FillRectangle(spriteBatch, rect, Color.MidnightBlue);
                    // 文字表示
                    DrawString(pauseLogo, cx, cy, Color.White);
                    spriteBatch.End();
                }
            }

            // フルスクリーン時、余白を黒で塗りつぶす
            ClearEdge();
            base.Draw(gameTime);
        }

        private void SetScene(Scn sceneNo)
        {   // ゲームシーンの切り替え タイトル画面、ゲーム画面、config画面、ランキング画面等
            switch (sceneNo)
            {
                case Scn.None:
                    // 何もしない（デフォルト）
                    return;
                case Scn.Game:
                    // ゲームへ
                    UpdateScene = _scenegame.Update;
                    DrawScene = _scenegame.Draw;
                    NextScene = _scenegame.Next;
                    BackColor = _scenegame.BackColor;
                    inp.SetReactionFast(true);
                    scr.CelInit(CEL_WIDTH_START,CEL_HEIGHT_START);
                    speedZoom = 1;
                    _scenegame.Init(this);
                    Debug.WriteLine("Scene Game");
                    break;
                case Scn.Boot:
                    // 起動画面
                    UpdateScene = _sceneboot.Update;
                    DrawScene = _sceneboot.Draw;
                    NextScene = _sceneboot.Next;
                    BackColor = _sceneboot.BackColor;
                    scr.CelInit();  // ここはデフォルトサイズで
                    _sceneboot.Init(this);
                    Debug.WriteLine("Scene Boot");
                    break;
                case Scn.Quit:
                    // プログラムを終了させる
                    OnExit();
                    Exit();
                    break;
                case Scn.Title:
                default:
                    // タイトル画面へ
                    UpdateScene = _scenetitle.Update;
                    DrawScene = _scenetitle.Draw;
                    NextScene = _scenetitle.Next;
                    BackColor = _scenetitle.BackColor;
                    inp.SetReactionFast(false);
                    scr.CelInit(CEL_WIDTH_START, CEL_HEIGHT_START);
                    _scenetitle.Init(this);
                    Debug.WriteLine("Scene Title");
                    break;
            }
            _scheneNo = sceneNo;
        }

        private void OnExit()
        { // プログラム終了時の処理
            // ゲームデータのセーブ
            rec.Save(scr.windowwidth(), scr.windowheight(), graphics.IsFullScreen);
        }

        private void ClearEdge()
        {// フルスクリーン時、余白を黒で塗りつぶす
            if (graphics.IsFullScreen)
            {
                scr.CalcEdge(GraphicsDevice);
                if (0 < scr.edgeWidth || 0 < scr.edgeHeight)
                {
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        if (0 < scr.edgeWidth)
                        {   // 左右の画面外余白を塗りつぶす
                            _viewedge.X = 0;
                            _viewedge.Y = 0;
                            _viewedge.Width = scr.edgeWidth / 2;
                            _viewedge.Height = GraphicsDevice.PresentationParameters.BackBufferHeight;
                            GraphicsDevice.Viewport = _viewedge;
                            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexpFullscreen, 0, 2);
                            _viewedge.X = GraphicsDevice.PresentationParameters.BackBufferWidth - scr.edgeWidth / 2;
                            _viewedge.Y = 0;
                            _viewedge.Width = scr.edgeWidth / 2;
                            _viewedge.Height = GraphicsDevice.PresentationParameters.BackBufferHeight;
                            GraphicsDevice.Viewport = _viewedge;
                            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexpFullscreen, 0, 2);
                        }
                        if (0 < scr.edgeHeight)
                        {   // 上下の画面外余白を塗りつぶす
                            _viewedge.X = 0;
                            _viewedge.Y = 0;
                            _viewedge.Width = GraphicsDevice.PresentationParameters.BackBufferWidth;
                            _viewedge.Height = scr.edgeHeight / 2;
                            GraphicsDevice.Viewport = _viewedge;
                            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexpFullscreen, 0, 2);
                            _viewedge.X = 0;
                            _viewedge.Y = GraphicsDevice.PresentationParameters.BackBufferHeight - scr.edgeHeight / 2;
                            _viewedge.Width = GraphicsDevice.PresentationParameters.BackBufferWidth;
                            _viewedge.Height = scr.edgeHeight / 2;
                            GraphicsDevice.Viewport = _viewedge;
                            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexpFullscreen, 0, 2);
                        }
                    }
                }
            }
        }
        
        public Rectangle Font(char code)
        {   // フォント画像上の、指定された文字コードの位置を返す（フォントは複数の文字が1画像にまとまっている）
            Rectangle area;
            int no = (int)code;

            area.X = (no % 16) * 16;
            area.Y = (no / 16) * 16;
            area.Width = 16;
            area.Height = 16;

            return area;
        }

        // 文字のフォントサイズの何ドット分かずらして文字列描画 文字の影や縁取りの表現に使う
        public void DrawStringOffset(string str, int x, int y, Color color, int xoffset, int yoffset)
        {
            char code;
            Rectangle rect;
            
            for(int i=0; i<str.Length; i++)
            {
                rect = scr.TextBoxOffset(x+i, y,xoffset, yoffset);
                code = str[i];
                spriteBatch.Draw(fonts, rect, Font(code), color);

            }
        }

        // 文字列描画 文字サイズ倍率指定あり
        // scaleは文字の大きさ倍率
        public void DrawString(string str, int x, int y, Color color,int scale)
        {
            char code;
            Rectangle rect;

            for (int i = 0; i < str.Length; i++)
            {
                rect = scr.TextBox(x + i*scale, y,scale,scale);
                code = str[i];
                spriteBatch.Draw(fonts, rect, Font(code), color);
            }
        }

        // 標準の文字サイズで文字列描画
        public void DrawString(string str, int x, int y, Color color)
        {
            DrawString(str, x, y, color, 1);
        }

    }
}
