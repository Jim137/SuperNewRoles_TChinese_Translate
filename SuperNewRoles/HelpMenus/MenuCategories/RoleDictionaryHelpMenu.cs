using System.Linq;
using System.Collections.Generic;
using SuperNewRoles.Modules;
using SuperNewRoles.Roles;
using SuperNewRoles.CustomOptions;
using SuperNewRoles.HelpMenus;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SuperNewRoles.HelpMenus.MenuCategories;

public class RoleDictionaryHelpMenu : HelpMenuCategoryBase
{
    public override string Name => "RoleDictionary";
    public override HelpMenuCategory Category => HelpMenuCategory.RoleDictionary;

    private GameObject Container;
    private GameObject MenuObject;
    private GameObject CurrentTeamContainer;
    private RoleOptionMenuType CurrentTeamType;
    private GameObject selectedButton;
    private GameObject RoleDetailObject; // 役職詳細表示用オブジェクト
    private GameObject LeftButtonsObject; // 左側のボタン
    private bool isShowingRoleDetail = false; // 役職詳細表示中かどうか
    private RoleId currentDisplayedRoleId = RoleId.None; // 現在表示中の役職ID
    public const RoleOptionMenuType DEFAULT_TEAM = RoleOptionMenuType.Impostor;

    public override void Show(GameObject Container)
    {
        // 役職詳細表示中なら閉じる
        if (isShowingRoleDetail)
        {
            CloseRoleDetail();
        }

        if (MenuObject != null)
            GameObject.Destroy(MenuObject);

        this.Container = Container;
        // RoleDictionaryHelpMenuをAssetManagerから取得
        MenuObject = GameObject.Instantiate(AssetManager.GetAsset<GameObject>("RoleDictionaryHelpMenu"), Container.transform);
        MenuObject.transform.localPosition = Vector3.zero;
        MenuObject.transform.localScale = Vector3.one;
        MenuObject.transform.localRotation = Quaternion.identity;

        // Teamsボタンの設定
        SetupTeamButtons();

        // 最初はImpostorを表示
        ShowTeamRoles(DEFAULT_TEAM);
    }

    private void SetupTeamButtons()
    {
        // ImpostorButton, NeutralButton, CrewmateButtonの設定
        SetupTeamButton("ImpostorButton", RoleOptionMenuType.Impostor);
        SetupTeamButton("NeutralButton", RoleOptionMenuType.Neutral);
        SetupTeamButton("CrewmateButton", RoleOptionMenuType.Crewmate);
        SetupTeamButton("GhostButton", RoleOptionMenuType.Ghost);
        SetupTeamButton("ModifierButton", RoleOptionMenuType.Modifier);
    }

