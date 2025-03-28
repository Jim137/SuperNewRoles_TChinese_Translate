global using SuperNewRoles.Modules;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AmongUs.Data;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using SuperNewRoles.CustomModOption;
using SuperNewRoles.CustomObject;
using SuperNewRoles.MapDatabase;
using SuperNewRoles.Roles.Role;
using SuperNewRoles.Roles.RoleBases;
using SuperNewRoles.SuperNewRolesWeb;
using SuperNewRoles.WaveCannonObj;
using TMPro;
using UnityEngine;

namespace SuperNewRoles;

[BepInAutoPlugin("jp.ykundesu.supernewroles", "SuperNewRoles")]
[BepInIncompatibility("com.emptybottle.townofhost")]
[BepInIncompatibility("me.eisbison.theotherroles")]
[BepInIncompatibility("me.yukieiji.extremeroles")]
[BepInIncompatibility("com.tugaru.TownOfPlus")]
[BepInProcess("Among Us.exe")]
public partial class SuperNewRolesPlugin : BasePlugin
{
    public static readonly string VersionString = $"{Assembly.GetExecutingAssembly().GetName().Version}";

    public const bool IsBeta = ThisAssembly.Git.Branch != MasterBranch && !IsHideText;

    public const bool IsSecretBranch = false; // プルリク時にtrueなら指摘してください
    public const bool IsHideText = false; // プルリク時にtrueなら指摘してください

    public static Assembly assembly => _assembly != null ? _assembly : (_assembly = Assembly.GetExecutingAssembly());
    private static Assembly _assembly = null;

    public const string ModUrl = "SuperNewRoles/SuperNewRoles";
    public const string MasterBranch = "master";
    public static string ModName { get { return AprilFoolsManager.getCurrentModName(); } }
    public static string ColorModName { get { return AprilFoolsManager.getCurrentModNameOnColor(); } }
    public const string DiscordServer = "https://discord.gg/Cqfwx82ynN";
    public const string Twitter1 = "https://twitter.com/SNRDevs";
    public const string Twitter2 = "https://twitter.com/SNROfficials";


    public static Version ThisVersion = System.Version.Parse($"{Assembly.GetExecutingAssembly().GetName().Version}");
    public static BepInEx.Logging.ManualLogSource Logger;
    public static Sprite ModStamp;
    public static int optionsPage = 1;
    public static int optionsMaxPage = 0;
    public Harmony Harmony { get; } = new Harmony("jp.ykundesu.supernewroles");
    public static SuperNewRolesPlugin Instance;
    public static bool IsUpdate = false;
    public static string NewVersion = "";
    public static string thisname;
    public static string ThisPluginModName;

    public const string SteamName = "steam";
    public const string EpicGamesStoreName = "egs";
    public static bool IsEpic => Constants.GetPurchasingPlatformType() == EpicGamesStoreName;

    //対応しているバージョン。nullなら全て。
    public static string[] SupportVanilaVersion = new string[] { "2024.3.5" };

