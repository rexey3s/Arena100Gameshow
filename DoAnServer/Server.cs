using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections;
using System.Threading;

namespace DoAnServer
{
    enum cmdMsg
    {
        /// <summary> gửi tín hiệu báo sẵn sàng từ server</summary>
        READY,
        /// <summary> default - tránh lỗi</summary>
        NONE,
        /// <summary> gửi tín hiệu gửi câu hỏi</summary>
        QUES,
        /// <summary> gửi tín hiệu thông báo</summary>
        MSG,
        /// <summary> gửi tín hiệu chào khi có client mới kết nối vào</summary>
        HELO,
        /// <summary> gửi tín hiệu từ chối kết nối khi server's full</summary>
        REJ,
        /// <summary> gửi tín hiệu chọn người chơi chính</summary>
        ONE,
        /// <summary> nhận tín hiệu trả lời từ client</summary>
        ANW,
        /// <summary> gửi tín hiệu báo loại client</summary>
        KICK,
        /// <summary> gửi tín hiệu tạm dừng </summary>
        PAUSE,
        /// <summary> gửi tín hiệu kết thúc trò chơi</summary>
        END,
        /// <summary> gửi và nhận tín hiệu cho người chơi chính chọn mức độ khó dễ</summary>
        CHO
    }
  
    public class DauTruongServer
    {
        #region các biến
        /// <summary>
        /// kích thước buffer
        /// </summary>
        public const int B_SIZE = 2048;
        /// <summary>
        /// buffer
        /// </summary>
        public byte[] buffer = new byte[B_SIZE];
        /// <summary>
        /// sự kiện đếm ngược dùng để cài đặt cho việc nhận đủ câu trả lời từ các client 
        /// </summary>
        public CountdownEvent countdownEvent;
        /// <summary>
        /// static UTF-8 encoder - dùng để encode chuỗi hỗ trợ có dấu tiếng Việt
        /// </summary>
        public static Encoding _ENCODE = new UTF8Encoding();
        /// <summary>
        /// socket lắng nghe 
        /// </summary>
        private Socket _listener = null;
        /// <summary>
        /// mảng chứa các pClient đang trong quá trình chơi
        /// </summary>
        public ArrayList _PlayerList;
        /// <summary>
        /// đếm số người chơi
        /// </summary>
        public int _iPlayers;
        /// <summary>
        /// cổng lắng nghe của server
        /// </summary>
        private int _port = 23456;
        /// <summary>
        /// form chính - cài đặt để khai thác các biến dữ liệu trong nó
        /// </summary>
        private Mainform GUI = null;
        /// <summary>
        /// trạng thái server
        /// </summary>
        public string _status = string.Empty;
        /// <summary>
        /// số người chơi còn lại
        /// </summary>
        public int _playerLeft;
        /// <summary>
        /// biến chỉ đọc của GETReady
        /// </summary>
        private bool _IsReady = false;
        /// <summary>
        /// xác định ID người chơi chính
        /// </summary>
        public pClient _mainPlayer;
        /// <summary>
        /// IP mặc định 
        /// </summary>
        private IPAddress _localAddr = IPAddress.Parse("127.0.0.1");
        /// <summary>
        /// true -> sử dụng loopback để lắng nghe
        /// </summary>
        public bool useLoopback = false;
        /// <summary>
        /// true -> đang lắng nghe
        /// </summary>
        public bool _online = false;
        /// <summary>
        /// câu hỏi đang hỏi - trả lời
        /// </summary>
        private Question _nowQuestion = new Question();
        /// <summary>
        /// đẩy câu hỏi vào _nowQuestion
        /// </summary>
        public Question NowQuestion
        {
            set
            {
                _nowQuestion = value;
                foreach (pClient client in _PlayerList)
                {
                    client._playerID.Push(_nowQuestion);
                }
            }
            get
            {
                return _nowQuestion;

            }
        }
        /// <summary>
        /// cài đặt sẵn sàng cho các client trong _PlayerList 
        /// </summary>
        public bool GetReady
        {
            set
            {
                Random rand = new Random();
                _mainPlayer = (pClient)_PlayerList[rand.Next(_PlayerList.Count)];
                //_PlayerList.Remove((pClient)_PlayerList[index]);
                _playerLeft = _PlayerList.Count;
                foreach (pClient client in _PlayerList)
                {
                    if (client._playerID.Equals(_mainPlayer._playerID))
                    {//Do not remove order
                        client.ChosenOne = true;

                        ListViewItem.ListViewSubItem sub = new ListViewItem.ListViewSubItem();
                        sub.Tag = client;
                        GUI.EditSubItems(sub);
                    }
                    else
                    {
                        client.GETReady = value;

                        ListViewItem.ListViewSubItem sub = new ListViewItem.ListViewSubItem();
                        sub.Tag = client;
                        GUI.EditSubItems(sub);
                    }
                }
                _IsReady = value;

            }
            get
            {
                return _IsReady;
            }
        }
        /// <summary>
        /// tổng cộng đểm của các client trả lời sai để cộng cho người chơi chính
        /// </summary>
        public int PointOfWrong;
        /// <summary>
        /// mức độ của câu hỏi hiện thời
        /// </summary>
        public Level currentLV = Level.None;
        /// <summary>
        /// có người chiến thắng if true
        /// </summary>
        public bool HaveWinner;
        /// <summary>
        /// delegate dùng để beginInvoke cho CheckWinner
        /// </summary>
        /// <returns>bool</returns>
        private delegate bool d_checkWinner();
        /// <summary>
        /// sự kiển dùng để báo người chơi chính đã chọn xong mức độ
        /// </summary>
        public AutoResetEvent autoReset = new AutoResetEvent(false);
        /// <summary>
        /// Chống can thiệp vào CS - Critical Section
        /// </summary>
        private static object lockObj = new object();
        /// <summary>
        /// Chống can thiệp vào CS - Critical Section
        /// </summary>
        private static object lockObj1 = new object();
        #endregion
        
