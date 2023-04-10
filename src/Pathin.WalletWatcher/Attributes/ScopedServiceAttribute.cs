// <copyright file="ScopedServiceAttribute.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Attributes;

/// <summary>
/// The ScopedServiceAttribute is an attribute that can be used to mark a class for registration as a scoped service.
/// Scoped services are created once per request within the scope.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ScopedServiceAttribute : Attribute
{
}