    public override void Load()
    {
        Logger = Log;
        Instance = this;
        Task LoadHarmonyPatchTask = Task.Run(() =>
        {
            Logger.LogInfo("Start Patch Harmony");
            Harmony.PatchAll();
            Logger.LogInfo("End Patch Harmony");
        });
        ModTranslation.LoadCsv();
        bool CreatedVersionPatch = false;

        //初期状態ではRoleInfoやOptionInfoなどが読み込まれていないため、
        //ここで読み込む
        Type RoleInfoType = typeof(RoleInfo);
        Type RoleBaseType = typeof(RoleBase);
        Assembly.GetAssembly(RoleBaseType)
        .GetTypes()
        .Where(t =>
        {
            if (t.IsSubclassOf(RoleBaseType))
            {
                foreach (FieldInfo field in t.GetFields())
                {
                    if (field.IsStatic && field.FieldType == RoleInfoType)
                        field.GetValue(null);
                }
            }
            return false;
        }).ToList();

        //SetNonVanilaVersionPatch();
        // All Load() Start
        OptionSaver.Load();
        ConfigRoles.Load();
        UpdateCPUProcessorAffinity();
        WebAccountManager.Load();
        ContentManager.Load();
        //WebAccountManager.SetToken("XvSwpZ8CsQgEksBg");
        CacheManager.Load();
        WebConstants.Load();
        CustomCosmetics.CustomColors.Load();
        ModDownloader.Load();
        CustomOptionHolder.Load();
        LegacyOptionDataMigration.Load();
        AccountLoginMenu.Initialize();
        // All Load() End


        // Old Delete Start
        try
        {
            DirectoryInfo d = new(Path.GetDirectoryName(Application.dataPath) + @"\BepInEx\plugins");
            string[] files = d.GetFiles("*.dll.old").Select(x => x.FullName).ToArray(); // Getting old versions
            foreach (string f in files)
                File.Delete(f);
        }
        catch (Exception e)
        {
            System.Console.WriteLine("Exception occured when clearing old versions:\n" + e);
        }

        // Old Delete End

        LoggerPlus.SetLogName();

        SuperNewRoles.Logger.Info(DateTime.Now.ToString("D"), "DateTime Now"); // 2022年11月24日
        SuperNewRoles.Logger.Info(ThisAssembly.Git.Branch, "Branch");
        SuperNewRoles.Logger.Info(ThisAssembly.Git.Commit, "Commit");
        SuperNewRoles.Logger.Info(ThisAssembly.Git.Commits, "Commits");
        SuperNewRoles.Logger.Info(ThisAssembly.Git.BaseTag, "BaseTag");
        SuperNewRoles.Logger.Info(ThisAssembly.Git.Tag, "Tag");
        SuperNewRoles.Logger.Info(VersionString, "VersionString");
        SuperNewRoles.Logger.Info(Version, nameof(Version));
        SuperNewRoles.Logger.Info($"{Application.version}({Constants.GetPurchasingPlatformType()})", "AmongUsVersion"); // アモングアス本体のバージョン(プレイしているプラットフォーム)
        try
        {
            var directoryPath = Path.GetDirectoryName(Application.dataPath) + @"\BepInEx\plugins";
            SuperNewRoles.Logger.Info($"DirectoryPathが半角のみ:{ModHelpers.IsOneByteOnlyString(directoryPath)}", "IsOneByteOnly path"); // フォルダパスが半角のみで構成されているか
            var di = new DirectoryInfo(directoryPath);
            var pluginFiles = di.GetFiles();
            foreach (var f in pluginFiles)
            {
                var name = f.Name;
                SuperNewRoles.Logger.Info($"---------- {name} -----------", "Data");
                SuperNewRoles.Logger.Info(name, nameof(pluginFiles)); // ファイル名
                SuperNewRoles.Logger.Info($"{f.Length}MB", name); // サイズをバイト単位で取得
            }
        }
        catch (Exception e)
        {
            SuperNewRoles.Logger.Error($"pluginFilesの取得時に例外発生{e.ToString()}", "pluginFiles");
        }

        Logger.LogInfo(ModTranslation.GetString("\n---------------\nSuperNewRoles\n" + ModTranslation.GetString("StartLogText") + "\n---------------"));

        ThisPluginModName = IL2CPPChainloader.Instance.Plugins.FirstOrDefault(x => x.Key == "jp.ykundesu.supernewroles").Value.Metadata.Name;

        //Register Il2cpp
        ClassInjector.RegisterTypeInIl2Cpp<CustomAnimation>();
        ClassInjector.RegisterTypeInIl2Cpp<WormHole>();
        ClassInjector.RegisterTypeInIl2Cpp<SluggerDeadbody>();
        ClassInjector.RegisterTypeInIl2Cpp<WaveCannonObject>();
        ClassInjector.RegisterTypeInIl2Cpp<RocketDeadbody>();
        ClassInjector.RegisterTypeInIl2Cpp<SpiderTrap>();
        ClassInjector.RegisterTypeInIl2Cpp<WCSantaHandler>();
        ClassInjector.RegisterTypeInIl2Cpp<PushedPlayerDeadbody>();
        ClassInjector.RegisterTypeInIl2Cpp<Lantern>();
        ClassInjector.RegisterTypeInIl2Cpp<WaveCannonEffect>();
        ClassInjector.RegisterTypeInIl2Cpp<ModSettingsMenu>();
        ClassInjector.RegisterTypeInIl2Cpp<ModOptionBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<ModCategoryHeaderEditRole>();
        ClassInjector.RegisterTypeInIl2Cpp<ModRoleOptionSetting>();
        ClassInjector.RegisterTypeInIl2Cpp<ModStringOption>();
        ClassInjector.RegisterTypeInIl2Cpp<ModToggleOption>();
        ClassInjector.RegisterTypeInIl2Cpp<Drone>();

        Logger.LogInfo("Start Load Resource");
        string[] resourceNames = assembly.GetManifestResourceNames();
        foreach (string resourceName in resourceNames)
        {
            if (resourceName.EndsWith(".png") && resourceName.Contains("_"))
            {
                ModHelpers.LoadSpriteFromResources(resourceName, 115f);
            }
        }
        AssetManager.Load();

        Logger.LogInfo("Resource Loaded");

        Logger.LogInfo("Start WaitLoad");
        // ロードが終わってないなら待つ
        LoadHarmonyPatchTask.Wait();
    }
    static bool ViewdNonVersion = false;
    public static void SetNonVanilaVersionPatch()
    {
        if (SupportVanilaVersion != null && !SupportVanilaVersion.Contains(Application.version))
        {
            var CVoriginal = AccessTools.Method(typeof(MainMenuManager), nameof(MainMenuManager.Awake));
            var CVpostfix = new HarmonyMethod(typeof(SuperNewRolesPlugin), nameof(SuperNewRolesPlugin.MainMenuVersionCheckPatch));
            SuperNewRolesPlugin.Instance.Harmony.Patch(CVoriginal, postfix: CVpostfix);
        }
    }
    // CPUの割当を変更する
    public static void UpdateCPUProcessorAffinity()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) return;

        ulong affinity = ConfigRoles._ProcessorAffinityMask.Value;
        if (!ConfigRoles._isCPUProcessorAffinity.Value || affinity == 0)
        {
            Logger.LogWarning("UpdateCPUProcessorAffinity: IsCPUProcessorAffinity is false");
            return;
        }

        Logger.LogInfo("Start UpdateCPUProcessorAffinity");
        if (Environment.ProcessorCount > 1)
        {
            // コア数上限突破の場合は全てのコアを使う
            if (Environment.ProcessorCount < System.Numerics.BitOperations.Log2(affinity))
            {
                affinity = 1;
                for (int i = 1; i < Environment.ProcessorCount; i++)
                {
                    affinity |= (ulong)1 << i;
                }
            }
            System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)affinity;
        }
        Logger.LogInfo($"UpdatedCPUProcessorAffinity To: {affinity}");
    }
    // https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/Manager/AuthManagerPatch.cs
    [HarmonyPatch(typeof(AuthManager), nameof(AuthManager.CoConnect))]
    public static class AuthManagerCoConnectPatch
    {
        public static bool Prefix(AuthManager __instance)
        {
            if (!ModHelpers.IsCustomServer() ||
                FastDestroyableSingleton<ServerManager>.Instance.CurrentRegion.Servers.Any(x => x.UseDtls))
                return true;
            if (__instance.connection != null)
                __instance.connection.Dispose();
            __instance.connection = null;
            return false;
        }
    }
    public static void MainMenuVersionCheckPatch(MainMenuManager __instance)
    {
        if (SupportVanilaVersion != null && !SupportVanilaVersion.Contains(Application.version) && !ViewdNonVersion)
        {
            GenericPopup popup = GameObject.Instantiate(DestroyableSingleton<DiscordManager>.Instance.discordPopup);
            popup.transform.FindChild("Background").transform.localScale = new(3, 2.5f, 1f);
            Transform ExitGame = popup.transform.FindChild("ExitGame");
            ExitGame.transform.localPosition = new(0, -2f, -0.5f);
            TextMeshPro egtmp = ExitGame.GetComponentInChildren<TextMeshPro>();
            GameObject.Destroy(egtmp.GetComponent<TextTranslatorTMP>());
            egtmp.text = "OK";
            StringBuilder builder = new($"<size=200%>やあ、みなさん</size>\n\nこのバージョンでは今のバニラバージョン、「{Application.version}」を\nサポートしていません。\nこのバージョンが対応しているバニラバージョンは、\n<size=150%>");
            int count = 0;
            foreach (string ver in SupportVanilaVersion)
            {
                builder.Append($"「{ver}」");
                count++;
                if (count >= 3)
                {
                    builder.Append('\n');
                    count = 0;
                }
            }
            if (count > 0)
                builder.Append('\n');
            builder.Append("</size>です。もし対応しているバニラバージョンより今のバニラバージョンが低いならば、\nSuperNewRolesをアップデートしてみましょう！\n");
            builder.Append("しかし、もし対応しているバニラバージョンの方が今のバニラバージョンより低かったなら、\n");
            builder.Append("SuperNewRolesの新しいアップデートを確認してみましょう！\n");
            builder.Append("もし、新バージョンが出ているならzipから更新してくださいね。\n出ていなかったら出るのを待ちましょう!");
            builder.AppendLine("出ているかはSuperNewRolesの公式Twitter(新X)を確認しましょう!");
            builder.AppendLine("<link=\"https://x.com/SNROfficials\">SuperNewRoles公式X:https://x.com/SNROfficials</link>");
            builder.AppendLine("<link=\"https://github.com/SuperNewRoles/SuperNewRoles/releases/\">SuperNewRoles公式Githubリリースページ:https://github.com/SuperNewRoles/SuperNewRoles/releases/</link>");
            //builder.AppendLine("(リンクを押すとブラウザが開きます)");
            builder.AppendLine("リリースを待っててね!");
            popup.TextAreaTMP.text = builder.ToString();
            popup.gameObject.SetActive(true);
            popup.TextAreaTMP.transform.localScale = Vector3.one * 1.5f;
            popup.TextAreaTMP.transform.localPosition = new(0, 0.3f, -0.5f);
            ViewdNonVersion = true;
        }
    }
    [HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
    class GetBroadcastVersionPatch
    {
        public static void Postfix(ref int __result)
        {
            if (AmongUsClient.Instance.NetworkMode is NetworkModes.LocalGame or NetworkModes.FreePlay) return;
            __result += 25;
        }
    }
    [HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
    public static class ConstantsVersionModdedPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
    // Reactor来たら確認
    /*
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
    public static class AmBannedPatch
    {
        public static void Postfix(out bool __result) => __result = false;
    }*/
    private static GameObject BackgroundCover;
    private static GameObject RegionSelectButton;
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenEnterCodeMenu))]
    public static class MainMenuManagerOpenEnterCodeMenuPatch
    {
        public static bool Prefix(MainMenuManager __instance, bool animate)
        {
            if (BackgroundCover == null)
            {
                BackgroundCover = GameObject.Instantiate(AssetManager.GetAsset<GameObject>("BackgroundCover"), __instance.transform);
            }
            if (BackgroundCover != null)
                BackgroundCover.SetActive(true);
            if (RegionSelectButton == null)
            {
                RegionSelectButton = GameObject.Instantiate(AssetManager.GetAsset<GameObject>("RegionSelectButton"), __instance.enterCodeContainer);
            }
            if (RegionSelectButton != null)
                RegionSelectButton.SetActive(true);
            if (!__instance.animating)
            {
                __instance.ResetScreen();
                __instance.screenTint.enabled = true;
                __instance.enterCodeButtons.SetActive(value: true);
                for (int i = 0; i < __instance.mainButtons.Count; i++)
                {
                    __instance.mainButtons[i].ControllerNav.selectOnRight = __instance.entercodeField;
                }
                ControllerManager.Instance.SetCurrentSelected(__instance.entercodeField);
                if (animate)
                {
                    __instance.StartCoroutine(EnterCodeSlideCo(__instance).WrapToIl2Cpp());
                    return false;
                }
                __instance.enterCodeHeader.SetActive(value: true);
                __instance.enterCodeContainer.localPosition = new Vector3(0f, __instance.enterCodeContainer.localPosition.y, __instance.enterCodeContainer.position.z);
                __instance.onlineButtonsContainer.localPosition = new Vector3(0f, __instance.onlineButtonsContainer.localPosition.y, __instance.onlineButtonsContainer.position.z);
            }
            return false;
        }
        private static IEnumerator EnterCodeSlideCo(MainMenuManager __instance)
        {
            // オンラインボタンの表示と初期位置の設定
            __instance.onlineButtons.SetActive(true);
            __instance.onlineButtonsContainer.localPosition = new Vector3(0f, __instance.onlineButtonsContainer.localPosition.y, __instance.onlineButtonsContainer.localPosition.z);
            // エンターコードのコンテナを画面外右に配置しておく
            __instance.enterCodeContainer.localPosition = new Vector3(10f, __instance.enterCodeContainer.localPosition.y, __instance.enterCodeContainer.localPosition.z);
            __instance.animating = true;
            __instance.onlineHeader.SetActive(true);
            __instance.enterCodeHeader.SetActive(false);

            // オンラインボタンコンテナを左方向へスムーズにスライドアウト（0.3秒）
            Vector2 onlineStart = new Vector2(0f, __instance.onlineButtonsContainer.localPosition.y);
            Vector2 onlineEnd = new Vector2(-8f, __instance.onlineButtonsContainer.localPosition.y);
            yield return Effects.Slide2D(source: onlineStart, dest: onlineEnd, target: __instance.onlineButtonsContainer, duration: 0.1f);

            // エンターコードコンテナを右からスライドインさせる
            // オーバーシュートさせてから最終位置に戻る演出（バウンス効果）
            Vector2 enterStart = new Vector2(10f, __instance.enterCodeContainer.localPosition.y);
            Vector2 enterOvershoot = new Vector2(-1f, __instance.enterCodeContainer.localPosition.y);
            Vector2 enterFinal = new Vector2(0f, __instance.enterCodeContainer.localPosition.y);
            yield return Effects.Slide2D(__instance.enterCodeContainer, enterStart, enterOvershoot, 0.08f);
            yield return Effects.Slide2D(__instance.enterCodeContainer, enterOvershoot, enterFinal, 0.05f);

            // スライドアニメーション完了後の後処理
            __instance.onlineButtons.SetActive(false);
            __instance.enterCodeHeader.SetActive(true);
            __instance.animating = false;
        }
    }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.ClickBackEnterCode))]
    public static class MainMenuManagerClickBackEnterCodePatch
    {
        public static bool Prefix(MainMenuManager __instance)
        {
            if (!__instance.animating)
            {
                __instance.StartCoroutine(AnimateEnterCodeExitSlideCo(__instance).WrapToIl2Cpp());
            }
            return false;
        }

        private static IEnumerator AnimateEnterCodeExitSlideCo(MainMenuManager __instance)
        {
            // オンラインボタンを表示し、初期位置を左側（オフスクリーン）に設定
            __instance.onlineButtons.SetActive(true);
            __instance.onlineButtonsContainer.localPosition = new Vector3(-8f, __instance.onlineButtonsContainer.localPosition.y, __instance.onlineButtonsContainer.localPosition.z);
            // エンターコードコンテナは中央に配置（表示中）
            __instance.enterCodeContainer.localPosition = new Vector3(0f, __instance.enterCodeContainer.localPosition.y, __instance.enterCodeContainer.localPosition.z);
            __instance.animating = true;
            // 現在、エンターコードヘッダーが表示され、オンラインヘッダーは非表示
            __instance.onlineHeader.SetActive(false);
            __instance.enterCodeHeader.SetActive(true);

            // エンターコードコンテナを右方向へバウンス効果付きでスライドアウト
            // 逆アニメーション：EnterCodeSlideCoで、コンテナは右側（10）から出現して-1を通り0に落ち着くので、
            // ここでは0からまず11（オーバーシュート）へ、次に10（オフスクリーン）へ移動する
            Vector2 codeStart = new Vector2(0f, __instance.enterCodeContainer.localPosition.y);
            Vector2 codeOvershoot = new Vector2(11f, __instance.enterCodeContainer.localPosition.y);
            Vector2 codeEnd = new Vector2(10f, __instance.enterCodeContainer.localPosition.y);
            yield return Effects.Slide2D(__instance.enterCodeContainer, codeStart, codeOvershoot, 0.08f);
            yield return Effects.Slide2D(__instance.enterCodeContainer, codeOvershoot, codeEnd, 0.05f);

            // オンラインボタンコンテナを左側（-8）から中央（0）へスライドイン
            Vector2 onlineStart = new Vector2(-8f, __instance.onlineButtonsContainer.localPosition.y);
            Vector2 onlineEnd = new Vector2(0f, __instance.onlineButtonsContainer.localPosition.y);
            yield return Effects.Slide2D(__instance.onlineButtonsContainer, onlineStart, onlineEnd, 0.1f);

            // アニメーション完了後にヘッダーを切り替え、オンラインメニューを開く
            __instance.enterCodeHeader.SetActive(false);
            __instance.onlineHeader.SetActive(true);
            __instance.animating = false;
            __instance.OpenOnlineMenu();
            if (BackgroundCover != null)
                BackgroundCover.SetActive(false);
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    public static class ChatControllerAwakePatch
    {
        public static void Prefix()
        {
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;
        }
        public static void Postfix(ChatController __instance)
        {
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (!__instance.isActiveAndEnabled) return;
                __instance.Toggle();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                __instance.SetVisible(false);
                new LateTask(() =>
                {
                    __instance.SetVisible(true);
                }, 0f, "AntiChatBug");
            }
            if (__instance.IsOpenOrOpening)
            {
                __instance.banButton.MenuButton.enabled = !__instance.IsAnimating;
            }
        }
    }
    public static void AgarthaLoad() => Agartha.AgarthaPlugin.Instance.Log.LogInfo("アガルタやで");
}