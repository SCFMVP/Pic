using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
/**
 * Author: Later
 * Time: 2020/2/13
 * Info: receive picture from stm32
 * Modified:
 **/
namespace Pic
{
    public partial class Form1 : Form
    {
        //全局变量
        SerialPort sp = null;   //声明一个串口类
        bool isOpen = false;    //打开串口标志位
        bool isSetProperty = false; //属性设置标志位
        bool isHex = true;     //十六进制显示标志位
        int receive_count = 0;
        int y = 0;
        int temp = 0;
        private StringBuilder sb = new StringBuilder();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量

        byte colorL, colorH;
        Color newColor = new Color();
        Bitmap OvImage = new Bitmap(240, 320,PixelFormat.Format16bppRgb555);   //bmp文件头（有）、位图信息头（240*320）、颜色信息（待传输）、图形数据（待传输从下向上扫描）
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            colorL = colorH = 0;
            //不允许拖动
            //this.MaximumSize = this.Size;
            //this.MinimumSize = this.Size;
            //this.MaximizeBox = false;
            for (int i = 0; i < 10; i++)//最大支持到串口10，可根据自己需求增加
            {
                cbxCOMPort.Items.Add("COM" + (i + 1).ToString());
            }
            cbxCOMPort.SelectedIndex = 2;
            //列出常用的波特率
            cbxBaudRate.Items.Add("1200");
            cbxBaudRate.Items.Add("2400");
            cbxBaudRate.Items.Add("4800");
            cbxBaudRate.Items.Add("9600");
            cbxBaudRate.Items.Add("19200");
            cbxBaudRate.Items.Add("38400");
            cbxBaudRate.Items.Add("43000");
            cbxBaudRate.Items.Add("56000");
            cbxBaudRate.Items.Add("57600");
            cbxBaudRate.Items.Add("115200");
            cbxBaudRate.SelectedIndex = 3;
            //列出停止位
            cbxStopBits.Items.Add("0");
            cbxStopBits.Items.Add("1");
            cbxStopBits.Items.Add("1.5");
            cbxStopBits.Items.Add("2");
            cbxStopBits.SelectedIndex = 1;
            //列出数据位
            cbxDataBits.Items.Add("8");
            cbxDataBits.Items.Add("7");
            cbxDataBits.Items.Add("6");
            cbxDataBits.Items.Add("5");
            cbxDataBits.SelectedIndex = 0;
            //列出奇偶校验位
            cbxParity.Items.Add("无");
            cbxParity.Items.Add("奇校验");
            cbxParity.Items.Add("偶校验");
            cbxParity.SelectedIndex = 0;
            //默认为Hex显示
            rbnHex.Checked = true;
            //初始接收字符数目为0
            tbxRecvLength.Text = "0";
            

        }

