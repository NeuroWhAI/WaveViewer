using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WaveViewer
{
    public partial class Form_Main : Form
    {
        public Form_Main()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);


            var argDict = ParseCmdLineArgs();

            if (argDict.TryGetValue("--name", out string name)
                && !string.IsNullOrWhiteSpace(name))
            {
                Text = $"{Text} - {name.Trim()}";
            }

            m_graph = ParseGraph(argDict);

            if (argDict.TryGetValue("--type", out string type))
            {
                switch (type)
                {
                    case "slink":
                        m_worker = ParseSlink(argDict);
                        break;

                    case "winston":
                        m_worker = ParseWinston(argDict);
                        break;

                    case "fdsn":
                        m_worker = ParseFdsn(argDict);
                        break;
                }
            }

            if (m_worker != null)
            {
                m_worker.WhenDataReceived += Worker_WhenDataReceived;

                m_worker.Start();
            }
        }

        private Graph m_graph = null;
        private Seismograph m_worker = null;

        private void Form_Main_Load(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists("size.txt"))
                {
                    using (var sr = new StreamReader("size.txt"))
                    {
                        Width = int.Parse(sr.ReadLine());
                        Height = int.Parse(sr.ReadLine());
                    }
                }
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.Message);
                Console.Error.WriteLine(err.StackTrace);
            }
        }

        private void Form_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                using (var sw = new StreamWriter("size.txt"))
                {
                    sw.WriteLine(Width);
                    sw.WriteLine(Height);
                }
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.Message);
                Console.Error.WriteLine(err.StackTrace);
            }

            if (m_worker != null)
            {
                m_worker.Stop();
            }
        }

        private void Form_Main_Paint(object sender, PaintEventArgs e)
        {
            var size = this.ClientSize;

            if (m_graph == null || m_worker == null)
            {
                var g = e.Graphics;

                g.DrawLine(Pens.Red, 0, 0, size.Width, size.Height);
            }
            else
            {
                using (var bufferG = BufferedGraphicsManager.Current.Allocate(e.Graphics, this.ClientRectangle))
                {
                    var g = bufferG.Graphics;

                    m_graph.Draw(g, size, m_worker.SamplingRate);

                    bufferG.Render(e.Graphics);
                }
            }
        }

        private void timer_update_Tick(object sender, EventArgs e)
        {
            this.timer_update.Stop();

            this.Invalidate();

            this.timer_update.Start();
        }

        private void Worker_WhenDataReceived(List<double> waveform)
        {
            if (m_graph != null)
            {
                m_graph.PushData(waveform);
            }
        }

        private Graph ParseGraph(Dictionary<string, string> args)
        {
            if (args.TryGetValue("--name", out string name)
                && args.TryGetValue("--gain", out string gainText)
                && args.TryGetValue("--danger", out string dangerText)
                && args.TryGetValue("--accel", out string accel)
                && args.TryGetValue("--len", out string lenText)
                && args.TryGetValue("--zoom", out string zoomText))
            {
                if (double.TryParse(gainText, out double gain)
                    && double.TryParse(dangerText, out double danger)
                    && int.TryParse(lenText, out int maxLen)
                    && double.TryParse(zoomText, out double zoom))
                {
                    var graph = new Graph()
                    {
                        Name = name,
                        Gain = gain,
                        DangerValue = danger,
                        IsAccel = (accel.ToLower() == "true"),
                        MaxLength = maxLen,
                        Zoom = zoom,
                    };

                    return graph;
                }
            }

            return null;
        }

        private SLinkSeismograph ParseSlink(Dictionary<string, string> args)
        {
            if (args.TryGetValue("--channel", out string channel)
                && args.TryGetValue("--network", out string network)
                && args.TryGetValue("--station", out string station))
            {
                var worker = new SLinkSeismograph("slinktool.exe", channel, network, station);

                return worker;
            }

            return null;
        }

        private WinstonSeismograph ParseWinston(Dictionary<string, string> args)
        {
            if (args.TryGetValue("--location", out string location)
                && args.TryGetValue("--channel", out string channel)
                && args.TryGetValue("--network", out string network)
                && args.TryGetValue("--station", out string station)
                && args.TryGetValue("--ip", out string ip)
                && args.TryGetValue("--port", out string portText)
                && args.TryGetValue("--endian", out string endian))
            {
                if (int.TryParse(portText, out int port))
                {
                    var worker = new WinstonSeismograph(ip, port, location, channel, network, station)
                    {
                        Endian = (endian.ToLower() == "true"),
                    };

                    return worker;
                }
            }

            return null;
        }

        private FDSNSeismograph ParseFdsn(Dictionary<string, string> args)
        {
            if (args.TryGetValue("--location", out string location)
                && args.TryGetValue("--channel", out string channel)
                && args.TryGetValue("--network", out string network)
                && args.TryGetValue("--station", out string station))
            {
                var worker = new FDSNSeismograph("mseedviewer.exe", location, channel, network, station);

                return worker;
            }

            return null;
        }

        private Dictionary<string, string> ParseCmdLineArgs()
        {
            var args = Environment.GetCommandLineArgs();

            var argDict = new Dictionary<string, string>();

            for (int i = 1; i + 1 < args.Length; i += 2)
            {
                string key = args[i];
                string val = args[i + 1];

                argDict[key] = val;
            }

            return argDict;
        }
    }
}