        #region Hàm khởi tạo
        public DauTruongServer()
        {

        }

        public DauTruongServer(Form gui, int playerCount)
        {
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _iPlayers = playerCount;
                _PlayerList = new ArrayList(_iPlayers);
                countdownEvent = new CountdownEvent(_PlayerList.Count);
                GUI = gui as Mainform;

                if (useLoopback == false)
                {
                    IPAddress[] addrList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    foreach (IPAddress ip in addrList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            _localAddr = ip;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Phương thức
        /// <summary>
        /// Bắt đầu lắng nghe
        /// </summary>
        public void StartServer()
        {
            IPEndPoint localEP = new IPEndPoint(_localAddr, _port);
           

            _listener.Bind(localEP);
            _listener.Listen(10);
            _online = true;
            LingerOption lo = new LingerOption(false, 0);
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);
            _listener.BeginAccept(new AsyncCallback(OnConnectRequest), _listener);

        }
        /// <summary>
        /// Dừng lắng nghe
        /// </summary>
        public void StopServer()
        {
            if (!(_listener.Poll(1, SelectMode.SelectRead) && _listener.Available == 0))
            {
                foreach (pClient player in _PlayerList)
                {
                    player.Disconnect();
                }


            }

        }
        /// <summary>
        /// gửi cho người chơi chính chọn mức độ khó - dễ
        /// </summary>
        public void ASKChoice()
        {

            autoReset.Reset();
            _mainPlayer.sock.Send(_ENCODE.GetBytes("CHO"));
            _mainPlayer.SetupForRecv(this);
            autoReset.WaitOne();

        }
        /// <summary>
        /// gửi câu hỏi 
        /// </summary>
        public void SendQuestionPackage()
        {

            //countdownEvent = new CountdownEvent(_PlayerList.Count);
            countdownEvent.Reset(_PlayerList.Count);
            foreach (pClient client in _PlayerList)
            {
                if (
                    //!client.ChosenOne && 
                    client.GETReady)

                    client.SetUpSend(this);
               
                //if (client.ChosenOne) client._playerID.POINTS += PointOfWrong;
                //client.sock.Send(_ENCODE.GetBytes(client._playerID.q_List[0].strContent.ToString()));
            }
        
            //PointOfWrong = 0;
            countdownEvent.Wait();


            //GUI.SetText("Question: " + client._playerID.q_List.Count + "/20", GUI.QuesLabel);
        }
        /// <summary>
        /// kiểm tra người thắng cuộc
        /// </summary>
        /// <returns></returns>
        public bool CheckWinner()
        {
            if (_PlayerList.Count == 1 && ((pClient)_PlayerList[0]).ChosenOne)
            {
                pClient pC = (pClient)_PlayerList[0];

                return true;
            }
            else return false;

        }
        #endregion
        
        #region các phương thức bất đồng bộ
        /// <summary>
        /// Trong lúc client yêu câu kết nối
        /// </summary>
        /// <param name="ar"></param>
        private void OnConnectRequest(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            if (!_online)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();

            }
            else
                try
                {
                    if (_PlayerList.Count < _iPlayers)
                    {
                        int readBytes = handler.Receive(buffer);
                        if (readBytes > 0)
                        {
                            //Remove all null-byte chars in buffer
                            string Msg = _ENCODE.GetString(buffer).Trim('\0');
                            cmdMsg cmd = (cmdMsg)Enum.Parse(typeof(cmdMsg), Msg.Substring(0, 4));
                            if (cmd == cmdMsg.HELO)
                            {
                                PlayerID id = new PlayerID(Msg.Remove(0, 4), _PlayerList.Count);
                                NewConnection(handler, id);
                            }
                        }
                        listener.BeginAccept(new AsyncCallback(OnConnectRequest), listener);
                    }
                    else
                    {

                        handler.Send(_ENCODE.GetBytes("REJ\nServer's full"));
                        //handler.Shutdown(SocketShutdown.Both);
                        //handler.Close(5);

                    }
                }
                catch (SocketException socketException)
                {
                    if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
                    {
                        handler.Close();

                    }
                }


        }
        /// <summary>
        /// tạo kết nối mới cho client dc chấp nhận 
        /// </summary>
        /// <param name="clientSock"></param>
        /// <param name="id"></param>
        private void NewConnection(Socket clientSock, PlayerID id)
        {
            pClient client = new pClient(clientSock, id);

            _PlayerList.Add(client);
            ListViewItem item = new ListViewItem();
            ListViewItem.ListViewSubItemCollection subitems = new ListViewItem.ListViewSubItemCollection(item);
            item.Tag = client;

            GUI.EditListView(item, subitems);

            byte[] Helo = _ENCODE.GetBytes("HELO\n" + GUI.timeOutSecond.ToString() + "\n" + GUI._ContainerList.Capacity.ToString() + "\n" + "Connected at: " + DateTime.Now.ToLongTimeString());
            client.sock.Send(Helo);
            client.SetupForRecv(this);
        }
        /// <summary>
        /// trong lúc nhận
        /// </summary>
        /// <param name="ar"></param>
        public void OnReceive(IAsyncResult ar)
        {
            pClient client = (pClient)ar.AsyncState;
            if (!client.IsConnected())
            {
                try
                {
                    client.Disconnect();
                    _PlayerList.Remove(client);
                    GUI.RemoveListViewItem(client);
                    if (!_IsReady)
                    {
                        _listener.BeginAccept(new AsyncCallback(OnConnectRequest), _listener);
                    }

                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }



            }
            byte[] aryRet = client.GetReceivedData(ar);
            if (aryRet.Length < 1)
            {
                client.Disconnect();
                _PlayerList.Remove(client);
                GUI.RemoveListViewItem(client);
                return;
            }
            else
            {

                string msg = DauTruongServer._ENCODE.GetString(aryRet);
                msg = msg.Trim('\0');
                cmdMsg CMD = (cmdMsg)Enum.Parse(typeof(cmdMsg), msg.Substring(0, 3));
                //string cmd = msg.Substring(0, 3).ToUpper();
                if (CMD == cmdMsg.CHO)
                //lock (lockObj1)
                {
                    Level lv = (Level)Enum.Parse(typeof(Level), msg.Remove(0, 3));
                    currentLV = lv;
                    autoReset.Set();
                }
                else if (CMD == cmdMsg.ANW)
                {
                    client._playerID.AnsweredQuestions++;
                    Answer ans = (Answer)Enum.Parse(typeof(Answer), msg.Remove(0, 3).ToUpper());

                    if (client._playerID.CheckRightAns(ans))
                        lock (lockObj)
                        {
                            client._playerID.POINTS += 1000 / _playerLeft;
                            ListViewItem.ListViewSubItem sub = new ListViewItem.ListViewSubItem();
                            sub.Tag = client;
                            GUI.EditSubItems(sub);
                        }
                    else
                        lock (lockObj)
                        {
                            client.Kick = true;
                            foreach (pClient pC in _PlayerList)
                            {
                                if (pC.ChosenOne) pC._playerID.POINTS += client._playerID.POINTS;
                            }

                            _PlayerList.Remove(client);
                            ListViewItem.ListViewSubItem sub = new ListViewItem.ListViewSubItem();
                            sub.Tag = client;
                            GUI.EditSubItems(sub);
                            d_checkWinner d = new d_checkWinner(CheckWinner);
                            d.BeginInvoke(new AsyncCallback(CheckWinnerCallback), d);

                        }
                    countdownEvent.Signal();
                }

                else if (CMD == cmdMsg.ONE)
                {
                    client._playerID.AnsweredQuestions++;
                    Answer ans = (Answer)Enum.Parse(typeof(Answer), msg.Remove(0, 3).ToUpper());
                    if (client._playerID.CheckRightAns(ans))
                        lock (lockObj)
                        {
                            /*client._playerID.POINTS += PointOfWrong;*/

                            ListViewItem.ListViewSubItem sub = new ListViewItem.ListViewSubItem();
                            sub.Tag = client;
                            GUI.EditSubItems(sub);
                        }
                    else
                        lock (lockObj)
                        {//Pause the game and choose a new main player 
                            client.Kick = true;
                            foreach (pClient pC in _PlayerList)
                            {
                                pC.sock.Send(_ENCODE.GetBytes("END\n" + "Người chơi chính đã bị loại cảm ơn bạn đả tham gia trò chơi !"));
                                //_PlayerList.Remove(pC);
                            }

                        }
                    countdownEvent.Signal();
                }

            }
            client.SetupForRecv(this);
        }
        /// <summary>
        /// trong lúc kiểm tra người thắng cuộc
        /// </summary>
        /// <param name="ar"></param>
        private void CheckWinnerCallback(IAsyncResult ar)
        {
            d_checkWinner d = (d_checkWinner)ar.AsyncState;
            HaveWinner = d.EndInvoke(ar);
            if (HaveWinner)
            {
                _mainPlayer.sock.Send(_ENCODE.GetBytes("END\n" + "Chúc mừng,Bạn là người chiến thắng !!!"));
                //if (!countdownEvent.IsSet) countdownEvent.Signal(countdownEvent.InitialCount - countdownEvent.CurrentCount);
            }
            else return;
        }
        /// <summary>
        /// trong lúc gửi
        /// </summary>
        /// <param name="ar"></param>
        public void SendCallback(IAsyncResult ar)
        {
            pClient SocketSent = (pClient)ar.AsyncState;
            int iBytSent;
            try
            {
                iBytSent = SocketSent.sock.EndSend(ar);
            }
            catch (SocketException sckEx)
            {
                MessageBox.Show("Error: " + sckEx.ErrorCode, sckEx.Message, MessageBoxButtons.OK);
                SocketSent.Disconnect();
                _PlayerList.Remove(SocketSent);
            }
        }
        #endregion
    }

