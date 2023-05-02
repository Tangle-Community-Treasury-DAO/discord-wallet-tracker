using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

public class DefaultTransactionParser : ITransactionParser
{
    public async Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var web3 = new Web3(evmEndpointConfig.RpcUrl);
        var transactionDisplayInfo = new TransactionDisplayInfo(
            $"New {walletConfig.Name} Transaction",
            new List<TransactionInfo>(),
            evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
            Web3.Convert.FromWei(receipt.GasUsed.Value * transaction.GasPrice.Value),
            transaction.TransactionHash
        );

        var transactionType = transaction.From.Equals(walletConfig.Address, StringComparison.OrdinalIgnoreCase) ? TransactionType.Send : TransactionType.Receive;
        string partyAddress = transactionType == TransactionType.Send ? transaction.To : transaction.From;

        decimal value = Web3.Convert.FromWei(transaction.Value);
        decimal newBalance = Web3.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(walletConfig.Address));

        transactionDisplayInfo.Transactions.Add(new TransactionInfo(transactionType, partyAddress, value, evmEndpointConfig.NativeToken, newBalance));

        return transactionDisplayInfo;
    }
}
