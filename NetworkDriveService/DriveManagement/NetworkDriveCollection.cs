using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace QuasaroDRV.DriveManagement
{
    /// <summary>
    /// Manages multiple drives and concurrent access to them.
    /// All public Properties and Methods are thread-safe or explicitly declared as not thread-safe.
    /// </summary>
    public class NetworkDriveCollection
    {
        ILog log;
        
        List<NetworkDrive> drives;
        /// <summary>
        /// Returns a snapshot of the current drives collection.
        /// Drives might be removed after taking this snapshot. Always lock the drive before working with it to detect these changes.
        /// </summary>
        public NetworkDrive[] Drives
        {
            get
            {
                lock (this.drives)
                    return this.drives.ToArray();
            }
        }
        /// <summary>
        /// Gets the number of drives managed by this instance of NetworkDriveCollection.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this.drives)
                    return this.drives.Count;
            }
        }
        

        /// <summary>
        /// Loads all drives from the default drive file location (NetworkDrive.GetDefaultDrivesStoragePath).
        /// </summary>
        public NetworkDriveCollection()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            this.drives = new List<NetworkDrive>();
        }

        /// <summary>
        /// Clear the current drive collection and reload drives from the default drive location (NetworkDrive.GetDefaultDrivesStoragePath).
        /// </summary>
        public void LoadDrives()
        {
            lock (this.drives)
            {
                this.drives.Clear();

                JArray config = Helper.SafeReadConfig<JArray>(GetDefaultDrivesStoragePath(), GetDefaultDrivesBackupPath());

                if (config != null)
                {
                    bool requireSave;
                    this.drives.AddRange(NetworkDrive.FromArray(config, out requireSave));
                    if(requireSave)
                    {
                        // plaintext values have been found in the configuration => save again to encrypt them
                        SaveDrives();
                    }
                }
            }
        }

        /// <summary>
        /// Saves all drives to the default drive file location (NetworkDrive.GetDefaultDrivesStoragePath).
        /// </summary>
        public void SaveDrives()
        {
            lock (this.drives)
            {
                Helper.SafeWriteConfig(GetDefaultDrivesStoragePath(), GetDefaultDrivesBackupPath(), NetworkDrive.ToArray(this.drives));
            }
        }

        public static string GetDefaultDrivesBackupPath()
        {
            string appData = Branding.AppDataPath;
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);
            return Path.Combine(appData, "drives_backup");
        }
        public static string GetDefaultDrivesStoragePath()
        {
            string appData = Branding.AppDataPath;
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);
            return Path.Combine(appData, "drives");
        }
        public static bool DrivesConfigured()
        {
            return File.Exists(GetDefaultDrivesStoragePath());
        }


        /// <summary>
        /// Removes all drives from this collection.
        /// </summary>
        public void Clear()
        {
            lock (this.drives)
                this.drives.Clear();
        }

        /// <summary>
        /// Adds a new drive to this drive collection.
        /// </summary>
        /// <param name="drive">Drive to add.</param>
        public void AddDrive(NetworkDrive drive)
        {
            lock (this.drives)
                this.drives.Add(drive);
        }

        public void AddMany(IEnumerable<NetworkDrive> drives)
        {
            lock (this.drives)
                this.drives.AddRange(drives);
        }

        /// <summary>
        /// Removes a drive from the list.
        /// Throws a DriveLockedException when the drive is currently locked.
        /// </summary>
        /// <param name="drive">Drive to remove.</param>
        /// <param name="waitForUnlock">When set to true, this method will wait for a lock to be removed instead of throwing an exception.</param>
        public void RemoveDrive(NetworkDrive drive, bool waitForUnlock = false)
        {
            lock (this.drives)
                if (this.drives.Contains(drive))
                    this.drives.Remove(drive);
        }

        /// <summary>
        /// Removes a drive from the list and releases the lock.
        /// </summary>
        /// <param name="drive">Drive to remove.</param>
        public void RemoveAndUnlock(NetworkDrive drive)
        {
            lock (this.drives)
                if (this.drives.Contains(drive))
                    this.drives.Remove(drive);
        }
        

        /// <summary>
        /// Returns whether one or many of the given drives are connected.
        /// Thsi method is not thread-safe.
        /// </summary>
        /// <param name="drives">Drives to check.</param>
        /// <returns></returns>
        public static bool HasConnectedDrives(IEnumerable<NetworkDrive> drives)
        {
            foreach (NetworkDrive drive in drives)
                if (drive.IsConnectedCached)
                    return true;
            return false;
        }

        /// <summary>
        /// Returns whether one or many of the drives managed by this instance of NetworkDriveCollection are connected.
        /// </summary>
        /// <returns></returns>
        public bool HasConnectedDrives()
        {
            lock (this.drives)
            {
                return HasConnectedDrives(this.drives);
            }
        }

        /// <summary>
        /// Returns true, when this instance of NetworkDriveCollection contains a drive with the same remote address.
        /// Ignores ignoreDrive in the comparison.
        /// </summary>
        /// <param name="remoteAddress">Remote address to search for.</param>
        /// <param name="ignoreDrive">Drive to ignore in the comparison.</param>
        /// <returns></returns>
        public bool RemoteAddressExists(string remoteAddress, NetworkDrive ignoreDrive = null)
        {
            lock (this.drives)
            {
                foreach (NetworkDrive drive in this.drives)
                {
                    if (drive != ignoreDrive)
                    {
                        if (drive.RemoteAddress.Equals(remoteAddress, StringComparison.InvariantCultureIgnoreCase))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true, when a similar network drive is already managed by this instance of NetworkDriveCollection.
        /// The comparison is based on the RemoteAddres-property.
        /// </summary>
        /// <param name="checkDrive">Drive to compare.</param>
        /// <param name="ignoreDrive">Drive to ignore in the comparison.</param>
        /// <returns></returns>
        public bool RemoteAddressExists(NetworkDrive checkDrive, NetworkDrive ignoreDrive = null)
        {
            return RemoteAddressExists(checkDrive.RemoteAddress, ignoreDrive);
        }


        /// <summary>
        /// Returns true, when the instance of NetworkDrive is contained in the drive list.
        /// </summary>
        /// <param name="drive">Drive object to compare.</param>
        /// <returns></returns>
        public bool ContainsNetworkDrive(NetworkDrive drive)
        {
            lock (this.drives)
                return this.drives.Contains(drive);
        }


        /// <summary>
        /// Performs the action in an thread-safe environment.
        /// Do not wait for other threads working on this NetworkDriveCollection to prevent deadlocks.
        /// </summary>
        /// <param name="action">Action to perform</param>
        public void PerformAction(Action action)
        {
            lock (this.drives)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // find innermost exception
                    while (ex.InnerException != null)
                        ex = ex.InnerException;
                    this.log.Error("Unhandled Exception in " + action.Target.GetType().Name + "." + action.Method.Name + ": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                }
            }
        }


        /// <summary>
        /// Returns true, when the given drive letter is neither used by another windows drive nor a managed network drive.
        /// </summary>
        /// <param name="letter">Drive letter to check.</param>
        /// <param name="ignore">NetworkDrive from this collection to ignore. Can be null.</param>
        /// <returns></returns>
        public bool IsDriveLetterAvailable(char letter, NetworkDrive ignore = null)
        {
            // all letters from A to Z are allowed (upper case only)
            letter = char.ToUpper(letter);
            if (letter < 'A' || letter > 'Z')
                return false;

            // check for existing drives in the system
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                char driveLetter = char.ToUpper(drive.Name[0]);
                if (ignore == null || letter != ignore.LocalDriveLetter[0])
                    if (driveLetter == letter)
                        return false;
            }

            lock (this.drives)
            {
                // check managed network drives (might contain some currently not connected drives)
                foreach (NetworkDrive drive in this.drives)
                {
                    char driveLetter = char.ToUpper(drive.LocalDriveLetter[0]);
                    if (ignore == null || letter != ignore.LocalDriveLetter[0])
                        if (driveLetter == letter)
                            return false;
                }
            }

            // letter can be used
            return true;
        }

        /// <summary>
        /// Returns a list of available drive letters (See IsDriveLetterAvailable).
        /// </summary>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public char[] GetUnusedDriveLetters(NetworkDrive ignore = null)
        {
            // prepare array with all drive letters
            List<char> driveLetters = new List<char>(26);
            for (char c = 'A'; c <= 'Z'; c++)
                driveLetters.Add(c);

            // remove letters which are currently beeing used in the sytem (hard drives, optical drives, etc...)
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                char letter = char.ToUpper(drive.Name[0]);
                if (ignore == null || letter != ignore.LocalDriveLetter[0])
                    driveLetters.Remove(letter);
            }

            lock (this.drives)
            {
                // remove other known network drives
                foreach (NetworkDrive drive in this.drives)
                {
                    char letter = char.ToUpper(drive.LocalDriveLetter[0]);
                    if (ignore == null || letter != ignore.LocalDriveLetter[0])
                        driveLetters.Remove(letter);
                }
            }

            // return remaining letters
            return driveLetters.ToArray();
        }
    }
}
