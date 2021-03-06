﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Linq;
using Modbus.Device;                                                                        // for modbus master
using System.IO.Ports;                                                                      // for serial port
using System.Threading;
using System.Text;


namespace C_Sharp_PC_RGB
{
    public partial class Form1 : Form
    {

		public List<string> existing_port_list = new List<string>() { "Не использовать" };  // лист доступных в системе СОМ портов в данный момент

		public string nomber_net_buf;                                                       // номер устройства в сети
		public string speed_net_buf;                                                        // скорость передачи по выбранному каналу модбас
		public string stop_bit_net_buf;                                                     // кол-во стоп бит
		public string quantity_bit_buf;                                                     // кол-во бит в посылке
		public string parity_net_buf;                                                   // выбранная проверка в посылке
		public string post_buf;                                                             // СОМ порт для канала связи

		public bool autoconnect_buf;                                                    // буль если тру, то прога самоподключается при старте

		public static ushort[] modbus_mass = new ushort[200];                               // массив для взаимодействия с потоком обработки модбас

		public Property_Form property_Form;                                                 // Объявляем класс с формой настроек				

		public Class_visu_prop_grid_modbus visu_MyClass_modbus = new Class_visu_prop_grid_modbus(); // создаём экземпляр класса для отображения в сетке свойств настроек modbus

		public Serializable_Class serializable_Class;

		public SerialPort serialPort = new SerialPort();                                           // create a new SerialPort object with default settings.			

		public ModBus_var modBus_var = new ModBus_var();                                    // создаём экземпляр класса для передачи в поток
		public Thread Thread_Modbus = new Thread(new ParameterizedThreadStart(Modbus_func));       // Вот так передаём в созданный поток класс

		public static ModbusSerialMaster master;



		public Form1()
        {
            InitializeComponent();
            colorDialog1.FullOpen = true;

			serializable_Class = new Serializable_Class(            // инициируем класс для работы с сериализером
					nomber_net_buf,                     // номер устройства в сети
					speed_net_buf,                      // скорость передачи по выбранному каналу модбас
					stop_bit_net_buf,                   // кол-во стоп бит
					quantity_bit_buf,                   // кол-во бит в посылке
					parity_net_buf,                     // выбранная проверка в посылке
					post_buf,
					autoconnect_buf);

			Deserialization("default.dat");                         // десериализуем ранее сохранённые настройки




			property_Form = new Property_Form(this);
			property_Form.Tag = this;


			existing_port_list.AddRange(SerialPort.GetPortNames());                     // узнаём какие порты активны сейчс и заносим их в лист

			label4.Text = "0";
			label5.Text = "0";
			label6.Text = "0";

			label7.Text = "0";
			label8.Text = "0";
			label9.Text = "0";

		}




        private void button1_Click(object sender, EventArgs e)      // кнопка вызова диаграммы выбора цвета
        {
			//colorDialog1.ShowDialog(); 

			if (colorDialog1.ShowDialog() == DialogResult.Cancel)
				return;

			label4.Text = colorDialog1.Color.R.ToString();
			label5.Text = colorDialog1.Color.G.ToString();
			label6.Text = colorDialog1.Color.B.ToString();

			ushort R = Convert.ToUInt16((((colorDialog1.Color.R * 100) / 255) * 4500) /100);
			ushort G = Convert.ToUInt16((((colorDialog1.Color.G * 100) / 255) * 4500) / 100);
			ushort B = Convert.ToUInt16((((colorDialog1.Color.B * 100) / 255) * 4500) / 100);

			modBus_var.mb_mass[3] = R;
			trackBar1.Value = Convert.ToInt16(R);
			numericUpDown_R.Value = Convert.ToInt16(R);

			modBus_var.mb_mass[2] = G;
			trackBar2.Value = Convert.ToInt16(G);
			numericUpDown_G.Value = Convert.ToInt16(G);

			modBus_var.mb_mass[1] = B;
			trackBar3.Value = Convert.ToInt16(B);
			numericUpDown_B.Value = Convert.ToInt16(B);


		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Serialization("default.dat");
            Thread_Modbus.Abort();                                                      // заставляет прервать поток обработки модбас
        }

        private void ToolStripMenuItem_transport_btn_Click(object sender, EventArgs e)
        {
            property_Form.tabControl1.SelectTab(0);                                         // показываем окно с первой вкладки
            property_Form.Show();                                                           // показываем окно
        }

        private void ToolStripMenuItem_exit_Click(object sender, EventArgs e)
        {
            Serialization("default.dat");
            Thread_Modbus.Abort();                                                          // заставляет прервать поток обработки модбас	
            Close();
        }

        private void timer_for_Displ_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel7.Text = Convert.ToString(modBus_var.mb_mass[8]);       // тестовый счётчик в потоке модбаса в плате. в строке состояния.

			label7.Text = Convert.ToString(modBus_var.mb_mass[9] / 10.0f); 
			label8.Text = Convert.ToString(modBus_var.mb_mass[10] / 10.0f); 
			label9.Text = Convert.ToString(modBus_var.mb_mass[11] / 10.0f); 

		}




        private void numericUpDown_R_ValueChanged(object sender, EventArgs e)			// R
        {
			modBus_var.mb_mass[3] = Convert.ToUInt16(numericUpDown_R.Value);
			trackBar1.Value = Convert.ToInt16(numericUpDown_R.Value);
		}

        private void numericUpDown_G_ValueChanged(object sender, EventArgs e)			// G
        {
			modBus_var.mb_mass[2] = Convert.ToUInt16(numericUpDown_G.Value);
			trackBar2.Value = Convert.ToInt16(numericUpDown_G.Value);
		}

        private void numericUpDown_B_ValueChanged(object sender, EventArgs e)			// B
        {
			modBus_var.mb_mass[1] = Convert.ToUInt16(numericUpDown_B.Value);
			trackBar3.Value = Convert.ToInt16(numericUpDown_B.Value);
		}



        private void trackBar1_Scroll(object sender, EventArgs e)						// R
        {
			numericUpDown_R.Value = trackBar1.Value;
		}

        private void trackBar2_Scroll(object sender, EventArgs e)						// G
        {
			numericUpDown_G.Value = trackBar2.Value;
		}

        private void trackBar3_Scroll(object sender, EventArgs e)						// B
        {
			numericUpDown_B.Value = trackBar3.Value;
		}
    }



    public class ModBus_var
	{
		public byte adrr_dev_in;
		public ushort adrr_var_in;
		public ushort adrr_var_out;
		public ushort lenght_in;

		public ushort buton;

		public ushort flag_connect;
		public ushort flag_read_param;
		public ushort flag_write_param;

		public ushort[] mb_mass;


		public ModBus_var()
		{
			mb_mass = new ushort[200];

		}

	}
}
