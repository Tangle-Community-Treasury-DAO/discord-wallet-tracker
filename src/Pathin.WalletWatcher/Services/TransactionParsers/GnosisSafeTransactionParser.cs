// <copyright file="GnosisSafeTransactionParser.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Globalization;
using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Pathin.WalletWatcher.Config;
using Pathin.WalletWatcher.Enums;
using Pathin.WalletWatcher.Interfaces;

namespace Pathin.WalletWatcher.Services.TransactionParsers;

/// <summary>
/// Represents an implementation of the <see cref="ITransactionParser"/> interface for parsing Gnosis Safe transactions.
/// </summary>
public class GnosisSafeTransactionParser : ITransactionParser
{
    /// <summary>
    /// Determines if a transaction is a Gnosis Safe transaction.
    /// </summary>
    /// <param name="transaction">The transaction to check.</param>
    /// <returns>True if the transaction is a Gnosis Safe transaction, otherwise false.</returns>
    public static bool IsGnosisSafeTransaction(Transaction transaction)
    {
        // Check if the transaction is a Gnosis Safe transaction by verifying its input data
        return transaction.Input.StartsWith(SmartContractConstants.GnosisExecFunctionSignature, StringComparison.OrdinalIgnoreCase) || transaction.Input.StartsWith(SmartContractConstants.MultisendContractFunctionSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<TransactionDisplayInfo?> ParseTransactionAsync(Transaction transaction, TransactionReceipt receipt, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var transactionDisplayInfo = new TransactionDisplayInfo(
            $"New {walletConfig.Name} Transaction",
            new List<TransactionInfo>(),
            evmEndpointConfig.ExplorerUrl != null ? string.Format(evmEndpointConfig.ExplorerUrl, transaction.TransactionHash) : null,
            Web3.Convert.FromWei(receipt.GasUsed.Value * transaction.GasPrice.Value),
            transaction.TransactionHash);

        if (transaction.Input.StartsWith(SmartContractConstants.GnosisExecFunctionSignature))
        {
            // This is a Gnosis Safe SingleSend transaction
            var gnosisTransaction = await DecodeSingleSendTransactionDataAsync(transaction.Input, walletConfig, evmEndpointConfig);
            transactionDisplayInfo.Transactions.Add(gnosisTransaction);
        }
        else if (transaction.Input.StartsWith(SmartContractConstants.MultisendContractFunctionSignature))
        {
            // This is a Gnosis Safe MultiSend transaction
            var multiSendTransactions = await DecodeMultiSendTransactionDataAsync(transaction, walletConfig, evmEndpointConfig);
            transactionDisplayInfo.Transactions.AddRange(multiSendTransactions);
        }

        return transactionDisplayInfo;
    }

    private static async Task<TransactionInfo> DecodeSingleSendTransactionDataAsync(string inputData, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var web3 = new Web3(evmEndpointConfig.RpcUrl);

        var to = inputData.Substring(34, 40).EnsureHexPrefix();
        var value = BigInteger.Parse(inputData.Substring(74, 64), NumberStyles.HexNumber);

        if (inputData.Substring(714, 10).EnsureHexPrefix().StartsWith(SmartContractConstants.ERC20TransferFunctionSignature))
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

    private static async Task<List<TransactionInfo>> DecodeMultiSendTransactionDataAsync(Transaction transaction, WalletConfig walletConfig, EvmEndpointConfig evmEndpointConfig)
    {
        var inputData = transaction.Input;

        var transactions = new List<TransactionInfo>();

        // Get Gnosis function signature without 0x prefix
        var functionSignature = SmartContractConstants.GnosisExecFunctionSignature[2..];

        // Get index of first gnosis exec (remove 0x prefix)
        int currentIndex = inputData.IndexOf(functionSignature);

        while (currentIndex >= 0 && currentIndex < inputData.Length)
        {
            int nextIndex = inputData.IndexOf(functionSignature, currentIndex + 1);
            int dataLengthNextTx = nextIndex - currentIndex;
            string data = dataLengthNextTx <= 0 ? inputData[currentIndex..] : inputData.Substring(currentIndex, dataLengthNextTx);
            var gnosisTransaction = await DecodeSingleSendTransactionDataAsync(data.EnsureHexPrefix(), walletConfig, evmEndpointConfig);
            transactions.Add(gnosisTransaction);

            currentIndex = nextIndex;
        }

        return transactions;
    }
}