        /// <summary>
        /// 接收串口数据,显示pic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            this.Invoke(new EventHandler(delegate
            {
                
                //---------------------------//
                int num = sp.BytesToRead;      //获取接收缓冲区中的字节数
                byte[] received_buf = new byte[num];    //声明一个大小为num的字节数据用于存放读出的byte型数据
                receive_count += num;             //接收字节计数变量增加nun
                sp.Read(received_buf, 0, num);    //读取接收缓冲区中num个字节到byte数组中
                sb.Clear();                       //防止出错,首先清空字符串构造器
                if (isHex == true)
                {
                    //u16 = 2bytes
                    //这里是按byte读取,换成Int16也不行,需要一次读两个byte出来
                    for (int i = 0; i < received_buf.Length; i++)
                    {
                        sb.Append(received_buf[i].ToString("X2") + ' ');    //将byte型数据转化为2位16进制文本显示，并用空格隔开
                        if ((i+1) * 2 <= received_buf.Length)
                        {
                            //读取一个像素
                            colorL = received_buf[i * 2];
                            colorH = received_buf[i * 2 + 1];
                            //解析RGB565
                            Int32 r, g, b;                        //0-255 , color 511
                            r = (colorH & 0xf8) >> 3;
                            g = ((colorH & 0x07) << 2) | ((colorL & 0xe0) >> 6);
                            b = colorL & 0x1f;
                            //Console.WriteLine("Red: "+r.ToString()+ " Green: " + g.ToString()+ " Blue: " + b.ToString());
                            //合成并显示像素
                            newColor = Color.FromArgb(r, g, b);
                            Int32 Row = (receive_count) / 320 / 2;    //计算列: 共240列,每列320个像素点
                            OvImage.SetPixel(Row, y++, newColor);
                            //换列显示
                            if (y == 320) { y = 0; }
                        }                        
                    }           
                }
                else
                {
                    //选中ASCII模式显示
                    sb.Append(Encoding.ASCII.GetString(received_buf));  //将整个数组解码为ASCII数组
                }            
                //更新UI显示
                ptbOv7725.Image = OvImage;        //放在外面按每次(一列)接收的来显示了          
                tbxRecvData.AppendText(sb.ToString());
                tbxRecvLength.Text =  receive_count.ToString() + "Bytes";
                //--------------------------------------//
            }));

        }

        #region[setting]
        /// <summary>
        /// 检测串口设置
        /// </summary>
        /// <returns></returns>
        private bool CheckPortSetting() //检查串口是否设置
        {
            if (cbxCOMPort.Text.Trim() == "") return false;
            if (cbxBaudRate.Text.Trim() == "") return false;
            if (cbxDataBits.Text.Trim() == "") return false;
            if (cbxParity.Text.Trim() == "") return false;
            if (cbxStopBits.Text.Trim() == "") return false;
            return true;
        }
        private bool CheckSendData()
        {
            if (tbxSendData.Text.Trim() == "") return false;
            return true;
        }
        private void SetPortProperty()  //设置串口的属性
        {
            sp = new SerialPort();
            sp.PortName = cbxCOMPort.Text.Trim();//设置串口名
            sp.BaudRate = Convert.ToInt32(cbxBaudRate.Text.Trim());//设置串口波特率
            float f = Convert.ToSingle(cbxStopBits.Text.Trim());   //设置停止位
            if (0 == f)
            {
                sp.StopBits = StopBits.None;
            }
            else if (1.5 == f)
            {
                sp.StopBits = StopBits.OnePointFive;
            }
            else if (1 == f)
            {
                sp.StopBits = StopBits.One;
            }
            else if (2 == f)
            {
                sp.StopBits = StopBits.Two;
            }
            else
            {
                sp.StopBits = StopBits.One;
            }       
            sp.DataBits = Convert.ToInt16(cbxDataBits.Text.Trim());//设置数据位
            string s = cbxParity.Text.Trim();//设置奇偶校验位
            if (0 == s.CompareTo("无"))
            {
                sp.Parity = Parity.None;
            }
            else if (0 == s.CompareTo("奇校验"))
            {
                sp.Parity = Parity.Odd;
            }
            else if (0 == s.CompareTo("偶校验"))
            {
                sp.Parity = Parity.Even;
            }
            else
            {
                sp.Parity = Parity.None;
            }
            //sp.ReadTimeout = -1;//设置超时读取时间-----会带来好多问题的哦
            //sp.RtsEnable = true;


            //定义DataReceived事件，当串口收到数据后触发事件
            sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            if (rbnHex.Checked)
            {
                isHex = true;
            }
            else
            {
                isHex = false;
            }
        }

        #endregion

        private void tbxRecvData_TextChanged(object sender, EventArgs e)
        {
            tbxRecvData.SelectionStart = tbxRecvData.Text.Length;
            tbxRecvData.ScrollToCaret();
        }

        /// <summary>
        /// 串口发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (isOpen)
            {
                try
                {
                    sp.WriteLine(tbxSendData.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("发送数据时发生错误！", "错误提示");
                    return;
                }
            }
            else
            {
                MessageBox.Show("串口未打开！", "错误提示");
                return;
            }
            if (CheckSendData())//检测要发送的数据
            {
                // MessageBox.Show("请输入要发送的数据！", "错误提示");
                return;
            }
        }

        /// <summary>
        /// 检测可用串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCheckCOM_Click(object sender, EventArgs e)
        {
            bool comExistence = false;  //有可用串口标志位
            cbxCOMPort.Items.Clear();
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    SerialPort sp = new SerialPort("COM" + (i + 1).ToString());
                    sp.Open();
                    sp.Close();
                    cbxCOMPort.Items.Add("COM" + (i + 1).ToString());
                    comExistence = true;
                }
                catch (Exception)
                {


                    continue;
                }
            }
            if (comExistence)
            {
                cbxCOMPort.SelectedIndex = 0;//使ListBox显示第一个添加的索引
            }
            else
            {
                MessageBox.Show("没有找到可用串口！", "错误提示");
            }
        }

        private void btnOpenCOM_Click(object sender, EventArgs e)
        {
            if (false == isOpen)
            {
                if (!CheckPortSetting()) //检查串口设置
                {
                    MessageBox.Show("串口未设置！", "错误提示");
                    return;
                }
                if (!isSetProperty)  //串口未设置则设置串口
                {
                    SetPortProperty();
                    isSetProperty = true;
                }
                try //打开串口
                {
                    sp.Open();
                    isOpen = true;
                    btnOpenCOM.Text = "关闭串口";
                    //串口打开后，相关的串口设置按钮便不可再用  

                    cbxCOMPort.Enabled = false;
                    cbxBaudRate.Enabled = false;
                    cbxDataBits.Enabled = false;
                    cbxParity.Enabled = false;
                    cbxStopBits.Enabled = false;
                    rbnChar.Enabled = false;
                    rbnHex.Enabled = false;

                }
                catch (Exception)
                {
                    //打开串口失败后，相应标志位取消
                    isSetProperty = false;
                    isOpen = false;
                    MessageBox.Show("串口无效或已被占用!", "错误提示");
                }
            }
            else
            {
                try //关闭串口
                {
                    sp.Close();
                    isOpen = false;
                    isSetProperty = false;
                    btnOpenCOM.Text = "打开串口";
                    //关闭串口后，串口设置选项便可以继续使用
                    cbxCOMPort.Enabled = true;
                    cbxBaudRate.Enabled = true;
                    cbxDataBits.Enabled = true;
                    cbxParity.Enabled = true;
                    cbxStopBits.Enabled = true;
                    rbnChar.Enabled = true;
                    rbnHex.Enabled = true;
                }
                catch (Exception)
                {
                    MessageBox.Show("关闭串口时发生错误！", "错误提示");
                }
            }
        }
       
        private void btnCleanData_Click(object sender, EventArgs e)
        {
            tbxRecvData.Text = "";
            tbxRecvLength.Text = "0";//更新接收框数据长度
            ptbOv7725.Image = null;
            //OvImage=null;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ptbOv7725.Image.Save("Ov7725.bmp");
            MessageBox.Show("保存图片成功！", "信息");
        }
    }
}
