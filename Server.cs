using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChattingServer
{
    class Server
    {
        Socket server;
        public static Socket sendClient;
        int counter = 0;
        Dictionary<Socket, string> clientList = new Dictionary<Socket, string>();

        static void Main(string[] args)
        {
            new Server();
        }

        public Server()
        {
            Thread t1 = new Thread(ServerStart);
            t1.Start();
        }

        private void ServerStart()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint point = new IPEndPoint(IPAddress.Any, 7000);
            server.Bind(point);
            server.Listen(10);
            Console.WriteLine("서버가 열렸습니다.");

            while (true)
            {
                try
                {
                    Socket clientSocket = server.Accept();
                    counter++;
                    string user_name = clientSocket.RemoteEndPoint.ToString();
                    clientList.Add(clientSocket, user_name);
                    SendMessage(user_name + "님이 들어왔습니다. -" + counter.ToString(), "", true);
                    Console.WriteLine(clientSocket.RemoteEndPoint.ToString());

                    Handler handler = new Handler();
                    handler.OnDisconnected += new Handler.DisconnectedHandler(OnDisconnected);
                    handler.OnReceived += new Handler.MessageDisplayHandler(OnReceived);
                    handler.OnFileReceived += new Handler.FileReceiveHandler(OnFileReceived);
                    handler.StartClient(clientSocket, clientList);
                }
                catch(SocketException se)
                {
                    Console.WriteLine(se.Message);
                    break;
                } 
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
            }

            server.Close();
        }

        private void OnReceived(string message, string user_name)
        {
            SendMessage(message, user_name, false);
        }

        private void OnDisconnected(Socket socket)
        {
            if(clientList.ContainsKey(socket))
            {
                counter--;
                clientList.Remove(socket);
            }
        }

        private void OnFileReceived(Socket sendSocket, string fileName)
        {
            foreach(var client in clientList)
            {
                Socket socket = client.Key as Socket;
                bool Confirm_Sender = client.Key.RemoteEndPoint.ToString().Equals(sendSocket.RemoteEndPoint.ToString());

                if(!Confirm_Sender)
                {
                    string sendSignal = "Command_FileSending-" + fileName;
                    byte[] buffer = Encoding.UTF8.GetBytes(sendSignal);
                    socket.Send(buffer);

                    buffer = new byte[4];
                    sendSocket.Receive(buffer);
                    int fileLength = BitConverter.ToInt32(buffer, 0);
                    Console.WriteLine(fileLength);

                    buffer = BitConverter.GetBytes(fileLength);
                    socket.Send(buffer);

                    buffer = new byte[4096];
                    int totalLength = 0;

                    while (totalLength < fileLength)
                    {
                        int receiveLength = sendSocket.Receive(buffer);
                        socket.Send(buffer, 0, receiveLength, SocketFlags.None);
                        totalLength += receiveLength;
                        Console.WriteLine(totalLength);
                    }
                }
            }
        }

        private void SendMessage(string message, string user_name, bool flag)
        {
            foreach(var client in clientList)
            {
                Socket socket = client.Key as Socket;
                if (flag)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                }
                else
                {
                    string msg = user_name + " : " + message;
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                }
            }
        }
    }
}
