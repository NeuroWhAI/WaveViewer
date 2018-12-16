using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveViewer
{
    public abstract class Worker
    {
        public Worker()
        {

        }

        //############################################################################################

        protected Task m_task = null;
        protected bool m_onRunning = false;
        public TimeSpan JobDelay
        { get; set; } = TimeSpan.FromSeconds(0.1);

        //############################################################################################

        public void Start()
        {
            Stop();


            BeforeStart();

            m_onRunning = true;
            m_task = Task.Factory.StartNew(Work);
        }

        public void Stop()
        {
            if (m_task != null)
            {
                m_onRunning = false;
                m_task.Wait();

                AfterStop();
            }
        }

        private void Work()
        {
            while (m_onRunning)
            {
                OnWork();

                System.Threading.Thread.Sleep(JobDelay);
            }
        }

        //############################################################################################

        protected abstract void BeforeStart();

        protected abstract void AfterStop();

        protected abstract void OnWork();
    }
}
