using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BizTalk.Tools.SSOStorage
{
    public class Utils
    {
        public static bool _gridChanged = false;

        public static bool ExportEncryptedAppToXML(string tempFileName, string appName, DataGridView grid, string password)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter writer = new XmlTextWriter(sw);

            try
            {
                //start document
                writer.WriteComment("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                //elements
                writer.WriteStartElement("SSOApplicationExport");
                writer.WriteStartElement("applicationData");

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Cells[0].Value != null)
                    {
                        writer.WriteStartElement("add");
                        writer.WriteAttributeString("key", row.Cells[0].Value.ToString());
                        writer.WriteAttributeString("value", row.Cells[1].FormattedValue.ToString());
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
                //close root
                writer.WriteEndElement();
                writer.Flush();
                writer.Close();

                string encrypted = Encryption.EncryptGeral(sw.ToString(), password);
                File.WriteAllText(tempFileName, encrypted);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot export application to XML  file", "Error");
                return false;
            }
        }      

        public static bool ImportAppFromXML(string path, string password)
        {
            var settings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

            XmlDocument configDoc = new XmlDocument();
            try
            {
                configDoc.LoadXml(Encryption.DecryptGeral(File.ReadAllText(@path), password));

                string appName = Path.GetFileNameWithoutExtension(@path);

                //grab fields
                XmlNodeList fields = configDoc.SelectNodes("//applicationData/add");
                ArrayList maskArray = new ArrayList();

                try
                {
                    HybridDictionary props = new HybridDictionary();

                    foreach (XmlNode field in fields)
                    {
                        props.Add(field.Attributes["key"].InnerText, field.Attributes["value"].InnerText);
                        maskArray.Add(0);
                    }


                    SSOPropBag propertiesBag = new SSOPropBag(props); ;

                    CreateAndEnableAppInSSO(appName, string.Empty, settings.Get("ContactInfo"),
                        settings.Get("AppUserAcct"), settings.Get("AppAdminAcct"), propertiesBag, maskArray);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Cannot create app '{0}'\n\n{1}", appName, ex.Message.ToString()),
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;

            }
            catch (XmlException)
            {
                MessageBox.Show("Password incorrect!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

        }

        /// <summary>
        /// Create application in SSO Store from app.config
        /// </summary>
        /// <param name="appName">Application to create/enable</param>
        public static bool CreateAppFromTemplate(string appName)
        {
            var settings = ConfigurationManager.GetSection("settings") as NameValueCollection;
            var templateOfKeys = ConfigurationManager.GetSection("TemplateOfKeys") as NameValueCollection;
            ArrayList maskArray = new ArrayList();

            try
            {
                HybridDictionary props = new HybridDictionary();

                foreach (var item in templateOfKeys)
                {
                    string value = templateOfKeys.GetValues(item.ToString()).FirstOrDefault();
                    props.Add(item.ToString(), value);
                    maskArray.Add(0);
                }

                SSOPropBag propertiesBag = new SSOPropBag(props); ;

                CreateAndEnableAppInSSO(appName, string.Empty, settings.Get("ContactInfo"),
                    settings.Get("AppUserAcct"), settings.Get("AppAdminAcct"), propertiesBag, maskArray);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot create app '{0}'\n\n{1}", appName, ex.Message.ToString()),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create and Enable application in SSO Store
        /// </summary>
        /// <param name="appName">Application to create/enable</param>
        /// <param name="description"></param>
        /// <param name="ContactInfo"></param>
        /// <param name="AppUserAcct"></param>
        /// <param name="AppAdminAcct"></param>
        /// <param name="propertiesBag"></param>
        /// <param name="maskArray"></param>
        public static void CreateAndEnableAppInSSO(string appName, string description, string ContactInfo,
            string AppUserAcct, string AppAdminAcct, SSOPropBag propertiesBag, ArrayList maskArray)
        {
            //create and enable application
            SSOConfigManager.CreateConfigStoreApplication(appName, description, ContactInfo,
                AppUserAcct, AppAdminAcct, propertiesBag, maskArray);
            //set default configuration field values
            SSOConfigManager.SetConfigProperties(appName, propertiesBag);
        }


        public static void Search(TreeView tvApps, string searchText, DataGridView dgvSearch)
        {
            tvApps.Nodes.Clear();
            TreeNode rootNode = new TreeNode("Applications");
            tvApps.Nodes.Add(rootNode);

            foreach (var application in SSOConfigManager.GetApplications())
            {
                string appUserAcct, appAdminAcct, description, contactInfo;
                HybridDictionary props = SSOConfigManager.GetConfigProperties(application, out description, out contactInfo,
                    out appUserAcct, out appAdminAcct);
                // search string in all keys and values
                if (!string.IsNullOrWhiteSpace(searchText) &&
                    //!application.Equals(searchText, StringComparison.InvariantCultureIgnoreCase) &&
                    !ContainsIgnoreCase(application,searchText, StringComparison.OrdinalIgnoreCase) &&
                    !SSOConfigManager.SearchKeys(props, searchText) &&
                    !SSOConfigManager.SearchValues(props, searchText)) continue;
                var node = new TreeNode(application) { ToolTipText = description };
                
                // add node if found
                rootNode.Nodes.Add(node);
            }

            TreeNodeCollection nodes = rootNode.Nodes;
            if (nodes.Count > 0)
            {
                // Select the root node
                tvApps.SelectedNode = nodes[0];
                tvApps.SelectedNode.Expand();
                LoadGrid(nodes[0].Text, dgvSearch);
                tvApps.Focus();
            }
        }

        public static bool ContainsIgnoreCase(string source, string toCheck, StringComparison comp)
         {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
          }

        public static void LoadGrid(string appName, DataGridView dgvSearch)
        {
            dgvSearch.Enabled = true;
            dgvSearch.Rows.Clear();
            dgvSearch.DefaultCellStyle.BackColor = SystemColors.Window;
            _gridChanged = false;

            string appUserAcct, appAdminAcct, description, contactInfo;


            HybridDictionary props = SSOConfigManager.GetConfigProperties(appName, out description, out contactInfo,
        out appUserAcct, out appAdminAcct);

            foreach (DictionaryEntry appProp in props)
            {
                //// for level 2
                //if (prefix != null && !appProp.Key.ToString().StartsWith(prefix + ".")) continue;

                //if (string.IsNullOrWhiteSpace(search) ||
                //    appProp.Key.ToString().ToLower().Contains(search.ToLower()) ||
                //    appProp.Value.ToString().ToLower().Contains(search.ToLower()))
                //{
                    int index = dgvSearch.Rows.Add();
                    DataGridViewRow dgvr = dgvSearch.Rows[index];

                    dgvr.Cells[0].Value = appProp.Key.ToString();
                    dgvr.Cells[1].Value = appProp.Value.ToString();
                    dgvr.Cells[2].Value = 0;
                //}
            }
            dgvSearch.Sort(dgvSearch.Columns[0], ListSortDirection.Ascending);
        }

        /// <summary>
        /// Remove nodes/keys (level 2) from treeview
        /// </summary>
        /// <param name="KeyValueToBeRemoved">Node/key to remove</param>
        /// <param name="parentNode">Parent of node/key to remove</param>
        public static bool RemoveKeyValueFromSSOApp(string KeyValueToBeRemoved, string parentNode)
        {
            string appUserAcct = string.Empty, appAdminAcct = string.Empty,
                    contactInfo = string.Empty, description = string.Empty;
            List<String> keysToRemove = new List<string>();
            ArrayList maskArray = new ArrayList();

            try
            {
                //Gets the configuration information from the configuration store about parentNode
                HybridDictionary props = SSOConfigManager.GetConfigProperties(parentNode, out description,
                        out contactInfo, out appUserAcct, out appAdminAcct);

                //search in 'props' if exists some key/prefix that contains 'KeyValueToBeRemoved'
                //if exists, store it in a list
                foreach (var item in props.Keys)
                {
                    if (item.ToString().Contains(KeyValueToBeRemoved))
                    {
                        keysToRemove.Add(item.ToString());
                        //in this foreach i cannot remove the key directly from 'props'
                    }
                    maskArray.Add(0);
                }

                //remove keys from 'props':
                foreach (string key in keysToRemove)
                {
                    props.Remove(key);
                    maskArray.Remove(0);
                }

                SSOConfigManager.DeleteApplication(parentNode);
                SSOPropBag propertiesBag = new SSOPropBag(props);
                CreateAndEnableAppInSSO(parentNode, string.Empty, contactInfo, appUserAcct, appAdminAcct, propertiesBag, maskArray);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Cannot delete key(s) '{0}'\n\n{1}", KeyValueToBeRemoved, ex.Message.ToString()),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Rename nodes/keys (level 2) from treeview
        /// </summary>
        /// <param name="node">Node already renamed</param>
        /// <param name="oldNode">Old node name</param>
        /// <param name="parentSelectedNode">Parent of node renamed</param>
        public static bool RenameKeyValueFromSSOApp(TreeNode node, string oldNode, string parentSelectedNode)
        {
            ArrayList maskArray = new ArrayList();
            string appUserAcct = string.Empty, appAdminAcct = string.Empty,
                    contactInfo = string.Empty, description = string.Empty;

            try
            {
                HybridDictionary props = SSOConfigManager.GetConfigProperties(parentSelectedNode, out description,
                        out contactInfo, out appUserAcct, out appAdminAcct);
                HybridDictionary propsTemp = new HybridDictionary();

                //save nodes to rename
                foreach (DictionaryEntry item in props)
                {
                    if (item.Key.ToString().Contains(oldNode))
                    {
                        propsTemp.Add(item.Key.ToString().Replace(oldNode, node.Text.ToString()), item.Value.ToString());
                    }
                    maskArray.Add(0);
                }

                Utils.RemoveKeyValueFromSSOApp(oldNode, parentSelectedNode);

                props = SSOConfigManager.GetConfigProperties(parentSelectedNode, out description,
                        out contactInfo, out appUserAcct, out appAdminAcct);

                foreach (DictionaryEntry item in propsTemp)
                {
                    props.Add(item.Key.ToString(), item.Value.ToString());
                }

                SSOConfigManager.DeleteApplication(parentSelectedNode);
                SSOPropBag propertiesBag = new SSOPropBag(props);
                Utils.CreateAndEnableAppInSSO(parentSelectedNode, string.Empty, contactInfo,
                    appUserAcct, appAdminAcct, propertiesBag, maskArray);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Cannot rename key(s) '{0}' to '{1}' \n\n{2}", oldNode, node.Text.ToString(), ex.Message.ToString()),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

        }

        public static class Encryption
        {
            public static string DecryptGeral(string toDecrypt, string key)
            {
                byte[] inputBuffer = Convert.FromBase64String(toDecrypt);
                byte[] buffer = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(key));
                TripleDESCryptoServiceProvider provider2 = new TripleDESCryptoServiceProvider
                {
                    Key = buffer,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                };
                byte[] bytes = provider2.CreateDecryptor().TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                return Encoding.UTF8.GetString(bytes);
            }

            public static string EncryptGeral(string toEncrypt, string key)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(toEncrypt);
                byte[] buffer = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(key));
                TripleDESCryptoServiceProvider provider2 = new TripleDESCryptoServiceProvider
                {
                    Key = buffer,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                };
                byte[] inArray = provider2.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
                return Convert.ToBase64String(inArray, 0, inArray.Length);
            }   
        }
    }
}