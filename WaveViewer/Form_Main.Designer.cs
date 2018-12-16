namespace WaveViewer
{
    partial class Form_Main
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel_wave = new System.Windows.Forms.Panel();
            this.timer_update = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // panel_wave
            // 
            this.panel_wave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_wave.Location = new System.Drawing.Point(0, 0);
            this.panel_wave.Name = "panel_wave";
            this.panel_wave.Size = new System.Drawing.Size(683, 191);
            this.panel_wave.TabIndex = 0;
            this.panel_wave.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_wave_Paint);
            // 
            // timer_update
            // 
            this.timer_update.Enabled = true;
            this.timer_update.Interval = 2000;
            this.timer_update.Tick += new System.EventHandler(this.timer_update_Tick);
            // 
            // Form_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(683, 191);
            this.Controls.Add(this.panel_wave);
            this.Name = "Form_Main";
            this.Text = "Wave Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Main_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel_wave;
        private System.Windows.Forms.Timer timer_update;
    }
}

