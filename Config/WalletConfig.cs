using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pathin.WalletWatcher.Config;

public record WalletConfig(){

    public WalletConfig(string name, string address, string webhookUrl, IEnumerable<string> evmEndpoints, bool active) : this()
    {
        Name = name;
        Address = address;
        WebhookUrl = webhookUrl;
        EvmEndpoints = evmEndpoints;
        Active = active;
    }

    public string Name { get; init; }
    public string Address { get; init; }
    public string WebhookUrl { get; init; }
    public IEnumerable<string> EvmEndpoints { get; init; } = new List<string>();
    public bool Active { get; init; }
}