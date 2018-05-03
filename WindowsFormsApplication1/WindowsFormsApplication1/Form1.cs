//单元3：指纹门禁
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //声明数据库操作相关变量
        OleDbConnection conn = new OleDbConnection();
        OleDbCommand comm = new OleDbCommand();
        OleDbDataAdapter adapter = new OleDbDataAdapter();
        DataSet ds = new DataSet();

        int DC;//设备状态
        bool DC_switch = false;//指纹是否开启
        int EnrollNum = 0;

        //表单装入事件，连接数据库
        private void Form1_Load(object sender, EventArgs e)
        {
            //创建并打开连接
            conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Database1.accdb;Persist Security Info=False;";
            conn.Open();
            comm.Connection = conn;
            //执行查询命令
            comm.CommandText = "select * from stuffTab";
            adapter.SelectCommand = comm;
            OleDbCommandBuilder builder = new OleDbCommandBuilder(adapter);
            //为表格控件建立数据源
            ds.Clear();
            adapter.Fill(ds, "stuffTab");
            dataGridView1.DataSource = ds.Tables["stuffTab"];
            //OpenDoor();
        }


        //比对指纹
        private void Verify_Click(object sender, EventArgs e)
        {
            MessageBox.Show("开始采集指纹...请将指纹按压指纹指纹仪1次...");
            axZKFPEngX1.BeginCapture();
        }

        //指纹登记OnEnroll事件
        private void axZKFPEngX1_OnEnroll(object sender, AxZKFPEngXControl.IZKFPEngXEvents_OnEnrollEvent e)
        {
            string fileName = "";
            axZKFPEngX1.CancelEnroll();//关闭登记状态
            if (!e.actionResult)
            {
                MessageBox.Show("登记指纹失败！");
                return;
            }

            EnrollNum++;
            fileName = "FingerPic" + EnrollNum + ".JPG";
            axZKFPEngX1.SaveJPG(fileName);//保存指纹图像
            pictureBox1.Image = Image.FromFile(fileName);
            string mb = axZKFPEngX1.GetTemplateAsString();//得到字符串格式的特征模板

            dataGridView1.CurrentRow.Cells["指纹"].Value = mb;
            adapter.Update(ds, "stuffTab");

            MessageBox.Show("登记指纹成功！");
        }

        //指纹比对OnCapture事件
        private void axZKFPEngX1_OnCapture(object sender, AxZKFPEngXControl.IZKFPEngXEvents_OnCaptureEvent e)
        {
            string fileName = "";
            axZKFPEngX1.CancelCapture();//关闭取验证指纹状态
            if (!e.actionResult)
            {
                MessageBox.Show("取验证模板失败！");
                return;
            }

            EnrollNum++;
            fileName = "FingerPic" + EnrollNum + ".JPG";
            axZKFPEngX1.SaveJPG(fileName);//保存指纹图像
            pictureBox1.Image = Image.FromFile(fileName);//显示指纹
            string mb = axZKFPEngX1.GetTemplateAsString();//得到字符串格式的特征模板

            bool flag = false;
            bool chang = false;
            for (int k = 0; k < dataGridView1.RowCount-1 ; k++)
            {
                string fingerFile = dataGridView1.Rows[k].Cells["指纹"].Value.ToString();
                if (axZKFPEngX1.VerFingerFromStr(ref fingerFile, mb, false, ref chang))
                {
                    MessageBox.Show("比对成功!");
                    //插入开启门禁
                    OpenDoor();
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                MessageBox.Show("比对失败！");
            }
        }

        //打开道闸
        private void OpenDoor()
        {
            TcpClient client = new TcpClient();//定义一个客户端

            client.Connect("127.0.0.1", 60006);//客户端连接地址

            NetworkStream ns = client.GetStream();//定义一个客户端流
            //打开道闸信号
            string dz;
            if(radioButton1.Checked)
            {
                dz = "N3000 -USER \"abc\" -PASSWORD \"123\" -OPEN \"m001-1号\"";
            }
            else
            {
                dz = "N3000 -USER \"abc\" -PASSWORD \"123\" -OPEN \"m002-1号\"";
            }
            Byte[] dat = Encoding.Default.GetBytes(dz);  //m001  m002
            //发送信号到服务器，由服务器远程打开道闸
            ns.Write(dat, 0, dat.Length);
            client.Close();//关闭客户端
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox0_Click_1(object sender, EventArgs e)
        {
            if (!DC_switch)
            {
                DC = axZKFPEngX1.InitEngine();
                if (DC == 0)
                {
                    pictureBox0.Image = Image.FromFile("开.jpg");
                    DC_switch = true;
                    MessageBox.Show("指纹识别仪初始化成功！");
                    Collecting.Enabled = true;
                    Verify.Enabled = true;
                }
                else
                {
                    MessageBox.Show("指纹识别仪初始化失败！");
                    return;
                }
            }
            else
            {
                axZKFPEngX1.EndEngine();
                pictureBox0.Image = Image.FromFile("关.jpg");
                DC_switch = false;
                MessageBox.Show("指纹仪关闭成功！");
                Collecting.Enabled = false;
                Verify.Enabled = false;
            }
        }
        //比对指纹
        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("开始采集指纹...请将指纹按压指纹指纹仪1次...");
            axZKFPEngX1.BeginCapture();
        }
       
        //指纹登记
        private void button1_Click(object sender, EventArgs e)
        {
            string NAME = dataGridView1.CurrentRow.Cells[1].Value.ToString();
            DialogResult dr = MessageBox.Show("确认登记人是" + NAME + "吗？", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.Cancel) return;

            MessageBox.Show("开始采集指纹...请将指纹按压指纹指纹仪3次...");
            axZKFPEngX1.BeginEnroll();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}

