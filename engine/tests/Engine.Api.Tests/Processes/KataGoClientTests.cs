using Engine.Api.Analysis;
using Engine.Api.Processes;
using Engine.Api.Tests.Fakes;

namespace Engine.Api.Tests.Processes;

public sealed class KataGoClientTests
{
  [Fact]
  public async Task QueryAsync_SingleQuery_ReturnsDeserializedResponse()
  {
    string?[] responses = ["""{"id":"test","rootInfo":{"winrate":0.5}}"""];

    TaskCompletionSource<string?> gate = new();
    gate.TrySetResult(null);

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
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
    string?[] responses = ["""{"id":"test","rootInfo":{"winrate":0.5}}"""];

    TaskCompletionSource<string?> gate = new();
    gate.TrySetResult(null);

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
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
    string?[] responses = ["""{"id":"test1","rootInfo":{"winrate":0.5}}""",
      """{"id":"test2","rootInfo":{"winrate":0.5}}"""];

    TaskCompletionSource<string?> gate1 = new();
    TaskCompletionSource<string?> gate2 = new();

    FakeKataGoProcessIO fakeIO = new(responses, [gate1, gate2]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));

    var task1 = client.QueryAsync(query1);
    var task2 = client.QueryAsync(query2);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    gate1.TrySetResult(null);
    gate2.TrySetResult(null);

    await Task.WhenAll(task1, task2);

    Assert.Equal(2, fakeIO.RequestsReceived.Length);
  }

  [Fact]
  public async Task QueryAsync_CancelledMidExchange_DoesNotCorruptNextCallersResponse()
  {
    string?[] responses = ["""{"id":"test1","rootInfo":{"winrate":0.5}}""",
      """{"id":"test2","rootInfo":{"winrate":0.6}}"""];

    TaskCompletionSource<string?> gate1 = new();
    TaskCompletionSource<string?> gate2 = new();

    FakeKataGoProcessIO fakeIO = new(responses, [gate1, gate2]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));

    using CancellationTokenSource callerCts = new();

    var task1 = client.QueryAsync(query1, callerCts.Token);
    var task2 = client.QueryAsync(query2);

    // wait until the worker has dequeued query1 and is blocked on gate1
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    callerCts.Cancel();

    // the caller gives up immediately, without waiting for the real exchange to finish
    await Assert.ThrowsAsync<TaskCanceledException>(() => task1);

    // query2 must not be dispatched early just because query1's caller gave up
    Assert.Single(fakeIO.RequestsReceived);

    // the real "KataGo response" for query1 finally arrives, even though nobody's listening
    gate1.TrySetResult(null);

    // wait until the worker has dequeued query2 and is blocked on gate2
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 2,
      "Timed out waiting for the second request.");

    gate2.TrySetResult(null);

    KataGoResponse response2 = await task2;

    Assert.Equal("test2", response2.Id);
    Assert.NotNull(response2.RootInfo);
    Assert.Equal(0.6, response2.RootInfo.Winrate);
  }

  [Fact]
  public async Task QueryAsync_CancelledBeforeExchange_DoesNotProcessQuery()
  {
    string?[] responses = ["""{"id":"test1","rootInfo":{"winrate":0.5}}""",
      """{"id":"test3","rootInfo":{"winrate":0.7}}"""];

    TaskCompletionSource<string?> gate1 = new();
    TaskCompletionSource<string?> gate3 = new();

    FakeKataGoProcessIO fakeIO = new(responses, [gate1, gate3]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query1 = new("test1", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query2 = new("test2", [], 9, 7.5, new BotStrength("Superhuman"));
    KataGoQuery query3 = new("test3", [], 9, 7.5, new BotStrength("Superhuman"));

    using CancellationTokenSource callerCts = new();

    var task1 = client.QueryAsync(query1);
    var task2 = client.QueryAsync(query2, callerCts.Token);
    var task3 = client.QueryAsync(query3);

    // wait until the worker has dequeued query1 and is blocked on gate1
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request.");

    Assert.Single(fakeIO.RequestsReceived);

    // query1 is blocked, query2 has not been queued, cancel query2
    callerCts.Cancel();

    // response 1 arrives
    gate1.TrySetResult(null);

    await Assert.ThrowsAsync<TaskCanceledException>(() => task2);

    // wait until worker has dequeued
    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 2,
      "Timed out waiting for the third request");

    // should only have received request #1 and #3 since request #2 was cancelled
    // before exchange and not dequeued
    Assert.Equal(2, fakeIO.RequestsReceived.Length);

    // response for request 3 arrives
    gate3.TrySetResult(null);

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
    string?[] responses = [null];

    TaskCompletionSource<string?> gate = new();
    gate.TrySetResult(null);

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
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
    string?[] responses = ["null"];

    TaskCompletionSource<string?> gate = new();
    gate.TrySetResult(null);

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
    KataGoClient client = new(fakeIO);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));

    KataGoResponse response = await client.QueryAsync(query);

    Assert.Equal("test", response.Id);
    Assert.True(response.IsError);
    Assert.Equal("KataGo process returned null.", response.Error);
  }

  // Note: very likely safe, but increase shutdownGracePeriodMs to be more generous
  // if this test fails on a certain machine
  [Fact]
  public async Task DisposeAsync_InFlightWorkThatFinishesWithinGracePeriod_IsNotCancelled()
  {
    int shutdownGracePeriodMs = 100;

    string?[] responses = ["""{"id":"test","rootInfo":{"winrate":0.5}}"""];

    TaskCompletionSource<string?> gate = new();

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
    KataGoClient client = new(fakeIO, shutdownGracePeriodMs);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));
    var task = client.QueryAsync(query);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request");

    Assert.Single(fakeIO.RequestsReceived);

    // shutdown while waiting for ProcessIO response, no await
    Task disposeTask = client.DisposeAsync().AsTask();

    // immediately release gate during grace period
    gate.TrySetResult(null);

    await disposeTask;

    var result = await task;
    Assert.NotNull(result);
    Assert.Equal("test", result.Id);
  }

  [Fact]
  public async Task DisposeAsync_InFlightWorkThatDoesNotFinishWithinGracePeriod_IsCancelled()
  {
    int shutdownGracePeriodMs = 100;

    string?[] responses = ["""{"id":"test","rootInfo":{"winrate":0.5}}"""];

    TaskCompletionSource<string?> gate = new();

    FakeKataGoProcessIO fakeIO = new(responses, [gate]);
    KataGoClient client = new(fakeIO, shutdownGracePeriodMs);

    KataGoQuery query = new("test", [], 9, 7.5, new BotStrength("Superhuman"));
    var task = client.QueryAsync(query);

    await WaitForConditionAsync(
      () => fakeIO.RequestsReceived.Length >= 1,
      "Timed out waiting for the first request");

    Assert.Single(fakeIO.RequestsReceived);

    // shutdown, wait until it's complete (grace period has passed)
    await client.DisposeAsync();

    // release gate after grace period
    gate.TrySetResult(null);

    await Assert.ThrowsAsync<TaskCanceledException>(() => task);
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
