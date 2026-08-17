using System.Net;
using System.Text;

namespace GoTrainingPlatform.Infrastructure.Tests;

public sealed class FakeHttpMessageHandler(
  Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
  : HttpMessageHandler
{
  public FakeHttpMessageHandler(
    HttpStatusCode statusCode,
    string body,
    string contentType = "application/json",
    Exception? exceptionToThrow = null,
    TimeSpan delay = default)
    : this(async (_, cancellationToken) =>
    {
      if (exceptionToThrow is not null)
      {
        throw exceptionToThrow;
      }

      if (delay != default)
      {
        await Task.Delay(delay, cancellationToken);
      }

      return new HttpResponseMessage(statusCode)
      {
        Content = new StringContent(body, Encoding.UTF8, contentType),
      };
    })
  {
  }

  public FakeHttpMessageHandler(HttpStatusCode statusCode, HttpContent content)
    : this((_, _) => Task.FromResult(new HttpResponseMessage(statusCode) { Content = content }))
  {
  }

  public string? LastRequestBody { get; private set; }

  public HttpRequestMessage? LastRequest { get; private set; }

  public int CallCount { get; private set; }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    CallCount++;
    LastRequest = request;

    if (request.Content is not null)
    {
      LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
    }

    return await respond(request, cancellationToken);
  }
}
