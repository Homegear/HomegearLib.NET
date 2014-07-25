using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomegearLib;
using HomegearLib.RPC;

namespace HomegearLibTest
{
    public partial class frmMain : Form
    {
        delegate void NoParameterCallback();
        delegate void DeviceParameterCallback(Device device);
        delegate void BooleanParameterCallback(Boolean value);
        delegate void SetTextCallback(string text);
        RPCController _rpc = null;
        Homegear _homegear = null;
        Device _rightClickedDevice = null;
        Channel _rightClickedChannel = null;
        MetadataVariable _rightClickedMetadata = null;
        Link _rightClickedLink = null;
        Device _selectedDevice = null;
        Link _selectedLink = null;
        Variable _selectedVariable = null;
        SystemVariable _selectedSystemVariable = null;
        MetadataVariable _selectedMetadata = null;
        bool _nodeLoading = false;
        Int32 _variableTimerIndex = 5;
        Timer _variableValueChangedTimer = new Timer();

        public frmMain()
        {
            InitializeComponent();

            _variableValueChangedTimer.Interval = 1000;
            _variableValueChangedTimer.Tick += _variableValueChangedTimer_Tick;
            lblVariableTimer.Text = "";
            lblSystemVariableTimer.Text = "";
            lblMetadataTimer.Text = "";
            txtHomegearHostname.Text = "192.168.0.105";
            txtHomegearPort.Text = "2001";
            txtCallbackHostname.Text = "192.168.0.113";
            chkSSL.Checked = false;
        }

        void _variableValueChangedTimer_Tick(object sender, EventArgs e)
        {
            _variableValueChangedTimer.Stop();
            if(_variableTimerIndex > 1)
            {
                _variableTimerIndex--;
                if(_selectedVariable != null) lblVariableTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                else if(_selectedSystemVariable != null) lblSystemVariableTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                else lblMetadataTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                _variableValueChangedTimer.Start();
                return;
            }
            lblVariableTimer.Text = "";
            lblSystemVariableTimer.Text = "";
            lblMetadataTimer.Text = "";
            if (_selectedVariable != null) SetVariable();
            else if (_selectedSystemVariable != null) SetSystemVariable();
            else SetMetadata();
        }

        void _homegear_OnReloaded(Homegear sender)
        {
            WriteLog("Reload complete. Received " + sender.Devices.Count + " devices.");
            UpdateTreeView();
            EnableSplitContainer(true);
        }

        void EnableSplitContainer(Boolean value)
        {
            if (splitContainer1.InvokeRequired)
            {
                BooleanParameterCallback d = new BooleanParameterCallback(EnableSplitContainer);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                splitContainer1.Enabled = value;
            }
        }

        void UpdateTreeView()
        {
            if(tvDevices.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(UpdateTreeView);
                this.Invoke(d);
            }
            else
            {
                tvDevices.Nodes.Clear();
                TreeNode systemVariablesNode = new TreeNode("System Variables");
                systemVariablesNode.Nodes.Add("<loading...>");
                systemVariablesNode.ContextMenuStrip = cmSystemVariables;
                tvDevices.Nodes.Add(systemVariablesNode);

                TreeNode interfacesNode = new TreeNode("Interfaces");
                interfacesNode.Nodes.Add("<loading...>");
                tvDevices.Nodes.Add(interfacesNode);

                TreeNode devicesNode = new TreeNode("Devices");
                devicesNode.ContextMenuStrip = cmDevices;
                foreach(KeyValuePair<Int32, Device> device in _homegear.Devices)
                {
                    TreeNode deviceNode = new TreeNode("Device " + ((device.Key >= 0x40000000) ? "0x" + device.Key.ToString("X2") : device.Key.ToString()));
                    deviceNode.Tag = device.Value;
                    deviceNode.ContextMenuStrip = cmDevice;

                    TreeNode metadataNode = new TreeNode("Metadata");
                    metadataNode.Nodes.Add("<loading...>");
                    metadataNode.ContextMenuStrip = cmMetadataVariables;
                    deviceNode.Nodes.Add(metadataNode);

                    foreach(KeyValuePair<Int32, Channel> channel in device.Value.Channels)
                    {
                        TreeNode channelNode = new TreeNode("Channel " + channel.Key);
                        channelNode.Tag = channel.Value;
                        
                        TreeNode valuesNode = new TreeNode("Variables");
                        foreach(KeyValuePair<String, Variable> variable in channel.Value.Variables)
                        {
                            TreeNode variableNode = new TreeNode(variable.Key);
                            variableNode.Tag = variable.Value;
                            valuesNode.Nodes.Add(variableNode);
                        }
                        channelNode.Nodes.Add(valuesNode);

                        TreeNode configNode = new TreeNode("Config");
                        configNode.Tag = channel.Value;
                        configNode.Nodes.Add("<loading...>");
                        channelNode.Nodes.Add(configNode);

                        TreeNode linksNode = new TreeNode("Links");
                        linksNode.Tag = channel.Value;
                        linksNode.ContextMenuStrip = cmLinks;
                        linksNode.Nodes.Add("<loading...>");
                        channelNode.Nodes.Add(linksNode);

                        deviceNode.Nodes.Add(channelNode);
                    }
                    devicesNode.Nodes.Add(deviceNode);
                }
                tvDevices.Nodes.Add(devicesNode);
            }
        }

        void WriteLog(String text)
        {
            if (txtLog.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(WriteLog);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (txtLog.Text.Length > 50000) txtLog.Text = txtLog.Text.Substring(0, 40000);
                txtLog.Text = txtLog.Text.Insert(0, text + "\r\n");
            }
        }

