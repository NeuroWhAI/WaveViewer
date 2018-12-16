using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace WaveViewer
{
    public class WinstonSeismograph : Seismograph
    {
        public WinstonSeismograph(string ip, int port,
            string location, string channel, string network, string station)
            : base(channel, network, station)
        {
            m_ip = ip;
            m_port = port;

            Location = location;
        }

        //###########################################################################################################

        public string Location
        { get; set; } = "--";

        public bool Endian
        { get; set; } = false;

        private readonly string m_ip;
        private readonly int m_port;

        private DateTime m_latestCheckTime = DateTime.UtcNow;
        private TimeSpan m_checkDelay = TimeSpan.FromSeconds(3.0);
        private TimeSpan m_limitTime = TimeSpan.FromSeconds(30.0);

        //###########################################################################################################

        protected override void BeforeStart()
        {
            base.BeforeStart();
        }

        protected override void AfterStop()
        {
            base.AfterStop();
        }

        protected override void OnWork()
        {
            base.OnWork();


            try
            {
                var now = DateTime.UtcNow;
                var elapsedTime = now - m_latestCheckTime;

                if (elapsedTime >= m_checkDelay)
                {
                    // 너무 많이 차이나면 포기.
                    if (elapsedTime > m_limitTime)
                    {
                        m_latestCheckTime = now - m_checkDelay;
                    }

                    GetWave(m_latestCheckTime, now);

                    m_latestCheckTime = now;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);

                Task.Delay(m_checkDelay).Wait();
            }
        }

        private void GetWave(DateTime beginTime, DateTime endTime)
        {
            var client = new TcpClient(m_ip, m_port);


            double beginTicks = ConvertTime(beginTime);
            double endTicks = ConvertTime(endTime);

            string message = string.Format("GETWAVERAW: GS {0} {1} {2} {3} {4} {5} 0\n",
                Station, Channel, Network, Location, beginTicks, endTicks);

            byte[] data = Encoding.ASCII.GetBytes(message);

            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = (int)m_limitTime.TotalMilliseconds / 2;
            stream.Write(data, 0, data.Length);


            bool parsingFail = true;


            var buffer = new StringBuilder();

            int oneByte = 0;
            while (oneByte != '\n')
            {
                oneByte = stream.ReadByte();
                buffer.Append((char)oneByte);
            }

            string[] header = buffer.ToString().TrimEnd().Split(' ');

            if (header.Length >= 2)
            {
                int limit = 0;
                int.TryParse(header[1], out limit);

                if (limit > 0)
                {
                    data = new byte[limit];

                    int leftBytes = limit;

                    while (leftBytes > 0)
                    {
                        int readBytes = stream.Read(data, 0, data.Length);

                        if (readBytes > 0)
                        {
                            leftBytes -= readBytes;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (leftBytes == 0)
                    {
                        double startTime = BitConverter.ToDouble(ReverseBytes(data, 0, 8), 0);
                        double samplingRate = BitConverter.ToDouble(ReverseBytes(data, 8, 8), 0);
                        double registrationOffset = BitConverter.ToDouble(ReverseBytes(data, 16, 8), 0);
                        int length = BitConverter.ToInt32(ReverseBytes(data, 24, 4), 0);

                        if (length > 0 && startTime > 0 && samplingRate > 0
                            && Math.Abs(beginTicks - startTime) < 30.0)
                        {
                            ReserveChunk(length, samplingRate);

                            for (int i = 0; i < length; ++i)
                            {
                                int count = BitConverter.ToInt32(ReverseBytes(data, 28 + i * 4, 4), 0);

                                AppendSample(count);
                            }


                            parsingFail = false;
                        }
                        else
                        {
                            Console.Error.WriteLine("Winston = Length: {0}, Time: {1}, Sampling Rate: {2}",
                                length, startTime - beginTicks, samplingRate);
                        }
                    }
                }
            }


            stream.Close();
            client.Close();


            if (parsingFail)
            {
                Console.Error.WriteLine("Parsing error in Winston seismograph.");
            }
        }

        private byte[] ReverseBytes(byte[] bytes, int offset, int size)
        {
            if (Endian)
            {
                return bytes.Skip(offset).Take(size).Reverse().ToArray();
            }

            return bytes.Skip(offset).ToArray();
        }

        private double ConvertTime(DateTime time)
        {
            return (time.Ticks - 621355968000000000L) / 10000000.0 - 9.46728e+8;
        }
    }
}
