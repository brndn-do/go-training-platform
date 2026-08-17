using System.Net;
using System.Net.Http.Headers;

namespace GoTrainingPlatform.Infrastructure.Tests;

public sealed class UnreadableHttpContent : HttpContent
{
  public UnreadableHttpContent()
  {
    Headers.ContentType = new MediaTypeHeaderValue("application/json");
  }

  protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
    throw new IOException("The response ended before the body was read.");

  protected override bool TryComputeLength(out long length)
  {
    length = 0;
    return false;
  }
}
