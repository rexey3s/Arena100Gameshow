using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace DoAnClient
{
   
    enum cmdMsg
    {
        /// <summary> gửi tín hiệu báo client sẵn sàng</summary>
        READY,
        /// <summary> default - tránh lỗi</summary>
        NONE,
        /// <summary> nhận tín hiệu gửi câu hỏi</summary>
        QUES,
        /// <summary> nhận tín hiệu thông báo</summary>
        MSG,
        /// <summary> nhận tín hiệu chào khi từ server khi mới kết nối vào</summary>
        HELO,
        /// <summary> nhận tín hiệu từ chối kết nối khi server's full</summary>
        REJ,
        /// <summary> nhận tín hiệu chọn người chơi chính</summary>
        ONE,
        /// <summary> gửi tín hiệu trả lời từ client</summary>
        ANW,
        /// <summary> nhận tín hiệu báo loại client</summary>
        KICK,
        /// <summary> nhận tín hiệu tạm dừng </summary>
        PAUSE,
        /// <summary> nhận tín hiệu kết thúc trò chơi</summary>
        END,
        /// <summary> gửi và nhận tín hiệu cho người chơi chính chọn mức độ khó dễ</summary>
        CHO
    }
    
    enum ANS
    {
        A,B,C,D
    }
    
    class PlayerClient
    {
        #region Các biến
        /// <summary> UFT-8 encoder  </summary>
        public static Encoding _ENCODE = new UTF8Encoding();
        /// <summary> kết quả của chuỗi nhận dc sau khi dc lấy đi phần cmdMsg </summary>
        public string _result = string.Empty;
        /// <summary> Network stream - dùng để gửi </summary>
        public NetworkStream _NS;
        /// <summary> thời gian đếm ngược để trả lời câu hỏi  </summary>
        public long TimeOutSeconds;
        /// <summary> có phải người chơi chính không </summary>
        public bool TheOne = false;
        /// <summary> trạng thái  </summary>
        public string _status = string.Empty;
        /// <summary> form chính </summary>
        private Form1 GUI = null;
        /// <summary> Sẵn sàng nhận câu hỏi -->if true </summary>
        private bool GETReady = false;
        /// <summary> port mặc định của server  </summary>
        private static int _remotePort = 23456;
        /// <summary> tcpClient   </summary>
        private TcpClient _server;
        
        #endregion
       
        #region Phương thức
        /// <summary>
        /// Hàm khởi tạo
        /// </summary>
        /// <param name="gui"></param>
        public PlayerClient(Form gui)
        {
            try
            {
                //IPEndPoint remoteAddr = new IPEndPoint(IPAddress.Parse(IP), _remotePort);
                _server = new TcpClient(AddressFamily.InterNetwork);
                GUI = gui as Form1;

                // _status = "Connected";



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                _status = "Disconnected";
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public bool Connect(string ip, string playerName)
        {
            IPAddress ipAddr = IPAddress.Any;
            if (IPAddress.TryParse(ip, out ipAddr))
            {
                EndPoint ep = new IPEndPoint(ipAddr, _remotePort);
                _server.Connect(ep as IPEndPoint);
                if (_server.Connected)
                {
                    byte[] buff = new byte[512];
                    buff = _ENCODE.GetBytes("HELO " + playerName);
                    _NS = _server.GetStream();
                    _NS.Write(buff, 0, buff.Length);
                    _NS.Flush();
                }
                return true;
            }
            else return false;
        }
        /// <summary>
        /// bắt đầu nhận thông tin từ server
        /// </summary>
        /// <returns></returns>
        public bool OpenPhase()
        {
            if (_server.Connected)
            {

                //_NS = _server.GetStream();
                //_NS.Write(_ENCODE.GetBytes("echo ..."), 0, 512);
                StateObj state = new StateObj(_server.Client);
                _server.Client.BeginReceive(state.buffer, 0, StateObj.BufferSize, SocketFlags.None, new AsyncCallback(OnReceive), state);



                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Trong lúc nhận
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceive(IAsyncResult ar)
        {
            StateObj state = (StateObj)ar.AsyncState;
            Socket handler = state.WorkingSock;
            int readBytes;
            if (handler.Connected)
            {
                _status = "Receiving...";
                GUI.StatusLB = _status;
                try
                {
                    readBytes = handler.EndReceive(ar);
                    if (readBytes > 0)
                    {



                        // There  might be more data, so store the data received so far.
                        state.sb.Remove(0, state.sb.Length);
                        state.sb.Append(_ENCODE.GetString(state.buffer, 0, readBytes));

                        _result = state.sb.ToString();
                        cmdMsg CMD = filterCMD(ref _result);
                        if (CMD == cmdMsg.QUES && GETReady)
                        {
                            GUI.Unpack(_result);
                            if (!TheOne)
                            {
                                GUI.Invoke(new Action(() =>
                                {

                                    GUI.timer1.Enabled = true;
                                }));
                                //GUI.timer1.Enabled = true;
                            }
                            handler.BeginReceive(state.buffer, 0, StateObj.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);
                            _status = "Continuously receiving...";
                            GUI.StatusLB = _status;
                        }
                        else if (CMD == cmdMsg.CHO)
                        {
                            byte[] buff = new byte[128];
                            DialogResult dlgRe = MessageBox.Show("<Yes> --> Dễ\n<No> --> Khó", "Bạn muốn chọn câu dễ hay khó", MessageBoxButtons.YesNo);
                            if (dlgRe == DialogResult.Yes)
                            {
                                buff = _ENCODE.GetBytes("CHO Easy");

                            }
                            else if (dlgRe == DialogResult.No)
                            {
                                buff = _ENCODE.GetBytes("CHO Hard");
                            }
                            else
                            {
                                buff = _ENCODE.GetBytes("CHO None");
                            }
                            _NS.Flush();
                            _NS.Write(buff, 0, buff.Length);
                            handler.BeginReceive(state.buffer, 0, StateObj.BufferSize, 0,
                         new AsyncCallback(OnReceive), state);
                            _status = "Continuously receiving...";
                            GUI.StatusLB = _status;
                        }
                        else if (CMD == cmdMsg.REJ)//Server's full so turn off the socket politely
                        {
                            _status = _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close(2);
                        }
                        else if (CMD == cmdMsg.MSG)
                        {
                            _status = _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;

                        }
                        else if (CMD == cmdMsg.ONE)
                        {
                            _status = "Bạn dc chọn là người chơi chính " + _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;
                            GUI.SetTextbox("Bạn là người chơi chính", GUI.label1);
                            this.TheOne = true;
                            GETReady = true;
                            GUI.Invoke(new Action(() => GUI.TurnPlayModeOn = GETReady));
                            handler.BeginReceive(state.buffer, 0, StateObj.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);
                        }
                        else if (CMD == cmdMsg.READY)
                        {
                            _status = _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;
                            GETReady = true;
                            GUI.Invoke(new Action(() => GUI.TurnPlayModeOn = GETReady));
                            handler.BeginReceive(state.buffer, 0, StateObj.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);
                        }
                        else if (CMD == cmdMsg.KICK)//
                        {
                            _status = _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;
                            //handler.Shutdown(SocketShutdown.Receive);
                            //handler.Close(2);
                            //GETReady = false;
                        }
                        else if (CMD == cmdMsg.HELO)//initial 
                        {
                            //_status = _result.Trim('\n').Trim('\r');
                            string[] initInfo = _result.Split('\n');
                            TimeOutSeconds = int.Parse(initInfo[0]);
                            GUI.Invoke(new Action(() => { GUI.lTickSeconds = TimeOutSeconds; }));
                            _status = initInfo[2].Trim('\r').Trim('\n');
                            GUI.StatusLB = _status;
                            handler.BeginReceive(state.buffer, 0, StateObj.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);
                        }
                        else if (CMD == cmdMsg.END)
                        {
                            _status = _result.Trim('\n').Trim('\r');
                            GUI.StatusLB = _status;
                            //handler.Shutdown(SocketShutdown.Both);
                            //handler.Close(2);

                        }



                    }
                }

                catch (SocketException socketException)
                {
                    //WSAECONNRESET, the other side closed impolitely
                    if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
                    {
                        handler.Close();

                    }
                }
            }
        }
        /// <summary>
        /// Đọc lệnh cmdMsg - xử lý chuỗi điều khiển từ server
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static cmdMsg filterCMD(ref string s)
        {
            cmdMsg cmd = cmdMsg.NONE;
            try
            {
                string[] ArraySTR = s.Split('\n');
                string cmdMSG = ArraySTR[0];
                cmd = (cmdMsg)Enum.Parse(typeof(cmdMsg), cmdMSG);
                StringBuilder s_builder = new StringBuilder();
                for (int i = 1; i < ArraySTR.Length; i++)
                {
                    s_builder.AppendLine(ArraySTR[i]);
                }
                s = s_builder.ToString();
                return cmd;
            }
            catch
            {
                return cmd;


            }
        }
        #endregion

        #region Ping static method
        static CountdownEvent countdown;
        //static int hostCount = 0;
        static int upCount = 0;

        /// <summary> các host ping dc</summary>
        public static List<IPAddress> pingSuccesslist = new List<IPAddress>();
        private static object lockObj = new object();
        const bool resolveNames = true;
        /// <summary>
        /// sự kiện ping hoàn thành
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                if (resolveNames)
                {
                    try
                    {
                        pingSuccesslist.Add(IPAddress.Parse(ip));
                    }
                    catch
                    {
                        pingSuccesslist.Add(IPAddress.Parse(ip));
                    }
                    //Debug.WriteLine("{0}  is up: ({1} ms)", ip, e.Reply.RoundtripTime);
                }
                else
                {
                    //Debug.WriteLine("{0} is up: ({1} ms)", ip, e.Reply.RoundtripTime);
                }
                lock (lockObj)
                {
                    upCount++;
                }
            }
            else if (e.Reply == null)
            {
                Console.WriteLine("Pinging {0} failed. (Null Reply object?)", ip);
            }
            countdown.Signal();
        }
        /// <summary>
        /// Ping để tìm kiếm host đang onl
        /// </summary>
        /// <param name="ipBase"></param>
        public static void PingToFind(String ipBase)
        {
            countdown = new CountdownEvent(1);
            for (int i = 1; i < 255; i++)
            {
                string ip = ipBase + i.ToString();

                Ping p = new Ping();
                p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                countdown.AddCount();
                p.SendAsync(ip, 100, ip);

            }
            countdown.Signal();
            countdown.Wait();

        }
        /// <summary>
        /// tìm trong các host onl xem có cái nào dang sử dụng port 9999 để host ko
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> FindHost()
        {
            //List<IPAddress> hosts = new List<IPAddress>();

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress myIP = null;
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip != IPAddress.Loopback)
                    myIP = ip;
            }

            byte[] byteIP = myIP.GetAddressBytes();
            String ipBase = byteIP[0].ToString() + "." + byteIP[1].ToString() + "." + byteIP[2].ToString() + ".";
            PingToFind(ipBase);

            //foreach (IPAddress ip in pingSuccesslist)
            //    try
            //    {
            //        Socket scanPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //        //try to connect
            //        //Socket scanPort = _server.Client;
            //        scanPort.Connect(ip, _remotePort);
            //        if (scanPort.Connected == true)  // if successful => something is listening on this port
            //        {




            //            hosts.Add(ip);
            //            hostCount++;
            //            scanPort.Shutdown(SocketShutdown.Both);
            //            scanPort.Disconnect(true);
            //        }
            //        //else -. goes to exception
            //    }
            //    catch (SocketException)
            //    {
            //        //TODO: if you want, do smth here
            //        //Console.WriteLine("\tDIDN'T work at " + ip);
            //    }
            //    catch (Exception) { }
            return pingSuccesslist;
            //return new List<IPAddress>() { IPAddress.Parse("127.0.0.1") };
        }
        #endregion
        
        #region State Object
        /// <summary>
        /// State object dùng truyền data từ cho các phương thức bất đồng bộ
        /// </summary>
        internal class StateObj
        {
            public const int BufferSize = 1024;
            //
            public byte[] buffer = new byte[BufferSize];
            //
            public Socket WorkingSock = null;
            //
            public StringBuilder sb = new StringBuilder();
            //
            public StateObj(Socket s)
            {
                WorkingSock = s;
            }
        }
#endregion
        

    }
}
