using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BizTalk.Tools.SSOApplicationConfiguration
{
    public partial class Password : Form
    {
        private static bool ok = false;
        private static string password = "";
        public Password()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Length >= 3)
            {
                ok = true;
                password = textBox1.Text;
                this.Dispose();
            }
            else
            {
                MessageBox.Show("The password must have more than 3 chars!", "Password Length", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }            
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void keyDown_Password(object sender,KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) okButton_Click(null, null);
        }

        public static void getPassword(out bool ok_aux,out string password_aux)
        {
            new Password().ShowDialog();
            ok_aux = ok;
            ok = false;
            password_aux = password;
        }
    }
}
