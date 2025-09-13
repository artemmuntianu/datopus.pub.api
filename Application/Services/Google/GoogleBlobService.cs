using System.Net;
using datopus.Application.DTOs;
using datopus.Application.Exceptions;
using datopus.Core.Entities;
using Google;
using Google.Cloud.Storage.V1;

public interface IGoogleBlobStorageService
{
    Task SaveSnapshotsAsync(RRWebEventPayloadDTO snapshots);

    Task<List<string>> FetchSnapshots(DateRange range, string measurementId, string sessionId);
}

namespace datopus.Application.Services
{
    public class GcsBlobStorageService : IGoogleBlobStorageService
    {
        private readonly StorageClient _client;
        private readonly string _bucketName;
        private readonly ILogger<GcsBlobStorageService> _logger;

        public GcsBlobStorageService(
            IGoogleCredentialProvider credentialProvider,
            string bucketName,
            ILogger<GcsBlobStorageService> logger
        )
        {
            _client = StorageClient.Create(credentialProvider.GetCredential());
            _bucketName = bucketName;
            _logger = logger;
        }

        public async Task SaveSnapshotsAsync(RRWebEventPayloadDTO snapshots)
        {
            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
            }
            if (string.IsNullOrWhiteSpace(snapshots.MeasurementId))
            {
                throw new ArgumentException(
                    "MeasurementId cannot be empty in payload.",
                    nameof(snapshots.MeasurementId)
                );
            }
            if (string.IsNullOrWhiteSpace(snapshots.SessionId))
            {
                throw new ArgumentException(
                    "SessionId cannot be empty in payload.",
                    nameof(snapshots.SessionId)
                );
            }
            if (
                snapshots.RRWebEventsDictionaryPerDate == null
                || !snapshots.RRWebEventsDictionaryPerDate.Any()
            )
            {
                _logger.LogInformation(
                    "No snapshot data provided in the payload for session {SessionId}.",
                    snapshots.SessionId
                );
                return;
            }

            IEnumerable<RrwebSnapshotBlob>? snapshotBlobs;
            try
            {
                snapshotBlobs = MapRRWebEventsToBlob(snapshots);
                if (snapshotBlobs == null || !snapshotBlobs.Any())
                {
                    _logger.LogWarning(
                        "Mapping resulted in no valid snapshot blobs to save for session {SessionId}.",
                        snapshots.SessionId
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to map RRWeb events to blob format for session {SessionId}",
                    snapshots.SessionId
                );
                throw new BlobDataFormatException(
                    $"Failed to process snapshot data for saving: {ex.Message}",
                    ex
                );
            }

            foreach (var snapshot in snapshotBlobs)
            {
                if (snapshot?.CompressedData == null || snapshot.CompressedData.Length == 0)
                {
                    _logger.LogWarning(
                        "Skipping empty or invalid snapshot blob data for date {Date}, session {SessionId}",
                        snapshot?.Date,
                        snapshot?.SessionId
                    );
                    continue;
                }

                string datePath = $"{snapshot.Date:yyyy/MM/dd}";
                string fileName = $"{Guid.NewGuid()}.bin";
                string objectPath =
                    $"measurements/{snapshot.MeasurementId}/date/{datePath}/sessions/{snapshot.SessionId}/snapshots/{fileName}";

                try
                {
                    using var stream = new MemoryStream(snapshot.CompressedData);

                    await _client.UploadObjectAsync(
                        _bucketName,
                        objectPath,
                        "application/octet-stream",
                        stream
                    );

                    _logger.LogInformation(
                        "Uploaded snapshot for session {SessionId} at {ObjectPath}",
                        snapshot.SessionId,
                        objectPath
                    );
                }
                catch (GoogleApiException ex)
                {
                    _logger.LogError(
                        ex,
                        "Google API error uploading snapshot for session {SessionId} to {ObjectPath}. Status: {StatusCode}",
                        snapshot.SessionId,
                        objectPath,
                        ex.HttpStatusCode
                    );

                    throw new BlobAccessException(
                        $"Failed to upload snapshot to {objectPath} due to Google API error: {ex.Message} (Status: {ex.HttpStatusCode})",
                        ex
                    );
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(
                        ioEx,
                        "I/O error uploading snapshot for session {SessionId} to {ObjectPath}",
                        snapshot.SessionId,
                        objectPath
                    );
                    throw new BlobAccessException(
                        $"Failed to upload snapshot to {objectPath} due to I/O error: {ioEx.Message}",
                        ioEx
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error uploading snapshot for session {SessionId} to {ObjectPath}",
                        snapshot.SessionId,
                        objectPath
                    );
                    throw new BlobAccessException(
                        $"An unexpected error occurred uploading snapshot to {objectPath}",
                        ex
                    );
                }
            }
        }

