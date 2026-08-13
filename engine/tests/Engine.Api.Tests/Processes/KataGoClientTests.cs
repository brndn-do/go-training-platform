using Engine.Api.Analysis;
using Engine.Api.Processes;
using Engine.Api.Tests.Fakes;

namespace Engine.Api.Tests.Processes;

public sealed class KataGoClientTests
{
  [Fact]
  public async Task QueryAsync_SingleQuery_ReturnsDeserializedResponse()
  {
    TaskCompletionSource<string?> responseTcs = new();
    responseTcs.TrySetResult("""{"id":"test","rootInfo":{"winrate":0.5}}""");

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));

    KataGoResponse response = await client.QueryAsync(query);

    Assert.Equal("test", response.Id);
    Assert.False(response.IsError);
    Assert.NotNull(response.RootInfo);
    Assert.Equal(0.5, response.RootInfo.Winrate);
  }

  [Fact]
  public async Task QueryAsync_SingleQuery_SendsCamelCaseRequest()
  {
    TaskCompletionSource<string?> responseTcs = new();
    responseTcs.TrySetResult("""{"id":"test","rootInfo":{"winrate":0.5}}""");

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));

    await client.QueryAsync(query);

    Assert.Single(fakeIO.RequestsReceived);
    string request = fakeIO.RequestsReceived[0]!;

    // JSON string for the query should use camel case
    Assert.Contains("\"id\":\"test\"", request);
    Assert.DoesNotContain("\"Id\":\"test\"", request);
  }

  [Fact]
  public async Task QueryAsync_ConcurrentQueries_SendsOneAtATime()
  {
    TaskCompletionSource<string?> responseTcs1 = new();
    TaskCompletionSource<string?> responseTcs2 = new();

    FakeKataGoProcessIO fakeIO = new([responseTcs1, responseTcs2]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));

    var task1 = client.QueryAsync(query1);
    var task2 = client.QueryAsync(query2);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    responseTcs1.TrySetResult("""{"id":"test1","rootInfo":{"winrate":0.5}}""");
    responseTcs2.TrySetResult("""{"id":"test2","rootInfo":{"winrate":0.5}}""");

    await Task.WhenAll(task1, task2);

    Assert.Equal(2, fakeIO.RequestsReceived.Length);
  }

  [Fact]
  public async Task QueryAsync_CancelledMidExchange_DoesNotCorruptNextCallersResponse()
  {
    TaskCompletionSource<string?> responseTcs1 = new();
    TaskCompletionSource<string?> responseTcs2 = new();

    FakeKataGoProcessIO fakeIO = new([responseTcs1, responseTcs2]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));

    using CancellationTokenSource callerCts = new();

    var task1 = client.QueryAsync(query1, callerCts.Token);
    var task2 = client.QueryAsync(query2);

    // wait until the worker has dequeued query1 and is blocked on responseTcs1
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    callerCts.Cancel();

    // the caller gives up immediately, without waiting for the real exchange to finish
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task1);

    // query2 must not be dispatched early just because query1's caller gave up
    Assert.Single(fakeIO.RequestsReceived);

    // the real "KataGo response" for query1 finally arrives, even though nobody's listening
    responseTcs1.TrySetResult("""{"id":"test1","rootInfo":{"winrate":0.5}}""");

    // wait until the worker has dequeued query2 and is blocked on responseTcs2
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 2,
      "Timed out waiting for the second request.");

    responseTcs2.TrySetResult("""{"id":"test2","rootInfo":{"winrate":0.6}}""");

    KataGoResponse response2 = await task2;

    Assert.Equal("test2", response2.Id);
    Assert.NotNull(response2.RootInfo);
    Assert.Equal(0.6, response2.RootInfo.Winrate);
  }

  [Fact]
  public async Task QueryAsync_CancelledBeforeExchange_DoesNotProcessQuery()
  {
    TaskCompletionSource<string?> responseTcs1 = new();
    TaskCompletionSource<string?> responseTcs3 = new();

    FakeKataGoProcessIO fakeIO = new([responseTcs1, responseTcs3]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query3 = new("test3", [], 9, 7.5, new BotStrength("Superhuman"));

    using CancellationTokenSource callerCts = new();

    var task1 = client.QueryAsync(query1);
    var task2 = client.QueryAsync(query2, callerCts.Token);
    var task3 = client.QueryAsync(query3);

    // wait until the worker has dequeued query1 and is blocked on responseTcs1
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    Assert.Single(fakeIO.RequestsReceived);

    // query1 is blocked, query2 has not been queued, cancel query2
    callerCts.Cancel();

    // response 1 arrives
    responseTcs1.TrySetResult("""{"id":"test1","rootInfo":{"winrate":0.5}}""");

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task2);

    // wait until worker has dequeued
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 2,
      "Timed out waiting for the third request");

    // should only have received request #1 and #3 since request #2 was cancelled
    // before exchange and not dequeued
    Assert.Equal(2, fakeIO.RequestsReceived.Length);

    // response for request 3 arrives
    responseTcs3.TrySetResult("""{"id":"test3","rootInfo":{"winrate":0.7}}""");

    KataGoResponse response1 = await task1;
    KataGoResponse response3 = await task3;

    Assert.Equal("test1", response1.Id);
    Assert.NotNull(response1.RootInfo);
    Assert.Equal(0.5, response1.RootInfo.Winrate);
    Assert.Equal("test3", response3.Id);
    Assert.NotNull(response3.RootInfo);
    Assert.Equal(0.7, response3.RootInfo.Winrate);
  }

  [Fact]
  public async Task QueryAsync_ProcessIODoesNotRespond_ReturnsErrorResponse()
  {
    TaskCompletionSource<string?> responseTcs = new();
    responseTcs.TrySetResult(null);

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));

    KataGoResponse response = await client.QueryAsync(query);

    Assert.Equal("test", response.Id);
    Assert.True(response.IsError);
    Assert.Equal("KataGo ProcessIO did not respond.", response.Error);
  }

  [Fact]
  public async Task QueryAsync_ProcessIOReturnsNullLiteral_ReturnsErrorResponse()
  {
    TaskCompletionSource<string?> responseTcs = new();
    responseTcs.TrySetResult("null");

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));

    KataGoResponse response = await client.QueryAsync(query);

    Assert.Equal("test", response.Id);
    Assert.True(response.IsError);
    Assert.Equal("KataGo process returned null.", response.Error);
  }

  [Fact]
  public async Task WarmUpAsync_SingleCall_WaitsForProcess()
  {
    TaskCompletionSource warmUpTcs = new();
    FakeKataGoProcessIO fakeIO = new([], warmUpTcs);
    KataGoClient client = new(fakeIO);

    var warmUpTask = client.WarmUpAsync();

    Assert.Equal(1, fakeIO.WarmUpCallCount);
    Assert.False(warmUpTask.IsCompleted);

    warmUpTcs.TrySetResult();
    await warmUpTask;

    Assert.True(warmUpTask.IsCompleted);
  }

  [Fact]
  public async Task WarmUpAsync_AlreadyWarm_ReturnsImmediately()
  {
    TaskCompletionSource warmUpTcs = new();
    FakeKataGoProcessIO fakeIO = new([], warmUpTcs);
    KataGoClient client = new(fakeIO);

    warmUpTcs.TrySetResult();

    var warmUpTask = client.WarmUpAsync();

    Assert.True(warmUpTask.IsCompleted);

    await warmUpTask;
  }

  [Fact]
  public async Task WarmUpAsync_TwoCallsWithOneCancelled_DoesNotAffectOtherCall()
  {
    TaskCompletionSource warmUpTcs = new();
    FakeKataGoProcessIO fakeIO = new([], warmUpTcs);
    KataGoClient client = new(fakeIO);

    CancellationTokenSource cts = new();
    var warmUpTask1 = client.WarmUpAsync(cts.Token);
    var warmUpTask2 = client.WarmUpAsync();

    // cancel call 1 before process is warm
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => warmUpTask1);

    warmUpTcs.TrySetResult();
    await warmUpTask2;

    Assert.True(warmUpTask2.IsCompleted);
  }

  [Fact]
  public async Task WarmUpAsync_ConcurrentQueryCalls_DoesNotGetBlocked()
  {
    TaskCompletionSource warmUpTcs = new();
    TaskCompletionSource<string?> responseTcs = new();
    FakeKataGoProcessIO fakeIO = new([responseTcs], warmUpTcs);
    KataGoClient client = new(fakeIO);

    var queryTask = client.QueryAsync(new("test1", [], 9, 7.5, new BotStrength("Superhuman")));
    var warmUpTask = client.WarmUpAsync();

    Assert.False(queryTask.IsCompleted);
    Assert.False(warmUpTask.IsCompleted);

    warmUpTcs.TrySetResult();
    await warmUpTask;
    Assert.True(warmUpTask.IsCompleted);
    Assert.False(queryTask.IsCompleted);

    responseTcs.TrySetResult("""{"id":"test1","rootInfo":{"winrate":0.5}}""");
    await queryTask;
  }

  [Fact]
  public async Task WarmUpAsync_ExceptionForProcess_Propagates()
  {
    TaskCompletionSource warmUpTcs = new();
    FakeKataGoProcessIO fakeIO = new([], warmUpTcs);
    KataGoClient client = new(fakeIO);

    var warmUpTask = client.WarmUpAsync();

    Assert.False(warmUpTask.IsCompleted);

    Exception thrown = new("test");
    warmUpTcs.TrySetException(thrown);

    Exception caught = await Assert.ThrowsAsync<Exception>(() => warmUpTask);
    Assert.Same(thrown, caught);
  }

  // Note: very likely safe, but increase shutdownGracePeriodMs to be more generous
  // if this test fails on a certain machine
  [Fact]
  public async Task DisposeAsync_InFlightWorkThatFinishesWithinGracePeriod_IsNotCancelled()
  {
    int shutdownGracePeriodMs = 100;

    TaskCompletionSource<string?> responseTcs = new();

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO, shutdownGracePeriodMs);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));
    var task = client.QueryAsync(query);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request");

    Assert.Single(fakeIO.RequestsReceived);

    // shutdown while waiting for ProcessIO response, no await
    Task disposeTask = client.DisposeAsync().AsTask();

    // immediately release responseTcs during grace period
    responseTcs.TrySetResult("""{"id":"test","rootInfo":{"winrate":0.5}}""");

    await disposeTask;

    var result = await task;
    Assert.NotNull(result);
    Assert.Equal("test", result.Id);
  }

  [Fact]
  public async Task DisposeAsync_InFlightWorkThatDoesNotFinishWithinGracePeriod_IsCancelled()
  {
    int shutdownGracePeriodMs = 100;

    TaskCompletionSource<string?> responseTcs = new();

    FakeKataGoProcessIO fakeIO = new([responseTcs]);
    KataGoClient client = new(fakeIO, shutdownGracePeriodMs);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));
    var task = client.QueryAsync(query);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request");

    Assert.Single(fakeIO.RequestsReceived);

    // shutdown, wait until it's complete (grace period has passed)
    await client.DisposeAsync();

    // release responseTcs after grace period
    responseTcs.TrySetResult("""{"id":"test","rootInfo":{"winrate":0.5}}""");

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
  }

  // polls condition every 1ms until it's true, failing with timeoutMessage if it never becomes
  // true within 5 seconds
  private static async Task WaitForConditionAsync(Func<bool> condition, string timeoutMessage)
  {
    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
    while (!condition() && !cts.IsCancellationRequested)
    {
      await Task.Delay(1);
    }

    Assert.True(condition(), timeoutMessage);
  }
}
