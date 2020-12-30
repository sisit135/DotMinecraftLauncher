#region License

/*
Copyright  2017-2021 sisit135

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion License

using DotMinecraftLauncher.Networking;
using DotMinecraftLauncher.Profiling;
using DotMinecraftLauncher.Resourcing;
using DotMinecraftLauncher.Versioning;
using Ionic.Zip;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace DotMinecraftLauncher.Launcher
{
    public class JavaEditionLauncher
    {
        public static RawVersionListManifest VersionList;
        private static UserManager _userManager;
        private static User _selectedUser;
        private readonly Dictionary<string, Tuple<string, DateTime>> _nicknameDictionary = new Dictionary<string, Tuple<string, DateTime>>();

        //private Profile _selectedProfile;
        private string _versionToLaunch;


        public bool DoRestoreVersion = false;

        private string BaseDir;
        private string AssetDir;
        private string LibDir;
        private string VersionsDir;

        //Logging
        //private static StringBuilder gameOutputStringBuilder = new StringBuilder();
        StringBuilder outputInfoBuilder = new StringBuilder();
        StringBuilder outputErrorBuilder = new StringBuilder();

        public void SetBaseDir(string dir)
        {
            BaseDir = dir;
            AssetDir = dir + "\\assets";
            LibDir = dir + "\\libraries";
            //LibDir = Path.Combine(dir, "libraries");
            VersionsDir = dir + "\\versions";
        }

        public void LaunchGameClient(Profile profile, string versionToLaunch, string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentNullException("playerName is empty");
            }
            _versionToLaunch = versionToLaunch;
            DownloadVersion(versionToLaunch ?? (profile.SelectedVersion ?? GetLatestVersion(profile)));
            //UpdateVersionListView();
            //string libraries = string.Empty;
            string libraries = CheckLibraries(profile);
            DownloadAssets(profile);

            if (DoRestoreVersion)
            {
                Debug.WriteLine($@"Successfully restored ""{_versionToLaunch}"" version.");
                DoRestoreVersion = false;
                //SetControlBlockState(false);
                //UpdateVersionListView();
                versionToLaunch = null;
                return;
            }
            if (!_userManager.Accounts.ContainsKey(playerName))
            {
                User user = new User
                {
                    Username = playerName,
                    Type = "offline"
                };
                _userManager.Accounts.Add(user.Username, user);
                _selectedUser = user;
            }
            else
            {
                _selectedUser = _userManager.Accounts[playerName];
                if (_selectedUser.Type != "offline")
                {
                    AuthManager am = new AuthManager
                    {
                        ClientToken = _selectedUser.ClientToken,
                        AccessToken = _selectedUser.AccessToken,
                        Uuid = _selectedUser.Uuid
                    };
                    bool check = am.Validate();
                    if (!check)
                    {
                        //invalid session
                        User user = new User
                        {
                            Username = playerName,
                            Type = "offline"
                        };
                        _selectedUser = user;
                    }
                    else
                    {
                        Refresh refresh = new Refresh(_selectedUser.AccessToken, _selectedUser.ClientToken);
                        refresh = (Refresh)refresh.DoPost();
                        _selectedUser.UserProperties = (JArray)refresh.User?["properties"];
                        _selectedUser.AccessToken = refresh.AccessToken;
                        _userManager.Accounts[playerName] = _selectedUser;
                    }
                }
            }
            _userManager.SelectedUsername = _selectedUser.Username;
            //SaveUsers();
            UpdateUserList();

            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(Path.Combine(VersionsDir, _versionToLaunch ?? (
                    profile.SelectedVersion ?? GetLatestVersion(profile)))));
            JObject properties = new JObject {
                            new JProperty("freelauncher", new JArray("cheeki_breeki_iv_damke"))
                        };
            if (profile.ConnectionSettigs != null)
            {
                selectedVersionManifest.ArgCollection.Add("server",
                    profile.ConnectionSettigs.ServerIp);
                selectedVersionManifest.ArgCollection.Add("port",
                    profile.ConnectionSettigs.ServerPort.ToString());
            }
            string javaArguments = profile.JavaArguments == null
                ? string.Empty
                : profile.JavaArguments + " ";
            if (profile.WorkingDirectory != null &&
                !Directory.Exists(profile.WorkingDirectory))
            {
                Directory.CreateDirectory(profile.WorkingDirectory);
            }
            string username;
            if (_selectedUser.Type != "offline")
            {
                while (true)
                {
                    try
                    {
                        if (_nicknameDictionary.ContainsKey(_selectedUser.Uuid) && _nicknameDictionary[_selectedUser.Uuid].Item2 > DateTime.Now)
                        {
                            username = _nicknameDictionary[_selectedUser.Uuid].Item1;
                            break;
                        }
                        if (_nicknameDictionary.ContainsKey(_selectedUser.Uuid) && _nicknameDictionary[_selectedUser.Uuid].Item2 <= DateTime.Now)
                        {
                            _nicknameDictionary.Remove(_selectedUser.Uuid);
                        }
                        _nicknameDictionary.Add(_selectedUser.Uuid, new Tuple<string, DateTime>(
                            new Username
                            {
                                Uuid = _selectedUser.Uuid
                            }.GetUsernameByUuid(),
                            DateTime.Now.AddMinutes(30)));
                        username = _nicknameDictionary[_selectedUser.Uuid].Item1;
                        break;
                    }
                    catch (WebException ex)
                    {
                        if ((int)((HttpWebResponse)ex.Response).StatusCode != 429)
                        {
                            //throw new Exception($"An unhandled exception has occured while getting username by UUID:{Environment.NewLine}{ex}");
                            username = playerName;
                            break;
                        }
                        Thread.Sleep(10000);
                    }
                }
            }
            else
            {
                username = playerName;
            }
            Dictionary<string, string> gameArgumentDictionary = new Dictionary<string, string> {
                            {
                                "auth_player_name", username
                            }, {
                                "version_name", profile.ProfileName
                            }, {
                                "game_directory",
                                profile.WorkingDirectory ?? BaseDir
                            }, {
                                "assets_root", Path.Combine(BaseDir, "assets")
                            }, {
                                "game_assets", Path.Combine(BaseDir, "assets", "virtual", "legacy")
                            }, {
                                "assets_index_name", selectedVersionManifest.GetAssetsIndex()
                            }, {
                                "version_type", selectedVersionManifest.ReleaseType
                            }, {
                                "auth_session", $"token:{_selectedUser.ClientToken}:{_selectedUser.Uuid}" ?? "token:sample_token:sample_token"
                            }, {
                                "auth_access_token", _selectedUser.AccessToken ?? "sample_token"
                            }, {
                                "auth_uuid", _selectedUser.Uuid ?? "sample_token"
                            }, {
                                "user_properties",
                                _selectedUser.UserProperties?.ToString(Newtonsoft.Json.Formatting.None) ??
                                properties.ToString(Newtonsoft.Json.Formatting.None)
                            }, {
                                "user_type", _selectedUser.Type
                            }
                        };
            Dictionary<string, string> jvmArgumentDictionary = new Dictionary<string, string> {
                            {
                                "natives_directory", Path.Combine(BaseDir, "natives")
                            }, {
                                "launcher_name", "DotMinecraftLauncher"
                            }, {
                                "launcher_version", "1.0.0"
                            }, {
                                "classpath", libraries.Contains(' ') ? $"\"{libraries}\"" : libraries
                            }
                        };
            string gameArguments, jvmArguments;
            if (selectedVersionManifest.Type == VersionManifestType.V2)
            {
                List<Rule> requiredRules = new List<Rule> {
                                new Rule {
                                    Action = "allow",
                                    Os = new Os {
                                        Name = "windows"
                                    }
                                }
                            };
                if (new ComputerInfo().OSFullName.ToUpperInvariant().Contains("WINDOWS 10"))
                {
                    requiredRules.Add(new Rule
                    {
                        Action = "allow",
                        Os = new Os
                        {
                            Name = "windows",
                            Version = "^10\\."
                        }
                    });
                }
                if (profile.WindowInfo != null && (profile.WindowInfo.Width != 854 || profile.WindowInfo.Height != 480))
                {
                    requiredRules.Add(new Rule
                    {
                        Action = "allow",
                        Features = new Features
                        {
                            IsForCustomResolution = true
                        }
                    });
                    gameArgumentDictionary.Add("resolution_width",
                        profile.WindowInfo?.Width.ToString());
                    gameArgumentDictionary.Add("resolution_height",
                        profile.WindowInfo?.Height.ToString());
                }
                gameArguments =
                    selectedVersionManifest.BuildArgumentsByGroup(ArgumentsGroupType.GAME, gameArgumentDictionary, requiredRules.ToArray());
                jvmArguments = selectedVersionManifest.BuildArgumentsByGroup(ArgumentsGroupType.JVM, jvmArgumentDictionary, requiredRules.ToArray());
            }
            else
            {
                string nativesPath = Path.Combine(BaseDir, "natives");
                nativesPath = nativesPath.Contains(' ') ? $@"""{nativesPath}""" : nativesPath;
                gameArguments = selectedVersionManifest.ArgCollection.ToString(gameArgumentDictionary);
                jvmArguments = javaArguments +
                    $"-Djava.library.path={nativesPath} -cp {(libraries.Contains(' ') ? $@"""{libraries}""" : libraries)}";
            }
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = true,
                FileName = profile.JavaExecutable ?? Environment.CurrentDirectory + "\\runtimes\\jre-x64\\bin\\java.exe",
                //FileName = BaseDir + "\\runtimes\\jre-x64\\bin\\java.exe",
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = profile.WorkingDirectory ?? BaseDir,
                Arguments =
                    $"{jvmArguments} {selectedVersionManifest.MainClass} {gameArguments}"
            };
            //AppendLog($"Command line executed: \"{proc.FileName}\" {proc.Arguments}")
            Debug.WriteLine(($"Command line executed: \"{proc.FileName}\" {proc.Arguments}"));
            Process mcProcess = new Process
            {
                StartInfo = proc,
            };
            /*mcProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                // Prepend line numbers to each line of the output.
                if (!String.IsNullOrEmpty(e.Data))
                {
                    gameOutputStringBuilder.Append("\n[Info]: " + e.Data);
                }
            });*/
            mcProcess.OutputDataReceived +=
                (sender, args) =>
                {
                    lock (this)
                    {
                        outputInfoBuilder.Append(
                            $"{(outputInfoBuilder.Length == 0 ? string.Empty : Environment.NewLine)}[I] {args.Data}");
                        Debug.WriteLine(outputInfoBuilder.ToString());
                    }

                   
                };
            mcProcess.ErrorDataReceived +=
                (sender, args) =>
                {
                    lock (this)
                    {
                        outputErrorBuilder.Append(
                            $"{(outputErrorBuilder.Length == 0 ? string.Empty : Environment.NewLine)}[E] {args.Data}");
                        Debug.WriteLine(outputErrorBuilder.ToString());
                    }
                };


            mcProcess.Start();
            _versionToLaunch = null;
        }

        //Ok
        public void DownloadVersion(string version)
        {
            string filename;
            WebClient downloader = new WebClient();
            downloader.DownloadProgressChanged += (_, e) =>
            {
                //StatusBarValue = e.ProgressPercentage;
                Debug.WriteLine(e.ProgressPercentage);
            };
            //SetStatusBarVisibility(true);
            //SetStatusBarMaxValue(100);
            //StatusBarValue = 0;
            //UpdateStatusBarText(string.Format(_configuration.Localization.CheckingVersionAvailability, version));
            //AppendLog($"Checking '{version}' version availability...");
            string path = Path.Combine(VersionsDir, version + @"\");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!File.Exists($@"{path}\{version}.json") || DoRestoreVersion)
            {
                if (true)
                {
                    filename = version + ".json";
                    //UpdateStatusBarAndLog($"Downloading {filename}...", new StackFrame().GetMethod().Name);
                    downloader.DownloadFileTaskAsync(
                        new Uri(VersionList.GetVersion(version)?.ManifestUrl ?? string.Format(
                            "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/{0}.json", version)),
                        string.Format(@"{0}\{1}\{1}.json", VersionsDir, version)).Wait();
                }
                else
                {
                    //AppendException($"Unable to download version {version}: offline-mode enabled.");
                    return;
                }
            }
            //StatusBarValue = 0;
            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(VersionsDir + @"\" + version), false);
            if ((!File.Exists($"{path}/{version}.jar") || DoRestoreVersion) &&
                selectedVersionManifest.InheritsFrom == null)
            {
                if (true)
                {
                    filename = version + ".jar";
                    //UpdateStatusBarAndLog($"Downloading {filename}...", new StackFrame().GetMethod().Name);
                    downloader.DownloadFileTaskAsync(new Uri(selectedVersionManifest.DownloadInfo?.Client.Url
                            ??
                            string.Format(
                                "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/{0}.jar",
                                version)),
                        string.Format("{0}/{1}/{1}.jar", VersionsDir, version)).Wait();
                }
                else
                {
                    //AppendException($"Unable to download version {version}: offline-mode enabled.");
                    return;
                }
            }
            if (selectedVersionManifest.InheritsFrom != null)
            {
                DownloadVersion(selectedVersionManifest.InheritsFrom);
            }
            //AppendLog($@"Finished checking {version} version avalability.");
        }

        //Need to fiX zip.
        private string CheckLibraries(Profile pf)
        {
            StringBuilder libraries = new StringBuilder();
            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(VersionsDir + @"\" + (_versionToLaunch ?? (pf.SelectedVersion ?? GetLatestVersion(pf)))));
            //SetStatusBarVisibility(true);
            //StatusBarValue = 0;
            //UpdateStatusBarText(_configuration.Localization.CheckingLibraries);
            //AppendLog("Preparing required libraries...");
            Dictionary<DownloadEntry, bool> libsToDownload = new Dictionary<DownloadEntry, bool>();
            foreach (Lib a in selectedVersionManifest.Libs)
            {
                if (!a.IsForWindows())
                {
                    continue;
                }
                if (a.DownloadInfo == null)
                {
                    libsToDownload.Add(new DownloadEntry
                    {
                        Path = a.GetPath(),
                        Url = a.GetUrl()
                    }, false);
                    continue;
                }
                foreach (DownloadEntry entry in a.DownloadInfo?.GetDownloadsEntries(OperatingSystem.WINDOWS))
                {
                    if (entry == null)
                    {
                        continue;
                    }
                    if (a.DownloadInfo.Classifiers?.ContainsKey("natives-windows") ?? false)
                    {
                        entry.Path = a.DownloadInfo.Classifiers["natives-windows"].Path ?? a.GetPath();
                        entry.Url = a.DownloadInfo.Classifiers["natives-windows"].Url ?? a.GetUrl();
                    }
                    else
                    {
                        entry.Path = entry.Path ?? a.GetPath();
                        entry.Url = entry.Url ?? a.Url;
                    }
                    entry.Path = entry.Path ?? a.GetPath();
                    entry.Url = entry.Url ?? a.Url;
                    libsToDownload.Add(entry, entry.IsNative);
                }
            }
            //SetStatusBarMaxValue(libsToDownload.Count + 1);
            foreach (DownloadEntry entry in libsToDownload.Keys)
            {
                //StatusBarValue++;
                if (!File.Exists(LibDir + @"\" + entry.Path) ||
                    DoRestoreVersion)
                {
                    if (Helper.CheckForInternetConnection())
                    {
                        //UpdateStatusBarAndLog($"Downloading {entry.Path.Replace('/', '\\')}...");
                        string directory = Path.GetDirectoryName(LibDir + @"\" + entry.Path);
                        //AppendDebug("Url: " + (entry.Url ?? @"https://libraries.minecraft.net/" + entry.Path));
                        //AppendDebug("DownloadDir: " + directory);
                        //AppendDebug("LibPath: " + entry.Path.Replace('/', '\\'));
                        if (!File.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        try
                        {
                            new WebClient().DownloadFile(entry.Url ?? @"https://libraries.minecraft.net/" + entry.Path, LibDir + @"\" + entry.Path);
                        }
                        catch (WebException ex)
                        {
                            //AppendException("Downloading failed: " + ex.Message);
                            Debug.WriteLine("Downloading failed: " + ex.ToString());
                            File.Delete(LibDir + @"\" + entry.Path);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            //AppendException("Downloading failed: " + ex.Message);
                            continue;
                        }
                    }
                    else
                    {
                        //AppendException($"Unable to download {entry.Path}: offline-mode enabled.");
                    }
                }
                if (entry.IsNative) {
                    //UpdateStatusBarAndLog($"Unpacking {entry.Path.Replace('/', '\\')}...");
                    using (ZipFile zip = ZipFile.Read(LibDir + @"/" + entry.Path))
                    {
                        foreach (ZipEntry zipEntry in zip.Where(zipEntry => zipEntry.FileName.EndsWith(".dll")))
                        {
                            //AppendDebug($"Unzipping {zipEntry.FileName}");
                            try
                            {
                                zipEntry.Extract(BaseDir + @"\natives\",
                                    ExtractExistingFileAction.OverwriteSilently);
                            }
                            catch (Exception ex)
                            {
                                //AppendException(ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    libraries.Append(LibDir + @"\" + entry.Path.Replace('/', '\\') + ";");
                }
                //UpdateStatusBarText(_configuration.Localization.CheckingLibraries);
            }
            libraries.Append(string.Format(@"{0}\{1}\{1}.jar", VersionsDir,
                selectedVersionManifest.GetBaseJar()));
            //AppendLog("Finished checking libraries.");
            return libraries.ToString();
        }

        //Ok
        private void DownloadAssets(Profile pf)
        {
            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(VersionsDir + @"\" +
                    (_versionToLaunch ??
                        (pf.SelectedVersion ?? GetLatestVersion(pf)))));
            if (selectedVersionManifest.InheritsFrom != null)
            {
                selectedVersionManifest = selectedVersionManifest.InheritableVersionManifest;
            }
            string file = string.Format(@"{0}\assets\indexes\{1}.json", BaseDir,
                selectedVersionManifest.AssetsIndex ?? "legacy");
            if (!File.Exists(file))
            {
                if (!Directory.Exists(Path.GetDirectoryName(file)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                }
                new WebClient().DownloadFile(selectedVersionManifest.GetAssetIndexDownloadUrl(), file);
            }
            AssetsManifest manifest = AssetsManifest.Parse(file);
            //StatusBarValue = 0;
            //SetStatusBarMaxValue(manifest.Objects.Select(pair => pair.Value.Hash.GetFullPath()).Count(filename => !File.Exists(_configuration.McDirectory + @"\assets\objects\" +
            //    filename) || DoRestoreVersion) + 1);
            foreach (Asset asset in manifest.Objects.Select(pair => pair.Value).Where(asset => !File.Exists(BaseDir + @"\assets\objects\" +
                asset.Hash.GetFullPath()) || DoRestoreVersion))
            {
                string directory = BaseDir + @"\assets\objects\" + asset.Hash.GetDirectoryName();
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                try
                {
                    //AppendDebug($"Downloading {asset.Hash}...");
                    new WebClient().DownloadFile(@"http://resources.download.minecraft.net/" + asset.Hash.GetFullPath(),
                        BaseDir + @"\assets\objects\" + asset.Hash.GetFullPath());
                }
                catch (Exception ex)
                {
                    //AppendException(ex.ToString());
                    //throw new LauncherException.FailedToDownloadException();
                    Debug.WriteLine(ex.ToString());
                }
                //StatusBarValue++;
            }
            //AppendLog("Finished checking game assets.");
            if (selectedVersionManifest.AssetsIndex == null || selectedVersionManifest.AssetsIndex == "legacy")
            {
                //StatusBarValue = 0;
                //SetStatusBarMaxValue(manifest.Objects.Select(pair => pair.Value.AssociatedName)
                //    .Count(
                //        filename =>
                //            !File.Exists(_configuration.McDirectory + @"\assets\virtual\legacy\" +
                //                filename) || _restoreVersion) + 1);
                //UpdateStatusBarAndLog("Converting assets...");
                foreach (Asset asset in manifest.Objects.Select(pair => pair.Value)
                    .Where(asset =>
                        !File.Exists(BaseDir + @"\assets\virtual\legacy\" +
                            asset.AssociatedName) || DoRestoreVersion))
                {
                    string filename = BaseDir + @"\assets\virtual\legacy\" + asset.AssociatedName;
                    try
                    {
                        if (!Directory.Exists(new FileInfo(filename).DirectoryName))
                        {
                            Directory.CreateDirectory(new FileInfo(filename).DirectoryName);
                        }
                        /*AppendDebug(
                            $"Converting '{asset.Hash.GetFullPath()}' to '{asset.AssociatedName}'");*/
                        File.Copy(BaseDir + @"\assets\objects\" + asset.Hash.GetFullPath(),
                            filename);
                    }
                    catch (Exception ex)
                    {
                    }
                    //StatusBarValue++;
                }
                //AppendLog("Finished converting assets.");
            }
            //SetStatusBarVisibility(false);
        }

        //Ok
        public string GetLatestVersion(Profile profile)
        {
            return profile.AllowedReleaseTypes != null
                ? profile.AllowedReleaseTypes.Contains("snapshot")
                    ? VersionList.LatestVersions.Snapshot
                    : VersionList.LatestVersions.Release
                : VersionList.LatestVersions.Release;
        }

        //Ok
        public void UpdateVersionsList()
        {
            string versionsManifestPath = Path.Combine(BaseDir, "versions.json");
            if (!Helper.CheckForInternetConnection())
            {
                //AppendLog("Unable to get new version list: offline-mode enabled.");
                if (File.Exists(versionsManifestPath))
                {
                    VersionList = RawVersionListManifest.ParseList(File.ReadAllText(versionsManifestPath));
                    return;
                }
                throw new FileNotFoundException("Need to download versions json for first time!");
                //Environment.Exit(0);
            }
            //AppendLog("Checking version.json...");
            RawVersionListManifest remoteManifest = RawVersionListManifest.ParseList(new WebClient().DownloadString(
                new Uri("https://launchermeta.mojang.com/mc/game/version_manifest.json")));
            if (!Directory.Exists(VersionsDir))
            {
                Directory.CreateDirectory(VersionsDir);
            }
            if (!File.Exists(versionsManifestPath))
            {
                File.WriteAllText(versionsManifestPath, remoteManifest.ToString());
                VersionList = remoteManifest;
                return;
            }
            //AppendLog("Latest snapshot: " + remoteManifest.LatestVersions.Snapshot);
            //AppendLog("Latest release: " + remoteManifest.LatestVersions.Release);
            RawVersionListManifest localManifest =
                RawVersionListManifest.ParseList(File.ReadAllText(versionsManifestPath));
            //AppendLog($"Local versions: {localManifest.Versions.Count}. "
            //    + $"Remote versions: {remoteManifest.Versions.Count}");
            if (remoteManifest.Versions.Count == localManifest.Versions.Count &&
                remoteManifest.LatestVersions.Release == localManifest.LatestVersions.Release &&
                remoteManifest.LatestVersions.Snapshot == localManifest.LatestVersions.Snapshot)
            {
                VersionList = localManifest;
                //AppendLog("No update found.");
                return;
            }
            //AppendLog("Writting new list...");
            File.WriteAllText(versionsManifestPath, remoteManifest.ToString());
            VersionList = remoteManifest;
        }

        //Ok
        public void UpdateUserList()
        {
            //NicknameDropDownList.Items.Clear();
            try
            {
                _userManager = File.Exists(BaseDir + @"\Users.json")
                    ? JsonConvert.DeserializeObject<UserManager>(
                        File.ReadAllText(BaseDir + @"\Users.json"))
                    : new UserManager();
            }
            catch (Exception ex)
            {
                //AppendException("Reading user list: an exception has occurred\n" + ex.Message);
                _userManager = new UserManager();
                //SaveUsers();
            }
            //NicknameDropDownList.Items.AddRange(_userManager.Accounts.Keys);
            //NicknameDropDownList.SelectedItem = NicknameDropDownList.FindItemExact(_userManager.SelectedUsername, true);
        }
    }

    /*public class OutputLog
    {
        public delegate string LogPart(object sender,LogRecivedEventArgs args);
        public static event LogPart LogRecivedEvent;
        public class LogRecivedEventArgs : EventArgs
        {
            private String str;
        }
    }*/
    internal class UserManager
    {
        [JsonProperty("selectedUsername")]
        public string SelectedUsername { get; set; }

        [JsonProperty("users")]
        public Dictionary<string, User> Accounts { get; set; } = new Dictionary<string, User>();
    }

    internal class User
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("sessionToken")]
        public string ClientToken { get; set; }

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("properties")]
        public JArray UserProperties { get; set; }
    }
}