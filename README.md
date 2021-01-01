# DotMinecraftLauncher
This is my customized DotMCLauncher
Used in my minecraft launcher.

## Modules
- Base module (`dotMCLauncher`);
- working with launcher profiles (`dotMCLauncher.Profiling`);
- working with version manifest and version metafiles (`dotMCLauncher.Versioning`);
- working with asset indexes (`dotMCLauncher.Resourcing`);
- working with Yggdrasil authentication requests (`dotMCLauncher.Yggdrasil`).
- Creating custom launcher (`DotMinecraftLauncher.Launcher`).
## Requirements

- VS2017/2019,
- **.NET Core 3.1 SDK**.

## To do
- Better null version manifest argument.
- Support FarbicMC version manifest.

# Example
1.Get latest shapshot version.
```cs
JavaEditionLauncher launcher = new JavaEditionLauncher();
// Set where file and assets should download to.
launcher.SetBaseDir(Environment.CurrentDirectory);
// Fetch latest version
launcher.UpdateVersionsList();
Console.WriteLine("Latest version: " + JavaEditionLauncher.VersionList.LatestVersions.Snapshot);
```
2.Simple Minecraft launcher
```cs
JavaEditionLauncher launcher = new JavaEditionLauncher();
launcher.SetBaseDir(Environment.CurrentDirectory);
launcher.UpdateVersionsList();
// Parse launcher_profile.json, this profile format compatible with vannila launcher.
ProfileManager profileManager = ProfileManager.ParseProfile("Profile.json");
Profile gameProfile = profileManager.GetProfile("Default");
            var t = Task.Run(() =>
            {
                launcher.LaunchGameClient(gameProfile,1.16.4 , "ExamplePlayer");
            });

// Wait untill game is launched.
t.Wait();
Console.WriteLine("Success!");
```
Profile.json
```
 {"selectedProfile":"Default","profiles":{"Latest Release":{"name":"Latest Release"},"Latest Snapshot":{"name":"Latest Snapshot","allowedReleaseTypes":["snapshot"]},"Default":{"name":"Default","allowedReleaseTypes":["snapshot","release"]}}}
```
Users.json
```
{"selectedUsername":"ExamplePlayer","users":{"ExamplePlayer":{"username":"ExamplePlayer","type":"offline"}}}
```












## License
dotMinecraftLauncher is licensed under MIT License. See [LICENSE.md](/LICENSE.md).

## Credits
2017 Igor Popov  
2019-2020 sisit135
