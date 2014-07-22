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
        delegate void BooleanParameterCallback(Boolean value);
        delegate void SetTextCallback(string text);
        RPCController _rpc = null;
        Homegear _homegear = null;
        Device _rightClickedDevice = null;
        Device _selectedDevice = null;
        Link _selectedLink = null;
        Variable _selectedVariable = null;
        bool _nodeLoading = false;
        Int32 _variableTimerIndex = 5;
        Timer _variableValueChangedTimer = new Timer();

        public frmMain()
        {
            InitializeComponent();

            _variableValueChangedTimer.Interval = 1000;
            _variableValueChangedTimer.Tick += _variableValueChangedTimer_Tick;
            lblVariableTimer.Text = "";
        }

        void _variableValueChangedTimer_Tick(object sender, EventArgs e)
        {
            _variableValueChangedTimer.Stop();
            if(_variableTimerIndex > 1)
            {
                _variableTimerIndex--;
                lblVariableTimer.Text = "Sending in " + _variableTimerIndex.ToString() + " seconds...";
                _variableValueChangedTimer.Start();
                return;
            }
            lblVariableTimer.Text = "";
            SetVariable();
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

        void _homegear_OnReloaded(Homegear sender)
        {
            WriteLog("Reload complete. Received " + sender.Devices.Count + " devices.");
            UpdateDevices();
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

        void UpdateDevices()
        {
            if(tvDevices.InvokeRequired)
            {
                NoParameterCallback d = new NoParameterCallback(UpdateDevices);
                this.Invoke(d);
            }
            else
            {
                tvDevices.Nodes.Clear();
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

        private void _homegear_OnDeviceLinksUpdated(Homegear sender, Device device, Channel channel)
        {
            WriteLog("Device links were updated: Device type: \"" + device.TypeString + "\", ID: " + device.ID.ToString() + ", Channel: " + channel.Index.ToString());
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
            _homegear.DeviceVariableUpdated += _homegear_OnDeviceVariableUpdated;
            _homegear.DeviceConfigParameterUpdated += _homegear_OnDeviceConfigParameterUpdated;
            _homegear.DeviceLinkConfigParameterUpdated += _homegear_OnDeviceLinkConfigParameterUpdated;
            _homegear.DeviceLinksUpdated += _homegear_OnDeviceLinksUpdated;
            _homegear.ReloadRequired += _homegear_OnReloadRequired;
            _homegear.Reloaded += _homegear_OnReloaded;
        }

        void _homegear_OnReloadRequired(Homegear sender)
        {
            WriteLog("Received reload required event. Reloading.");
            EnableSplitContainer(false);
            _homegear.Reload();
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
            if (e.Node == null) return;
            _nodeLoading = true;
            if (e.Node.FullPath.StartsWith("Devices"))
            {
                pnInterface.Visible = false;
                DeviceSelected(e);
            }
            else if (e.Node.FullPath.StartsWith("Interfaces"))
            {
                pnDevice.Visible = false;
                pnVariable.Visible = false;
                pnChannel.Visible = false;
                InterfaceSelected(e);
            }
            _nodeLoading = false;
        }

        private void DeviceSelected(TreeViewEventArgs e)
        {
            _selectedDevice = null;
            _selectedLink = null;
            _selectedVariable = null;
            if (e.Node.Level == 1)
            {
                if (e.Node.Level == 1) _selectedDevice = (Device)e.Node.Tag;
                txtSerialNumber.Text = _selectedDevice.SerialNumber;
                txtID.Text = (_selectedDevice.ID >= 0x40000000) ? "0x" + _selectedDevice.ID.ToString("X2") : _selectedDevice.ID.ToString();
                txtTypeString.Text = _selectedDevice.TypeString;
                if(_selectedDevice.Family != null) txtFamily.Text = _selectedDevice.Family.Name;
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
            else if (e.Node.Level > 1 && e.Node.Level <= 3)
            {
                Channel channel = null;
                if (e.Node.Level == 2)
                {
                    _selectedDevice = (Device)e.Node.Parent.Tag;
                    channel = (Channel)e.Node.Tag;
                }
                if (e.Node.Level == 3)
                {
                    _selectedDevice = (Device)e.Node.Parent.Parent.Tag;
                    channel = (Channel)e.Node.Parent.Tag;
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
                if (e.Node.Tag == null)
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

        private void tvDevices_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if(e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if(e.Node.FullPath.StartsWith("Devices") && e.Node.Level == 1)
                {
                    _rightClickedDevice = (Device)e.Node.Tag;
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
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void AfterExpandDevice(TreeViewEventArgs e)
        {
            if (e.Node.Level == 3)
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

        private void AfterExpandInterface(TreeViewEventArgs e)
        {
            if(e.Node.Level == 0)
            {
                e.Node.Nodes.Clear();
                foreach(KeyValuePair<String, Interface> interfacePair in _homegear.Interfaces)
                {
                    TreeNode interfaceNode = new TreeNode(interfacePair.Key);
                    interfaceNode.Tag = interfacePair.Value;
                    e.Node.Nodes.Add(interfaceNode);
                }
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
            catch(Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        #region Devices
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
