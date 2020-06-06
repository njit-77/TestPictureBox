using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace TestPictureBox
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }


        #region Field

        /// <summary>默认绘制点 实心圆大小(像素)</summary>
        private const float PiexlPoint = 8.0f;

        /// <summary>默认缩放步进</summary>
        private const double ZoomStep = 1.3;

        /// <summary>最小倍率</summary>
        private const double MinScale = 0.025; //1.0 / (1.3 ^ 14) = 0.025397622732539368

        /// <summary>最大倍率</summary>
        private const double MaxScale = 40; // (1.3 ^ 14) = 39.373763856992909

        /// <summary>默认比例系数</summary>
        private double ScaleXY = 1.0;

        /// <summary>当前倍率</summary>
        private double ZoomScale = 1.0;

        /// <summary>画面中心坐标 X (世界坐标值)</summary>
        private double CenterX = 0.0;

        /// <summary>画面中心坐标 Y (世界坐标值)</summary>
        private double CenterY = 0.0;

        /// <summary>鼠标左键按下记录点坐标</summary>
        private System.Drawing.Point MouseDownPosition;

        /// <summary>鼠标移动记录点坐标</summary>
        private System.Drawing.Point MouseMovePosition;

        /// <summary>Lime画笔</summary>
        private System.Drawing.Pen LimePen;

        /// <summary>Red画笔</summary>
        private System.Drawing.Pen RedPen;

        /// <summary>Blue画笔</summary>
        private System.Drawing.Pen BluePen;

        /// <summary>Red画刷</summary>
        private System.Drawing.SolidBrush RedBrush;

        /// <summary>待绘制点集合</summary>
        private List<Point> DrawPoints;

        /// <summary>字体格式</summary>
        private System.Drawing.Font DrawFont;

        #endregion


        #region PictureBox_Event

        private void PictureBox_MouseEnter(object sender, EventArgs e)
        {
            ((System.Windows.Forms.PictureBox)sender)?.Focus();
        }

        private void PictureBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (System.Windows.Forms.MouseButtons.Right == e.Button)
            {
                ZoomScale = 1.0;
                CenterX = 0.0;
                CenterY = 0.0;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control
                && System.Windows.Forms.MouseButtons.Left == e.Button)
            {
                MouseDownPosition.X = e.X;
                MouseDownPosition.Y = e.Y;
            }
        }

        private void PictureBox_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control
                && System.Windows.Forms.MouseButtons.Left == e.Button)
            {
                var x = (e.X - MouseDownPosition.X) * (ScaleXY / ZoomScale);
                var y = (e.Y - MouseDownPosition.Y) * (ScaleXY / ZoomScale);
                CenterX += -x;
                CenterY += y;
                MouseDownPosition = e.Location;
            }
            MouseMovePosition = e.Location;
            MouseMovePosition.Y = e.Y;
            ((System.Windows.Forms.PictureBox)sender)?.Refresh();
        }

        private void PictureBox_MouseWheelMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var oldX = 0.0;
                var oldY = 0.0;
                StreenToWorld(sender as System.Windows.Forms.PictureBox, e.X, e.Y, out oldX, out oldY);

                if (e.Delta > 0)
                {
                    ZoomScale *= ZoomStep;
                }
                else
                {
                    ZoomScale /= ZoomStep;
                }
                if (ZoomScale < MinScale)
                {
                    ZoomScale = MinScale;
                }
                if (ZoomScale > MaxScale)
                {
                    ZoomScale = MaxScale;
                }

                var newX = 0.0;
                var newY = 0.0;
                StreenToWorld(sender as System.Windows.Forms.PictureBox, e.X, e.Y, out newX, out newY);

                CenterX -= newX - oldX;
                CenterY -= newY - oldY;
            }
            ((System.Windows.Forms.PictureBox)sender)?.Refresh();
        }

        private void PictureBox_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var pictureBox = sender as System.Windows.Forms.PictureBox;
            if (pictureBox != null)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                Draw_Text(pictureBox, g);
                for (int i = 0, Count = DrawPoints.Count; i < Count; i++)
                {
                    Draw_Point(pictureBox, g, DrawPoints[i]);
                }
                Draw_CoordinateSystem(pictureBox, g);
            }
            ((System.Windows.Forms.PictureBox)sender)?.Refresh();
        }

        #endregion


        #region private Method

        private void Init()
        {
            LimePen = new System.Drawing.Pen(System.Drawing.Color.Lime, 1.5f);
            RedPen = new System.Drawing.Pen(System.Drawing.Color.Red, 1.5f);
            BluePen = new System.Drawing.Pen(System.Drawing.Color.Blue, 1.5f);
            RedBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, RedPen.Color));
            DrawFont = new System.Drawing.Font("Arial", 14.0f);

            var rd = new Random();
            DrawPoints = new List<Point>();
            for (int i = 0; i < 10; i++)
            {
                DrawPoints.Add(new Point(rd.Next(-300, 300), rd.Next(-300, 300)));
            }
        }

        /// <summary>
        /// 世界坐标 -> PictureBox坐标
        /// </summary>
        /// <param name="pictureBox">PictureBox控件</param>
        /// <param name="x1">世界坐标</param>
        /// <param name="y1">世界坐标</param>
        /// <param name="x2">PictureBox坐标</param>
        /// <param name="y2">PictureBox坐标</param>
        private void WorldToStreen(System.Windows.Forms.PictureBox pictureBox, double x1, double y1, out float x2, out float y2)
        {
            x2 = Convert.ToSingle((x1 - CenterX) / (ScaleXY / ZoomScale) + 0.5 * pictureBox.Width);
            y2 = Convert.ToSingle((CenterY - y1) / (ScaleXY / ZoomScale) + 0.5 * pictureBox.Height);
        }

        /// <summary>
        /// PictureBox坐标 -> 世界坐标
        /// </summary>
        /// <param name="pictureBox">PictureBox控件</param>
        /// <param name="x1">PictureBox坐标</param>
        /// <param name="y1">PictureBox坐标</param>
        /// <param name="x2">世界坐标</param>
        /// <param name="y2">世界坐标</param>
        private void StreenToWorld(System.Windows.Forms.PictureBox pictureBox, float x1, float y1, out double x2, out double y2)
        {
            x2 = CenterX + (x1 - 0.5 * pictureBox.Width) * (ScaleXY / ZoomScale);
            y2 = CenterY + (0.5 * pictureBox.Height - y1) * (ScaleXY / ZoomScale);
        }

        private void Draw_Text(System.Windows.Forms.PictureBox pictureBox, System.Drawing.Graphics g)
        {
            g.DrawString($"X:{MouseMovePosition.X}, Y:{MouseMovePosition.Y}", DrawFont, RedBrush, 10.0f, 10.0f);
        }

        /// <summary>
        /// 绘制坐标系
        /// </summary>
        /// <param name="g"></param>
        private void Draw_CoordinateSystem(System.Windows.Forms.PictureBox pictureBox, System.Drawing.Graphics g)
        {
            float x1, y1;
            var pa = new System.Drawing.PointF();
            var pb = new System.Drawing.PointF();

            // 绘制原点
            {
                WorldToStreen(pictureBox, 0, 0, out x1, out y1);

                g.DrawEllipse(RedPen, (float)(x1 - PiexlPoint * 0.5), (float)(y1 - PiexlPoint * 0.5), PiexlPoint, PiexlPoint);
                g.FillEllipse(RedBrush, (float)(x1 - PiexlPoint * 0.5), (float)(y1 - PiexlPoint * 0.5), PiexlPoint, PiexlPoint);
            }

            // 绘制 X 轴
            {
                pa.X = 0;
                pa.Y = 0;
                WorldToStreen(pictureBox, pa.X, pa.Y, out x1, out y1);
                pa.X = x1;
                pa.Y = y1;

                pb.X = 30;
                pb.Y = 0;
                WorldToStreen(pictureBox, pb.X, pb.Y, out x1, out y1);
                pb.X = x1;
                pb.Y = y1;

                g.DrawLine(RedPen, pa, pb);
            }

            // 绘制 Y 轴
            {
                pa.X = 0;
                pa.Y = 0;
                WorldToStreen(pictureBox, pa.X, pa.Y, out x1, out y1);
                pa.X = x1;
                pa.Y = y1;

                pb.X = 0;
                pb.Y = 30;
                WorldToStreen(pictureBox, pb.X, pb.Y, out x1, out y1);
                pb.X = x1;
                pb.Y = y1;

                g.DrawLine(BluePen, pa, pb);
            }
        }

        private void Draw_Point(System.Windows.Forms.PictureBox pictureBox, System.Drawing.Graphics g, System.Windows.Point pa)
        {
            float x1, y1;

            WorldToStreen(pictureBox, pa.X, pa.Y, out x1, out y1);

            g.DrawEllipse(RedPen, (float)(x1 - PiexlPoint * 0.5), (float)(y1 - PiexlPoint * 0.5), PiexlPoint, PiexlPoint);
            g.FillEllipse(RedBrush, (float)(x1 - PiexlPoint * 0.5), (float)(y1 - PiexlPoint * 0.5), PiexlPoint, PiexlPoint);
        }

        #endregion

    }
}
