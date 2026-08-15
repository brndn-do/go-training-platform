using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Engine.Api.Processes;

/// <summary>
/// Wraps a KataGo analysis-engine process, started and owned for the lifetime of this instance.
/// </summary>
public sealed class KataGoProcessIO : IKataGoProcessIO, IAsyncDisposable
{
  private const string ReadyMessage = "Started, ready to begin handling requests";

  private readonly Process _process;

  private readonly TimeSpan _shutdownGracePeriod;

  private readonly TaskCompletionSource _processReadyTcs;

  private readonly CancellationTokenSource _processExitedCts;

  private bool _disposed;

  /// <summary>
  /// Initializes a new instance of the <see cref="KataGoProcessIO"/> class, starting the
  /// KataGo process immediately.
  /// </summary>
  /// <param name="options">Paths to the KataGo binary, config file, and models, plus the
  /// grace period <see cref="DisposeAsync"/> waits for a graceful shutdown of the KataGo
  /// process before force-killing.</param>
  public KataGoProcessIO(IOptions<KataGoProcessOptions> options)
  {
    var psi = GetProcessStartInfo(options.Value);
    _process = Process.Start(psi)!;
    _process.EnableRaisingEvents = true;
    _processExitedCts = new();

    _process.Exited += (_, e) => _processExitedCts.Cancel();

    try
    {
      _shutdownGracePeriod = TimeSpan.FromMilliseconds(options.Value.ProcessShutdownGracePeriodMs);
      _processReadyTcs = new();

      // listen for ready message
      _process.ErrorDataReceived += (_, e) =>
      {
        if (e.Data != null && e.Data.Contains(ReadyMessage))
        {
          _processReadyTcs.TrySetResult();
        }
      };

      _process.BeginErrorReadLine();
    }
    catch
    {
      // if anything fails after the KataGo binary has started, kill it manually
      // remember garbage collection doesn't handle this
      _process.Kill(entireProcessTree: true);
      throw;
    }
  }

  /// <inheritdoc/>
  public bool HasLoaded => _processReadyTcs.Task.IsCompletedSuccessfully;

  /// <inheritdoc/>
  public bool HasExited => _disposed || _process.HasExited;

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
    ObjectDisposedException.ThrowIf(_disposed, this);

    try
    {
      await _processReadyTcs.Task.WaitAsync(cancellationToken).WaitAsync(_processExitedCts.Token);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == _processExitedCts.Token)
    {
      throw new InvalidOperationException("The process has exited.");
    }
  }

  /// <summary>
  /// Kills the KataGo process and releases its resources.
  /// </summary>
  /// <returns>
  /// The completed disposal.
  /// </returns>
  public async ValueTask DisposeAsync()
  {
    if (_disposed)
    {
      return;
    }

    if (!_process.HasExited)
    {
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
        // still has not exited
        _process.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync();
      }
    }

    _process.Dispose();
    _processExitedCts.Dispose();
    _disposed = true;
  }

  private static ProcessStartInfo GetProcessStartInfo(KataGoProcessOptions options)
  {
    ProcessStartInfo psi = new()
    {
      FileName = options.BinaryPath,
      RedirectStandardOutput = true,
      RedirectStandardInput = true,
      RedirectStandardError = true,
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
