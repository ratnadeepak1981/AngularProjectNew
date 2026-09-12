using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CampusServicesPortal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CampusServicesPortal.Interceptors
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private List<AuditEntry>? _pendingAuditEntries;

        private static readonly HashSet<string> SensitiveKeyWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passwordhash", "passwordsalt", "token", "refreshtoken",
            "jwt", "otp", "mobileotpcode", "cvc", "cardnumber", "creditcard",
            "secret", "authorization", "securitystamp", "pin"
        };

        // Enterprise Compliance Filter: Exclude technical background noise & downstream notification rows
        private static readonly HashSet<string> ExcludedAuditEntities = new(StringComparer.OrdinalIgnoreCase)
        {
            "AuditLog",
            "RefreshToken",
            "PasswordResetToken",
            "Notification"
        };

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // =========================================================================
        // PHASE 1: PREPARE AUDIT ENTRIES BEFORE DATABASE EXECUTION
        // =========================================================================
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                PrepareAuditEntries(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                PrepareAuditEntries(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        private void PrepareAuditEntries(DbContext context)
        {
            // Crucial: Force snapshot change detection before reading ChangeTracker entries
            context.ChangeTracker.DetectChanges();

            var httpContext = _httpContextAccessor.HttpContext;
            int? currentUserId = null;
            string? currentUserEmail = null;
            string? clientIp = null;
            string? traceId = null;

            if (httpContext != null)
            {
                var userIdClaim = httpContext.User.FindFirst("UserId")?.Value
                                  ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? httpContext.User.FindFirst("sub")?.Value;
                if (int.TryParse(userIdClaim, out var parsedId))
                {
                    currentUserId = parsedId;
                }

                currentUserEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                                   ?? httpContext.User.FindFirst("email")?.Value
                                   ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                                   ?? httpContext.User.Identity?.Name;

                clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
                traceId = httpContext.TraceIdentifier;
            }

            var entries = context.ChangeTracker.Entries()
                .Where(e => !ExcludedAuditEntities.Contains(e.Entity.GetType().Name) &&
                           (e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted))
                .ToList();

            if (entries.Count == 0)
            {
                _pendingAuditEntries = null;
                return;
            }

            var auditEntries = new List<AuditEntry>();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                var action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => entry.State.ToString()
                };

                var primaryKeyProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                string? initialEntityId = null;

                if (entry.State != EntityState.Added)
                {
                    initialEntityId = primaryKeyProp?.CurrentValue?.ToString() ?? primaryKeyProp?.OriginalValue?.ToString();
                }

                // Identify Parent / Root Entity hierarchy (e.g. StudentAddress & StudentPhoneNumber belong to Student root)
                string parentEntityName = entityName;
                string? parentEntityId = initialEntityId;
                string propertyPrefix = string.Empty;

                if (entityName == "StudentAddress")
                {
                    parentEntityName = "Student";
                    var studentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "StudentId");
                    parentEntityId = studentIdProp?.CurrentValue?.ToString() ?? studentIdProp?.OriginalValue?.ToString();
                    propertyPrefix = "Address_";
                }
                else if (entityName == "StudentPhoneNumber")
                {
                    parentEntityName = "Student";
                    var studentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "StudentId");
                    parentEntityId = studentIdProp?.CurrentValue?.ToString() ?? studentIdProp?.OriginalValue?.ToString();
                    propertyPrefix = "Phone_";
                }
                else if (entityName == "LabBookingTimeSlot")
                {
                    parentEntityName = "LabBooking";
                    var parentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "LabBookingId" || p.Metadata.Name == "BookingId");
                    parentEntityId = parentIdProp?.CurrentValue?.ToString() ?? parentIdProp?.OriginalValue?.ToString();
                    propertyPrefix = "TimeSlot_";
                }
                else if (entityName == "Room")
                {
                    parentEntityName = "Hostel";
                    var parentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "HostelId");
                    parentEntityId = parentIdProp?.CurrentValue?.ToString() ?? parentIdProp?.OriginalValue?.ToString();
                    propertyPrefix = "Room_";
                }
                else if (entityName == "EventRegistration")
                {
                    parentEntityName = "Event";
                    var parentIdProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "EventId");
                    parentEntityId = parentIdProp?.CurrentValue?.ToString() ?? parentIdProp?.OriginalValue?.ToString();
                    propertyPrefix = "Registration_";
                }

                var beforeValues = new Dictionary<string, object?>();
                var afterValues = new Dictionary<string, object?>();
                bool hasChangedProperties = false;

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsShadowProperty()) continue;

                    var propName = prop.Metadata.Name;

                    // Skip internal foreign keys / sub-IDs when aggregating into parent
                    if ((entityName == "StudentAddress" || entityName == "StudentPhoneNumber") &&
                        (propName == "Id" || propName == "StudentId"))
                    {
                        continue;
                    }

                    var isSensitive = IsSensitiveProperty(propName);
                    var outputKey = string.IsNullOrEmpty(propertyPrefix) ? propName : $"{propertyPrefix}{propName}";

                    if (entry.State == EntityState.Added)
                    {
                        afterValues[outputKey] = isSensitive ? "***REDACTED***" : prop.CurrentValue;
                        hasChangedProperties = true;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        beforeValues[outputKey] = isSensitive ? "***REDACTED***" : prop.OriginalValue;
                        hasChangedProperties = true;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (prop.IsModified)
                        {
                            beforeValues[outputKey] = isSensitive ? "***REDACTED***" : prop.OriginalValue;
                            afterValues[outputKey] = isSensitive ? "***REDACTED***" : prop.CurrentValue;
                            hasChangedProperties = true;
                        }
                    }
                }

                if (!hasChangedProperties && entry.State == EntityState.Modified)
                {
                    continue;
                }

                var descriptionTemplate = entry.State switch
                {
                    EntityState.Added => $"Created new {parentEntityName} record #{'{'}entityId{'}'}.",
                    EntityState.Modified => $"Updated properties on {parentEntityName} #{'{'}entityId{'}'}.",
                    EntityState.Deleted => $"Deleted {parentEntityName} #{'{'}entityId{'}'} record.",
                    _ => $"Modified {parentEntityName} record."
                };

                var auditEntry = new AuditEntry
                {
                    Entry = entry,
                    UserId = currentUserId,
                    UserDisplayName = currentUserEmail ?? "System Internal",
                    Action = action,
                    Module = MapEntityToModule(parentEntityName),
                    EntityName = entityName,
                    ParentEntityName = parentEntityName,
                    ParentEntityId = parentEntityId,
                    Timestamp = DateTime.UtcNow,
                    IsSuccess = true,
                    IpAddress = clientIp,
                    TraceId = traceId,
                    DescriptionTemplate = descriptionTemplate,
                    PrimaryKeyProperty = primaryKeyProp,
                    ExplicitEntityId = initialEntityId,
                    HasChangedProperties = hasChangedProperties
                };

                foreach (var kvp in beforeValues) auditEntry.BeforeValues[kvp.Key] = kvp.Value;
                foreach (var kvp in afterValues) auditEntry.AfterValues[kvp.Key] = kvp.Value;

                auditEntries.Add(auditEntry);
            }

            _pendingAuditEntries = auditEntries.Count > 0 ? auditEntries : null;
        }

        // =========================================================================
        // PHASE 2: FINALIZE AND COMMIT AUDIT ENTRIES WITH REAL SQL IDENTITY KEYS & MERGING
        // =========================================================================
        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            var pending = _pendingAuditEntries;
            _pendingAuditEntries = null;

            if (pending != null && pending.Count > 0 && eventData.Context != null)
            {
                var logs = ConsolidateAndResolveAuditLogs(pending);
                if (logs.Count > 0)
                {
                    eventData.Context.Set<AuditLog>().AddRange(logs);
                    await eventData.Context.SaveChangesAsync(cancellationToken);
                }
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {
            var pending = _pendingAuditEntries;
            _pendingAuditEntries = null;

            if (pending != null && pending.Count > 0 && eventData.Context != null)
            {
                var logs = ConsolidateAndResolveAuditLogs(pending);
                if (logs.Count > 0)
                {
                    eventData.Context.Set<AuditLog>().AddRange(logs);
                    eventData.Context.SaveChanges();
                }
            }

            return base.SavedChanges(eventData, result);
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            _pendingAuditEntries = null;
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _pendingAuditEntries = null;
            base.SaveChangesFailed(eventData);
        }

        private static List<AuditLog> ConsolidateAndResolveAuditLogs(List<AuditEntry> entries)
        {
            if (entries == null || entries.Count == 0) return new List<AuditLog>();

            // Group entries by (Module, ParentEntityName/TargetEntity, RootEntityId, TraceId) to merge parent-child twin passes into a SINGLE database row
            var grouped = entries
                .GroupBy(e => new
                {
                    Module = e.Module,
                    TargetEntity = e.ParentEntityName ?? e.EntityName,
                    EntityId = e.ResolveRootEntityId(),
                    TraceId = e.TraceId ?? string.Empty
                });

            var result = new List<AuditLog>();

            foreach (var group in grouped)
            {
                var first = group.First();
                var resolvedId = group.Key.EntityId;
                var targetEntity = group.Key.TargetEntity;

                // Determine overall action across the group:
                // If there's a mix of Delete & Create, or any Update -> overall action is "Update"
                // Otherwise if all items are Create -> "Create", if all items are Delete -> "Delete"
                var actions = group.Select(g => g.Action).Distinct().ToList();
                string resolvedAction;
                if (actions.Contains("Update") || (actions.Contains("Delete") && actions.Contains("Create")))
                {
                    resolvedAction = "Update";
                }
                else if (actions.All(a => a == "Create"))
                {
                    resolvedAction = "Create";
                }
                else if (actions.All(a => a == "Delete"))
                {
                    resolvedAction = "Delete";
                }
                else
                {
                    resolvedAction = actions.FirstOrDefault() ?? "Update";
                }

                // Merge BeforeValues: retain the earliest before-value for each property
                var mergedBefore = new Dictionary<string, object?>();
                foreach (var item in group)
                {
                    foreach (var kvp in item.BeforeValues)
                    {
                        if (!mergedBefore.ContainsKey(kvp.Key))
                        {
                            mergedBefore[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Merge AfterValues: retain the latest after-value for each property
                var mergedAfter = new Dictionary<string, object?>();
                foreach (var item in group)
                {
                    foreach (var kvp in item.AfterValues)
                    {
                        mergedAfter[kvp.Key] = kvp.Value;
                    }
                }

                // For Update action, prune unchanged properties (where before == after) to only record real deltas
                if (resolvedAction == "Update")
                {
                    var unchangedKeys = mergedBefore.Keys
                        .Where(k => mergedAfter.ContainsKey(k) &&
                                    (Equals(mergedBefore[k], mergedAfter[k]) ||
                                     string.Equals(mergedBefore[k]?.ToString(), mergedAfter[k]?.ToString(), StringComparison.Ordinal)))
                        .ToList();

                    foreach (var k in unchangedKeys)
                    {
                        mergedBefore.Remove(k);
                        mergedAfter.Remove(k);
                    }
                }

                // If no actual properties changed across the entire group for an Update action, skip emitting an empty log
                if (resolvedAction == "Update" && mergedBefore.Count == 0 && mergedAfter.Count == 0)
                {
                    continue;
                }

                var description = resolvedAction switch
                {
                    "Create" => $"Created new {targetEntity} record #{resolvedId}.",
                    "Delete" => $"Deleted {targetEntity} #{resolvedId} record.",
                    _ => $"Updated properties on {targetEntity} #{resolvedId}."
                };

                result.Add(new AuditLog
                {
                    UserId = first.UserId,
                    UserDisplayName = first.UserDisplayName ?? "System Internal",
                    Action = resolvedAction,
                    Module = first.Module,
                    EntityId = resolvedId,
                    Timestamp = first.Timestamp,
                    IsSuccess = first.IsSuccess,
                    IsReviewed = first.IsSuccess,
                    IpAddress = first.IpAddress,
                    TraceId = first.TraceId,
                    Description = description,
                    BeforeValuesJson = mergedBefore.Count > 0 ? JsonSerializer.Serialize(mergedBefore) : null,
                    AfterValuesJson = mergedAfter.Count > 0 ? JsonSerializer.Serialize(mergedAfter) : null
                });
            }

            return result;
        }

        private static bool IsSensitiveProperty(string propName)
        {
            foreach (var sensitive in SensitiveKeyWords)
            {
                if (propName.IndexOf(sensitive, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static string MapEntityToModule(string entityName)
        {
            return entityName switch
            {
                "User" or "RefreshToken" or "PasswordResetToken" or "PasswordHistory" => "Auth",
                "Student" or "StudentPhoneNumber" or "StudentAddress" => "Students",
                "StudentMasterList" => "StudentMaster",
                "Hostel" or "Room" or "HostelApplication" => "Hostels",
                "Lab" or "LabSeat" or "LabBooking" => "Labs",
                "Event" or "Venue" or "EventRegistration" => "Events",
                "Complaint" or "ComplaintCategory" => "Complaints",
                "CertificateRequest" or "CertificateType" => "Certificates",
                "FeePayment" or "FeeType" => "Billing",
                "Notification" => "Notifications",
                "SystemSetting" => "SystemSettings",
                "Faculty" => "Faculties",
                _ => entityName
            };
        }

        // Internal helper model to hold state between SavingChanges and SavedChanges
        private class AuditEntry
        {
            public EntityEntry Entry { get; set; } = null!;
            public int? UserId { get; set; }
            public string? UserDisplayName { get; set; }
            public string Action { get; set; } = string.Empty;
            public string Module { get; set; } = string.Empty;
            public string EntityName { get; set; } = string.Empty;
            public string? ParentEntityName { get; set; }
            public string? ParentEntityId { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsSuccess { get; set; }
            public string? IpAddress { get; set; }
            public string? TraceId { get; set; }
            public string DescriptionTemplate { get; set; } = string.Empty;
            public PropertyEntry? PrimaryKeyProperty { get; set; }
            public string? ExplicitEntityId { get; set; }
            public bool HasChangedProperties { get; set; }
            public Dictionary<string, object?> BeforeValues { get; } = new();
            public Dictionary<string, object?> AfterValues { get; } = new();

            public string ResolveRootEntityId()
            {
                if (!string.IsNullOrWhiteSpace(ParentEntityId))
                {
                    return ParentEntityId;
                }
                var entityId = ExplicitEntityId;
                if (string.IsNullOrWhiteSpace(entityId) && PrimaryKeyProperty != null)
                {
                    entityId = PrimaryKeyProperty.CurrentValue?.ToString() ?? PrimaryKeyProperty.OriginalValue?.ToString();
                }
                return entityId ?? "N/A";
            }
        }
    }
}
