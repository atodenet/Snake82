using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Atode;
using System;
using System.Diagnostics;

namespace Snake82
{
    public class Game1 : Game
    {
        private const string _titleName = "Test2";
        private const int CEL_WIDTH_START = 18;       // 横キャラクター数の初期値
        private const int CEL_HEIGHT_START = 10;      // 縦キャラクター数の初期値
        private const int PAUSE_INVISIVLE_TIME = 120;   // PAUSE表示を消す期間
        private int _pauseinvisible;

        public GraphicsDeviceManager graphics;
        public SpriteBatch spriteBatch;
        public Input inp;
        public Screen scr;
        public SaveData save;
        public Texture2D fonts = null;
        public Texture2D txgameover = null;
        public Random rand;
        private bool _resized = false;
        private Viewport _viewedge;
        private BasicEffect effect;
        private VertexPositionColor[] vertexpFullscreen;

        // シーン関係
        private int _scheneNo = (int)Scn.None;
        private SceneBoot _sceneboot;
        private SceneTitle _scenetitle;
        private SceneGame _scenegame;

        delegate void DeleUpdate(Game1 game);
        private DeleUpdate UpdateScene;
        delegate void DeleDraw(Game1 game);
        private DeleDraw DrawScene;
        delegate int DeleNext();
        private DeleNext NextScene;
        delegate Color DeleBackColor();
        private DeleBackColor BackColor;

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
            scr = new Screen();
            save = new SaveData(_titleName);

            _sceneboot = new SceneBoot();
            _scenetitle = new SceneTitle();
            _scenegame = new SceneGame();
            SetScene((int)Scn.Boot);

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
        }
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            // セーブデータをロードする Screen初期化より前に
            save.Load();
            Debug.WriteLine("SaveData " + save.width + " x " + save.height);
            // Screen初期化 （ウィンドウサイズを指定）
            scr.Init(this,save.width,save.height);

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

                if (inp.Get((int)Key.Back) && 
                    (_scheneNo == (int)Scn.Title || _scheneNo == (int)Scn.Boot))
                {   // タイトル画面でBackボタンを押されたらプログラム終了
                    OnExit();
                    Exit();
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

                if ( inp.bPause == false)
                {   // シーンに関係なく、ポーズ中でない場合のみゲームは進行する。
                    UpdateScene(this);
                }
                if(inp.Get((int)Key.LB) == true){
                    _pauseinvisible = PAUSE_INVISIVLE_TIME;
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
            if( inp.bPause)
            {
                if( 0 < _pauseinvisible)
                {
                    _pauseinvisible--;
                }
                else
                {
                    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    // 文字の位置
                    int cx = celwidth() / 2 - 3;
                    int cy = celheight() / 2;
                    // 背景の黒箱描画
                    Rectangle rect;
                    rect = scr.TextBox(cx - 1, cy);
                    rect.Width += scr.TextWidth(6, cx - 1);
                    Primitive.FillRectangle(spriteBatch, rect, Color.MidnightBlue);
                    // 文字表示
                    DrawString("PAUSE", cx, cy, Color.White);
                    spriteBatch.End();
                }
            }

            // フルスクリーン時、余白を黒で塗りつぶす
            ClearEdge();
            base.Draw(gameTime);
        }

        private void SetScene(int sceneNo)
        {   // ゲームシーンの切り替え タイトル画面、ゲーム画面、config画面、ランキング画面等
            switch (sceneNo)
            {
                case (int)Scn.None:
                    // 何もしない（デフォルト）
                    return;
                case (int)Scn.Game:
                    // ゲームへ
                    UpdateScene = _scenegame.Update;
                    DrawScene = _scenegame.Draw;
                    NextScene = _scenegame.Next;
                    BackColor = _scenegame.BackColor;
                    scr.CelInit(CEL_WIDTH_START,CEL_HEIGHT_START);
                    _scenegame.Init(this);
                    Debug.WriteLine("Scene Game");
                    break;
                case (int)Scn.Boot:
                    // 起動画面
                    UpdateScene = _sceneboot.Update;
                    DrawScene = _sceneboot.Draw;
                    NextScene = _sceneboot.Next;
                    BackColor = _sceneboot.BackColor;
                    scr.CelInit();  // ここはデフォルトサイズで
                    _sceneboot.Init(this);
                    Debug.WriteLine("Scene Boot");
                    break;
                case (int)Scn.Title:
                default:
                    // タイトル画面へ
                    UpdateScene = _scenetitle.Update;
                    DrawScene = _scenetitle.Draw;
                    NextScene = _scenetitle.Next;
                    BackColor = _scenetitle.BackColor;
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
            save.Save(scr.windowwidth(), scr.windowheight());
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

        public void DrawString(String str, int x, int y, Color color, int xoffset, int yoffset)
        {
            char code;
            Rectangle rect;
            
            for(int i=0; i<str.Length; i++)
            {
                rect = scr.TextBox(x+i, y,xoffset, yoffset);
                code = str[i];
                spriteBatch.Draw(fonts, rect, Font(code), color);

            }
        }

        public void DrawString(String str, int x, int y, Color color)
        {
            DrawString(str, x, y, color, 0, 0);
        }

        }
    }
