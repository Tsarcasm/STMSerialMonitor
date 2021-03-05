using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.IO.Ports;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace STMSerialMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = flowLayoutPanel1.Controls.Count - 1; i >= 0; i--)
            {
                (SerialPort port, bool paused) = ((SerialPort, bool))flowLayoutPanel1.Controls[i].Tag;
                port.Close();
                flowLayoutPanel1.Controls.RemoveAt(i);
            }
        }

        string previousPorts = "";
        SerialPort port;
        private void timer1_Tick(object sender, EventArgs e)
        {
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();

            // If we have a change in ports then represent that
            if (previousPorts != ports.Aggregate((i, j) => i + j))
            {
                previousPorts = ports.Aggregate((i, j) => i + j);
                foreach (string portString in ports)
                {
                    if (portString == "COM1") continue;
                    bool found = false;
                    // See if we already have a panel for this port
                    foreach (Panel item in flowLayoutPanel1.Controls)
                    {
                        (SerialPort port, bool paused) = ((SerialPort, bool))item.Tag;
                        if (port.PortName == portString) found = true;
                    }

                    //If we don't have a panel then add one 
                    if (!found)
                    {
                        var newPort = new SerialPort(portString, 115200);
                        newPort.Open();
                        addPortBox(newPort);
                    }
                }

            }

            for (int i = flowLayoutPanel1.Controls.Count - 1; i >= 0; i--)
            {
                Panel p = (Panel)flowLayoutPanel1.Controls[i];
                TextBox textBox = (TextBox)p.Controls[0];
                (SerialPort port, bool paused) = ((SerialPort, bool))p.Tag;
                lock (port)
                {
                    if (!port.IsOpen && !paused)
                    {
                        try
                        {
                            textBox.Enabled = false;
                            if (ports.Contains(port.PortName))
                            {
                                port.Open();
                                if (port.IsOpen)
                                {
                                    textBox.Enabled = true;
                                }
                                else
                                {
                                    MessageBox.Show("Error");
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else
                    {
                        if (port.IsOpen)
                        {
                            if (((CheckBox)p.Controls[4]).Checked)
                            {
                                string portStr = port.ReadExisting();
                                if (portStr.Contains("DATA_UPLOAD"))
                                {
                                    upload(portStr);
                                }

                                ((TextBox)p.Controls[0]).AppendText(portStr);
                            }
                        }
                    }
                }
            }




        }

        static WebClient webClient = new WebClient();
        private void upload(string str)
        {
            // First get a token
            string result = webClient.DownloadString("http://157.245.47.19/moonitoring/api/start_upload.php?b=1&f=2");
            int token = int.Parse(JsonConvert.DeserializeObject<Dictionary<string, string>>(result)["token"]);
            var data = str.Split('|')[1];
            httpbox.AppendText($"Obtained token {token}\n");
            var uploadStr = $"t={token}&{data}";
            httpbox.AppendText($"Data request: {uploadStr}\n");
            result = webClient.DownloadString("http://157.245.47.19/moonitoring/api/upload_data.php?" + uploadStr);
            httpbox.AppendText(result);
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string newPort = (string)listBox1.SelectedItem;
            if (newPort == null)
            {
                return;
            }
            port = new SerialPort(newPort, 115200);
            port.Open();
            //textBox1.Enabled = true;
        }


        void addPortBox(SerialPort port)
        {
            Panel panel = new Panel()
            {
                Width = 400,
                Height = flowLayoutPanel1.Height,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = (port, false),
            };

            Label label = new Label()
            {
                Text = port.PortName,
                Location = new Point(3, 3),
            };


            TextBox textBox = new TextBox()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(3, 35),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(400, flowLayoutPanel1.Height - 80),
                TabIndex = 0,
                Font = new Font("Consolas", 10)
            };

            Button clearBtn = new Button()
            {
                Text = "Clear",
                Top = flowLayoutPanel1.Height - 30,
                Size = new Size(100, 25)
            };
            clearBtn.Click += (_, __) =>
            {
                textBox.Clear();
            };
            Button pauseBtn = new Button()
            {
                Text = "Pause",
                Top = flowLayoutPanel1.Height - 30,
                Left = 110,
                Size = new Size(100, 25)
            };

            pauseBtn.Click += (_, __) =>
            {
                (SerialPort _p, bool paused) = ((SerialPort, bool))panel.Tag;
                lock (_p)
                {
                    if (paused)
                    {
                        pauseBtn.Text = "Pause";
                        panel.Tag = (_p, false);
                    }
                    else
                    {
                        pauseBtn.Text = "Unpause";
                        _p.Close();
                        panel.Tag = (_p, true);
                        textBox.Enabled = false;
                    }
                }
            };

            CheckBox checkBox = new CheckBox()
            {
                Text = "Auto Print",
                Checked = true,
                Top = flowLayoutPanel1.Height - 30,
                Left = 220
            };

            panel.Controls.Add(textBox);
            panel.Controls.Add(label);
            panel.Controls.Add(clearBtn);
            panel.Controls.Add(pauseBtn);
            panel.Controls.Add(checkBox);
            flowLayoutPanel1.Controls.Add(panel);

        }
    }
}
