using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveViewer
{
    public class Seismograph : Worker
    {
        public Seismograph(string channel, string network, string station)
        {
            Channel = channel;
            Network = network;
            Station = station;
        }

        //###########################################################################################################

        public string Channel
        { get; protected set; } = string.Empty;

        public string Network
        { get; protected set; } = string.Empty;

        public string Station
        { get; protected set; } = string.Empty;

        private List<double> m_samples = new List<double>();
        private readonly object m_lockSamples = new object();

        private Queue<int> m_sampleCountList = new Queue<int>();
        private readonly object m_lockSampleCount = new object();

        private double? m_prevRawData = null;
        private double m_prevProcData = 0;

        public delegate void SeismographDataReceivedEventHandler(List<double> waveform);
        public event SeismographDataReceivedEventHandler WhenDataReceived = null;

        public int SamplingRate
        { get; private set; } = 0;

        //###########################################################################################################

        protected override void BeforeStart()
        {
            this.JobDelay = TimeSpan.FromMilliseconds(200.0);
        }

        protected override void AfterStop()
        {
            m_samples.Clear();

            m_sampleCountList.Clear();

            m_prevRawData = null;
            m_prevProcData = 0;
        }

        protected override void OnWork()
        {
            try
            {
                int sampleCount = 0;

                lock (m_lockSampleCount)
                {
                    if (m_sampleCountList.Count > 0)
                    {
                        sampleCount = m_sampleCountList.Peek();
                    }
                }


                if (sampleCount > 0)
                {
                    bool runCheck = false;

                    lock (m_lockSamples)
                    {
                        // 현재까지 얻은 샘플 개수가 충분하면
                        if (m_samples.Count >= sampleCount)
                        {
                            runCheck = true;
                        }
                    }


                    if (runCheck)
                    {
                        lock (m_lockSampleCount)
                        {
                            m_sampleCountList.Dequeue();
                        }


                        /// 청크 샘플
                        List<double> subSamples = null;

                        lock (m_lockSamples)
                        {
                            // 파형 저장.
                            subSamples = Enumerable.Take(m_samples, sampleCount).ToList();


                            // 처리한 파형 제거.
                            m_samples.RemoveRange(0, sampleCount);
                        }


                        // 비동기로 데이터 수신 이벤트 발생.
                        if (WhenDataReceived != null)
                        {
                            Task.Factory.StartNew(delegate ()
                            {
                                WhenDataReceived(subSamples);
                            });
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.Error.WriteLine(exp.Message);
                Console.Error.WriteLine(exp.StackTrace);
            }
        }

        //###########################################################################################################

        protected void ReserveChunk(int sampleCount, double samplingRate)
        {
            if ((int)samplingRate > 0)
            {
                this.SamplingRate = (int)samplingRate;
            }


            if (sampleCount > 1)
            {
                lock (m_lockSampleCount)
                {
                    // 얻을 샘플 개수인데 첫번째 데이터는 버리므로 1을 뺌.
                    m_sampleCountList.Enqueue(sampleCount - 1);
                }
            }

            m_prevRawData = null;
            m_prevProcData = 0;
        }

        protected void AppendSample(int data)
        {
            // 첫번째 데이터는 샘플에 넣지 않는다.
            // 청크 단위로 파형을 얻고 있기에 이전 파형과 시간적으로 맞물리지 않는 경우가 있어
            // 고역통과필터에 문제가 생긴다.

            if (m_prevRawData != null)
            {
                const double weight = 0.16;
                double procData = ((weight + 1) / 2) * (data - m_prevRawData.Value) + weight * m_prevProcData;

                lock (m_lockSamples)
                {
                    m_samples.Add(procData);
                }

                m_prevProcData = procData;
            }

            m_prevRawData = data;
        }
    }
}
