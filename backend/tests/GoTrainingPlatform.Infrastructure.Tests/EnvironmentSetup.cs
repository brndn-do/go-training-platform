using System.Runtime.CompilerServices;
using DotNetEnv;

namespace GoTrainingPlatform.Infrastructure.Tests;

public static class EnvironmentSetup
{
  [ModuleInitializer]
  public static void Initialize()
  {
    Env.TraversePath().NoClobber().Load();
  }
}