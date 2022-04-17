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

        private void subscribeButton_Click(object sender, EventArgs e)
        {
            if (topicsList.Items.Contains(txtTopic.Text))
            {
                return;
            }
            if (txtTopic.Text.Length > 0 && mqttIsConnected)
            {
                subscribe(txtTopic.Text);
                topicsList.Items.Add(txtTopic.Text);
                topicsList.SetSelected(topicsList.Items.Count - 1, true);
                
                // txtTopic.Clear();
            }
        }

        private void clearLogButton_Click(object sender, EventArgs e)
        {
            clearLog();
        }

        private void publishButton_Click(object sender, EventArgs e)
        {
            if (topicsList.SelectedIndex >= 0 && mqttIsConnected)
            {
                publish(topicsList.SelectedItem.ToString(), txtMessage.Text, checkReatain.Checked);
            }
        }

        private void unsubscribeButton_Click(object sender, EventArgs e)
        {
            if (topicsList.SelectedIndex >= 0 && mqttIsConnected)
            {
                unsubscribe(topicsList.SelectedItem.ToString());
                topicsList.Items.Remove(topicsList.SelectedItem);
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {

            if (mqttIsConnected)
            {
                disconnect();
                connectButton.Text = "Connect";
            }
            else
            {
                mqttIsConnected = connect(txtHost.Text, txtUser.Text, txtPasswd.Text);
                if (mqttIsConnected)
                {
                    mqttHost = txtHost.Text;
                    connectButton.Text = "Disconnect";
                    statusLabel.Text = "Connected (" + mqttHost + ")";

                    Properties.Settings.Default.host = mqttHost;
                    Properties.Settings.Default.user = txtUser.Text;
                    Properties.Settings.Default.passwd = txtPasswd.Text;
                    Properties.Settings.Default.Save();
                }

                logConnected(txtHost.Text, mqttIsConnected);
            }
        }


        private void topicsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(topicsList.SelectedItems.Count > 0)
            {
                selectedSubscription = topicsList.SelectedItems[0].ToString();
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

        #region MQTT Actions

        void subscribe(string topic)
        {
            if (!mqttIsConnected) { return; }
            ushort msgId = client.Subscribe(new string[] {"/", topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        }

        void unsubscribe(string topic)
        {
            if (!mqttIsConnected) { return; }
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

        #region Connection Handling

        bool mqttIsConnected = false;
        string mqttHost = "";

        string selectedSubscription = "";
        
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
                logDisconnected(mqttHost);
                mqttIsConnected = false;
                topicsList.Items.Clear();

                connectButton.Text = "Connect";
                statusLabel.Text = "Disconnected";
            }
            catch (Exception)
            {

                
            }
            
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
            logsTextBox.AppendLog("Received", Color.Yellow, boldFont);
            printLog(" (");
            logsTextBox.AppendLog( e.Topic, Color.LightBlue, normalFont);
            printLog("): ");
            logsTextBox.AppendLog(Encoding.UTF8.GetString(e.Message) + Environment.NewLine, Color.LightGray, normalFont);
        }


        private void logConnected(string url, bool success)
        {
            printTimestamp();

            if (success)
            {
                printLog("Successfully connected to ");
                logsTextBox.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);
            } else
            {
                printLog("Failed connecting to ");
                logsTextBox.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);
            }

        }

        private void logDisconnected(string url)
        {
            printTimestamp();

            printLog("Successfully disconnected from ");
            logsTextBox.AppendLog(url + Environment.NewLine, Color.LightGray, boldFont);


        }

        private void logPublished(MqttMsgPublishedEventArgs e)
        {
            printTimestamp();
            logsTextBox.AppendLog("Published to ", Color.Orange, boldFont);
            logsTextBox.AppendLog(selectedSubscription, Color.LightGray, boldFont);
            printLog(" (" + e.MessageId + "): success = " + e.IsPublished + Environment.NewLine);
        }

        private void logSubscribed(MqttMsgSubscribedEventArgs e)
        {
            printTimestamp();
            logsTextBox.AppendLog("Subscribed ", Color.LightSkyBlue, boldFont);
            logsTextBox.AppendLog(selectedSubscription, Color.LightGray, boldFont);
            printLog(" (" + e.MessageId + ") " + Environment.NewLine);
        }

        private void logUnsubscribed(MqttMsgUnsubscribedEventArgs e)
        {
            printTimestamp();
            logsTextBox.AppendLog("Unsubscribed ", Color.Coral, boldFont);
            logsTextBox.AppendLog(selectedSubscription, Color.LightGray, boldFont);
            printLog(" (" + e.MessageId + ") " + Environment.NewLine);
        }

        private void printTimestamp()
        {
            DateTime time = DateTime.Now;
            logsTextBox.AppendLog("[" + time.ToString("HH:mm:ss") + "] ", Color.LightGreen, normalFont);
        }

        private void printLog(string text)
        {
            logsTextBox.AppendLog(text, Color.GreenYellow, normalFont);
        }

        private void clearLog()
        {
            logsTextBox.Clear();
            logsTextBox.Text = ("=====================\n" +
                    "MQTT-CLIENT v." + this.ProductVersion + "\n" +
                    "=====================\n" +
                    "Created by Tim Strasser.\n\n");
        }

        #endregion

    }
}
