using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
//using System.Net;
//using System.Net.Sockets;
using System.IO;
using System.Xml;

namespace DoAnServer
{
    public partial class Mainform : Form
    {
        #region Các biến 
        /// <summary> Đấu trường server </summary>
        private DauTruongServer server = null;
        /// <summary> </summary>
        private OpenFileDialog openDlg = new OpenFileDialog();
        /// <summary> Thread dùng gửi câu hỏi </summary>
        private Thread sendThread;
        /// <summary>true -> data đã dc import thành công </summary>
        private bool _isDataIMPORT = false;
        /// <summary>Option form </summary>
        public OptionForm _OptionForm = null;
        /// <summary> tài liệu xml  </summary>
        public XmlDocument Content = new XmlDocument();
        /// <summary> đường dẫn của tài liệu xml</summary>
        public string pathOfXml = string.Empty;
        /// <summary> thời gian trả lời câu hỏi của người chơi thường</summary>
        public int timeOutSecond;
        /// <summary> treeview cho _ContainerList </summary>
        private TreeView treeView2 = new TreeView();
        /// <summary> Listview hiển thị các client đang chơi </summary>
        public ListView ClientListView
        {
            get { return listView1; }
        }
        /// <summary> trả về label1 </summary>
        public Label QuesLabel
        {
            get
            {
                return label1;
            }
        }
        /// <summary> long ticks  </summary>
        public long lTickSeconds = 0;
        #endregion
        
        #region Danh sách câu hỏi
        /// <summary>danh sách lớn câu hỏi dễ  </summary>
        private List<Question> Easy_questionList = new List<Question>();
        /// <summary> danh sách lớn câu hỏi khó</summary>
        private List<Question> Hard_questionList = new List<Question>();
        /// <summary> danh sách câu hỏi dễ dc random từ ds lớn</summary>
        public List<Question> easyQuestions;
        /// <summary> danh sách câu hỏi khó dc random từ ds lớn</summary>
        public List<Question> hardQuestions;
        /// <summary> tổng hợp 2 ds dễ và khó ở trên </summary>
        public List<Question> _ContainerList;
       
        #endregion
      
        #region Delegates
        private delegate void SetListViewItemCallback(ListViewItem item, ListViewItem.ListViewSubItemCollection subitems);
        private delegate void RemoveItemListViewCallback(pClient tag);
        private delegate void SetListViewSubItem(ListViewItem.ListViewSubItem sub);
        public delegate void SetTextCallback(string text, object sender);
#endregion

        #region Hàm dựng
        /// <summary>
        /// Mặc định
        /// </summary>
        public Mainform()
        {
            InitializeComponent();
            server = new DauTruongServer(this, 5);
            treeView2.Size = treeView1.Size;
            treeView2.Location = treeView1.Location;
            treeView2.Visible = false;
            Controls.Add(treeView2);


        }
        /// <summary>
        /// Hàm khởi tạo với tham số 
        /// </summary>
        /// <param name="playerCount">số người chơi</param>
        /// <param name="timeOut">thời gian trả lời câu hỏi của người chơi thường</param>
        /// <param name="QuesCount">số câu hỏi </param>
        public Mainform(int playerCount, int timeOut, int QuesCount)
        {
            InitializeComponent();
            server = new DauTruongServer(this, playerCount);
            treeView2.Size = treeView1.Size;
            treeView2.Location = treeView1.Location;
            treeView2.Visible = false;
            Controls.Add(treeView2);
            timeOutSecond = timeOut;
            sendThread = new Thread(SendQues);
            sendThread.IsBackground = true;
            easyQuestions = new List<Question>(QuesCount / 2);
            hardQuestions = new List<Question>(QuesCount - easyQuestions.Capacity);
            _ContainerList = new List<Question>(easyQuestions.Capacity + hardQuestions.Capacity);
        }
        #endregion
        
