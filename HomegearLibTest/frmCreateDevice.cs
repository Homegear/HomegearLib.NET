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

namespace HomegearLibTest
{
    public partial class frmCreateDevice : Form
    {
        private Homegear _homegear = null;

        public Family Family
        {
            get
            {
                return (Family)cbFamilies.SelectedItem;
            }
        }

        public Int32 DeviceType
        {
            get
            {
                if (!Int32.TryParse(txtDeviceType.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int deviceType))
                {
                    deviceType = 0;
                }

                return deviceType;
            }
        }

        public String SerialNumber { get { return txtSerialNumber.Text; } }

        public Int32 Address
        {
            get
            {
                Int32 address = -1;
                if (!Int32.TryParse(txtAddress.Text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out address))
                {
                    address = -1;
                }

                return address;
            }
        }

        public Int32 FirmwareVersion
        {
            get
            {
                Int32 firmwareVersion = -1;
                if(!Int32.TryParse(txtFirmwareVersion.Text, out firmwareVersion))
                {
                    firmwareVersion = -1;
                }

                return firmwareVersion;
            }
        }

        public frmCreateDevice(Homegear homegear, Int64 address = 0, Family family = null)
        {
            _homegear = homegear;
            InitializeComponent();
            foreach(KeyValuePair<Int64, Family> familyEntry in homegear.Families)
            {
                cbFamilies.Items.Add(familyEntry.Value);
            }
            if (family != null)
            {
                cbFamilies.SelectedItem = family;
            }

            if (address != 0)
            {
                txtAddress.Text = address.ToString("X");
            }
        }

        private void bnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void bnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Abort;
            Close();
        }
    }
}
