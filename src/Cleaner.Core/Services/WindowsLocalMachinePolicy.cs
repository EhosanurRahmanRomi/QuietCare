using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
namespace Cleaner.Core.Services;
    // Official gpedit COM API: loads the local machine GPO, changes one allowlisted
    // value, saves Registry.pol and asks Windows to refresh its own policy.
    internal sealed class WindowsLocalMachinePolicy : IDisposable
    {
        readonly IGroupPolicyObject policy;
        readonly RegistryKey root;
        readonly Mutex? editGate;
        public WindowsLocalMachinePolicy(bool readOnly=false)
        {
            if(Thread.CurrentThread.GetApartmentState()!=ApartmentState.STA)throw new InvalidOperationException("Local policy must be edited on an STA thread.");
            if(!readOnly)
            {
                editGate=new Mutex(false,@"Global\QuietCare.LocalPolicyEdits");
                bool acquired;try{acquired=editGate.WaitOne(TimeSpan.FromSeconds(5));}catch(AbandonedMutexException){acquired=true;}
                if(!acquired){editGate.Dispose();throw new InvalidOperationException("Another local policy edit is running. Retry after it finishes.");}
            }
            IGroupPolicyObject? instance=null;
            try
            {
                policy=instance=(IGroupPolicyObject)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("EA502722-A23D-11D1-A7D3-0000F87571E3"),true)!)!;
                policy.OpenLocalMachineGPO(readOnly?3u:1u); // LOAD_REGISTRY and optional READ_ONLY
                policy.GetRegistryKey(2,out var handle); // GPO_SECTION_MACHINE
                root=RegistryKey.FromHandle(new SafeRegistryHandle(handle,true),RegistryView.Registry64);
            }
            catch{if(instance is not null)Marshal.FinalReleaseComObject(instance);editGate?.ReleaseMutex();editGate?.Dispose();throw;}
        }
        public int? Read(string path,string name)
        {
            using var key=root.OpenSubKey(path);
            object? value=key?.GetValue(name);
            if(value is null)return null;
            if(value is not int number||key!.GetValueKind(name)!=RegistryValueKind.DWord)throw new InvalidOperationException("The local policy has an unsupported type. Nothing was overwritten.");
            return number;
        }
        public void Set(string path,string name,int? value)
        {
            using(var key=root.CreateSubKey(path,true))
            {if(value.HasValue)key.SetValue(name,value.Value,RegistryValueKind.DWord);else key.DeleteValue(name,false);}
        }
        public void Commit()
        {
            var extension=new Guid("35378EAC-683F-11D2-A89A-00C04FBBCFA2");
            var editor=new Guid("50E07422-B626-46BB-A7EB-351B576585AF");
            policy.Save(true,true,ref extension,ref editor);
            if(!RefreshPolicyEx(true,1))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"The policy was saved but Windows could not refresh it.");
        }
        public void Dispose(){root.Dispose();Marshal.FinalReleaseComObject(policy);editGate?.ReleaseMutex();editGate?.Dispose();}
    [DllImport("userenv.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool RefreshPolicyEx([MarshalAs(UnmanagedType.Bool)]bool machine,uint options);
    // Method order follows Microsoft's GPEdit.h. Unused slots are retained for ABI correctness.
    [ComImport,Guid("EA502723-A23D-11D1-A7D3-0000F87571E3"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGroupPolicyObject
    {
        void New([MarshalAs(UnmanagedType.LPWStr)]string domain,[MarshalAs(UnmanagedType.LPWStr)]string display,uint flags);
        void OpenDSGPO([MarshalAs(UnmanagedType.LPWStr)]string path,uint flags);
        void OpenLocalMachineGPO(uint flags);
        void OpenRemoteMachineGPO([MarshalAs(UnmanagedType.LPWStr)]string name,uint flags);
        void Save([MarshalAs(UnmanagedType.Bool)]bool machine,[MarshalAs(UnmanagedType.Bool)]bool add,ref Guid extension,ref Guid editor);
        void Delete();
        void GetName([MarshalAs(UnmanagedType.LPWStr)]StringBuilder value,int max);
        void GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]StringBuilder value,int max);
        void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string value);
        void GetPath([MarshalAs(UnmanagedType.LPWStr)]StringBuilder value,int max);
        void GetDSPath(uint section,[MarshalAs(UnmanagedType.LPWStr)]StringBuilder value,int max);
        void GetFileSysPath(uint section,[MarshalAs(UnmanagedType.LPWStr)]StringBuilder value,int max);
        void GetRegistryKey(uint section,out IntPtr key);
    }
}
