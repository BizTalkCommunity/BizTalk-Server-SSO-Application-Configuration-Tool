using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Microsoft.EnterpriseSingleSignOn.Interop;

namespace BizTalk.Tools.SSOApplicationConfiguration
{
    public static class SSOConfigManager
    {
        //don't actually need a GUID value
        private static string idenifierGUID = "ConfigProperties";

        /// <summary>
        /// Creates a new SSO ConfigStore application.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="description"></param>
        /// <param name="uAccountName"></param>
        /// <param name="adminAccountName"></param>
        /// <param name="propertiesBag"></param>
        /// <param name="maskArray"></param>
        public static void CreateConfigStoreApplication(string appName, string description, string contactInfo, string uAccountName, string adminAccountName, SSOPropBag propertiesBag, ArrayList maskArray)
        {
            int appFlags = 0;

            //bitwise operation for flags
            appFlags |= SSOFlag.SSO_FLAG_APP_CONFIG_STORE;
            appFlags |= SSOFlag.SSO_FLAG_SSO_WINDOWS_TO_EXTERNAL;
            appFlags |= SSOFlag.SSO_FLAG_APP_ALLOW_LOCAL;

            ISSOAdmin ssoAdmin = new ISSOAdmin();
            
            //create app
            if (propertiesBag.PropertyCount > 0)
                ssoAdmin.CreateApplication(appName, description, contactInfo, uAccountName, adminAccountName, appFlags, propertiesBag.PropertyCount);
            else
            {
                ssoAdmin.CreateApplication(appName, description, contactInfo, uAccountName, adminAccountName, appFlags, 1);
            }
                

            //create property fields
            int counter = 0;

            //create dummy field in first slot
            ssoAdmin.CreateFieldInfo(appName, "dummy", 0);
            //create real fields
            foreach (DictionaryEntry de in propertiesBag.properties)
            {
                string propName = de.Key.ToString();
                int fieldFlags = 0;
                fieldFlags |= Convert.ToInt32(maskArray[counter]);

                //create property
                ssoAdmin.CreateFieldInfo(appName, propName, fieldFlags);

                counter++;
            }

            //enable application
            ssoAdmin.UpdateApplication(appName, null, null, null, null, SSOFlag.SSO_FLAG_ENABLED, SSOFlag.SSO_FLAG_ENABLED);

        }



        /// <summary>
        /// Set values for application fields
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="propertyBag"></param>
        public static void SetConfigProperties(string appName, SSOPropBag propertyBag)
        {
            ISSOConfigStore configStore = new ISSOConfigStore();

            configStore.SetConfigInfo(appName, idenifierGUID, propertyBag);
 
        }

        /// <summary>
        /// Retrieve dictionary of field/value pairs
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="description"></param>
        /// <param name="contactInfo"></param>
        /// <param name="appUserAcct"></param>
        /// <param name="appAdminAcct"></param>
        /// <returns></returns>
        public static HybridDictionary GetConfigProperties(string appName, out string description, out string contactInfo, out string appUserAcct, out string appAdminAcct)
        {
            int flags;
            int count;

            //get config info
            ISSOAdmin ssoAdmin = new ISSOAdmin();
            ssoAdmin.GetApplicationInfo(appName, out description, out contactInfo, out appUserAcct, out appAdminAcct, out flags, out count);
            
            //get properties
            ISSOConfigStore configStore = new ISSOConfigStore();
            SSOPropBag propertiesBag = new SSOPropBag();

            try
            {
                configStore.GetConfigInfo(appName, idenifierGUID, SSOFlag.SSO_FLAG_RUNTIME, propertiesBag);
            }
            catch (COMException e)
            {
                if (!e.Message.StartsWith("The application is currently disabled."))
                    throw;
            }

            return propertiesBag.properties;
        }

        /// <summary>
        /// Remove the application
        /// </summary>
        /// <param name="appName"></param>
        public static void DeleteApplication(string appName)
        {
            ISSOAdmin ssoAdmin = new ISSOAdmin();

            ssoAdmin.DeleteApplication(appName);
        }

        /// <summary>
        /// Returns a list of applications in the SSO store
        /// </summary>
        /// <returns>Application names</returns>
        public static List<string> GetApplications()
        {
            //get the app list:
            string[] applications = null;
            string[] descs;
            string[] contacts = null;

            try
            {
                var mapper = new ISSOMapper();

                AffiliateApplicationType appTypes = AffiliateApplicationType.ConfigStore;
                appTypes |= AffiliateApplicationType.All;

                IPropertyBag propBag = (IPropertyBag)mapper;

                uint appFilterFlagMask = SSOFlag.SSO_FLAG_APP_FILTER_BY_TYPE;
                uint appFilterFlags = (uint)appTypes;

                object appFilterFlagsObj = (object)appFilterFlags;
                object appFilterFlagMaskObj = (object)appFilterFlagMask;

                propBag.Write("AppFilterFlags", ref appFilterFlagsObj);
                propBag.Write("AppFilterFlagMask", ref appFilterFlagMaskObj);                
                
                mapper.GetApplications(out applications, out descs, out contacts);
            }
            catch (COMException comEx)
            {
                HandleCOMException(comEx, 0);
            }

            List<string> apps = new List<string>();

            if (contacts != null)
            {

                for (int i = 0; i < applications.Length; i++)
                {
                    if (string.Equals(contacts[i], ApplicationManager.ContactInfo))
                        apps.Add(applications[i]);
                }
            }

            return apps;
        }

        private static void HandleCOMException(COMException comEx, int ignoreErrorCode)
        {
            //if this error code is OK, ignore it:
            if (comEx.ErrorCode != ignoreErrorCode)
            {
                throw new ApplicationException(string.Format("SSO error - code: {0}, message: {1}", comEx.ErrorCode, comEx.Message), comEx);

            }
        }

        public static bool SearchKeys(HybridDictionary dict, string key)
        {
            foreach (var k in dict.Keys)
            {
                if (k.ToString().ToUpper().Contains(key.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool SearchValues(HybridDictionary dict, string value)
        {
            foreach (var v in dict.Values)
            {
                if (v.ToString().ToUpper().Contains(value.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }
    }


    public class SSOPropBag : IPropertyBag
    {
        internal HybridDictionary properties;

        public SSOPropBag()
        {
            properties = new HybridDictionary(true);
        }

        public SSOPropBag(HybridDictionary d)
        {
            properties = d;
        }

        #region IPropertyBag Members

        public void Read(string propName, out object ptrVar, int errorLog)
        {
            ptrVar = properties[propName];
        }

        public void Write(string propName, ref object ptrVar)
        {
            properties.Add(propName, ptrVar);
        }

        public int PropertyCount
        {
            get
            {
                return properties.Count;
            }
        }

        #endregion
    }

}
