using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Win32.SafeHandles;
using Windows.Devices.Custom;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Buffer = Windows.Storage.Streams.Buffer;
using IOControlCode = Windows.Devices.Custom.IOControlCode;

class Program
{
    private DeviceWatcher _deviceWatcher;
    private IOControlCode _controlCode = new(0x7777, 0x100, IOControlAccessMode.ReadWrite, IOControlBufferingMethod.Buffered);
    private Guid interfaceGUID;
    private CustomDevice? _device = null;
    private string? DeviceId;
    private static ManualResetEvent waitForEnum = new ManualResetEvent(false);

    public static void Main(string[] args) => new Program().Run();

    private class IOCTLControlCode : IIOControlCode
    {
        IOControlAccessMode IIOControlCode.AccessMode
        {
            get;
        } = IOControlAccessMode.ReadWrite;


        public IOControlBufferingMethod BufferingMethod
        {
            get;
        } = IOControlBufferingMethod.Buffered;

        public uint ControlCode
        {
            get;
        } = 0x77772400;

        public ushort DeviceType
        {
            get;
        } = 0x7777;

        public ushort Function
        {
            get;
        } = 0x100;
    }

    void Run()
    {
        interfaceGUID = new Guid("{c37acb87-d563-4aa0-b761-996e7864af79}");
        _deviceWatcher = DeviceInformation.CreateWatcher(CustomDevice.GetDeviceSelector(interfaceGUID));

        _deviceWatcher.Added += DeviceAddedEvent;
        _deviceWatcher.EnumerationCompleted += EnumerationCompleteEvent;
        _deviceWatcher.Start();
        waitForEnum.WaitOne();
        waitForEnum.Reset();

        if (DeviceId == null)
        {
            return;
        }

        _device = CustomDevice.FromIdAsync(DeviceId, DeviceAccessMode.ReadWrite, DeviceSharingMode.Shared).GetResults();

        int numOfLeds = 5;
        int device = 3; // 2 = External RGB Header, 3 = Internal RGB

        byte[] header = { // This should turn off all leds (or at least the onboard ones)
                (byte)device, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)numOfLeds, 0x00, 0x00, 0x00,
                0x14, 0x00, 0x00, 0x00
            };
        byte[] command = new byte[1044];
        header.CopyTo(command, 0);
        for (int i = 0; i < 4; i++)
        {
            command[20 + (4 * i)] = 0xFF; //Red
            command[21 + (4 * i)] = 0x00; //Green
            command[22 + (4 * i)] = 0x00; //Blue
            command[23 + (4 * i)] = 0xFF; //Splitter
        }
        
        var inputBuffer = CryptographicBuffer.CreateFromByteArray(command);
        var outputBuffer = new Buffer(1044);
        var bufferOtherSide = inputBuffer.ToArray();
        Console.WriteLine(_controlCode.ControlCode);

        uint success;
        Console.WriteLine(_device.SendIOControlAsync(new IOCTLControlCode(), inputBuffer, outputBuffer).GetResults());
    }


    void EnumerationCompleteEvent(DeviceWatcher sender, object idk)
    {
        waitForEnum.Set();
    }


    // TODO: Proper async for AE5's DeviceAddedEvent
    void DeviceAddedEvent(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        DeviceId = deviceInfo.Id;
    }
}