    public class pClient
    {
        #region Biến
        /// <summary>
        /// Buffer size = 2 MB
        /// </summary>
        public const int B_SIZE = 2048;
        /// <summary>
        /// Buffer
        /// </summary>
        public byte[] buffer = new byte[B_SIZE];
        /// <summary>
        /// Socket dùng của từng client kết nối sẽ dc nạp vào đây
        /// </summary>
        public Socket sock = null;
        /// <summary>
        /// Dựng lại chuỗi nhận dc hay truyền đi
        /// </summary>
        public StringBuilder sb = new StringBuilder();
        /// <summary>
        /// Loại client trả lời sai
        /// </summary>
        public bool Kick
        {
            set
            {
                if (value)
                {
                    sock.Send(DauTruongServer._ENCODE.GetBytes("KICK\nBạn đã bị loại"));
                    IsReady = !value;
                    _status = "Eliminated";
                }
            }
            get { return !IsReady; }
        }
        /// <summary>
        /// Nhận dạng của 1 client 
        /// </summary>
        public PlayerID _playerID;
        /// <summary>
        /// biến private của ChoseOne 
        /// </summary>
        private bool IsChosenOne = false;
        /// <summary>
        /// biến chỉ đọc của GETReady
        /// </summary>
        private bool IsReady = false;
        /// <summary>
        /// trạng thái của pClient 
        /// </summary>
        public string _status = string.Empty;
        /// <summary>
        /// Cài đặt đây là người chơi chính
        /// </summary>
        public bool ChosenOne
        {
            set
            {
                IsChosenOne = value;
                IsReady = IsChosenOne;
                if (this.IsConnected())
                {
                    sock.Send(DauTruongServer._ENCODE.GetBytes("ONE\n"));
                    _status = "The One";
                }

            }
            get { return IsChosenOne; }
        }
        /// <summary>
        /// Cài đặt sẵn sàng để gửi câu hỏi 
        /// </summary>
        public bool GETReady
        {
            set
            {

                if (this.IsConnected())
                {

                    IsReady = value;
                    sock.Send(DauTruongServer._ENCODE.GetBytes("READY\n"));
                    _status = "Ready";

                }
                else IsReady = false;
            }
            get
            {
                return IsReady;
            }
        }
        #endregion
        
