using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebFlow.Attributes;

/// <summary>
/// Specifies that the value must not be a duplicate within the database
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class UniqueAttribute : Attribute
{
}