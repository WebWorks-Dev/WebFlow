using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WebFlow.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class UniqueAttribute : Attribute
{
}