    private void SetupTeamButton(string buttonName, RoleOptionMenuType teamType)
    {
        var buttonTransform = MenuObject.transform.Find("Teams").Find(buttonName);
        if (buttonTransform == null) return;

        var button = buttonTransform.gameObject;
        var passiveButton = button.GetComponent<PassiveButton>() ?? button.AddComponent<PassiveButton>();

        // ボタンのテキスト設定
        var textComponent = button.transform.Find("Text")?.GetComponent<TextMeshPro>();
        if (textComponent != null)
        {
            // 翻訳キーを適切に設定
            textComponent.text = ModHelpers.CsWithTranslation(RoleDetailMenu.GetTeamColor(teamType), buttonName);
        }

        // デフォルトのチームのボタンを選択状態にする
        if (teamType == DEFAULT_TEAM)
        {
            button.transform.Find("Selected")?.gameObject.SetActive(true);
            selectedButton = button;
        }

        // ボタンクリック時のイベント設定
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            ShowTeamRoles(teamType);
            if (selectedButton != null && selectedButton != button)
            {
                selectedButton.transform.Find("Selected")?.gameObject.SetActive(false);
            }
            button.transform.Find("Selected")?.gameObject.SetActive(true);
            selectedButton = button;
        }));

        // マウスオーバー時のイベント
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            if (button != selectedButton)
                button.transform.Find("Selected")?.gameObject.SetActive(true);
        }));

        // マウスアウト時のイベント
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            if (button != selectedButton)
                button.transform.Find("Selected")?.gameObject.SetActive(false);
        }));
    }

    private void ShowTeamRoles(RoleOptionMenuType teamType)
    {
        // 既存のコンテナがあれば削除
        if (CurrentTeamContainer != null)
        {
            GameObject.Destroy(CurrentTeamContainer);
        }

        CurrentTeamType = teamType;

        // コンテナを作成
        var roleInformation = MenuObject.transform.Find("Scroller").Find("RoleInformation");
        if (roleInformation == null) return;

        CurrentTeamContainer = new GameObject($"{teamType}Container");
        CurrentTeamContainer.transform.SetParent(roleInformation);
        CurrentTeamContainer.transform.localPosition = Vector3.zero;
        CurrentTeamContainer.transform.localScale = Vector3.one;

        // チーム別の役職を表示
        GenerateRoleButtons(CurrentTeamContainer.transform, teamType);
    }

    private void GenerateRoleButtons(Transform container, RoleOptionMenuType teamType)
    {
        if (teamType == RoleOptionMenuType.Ghost)
        {
            GenerateGhostRoleButtons(container);
            return;
        }

        if (teamType == RoleOptionMenuType.Modifier)
        {
            GenerateModifierRoleButtons(container);
            return;
        }

        // 役職一覧を取得
        var roles = CustomRoleManager.AllRoles
            .Where(r => r.QuoteMod != QuoteMod.Vanilla && (r.OptionTeam == teamType || (r.OptionTeam == RoleOptionMenuType.Hidden && r.AssignedTeam == (AssignedTeamType)teamType)))
            .OrderBy(r => r.Role.ToString())
            .ToList();

        // ボタンの配置設定
        int maxColumns = 4; // 1行に4役職
        float startX = -1.03f; // 左上はx: -1から開始
        float startY = 1.2f; // 上からy: 1.2で開始
        float buttonWidth = 1.37f; // 横は1.5ずつ増加
        float buttonHeight = 0.35f; // 縦は0.7ずつ減少

        // 役職ボタンを作成
        for (int i = 0; i < roles.Count; i++)
        {
            int column = i % maxColumns;
            int row = i / maxColumns;

            Vector3 position = new(
                startX + (column * buttonWidth),
                startY - (row * buttonHeight),
                0f
            );

            CreateRoleButton(container, roles[i], position);
        }
    }

    private void GenerateGhostRoleButtons(Transform container)
    {
        // ゴースト役職一覧を取得
        var ghostRoles = CustomRoleManager.AllGhostRoles
            .Where(r => r.QuoteMod != QuoteMod.Vanilla && !r.HiddenOption)
            .OrderBy(r => r.Role.ToString())
            .ToList();

        // ボタンの配置設定
        int maxColumns = 4; // 1行に4役職
        float startX = -1.03f; // 左上はx: -1から開始
        float startY = 1.2f; // 上からy: 1.2で開始
        float buttonWidth = 1.37f; // 横は1.5ずつ増加
        float buttonHeight = 0.35f; // 縦は0.7ずつ減少

        // ゴースト役職ボタンを作成
        for (int i = 0; i < ghostRoles.Count; i++)
        {
            int column = i % maxColumns;
            int row = i / maxColumns;

            Vector3 position = new(
                startX + (column * buttonWidth),
                startY - (row * buttonHeight),
                0f
            );

            CreateGhostRoleButton(container, ghostRoles[i], position);
        }
    }

    private void GenerateModifierRoleButtons(Transform container)
    {
        // モディファイア役職一覧を取得
        var modifierRoles = CustomRoleManager.AllModifiers
            .Where(r => r.QuoteMod != QuoteMod.Vanilla)
            .OrderBy(r => r.ModifierRole.ToString())
            .ToList();

        // ボタンの配置設定
        int maxColumns = 4; // 1行に4役職
        float startX = -1.03f; // 左上はx: -1から開始
        float startY = 1.2f; // 上からy: 1.2で開始
        float buttonWidth = 1.37f; // 横は1.5ずつ増加
        float buttonHeight = 0.35f; // 縦は0.7ずつ減少

        // モディファイア役職ボタンを作成
        for (int i = 0; i < modifierRoles.Count; i++)
        {
            int column = i % maxColumns;
            int row = i / maxColumns;

            Vector3 position = new(
                startX + (column * buttonWidth),
                startY - (row * buttonHeight),
                0f
            );

            CreateModifierRoleButton(container, modifierRoles[i], position);
        }
    }

    private GameObject CreateRoleButton(Transform parent, IRoleBase role, Vector3 position)
    {
        var bulkRoleButtonAsset = AssetManager.GetAsset<GameObject>("RoleDictButton");
        var roleButton = GameObject.Instantiate(bulkRoleButtonAsset, parent);
        roleButton.name = $"RoleButton_{role.Role}";
        roleButton.transform.localPosition = position;
        roleButton.transform.localScale = Vector3.one * 0.25f;

        // テキスト設定
        var textComponent = roleButton.transform.Find("Text")?.GetComponent<TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = ModHelpers.CsWithTranslation(role.RoleColor, role.Role.ToString());
        }

        // ボタン設定
        var passiveButton = roleButton.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { roleButton.GetComponent<BoxCollider2D>() };
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            // 役職詳細表示の呼び出し
            ShowRoleDetail(role.Role);
        }));

        // マウスオーバー時のイベント
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(true);
        }));

        // マウスアウト時のイベント
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(false);
        }));

        return roleButton;
    }

    private GameObject CreateGhostRoleButton(Transform parent, IGhostRoleBase ghostRole, Vector3 position)
    {
        var bulkRoleButtonAsset = AssetManager.GetAsset<GameObject>("RoleDictButton");
        var roleButton = GameObject.Instantiate(bulkRoleButtonAsset, parent);
        roleButton.name = $"GhostRoleButton_{ghostRole.Role}";
        roleButton.transform.localPosition = position;
        roleButton.transform.localScale = Vector3.one * 0.25f;

        // テキスト設定
        var textComponent = roleButton.transform.Find("Text")?.GetComponent<TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = ModHelpers.CsWithTranslation(ghostRole.RoleColor, ghostRole.Role.ToString());
        }

        // ボタン設定
        var passiveButton = roleButton.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { roleButton.GetComponent<BoxCollider2D>() };
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            // ゴースト役職詳細表示の呼び出し
            ShowGhostRoleDetail(ghostRole.Role);
        }));

        // マウスオーバー時のイベント
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(true);
        }));

        // マウスアウト時のイベント
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(false);
        }));

        return roleButton;
    }

    private GameObject CreateModifierRoleButton(Transform parent, IModifierBase modifierRole, Vector3 position)
    {
        var bulkRoleButtonAsset = AssetManager.GetAsset<GameObject>("RoleDictButton");
        var roleButton = GameObject.Instantiate(bulkRoleButtonAsset, parent);
        roleButton.name = $"ModifierRoleButton_{modifierRole.ModifierRole}";
        roleButton.transform.localPosition = position;
        roleButton.transform.localScale = Vector3.one * 0.25f;

        // テキスト設定
        var textComponent = roleButton.transform.Find("Text")?.GetComponent<TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = ModHelpers.CsWithTranslation(modifierRole.RoleColor, modifierRole.ModifierRole.ToString());
        }

        // ボタン設定
        var passiveButton = roleButton.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { roleButton.GetComponent<BoxCollider2D>() };
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            // モディファイア役職詳細表示の呼び出し
            ShowModifierRoleDetail(modifierRole.ModifierRole);
        }));

        // マウスオーバー時のイベント
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(true);
        }));

        // マウスアウト時のイベント
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            roleButton.transform.Find("Selected")?.gameObject.SetActive(false);
        }));

        return roleButton;
    }

    // 役職詳細を表示する
    private void ShowRoleDetail(RoleId roleId)
    {
        // 既存の詳細表示を削除
        if (RoleDetailObject != null)
            GameObject.Destroy(RoleDetailObject);

        // 役職詳細表示中フラグを設定
        isShowingRoleDetail = true;

        // 役職詳細オブジェクトを作成
        RoleDetailObject = RoleDetailHelper.ShowRoleDetail(roleId, Container, MenuObject, CloseRoleDetail);

        // 現在表示中の役職IDを保存
        currentDisplayedRoleId = roleId;
    }

    // ゴースト役職詳細を表示する
    private void ShowGhostRoleDetail(GhostRoleId ghostRoleId)
    {
        // 既存の詳細表示を削除
        if (RoleDetailObject != null)
            GameObject.Destroy(RoleDetailObject);

        // 役職詳細表示中フラグを設定
        isShowingRoleDetail = true;

        // 役職詳細オブジェクトを作成
        RoleDetailObject = RoleDetailHelper.ShowRoleDetail(RoleId.None, Container, MenuObject, CloseRoleDetail);

        // RoleDetailMenuにMenuObjectを設定
        RoleDetailMenu.SetMenuObject(RoleDetailObject);

        // ゴースト役職の情報を表示
        IGhostRoleBase ghostRoleBase = CustomRoleManager.GetGhostRoleById(ghostRoleId);
        if (ghostRoleBase != null)
        {
            RoleDetailMenu.ShowRoleInformation(ghostRoleBase);
        }

        // 現在表示中の役職IDを保存（通常の役職IDとして保存）
        currentDisplayedRoleId = RoleId.None;
    }

    // モディファイア役職詳細を表示する
    private void ShowModifierRoleDetail(ModifierRoleId modifierRoleId)
    {
        // 既存の詳細表示を削除
        if (RoleDetailObject != null)
            GameObject.Destroy(RoleDetailObject);

        // 役職詳細表示中フラグを設定
        isShowingRoleDetail = true;

        // 役職詳細オブジェクトを作成
        RoleDetailObject = RoleDetailHelper.ShowRoleDetail(RoleId.None, Container, MenuObject, CloseRoleDetail);

        // RoleDetailMenuにMenuObjectを設定
        RoleDetailMenu.SetMenuObject(RoleDetailObject);

        // モディファイア役職の情報を表示
        IModifierBase modifierBase = CustomRoleManager.GetModifierById(modifierRoleId);
        if (modifierBase != null)
        {
            RoleDetailMenu.ShowRoleInformation(modifierBase);
        }

        // 現在表示中の役職IDを保存（通常の役職IDとして保存）
        currentDisplayedRoleId = RoleId.None;
    }

    // 役職詳細を閉じる
    private void CloseRoleDetail()
    {
        // ヘルパークラスを使用して役職詳細を閉じる
        RoleDetailHelper.CloseRoleDetail(RoleDetailObject, MenuObject);
        RoleDetailObject = null;

        // フラグをリセット
        isShowingRoleDetail = false;
        currentDisplayedRoleId = RoleId.None;
    }

    public override void UpdateShow()
    {
        // 現在表示中のチームの表示を更新
        if (CurrentTeamType != RoleOptionMenuType.Hidden && MenuObject != null)
        {
            ShowTeamRoles(CurrentTeamType);
        }
    }

    public override void Hide(GameObject Container)
    {
        // 役職詳細が表示されていれば閉じる
        if (isShowingRoleDetail)
        {
            CloseRoleDetail();
        }

        if (MenuObject != null)
        {
            GameObject.Destroy(MenuObject);
            MenuObject = null;
        }

        if (CurrentTeamContainer != null)
        {
            GameObject.Destroy(CurrentTeamContainer);
            CurrentTeamContainer = null;
        }
    }

    public override void OnUpdate()
    {
        // ヘルプメニューのインスタンスが取得できなければ詳細画面を閉じる
        if (isShowingRoleDetail)
        {
            var helpMenuObject = GameObject.Find("HelpMenuObject");
            if (helpMenuObject == null || !helpMenuObject.activeSelf)
            {
                CloseRoleDetail();
            }
        }
    }
}