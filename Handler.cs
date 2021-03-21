using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChattingServer
{
    class Handler
    {
        Socket socket;
        Dictionary<Socket, string> clientList = new Dictionary<Socket, string>();

        public delegate void MessageDisplayHandler(string message, string user_name);
        public event MessageDisplayHandler OnReceived;

        public delegate void FileReceiveHandler(Socket socket, string fileName);
        public event FileReceiveHandler OnFileReceived;

        public delegate void DisconnectedHandler(Socket socket);
        public event DisconnectedHandler OnDisconnected;

        public void StartClient(Socket socket, Dictionary<Socket, string> clientList)
        {
            this.socket = socket;
            this.clientList = clientList;

            Thread t1 = new Thread(ChatStart);
            t1.IsBackground = true;
            t1.Start();
        }

        private void ChatStart()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    string msg = String.Empty;
                    int length = 0;

                    length = socket.Receive(buffer);
                    msg = Encoding.UTF8.GetString(buffer, 0, length);
                    if (msg.Contains("Command_FileSendMode"))
                    {
                        int msgLength = msg.IndexOf('-');
                        string fileName = msg.Substring(msgLength + 1, msg.Length - msgLength - 1);
                        OnFileReceived(socket, fileName);
                    }
                    else
                    {
                        OnReceived(msg, clientList[socket].ToString());
                    }
                }
            } 
            catch(SocketException se)
            {
                if(socket != null)
                {
                    if(OnDisconnected != null)
                    {
                        OnDisconnected(socket);
                    }
                }

                socket.Close();
            }
            catch (Exception e)
            {
                if (socket != null)
                {
                    if (OnDisconnected != null)
                    {
                        OnDisconnected(socket);
                    }
                }

                socket.Close();
            }
        }
    }
}