        #region Hàm khởi tạo 
        public pClient()
        {

        }
        /// <summary>
        /// hàm khởi tạo pClietn từ socket đang muốn kết nối vào và ID của client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        public pClient(Socket client,PlayerID id)
        {
            sock = client;
            _playerID = id;
            _status = "Connected";
        }
        #endregion 
        
        #region Phương thức
        /// <summary>
        /// Hủy kết nối với bên client 
        /// </summary>
        public void Disconnect()
        {
            if (this.IsConnected())
            {
                sock.Shutdown(SocketShutdown.Both);
                sock.Close(1);
            }
        }
        /// <summary>
        /// Cài đặt chuẩn bị cho việc send
        /// </summary>
        /// <param name="serv"></param>
        public void SetUpSend(DauTruongServer serv)
        {
            try
            {
                //sock.Send(DauTruongServer._ENCODE.GetBytes("READY"));

                AsyncCallback sendCallback = new AsyncCallback(serv.SendCallback);

                sb.Clear();
                sb.AppendLine("QUES");
                sb.AppendLine(serv.NowQuestion.strContent.ToString());
                serv.buffer = DauTruongServer._ENCODE.GetBytes(sb.ToString());
                sock.BeginSend(serv.buffer, 0, serv.buffer.Length, SocketFlags.None, sendCallback, this);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }
        /// <summary>
        /// Kiểm tra pClient này còn đang kết nối không 
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            try
            {
                return !(sock.Poll(1, SelectMode.SelectRead) && sock.Available == 0);
            }
            catch (SocketException) { return false; }
        }
        /// <summary>
        /// cài đặt trước cho việc nhận 
        /// </summary>
        /// <param name="serv"></param>
        public void SetupForRecv(DauTruongServer serv)
        {
            try
            {
                AsyncCallback receiveData = new AsyncCallback(serv.OnReceive);
                sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, receiveData, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Recieve callback setup failed! {0}", ex.Message);
            }
        }
        /// <summary>
        /// Nhận dữ liệu và đưa vào trong mảng byte
        /// </summary>
        /// <param name="ar"></param>
        /// <returns> mảng byte</returns>
        public byte[] GetReceivedData(IAsyncResult ar)
        {
            int nBytesRec = 0;
            try
            {
                nBytesRec = sock.EndReceive(ar);

            }
            catch { }
            byte[] byReturn = new byte[nBytesRec];

            Array.Copy(buffer, byReturn, nBytesRec);


            return byReturn;
        }
        #endregion
    
    }
}
