using System.Runtime.CompilerServices;
using DotNetEnv;

namespace Engine.Api.Tests;

public static class EnvironmentSetup
{
  [ModuleInitializer]
  public static void Initialize()
  {
    Env.TraversePath().NoClobber().Load();
  }
}