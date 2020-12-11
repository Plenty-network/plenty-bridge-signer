using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hid.Net;
using HidSharp;
using LedgerWallet.HIDProviders;

namespace Ledger.HIDProviders.HidSharp
{
    public class HidSharpProvider : IHIDProvider
    {
        public IHIDDevice CreateFromDescription(HIDDeviceInformation description)
        {
            return new HidSharpDevice((HidDevice) description.ProviderInformation);
        }

        public Task<IEnumerable<HIDDeviceInformation>> EnumerateDeviceDescriptions(
            IEnumerable<VendorProductIds> vendorProductIds, UsageSpecification[] acceptedUsages)
        {
            var devices = new List<HidDevice>();

            var collection = DeviceList.Local.GetHidDevices();

            foreach (var ids in vendorProductIds)
            {
                if (ids.ProductId == null)
                    devices.AddRange(collection.Where(c => c.VendorID == ids.VendorId));
                else
                    devices.AddRange(collection.Where(c => c.VendorID == ids.VendorId && c.ProductID == ids.ProductId));
            }

            var retVal = devices
                /*.Where(d =>
                    acceptedUsages == null ||
                    acceptedUsages.Length == 0 ||
                    acceptedUsages.Any(u => d.UsagePage == u.UsagePage && d.Usage == u.Usage))*/
                .ToList();

            return Task.FromResult<IEnumerable<HIDDeviceInformation>>(retVal.Select(r => new HIDDeviceInformation()
            {
                ProductId = (ushort) r.ProductID,
                VendorId = (ushort) r.VendorID,
                DevicePath = r.DevicePath,
                ProviderInformation = r
            }));
        }
    }
}