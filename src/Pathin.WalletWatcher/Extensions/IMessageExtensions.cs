// <copyright file="IMessageExtensions.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Pathin.WalletWatcher.Extensions;

/// <summary>
/// Extensions for the <see cref="IMessage"/> interface.
/// </summary>
public static class IMessageExtensions
{
    /// <summary>
    /// Converts a message to a message reference.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="throwError">Throw an error if the reference doesn't exist.</param>
    /// <returns>A message reference.</returns>
    public static MessageReference ToReference(this IMessage message, bool throwError = false) => new(message.Id, message.Channel.Id, null, throwError);
}