        public async Task<List<string>> FetchSnapshots(
            DateRange range,
            string measurementId,
            string sessionId
        )
        {
            if (range == null)
                throw new ArgumentNullException(nameof(range));
            if (string.IsNullOrWhiteSpace(measurementId))
                throw new ArgumentException(
                    "Measurement ID cannot be empty.",
                    nameof(measurementId)
                );
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be empty.", nameof(sessionId));

            var datesList = range.GetDatesInRange();
            var resultList = new List<string>();
            bool anyBlobFound = false;

            var objectPaths = datesList.Select(
                (date) =>
                    $"measurements/{measurementId}/date/{date:yyyy/MM/dd}/sessions/{sessionId}/snapshots/"
            );

            foreach (var path in objectPaths)
            {
                try
                {
                    var objects = _client.ListObjectsAsync(_bucketName, path);
                    bool foundInPath = false;

                    await foreach (var obj in objects)
                    {
                        foundInPath = true;
                        anyBlobFound = true;
                        try
                        {
                            using var memoryStream = new MemoryStream();
                            await _client.DownloadObjectAsync(_bucketName, obj.Name, memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            List<string> base64Events = ExtractBase64EventsFromBlob(memoryStream);
                            resultList.AddRange(base64Events);
                        }
                        catch (GoogleApiException ex)
                            when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning(
                                ex,
                                "Blob object {ObjectName} listed but not found during download attempt in bucket {BucketName}.",
                                obj.Name,
                                _bucketName
                            );
                            continue;
                        }
                        catch (GoogleApiException ex)
                        {
                            _logger.LogError(
                                ex,
                                "Google API error downloading blob object {ObjectName} from bucket {BucketName}.",
                                obj.Name,
                                _bucketName
                            );
                            throw new BlobAccessException(
                                $"Failed to download blob: {obj.Name}",
                                ex
                            );
                        }
                        catch (BlobDataFormatException ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to parse data format for blob {ObjectName} from bucket {BucketName}.",
                                obj.Name,
                                _bucketName
                            );
                            throw;
                        }
                        catch (IOException ex)
                        {
                            _logger.LogError(
                                ex,
                                "I/O error processing blob {ObjectName} from bucket {BucketName}.",
                                obj.Name,
                                _bucketName
                            );
                            throw new BlobAccessException(
                                $"I/O error processing blob: {obj.Name}",
                                ex
                            );
                        }
                    }
                    if (!foundInPath)
                    {
                        _logger.LogInformation(
                            "No blob objects found for path prefix {PathPrefix} in bucket {BucketName}.",
                            path,
                            _bucketName
                        );
                    }
                }
                catch (GoogleApiException ex)
                {
                    _logger.LogError(
                        ex,
                        "Google API error listing objects with prefix {PathPrefix} in bucket {BucketName}.",
                        path,
                        _bucketName
                    );
                    throw new BlobAccessException($"Failed to list blobs for path: {path}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error processing path prefix {PathPrefix} in bucket {BucketName}.",
                        path,
                        _bucketName
                    );
                    throw;
                }
            }

            if (!anyBlobFound && datesList.Any())
            {
                _logger.LogWarning(
                    "No blob snapshots found for the entire date range and specified IDs."
                );
            }

            return resultList;
        }

        private static IEnumerable<RrwebSnapshotBlob> MapRRWebEventsToBlob(
            RRWebEventPayloadDTO payload
        )
        {
            return payload.RRWebEventsDictionaryPerDate?.Select(
                    (entry) =>
                    {
                        using var memoryStream = new MemoryStream();
                        var date = entry.Key;
                        string[] compressedEvents = entry.Value;

                        foreach (var item in compressedEvents)
                        {
                            var compressedBytes = Convert.FromBase64String(item);
                            var lengthPrefix = BitConverter.GetBytes(compressedBytes.Length);

                            memoryStream.Write(lengthPrefix, 0, lengthPrefix.Length);
                            memoryStream.Write(compressedBytes, 0, compressedBytes.Length);
                        }

                        return new RrwebSnapshotBlob
                        {
                            Date = DateOnly.FromDateTime(date),
                            CompressedData = memoryStream.ToArray(),
                            MeasurementId = payload.MeasurementId!,
                            SessionId = payload.SessionId!,
                        };
                    }
                ) ?? [];
        }

        private static List<string> ExtractBase64EventsFromBlob(MemoryStream stream)
        {
            var result = new List<string>();

            try
            {
                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < 4)
                    {
                        throw new BlobDataFormatException(
                            "Insufficient data remaining for length prefix."
                        );
                    }

                    var lengthBuffer = new byte[4];
                    int bytesRead = stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead < 4)
                    {
                        throw new BlobDataFormatException(
                            "Could not read full 4-byte length prefix."
                        );
                    }

                    int length = BitConverter.ToInt32(lengthBuffer, 0);

                    if (length < 0 || length > (stream.Length - stream.Position))
                    {
                        throw new BlobDataFormatException(
                            $"Invalid event length prefix detected: {length}. Remaining stream length: {stream.Length - stream.Position}"
                        );
                    }
                    if (length == 0)
                        continue;

                    var eventBytes = new byte[length];
                    bytesRead = stream.Read(eventBytes, 0, length);

                    if (bytesRead < length)
                    {
                        throw new BlobDataFormatException(
                            $"Expected to read {length} bytes for event, but only read {bytesRead}. Stream ended prematurely."
                        );
                    }

                    result.Add(Convert.ToBase64String(eventBytes));
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new BlobDataFormatException(
                    "Unexpected end of stream while reading blob data.",
                    ex
                );
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new BlobDataFormatException(
                    "Data format error encountered during processing.",
                    ex
                );
            }

            return result;
        }
    }
}
