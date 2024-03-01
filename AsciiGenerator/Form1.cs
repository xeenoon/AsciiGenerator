using System.Diagnostics.Eventing.Reader;
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
        Label scalelabel = new Label();
        TextBox scalevalue = new TextBox();

        Panel colorareas =new Panel();
        List<Panel> areaitems = new List<Panel>();
        List<PixelArea> areas = new List<PixelArea>();
        Button resetchars = new Button();

        Bitmap image;
        const string lighttodark = " `.-':_,^=;><+!rc/z?sLTv)J7(|Fi{C}fI31tlu[neoZ5Yxjya]2ESwqkP6h9d4VpOGbUAKXHm8RD#$Bg0MNWQ%&@";

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
            resetchars.Location = new Point(150, 35);
            resetchars.Size = new Size(70, 20);
            resetchars.Text = "Reset";
            resetchars.Click += ResetCharsClick;

            asciicharacters.Location = new Point(0,55);
            asciicharacters.Size = new Size(Width/4 - 10, 30);
            asciicharacters.Text = lighttodark;

            scalelabel.Location = new Point(0,95);
            scalelabel.Text = "Area color distance";

            scalevalue.Location = new Point(0, 115);
            scalevalue.Size = new Size(50, 30);
            scalevalue.Text = "0.05";

            //Panel to show all the color areas and allow users to change the character customizeably
            colorareas.Location = new Point(0, 160);
            colorareas.Width = (Width / 4) - 10;
            colorareas.Height = Height - 230;
            colorareas.BackColor = Color.White;
            colorareas.AutoScroll = true;

            Controls.Add(rtb);
            Controls.Add(resetchars);
            Controls.Add(resetascii);
            Controls.Add(colorareas);
            Controls.Add(scalelabel);
            Controls.Add(scalevalue);
            Controls.Add(asciicharacterlabel);
            Controls.Add(asciicharacters);
            Controls.Add(uploadimage);
        }
        public void ResetCharsClick(object sender, EventArgs e)
        {
            asciicharacters.Text = lighttodark;
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
            areas = areas.OrderByDescending(a=>a.color.GetBrightness()).ToList();
            float brightnessstepsize = asciicharacters.Text.Count() / (float)areas.Count();
            for (int i = 0; i < areas.Count; i++)
            {
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

                coloroptionspanel.Controls.Add(identifier);
                coloroptionspanel.Controls.Add(asciiletter);
                coloroptionspanel.Controls.Add(highlighttext);
                areaitems.Add(coloroptionspanel);
                colorareas.Controls.Add(coloroptionspanel);
            }
            ResetASCII(null,null);
        }
        public void HighlightAreaToggle(object sender, EventArgs e)
        {
            ResetASCII(sender, e);
        }
        public struct PixelArea
        {
            public List<Point> pixels = new List<Point>();
            public Color color;

            public PixelArea(Color color)
            {
                this.color = color;
            }
        }
        public void ResetASCII(object sender, EventArgs e)
        {
            rtb.Text = "";
            StringBuilder result = new StringBuilder(string.Concat(Enumerable.Repeat(new string(' ', image.Width) + "\n", image.Height)));
            List<int> redletteridxs = new List<int>();
            for (int i = 0; i < areas.Count; i++)
            {
                List<Point>? area = areas[i].pixels;
                foreach (var point in area)
                {
                    int stridx = point.X + point.Y * (image.Height + 1);
                    result[stridx] = areaitems[i].Controls[1].Text[0];
                    if (((CheckBox)areaitems[i].Controls[2]).Checked)
                    {
                        redletteridxs.Add(stridx);
                    }
                    else
                    {

                    }
                }
            }
            //Go through the string and place the red text
            string buffer = "";
            string str = result.ToString();
            for (int i = 0; i < str.Length; i++)
            {
                if (redletteridxs.Contains(i))
                {
                    rtb.AppendText(buffer, Color.Black);
                    buffer = "";
                    rtb.AppendText(str[i].ToString(), Color.Red); //Batch?
                }
                else
                {
                    buffer += str[i];
                }
            }
       //     rtb.Text += buffer;
            rtb.AppendText(buffer, Color.Black);
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
                    float m_brightness = Color.FromArgb(m_a, m_r, m_g, m_b).GetBrightness();
                    if (m_a < 200) { continue; }
                    foreach (var area in areas)
                    {
                        var basecolor = area.pixels[0];

                        byte* area_ptr = (byte*)(lockedbits.Scan0 + (basecolor.X * bytesperpixel) + (basecolor.Y * lockedbits.Stride));
                        byte area_r = area_ptr[0];
                        byte area_g = area_ptr[1];
                        byte area_b = area_ptr[2];
                        byte area_a = area_ptr[3];
                        float area_brightness = Color.FromArgb(area_a, area_r, area_g, area_b).GetBrightness();

                        if (Math.Abs(area_brightness - m_brightness) < double.Parse(scalevalue.Text)) //Check the sums of the difference of the colors
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
