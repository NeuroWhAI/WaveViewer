using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace WaveViewer
{
    public class Graph
    {
        public Graph()
        {

        }

        //##############################################################################################

        protected Queue<int> m_waveform = new Queue<int>();
        protected readonly object m_lockWaveform = new object();

        public string Name
        { get; set; } = "";

        public bool IsAccel
        { get; set; }

        public double Gain
        { get; set; } = 1.0;

        private double HeightScale
        { get; set; } = 10000.0;

        public int MaxLength
        { get; set; } = 3072;

        public double DangerValue
        { get; set; } = 0.0016;

        private DateTime m_latestDataTime;
        private readonly object m_syncDataTime = new object();

        //##############################################################################################

        public void PushData(IEnumerable<double> wave)
        {
            lock (m_lockWaveform)
            {
                foreach (int data in wave)
                {
                    m_waveform.Enqueue(data);
                }

                while (m_waveform.Count > MaxLength)
                {
                    m_waveform.Dequeue();
                }
            }

            lock (m_syncDataTime)
            {
                m_latestDataTime = DateTime.UtcNow.AddHours(9.0);
            }
        }

        public void Clear()
        {
            m_waveform.Clear();
        }

        //##############################################################################################

        public void Draw(Graphics g, Size size)
        {
            if (m_waveform.Count <= 0)
            {
                return;
            }


            g.Clear(Color.White);


            int[] copyWaveform = null;

            lock (m_lockWaveform)
            {
                copyWaveform = m_waveform.ToArray();
            }


            int maxData = copyWaveform.AsParallel().Max(Math.Abs);


            HeightScale = size.Height / 2 * 0.9 / Math.Max(maxData / Gain, DangerValue / 4);


            int halfHeight = size.Height / 2;

            float dangerY = (float)(DangerValue * HeightScale);
            if (dangerY < halfHeight)
            {
                g.DrawLine(Pens.Red, 0, halfHeight + dangerY,
                    size.Width, halfHeight + dangerY);
                g.DrawLine(Pens.Red, 0, halfHeight - dangerY,
                    size.Width, halfHeight - dangerY);
            }


            if (copyWaveform.Length > 0)
            {
                double widthScale = (double)size.Width / copyWaveform.Length;

                int i = 0;
                float prevY = 0;

                foreach (var data in copyWaveform)
                {
                    float y = (float)(data / Gain * HeightScale);

                    g.DrawLine(Pens.Blue, (float)((i - 1) * widthScale), prevY + halfHeight,
                        (float)(i * widthScale), y + halfHeight);

                    prevY = y;
                    ++i;
                }
            }


            double groundVal = maxData / Gain;

            int mmi = 0;

            if (IsAccel)
            {
                mmi = Earthquake.ConvertPgaToMMI(groundVal);
            }
            else
            {
                mmi = Earthquake.ConvertPgvToMMI(groundVal);
            }

            DateTime time;

            lock (m_syncDataTime)
            {
                time = m_latestDataTime;
            }

            using (var font = new Font(SystemFonts.DefaultFont.FontFamily, 14.0f, FontStyle.Regular))
            {
                g.DrawString(string.Format("{0} ({1})", Name, time.ToString("s")), font, Brushes.Black,
                    2, size.Height - font.Height - 2);

                g.DrawString(string.Format("Danger: {0:F5}%", groundVal / DangerValue * 100.0),
                    font, Brushes.Black, 2, 4);

                g.DrawString(string.Format("Intensity: {0} ({1:F5})", Earthquake.MMIToString(mmi), groundVal),
                    font, Brushes.Black, 2, 6 + font.Height);
            }
        }
    }
}
