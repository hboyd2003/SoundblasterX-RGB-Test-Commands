using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Text;
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

class Program
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
    static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, ref uint requiredSize, IntPtr deviceInfoData);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_Interface_List_ExW(ref Guid InterfaceClassGuid, IntPtr DeviceId, [Out] char[] Buffer, int BufferLength, int Flags, IntPtr Machine);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_Interface_Property_ExW(string DeviceInterface, ref Guid PropertyKey, IntPtr PropertyType, IntPtr PropertyBuffer, ref int PropertyBufferSize, int Flags, IntPtr Machine);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Class_Property_ExW(ref Guid ClassGuid, ref Guid PropertyKey, IntPtr PropertyType, IntPtr PropertyBuffer, ref int PropertyBufferSize, int Flags, IntPtr Machine);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Locate_DevNode_ExW(out int pdnDevInst, string pDeviceID, int ulFlags, IntPtr Machine);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_DevNode_Property_ExW(int dnDevInst, ref Guid PropertyKey, IntPtr PropertyType, IntPtr PropertyBuffer, ref int PropertyBufferSize, int Flags, IntPtr Machine);

    [StructLayout(LayoutKind.Sequential)]
    struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid interfaceClassGuid;
        public int flags;
        private IntPtr reserved;
    }

    const uint DIGCF_PRESENT = 0x02;
    const uint DIGCF_DEVICEINTERFACE = 0x10;

    static void Main(string[] args)
    {
        Console.ReadKey();
        //c37acb87-d563-4aa0-b761-996e7864af79
        //a17579f0-4fec-4936-9364-249460863be5
        Guid deviceInterfaceClassGuid = new Guid("c37acb87-d563-4aa0-b761-996e7864af79");

        IntPtr deviceInfoSet = SetupDiGetClassDevs(ref deviceInterfaceClassGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

        if (deviceInfoSet != IntPtr.Zero)
        {
            SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

            if (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref deviceInterfaceClassGuid, 0, ref deviceInterfaceData))
            {
                uint requiredSize = 0;
                SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);

                IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);

                Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, requiredSize, ref requiredSize, IntPtr.Zero))
                {
                    IntPtr pDevicePathName = new IntPtr(detailDataBuffer.ToInt64() + 4);
                    string devicePath = Marshal.PtrToStringAuto(pDevicePathName);

                    Console.WriteLine("Device path: " + devicePath);
                    Console.ReadKey();

                    char[] idkBuffer = new char[16384];
                    devicePath.CopyTo(0, idkBuffer, 0, devicePath.Length);


                    openDevice(devicePath);
                }

                Marshal.FreeHGlobal(detailDataBuffer);
            }
        }
    }

    static void openDevice(string devicePath)
    {
        //string deviceGuid = "{4d36e96c-e325-11ce-bfc1-08002be10318}";
        //string devicePath = @"\\.\GLOBALROOT\Device\" + deviceGuid;

        SafeFileHandle deviceHandle = CreateFile(devicePath, 0xc0000000, 0x00000003, IntPtr.Zero, 0x00000003, 0, IntPtr.Zero);
        Console.WriteLine(Marshal.GetLastWin32Error());
        Console.ReadKey();
        Console.WriteLine(Marshal.GetLastWin32Error());
        if (!deviceHandle.IsInvalid)
        {
            uint bytesReturned = 0x01863230;
            uint IOCTL_CODE = 0x77772400; // Replace with your IOCTL code
            uint nInBufferSize = 1044; // Replace with your input buffer size
            //string path = @"C:\Users\harri\source\repos\SoundblasterX RGB Test Commands\bin\x86\Debug\net6.0\SoundblasterX RGB Test Commands.exe";
            //byte[] pathBytes = Encoding.Unicode.GetBytes(path);

            byte[] inputBuffer = new byte[1044];
            byte[] inputData = new byte[] {
                0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00,
                0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF,
                0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            Array.Copy(inputData, inputBuffer, inputData.Length);

            

            byte[] nOutBuffer = new byte[1044];

            GCHandle inputHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
            IntPtr lpInBuffer = inputHandle.AddrOfPinnedObject();
            Marshal.Copy(inputBuffer, 0, lpInBuffer, inputBuffer.Length);

            GCHandle outputHandle = GCHandle.Alloc(nOutBuffer, GCHandleType.Pinned);
            IntPtr lpOutBuffer = outputHandle.AddrOfPinnedObject();
            Marshal.Copy(lpOutBuffer, nOutBuffer, 0, nOutBuffer.Length);


            bool result = DeviceIoControl(deviceHandle, IOCTL_CODE, lpInBuffer, nInBufferSize, lpOutBuffer, 1044, out bytesReturned, IntPtr.Zero);

            if (result)
            {
                Console.WriteLine("IOCTL command sent successfully.");
            }
            else
            {
                Console.WriteLine("Failed to send IOCTL command.");
            }
        }
        else
        {
            Console.WriteLine("Failed to open device.");
        }
    }
}
