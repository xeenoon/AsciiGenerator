using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;

namespace AsciiGenerator
{
    public partial class Form1 : Form
    {
        PictureBox preview = new PictureBox();
        RichTextBox rtb = new RichTextBox();
        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            Width = Screen.PrimaryScreen.Bounds.Width;
            Height = Screen.PrimaryScreen.Bounds.Height;

            preview.Width = Width/2;
            preview.Height = Height;
            preview.Click += OnClick;
            rtb.Location = new Point(Width/2, 0);
            rtb.Width = Width / 2;
            rtb.Height = Height;
            rtb.Font = new Font("Lucida Console", 8);
            Controls.Add(preview);
            Controls.Add(rtb);
        }
        public void OnClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
            {
                var bmp = Bitmap.FromFile(openFileDialog.FileName);
                preview.Image = bmp;
            }
            
            var areas = GenerateColorAreas();
            areas = areas.OrderByDescending(a=>a.colorsum).ToList();
            const string lighttodark = " `.-':_,^=;><+!rc*/z?sLTv)J7(|Fi{C}fI31tlu[neoZ5Yxjya]2ESwqkP6h9d4VpOGbUAKXHm8RD#$Bg0MNWQ%&@";
            int brightnessstepsize = lighttodark.Count() / areas.Count();
            StringBuilder result = new StringBuilder(string.Concat(Enumerable.Repeat(new string(' ', preview.Image.Width) + "\n", preview.Image.Height)));
            for (int i = 0; i < areas.Count; i++)
            {
                List<Point>? area = areas[i].pixels;
                foreach (var point in area)
                {
                    result[point.X + point.Y * (preview.Image.Height+1)] = lighttodark[i * brightnessstepsize];
                }
            }
            rtb.Text = result.ToString();
        }
        public struct PixelArea
        {
            public List<Point> pixels = new List<Point>();
            public Color color;
            public int colorsum;

            public PixelArea(Color color)
            {
                this.color = color;
                colorsum = color.A + color.R + color.G + color.B;
            }
        }
        public unsafe List<PixelArea> GenerateColorAreas()
        {
            List<PixelArea> areas = new List<PixelArea>();
            BitmapData lockedbits = ((Bitmap)preview.Image).LockBits(new Rectangle(0,0,preview.Image.Width, preview.Image.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            const int bytesperpixel = 4;

            for (int x = 0; x < preview.Image.Width; ++x)
            {
                for (int y = 0; y < preview.Image.Height; ++y)
                {
                    bool placedpoint = false;

                    byte* m_ptr = (byte*)(lockedbits.Scan0 + (x * bytesperpixel) + (y * lockedbits.Stride));
                    byte m_r = m_ptr[0];
                    byte m_g = m_ptr[1];
                    byte m_b = m_ptr[2];
                    byte m_a = m_ptr[3];
                    int m_sum = m_a + m_r + m_g + m_b;
                    if (m_a < 200) { continue; }
                    foreach (var area in areas)
                    {
                        var basecolor = area.pixels[0];

                        byte* area_ptr = (byte*)(lockedbits.Scan0 + (basecolor.X * bytesperpixel) + (basecolor.Y * lockedbits.Stride));
                        byte area_r = area_ptr[0];
                        byte area_g = area_ptr[1];
                        byte area_b = area_ptr[2];
                        byte area_a = area_ptr[3];
                        int area_sum = area_a + area_r + area_g + area_b;

                        if (Math.Abs(area_sum - m_sum) < 20) //Check the sums of the difference of the colors
                        {
                            //If the colors are siiliar, add myself to the area
                            area.pixels.Add(new Point(x, y));
                            placedpoint = true;
                            break;
                        }
                    }
                    if (!placedpoint) //Found no areas that fit
                    {
                        areas.Add(new PixelArea(Color.FromArgb(m_a, m_r, m_g, m_b)));
                        areas.Last().pixels.Add(new Point(x,y));
                    }
                }
            }
            ((Bitmap)preview.Image).UnlockBits(lockedbits);
            return areas;
        }
    }
}
