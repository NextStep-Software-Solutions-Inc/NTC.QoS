namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

/// <summary>
/// Lightweight signal used to derive speedtests list cache keys from the data itself
/// rather than from explicit invalidation calls. Count catches deletes that don't move
/// the max timestamp (e.g. a hard delete of a row that wasn't the most recently touched
/// one); LastModifiedAt catches inserts and updates. Either changing produces a new
/// cache key automatically, regardless of whether the write came through this API,
/// a job, or a raw SQL statement.
/// </summary>
public record SpeedtestsCacheVersionDto(int Count, DateTime LastModifiedAt);
