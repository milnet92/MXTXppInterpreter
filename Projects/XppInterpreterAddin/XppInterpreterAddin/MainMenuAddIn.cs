using Microsoft.Dynamics.Framework.Tools.Extensibility;
using Microsoft.Dynamics.Framework.Tools.MetaModel.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;

using System.Text;
using EnvDTE;
using EnvDTE80;

namespace XppInterpreterAddin
{
    /// <summary>
    /// TODO: Say a few words about what your AddIn is going to do
    /// </summary>
    [Export(typeof(IMainMenu))]
    public class MainMenuAddIn : MainMenuBase
    {
        #region Member variables
        private const string addinName = "XppInterpreterAddin";
        #endregion

        #region Properties
        /// <summary>
        /// Caption for the menu item. This is what users would see in the menu.
        /// </summary>
        public override string Caption
        {
            get
            {
                return AddinResources.MainMenuAddInCaption;
            }
        }

        /// <summary>
        /// Unique name of the add-in
        /// </summary>
        public override string Name
        {
            get
            {
                return MainMenuAddIn.addinName;
            }
        }

        #endregion

        #region Callbacks
        /// <summary>
        /// Called when user clicks on the add-in menu
        /// </summary>
        /// <param name="e">The context of the VS tools and metadata</param>
        public override void OnClick(AddinEventArgs e)
        {
            try
            {
                var environment = Microsoft.Dynamics.ApplicationPlatform.Environment.EnvironmentFactory.GetApplicationEnvironment();
                string baseUrl = environment.Infrastructure.HostUrl;
                string encodedSourceCode = GetSourceCodeContent();

                if (string.IsNullOrEmpty(encodedSourceCode))
                {
                    throw new Exception("No source code was specified.");
                }
                else
                {
                    System.Diagnostics.Process.Start($"{baseUrl}/?mi=MXTXppInterpreterRunner&sc={encodedSourceCode}");
                }
            }
            catch (Exception ex)
            {
                CoreUtility.HandleExceptionWithErrorMessage(ex);
            }
        }
        #endregion

        private string GetSourceCodeContent()
        {
            var dte = CoreUtility.ServiceProvider.GetService(typeof(DTE)) as DTE2;
            string encodedSourceCode = "";
            if (dte.ActiveDocument != null)
            {
                var selection = dte.ActiveDocument.Selection as TextSelection;

                if (selection != null)
                { 
                    string sourceCode = selection.Text;
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(sourceCode);
                    encodedSourceCode = System.Convert.ToBase64String(plainTextBytes);
                }
            }

            return encodedSourceCode;
        }
    }
}