        void _homegear_OnSystemVariableUpdated(Homegear sender, SystemVariable variable)
        {
            WriteLog("System variable updated: Value: " + variable.ToString());
            if (_selectedSystemVariable == variable)
            {
                SetSystemVariableValue(variable.ToString());
            }
        }

        void _homegear_OnMetadataUpdated(Homegear sender, Device device, MetadataVariable variable)
        {
            WriteLog("Metadata updated: Device: " + device.ID.ToString() + ", Value: " + variable.ToString());
            if (_selectedMetadata == variable)
            {
                SetMetadataValue(variable.ToString());
            }
        }

        void _homegear_OnDeviceVariableUpdated(Homegear sender, Device device, Channel channel, Variable variable)
        {
            WriteLog("Variable updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Variable Name: \"" + variable.Name + "\", Value: " + variable.ToString());
            if(_selectedVariable == variable)
            {
                SetVariableValue(variable.ToString());
            }
        }

        void _homegear_OnDeviceConfigParameterUpdated(Homegear sender, Device device, Channel channel, ConfigParameter parameter)
        {
            WriteLog("Config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
            if (_selectedVariable == parameter)
            {
                SetVariableValue(parameter.ToString());
            }
        }

        void _homegear_OnDeviceLinkConfigParameterUpdated(Homegear sender, Device device, Channel channel, Link link, ConfigParameter parameter)
        {
            WriteLog("Link config parameter updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString() + ", Remote Peer: " + link.RemotePeerID.ToString() + ", Remote Channel: " + link.RemoteChannel.ToString() + ", Parameter Name: \"" + parameter.Name + "\", Value: " + parameter.ToString());
            if (_selectedVariable == parameter)
            {
                SetVariableValue(parameter.ToString());
            }
        }

        void _homegear_OnConnectError(Homegear sender, string message, string stackTrace)
        {
            WriteLog("Error connecting to Homegear: " + message + "\r\nStacktrace: " + stackTrace);
        }

        private void frmHaupt_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(_homegear != null) _homegear.Dispose();
        }

        /*private void button1_Click(object sender, EventArgs e)
        {
            _homegear.Devices[151].Channels[1].Variables["SUBMIT"].StringValue = "1,200,108000,33,0,17,0,49,0";
            _homegear.Devices[151].Channels[2].Variables["SUBMIT"].StringValue = "1,1,108000,2,1";
        }*/

