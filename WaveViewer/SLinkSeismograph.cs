using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WaveViewer
{
    public class SLinkSeismograph : Seismograph
    {
        public SLinkSeismograph(string slinktoolPath, string channel, string network, string station)
            : base(channel, network, station)
        {
            SLinkToolPath = slinktoolPath;
        }

        //###########################################################################################################

        public string SLinkToolPath
        { get; protected set; } = string.Empty;

        private Process m_proc = null;

        private StringBuilder m_buffer = new StringBuilder();
        private int m_leftSample = 0;

        //###########################################################################################################

        protected void StartProcess()
        {
            m_proc = new Process();
            m_proc.StartInfo = new ProcessStartInfo()
            {
                Arguments = "-p -u -s " + Channel + " -S " + Network + "_" + Station + " rtserve.iris.washington.edu:18000",
                CreateNoWindow = true,
                FileName = SLinkToolPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };


            m_proc.OutputDataReceived += Proc_OutputDataReceived;


            m_proc.Start();

            m_proc.BeginOutputReadLine();
        }

        protected void StopProcess()
        {
            if (m_proc.HasExited == false)
            {
                m_proc.OutputDataReceived -= Proc_OutputDataReceived;

                m_proc.CloseMainWindow();
                m_proc.WaitForExit(3000);
                if (m_proc.HasExited == false)
                {
                    m_proc.Kill();
                    m_proc.Dispose();
                }
                m_proc = null;
            }

            m_buffer.Clear();
            m_leftSample = 0;
        }

        //###########################################################################################################

        protected override void BeforeStart()
        {
            base.BeforeStart();


            StartProcess();
        }

        protected override void AfterStop()
        {
            StopProcess();


            base.AfterStop();
        }

        //###########################################################################################################

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_buffer.Append(e.Data);
            string buf = m_buffer.ToString();


            if (m_leftSample <= 0)
            {
                Regex rgx = new Regex(@"([^,]+),\s?(\d+)\s?samples,\s?(\d+\.?\d*)\s?Hz,\s?([^\s]+)\s?\(.+\)");
                var m = rgx.Match(buf);
                if (m.Success)
                {
                    //Console.WriteLine("Location: " + m.Groups[1]); // string
                    //Console.WriteLine("Samples: " + m.Groups[2]); // int
                    //Console.WriteLine("Hz: " + m.Groups[3]); // double
                    //Console.WriteLine("Time: " + m.Groups[4]); // string

                    
                    m_leftSample = int.Parse(m.Groups[2].ToString());

                    double.TryParse(m.Groups[3].ToString(), out double rate);

                    ReserveChunk(m_leftSample, rate);


                    m_buffer = m_buffer.Remove(m.Index, m.Length);
                }
            }
            else
            {
                int beginIndex = -1, endIndex = 0;

                Regex rgx = new Regex(@"(-?\d+)\s+");
                var m = rgx.Match(buf);
                while (m.Success && m_leftSample > 0)
                {
                    int data = 0;
                    if (int.TryParse(m.Groups[1].ToString().Trim(), out data))
                    {
                        AppendSample(data);
                    }
                    else
                    {
                        m_leftSample = -1;
                        break;
                    }


                    if (beginIndex < 0)
                        beginIndex = m.Index;
                    endIndex = m.Index + m.Length;

                    m_leftSample--;

                    m = m.NextMatch();
                }

                if (beginIndex >= 0)
                {
                    m_buffer = m_buffer.Remove(beginIndex, endIndex - beginIndex);

                    // 선행 공백 제거
                    int len = m_buffer.Length;
                    for (int i = 0; i < len; ++i)
                    {
                        if (m_buffer[0] == ' '
                            || m_buffer[0] == '\t'
                            || m_buffer[0] == '\n')
                        {
                            m_buffer.Remove(0, 1);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
