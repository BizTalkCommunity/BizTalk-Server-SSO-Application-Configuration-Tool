using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

//Step #1 - add reference
//Step #2 - add "using" statement for SSO class
using Microsoft.SSO.Utility;

namespace BizTalk.Tools.SSOStorage.Tester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Step #3, make call to "read" operation
            string response = SSOConfigHelper.Read(txtAppName.Text, txtProp.Text);

            txtVal.Text = response;
        }
    }
}