using System.Linq;
using System.Threading.Tasks;
using Ledger.HIDProviders.HidSharp;
using LedgerWallet.HIDProviders;
using Netezos.Ledger;

namespace Ledger
{
    public class Client
    {
        public static async Task<TezosLedgerClient> get()
        {
            HIDProvider.Provider = new HidSharpProvider();
            var clients = await TezosLedgerClient.GetHIDLedgersAsync();
            return clients.First();
        }
    }
}