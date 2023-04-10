// <copyright file="TransientServiceAttribute.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace Pathin.WalletWatcher.Attributes;

/// <summary>
/// The TransientServiceAttribute is an attribute that can be used to mark a class for registration as a transient service.
/// Transient services are created each time they are requested.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TransientServiceAttribute : Attribute
{
}