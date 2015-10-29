using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace System.Windows.Forms
{
    public class OpenFolderDialog
    {
        private class WindowWrapper : IWin32Window
        {
            public IntPtr Handle { get; private set; }

            public WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }
        }

        private OpenFileDialog openFileDialog = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public OpenFolderDialog()
        {
            openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Folders|\n";
            openFileDialog.AddExtension = false;
            openFileDialog.CheckFileExists = false;
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Multiselect = false;
        }

        /// <summary>
        /// Gets/Sets the initial folder to be selected. A null value selects the current directory.
        /// </summary>
        public string InitialDirectory
        {
            get { return openFileDialog.InitialDirectory; }
            set { openFileDialog.InitialDirectory = value == null || value.Length == 0 ? Environment.CurrentDirectory : value; }
        }

        /// <summary>
        /// Gets/Sets the title to show in the dialog
        /// </summary>
        public string Title
        {
            get { return openFileDialog.Title; }
            set { openFileDialog.Title = value == null ? "Select a folder" : value; }
        }

        /// <summary>
        /// Gets the selected folder
        /// </summary>
        public string FileName
        {
            get { return openFileDialog.FileName; }
        }

        /// <summary>
        /// Shows the dialog
        /// </summary>
        /// <returns>True if the user presses OK else false</returns>
        public bool ShowDialog()
        {
            return ShowDialog(IntPtr.Zero);
        }

        /// <summary>
        /// Shows the dialog
        /// </summary>
        /// <param name="owner">Handle of the control to be parent</param>
        /// <returns>True if the user presses OK else false</returns>
        public bool ShowDialog(IntPtr owner)
        {
            bool result = false;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                Assembly assembly = Assembly.GetAssembly(typeof(OpenFileDialog));

                Type openFileDialogType = typeof(OpenFileDialog);
                Type nativeFileDialogType = assembly.GetType("System.Windows.Forms.FileDialogNative+IFileDialog", true, true);
                Type fosType = assembly.GetType("System.Windows.Forms.FileDialogNative+FOS");
                Type dialogEventsType = assembly.GetType("System.Windows.Forms.FileDialog+VistaDialogEvents");

                MethodInfo createVistaDialogMethod = openFileDialogType.GetMethod("CreateVistaDialog", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo onBeforeVistaDialogMethod = openFileDialogType.GetMethod("OnBeforeVistaDialog", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getOptionsMethod = nativeFileDialogType.GetMethod("GetOptions");
                MethodInfo setOptionsMethod = nativeFileDialogType.GetMethod("SetOptions");
                MethodInfo adviseMethod = nativeFileDialogType.GetMethod("Advise");
                MethodInfo unadviseMethod = nativeFileDialogType.GetMethod("Unadvise");
                MethodInfo showMethod = nativeFileDialogType.GetMethod("Show");

                ConstructorInfo dialogEventsCtor = dialogEventsType.GetConstructors().FirstOrDefault();

                object nativeFileDialog = createVistaDialogMethod.Invoke(openFileDialog, new object[0]);
                onBeforeVistaDialogMethod.Invoke(openFileDialog, new object[] { nativeFileDialog });

                object[] parameters = new object[1];
                getOptionsMethod.Invoke(nativeFileDialog, parameters);
                uint options = (uint)parameters[0];
                options |= (uint)Enum.Parse(fosType, "FOS_PICKFOLDERS");
                setOptionsMethod.Invoke(nativeFileDialog, new object[] { options });

                object dialogEvents = dialogEventsCtor.Invoke(new object[] { openFileDialog });
                uint num = 0;
                parameters = new object[] { dialogEvents, num };
                adviseMethod.Invoke(nativeFileDialog, parameters);
                num = (uint)parameters[1];

                try
                {
                    int dialogResult = (int)showMethod.Invoke(nativeFileDialog, new object[] { owner });
                    result = dialogResult == 0;
                }
                finally
                {
                    unadviseMethod.Invoke(nativeFileDialog, new object[] { num });
                    GC.KeepAlive(dialogEvents);
                }
            }
            else
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.Description = Title;
                folderDialog.SelectedPath = InitialDirectory;
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog(new WindowWrapper(owner)) != DialogResult.OK)
                    return false;

                openFileDialog.FileName = folderDialog.SelectedPath;
                result = true;
            }

            return result;
        }
    }
}