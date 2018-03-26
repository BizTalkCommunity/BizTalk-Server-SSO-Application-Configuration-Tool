using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.EnterpriseSingleSignOn.Interop;

using System.Configuration;
using System.Text.RegularExpressions;

namespace BizTalk.Tools.SSOApplicationConfiguration
{
    public partial class ApplicationManager : Form
    {
        private const string _newapplication = "_NewApplication";
        private int indexRow = -1;
        private bool showDiscard = false;

        public static string ContactInfo = string.Empty;
        public static string AdminAcct = string.Empty;
        public static string AuserAcct = string.Empty;   

        internal Boolean invalidCell = false;

        public ApplicationManager()
        { 
            InitializeComponent();
            try
            {
                var settings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

                ContactInfo = settings.Get("ContactInfo");
                AdminAcct = settings.Get("AppAdminAcct");
                AuserAcct = settings.Get("AppUserAcct");

                Utils.Search(tvApps, "", dgvSearch);

            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app.config.");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            Utils.Search(tvApps, txbSearch.Text, dgvSearch);
        }

        private void btnSaveGrid_Click(object sender, EventArgs e)
        {
            // exit if no node is selected
            var selectedNode = tvApps.SelectedNode;
            if (selectedNode == null || selectedNode.Level == 0) return;
    
            save(selectedNode.Text);
            Utils._gridChanged = false;
        }

        private void save (string name)
        {
            SSOPropBag propertiesBag = new SSOPropBag();

            ISSOAdmin a = new ISSOAdmin();

            string appUserAcct, appAdminAcct, description, contactInfo;

            HybridDictionary props = SSOConfigManager.GetConfigProperties(tvApps.SelectedNode.Text, out description, out contactInfo, out appUserAcct, out appAdminAcct);

            foreach (DataGridViewRow row in dgvSearch.Rows)
            {
                if (row.Cells[0].Value != null)
                {
                    if (row.Cells[1].Value == null)
                    { 
                        MessageBox.Show("The key value cannot be blank!", "Blank key value",MessageBoxButtons.OK,MessageBoxIcon.Error);
                        return;
                    }

                    //set values
                    string propName = row.Cells[0].Value.ToString();
                    object objPropValue = row.Cells[1].Value;

                    propertiesBag.Write(propName, ref objPropValue);

                    if (!props.Contains(propName))
                        a.CreateFieldInfo(tvApps.SelectedNode.Text, propName, 0);

                }
            }

            a.UpdateApplication(tvApps.SelectedNode.Text, null, null, null, null, SSOFlag.SSO_FLAG_ENABLED, SSOFlag.SSO_FLAG_ENABLED);

            try
            {
                SSOConfigManager.SetConfigProperties(tvApps.SelectedNode.Text, propertiesBag);

                MessageBox.Show("Properties saved successfully.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occured.  Details: " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool UpdateSSOApplication(string newName, string oldName)
        {
            //write to SSOPropBag to figure out how many rows we have
            ArrayList maskArray = new ArrayList();

            try
            {
                HybridDictionary props = new HybridDictionary();
                
                foreach (DataGridViewRow row in dgvSearch.Rows)
                {
                    int keyIndex = 0;
                    int valueIndex = 1;
                    
                    if (row.Cells[keyIndex].Value != null)
                    {                    
                        // insert values on grid from property bag, replace special characters like \r \t
                        object objPropValue = row.Cells[valueIndex].Value.ToString().Replace("\t", "").Replace("\r", "");
                        object objPropKey = row.Cells[keyIndex].Value.ToString().Replace("\t", "").Replace("\r", "");
                        props.Add(objPropKey, objPropValue);
                        maskArray.Add(0);
                    }
                }

                //if (newApp)
                //{
                    SSOConfigManager.DeleteApplication(oldName);
                //}

                SSOPropBag propertiesBag = new SSOPropBag(props);

                //create and enable application
                SSOConfigManager.CreateConfigStoreApplication(newName, "", ContactInfo,
                    AuserAcct, AdminAcct, propertiesBag, maskArray);

                //set default configuration field values
                SSOConfigManager.SetConfigProperties(newName, propertiesBag);

            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Erro, verifique se existe alguma key repetida\n" + ex.ToString(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occured.  Details: " + ex.ToString(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        private void tvApps_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string appName = e.Node.Text;

            try
            {
                switch (e.Node.Level)
                {
                    case 0:
                        ClearSearchGrid();
                        break;
                    case 1:
                        try
                        {
                            Utils.LoadGrid(appName, dgvSearch);
                        }catch { 
                            InicSearchGrid();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occured.  Details: " + ex.ToString(),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ctmTreeDelete_Click(object sender, EventArgs e)
        {

            if (
                MessageBox.Show(string.Format("Are you sure you wish to delete '{0}'?", tvApps.SelectedNode.Text),
                "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    SSOConfigManager.DeleteApplication(tvApps.SelectedNode.Text);

                    //confirmation
                    MessageBox.Show("Application deleted.", "Information", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Occured.  Details: " + ex.ToString(), "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                Utils._gridChanged = false;
                tvApps.SelectedNode = tvApps.Nodes[0];
                Utils.Search(tvApps, txbSearch.Text, dgvSearch);
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tvApps.LabelEdit = true;
            tvApps.SelectedNode.BeginEdit();
        }

        private bool appExist(string name)
        {

            foreach (string s in SSOConfigManager.GetApplications())
            {
                if (s.Equals(name)) return true;
            }
            return false;
        }

        private void tvApps_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (tvApps.SelectedNode.Level < 2)
            {
                if (string.IsNullOrWhiteSpace(e.Label))
                {
                    e.CancelEdit = true;
                }
                else
                {
                    if (e.Label != null)
                    {
                        if (e.Label.Length > 0)
                        {
                            if (e.Label.IndexOfAny(new char[] { '@', ',', '!' }) == -1)
                            {
                                if (appExist(e.Label))
                                {
                                    e.CancelEdit = true;
                                    MessageBox.Show("Invalid application name.\nThat application name already exists!",
                                        "Aplication Name Edit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                    e.Node.BeginEdit();

                                }
                                else
                                {
                                    // Stop editing without canceling the label change.
                                    e.Node.EndEdit(false);
                                    UpdateSSOApplication(e.Label, e.Node.Text);
                                }
                            }
                            else
                            {
                                /* Cancel the label edit action, inform the user, and 
                                place the node in edit mode again. */
                                e.CancelEdit = true;
                                MessageBox.Show("Invalid application name.\nThe invalid characters are: '@','.', ',', '!'",
                                    "Aplication Name Edit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                e.Node.BeginEdit();

                            }
                        }
                        else
                        {
                            /* Cancel the label edit action, inform the user, and 
                            place the node in edit mode again. */
                            e.CancelEdit = true;
                            MessageBox.Show("Invalid application name.\nThe name cannot be blank",
                                "Node Label Edit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            e.Node.BeginEdit();

                        }
                    }
                }
            }
            else
            {
                string oldNode = e.Node.Text;
                this.BeginInvoke(new Action(() => afterEditNode(e.Node, oldNode)));
            }
        }

        private void afterEditNode(TreeNode node, string oldNode)
        {
            string parentSelectedNode = tvApps.SelectedNode.Parent.Text.ToString();

            if (Utils.RenameKeyValueFromSSOApp(node, oldNode, parentSelectedNode))
            {
                Utils.Search(tvApps, string.Empty, dgvSearch);
            }
        }


        private void tvApps_KeyDown(object sender, KeyEventArgs e)
        {
            //Delete
            if (e.KeyCode == Keys.Delete && tvApps.SelectedNode != null && tvApps.SelectedNode.Level == 1)
            {
                ctmTreeDelete_Click(null,null);
            }

            //Rename
            if(e.KeyCode == Keys.F2 && tvApps.SelectedNode != null && tvApps.SelectedNode.Level < 2)
            {
                tvApps.LabelEdit = true;
                tvApps.SelectedNode.BeginEdit();
            }
        }

        private void refreshF5(Object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                Utils.Search(tvApps, txbSearch.Text, dgvSearch);
            }
        }

        private void dgvSearch_KeyDown(object sender, KeyEventArgs e)
        {
            //Delete 
            if (e.KeyCode == Keys.Delete)
            {

                deleteRowToolStripMenuItem_Click(null, null);
            }
        }


        private void InicSearchGrid()
        {
            dgvSearch.Enabled = true;
            dgvSearch.Rows.Clear();
            dgvSearch.DefaultCellStyle.BackColor = SystemColors.Window;
            Utils._gridChanged = false;
        }
        private void ClearSearchGrid()
        {
            dgvSearch.Rows.Clear();
            dgvSearch.DefaultCellStyle.BackColor = SystemColors.ControlDark;
            dgvSearch.Enabled = false;
            Utils._gridChanged = false;
        }

        private void dgvSearch_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Utils._gridChanged = true;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                //BeginInvoke it's important to prevent that results in 
                //the active cell being changed while the DataGridView is still using it
                for (int row = 0; row < dgvSearch.Rows.Count; row++)
                {
                    if (dgvSearch.Rows[row].Cells[0].Value != null &&
                               row != e.RowIndex &&
                               dgvSearch.Rows[row].Cells[0].Value.Equals(dgvSearch.Rows[e.RowIndex].Cells[0].Value))
                    {

                        string aplicacao = (tvApps.SelectedNode.Level == 2) ?
                            tvApps.SelectedNode.Parent.Text : tvApps.SelectedNode.Text;

                        MessageBox.Show(
                            string.Format("The key '{0}' already exists in app '{1}'",
                                dgvSearch.Rows[e.RowIndex].Cells[e.ColumnIndex].Value,
                                aplicacao)
                            );

                        //delete current row
                        dgvSearch.CurrentCell = dgvSearch[e.ColumnIndex, e.RowIndex];
                        dgvSearch.BeginEdit(true);
                        return;

                    }
                }
            }));

        }

        private void tvApps_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (Utils._gridChanged)
            {
                if (!showDiscard)
                {
                    DialogResult dialogResult = MessageBox.Show("Discard changes?",
                        "Unsaved data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (dialogResult == DialogResult.No)
                    {
                        e.Cancel = true;
                        showDiscard = true;
                    }
                    else
                        showDiscard = false;
                }
                else 
                {
                    e.Cancel = true;
                    showDiscard = false;
                }

            }
        }

        private void copyNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string randomString = copyAppName(tvApps.SelectedNode.Text);

            if (UpdateSSOApplication(randomString, _newapplication))
            {
                MessageBox.Show(string.Format("The new application {0} was saved", randomString),
                    "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                //update TreeView:
                Utils.Search(tvApps, txbSearch.Text, dgvSearch);
            }
        }

        //se carregar num nó da Treeview, apresenta diferentes tipos de contextmenu
        private void tvApps_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {         
                // Ensure selected node is selected.
                tvApps.BeginUpdate();
                this.tvApps.SelectedNode = e.Node;
                try
                {
                     if (e.Node.Level == 0)
                        {
                            if (e.Button == MouseButtons.Right)
                                e.Node.ContextMenuStrip = ctmTreeParent;
                        }
                        else if (e.Node.Level == 1)
                        {
                            if (e.Button == MouseButtons.Right)
                                e.Node.ContextMenuStrip = ctmTree;
                        }
                    }
                
                finally
                {
                    this.tvApps.EndUpdate();
                }
            
        }

        private void exportToXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.fbdSavePath.FileName = tvApps.SelectedNode.Text;
            if (fbdSavePath.ShowDialog() == DialogResult.OK)
            {
                string path = fbdSavePath.FileName;

                bool ok;
                string password_aux;
                Password.getPassword(out ok, out password_aux);

                if (ok) fbdSavePath.Dispose();

                if (ok && Utils.ExportEncryptedAppToXML(path, tvApps.SelectedNode.Text, dgvSearch, password_aux))
                {
                    MessageBox.Show("Configuration saved successfully.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void importAppFromXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdConfigFile.ShowDialog() == DialogResult.OK)
            {
                string path = ofdConfigFile.FileName;

                bool ok;
                string password_aux;
                Password.getPassword(out ok, out password_aux);

                if (ok) ofdConfigFile.Dispose();

                if (ok && Utils.ImportAppFromXML(path, password_aux))
                {
                    MessageBox.Show("Configuration saved successfully.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Utils.Search(tvApps, txbSearch.Text, dgvSearch);
                }
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Search(tvApps, txbSearch.Text, dgvSearch);
        }

        private static int lastAppNumberAdded(string appName)
        {
            List<string> al = SSOConfigManager.GetApplications();

            int lastAppNumber = -1;

            foreach (string s in al)
            {
                if (s.Equals(appName)) lastAppNumber = 0;
                if (s.Contains(appName + "("))
                {

                    string regex = Regex.Match(s, @"\([0-9]+\)(?!.*(\([0-9]+\)))").Value;
                    if (regex == "") continue;


                    int splitNumber = Int32.Parse(regex.Split('(')[1].Split(')')[0]);
                    if (splitNumber > lastAppNumber) lastAppNumber = splitNumber;

                }
            }

            return lastAppNumber;
        }

        private static string copyAppName(string appName)
        {
            int splitNumber = lastAppNumberAdded(appName);

            if (splitNumber == -1) return appName;

            return appName + "(" + ++splitNumber + ")";

        }

        private void AddBlankApp()
        {

            string appName = copyAppName(_newapplication);
            TreeNode node = new TreeNode(appName);
            TreeNode rootNode = tvApps.TopNode;
            rootNode.Nodes.Add(node);
            tvApps.SelectedNode = node;
            tvApps.LabelEdit = true;
            tvApps.SelectedNode.BeginEdit();
            SSOConfigManager.CreateConfigStoreApplication(appName, "", ContactInfo,
                    AuserAcct, AdminAcct, new SSOPropBag(), new ArrayList());
            InicSearchGrid();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            AddBlankApp();
        }

        private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(DataGridViewRow row in this.dgvSearch.SelectedRows)
            {
                if (!this.dgvSearch.Rows[this.dgvSearch.SelectedRows[0].Index].IsNewRow)
                {
                    Utils.RemoveKeyValueFromSSOApp(this.dgvSearch.Rows[this.dgvSearch.SelectedRows[0].Index].Cells[0].Value.ToString(), tvApps.SelectedNode.Text);
                    this.dgvSearch.Rows.RemoveAt(this.dgvSearch.SelectedRows[0].Index);
                }
            }
        }

        private void restoreButton_Click(object sender, EventArgs e)
        {
            txbSearch.Text = string.Empty;
            Utils.Search(tvApps, txbSearch.Text, dgvSearch);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils._gridChanged = false;
            bool ok;
            Settings.updateSettings(out ok);

            if (ok)
            {
                txbSearch.Text = string.Empty;
                Utils.Search(tvApps, txbSearch.Text, dgvSearch);
            }
        }

        private void appManagerResized(Object sender, EventArgs e)
        {

            this.splitContainer1.Size = new Size(this.Size.Width - 42 , this.Size.Height - 192);

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }
    }
}