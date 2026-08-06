using System.Diagnostics;

namespace Engine.Api;

class Program()
{
  const string KataGoPath = "../../katago/katago";
  const string ConfigPath = "../../config/go_training_platform_config.cfg";
  const string ModelPath = "../../models/kata1-zhizi-b40c768nbt-s11272M-d5935M.bin.gz";
  const string HumanModelPath = "../../models/b18c384nbt-humanv0.bin.gz";

  const string query = """{"id":"test-humansl-1","moves":[["B","E5"],["W","C3"],["B","G3"]],"rules":"chinese","komi":7.5,"boardXSize":19,"boardYSize":19,"includePolicy":true,"overrideSettings":{"humanSLProfile":"rank_5k"}}""";

  private static ProcessStartInfo GetProcessStartInfo()
  {
    ProcessStartInfo psi = new()
    {
      FileName = KataGoPath,
      RedirectStandardOutput = true,
      RedirectStandardInput = true,
      UseShellExecute = false,
    };

    psi.ArgumentList.Add("analysis");
    psi.ArgumentList.Add("-config");
    psi.ArgumentList.Add(ConfigPath);
    psi.ArgumentList.Add("-model");
    psi.ArgumentList.Add(ModelPath);
    psi.ArgumentList.Add("-human-model");
    psi.ArgumentList.Add(HumanModelPath);

    return psi;
  }

  static void Main()
  {
    var psi = GetProcessStartInfo();
    using Process process = Process.Start(psi)!;

    process.StandardInput.WriteLine(query);
    process.StandardInput.Flush();

    string? response = process.StandardOutput.ReadLine();
    Console.WriteLine(response);

    process.Kill(entireProcessTree: true);
    process.WaitForExit();
  }
}
