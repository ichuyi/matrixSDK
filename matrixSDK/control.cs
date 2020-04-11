using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace matrixSDK
{
    public class MatrixControl
    {
        private string localhost = "127.0.0.1";
        private int port = 8080;
        private int maxConn = 100;
        private Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private List<Socket> clients = new List<Socket>();

        public MatrixControl()
        {
            Init();
        }
        /// <summary>
        /// 构造函数的参数为监听端口和最大排队请求数目
        /// </summary>
        /// <param name="port"></param>
        /// <param name="maxConn"></param>
        public MatrixControl(int port, int maxConn)
        {
            this.port = port;
            this.maxConn = maxConn;
            Init();
        }
        private void Init()
        {
            Console.WriteLine("服务端监听的IP为:{0},端口为:{1},最大排队连接请求数为:{2}", this.localhost, this.port, this.maxConn);
            this.server.Bind(new IPEndPoint(IPAddress.Parse(this.localhost), this.port));
            this.server.Listen(this.maxConn);
            Console.WriteLine("服务端已经启动");
            Thread myThread = new Thread(this.ListenClientConnect);//通过多线程监听客户端连接  
            myThread.Start();
        }
        private void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = this.server.Accept();
                lock (this.clients)
                {
                    this.clients.Add(clientSocket);
                }
                Console.WriteLine("与{0}已经建立连接", clientSocket.RemoteEndPoint);
            }
        }
        private void SendMessage(string msg)
        {
            for (int i = 0; i < this.clients.Count;)
            {
                try
                {
                    byte[] vs = System.Text.Encoding.Default.GetBytes(msg);
                    this.clients[i].Send(vs, vs.Length, 0);
                    Console.WriteLine("成功给{0}发送信息:{1}", this.clients[i].RemoteEndPoint, msg);
                    i++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("给{0}发送失败，错误信息为:{1}", this.clients[i].RemoteEndPoint, e.Message);
                    this.clients[i].Shutdown(SocketShutdown.Both);
                    this.clients[i].Close();
                    lock (this.clients)
                    {
                        this.clients.RemoveAt(i);
                    }
                }
            }
        }
        /// <summary>
        /// 输入的参数为矩阵信息，应该是每个电机的状态，用0、1表示，以.分隔，如"0.1.0.1"
        /// 若返回true则表示输入的参数合法，会执行命令
        /// 若返回false则表示输入的参数不合法，不会执行命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool SendCommand(string command)
        {
            string[] input = command.Split('.');
            Regex m = new Regex("^[0-9]+$");
            foreach (string i in input)
            {
                if (!m.Match(i).Success)
                {
                    return false;
                }
            }
            command = command.Replace("0", "2");
            this.SendMessage(command + " ");
            return true;
        }
        /// <summary>
        /// 重置所有步进电机，输入参数为步进电机的个数
        /// </summary>
        /// <param name="count"></param>
        public void Reset(int count)
        {
            string[] cmd = new string[count];
            for (int i = 0; i < count; i++)
            {
                cmd[i] = "0";
            }
            this.SendMessage(string.Join(".", cmd) + " ");
        }
        ~MatrixControl()
        {
            for (int i = 0; i < this.clients.Count; i++)
            {
                this.clients[i].Shutdown(SocketShutdown.Both);
                this.clients[i].Close();
                Console.WriteLine("已关闭与{0}的连接", this.clients[i].RemoteEndPoint);
            }
        }
    }
}
