using Microsoft.Deployment.WindowsInstaller;
using System;

namespace InstallerCustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult GetInstallerFileName(Session session)
        {
            session.Log("Begin GetInstallerFileName");

            string msiPath = null;

            try
            {
                if (string.IsNullOrEmpty(msiPath))
                    msiPath = session["OriginalDatabase"];
            }
            catch (Exception ex)
            {
                session.Log("-> " + ex.GetType().Name + ": " + ex.Message);
            }

            try
            {
                if (string.IsNullOrEmpty(msiPath))
                    msiPath = session.Database.FilePath;
            }
            catch (Exception ex)
            {
                session.Log("-> " + ex.GetType().Name + ": " + ex.Message);
            }

            try
            {
                if (!string.IsNullOrEmpty(msiPath))
                {
                    session.Log("-> Installer file name is \"" + msiPath + "\"");
                    session["InstallerFileName"] = msiPath;
                }
                else
                    session.Log("-> Installer file name not found");
            }
            catch (Exception ex)
            {
                session.Log("-> " + ex.GetType().Name + ": " + ex.Message);
            }

            return ActionResult.Success;
        }
    }
}
