using XtcSystems.KsefScenarioCheck.Core.Contracts;

namespace XtcSystems.KsefScenarioCheck.Cli;

internal static class BoundedFile
{
    public static async Task<byte[]> ReadAllBytesAsync(
        string path,
        int maximumBytes,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using var output = new MemoryStream(capacity: Math.Min(maximumBytes, 81_920));
            var buffer = new byte[81_920];
            while (true)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (output.Length + read > maximumBytes)
                {
                    throw new ContractException(errorCode, "The selected file exceeds the accepted size limit.");
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            return output.ToArray();
        }
        catch (ContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw new ContractException(errorCode, "The selected file could not be read safely.");
        }
    }
}
