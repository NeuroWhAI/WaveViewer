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

        private double m_gain = 1.0;
        public double Gain
        {
            get => m_gain;
            set
            {
                if (value > 0)
                {
                    m_gain = value;
                }
            }
        }

        private int m_maxLen = 3072;
        public int MaxLength
        {
            get => m_maxLen;
            set
            {
                if (value > 0)
                {
                    m_maxLen = value;
                }
            }
        }

        private double m_dangerValue = 0.0016;
        public double DangerValue
        {
            get => m_dangerValue;
            set
            {
                if (value > 0)
                {
                    m_dangerValue = value;
                }
            }
        }

        private double m_zoom = 4;
        public double Zoom
        {
            get => m_zoom;
            set
            {
                if (value > 0)
                {
                    m_zoom = value;
                }
            }
        }

        private double HeightScale
        { get; set; } = 10000.0;

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

        public void Draw(Graphics g, Size size, int samplingRate)
        {
            g.Clear(Color.White);


            if (m_waveform.Count <= 0)
            {
                return;
            }


            int[] copyWaveform = null;

            lock (m_lockWaveform)
            {
                copyWaveform = m_waveform.ToArray();
            }


            int maxData = copyWaveform.AsParallel().Max(Math.Abs);


            HeightScale = size.Height / 2 * 0.9 / Math.Max(maxData / Gain, DangerValue / Zoom);


            int halfHeight = size.Height / 2;

            float dangerY = (float)(DangerValue * HeightScale);
            if (dangerY < halfHeight)
            {
                g.DrawLine(Pens.Red, 0, halfHeight + dangerY,
                    size.Width, halfHeight + dangerY);
                g.DrawLine(Pens.Red, 0, halfHeight - dangerY,
                    size.Width, halfHeight - dangerY);
            }


            DateTime time;

            lock (m_syncDataTime)
            {
                time = m_latestDataTime;
            }


            if (copyWaveform.Length > 0)
            {
                var elapsedTime = DateTime.UtcNow.AddHours(9.0) - time;
                double futureDataCount = elapsedTime.TotalSeconds * samplingRate;
                if (futureDataCount < 0)
                {
                    futureDataCount = 0;
                }
                else if (futureDataCount > MaxLength)
                {
                    futureDataCount = MaxLength;
                }

                double widthScale = (double)size.Width / copyWaveform.Length;

                int i = 0;
                float prevY = 0;

                foreach (var data in copyWaveform)
                {
                    float y = (float)(data / Gain * HeightScale);

                    g.DrawLine(Pens.Blue, (float)((i - 1 - futureDataCount) * widthScale), prevY + halfHeight,
                        (float)((i - futureDataCount) * widthScale), y + halfHeight);

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
