using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.GnosisSafe;
using Nethereum.GnosisSafe.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

public static class GnosisSafeConstants
{
    public const string MultisendContractFunctionSignature = "0x8d80ff0a";
    public const string GnosisSafeContractFunctionSignature = "0x6a761202";
    public const string ERC20TransferFunctionSignature = "0xa9059cbb";
}

public class GnosisSafeTransactionParser : ITransactionParser
{
    public async Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var transactionDisplayInfo = new TransactionDisplayInfo(
            $"New {walletConfig.Name} Transaction",
            new List<TransactionInfo>(),
            evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
            Web3.Convert.FromWei(receipt.GasUsed.Value * transaction.GasPrice.Value),
            transaction.TransactionHash
        );

        if (transaction.Input.StartsWith(GnosisSafeConstants.GnosisSafeContractFunctionSignature))
        {
            // This is a Gnosis Safe SingleSend transaction
            var gnosisTransaction = await DecodeSingleSendTransactionDataAsync(transaction, walletConfig, evmEndpointConfig);
            transactionDisplayInfo.Transactions.Add(gnosisTransaction);
        }

        return transactionDisplayInfo;
    }

    private List<TransactionInfo> DecodeMultiSendTransactionData(Transaction transaction)
    {
        var inputData = transaction.Input;

        int currentIndex = 10; // Start after the MultiSend signature
        int transactionsCount = (int)BigInteger.Parse(inputData.Substring(currentIndex, 64), NumberStyles.HexNumber);
        currentIndex += 64;

        List<TransactionInfo> transactions = new List<TransactionInfo>();

        for (int i = 0; i < transactionsCount; i++)
        {
            var to = inputData.Substring(currentIndex, 40).EnsureHexPrefix();
            currentIndex += 40;

            var value = BigInteger.Parse(inputData.Substring(currentIndex, 64), NumberStyles.HexNumber);
            currentIndex += 64;

            int dataLength = (int)BigInteger.Parse(inputData.Substring(currentIndex, 64), NumberStyles.HexNumber);
            currentIndex += 64;

            var data = inputData.Substring(currentIndex, dataLength * 2).EnsureHexPrefix();
            currentIndex += dataLength * 2;

            // Add the TransactionInfo to the transactions list
            var transactionInfo = new TransactionInfo(TransactionType.Send, to, (decimal)value, "ETH", 0, data);
            transactions.Add(transactionInfo);
        }

        return transactions;
    }

    private async Task<TransactionInfo> DecodeSingleSendTransactionDataAsync(Transaction transaction, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var inputData = transaction.Input;
        var web3 = new Web3(evmEndpointConfig.RpcUrl);

        var to = inputData.Substring(34, 40).EnsureHexPrefix();
        var value = BigInteger.Parse(inputData.Substring(74, 64), NumberStyles.HexNumber);

        if (inputData.Substring(714, 10).EnsureHexPrefix().StartsWith(GnosisSafeConstants.ERC20TransferFunctionSignature))
        {
            // This is an ERC20 token transfer
            var tokenAddress = to;
            var erc20To = inputData.Substring(746, 40).EnsureHexPrefix();
            var tokenAmount = BigInteger.Parse(inputData.Substring(786, 64), NumberStyles.HexNumber);
            var tokenSymbol = await Erc20TransactionParser.GetTokenSymbol(web3, tokenAddress);
            int decimals = await Erc20TransactionParser.GetTokenDecimals(web3, tokenAddress);
            BigInteger newBalance = await Erc20TransactionParser.GetTokenBalanceAsync(web3, tokenAddress, walletConfig.Address);

            decimal total = (decimal)newBalance / (decimal)Math.Pow(10, decimals);
            var tokenValue = (decimal)tokenAmount / (decimal)Math.Pow(10, decimals);

            // Create a TransactionInfo object for the ERC20 token transfer
            return new TransactionInfo(TransactionType.Send, erc20To, tokenValue, tokenSymbol, total, inputData);
        }
        else
        {
            // This is a native token transfer
            decimal newBalance = Web3.Convert.FromWei(await web3.Eth.GetBalance.SendRequestAsync(walletConfig.Address));
            var nativeTokenValue = Web3.Convert.FromWei(value);
            return new TransactionInfo(TransactionType.Send, to, nativeTokenValue, evmEndpointConfig.NativeToken, newBalance, inputData);
        }
    }

    public static bool IsGnosisSafeTransaction(Transaction transaction)
    {
        // Check if the transaction is a Gnosis Safe transaction by verifying its input data
        return transaction.Input.StartsWith(GnosisSafeConstants.GnosisSafeContractFunctionSignature, StringComparison.OrdinalIgnoreCase) || transaction.Input.StartsWith(GnosisSafeConstants.MultisendContractFunctionSignature, StringComparison.OrdinalIgnoreCase);
    }
}