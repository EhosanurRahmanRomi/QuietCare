using System;
class MemoryFixture {
    static int Main(){byte[] data=new byte[96*1024*1024];long expected=0;for(int i=0;i<data.Length;i++){data[i]=(byte)(i%251);expected+=data[i];}Console.WriteLine("READY");Console.ReadLine();long actual=0;for(int i=0;i<data.Length;i++)actual+=data[i];GC.KeepAlive(data);Console.WriteLine(actual==expected?"PRESERVED":"CORRUPTED");return actual==expected?0:1;}
}