        private void chkSSL_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSSL.Checked) gbSSL.Enabled = true; else gbSSL.Enabled = false;
        }

        private void bnConnect_Click(object sender, EventArgs e)
        {
            bnConnect.Enabled = false;
            gbSSL.Enabled = false;
            txtHomegearHostname.ReadOnly = true;
            txtHomegearPort.ReadOnly = true;
            txtListenIP.ReadOnly = true;
            txtListenPort.ReadOnly = true;
            txtCallbackHostname.ReadOnly = true;
            chkSSL.Enabled = false;

            SSLClientInfo sslClientInfo = null;
            SSLServerInfo sslServerInfo = null;
            if (chkSSL.Checked)
            {
                sslClientInfo = new SSLClientInfo(txtCallbackHostname.Text, txtHomegearUsername.Text, txtHomegearPassword.Text, chkVerifyCertificate.Checked);
                sslServerInfo = new SSLServerInfo(txtCertificatePath.Text, txtCertificatePassword.Text, txtCallbackUsername.Text, txtCallbackPassword.Text);
            }
            Int32 homegearPort = 0;
            Int32 listenPort = 0;
            Int32.TryParse(txtHomegearPort.Text, out homegearPort);
            Int32.TryParse(txtListenPort.Text, out listenPort);
            _rpc = new RPCController(txtHomegearHostname.Text, homegearPort, txtCallbackHostname.Text, txtListenIP.Text, listenPort, sslClientInfo, sslServerInfo);
            _rpc.Connected += _rpc_Connected;
            _rpc.Disconnected += _rpc_Disconnected;
            _homegear = new Homegear(_rpc);
            _homegear.ConnectError += _homegear_OnConnectError;
            _homegear.SystemVariableUpdated += _homegear_OnSystemVariableUpdated;
            _homegear.MetadataUpdated += _homegear_OnMetadataUpdated;
            _homegear.DeviceVariableUpdated += _homegear_OnDeviceVariableUpdated;
            _homegear.DeviceConfigParameterUpdated += _homegear_OnDeviceConfigParameterUpdated;
            _homegear.DeviceLinkConfigParameterUpdated += _homegear_OnDeviceLinkConfigParameterUpdated;
            _homegear.ReloadRequired += _homegear_OnReloadRequired;
            _homegear.DeviceReloadRequired += _homegear_OnDeviceReloadRequired;
            _homegear.Reloaded += _homegear_OnReloaded;
        }

        void _homegear_OnDeviceReloadRequired(Homegear sender, Device device, Channel channel, DeviceReloadType reloadType)
        {
            if(reloadType == DeviceReloadType.Full)
            {
                WriteLog("Reloading device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                device.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if(reloadType == DeviceReloadType.Metadata)
            {
                WriteLog("Reloading metadata of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                device.Metadata.Reload();
                UpdateMetadata(device);
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Channel)
            {
                WriteLog("Reloading channel " + channel.Index + " of device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
            else if (reloadType == DeviceReloadType.Links)
            {
                WriteLog("Device links were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
                WriteLog("Reloading links of channel " + channel.Index + " and device " + device.ID.ToString() + ".");
                EnableSplitContainer(false);
                channel.Links.Reload();
                UpdateTreeView();
                EnableSplitContainer(true);
            }
        }

        void _homegear_OnReloadRequired(Homegear sender, ReloadType reloadType)
        {
            if (reloadType == ReloadType.Full)
            {
                WriteLog("Received reload required event. Reloading.");
                EnableSplitContainer(false);
                _homegear.Reload();
            }
            else if(reloadType == ReloadType.SystemVariables)
            {
                WriteLog("Reloading system variables.");
                EnableSplitContainer(false);
                _homegear.SystemVariables.Reload();
                UpdateSystemVariables();
                EnableSplitContainer(true);
            }
        }

        void _rpc_Disconnected(RPCController sender)
        {
            WriteLog("Disconnected from Homegear.");
        }

        void _rpc_Connected(RPCController sender)
        {
            WriteLog("Connected to Homegear.");
        }

        private void tvDevices_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _variableValueChangedTimer.Stop();
            _selectedDevice = null;
            _selectedLink = null;
            _selectedVariable = null;
            _selectedSystemVariable = null;
            _selectedMetadata = null;
            if (e.Node == null) return;
            _nodeLoading = true;
            if(e.Node.Level == 0)
            {
                pnMetadata.Visible = false;
                pnDevice.Visible = false;
                pnVariable.Visible = false;
                pnChannel.Visible = false;
                pnSystemVariable.Visible = false;
                pnInterface.Visible = false;
                HomegearSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Devices"))
            {
                pnHomegear.Visible = false;
                pnInterface.Visible = false;
                pnSystemVariable.Visible = false;
                pnMetadata.Visible = false;
                DeviceSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Interfaces"))
            {
                pnHomegear.Visible = false;
                pnMetadata.Visible = false;
                pnDevice.Visible = false;
                pnVariable.Visible = false;
                pnChannel.Visible = false;
                pnSystemVariable.Visible = false;
                InterfaceSelected(e);
            }
            else if(e.Node.FullPath.StartsWith("System Variables"))
            {
                pnHomegear.Visible = false;
                pnMetadata.Visible = false;
                pnDevice.Visible = false;
                pnVariable.Visible = false;
                pnChannel.Visible = false;
                pnInterface.Visible = false;
                SystemVariableSelected(e);
            }
            _nodeLoading = false;
        }

        private void HomegearSelected(TreeViewEventArgs e)
        {
            if (e.Node.Level > 0) return;
            txtVersion.Text = _homegear.Version;
            txtLogLevel.Text = _homegear.LogLevel.ToString();
            List<ServiceMessage> serviceMessages = _homegear.ServiceMessages;
            txtServiceMessages.Text = "";
            foreach(ServiceMessage message in serviceMessages)
            {
                txtServiceMessages.Text += "Device ID: " + message.PeerID.ToString() + "\t" + "Channel: " + message.Channel.ToString() + "\t" + "Type: " + message.Type + "\t" + "Value: " + message.Value.ToString() + "\r\n";
            }
            pnHomegear.Visible = true;
        }

        private void tvDevices_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _rightClickedDevice = null;
            _rightClickedMetadata = null;
            _rightClickedLink = null;
            _rightClickedChannel = null;
            if(e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if(e.Node.FullPath.StartsWith("Devices"))
                {
                    if (e.Node.Level == 1) _rightClickedDevice = (Device)e.Node.Tag;
                    else if (e.Node.Level == 2) _rightClickedDevice = (Device)e.Node.Parent.Tag;
                    else if (e.Node.Level == 3)
                    {
                        _rightClickedDevice = (Device)e.Node.Parent.Parent.Tag;
                        if (e.Node.Tag is MetadataVariable) _rightClickedMetadata = (MetadataVariable)e.Node.Tag;
                        if (e.Node.Parent.Tag is Channel) _rightClickedChannel = (Channel)e.Node.Parent.Tag;
                    }
                    else if (e.Node.Level == 5 && e.Node.Tag is Link) _rightClickedLink = (Link)e.Node.Tag;
                }
            }
        }

        private void tvDevices_AfterExpand(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e.Node == null) return;
                if (e.Node.FullPath.StartsWith("Devices")) AfterExpandDevice(e);
                else if (e.Node.FullPath.StartsWith("Interfaces")) AfterExpandInterface(e);
                else if (e.Node.FullPath.StartsWith("System Variables")) AfterExpandSystemVariables(e);
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void txtLogLevel_TextChanged(object sender, EventArgs e)
        {
            if (_nodeLoading) return;
            Int32 integerValue = 0;
            if(Int32.TryParse(txtLogLevel.Text, out integerValue))
            {
                _homegear.LogLevel = integerValue;
            }
        }

        #region Interfaces
        private void AfterExpandInterface(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (KeyValuePair<String, Interface> interfacePair in _homegear.Interfaces)
                {
                    TreeNode interfaceNode = new TreeNode(interfacePair.Key);
                    interfaceNode.Tag = interfacePair.Value;
                    e.Node.Nodes.Add(interfaceNode);
                }
            }
        }

        private void InterfaceSelected(TreeViewEventArgs e)
        {
            if (e.Node.Level == 1)
            {
                //Get interface from Homegear object to update times.
                Interface physicalInterface = _homegear.Interfaces[((Interface)e.Node.Tag).ID];
                txtInterfaceID.Text = physicalInterface.ID;
                txtInterfaceConnected.Text = physicalInterface.Connected.ToString();
                txtInterfaceDefault.Text = physicalInterface.Default.ToString();
                txtInterfaceFamily.Text = physicalInterface.Family.Name.ToString();
                txtInterfaceType.Text = physicalInterface.Type;
                txtInterfaceAddress.Text = "0x" + physicalInterface.PhysicalAddress.ToString("X2");
                System.DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                txtInterfaceSent.Text = epoch.AddSeconds(physicalInterface.LastPacketSent).ToLocalTime().ToLongTimeString();
                txtInterfaceReceived.Text = epoch.AddSeconds(physicalInterface.LastPacketReceived).ToLocalTime().ToLongTimeString();
                pnInterface.Visible = true;
            }
        }
        #endregion

        #region System Variables
        void UpdateSystemVariables()
        {
            if (tvDevices.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(UpdateSystemVariables);
                this.Invoke(d);
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "System Variables")
                    {
                        node.Collapse();
                        node.Nodes.Clear();
                        node.Nodes.Add("<loading...>");
                    }
                }
            }
        }

        private void AfterExpandSystemVariables(TreeViewEventArgs e)
        {
            if (e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach (KeyValuePair<String, SystemVariable> variablePair in _homegear.SystemVariables)
                {
                    TreeNode variableNode = new TreeNode(variablePair.Key);
                    variableNode.Tag = variablePair.Value;
                    variableNode.ContextMenuStrip = cmSystemVariable;
                    e.Node.Nodes.Add(variableNode);
                }
            }
        }

        private void SystemVariableSelected(TreeViewEventArgs e)
        {
            if (e.Node.Level == 1)
            {
                _selectedSystemVariable = (SystemVariable)e.Node.Tag;
                txtSystemVariableName.Text = _selectedSystemVariable.Name;
                txtSystemVariableType.Text = _selectedSystemVariable.Type.ToString();
                txtSystemVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtSystemVariableValue.Text = _selectedSystemVariable.ToString();
                lblSystemVariableTimer.Text = "";
                pnSystemVariable.Visible = true;
            }
        }

        private void txtSystemVariableValue_TextChanged(object sender, EventArgs e)
        {
            if (_selectedSystemVariable == null || _nodeLoading) return;
            _variableValueChangedTimer.Stop();
            _variableTimerIndex = 5;
            lblSystemVariableTimer.Text = "Sending in 5 seconds...";
            _variableValueChangedTimer.Start();
        }

        void SetSystemVariableValue(String text)
        {
            if (txtSystemVariableValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetSystemVariableValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtSystemVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtSystemVariableValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetSystemVariable()
        {
            if (_selectedSystemVariable == null || _nodeLoading) return;
            switch (_selectedSystemVariable.Type)
            {
                case RPCVariableType.rpcString:
                    _selectedSystemVariable.StringValue = txtSystemVariableValue.Text;
                    WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + txtSystemVariableValue.Text);
                    break;
                case RPCVariableType.rpcBase64:
                    _selectedSystemVariable.StringValue = txtSystemVariableValue.Text;
                    WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + txtSystemVariableValue.Text);
                    break;
                case RPCVariableType.rpcInteger:
                    Int32 integerValue = 0;
                    if (Int32.TryParse(txtSystemVariableValue.Text, out integerValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.IntegerValue = integerValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + integerValue.ToString());
                    }
                    else txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    break;
                case RPCVariableType.rpcBoolean:
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(txtSystemVariableValue.Text, out booleanValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.BooleanValue = booleanValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + booleanValue.ToString());
                    }
                    else txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    break;
                case RPCVariableType.rpcFloat:
                    Double floatValue = 0;
                    if (Double.TryParse(txtSystemVariableValue.Text, out floatValue))
                    {
                        txtSystemVariableValue.BackColor = Color.PaleGreen;
                        _selectedSystemVariable.FloatValue = floatValue;
                        WriteLog("Setting system variable \"" + _selectedSystemVariable.Name + "\" to: " + floatValue.ToString());
                    }
                    else txtSystemVariableValue.BackColor = Color.PaleVioletRed;
                    break;
            }
        }

        private void tsAddSystemVariable_Click(object sender, EventArgs e)
        {
            frmAddSystemVariable dialog = new frmAddSystemVariable();
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.VariableType.Length == 0 || dialog.VariableName.Length == 0) return;
                SystemVariable variable = null;
                switch(dialog.VariableType)
                {
                    case "Boolean":
                        Boolean booleanValue = false;
                        if(Boolean.TryParse(dialog.VariableValue, out booleanValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, booleanValue);
                        }
                        break;
                    case "Integer":
                        Int32 integerValue = 0;
                        if (Int32.TryParse(dialog.VariableValue, out integerValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, integerValue);
                        }
                        break;
                    case "Double":
                        Double doubleValue = 0;
                        if (Double.TryParse(dialog.VariableValue, out doubleValue))
                        {
                            variable = new SystemVariable(dialog.VariableName, doubleValue);
                        }
                        break;
                    case "String":
                        variable = new SystemVariable(dialog.VariableName, dialog.VariableValue);
                        break;
                    case "Base64":
                        variable = new SystemVariable(dialog.VariableName, RPCVariableType.rpcBase64);
                        variable.StringValue = dialog.VariableValue;
                        break;
                }
                if (variable != null) _homegear.SystemVariables.Add(variable);
            }
        }

        private void tsDeleteSystemVariable_Click(object sender, EventArgs e)
        {
            if (_selectedSystemVariable == null) return;
            _selectedSystemVariable.Remove();
        }
        #endregion

        #region Devices
        #region Metadata
        void UpdateMetadata(Device device)
        {
            if (tvDevices.InvokeRequired)
            {
                DeviceParameterCallback d = new DeviceParameterCallback(UpdateMetadata);
                this.Invoke(d, new object[] { device });
            }
            else
            {
                foreach (TreeNode node in tvDevices.Nodes)
                {
                    if (node.Text == "Devices")
                    {
                        foreach (TreeNode deviceNode in node.Nodes)
                        {
                            if (deviceNode.Tag is Device)
                            {
                                Device currentDevice = (Device)deviceNode.Tag;
                                if (currentDevice.ID != device.ID) continue;
                                foreach (TreeNode metadataNode in deviceNode.Nodes)
                                {
                                    if (metadataNode.Text == "Metadata")
                                    {
                                        metadataNode.Collapse();
                                        metadataNode.Nodes.Clear();
                                        metadataNode.Nodes.Add("<loading...>");
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }

            }
        }

        private void MetadataSelected(TreeViewEventArgs e)
        {
            if (e.Node.Level == 3)
            {
                _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                _selectedMetadata = (MetadataVariable)e.Node.Tag;
                txtMetadataName.Text = _selectedMetadata.Name;
                txtMetadataType.Text = _selectedMetadata.Type.ToString();
                txtMetadataValue.BackColor = System.Drawing.SystemColors.Window;
                txtMetadataValue.Text = _selectedMetadata.ToString();
                lblMetadataTimer.Text = "";
                pnMetadata.Visible = true;
            }
        }

        private void txtMetadataValue_TextChanged(object sender, EventArgs e)
        {
            if (_selectedMetadata == null || _nodeLoading) return;
            _variableValueChangedTimer.Stop();
            _variableTimerIndex = 5;
            lblMetadataTimer.Text = "Sending in 5 seconds...";
            _variableValueChangedTimer.Start();
        }

        void SetMetadataValue(String text)
        {
            if (txtMetadataValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetMetadataValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtMetadataValue.BackColor = System.Drawing.SystemColors.Window;
                txtMetadataValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetMetadata()
        {
            if (_selectedDevice == null || _selectedMetadata == null || _nodeLoading) return;
            switch (_selectedMetadata.Type)
            {
                case RPCVariableType.rpcString:
                    _selectedMetadata.StringValue = txtMetadataValue.Text;
                    WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + txtMetadataValue.Text);
                    break;
                case RPCVariableType.rpcBase64:
                    _selectedMetadata.StringValue = txtMetadataValue.Text;
                    WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + txtMetadataValue.Text);
                    break;
                case RPCVariableType.rpcInteger:
                    Int32 integerValue = 0;
                    if (Int32.TryParse(txtMetadataValue.Text, out integerValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.IntegerValue = integerValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + integerValue.ToString());
                    }
                    else txtMetadataValue.BackColor = Color.PaleVioletRed;
                    break;
                case RPCVariableType.rpcBoolean:
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(txtMetadataValue.Text, out booleanValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.BooleanValue = booleanValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + booleanValue.ToString());
                    }
                    else txtMetadataValue.BackColor = Color.PaleVioletRed;
                    break;
                case RPCVariableType.rpcFloat:
                    Double floatValue = 0;
                    if (Double.TryParse(txtMetadataValue.Text, out floatValue))
                    {
                        txtMetadataValue.BackColor = Color.PaleGreen;
                        _selectedMetadata.FloatValue = floatValue;
                        WriteLog("Setting metadata \"" + _selectedMetadata.Name + "\" of device \"" + _selectedDevice.ID + "\" to: " + floatValue.ToString());
                    }
                    else txtMetadataValue.BackColor = Color.PaleVioletRed;
                    break;
            }
        }

        private void tsAddMetadata_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null) return;
            frmAddSystemVariable dialog = new frmAddSystemVariable();
            dialog.Text = "Add Metadata (Device " + _rightClickedDevice.ID.ToString() + ")";
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.VariableType.Length == 0 || dialog.VariableName.Length == 0) return;
                MetadataVariable variable = null;
                switch (dialog.VariableType)
                {
                    case "Boolean":
                        Boolean booleanValue = false;
                        if (Boolean.TryParse(dialog.VariableValue, out booleanValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, booleanValue);
                        }
                        break;
                    case "Integer":
                        Int32 integerValue = 0;
                        if (Int32.TryParse(dialog.VariableValue, out integerValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, integerValue);
                        }
                        break;
                    case "Double":
                        Double doubleValue = 0;
                        if (Double.TryParse(dialog.VariableValue, out doubleValue))
                        {
                            variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, doubleValue);
                        }
                        break;
                    case "String":
                        variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, dialog.VariableValue);
                        break;
                    case "Base64":
                        variable = new MetadataVariable(_rightClickedDevice.ID, dialog.VariableName, RPCVariableType.rpcBase64);
                        variable.StringValue = dialog.VariableValue;
                        break;
                }
                if (variable != null) _rightClickedDevice.Metadata.Add(variable);
            }
        }

        private void tsRemoveMetadata_Click(object sender, EventArgs e)
        {
            if (_rightClickedMetadata == null) return;
            _rightClickedMetadata.Remove();
        }
        #endregion

        #region "Links"
        private void tsAddLink_Click(object sender, EventArgs e)
        {
            if (_rightClickedChannel == null || _rightClickedDevice == null) return;
            frmAddLink dialog = new frmAddLink(_rightClickedChannel, _homegear);
            dialog.Text = "Add Link (Device: " + _rightClickedDevice.ID.ToString() + ", Channel: " + _rightClickedChannel.Index.ToString() + ")";
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if (dialog.LinkTo == null) return;
                bool isSender = _rightClickedChannel.LinkSourceRoles.Length > 0;
                _rightClickedChannel.Links.Add(dialog.LinkTo.PeerID, dialog.LinkTo.Index, isSender);
                if(isSender && _rightClickedChannel.GroupedWith > -1)
                {
                    _rightClickedDevice.Channels[_rightClickedChannel.GroupedWith].Links.Add(dialog.LinkTo.PeerID, dialog.LinkTo.Index, isSender);
                }
            }
        }

        private void tsRemoveLink_Click(object sender, EventArgs e)
        {
            if (_rightClickedLink == null) return;
            _rightClickedLink.Remove();
        }
        #endregion

        private void AfterExpandDevice(TreeViewEventArgs e)
        {
            if (e.Node.Level == 2)
            {
                if (e.Node.Text == "Metadata")
                {
                    Device device = (Device)e.Node.Parent.Tag;
                    e.Node.Nodes.Clear();
                    foreach (KeyValuePair<String, MetadataVariable> variable in device.Metadata)
                    {
                        TreeNode variableNode = new TreeNode(variable.Key);
                        variableNode.Tag = variable.Value;
                        variableNode.ContextMenuStrip = cmMetadataVariable;
                        e.Node.Nodes.Add(variableNode);
                    }
                    if (e.Node.Nodes.Count == 0) e.Node.Nodes.Add("Empty");
                }
            }
            else if (e.Node.Level == 3)
            {
                if (e.Node.Text == "Config")
                {
                    e.Node.Nodes.Clear();
                    Channel channel = (Channel)e.Node.Tag;
                    foreach (KeyValuePair<String, ConfigParameter> parameter in channel.Config)
                    {
                        TreeNode parameterNode = new TreeNode(parameter.Key);
                        parameterNode.Tag = parameter.Value;
                        e.Node.Nodes.Add(parameterNode);
                    }
                    if (e.Node.Nodes.Count == 0) e.Node.Nodes.Add("Empty");
                }
                else if (e.Node.Text == "Links")
                {
                    e.Node.Nodes.Clear();
                    Channel channel = (Channel)e.Node.Tag;
                    foreach (KeyValuePair<Int32, ReadOnlyDictionary<Int32, Link>> remotePeer in channel.Links)
                    {
                        TreeNode remotePeerNode = new TreeNode("Device " + remotePeer.Key.ToString());
                        remotePeerNode.Tag = remotePeer.Value;

                        foreach (KeyValuePair<Int32, Link> linkPair in remotePeer.Value)
                        {
                            TreeNode remoteChannelNode = new TreeNode("Channel " + linkPair.Key.ToString());
                            remoteChannelNode.Tag = linkPair.Value;
                            remoteChannelNode.ContextMenuStrip = cmLink;

                            TreeNode linkConfigNode = new TreeNode("Config");
                            linkConfigNode.Tag = linkPair.Value;
                            linkConfigNode.Nodes.Add("<loading...>");
                            remoteChannelNode.Nodes.Add(linkConfigNode);

                            remotePeerNode.Nodes.Add(remoteChannelNode);
                        }

                        e.Node.Nodes.Add(remotePeerNode);
                    }
                    if (e.Node.Nodes.Count == 0) e.Node.Nodes.Add("Empty");
                }
            }
            else if (e.Node.Level == 6)
            {
                if (e.Node.Text == "Config")
                {
                    e.Node.Nodes.Clear();
                    Link link = (Link)e.Node.Tag;
                    foreach (KeyValuePair<String, ConfigParameter> parameter in link.Config)
                    {
                        TreeNode parameterNode = new TreeNode(parameter.Key);
                        parameterNode.Tag = parameter.Value;
                        e.Node.Nodes.Add(parameterNode);
                    }
                    if (e.Node.Nodes.Count == 0) e.Node.Nodes.Add("Empty");
                }
            }
        }

        private void DeviceSelected(TreeViewEventArgs e)
        {
            if (e.Node.Level == 1)
            {
                if (e.Node.Level == 1) _selectedDevice = (Device)e.Node.Tag;
                txtSerialNumber.Text = _selectedDevice.SerialNumber;
                txtID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                txtTypeString.Text = _selectedDevice.TypeString;
                if (_selectedDevice.Family != null) txtFamily.Text = _selectedDevice.Family.Name;
                txtDeviceName.Text = _selectedDevice.Name;
                txtInterface.BackColor = System.Drawing.SystemColors.Window;
                txtInterface.Text = _selectedDevice.Interface.ID;
                txtPhysicalAddress.Text = "0x" + _selectedDevice.Address.ToString("X2");
                txtFirmware.Text = _selectedDevice.Firmware;
                txtAvailableFirmware.Text = _selectedDevice.AvailableFirmware;
                txtRXModes.Text = "";
                if ((_selectedDevice.RXMode & DeviceRXMode.Always) == DeviceRXMode.Always) txtRXModes.Text += "Always\r\n";
                if ((_selectedDevice.RXMode & DeviceRXMode.Burst) == DeviceRXMode.Burst) txtRXModes.Text += "Burst\r\n";
                if ((_selectedDevice.RXMode & DeviceRXMode.Config) == DeviceRXMode.Config) txtRXModes.Text += "Config\r\n";
                if ((_selectedDevice.RXMode & DeviceRXMode.LazyConfig) == DeviceRXMode.LazyConfig) txtRXModes.Text += "LazyConfig\r\n";
                if ((_selectedDevice.RXMode & DeviceRXMode.WakeUp) == DeviceRXMode.WakeUp) txtRXModes.Text += "WakeUp\r\n";
                pnVariable.Visible = false;
                pnChannel.Visible = false;
                pnDevice.Visible = true;
            }
            else if (e.Node.Level == 3 && e.Node.Tag is MetadataVariable) MetadataSelected(e);
            else if (e.Node.Level > 1 && e.Node.Level <= 3)
            {
                Channel channel = null;
                if (e.Node.Level == 2 && e.Node.Tag is Channel)
                {
                    _selectedDevice = (Device)e.Node.Parent.Tag;
                    channel = (Channel)e.Node.Tag;
                }
                if (e.Node.Level == 3 && e.Node.Parent.Tag is Channel)
                {
                    _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                    channel = (Channel)e.Node.Parent.Tag;
                }
                if (channel == null)
                {
                    pnChannel.Visible = false;
                    pnDevice.Visible = false;
                    _nodeLoading = false;
                    return;
                }
                txtChannelPeerID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                txtChannelIndex.Text = channel.Index.ToString();
                txtChannelTypeString.Text = channel.TypeString;
                txtChannelAESActive.Text = channel.AESActive.ToString();
                txtChannelDirection.Text = channel.Direction.ToString();
                txtChannelLinkSourceRoles.Text = "";
                foreach (String role in channel.LinkSourceRoles)
                {
                    txtChannelLinkSourceRoles.Text += role + "\r\n";
                }
                txtChannelLinkTargetRoles.Text = "";
                foreach (String role in channel.LinkTargetRoles)
                {
                    txtChannelLinkTargetRoles.Text += role + "\r\n";
                }
                txtChannelTeam.Text = channel.Team;
                txtChannelTeamTag.Text = channel.TeamTag;
                txtChannelTeamMembers.Text = "";
                foreach (String teamMember in channel.TeamMembers)
                {
                    txtChannelTeamMembers.Text += teamMember + "\r\n";
                }
                txtChannelGroupedWith.Text = channel.GroupedWith.ToString();
                pnDevice.Visible = false;
                pnVariable.Visible = false;
                pnChannel.Visible = true;
            }
            else if (e.Node.Level == 4 || e.Node.Level == 7)
            {
                if (e.Node.Tag == null || !(e.Node.Tag is Variable))
                {
                    _nodeLoading = false;
                    return;
                }
                _selectedVariable = (Variable)e.Node.Tag;
                if (e.Node.Level == 4) _selectedDevice = (Device)e.Node.Parent.Parent.Parent.Tag;
                else
                {
                    _selectedDevice = (Device)e.Node.Parent.Parent.Parent.Parent.Parent.Parent.Tag;
                    _selectedLink = (Link)e.Node.Parent.Tag;
                }
                txtDeviceID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                txtDeviceChannel.Text = _selectedVariable.Channel.ToString();
                txtVariableName.Text = _selectedVariable.Name;
                txtVariableType.Text = _selectedVariable.Type.ToString();
                chkVariableReadable.Checked = _selectedVariable.Readable;
                chkVariableWriteable.Checked = _selectedVariable.Writeable;
                txtUnit.Text = _selectedVariable.Unit;
                txtVariableMin.Text = (_selectedVariable.Type == VariableType.tDouble) ? _selectedVariable.MinDouble.ToString() : ((_selectedVariable.Type == VariableType.tInteger || _selectedVariable.Type == VariableType.tEnum) ? _selectedVariable.MinInteger.ToString() : "");
                txtVariableMax.Text = (_selectedVariable.Type == VariableType.tDouble) ? _selectedVariable.MaxDouble.ToString() : ((_selectedVariable.Type == VariableType.tInteger || _selectedVariable.Type == VariableType.tEnum) ? _selectedVariable.MaxInteger.ToString() : "");
                txtVariableDefault.Text = _selectedVariable.DefaultToString();
                txtVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtVariableValue.Text = _selectedVariable.ToString();
                if (_selectedVariable is ConfigParameter) bnPutParamset.Visible = true; else bnPutParamset.Visible = false;
                lblVariableTimer.Text = "";
                txtUIFlags.Text = "";
                if ((_selectedVariable.UIFlags & VariableUIFlags.fVisible) == VariableUIFlags.fVisible) txtUIFlags.Text += "Visible\r\n";
                if ((_selectedVariable.UIFlags & VariableUIFlags.fInternal) == VariableUIFlags.fInternal) txtUIFlags.Text += "Internal\r\n";
                if ((_selectedVariable.UIFlags & VariableUIFlags.fTransform) == VariableUIFlags.fTransform) txtUIFlags.Text += "Transform\r\n";
                if ((_selectedVariable.UIFlags & VariableUIFlags.fService) == VariableUIFlags.fService) txtUIFlags.Text += "Service\r\n";
                if ((_selectedVariable.UIFlags & VariableUIFlags.fSticky) == VariableUIFlags.fSticky) txtUIFlags.Text += "Sticky\r\n";
                txtValueList.Text = "";
                for (Int32 i = 0; i < _selectedVariable.ValueList.Length; i++)
                {
                    txtValueList.Text += i.ToString() + " " + _selectedVariable.ValueList[i] + "\r\n";
                }
                txtSpecialValues.Text = "";
                if (_selectedVariable.Type == VariableType.tDouble)
                {
                    foreach (KeyValuePair<Double, String> specialValue in _selectedVariable.SpecialDoubleValues)
                    {
                        txtSpecialValues.Text += specialValue.Key.ToString() + ": " + specialValue.Value + "\r\n";
                    }
                }
                else
                {
                    foreach (KeyValuePair<Int32, String> specialValue in _selectedVariable.SpecialIntegerValues)
                    {
                        txtSpecialValues.Text += specialValue.Key.ToString() + ": " + specialValue.Value + "\r\n";
                    }
                }
                if (_selectedVariable.Writeable) txtVariableValue.ReadOnly = false; else txtVariableValue.ReadOnly = true;
                pnDevice.Visible = false;
                pnChannel.Visible = false;
                pnVariable.Visible = true;
            }
        }

        private void txtVariableValue_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_selectedVariable == null || _nodeLoading || !_selectedVariable.Writeable) return;
                if (_selectedVariable is ConfigParameter)
                {
                    SetVariable();
                }
                else
                {
                    _variableValueChangedTimer.Stop();
                    _variableTimerIndex = 5;
                    lblVariableTimer.Text = "Sending in 5 seconds...";
                    _variableValueChangedTimer.Start();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        void SetVariableValue(String text)
        {
            if (txtVariableValue.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetVariableValue);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                _nodeLoading = true;
                txtVariableValue.BackColor = System.Drawing.SystemColors.Window;
                txtVariableValue.Text = text;
                _nodeLoading = false;
            }
        }

        void SetVariable()
        {
            if (_selectedVariable == null || _nodeLoading || !_selectedVariable.Writeable) return;
            Int32 integerValue = 0;
            switch (_selectedVariable.Type)
            {
                case VariableType.tString:
                    _selectedVariable.StringValue = txtVariableValue.Text;
                    WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + txtVariableValue.Text);
                    break;
                case VariableType.tInteger:
                    if (Int32.TryParse(txtVariableValue.Text, out integerValue))
                    {
                        txtVariableValue.BackColor = Color.PaleGreen;
                        _selectedVariable.IntegerValue = integerValue;
                        WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + integerValue.ToString());
                    }
                    else txtVariableValue.BackColor = Color.PaleVioletRed;
                    break;
                case VariableType.tEnum:
                    if (Int32.TryParse(txtVariableValue.Text, out integerValue))
                    {
                        txtVariableValue.BackColor = Color.PaleGreen;
                        _selectedVariable.IntegerValue = integerValue;
                        WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + integerValue.ToString());
                    }
                    else txtVariableValue.BackColor = Color.PaleVioletRed;
                    break;
                case VariableType.tDouble:
                    Double doubleValue = 0;
                    if (Double.TryParse(txtVariableValue.Text, out doubleValue))
                    {
                        txtVariableValue.BackColor = Color.PaleGreen;
                        _selectedVariable.DoubleValue = doubleValue;
                        WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + doubleValue.ToString());
                    }
                    else txtVariableValue.BackColor = Color.PaleVioletRed;
                    break;
                case VariableType.tBoolean:
                    Boolean booleanValue = false;
                    if (Boolean.TryParse(txtVariableValue.Text, out booleanValue))
                    {
                        txtVariableValue.BackColor = Color.PaleGreen;
                        _selectedVariable.BooleanValue = booleanValue;
                        WriteLog("Setting variable \"" + _selectedVariable.Name + "\" of device " + _selectedVariable.PeerID.ToString() + " and channel " + _selectedVariable.Channel.ToString() + " to: " + booleanValue.ToString());
                    }
                    else txtVariableValue.BackColor = Color.PaleVioletRed;
                    break;
            }
        }

        private void bnPutParamset_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedVariable == null || _selectedDevice == null || !_selectedVariable.Writeable || !_selectedDevice.Channels.ContainsKey(_selectedVariable.Channel)) return;
                if (_selectedLink != null) _selectedLink.Config.Put();
                else _selectedDevice.Channels[_selectedVariable.Channel].Config.Put();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void txtDeviceName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null || _nodeLoading) return;
            _selectedDevice.Name = txtDeviceName.Text;
        }

        private void txtInterface_TextChanged(object sender, EventArgs e)
        {
            if (_selectedDevice == null || _nodeLoading) return;
            Interfaces interfaces = _homegear.Interfaces;
            if (!interfaces.ContainsKey(txtInterface.Text))
            {
                txtInterface.BackColor = Color.PaleVioletRed;
                return;
            }
            txtInterface.BackColor = Color.PaleGreen;
            _selectedDevice.Interface = interfaces[txtInterface.Text];
        }

        private void tsAddDevice_Click(object sender, EventArgs e)
        {
            frmAddDevice dialog = new frmAddDevice();
            if(dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                if(dialog.SerialNumber.Length == 0) return;
                if(_homegear.Devices.Add(dialog.SerialNumber))
                {
                    MessageBox.Show(this, "Device added successfully.", "Device added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "No device was found.", "Device not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        private void tsEnablePairingMode_Click(object sender, EventArgs e)
        {
            Int32 timeLeftInPairingMode = _homegear.TimeLeftInPairingMode();
            if (timeLeftInPairingMode == 0) _homegear.EnablePairingMode(true);
            else MessageBox.Show(this, "Pairing mode is still enabled for another " + timeLeftInPairingMode.ToString() + " seconds.", "Already in pairing mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsDisablePairingMode_Click(object sender, EventArgs e)
        {
            _homegear.EnablePairingMode(false);
        }

        private void tsSearchDevices_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, _homegear.SearchDevices().ToString() + " new devices found.", "Device Search Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsUnpair_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null) return;
            _rightClickedDevice.Unpair();
            MessageBox.Show(this, "Unpairing device with ID " + _rightClickedDevice.ID.ToString(), "Unpairing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsReset_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null) return;
            _rightClickedDevice.Reset();
            MessageBox.Show(this, "Resetting device with ID " + _rightClickedDevice.ID.ToString(), "Resetting", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void tsRemove_Click(object sender, EventArgs e)
        {
            if (_rightClickedDevice == null) return;
            _rightClickedDevice.Remove();
            MessageBox.Show(this, "Removing device with ID " + _rightClickedDevice.ID.ToString(), "Removing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
