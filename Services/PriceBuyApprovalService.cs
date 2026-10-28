using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TMSBilling.Data;
using TMSBilling.Models;

namespace TMSBilling.Services;

public sealed class PriceBuyApprovalService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _db;

    public PriceBuyApprovalService(AppDbContext db) => _db = db;

    public async Task<long> SubmitAsync(
        PriceBuyApprovalSubmitDto request, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("The session username could not be found.");

        var requestType = Normalize(request.RequestType);
        if (requestType is not ("CREATE" or "UPDATE" or "DEACTIVATE" or "IMPORT"))
            throw new InvalidOperationException("The request type is invalid.");

        if (request.Items == null || request.Items.Count == 0)
            throw new InvalidOperationException("At least one detail item must be submitted.");

        // All validation is done before creating the request.
        var prepared = new List<(string Operation, PriceBuy? Existing, PriceBuy NewValue, string Key, string? OldJson, string NewJson)>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            var operation = Normalize(item.OperationType);
            if (operation is not ("CREATE" or "UPDATE" or "DEACTIVATE"))
                throw new InvalidOperationException("The detail operation type is invalid.");

            if (requestType != "IMPORT" && request.Items.Count == 1 && operation != requestType)
                throw new InvalidOperationException("The detail operation type does not match the request type.");

            var value = JsonSerializer.Deserialize<PriceBuy>(item.NewValuesJson, JsonOptions)
                ?? throw new InvalidOperationException("The tariff payload is invalid.");

            NormalizeTariffFields(value);
            ValidateRequiredIdentity(value);

            PriceBuy? existing = null;
            if (operation is "UPDATE" or "DEACTIVATE")
            {
                if (item.PriceBuyId is null or <= 0)
                    throw new InvalidOperationException("PriceBuyId is required for update or deactivation requests.");

                existing = await _db.PriceBuys.SingleOrDefaultAsync(x => x.id_seq == item.PriceBuyId, ct);
                if (existing == null)
                    throw new InvalidOperationException($"Tariff ID {item.PriceBuyId} was not found.");

                // Key fields are immutable for update/deactivate; identity comes from existing row.
                value.id_seq = existing.id_seq;
                value.sup_code = existing.sup_code;
                value.origin = existing.origin;
                value.dest = existing.dest;
                value.serv_type = existing.serv_type;
                value.serv_moda = existing.serv_moda;
                value.truck_size = existing.truck_size;
                value.charge_uom = existing.charge_uom;

                if (operation == "DEACTIVATE")
                    value.active_flag = 0;
            }
            else
            {
                value.id_seq = 0;
                var exists = await _db.PriceBuys.AnyAsync(x =>
                    x.sup_code == value.sup_code &&
                    x.origin == value.origin &&
                    x.dest == value.dest &&
                    x.serv_type == value.serv_type &&
                    x.serv_moda == value.serv_moda &&
                    x.truck_size == value.truck_size &&
                    x.charge_uom == value.charge_uom, ct);

                if (exists)
                    throw new InvalidOperationException($"A tariff with this identity already exists: {IdentityLabel(value)}");
            }

            await ValidateMasterDataAsync(value, ct);

            var key = MakeConflictKey(value);
            if (!keys.Add(key))
                throw new InvalidOperationException($"Duplicate tariff found in the request: {IdentityLabel(value)}");

            var newJson = JsonSerializer.Serialize(value, JsonOptions);
            var oldJson = existing == null ? null : JsonSerializer.Serialize(existing, JsonOptions);
            prepared.Add((operation, existing, value, key, oldJson, newJson));
        }

        // One transaction creates the header and all details. PendingConflictKey has a
        // filtered unique index, so concurrent requests cannot reserve the same identity.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var pendingKeys = prepared.Select(x => x.Key).ToList();
        var existingPending = await _db.PriceBuyApprovalDetails
            .AnyAsync(d => d.PendingConflictKey != null && pendingKeys.Contains(d.PendingConflictKey), ct);
        if (existingPending)
            throw new InvalidOperationException("One or more tariffs already have a pending approval request.");

        var now = DateTime.Now;
        var header = new PriceBuyApproval
        {
            RequestNo = await GenerateRequestNoAsync(ct),
            RequestType = requestType,
            Status = "PENDING",
            Requester = username,
            RequestedAt = now,
            SourceFileName = request.SourceFileName,
            RowCount = prepared.Count
        };

        foreach (var item in prepared)
        {
            header.Details.Add(new PriceBuyApprovalDetail
            {
                OperationType = item.Operation,
                PriceBuyId = item.Existing?.id_seq,
                ConflictKey = item.Key,
                PendingConflictKey = item.Key,
                OldValuesJson = item.OldJson,
                NewValuesJson = item.NewJson
            });
        }

        _db.PriceBuyApprovals.Add(header);
        await _db.SaveChangesAsync(ct);

        _db.PriceBuyAudits.Add(new PriceBuyAudit
        {
            ApprovalId = header.ApprovalId,
            Action = "SUBMITTED",
            PerformedBy = username,
            PerformedAt = now,
            Details = JsonSerializer.Serialize(new { header.RequestNo, header.RequestType, header.RowCount })
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return header.ApprovalId;
    }

    public async Task ApproveAsync(long approvalId, string username, string? note, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var header = await _db.PriceBuyApprovals
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.ApprovalId == approvalId, ct);

        if (header == null) throw new KeyNotFoundException("The approval request was not found.");
        if (header.Status != "PENDING") throw new InvalidOperationException("The request is not in PENDING status.");

        // Claim the request atomically; concurrent approve/reject/cancel will fail.
        var claimed = await _db.PriceBuyApprovals
            .Where(x => x.ApprovalId == approvalId && x.Status == "PENDING")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, "PROCESSING")
                .SetProperty(x => x.Approver, username)
                .SetProperty(x => x.DecidedAt, DateTime.Now), ct);

        if (claimed != 1) throw new InvalidOperationException("The request has already been processed by another user.");

        // Revalidate every detail before applying any changes.
        foreach (var detail in header.Details)
        {
            var proposed = JsonSerializer.Deserialize<PriceBuy>(detail.NewValuesJson, JsonOptions)
                ?? throw new InvalidOperationException("The new tariff snapshot is invalid.");

            if (detail.OperationType == "CREATE")
            {
                var duplicate = await _db.PriceBuys.AnyAsync(x =>
                    x.sup_code == proposed.sup_code &&
                    x.origin == proposed.origin &&
                    x.dest == proposed.dest &&
                    x.serv_type == proposed.serv_type &&
                    x.serv_moda == proposed.serv_moda &&
                    x.truck_size == proposed.truck_size &&
                    x.charge_uom == proposed.charge_uom, ct);

                if (duplicate) throw new InvalidOperationException($"The tariff was created after this request was submitted: {IdentityLabel(proposed)}");
                await ValidateMasterDataAsync(proposed, ct);

                proposed.id_seq = 0;
                proposed.entry_user = header.Requester;
                proposed.entry_date = header.RequestedAt;
                proposed.update_user = null;
                proposed.update_date = null;
                _db.PriceBuys.Add(proposed);
            }
            else
            {
                var existing = await _db.PriceBuys.SingleOrDefaultAsync(x => x.id_seq == detail.PriceBuyId, ct)
                    ?? throw new InvalidOperationException($"Tariff ID {detail.PriceBuyId} could no longer be found.");

                var old = JsonSerializer.Deserialize<PriceBuy>(detail.OldValuesJson ?? "{}", JsonOptions)
                    ?? throw new InvalidOperationException("The previous tariff snapshot is invalid.");

                if (MakeConflictKey(existing) != detail.ConflictKey ||
                    !SameSnapshot(existing, old))
                    throw new InvalidOperationException($"Tariff ID {existing.id_seq} has changed since the request was submitted. Please submit a new request.");

                if (detail.OperationType == "DEACTIVATE")
                    existing.active_flag = 0;
                else
                    CopyEditableFields(proposed, existing);

                existing.update_user = username;
                existing.update_date = DateTime.Now;
            }
        }

        // Release pending keys and finalize request in the same transaction.
        foreach (var detail in header.Details)
            detail.PendingConflictKey = null;

        header.Status = "APPROVED";
        header.Approver = username;
        header.DecidedAt = DateTime.Now;
        header.DecisionNote = note;
        header.AppliedAt = DateTime.Now;

        _db.PriceBuyAudits.Add(new PriceBuyAudit
        {
            ApprovalId = header.ApprovalId,
            Action = "APPROVED_APPLIED",
            PerformedBy = username,
            PerformedAt = DateTime.Now,
            Details = note
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RejectAsync(long approvalId, string username, string note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("A rejection reason is required.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var header = await _db.PriceBuyApprovals.Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.ApprovalId == approvalId, ct);

        if (header == null) throw new KeyNotFoundException("The approval request was not found.");
        if (header.Status != "PENDING") throw new InvalidOperationException("The request is not in PENDING status.");

        var changed = await _db.PriceBuyApprovals
            .Where(x => x.ApprovalId == approvalId && x.Status == "PENDING")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, "REJECTED")
                .SetProperty(x => x.Approver, username)
                .SetProperty(x => x.DecidedAt, DateTime.Now)
                .SetProperty(x => x.DecisionNote, note), ct);

        if (changed != 1) throw new InvalidOperationException("The request has already been processed by another user.");

        foreach (var detail in header.Details) detail.PendingConflictKey = null;
        _db.PriceBuyAudits.Add(new PriceBuyAudit
        {
            ApprovalId = approvalId, Action = "REJECTED", PerformedBy = username,
            PerformedAt = DateTime.Now, Details = note
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task CancelAsync(long approvalId, string username, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var header = await _db.PriceBuyApprovals.Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.ApprovalId == approvalId, ct);

        if (header == null) throw new KeyNotFoundException("The approval request was not found.");
        if (!string.Equals(header.Requester, username, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only the original requester can cancel this request.");
        if (header.Status != "PENDING") throw new InvalidOperationException("Only pending requests can be cancelled.");

        var changed = await _db.PriceBuyApprovals
            .Where(x => x.ApprovalId == approvalId && x.Status == "PENDING" && x.Requester == username)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, "CANCELLED")
                .SetProperty(x => x.DecidedAt, DateTime.Now)
                .SetProperty(x => x.DecisionNote, "Cancelled by requester"), ct);

        if (changed != 1) throw new InvalidOperationException("The request has already been processed or cancelled.");

        foreach (var detail in header.Details) detail.PendingConflictKey = null;
        _db.PriceBuyAudits.Add(new PriceBuyAudit
        {
            ApprovalId = approvalId, Action = "CANCELLED", PerformedBy = username,
            PerformedAt = DateTime.Now, Details = "Cancelled by requester"
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ValidateMasterDataAsync(PriceBuy x, CancellationToken ct)
    {
        if (!await _db.Vendors.AnyAsync(c => c.SUP_CODE == x.sup_code, ct))
            throw new InvalidOperationException($"The main customer is invalid: {x.sup_code}");
        if (!await _db.Origins.AnyAsync(l => l.origin_code == x.origin, ct))
            throw new InvalidOperationException($"The origin is invalid: {x.origin}");
        if (!await _db.Destinations.AnyAsync(l => l.destination_code == x.dest, ct))
            throw new InvalidOperationException($"The destination is invalid: {x.dest}");
        if (!await _db.ServiceTypes.AnyAsync(s => s.serv_name == x.serv_type, ct))
            throw new InvalidOperationException($"The service type is invalid: {x.serv_type}");
        if (!await _db.ServiceModas.AnyAsync(s => s.moda_name == x.serv_moda, ct))
            throw new InvalidOperationException($"The service mode is invalid: {x.serv_moda}");
        if (!await _db.TruckSizes.AnyAsync(t => t.trucksize_code == x.truck_size, ct))
            throw new InvalidOperationException($"The truck size is invalid: {x.truck_size}");
        if (!await _db.ChargeUoms.AnyAsync(c => c.charge_name == x.charge_uom, ct))
            throw new InvalidOperationException($"The charge UOM is invalid: {x.charge_uom}");
    }

    private static void ValidateRequiredIdentity(PriceBuy x)
    {
        if (string.IsNullOrWhiteSpace(x.sup_code) || string.IsNullOrWhiteSpace(x.origin) ||
            string.IsNullOrWhiteSpace(x.dest) || string.IsNullOrWhiteSpace(x.serv_type) ||
            string.IsNullOrWhiteSpace(x.serv_moda) || string.IsNullOrWhiteSpace(x.truck_size) ||
            string.IsNullOrWhiteSpace(x.charge_uom))
            throw new InvalidOperationException("All tariff identity fields are required.");
    }

    private static void NormalizeTariffFields(PriceBuy x)
    {
        x.sup_code = x.sup_code?.Trim();
        x.origin = x.origin?.Trim();
        x.dest = x.dest?.Trim();
        x.serv_type = x.serv_type?.Trim();
        x.serv_moda = x.serv_moda?.Trim();
        x.truck_size = x.truck_size?.Trim();
        x.charge_uom = x.charge_uom?.Trim();
        x.curr = x.curr?.Trim();
    }

    private static string Normalize(string? value) => (value ?? "").Trim().ToUpperInvariant();

    private static string MakeConflictKey(PriceBuy x)
    {
        var identity = string.Join("\u001f", new[]
        {
            x.sup_code ?? "", x.origin ?? "", x.dest ?? "", x.serv_type ?? "",
            x.serv_moda ?? "", x.truck_size ?? "", x.charge_uom ?? ""
        }.Select(s => s.Trim().ToUpperInvariant()));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static string IdentityLabel(PriceBuy x) =>
        $"{x.sup_code} | {x.origin} | {x.dest} | {x.serv_type} | {x.serv_moda} | {x.truck_size} | {x.charge_uom}";

    private static bool SameSnapshot(PriceBuy a, PriceBuy b)
    {
        // Compare all persisted business fields; ignore audit fields and identity id.
        return a.sup_code == b.sup_code && a.origin == b.origin && a.dest == b.dest &&
            a.serv_type == b.serv_type && a.serv_moda == b.serv_moda &&
            a.truck_size == b.truck_size && a.charge_uom == b.charge_uom &&
            a.flag_min == b.flag_min && a.charge_min == b.charge_min &&
            a.flag_range == b.flag_range && a.min_range == b.min_range &&
            a.max_range == b.max_range && a.buy1 == b.buy1 && a.buy2 == b.buy2 &&
            a.buy3 == b.buy3 && a.buy_ret_empt == b.buy_ret_empt &&
            a.buy_ret_cargo == b.buy_ret_cargo && a.buy_ovnight == b.buy_ovnight &&
            a.buy_cancel == b.buy_cancel && a.buytrip2 == b.buytrip2 &&
            a.buytrip3 == b.buytrip3 && a.buy_diff_area == b.buy_diff_area &&
            a.valid_date == b.valid_date && a.active_flag == b.active_flag &&
            a.curr == b.curr && a.rate_value == b.rate_value;
    }

    private static void CopyEditableFields(PriceBuy from, PriceBuy to)
    {
        to.flag_min = from.flag_min;
        to.charge_min = from.charge_min;
        to.flag_range = from.flag_range;
        to.min_range = from.min_range;
        to.max_range = from.max_range;
        to.buy1 = from.buy1;
        to.buy2 = from.buy2;
        to.buy3 = from.buy3;
        to.buy_ret_empt = from.buy_ret_empt;
        to.buy_ret_cargo = from.buy_ret_cargo;
        to.buy_ovnight = from.buy_ovnight;
        to.buy_cancel = from.buy_cancel;
        to.buytrip2 = from.buytrip2;
        to.buytrip3 = from.buytrip3;
        to.buy_diff_area = from.buy_diff_area;
        to.valid_date = from.valid_date;
        to.active_flag = from.active_flag;
        to.curr = from.curr;
        to.rate_value = from.rate_value;
    }

    private async Task<string> GenerateRequestNoAsync(CancellationToken ct)
    {
        // Unique index on RequestNo is still required to protect concurrent generation.
        var date = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var prefix = $"PB-{date}-";
        var last = await _db.PriceBuyApprovals
            .Where(x => x.RequestNo.StartsWith(prefix))
            .OrderByDescending(x => x.RequestNo)
            .Select(x => x.RequestNo)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last != null && int.TryParse(last.Substring(prefix.Length), out var n))
            seq = n + 1;

        return prefix + seq.ToString("D4", CultureInfo.InvariantCulture);
    }
}
