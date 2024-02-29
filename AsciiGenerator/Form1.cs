using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;

namespace AsciiGenerator
{
    public partial class Form1 : Form
    {
        RichTextBox rtb = new RichTextBox();
        TextBox asciicharacters = new TextBox();
        Label asciicharacterlabel = new Label();
        Button uploadimage = new Button();
        Button resetascii = new Button();

        Panel colorareas =new Panel();
        List<Panel> areaitems = new List<Panel>();
        List<PixelArea> areas = new List<PixelArea>();

        Bitmap image;
        const string lighttodark = " `.-':_,^=;><+!rc*/z?sLTv)J7(|Fi{C}fI31tlu[neoZ5Yxjya]2ESwqkP6h9d4VpOGbUAKXHm8RD#$Bg0MNWQ%&@";

        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            Width = Screen.PrimaryScreen.Bounds.Width;
            Height = Screen.PrimaryScreen.Bounds.Height;

            uploadimage.Location = new Point(0,0);
            uploadimage.Size = new Size(100,30);
            uploadimage.Text = "Upload image";
            uploadimage.Click += OnClick;

            resetascii.Location = new Point(220, 0);
            resetascii.Size = new Size(100,30);
            resetascii.Text = "Reload";
            resetascii.Click += ResetASCII;

            rtb.Location = new Point(Width/4, 0);
            rtb.Width = (Width / 4) * 3;
            rtb.Height = Height;
            rtb.Font = new Font("Lucida Console", 5);

            asciicharacterlabel.Location = new Point(0, 35);
            asciicharacterlabel.Text = "Gradient colors: ";

            asciicharacters.Location = new Point(0,65);
            asciicharacters.Size = new Size(Width/4 - 10, 30);
            asciicharacters.Text = lighttodark;

            //Panel to show all the color areas and allow users to change the character customizeably
            colorareas.Location = new Point(0, 100);
            colorareas.Width = (Width / 4) - 10;
            colorareas.Height = Height - 110;
            colorareas.BackColor = Color.White;
            colorareas.AutoScroll = true;

            Controls.Add(rtb);
            Controls.Add(resetascii);
            Controls.Add(colorareas);
            Controls.Add(asciicharacterlabel);
            Controls.Add(asciicharacters);
            Controls.Add(uploadimage);
        }
        public void OnClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
            {
                var bmp = Bitmap.FromFile(openFileDialog.FileName);
                image = (Bitmap)bmp;
            }
            
            areas = GenerateColorAreas();
            areas = areas.OrderByDescending(a=>a.colorsum).ToList();
            float brightnessstepsize = asciicharacters.Text.Count() / (float)areas.Count();
            StringBuilder result = new StringBuilder(string.Concat(Enumerable.Repeat(new string(' ', image.Width) + "\n", image.Height)));
            for (int i = 0; i < areas.Count; i++)
            {
                List<Point>? area = areas[i].pixels;
                foreach (var point in area)
                {
                    result[point.X + point.Y * (image.Width + 1)] = asciicharacters.Text[(int)(i * brightnessstepsize)];
                }

                Panel coloroptionspanel = new Panel();
                coloroptionspanel.Location = new Point(0, areaitems.Count() * 60 + 10);
                coloroptionspanel.Size = new Size(colorareas.Width - 20, 50);
                coloroptionspanel.BackColor = Color.LightBlue;
                
                Label identifier = new Label();
                identifier.Location = new Point(0,0);
                identifier.Text = "Color area " + (i+1) + ":";
                TextBox asciiletter = new TextBox();
                asciiletter.Location = new Point(identifier.Size.Width, 0);
                asciiletter.Size = new Size(30, identifier.Height);
                asciiletter.Text = asciicharacters.Text[(int)(i * brightnessstepsize)].ToString();
                CheckBox highlighttext = new CheckBox();
                highlighttext.Location = new Point(identifier.Size.Width + 40);
                highlighttext.Size = new Size(200,identifier.Height);
                highlighttext.Text = "Highlight area";
                highlighttext.Click += HighlightAreaToggle;

                coloroptionspanel.Controls.Add(identifier);
                coloroptionspanel.Controls.Add(asciiletter);
                coloroptionspanel.Controls.Add(highlighttext);
                areaitems.Add(coloroptionspanel);
                colorareas.Controls.Add(coloroptionspanel);
            }
            rtb.Text = result.ToString();
        }
        public void HighlightAreaToggle(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
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
        public void ResetASCII(object sender, EventArgs e)
        {
            StringBuilder result = new StringBuilder(string.Concat(Enumerable.Repeat(new string(' ', image.Width) + "\n", image.Height)));

            for (int i = 0; i < areas.Count; i++)
            {
                List<Point>? area = areas[i].pixels;
                foreach (var point in area)
                {
                    result[point.X + point.Y * (image.Height + 1)] = areaitems[i].Controls[1].Text[0];
                }
            }

            rtb.Text = result.ToString();
        }
        public unsafe List<PixelArea> GenerateColorAreas()
        {
            List<PixelArea> areas = new List<PixelArea>();
            BitmapData lockedbits = (image).LockBits(new Rectangle(0,0,image.Width, image.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            const int bytesperpixel = 4;

            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
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
            image.UnlockBits(lockedbits);
            return areas;
        }
    }
}
