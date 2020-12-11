using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using LedgerWallet.HIDProviders;
using NBitcoin;

namespace Ledger.HIDProviders.HidSharp
{
    public class HidSharpDevice : IHIDDevice
    {
        private readonly HidDevice _device;
        private HidStream _hidStream;
        private byte[] _inputReportBuffer;
        private bool _initialized = false;
        private DeviceItemInputParser _inputParser;
        private HidDeviceInputReceiver _inputReceiver;

        public HidSharpDevice(HidDevice device)
        {
            _device = device;
        }

        public int VendorId => _device.VendorID;

        public int ProductId => _device.ProductID;
        public string DevicePath => _device.DevicePath;

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(_initialized);
        }

        public async Task EnsureInitializedAsync(CancellationToken cancellation)
        {
            if (_initialized)
            {
                return;
            }
            await Task.Run(() =>
            {
                _hidStream = _device.Open();
                var item = _device.GetReportDescriptor().DeviceItems.First();
                var inputReport = item.InputReports.First();
                var outputReport = item.OutputReports.First();
                _inputReportBuffer = new byte[_device.GetMaxInputReportLength()];
                var inputReceiver = _device.GetReportDescriptor().CreateHidDeviceInputReceiver();
                _inputParser = item.CreateDeviceItemInputParser();
                _inputReceiver = inputReceiver;
                _inputReceiver.Start(_hidStream);
                _hidStream.WriteTimeout = int.MaxValue;
                _hidStream.ReadTimeout = int.MaxValue;
                _initialized = true;
            }).WithCancellation(cancellation);
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellation)
        {
            return await Task.Run(() =>
            {
                _inputReceiver.WaitHandle.WaitOne(1000);
                Report report;
                while (_inputReceiver.TryRead(_inputReportBuffer, 0, out report))
                {
                    // Parse the report if possible.
                    // This will return false if (for example) the report applies to a different DeviceItem.
                    if (_inputParser.TryParseReport(_inputReportBuffer, 0, report))
                    {
                        var bytes = new byte[64];
                        Array.Copy(_inputReportBuffer, 1, bytes, 0, 64);
                        return bytes;
                    }
                }

                return _inputReportBuffer;
            }).WithCancellation(cancellation);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken cancellation)
        {
            if (offset != 0 || length != buffer.Length)
            {
                var newBuffer = new byte[buffer.Length - offset];
                Buffer.BlockCopy(buffer, offset, newBuffer, 0, length);
                buffer = newBuffer;
            }
            var report = new byte[65];
            Buffer.BlockCopy(buffer, 0, report, 1, length);

            await _hidStream.WriteAsync(report, cancellation);
        }

        public IHIDDevice Clone()
        {
            return this;
        }
    }
}