using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTT
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                txtHost.Text = Properties.Settings.Default["host"].ToString();
                txtUser.Text = Properties.Settings.Default["user"].ToString();
                txtPasswd.Text = Properties.Settings.Default["passwd"].ToString();
            }
            catch (Exception)
            {

            }

            clearLog();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(1);
        }

        #region Button Events

        private void subButton_Click(object sender, EventArgs e)
        {
            if (topicsList.Items.Contains(txtTopic.Text))
            {
                return;
            }
            if (txtTopic.Text.Length > 0 && isConnected)
            {
                subscribe(txtTopic.Text);
                topicsList.Items.Add(txtTopic.Text);
                // txtTopic.Clear();
            }
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            clearLog();
        }

        private void publishButton_Click(object sender, EventArgs e)
        {
            if (topicsList.SelectedIndex >= 0 && isConnected)
            {
                publish(topicsList.SelectedItem.ToString(), txtMessage.Text, checkReatain.Checked);
            }
        }

        private void unsubButton_Click(object sender, EventArgs e)
        {
            if (topicsList.SelectedIndex >= 0 && isConnected)
            {
                unsubscribe(topicsList.SelectedItem.ToString());
                topicsList.Items.Remove(topicsList.SelectedItem);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {

            if (isConnected)
            {
                disconnect();
                btnConnect.Text = "Connect";
            }
            else
            {
                isConnected = connect(txtHost.Text, txtUser.Text, txtPasswd.Text);
                if (isConnected)
                {
                    host = txtHost.Text;
                    btnConnect.Text = "Disconnect";
                    labelStatus.Text = "Connected (" + host + ")";

                    Properties.Settings.Default.host = host;
                    Properties.Settings.Default.user = txtUser.Text;
                    Properties.Settings.Default.passwd = txtPasswd.Text;
                    Properties.Settings.Default.Save();
                }

                logConnected(txtHost.Text, isConnected);
            }
        }

        #endregion

        #region MQTT Events

        MqttClient client;
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            // logText("Published: " + e.MessageId + ", success = " + e.IsPublished);
            logPublished(e);
        }

        void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            // logText("Subscribed for id = " + e.MessageId);
            logSubscribed(e);
        }

        void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            // logText("Unsubscribed for id = " + e.MessageId);
            logUnsubscribed(e);
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // logText("Received ("+ e.Topic+"): " + Encoding.UTF8.GetString(e.Message));
            logReceived(e);
        }

        #endregion

        #region Connection Handling

        Boolean isConnected = false;
        string host = "///";
        
        Boolean connect(string host, string user, string passwd)
        {
      
            try
            {
                client = new MqttClient(host);
                byte code = client.Connect(Guid.NewGuid().ToString(), user, passwd);
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;
                client.MqttMsgSubscribed += client_MqttMsgSubscribed;
                client.MqttMsgPublished += client_MqttMsgPublished;
                return client.IsConnected;
            }
            catch (Exception)
            {
                return false;
            }

            
        }

        void disconnect()
        {
            try
            {
                client.Disconnect();
                logDisconnected(host);
                isConnected = false;
                // Close();
            }
            catch (Exception)
            {

                
            }
            
        }

        #endregion

        #region MQTT Actions

        void subscribe(string topic)
        {
            if (!isConnected) { return; }
            ushort msgId = client.Subscribe(new string[] {"/", topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        }

        void unsubscribe(string topic)
        {
            if (!isConnected) { return; }
            ushort msgId = client.Unsubscribe(new string[] { topic });
        }

        void publish(string topic, string message, Boolean retain)
        {
            ushort msgId = client.Publish(topic, // topic
                              Encoding.UTF8.GetBytes(message), // message body
                              MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                              retain); // retained
        }

        #endregion

        #region Logging

        Font boldFont = new Font(
             new FontFamily("Consolas"),
             9,
             FontStyle.Bold
        );

        Font normalFont = new Font(
             new FontFamily("Consolas"),
             9,
             FontStyle.Regular
        );

        private void logReceived(MqttMsgPublishEventArgs e)
        {
            printTimestamp();
            txtConsole.AppendLog("Received", Color.Yellow, boldFont);
            printLog(" (");
            txtConsole.AppendLog( e.Topic, Color.LightBlue, normalFont);
            printLog("): ");
            txtConsole.AppendLog(Encoding.UTF8.GetString(e.Message) + Environment.NewLine, Color.LightGray, normalFont);
        }


        private void logConnected(string url, bool success)
        {
            printTimestamp();

            if (success)
            {
                printLog("Successfully connected to ");
                txtConsole.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);
            } else
            {
                printLog("Failed connecting to ");
                txtConsole.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);
            }

        }

        private void logDisconnected(string url)
        {
            printTimestamp();

            printLog("Successfully disconnected from ");
            txtConsole.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);


        }

        private void logPublished(MqttMsgPublishedEventArgs e)
        {
            printTimestamp();
            txtConsole.AppendLog("Published", Color.Orange, boldFont);
            printLog(" (" + e.MessageId + "): success = " + e.IsPublished + Environment.NewLine);
        }

        private void logSubscribed(MqttMsgSubscribedEventArgs e)
        {
            printTimestamp();
            txtConsole.AppendLog("Subscribed", Color.LightSkyBlue, boldFont);
            printLog(" (" + e.MessageId + ") " + Environment.NewLine);
        }

        private void logUnsubscribed(MqttMsgUnsubscribedEventArgs e)
        {
            printTimestamp();
            txtConsole.AppendLog("Unsubscribed", Color.Coral, boldFont);
            printLog(" (" + e.MessageId + ") " + Environment.NewLine);
        }

        private void printTimestamp()
        {
            DateTime time = DateTime.Now;
            txtConsole.AppendLog("[" + time.ToString("HH:mm:ss") + "] ", Color.YellowGreen, normalFont);
        }

        private void printLog(string text)
        {
            txtConsole.AppendLog(text, Color.GreenYellow, normalFont);
        }

        private void clearLog()
        {
            txtConsole.Clear();
            txtConsole.Text = ("=====================\n" +
                    "MQTT-CLIENT v." + this.ProductVersion + "\n" +
                    "=====================\n" +
                    "Created by Tim Strasser.\n\n");
        }

        #endregion

        /*

        delegate void StringArgReturningVoidDelegate(string text);

        private void logText(string text)
        {

            DateTime time = DateTime.Now;

            // InvokeRequired required compares the thread ID of the  
            // calling thread to the thread ID of the creating thread.  
            // If these threads are different, it returns true.  
            if (this.txtConsole.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(logText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtConsole.AppendText("[" + time.ToString("HH:mm:ss") + "] " + text + System.Environment.NewLine);

                // this.txtConsole.Text += ;
                txtConsole.SelectionStart = txtConsole.Text.Length;
                txtConsole.ScrollToCaret();
            }
        }

        private void CheckKeyword(string word, Color color, int startIndex, FontStyle style)
        {
            if (this.txtConsole.Text.Contains(word))
            {
                int index = -1;
                int selectStart = this.txtConsole.SelectionStart;

                while ((index = this.txtConsole.Text.IndexOf(word, (index + 1))) != -1)
                {
                    if (index > txtConsole.Text.Length - 200)
                    {
                        this.txtConsole.Select((index + startIndex), word.Length);
                        this.txtConsole.SelectionColor = color;
                        this.txtConsole.SelectionFont = new Font(
                         txtConsole.Font.FontFamily,
                         txtConsole.Font.Size,
                         style);
                        this.txtConsole.Select(selectStart, 0);


                        this.txtConsole.SelectionColor = Color.YellowGreen;
                    }

                }
            }
        }

        */



        /*
        private void txtConsole_TextChanged(object sender, EventArgs e)
        {
            return;


            int lenght = txtConsole.Text.Length - 200;
            if (lenght < 0)
            {
                lenght = 0;
            }

            this.CheckKeyword("Received", Color.Yellow, 0, FontStyle.Bold);
            this.CheckKeyword("Published", Color.Orange, 0, FontStyle.Bold);
            this.CheckKeyword(host, Color.LightGray, 0, FontStyle.Bold);
            foreach (var listBoxItem in topicsList.Items)
            {
                this.CheckKeyword(listBoxItem.ToString(), Color.LightBlue, 0, FontStyle.Regular);
            }
        }
        */

    }
}
