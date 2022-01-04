open System.Diagnostics
open System

if not Environment.Is64BitOperatingSystem then
    Console.WriteLine("Error: only 64 bit OS supported")
    exit -1

let baseName = IO.DirectoryInfo(Environment.CurrentDirectory).Name
let (rid, exeName) =
    if OperatingSystem.IsLinux() then "linux-x64", baseName
    elif OperatingSystem.IsWindows() then "win-x64", baseName + ".exe"
    else "osx-x64", baseName

// Publish a binary with AOT compilation, so there is little to no JIT overhead polluting the benchmark
Process.Start("dotnet", $"publish -c Release -r {rid} --no-self-contained -p:PublishReadyToRun=true -o bin/Benchmark").WaitForExit()
Process.Start($"bin/Benchmark/{exeName}").WaitForExit()