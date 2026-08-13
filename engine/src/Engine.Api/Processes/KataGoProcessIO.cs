using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Engine.Api.Processes;

/// <summary>
/// Wraps a KataGo analysis-engine process, started and owned for the lifetime of this instance.
/// </summary>
public sealed class KataGoProcessIO : IKataGoProcessIO, IAsyncDisposable
{
  private readonly Process _process;

  private readonly TimeSpan _shutdownGracePeriod;

  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoProcessIO"/> class, starting the
  /// KataGo process immediately.
  /// </summary>
  /// <param name="options">Paths to the KataGo binary, config file, and models.</param>
  /// <param name="shutdownGracePeriodMs">How long <see cref="DisposeAsync"/> waits for a graceful
  /// shutdown of the KataGo process before force-killing.</param>
  public KataGoProcessIO(IOptions<KataGoProcessOptions> options, int shutdownGracePeriodMs = 5000)
  {
    var psi = GetProcessStartInfo(options.Value);
    _process = Process.Start(psi)!;
    _shutdownGracePeriod = TimeSpan.FromMilliseconds(shutdownGracePeriodMs);
  }

  /// <inheritdoc/>
  public async Task<string?> ExchangeAsync(string request, CancellationToken cancellationToken = default)
  {
    await _process.StandardInput.WriteLineAsync(request.AsMemory(), cancellationToken);
    await _process.StandardInput.FlushAsync(cancellationToken);

    string? response = await _process.StandardOutput.ReadLineAsync(cancellationToken);
    return response;
  }

  /// <inheritdoc/>
  public async Task WarmUpAsync(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Kills the KataGo process and releases its resources.
  /// </summary>
  /// <returns>
  /// The completed disposal.
  /// </returns>
  public async ValueTask DisposeAsync()
  {
    if (_process.HasExited)
    {
      _process.Dispose();
      return;
    }

    try
    {
      _process.StandardInput.Close();
    }
    catch
    {
    }

    using var cts = new CancellationTokenSource(_shutdownGracePeriod);
    try
    {
      await _process.WaitForExitAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
    }

    if (!_process.HasExited)
    {
      _process.Kill(entireProcessTree: true);
      await _process.WaitForExitAsync();
    }

    _process.Dispose();
  }

  private static ProcessStartInfo GetProcessStartInfo(KataGoProcessOptions options)
  {
    ProcessStartInfo psi = new()
    {
      FileName = options.BinaryPath,
      RedirectStandardOutput = true,
      RedirectStandardInput = true,
      UseShellExecute = false,
    };

    psi.ArgumentList.Add("analysis");
    psi.ArgumentList.Add("-config");
    psi.ArgumentList.Add(options.ConfigPath);
    psi.ArgumentList.Add("-model");
    psi.ArgumentList.Add(options.ModelPath);
    psi.ArgumentList.Add("-human-model");
    psi.ArgumentList.Add(options.HumanModelPath);

    return psi;
  }
}
