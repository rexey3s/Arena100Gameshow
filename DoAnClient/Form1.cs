using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace DoAnClient
{
    public partial class Form1 : Form
    {
        #region Các biến
        /// <summary>  </summary>
        private PlayerClient client;
        /// <summary> hiễn thị các host ping dc</summary>
        private ListBox hostBox;
        /// <summary> nhập tên người chơi vào đây </summary>
        private TextBox IDtextBox;
        /// <summary> "Enter your name:" </summary>
        private Label lbName;
        /// <summary>đang chơi ->if true </summary>
        private bool _playingMode = false;
        /// <summary>delegate dùng beginInvoke hàm Findhost trong lớp PlayerClient </summary>
        private delegate List<IPAddress> d_Findhost();
        /// <summary>delegate dùng trong hàm setText cho các control trong form chính </summary>
        private delegate void SetTextCallback(string text, object sender);
        /// <summary>delegate dùng trong hàm setStatus cho toolStripStatus </summary>
        private delegate void SetStatusCallback(string text, ToolStripStatusLabel lb);
        /// <summary>danh sách các host ping thấy trong mạng </summary>
        public static List<IPAddress> HostList = new List<IPAddress>();
        /// <summary> mảng chứa 4 nút trả lời A,B,C,D </summary>
        public Button[] btnArray;
        /// <summary> thời gian đếm ngược </summary>
        public int timeOutSeconds;
        /// <summary> long ticks nano-seconds </summary>
        public long lTickSeconds;
        /// <summary> if true -> đóng IDbox , hostBox vào giao diện trả lời câu hỏi</summary>
        public bool TurnPlayModeOn
        {
            set
            {
                _playingMode = value;
                groupBox1.Visible = _playingMode;
                groupBox2.Visible = _playingMode;

            }
            get
            {
                return _playingMode;
            }
        }
        /// <summary> bật - tắt các nút trả lời </summary>
        public bool EnableChoice
        {
            set
            {
                foreach (Button btn in btnArray)
                {
                    btn.Enabled = value;
                }
            }
            get
            {
                return btnArray[0].Enabled;
            }
        }
        /// <summary> set text cho toolStripStatusLabel1 </summary>
        public string StatusLB
        {
            set
            {
                SetStatusLabel(value, toolStripStatusLabel1);
            }
        }
        #endregion

        #region Phương thức
        
        
        /// <summary>
        /// khởi tạo
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            btnArray = new Button[] { button1, button2, button3, button4 };
            foreach (Button b in btnArray)
            {
                b.BackColor = Color.FromArgb(97, 16, 186);
            }

            //
            EnableChoice = false;

            //
            hostBox = new ListBox();
            hostBox.Location = new Point(16, 40);
            hostBox.Size = new Size(240, 120);
            hostBox.Visible = true;
            //
            IDtextBox = new TextBox();
            IDtextBox.Location = new Point(96, 160);
            IDtextBox.Size = new Size(160, 20);
            IDtextBox.Visible = true;
            //
            lbName = new Label();
            lbName.Location = new Point(16, 162);
            lbName.Text = "Enter your name:";
            lbName.Size = new Size(120, 20);
            lbName.Visible = true;
            //
            client = new PlayerClient(this);
            //
            TurnPlayModeOn = false;
            panel1.Visible = false;
        }
        /// <summary>
        /// setTextbox an toàn - tránh hiện tượng cross-thread
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sender"></param>
        public void SetTextbox(string text,object sender)
        {
            Control  ui = (Control)sender;
            if (ui.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTextbox);
                ui.Invoke(d, new object[] { text, sender });

            }
            else
            {
                ui.Text = text;
            } 
         
        }
        /// <summary>
        /// settext an toàn - tránh hiện tượng cross-thread
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lb"></param>
        public void SetStatusLabel(string text, ToolStripStatusLabel lb)
        {
            
            if (InvokeRequired)
            {
                SetStatusCallback d = new SetStatusCallback(SetStatusLabel);
                Invoke(d, new object[] { text, lb });

            }
            else
            {
                lb.Text = text;
            }
        }
        /// <summary>
        /// click chọn câu trả lời
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendAnsclickEvent(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            DialogResult result =  MessageBox.Show("Bạn có chắc là câu "+b.Text.Substring(0,1), "Gửi câu trả lời", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                byte[] buff = new byte[512];
                if (this.client.TheOne)
                {
                    buff = PlayerClient._ENCODE.GetBytes("ONE " + b.Text.Substring(0, 1));
                }
                else
                {
                    buff = PlayerClient._ENCODE.GetBytes("ANW " + b.Text.Substring(0, 1));
                    timer1.Stop();
                    lTickSeconds = client.TimeOutSeconds;
                }
                client._NS.Write(buff,0,buff.Length);
                client._NS.Flush();
                EnableChoice = false;
            }
        }
        /// <summary>
        /// bung gói câu hỏi nhận dc
        /// </summary>
        /// <param name="result"></param>
        public void Unpack(string result)
        {
            try
            {
                string[] data = result.Split('\n');
                int array_size = data.Length;
                if (array_size >= 5)
                {
                    SetTextbox(data[2], textBox1);
                    SetTextbox("A. " + data[3], button1);
                    SetTextbox("B. " + data[4], button2);
                    SetTextbox("C. " + data[5], button3);
                    SetTextbox("D. " + data[6], button4);

                    Invoke(new Action(() => { EnableChoice = true; }));
                }
                //else
                //    SetStatusLabel(result, toolStripStatusLabel1);
                
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// nút tim host trong menu file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connectToToolStripMenuItem_Click(object sender, EventArgs e)
        {
               d_Findhost delFindhost = new d_Findhost(PlayerClient.FindHost);
               delFindhost.BeginInvoke(new AsyncCallback(FindhostCallback), delFindhost);
               this.toolStripStatusLabel1.Text = "Scanning for host ...";
               ToolStripMenuItem tSMI = sender as ToolStripMenuItem;
               tSMI.Enabled = false;
        }
        /// <summary>
        /// bất đồng bộ của tìm host
        /// </summary>
        /// <param name="ar"></param>
        private void FindhostCallback(IAsyncResult ar)
        {
            IAsyncResult result  = (IAsyncResult)ar;
            d_Findhost d = (d_Findhost)result.AsyncState;
            HostList = d.EndInvoke(result);
            if (HostList.Count > 0)
            {
                
                Invoke(new Action(() => { 
                    Controls.Add(hostBox);
                    Controls.Add(IDtextBox);
                    Controls.Add(lbName);
                }));
                foreach (IPAddress ip in HostList)
                {
                    Invoke(new Action(() => {

                        hostBox.Items.Add(ip); 
                    }));
                    //SetTextbox(ip.ToString(), textBox1);

                }
                hostBox.DoubleClick += new EventHandler(connect_DclickEvent);
            }
            this.toolStripStatusLabel1.Text = HostList.Count != 0 ? "Host(s) found" : "No host found";
        }
        /// <summary>
        /// chọn host ->double-click để kết nối
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connect_DclickEvent(object sender, EventArgs e)
        {
            ListBox box = (ListBox)sender;
            string ip = box.SelectedItem.ToString();
            try
            {


                if(IDtextBox.Text == string.Empty)
                MessageBox.Show("Vui lòng nhập tên người chơi");
            else
                    if (!client.Connect(ip, IDtextBox.Text))
                        toolStripStatusLabel1.Text = "Unable to connect to --> " + ip;
                    else
                    {

                        if (InvokeRequired)
                            Invoke(new Action(() =>
                                {
                                    this.Controls.Remove(hostBox);
                                    this.Controls.Remove(IDtextBox);
                                    this.Controls.Remove(lbName);
                                    //TurnPlayModeOn = true;
                                    panel1.Visible = true;
                                }));
                        else
                        {
                            this.Controls.Remove(hostBox);
                            this.Controls.Remove(IDtextBox);
                            this.Controls.Remove(lbName);
                            //TurnPlayModeOn = true;
                            panel1.Visible = true;
                        }
                        this.client.OpenPhase();
                        toolStripStatusLabel1.Text = client._status;
                        foreach (Button btn in btnArray)
                        {
                            btn.Enabled = true;
                        }
                       
                    }
                    
            }
            
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            
        }
        /// <summary>
        ///  timer1 ticks's Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            lTickSeconds -= 1;
            label1.Text = GetHumanElapse(new TimeSpan(lTickSeconds*10000000));
            if (lTickSeconds <= 0)
            {
                timer1.Stop();
                byte[] buff = new byte[128];
                buff = PlayerClient._ENCODE.GetBytes("ANW NONE");

                client._NS.Write(buff, 0, buff.Length);
                client._NS.Flush();
                EnableChoice = false;
                lTickSeconds =client.TimeOutSeconds;
            }
        }
        /// <summary>
        /// Exit menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// hiển thị thời gian đếm ngược
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static string GetHumanElapse(TimeSpan timeSpan)
        {
            string strRetVal;
            int iIndex;

            strRetVal = timeSpan.ToString();
            iIndex = strRetVal.IndexOf(':');
            if (iIndex != -1)
            {
                iIndex = strRetVal.IndexOf('.', iIndex);
                if (iIndex != -1)
                {
                    strRetVal = strRetVal.Substring(0, iIndex);
                }
            }
            return (strRetVal);
        }
                
#endregion
     
    }
}