        #region Events
        /// <summary>
        /// Start listening button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                server.StartServer();
                if (server._online) radioButton1.Checked = true;
                Button b = (Button)sender;
                b.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Get ready button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click_1(object sender, EventArgs e)
        {
            if (_ContainerList.Count == 0)
                MessageBox.Show("Bạn phải nhập database câu hỏi vào và random trước khi thực hiện thao tác này", "", MessageBoxButtons.OK);
            else if (server._PlayerList.Count == 0)
            {
                MessageBox.Show("Chưa có client nào kết nối để thực hiện thao tác này", "", MessageBoxButtons.OK);
            }
           
          
            else
            if (!server.GetReady)
            {

                server.GetReady = true;
                button2.Text = "Ready!";
                button2.Enabled = false;

            }
            else
            {
                server.GetReady = false;
                button2.Text = "NotReady";
            }
        }
        /// <summary>
        /// Random menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void random_Click(object sender, EventArgs e)
        {
            if (!_isDataIMPORT)
                MessageBox.Show("You must import data first");
            else
            {

                #region Random 20 question
                Random rand = new Random();
                int k = 0;
                List<int> k_list = new List<int>();
                for (int i = 0; i < Easy_questionList.Count; i++)

                    k_list.Add(i);
                for (int i = 0; i < easyQuestions.Capacity; i++)
                {
                    k = rand.Next(k_list.Count);

                    easyQuestions.Add(Easy_questionList[k]);
                    k_list.RemoveAt(k);

                }
                int j = 0;
                List<int> j_list = new List<int>();
                for (int i = 0; i < Hard_questionList.Count; i++)

                    j_list.Add(i);
                for (int i = 0; i < hardQuestions.Capacity; i++)
                {
                    j = rand.Next(j_list.Count);

                    hardQuestions.Add(Hard_questionList[j]);
                    j_list.RemoveAt(j);

                }
                _ContainerList.AddRange(easyQuestions);
                _ContainerList.AddRange(hardQuestions);
                #endregion
                PopulateTreeView2();
                treeView2.Visible = true;
                treeView1.Visible = false;
                label1.Text = "Question: ";


                MessageBox.Show("Fetching Done.");
            }

        }
        /// <summary>
        /// Show các câu hỏi trong 2 list Easy va Hard - Show all
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showQuestionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.Visible = true;
            treeView2.Visible = false;
        }
        /// <summary>
        /// start sending questions menu item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_ContainerList.Count == 0)
                MessageBox.Show("Bạn phải nhập database câu hỏi vào và random trước khi thực hiện thao tác này", "", MessageBoxButtons.OK);
            else if (server._PlayerList.Count == 0)
            {
                MessageBox.Show("Chưa có client nào kết nối để thực hiện thao tác này", "", MessageBoxButtons.OK);
            }
            else if (!server.GetReady)
            {
                MessageBox.Show("Hãy nhấn nút Get ready trên giao diện", "", MessageBoxButtons.OK);
            }
            else
            {
                sendThread.Start();
                timer1.Enabled = true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            this.sendThread.Abort();
            _OptionForm.Close();
        }
        /// <summary>
        /// Show các câu hỏi trong _ContainerList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showHideQuestionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView2.Visible = true;
            treeView1.Visible = false;
        }
        /// <summary>
        /// mỗi giây đồng hồ thì in ra label2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            lTickSeconds += 1;
            label2.Text = "Up time: " + GetHumanElapse(new TimeSpan(lTickSeconds * 10000000));
        }
        /// <summary>
        /// Nhập xml từ file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void importxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_isDataIMPORT)
            {
                DialogResult dlgRe = openDlg.ShowDialog(this);
                if (dlgRe == DialogResult.OK)
                {
                    try
                    {
                        FileStream fs = new FileStream(openDlg.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        Content.Load(fs);
                        pathOfXml = openDlg.FileName;
                        for (int i = 0; i < 5; i++)
                        {
                            XmlNode easylevelNode = Content.GetElementsByTagName("cat")[i].ChildNodes[2];
                            XmlLoad(easylevelNode);
                            XmlNode hardlevelNode = Content.GetElementsByTagName("cat")[i].ChildNodes[3];
                            XmlLoad(hardlevelNode);
                        }

                        _isDataIMPORT = true;
                        MessageBox.Show("Database imported successfully !");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

#endregion

        #region  Các phương thức xử lý crosss-thread
        /// <summary>
        /// ghi text cho các control trong form
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sender"></param>
        public void SetText(string text, object sender)
        {
            Control ui = (Control)sender;
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (ui.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { text, sender });
            }
            else
            {
                ui.Text = text + "\r\n";
            }
        }
        /// <summary>
        /// Xóa các item chứa các client ko còn kết nối vs server
        /// </summary>
        /// <param name="tag"></param>
        public void RemoveListViewItem(pClient tag)
        {
            if (listView1.InvokeRequired)
            {
                RemoveItemListViewCallback d = new RemoveItemListViewCallback(RemoveListViewItem);
                listView1.Invoke(d, new object[] { tag });
            }
            else
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Tag.Equals(tag))
                        listView1.Items.Remove(item);
                }
            }
        }
        /// <summary>
        /// Thêm các item chứa các client mới kết nối vs server
        /// </summary>
        /// <param name="item"></param>
        /// <param name="subitems"></param>
        public void EditListView(ListViewItem item, ListViewItem.ListViewSubItemCollection subitems)
        {
            if (this.listView1.InvokeRequired)
            {
                SetListViewItemCallback delg = new SetListViewItemCallback(EditListView);
                listView1.Invoke(delg, new object[] { item, subitems });
            }
            else
            {

                pClient tag = (pClient)item.Tag;
                item.Text = tag._playerID._Name.ToString();
                subitems.Add("Client : " + tag.sock.RemoteEndPoint.ToString());
                subitems.Add(tag._status);
                subitems.Add(tag._playerID.AnsweredQuestions.ToString());
                subitems.Add(tag._playerID.RightAnswers.ToString());
                subitems.Add(tag._playerID.POINTS.ToString());
                listView1.Items.Add(item);
            }
        }
        /// <summary>
        /// chỉnh sửa các subItems của các client đang trong quá trình chơi
        /// </summary>
        /// <param name="sub"></param>
        public void EditSubItems(ListViewItem.ListViewSubItem sub)
        {
            if (listView1.InvokeRequired)
            {
                SetListViewSubItem d = new SetListViewSubItem(EditSubItems);
                listView1.Invoke(d, new object[] { sub });
            }
            else
            {
                pClient subTag = (pClient)sub.Tag;
                foreach (ListViewItem item in listView1.Items)
                {
                    pClient tag = (pClient)item.Tag;
                    if (tag._playerID._ID == subTag._playerID._ID)
                    {
                        item.SubItems.RemoveAt(5);
                        item.SubItems.RemoveAt(4);
                        item.SubItems.RemoveAt(3);
                        item.SubItems.RemoveAt(2);
                        item.SubItems.Add(tag._status.ToString());
                        item.SubItems.Add(tag._playerID.AnsweredQuestions.ToString());
                        item.SubItems.Add(tag._playerID.RightAnswers.ToString());
                        item.SubItems.Add(tag._playerID.POINTS.ToString());
                    }


                }
            }
        }
        #endregion

        #region Các phương thức còn lại
        /// <summary>
        /// Send question function
        /// 
        /// </summary>
        private void SendQues()
        {
            //Phase 1
            while (_ContainerList.Count > 0)
            {   
                //
                // hỏi lựa chọn của người chơi chính
                //
                server.ASKChoice();
               
                if (server.currentLV == Level.Hard && hardQuestions.Count > 0)
                {
                    int iTop = hardQuestions.Count - 1;
                    server.NowQuestion = hardQuestions[iTop];
                    server.SendQuestionPackage();
                    hardQuestions.RemoveAt(iTop);
                    _ContainerList.Remove(server.NowQuestion);
                }
                else if (server.currentLV == Level.Easy && easyQuestions.Count > 0)
                {
                    int jTop = easyQuestions.Count - 1;
                    server.NowQuestion = easyQuestions[jTop];
                    server.SendQuestionPackage();
                    easyQuestions.RemoveAt(jTop);
                    _ContainerList.Remove(server.NowQuestion);
                }
                else
                {
                    Random r = new Random();
                    server.NowQuestion = _ContainerList[r.Next(_ContainerList.Count)];
                    server.SendQuestionPackage();
                    if (!easyQuestions.Remove(server.NowQuestion)) hardQuestions.Remove(server.NowQuestion);
                    _ContainerList.Remove(server.NowQuestion);

                }
                SetText("Question: " +_ContainerList.Count + "/" + _ContainerList.Capacity, QuesLabel);
                if (server.CheckWinner()) break;

            }
            //Phase 2
            if (!server.CheckWinner())
            {
                while (!server.CheckWinner() && Hard_questionList.Count > 0)
                {
                    try
                    {
                        Random r = new Random();
                        server.NowQuestion = Hard_questionList[r.Next(Hard_questionList.Count)];
                        server.SendQuestionPackage();
                        Hard_questionList.Remove(server.NowQuestion);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }
            }



        }
        /// <summary>
        /// phương thức load xml từ file
        /// </summary>
        /// <param name="content"></param>
        private void XmlLoad(XmlNode content)
        {
            if (content.InnerText.Contains("Easy"))
            {
                foreach (XmlNode node in content)
                {
                    if (node.Name == "content")
                    {
                        Question ques = new Question(node);
                        Easy_questionList.Add(ques);
                    }
                }
            }
            if (content.InnerText.Contains("Hard"))
            {
                foreach (XmlNode node in content)
                {
                    if (node.Name == "content")
                    {
                        Question ques = new Question(node);
                        Hard_questionList.Add(ques);
                    }
                }
            }

            PopulateTreeView1();
        }
        /// <summary>
        /// tạo cây cho cho _ContainerList
        /// </summary>
        private void PopulateTreeView2()
        {
            treeView2.Nodes.Clear();
            TreeNode root = new TreeNode("Noi_dung");
            foreach (Question q in _ContainerList)
            {
                string raw = q.strContent.ToString();
                string[] arr_of_raw = raw.Split('\n');
                TreeNode index = new TreeNode(q.ID.ToString() + "-" + q.level.ToString() + "-" + q.cat.ToString());
                //TreeNode nodeLevel = new TreeNode("Level " + arr_of_raw[0]);
                TreeNode nodeQ = new TreeNode("Cau_hoi: " + arr_of_raw[2]);
                TreeNode nodeA = new TreeNode("A. " + arr_of_raw[3]);
                TreeNode nodeB = new TreeNode("B. " + arr_of_raw[4]);
                TreeNode nodeC = new TreeNode("C. " + arr_of_raw[5]);
                TreeNode nodeD = new TreeNode("D. " + arr_of_raw[6]);
                TreeNode right_ans = new TreeNode("Right_Ans: " + q.right_ans.ToString());
                TreeNode[] nodes = { nodeQ, nodeA, nodeB, nodeC, nodeD, right_ans };
                index.Nodes.AddRange(nodes);
                index.Tag = q;
                root.Nodes.Add(index);

            }
            treeView2.Nodes.Add(root);
        }
        /// <summary>
        /// tạo cây cho Easy và Hard list 
        /// </summary>
        private void PopulateTreeView1()
        {
            treeView1.Nodes.Clear();
            TreeNode root = new TreeNode("Noi_dung");
            foreach (Question q in Easy_questionList)
            {
                string raw = q.strContent.ToString();
                string[] arr_of_raw = raw.Split('\n');
                TreeNode index = new TreeNode(q.ID.ToString() + "-" + q.level.ToString() + "-" + q.cat.ToString());
                //TreeNode nodeLevel = new TreeNode("Level " + arr_of_raw[0]);
                TreeNode nodeQ = new TreeNode("Cau_hoi: " + arr_of_raw[2]);
                TreeNode nodeA = new TreeNode("A. " + arr_of_raw[3]);
                TreeNode nodeB = new TreeNode("B. " + arr_of_raw[4]);
                TreeNode nodeC = new TreeNode("C. " + arr_of_raw[5]);
                TreeNode nodeD = new TreeNode("D. " + arr_of_raw[6]);
                TreeNode right_ans = new TreeNode("Right_Ans: " + q.right_ans.ToString());
                TreeNode[] nodes = { nodeQ, nodeA, nodeB, nodeC, nodeD, right_ans };
                index.Nodes.AddRange(nodes);
                index.Tag = q;
                root.Nodes.Add(index);

            }

            foreach (Question q in Hard_questionList)
            {
                string raw = q.strContent.ToString();
                string[] arr_of_raw = raw.Split('\n');
                TreeNode index = new TreeNode(q.ID.ToString() + "-" + q.level.ToString() + "-" + q.cat.ToString());
                //TreeNode nodeLevel = new TreeNode("Level " + arr_of_raw[0]);
                TreeNode nodeQ = new TreeNode("Cau_hoi: " + arr_of_raw[2]);
                TreeNode nodeA = new TreeNode("A. " + arr_of_raw[3]);
                TreeNode nodeB = new TreeNode("B. " + arr_of_raw[4]);
                TreeNode nodeC = new TreeNode("C. " + arr_of_raw[5]);
                TreeNode nodeD = new TreeNode("D. " + arr_of_raw[6]);
                TreeNode right_ans = new TreeNode("Right_Ans: " + q.right_ans.ToString());
                TreeNode[] nodes = { nodeQ, nodeA, nodeB, nodeC, nodeD, right_ans };
                index.Nodes.AddRange(nodes);
                index.Tag = q;
                root.Nodes.Add(index);

            }
            treeView1.Nodes.Add(root);
        }
        /// <summary>
        /// chuyển đổi từ ticks của timer ra thời gian thực
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
