// <copyright file="SingletonServiceAttribute.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Attributes;

/// <summary>
/// The SingletonServiceAttribute is an attribute that can be used to mark a class for registration as a singleton service.
/// Singleton services are created once per application and shared across all requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class SingletonServiceAttribute : Attribute
{
}