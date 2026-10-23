using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuietCare {
public sealed class AudioActivity {
    public bool Verified;public readonly HashSet<int> ActiveProcesses=new HashSet<int>();
    // Read session state and process IDs only. No audio capture, playback or device changes.
    public static AudioActivity Read(){
        var result=new AudioActivity();object enumerator=null;AudioDevices devices=null;
        try {
            enumerator=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")));
            ((AudioEndpoints)enumerator).Enumerate(2,1,out devices);uint count;devices.Count(out count);
            for(uint i=0;i<count;i++) {
                AudioDevice device=null;object manager=null;AudioSessions sessions=null;
                try {
                    devices.Item(i,out device);Guid iid=new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");device.Activate(ref iid,23,IntPtr.Zero,out manager);
                    ((AudioManager)manager).Sessions(out sessions);int length;sessions.Count(out length);
                    for(int n=0;n<length;n++) {
                        SessionBase session=null;
                        try{sessions.Item(n,out session);int state;session.State(out state);if(state!=1)continue;uint pid;((SessionDetails)session).ProcessId(out pid);if(pid!=0)result.ActiveProcesses.Add((int)pid);}
                        finally{Release(session);}
                    }
                }finally{Release(sessions);Release(manager);Release(device);}
            }
            result.Verified=true;
        }catch(COMException){result.Verified=false;}catch(InvalidCastException){result.Verified=false;}catch(UnauthorizedAccessException){result.Verified=false;}
        finally{Release(devices);Release(enumerator);}return result;
    }
    static void Release(object value){if(value!=null && Marshal.IsComObject(value))Marshal.FinalReleaseComObject(value);}
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface AudioEndpoints {
        void Enumerate(int flow,uint state,out AudioDevices devices);
    }
    [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface AudioDevices {
        void Count(out uint count);void Item(uint index,out AudioDevice device);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface AudioDevice {
        void Activate(ref Guid iid,uint context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)]out object instance);
    }
    [ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface AudioManager {
        void SessionControl(ref Guid session,uint flags,out IntPtr value);void SimpleVolume(ref Guid session,uint flags,out IntPtr value);void Sessions(out AudioSessions sessions);
    }
    [ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface AudioSessions {
        void Count(out int count);void Item(int index,out SessionBase control);
    }
    [ComImport,Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface SessionBase {
        void State(out int state);
    }
    [ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface SessionDetails {
        void State(out int state);void DisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);void SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,ref Guid context);
        void IconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);void SetIconPath([MarshalAs(UnmanagedType.LPWStr)]string path,ref Guid context);
        void Group(out Guid group);void SetGroup(ref Guid group,ref Guid context);void Register(IntPtr callback);void Unregister(IntPtr callback);
        void SessionId([MarshalAs(UnmanagedType.LPWStr)]out string id);void InstanceId([MarshalAs(UnmanagedType.LPWStr)]out string id);void ProcessId(out uint pid);
    }
}
}
