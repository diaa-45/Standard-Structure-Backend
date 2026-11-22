using System;
using System.Collections.Generic;

namespace Application.Core.Entities
{
 public class Role
 {
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Name { get; set; } = null!;

 // Navigation
 public List<RolePermission> RolePermissions { get; set; } = new();
 public List<UserRole> UserRoles { get; set; } = new();
 }
}
