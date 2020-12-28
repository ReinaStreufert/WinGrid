using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinGrid
{
    public partial class Form1 : Form
    {
        public static IntPtr hwnd;
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect, // x-coordinate of upper-left corner
            int nTopRect, // y-coordinate of upper-left corner
            int nRightRect, // x-coordinate of lower-right corner
            int nBottomRect, // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
         );

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS                           // struct for box shadow
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        public enum WinGridMode : byte
        {
            Grid = 0,
            Layers = 1
        }


        Point mousePos = new Point(-1, -1);
        Point anchor = new Point(-1, -1);
        DateTime lastControlPress = new DateTime();
        Screen currentScreen;
        ScreenLayer currentScreenLayer;
        IntPtr targetWindow;
        WinGridMode currentMode = WinGridMode.Grid;
        bool thirdPress = false;

        List<ScreenLayer> ScreenLayers = new List<ScreenLayer>();

        const int divisions = 6;
        const float step = 400F / divisions;
        bool showing = false;
        public Form1()
        {
            int i = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                ScreenLayers.Add(new ScreenLayer(screen.DeviceName, i));
                i++;
            }
            InitializeComponent();
            this.DoubleBuffered = true;
            KeyboardHook.Callback = keyCall;
            KeyboardHook.Start();
        }
        private void keyCall(KeyboardEventType eventType, Keys keys, Reference<bool> prop)
        {
            if (eventType == KeyboardEventType.KeyUp)
            {
                if (keys == Keys.Control || keys == Keys.ControlKey || keys == Keys.LControlKey || keys == Keys.RControlKey)
                {
                    if ((DateTime.Now - lastControlPress).TotalMilliseconds < 500)
                    {
                        if (showing)
                        {
                            if (thirdPress)
                            {
                                currentMode = WinGridMode.Layers;
                                this.Invalidate();
                                thirdPress = false;
                            } else
                            {
                                showing = false;
                                anchor = new Point(-1, -1);
                                mousePos = new Point(-1, -1);
                                this.Visible = false;
                                
                            }
                        } else
                        {
                            currentMode = WinGridMode.Grid;
                            Point cursorPosition = Cursor.Position;
                            currentScreen = Screen.FromPoint(cursorPosition);
                            currentScreenLayer = ScreenLayers.Where((ScreenLayer sl) => { return (sl.ScreenDeviceName == currentScreen.DeviceName); }).First();
                            int x = cursorPosition.X;
                            int y = cursorPosition.Y;
                            if (x + 400 > currentScreen.Bounds.Right)
                            {
                                x -= 399;
                            }
                            if (y + 400 > currentScreen.Bounds.Bottom)
                            {
                                y -= 399;
                            }
                            this.Location = new Point(x, y);

                            targetWindow = GetForegroundWindow();
                            showing = true;
                            this.Visible = true;
                            thirdPress = true;
                        }
                    } else
                    {
                        thirdPress = false;
                    }
                    lastControlPress = DateTime.Now;
                }
            }
        }
        private Rectangle pointsToRect(int startX, int startY, int currentX, int currentY)
        {
            if (startX < 0)
            {
                startX = 0;
            }
            if (startX >= divisions)
            {
                startX = divisions - 1;
            }
            if (startY < 0)
            {
                startY = 0;
            }
            if (startY >= divisions)
            {
                startY = divisions - 1;
            }
            if (currentX < 0)
            {
                currentX = 0;
            }
            if (currentX >= divisions)
            {
                currentX = divisions - 1;
            }
            if (currentY < 0)
            {
                currentY = 0;
            }
            if (currentY >= divisions)
            {
                currentY = divisions - 1;
            }
            int rX = Math.Min(startX, currentX);
            int rY = Math.Min(startY, currentY);
            int rWidth = Math.Abs(startX - currentX) + 1;
            int rHeight = Math.Abs(startY - currentY) + 1;
            return new Rectangle(rX, rY, rWidth, rHeight);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(25, 25, 25));
            if (currentMode == WinGridMode.Grid)
            {
                if (mousePos != new Point(-1, -1) && anchor == new Point(-1, -1))
                {
                    int x = (int)Math.Floor(mousePos.X / step);
                    int y = (int)Math.Floor(mousePos.Y / step);
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(255, 20, 100)))
                    {
                        e.Graphics.FillRectangle(sb, x * step, y * step, step - 1, step - 1);
                    }
                }
                else if (mousePos != new Point(-1, -1) && anchor != new Point(-1, -1))
                {
                    int startX = (int)Math.Floor(anchor.X / step);
                    int startY = (int)Math.Floor(anchor.Y / step);
                    int currentX = (int)Math.Floor(mousePos.X / step);
                    int currentY = (int)Math.Floor(mousePos.Y / step);

                    Rectangle r = pointsToRect(startX, startY, currentX, currentY);

                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(255, 20, 100)))
                    {
                        e.Graphics.FillRectangle(sb, r.X * step, r.Y * step, r.Width * step - 1, r.Height * step - 1);
                    }
                }
                using (Pen p = new Pen(Color.FromArgb(25, 25, 25), 1F))
                {
                    for (int i = 1; i <= divisions - 1; i++)
                    {
                        int x = (int)((400F / divisions) * i);
                        e.Graphics.DrawLine(p, new Point(x, 0), new Point(x, this.Height));
                    }
                    for (int i = 1; i <= divisions - 1; i++)
                    {
                        int y = (int)((400F / divisions) * i);
                        e.Graphics.DrawLine(p, new Point(0, y), new Point(this.Width, y));
                    }
                }
            } else if (currentMode == WinGridMode.Layers)
            {
                int layerCount = currentScreenLayer.Layers.Count;
                int currentLayerIndex = currentScreenLayer.CurrentLayer;
                using (Font font = new Font("Segoe UI Light", 100, GraphicsUnit.Pixel))
                {
                    using (SolidBrush sb = new SolidBrush(Color.White))
                    {
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            e.Graphics.DrawString((currentLayerIndex + 1).ToString(), font, sb, this.ClientRectangle, sf);
                        }
                    }
                }

                if (layerCount > 1)
                {
                    int y = 300;
                    int x = 200 - ((5 * layerCount) + (2 * (layerCount - 1)));
                    using (SolidBrush gray = new SolidBrush(Color.FromArgb(60, 60, 60)))
                    {
                        using (SolidBrush pink = new SolidBrush(Color.FromArgb(255, 20, 100)))
                        {
                            for (int i = 0; i < layerCount; i++)
                            {
                                if (i == currentLayerIndex)
                                {
                                    e.Graphics.FillEllipse(pink, new Rectangle(x - 1, y - 1, 12, 12));
                                    x += 14;
                                }
                                else
                                {
                                    e.Graphics.FillEllipse(gray, new Rectangle(x, y, 10, 10));
                                    x += 14;
                                }
                            }
                        }
                    }
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (currentMode == WinGridMode.Grid)
            {
                mousePos = e.Location;
                this.Invalidate();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            mousePos = new Point(-1, -1);
            if (anchor == new Point(-1, -1))
            {
                showing = false;
                anchor = new Point(-1, -1);
                mousePos = new Point(-1, -1);
                this.Visible = false;
            }
            this.Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (currentMode == WinGridMode.Grid)
            {
                base.OnMouseDown(e);
                anchor = e.Location;
                this.Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (currentMode == WinGridMode.Grid)
            {
                base.OnMouseUp(e);

                int startX = (int)Math.Floor(anchor.X / step);
                int startY = (int)Math.Floor(anchor.Y / step);
                int currentX = (int)Math.Floor(mousePos.X / step);
                int currentY = (int)Math.Floor(mousePos.Y / step);

                Rectangle r = pointsToRect(startX, startY, currentX, currentY);

                ShowWindow(targetWindow, 1);
                float screenXStep = currentScreen.WorkingArea.Width / (float)divisions;
                float screenYStep = currentScreen.WorkingArea.Height / (float)divisions;
                //Console.WriteLine("XStep: " + screenXStep + " YStep: " + screenYStep);
                //Console.WriteLine("X: " + (int)(r.X * screenXStep) + " Y: " + (int)(r.Y * screenYStep) + " Width: " + (int)(r.Width * screenXStep) + " Height: " + (int)(r.Height * screenYStep));
                //Cursor.Position = new Point((int)(r.X * screenXStep), (int)(r.Y * screenYStep));
                Rectangle windowMargins = WindowUtils.GetWindowMargins(targetWindow);
                Console.WriteLine(windowMargins);
                if (windowMargins.X == 7)
                {
                    windowMargins.X++;
                    windowMargins.Y++;
                    windowMargins.Width += 2;
                    windowMargins.Height += 2;
                }
                WindowUtils.SetWindowPos(targetWindow, new IntPtr(0), ((int)(r.X * screenXStep) - windowMargins.X) + currentScreen.Bounds.X, ((int)(r.Y * screenYStep) - windowMargins.Y) + currentScreen.Bounds.Y, (int)(r.Width * screenXStep) + windowMargins.Width, (int)(r.Height * screenYStep) + windowMargins.Height, WindowUtils.SWP.NOZORDER);
                if (r.Width >= divisions && r.Height >= divisions)
                {
                    ShowWindow(targetWindow, 3);
                }
                this.Visible = false;
                showing = false;
                anchor = new Point(-1, -1);
            }
        }
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.KeyCode == Keys.Right)
            {
                if (currentScreenLayer.Layers.Count > 1)
                {
                    currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].SaveCurrentState();
                    currentScreenLayer.CurrentLayer++;
                    if (currentScreenLayer.CurrentLayer >= currentScreenLayer.Layers.Count)
                    {
                        currentScreenLayer.CurrentLayer = 0;
                    }
                    currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].RestoreState();
                    this.Invalidate();

                    this.BringToFront();
                }
            } else if (e.KeyCode == Keys.Left)
            {
                if (currentScreenLayer.Layers.Count > 1)
                {
                    currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].SaveCurrentState();
                    currentScreenLayer.CurrentLayer--;
                    if (currentScreenLayer.CurrentLayer < 0)
                    {
                        currentScreenLayer.CurrentLayer = currentScreenLayer.Layers.Count - 1;
                    }
                    currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].RestoreState();
                    this.Invalidate();

                    this.BringToFront();
                }
            } else if (e.KeyCode == Keys.Oemplus)
            {
                currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].SaveCurrentState();
                currentScreenLayer.Layers.Add(new Layer());
                currentScreenLayer.CurrentLayer = currentScreenLayer.Layers.Count - 1;
                currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].SaveCurrentState();
                this.Invalidate();
                this.BringToFront();
            } else if (e.KeyCode == Keys.OemMinus)
            {
                if (currentScreenLayer.Layers.Count > 1)
                {
                    currentScreenLayer.Layers.RemoveAt(currentScreenLayer.CurrentLayer);
                    if (currentScreenLayer.CurrentLayer >= currentScreenLayer.Layers.Count)
                    {
                        currentScreenLayer.CurrentLayer--;
                    }
                    currentScreenLayer.Layers[currentScreenLayer.CurrentLayer].RestoreState();
                    this.Invalidate();
                    this.BringToFront();
                }
            }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            hwnd = this.Handle;
            int v = 2;
            DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
            MARGINS margins = new MARGINS()
            {
                bottomHeight = 2,
                leftWidth = 2,
                rightWidth = 2,
                topHeight = 2
            };
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            this.TopMost = true;
            this.Visible = showing;
        }

    }
}
