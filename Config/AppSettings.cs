using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pathin.WalletWatcher.Config;

public class AppSettings
{
    public List<EvmEndpointConfig> EvmEndpoints { get; set; } = new();

    public List<WalletConfig> Wallets { get; set; } = new();

    public IEnumerable<EvmEndpointConfig> GetEvmEndpointConfigsForWallet(WalletConfig wallet) {  
        return EvmEndpoints.Where(e => wallet.EvmEndpoints.Contains(e.Name));
    }
}
