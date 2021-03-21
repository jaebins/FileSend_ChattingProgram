using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChattingClient
{
    public partial class Form1 : Form
    {
        int userCounter = 0;
        Socket socket;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread t1 = new Thread(ServerJoin);
            t1.IsBackground = true;
            t1.Start();
        }

        private void ServerJoin()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse("118.47.113.239"), 7000);
                socket.Connect(ep);
            } 
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }

            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int length = socket.Receive(buffer);
                    string msg = Encoding.UTF8.GetString(buffer, 0, length);
                    if (msg.Contains("님이 들어왔습니다. -"))
                    {
                        int joinMsgLength = msg.IndexOf('-');
                        string joinMsg = msg.Substring(msg.Length - 1, 1);
                        userCounter = Int32.Parse(joinMsg);
                    }
                    if (msg.Contains("Command_FileSending-"))
                    {
                        int msgLength = msg.IndexOf('-');
                        string fileName = msg.Substring(msgLength + 1, msg.Length - msgLength - 1);
                        buffer = new byte[4];
                        socket.Receive(buffer);
                        int fileLength = BitConverter.ToInt32(buffer, 0);

                        FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + fileName, FileMode.Create, FileAccess.Write);
                        BinaryWriter bw = new BinaryWriter(fs);

                        buffer = new byte[1024];
                        int totalLength = 0;

                        while (totalLength < fileLength)
                        {
                            int recevieLength = socket.Receive(buffer);
                            bw.Write(buffer, 0, recevieLength);
                            totalLength += recevieLength;
                        }

                        fs.Close();
                        bw.Close();

                        MessageBox.Show(fileName " 이 다운로드가 되었습니다.");
                    }
                    else
                    {
                        ReadText(msg);
                    }
                }
                catch(SocketException se)
                {
                    MessageBox.Show(se.Message);
                    break;
                }catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                    break;
                }
            }

            socket.Close();
            Application.Exit();
        }

        private void but_SendMessage_Click(object sender, EventArgs e)
        {
            SendMessage(textBox_InputText.Text);
        }

        private void but_SendFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();

            if(ofd.FileName.Length > 0)
            {
                if(userCounter > 1)
                {
                    SendMessage("Command_FileSendMode-" + ofd.SafeFileName);
                    string filePath = ofd.FileName;

                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    BinaryReader reader = new BinaryReader(fs);

                    for(int i = 0; i < userCounter - 1; i++)
                    {
                        // 파일 크기를 구하고 서버로 보냄
                        int fileLength = (int)fs.Length;
                        byte[] buffer = BitConverter.GetBytes(fileLength);
                        socket.Send(buffer);

                        // 파일을 송신
                        int count = fileLength / 4096 + 4;
                        for(int j = 0; j < count; j++)
                        {
                            buffer = reader.ReadBytes(4096);
                            socket.Send(buffer);
                        }
                    }

                    reader.Close();
                    fs.Close();
                }
            }
        }

        private void textBox_InputText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar.Equals(Keys.Enter))
            {
                SendMessage(textBox_InputText.Text);
            }
        }

        private void SendMessage(string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            textBox_InputText.Text = "";
        }

        private void ReadText(string msg)
        {
            if (textBox_ShowText.InvokeRequired)
            {
                textBox_ShowText.Invoke(new MethodInvoker(delegate ()
                {
                    textBox_ShowText.AppendText(msg + Environment.NewLine + Environment.NewLine);
                }));
            }
            else
            {
                textBox_ShowText.AppendText(msg + Environment.NewLine + Environment.NewLine);
            }
        }
    }
}
