using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;

namespace BizTalk.Tools.SSOApplicationConfiguration
{
    public partial class Settings : Form
    {
        private static bool ok = false;
        public Settings()
        {
            InitializeComponent();
            var settings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
            textBox1.Text = settings.Get("AppAdminAcct");
            textBox2.Text = settings.Get("ContactInfo");
            textBox3.Text = settings.Get("AppUserAcct");
        }


        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void okButton_Click(object sender, EventArgs e)
        {

            if (textBox1.Text.Equals(string.Empty) ||
                textBox2.Text.Equals(string.Empty) ||
                textBox3.Text.Equals(string.Empty))
            {
                MessageBox.Show("Fields Missing!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                config.AppSettings.Settings.Remove("AppAdminAcct");
                config.AppSettings.Settings.Remove("ContactInfo");
                config.AppSettings.Settings.Remove("AppUserAcct");

                config.AppSettings.Settings.Add("AppAdminAcct", textBox1.Text);
                config.AppSettings.Settings.Add("ContactInfo", textBox2.Text);
                config.AppSettings.Settings.Add("AppUserAcct", textBox3.Text);

                ApplicationManager.AdminAcct = textBox1.Text;
                ApplicationManager.ContactInfo = textBox2.Text;
                ApplicationManager.AuserAcct = textBox3.Text;

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MessageBox.Show("Settings Updated", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ok = true;
                this.Dispose();
            }

        }
        private void keyDown_Settings(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) okButton_Click(null, null);
        }

        public static void updateSettings(out bool k)
        {
            new Settings().ShowDialog();
            k = ok;
            ok = false;
        }
    }
}
