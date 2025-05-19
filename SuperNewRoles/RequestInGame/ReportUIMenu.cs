using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using SuperNewRoles.CustomOptions;
using SuperNewRoles.CustomOptions.Categories;
using SuperNewRoles.Modules;
using UnityEngine;
using UnityEngine.Events;

namespace SuperNewRoles.RequestInGame;

public class ReportUIMenu
{
    public static void ShowReportUIMenu(GameObject parent, RequestInGameType requestInGameType)
    {
        GameObject reportUIMenu = GameObject.Instantiate(AssetManager.GetAsset<GameObject>("ReportUI"), parent.transform.parent);
        reportUIMenu.transform.localPosition = new(0f, 0f, -10f);
        TextBoxTMP descriptionTextBox = reportUIMenu.transform.Find("Inner/InputBoxDescription").GetComponent<TextBoxTMP>();
        TextBoxTMP titleTextBox = reportUIMenu.transform.Find("Inner/InputBoxTitle").GetComponent<TextBoxTMP>();
        ConfigureTextBox(descriptionTextBox);
        ConfigureTextBox(titleTextBox);
        GameObject Button_Send = reportUIMenu.transform.Find("Inner/Button_Send").gameObject;
        PassiveButton passiveButton = Button_Send.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { passiveButton.GetComponent<Collider2D>() };
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            if (!ValidateReport(descriptionTextBox.text, titleTextBox.text, out string errorMessage))
            {
                Logger.Error($"Report validation failed: {errorMessage}");
            }
            else
            {
                Logger.Info($"Report sent: {titleTextBox.text} - {descriptionTextBox.text}");
                SendReport(reportUIMenu.transform, requestInGameType, descriptionTextBox.text, titleTextBox.text);
            }
        }));
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            Button_Send.transform.Find("Selected").gameObject.SetActive(true);
        }));
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            Button_Send.transform.Find("Selected").gameObject.SetActive(false);
        }));
    }
    private static bool ValidateReport(string description, string title, out string errorMessage)
    {
        if (string.IsNullOrEmpty(description))
        {
            errorMessage = "Description cannot be empty";
            return false;
        }
        if (string.IsNullOrEmpty(title))
        {
            errorMessage = "Title cannot be empty";
            return false;
        }
        if (title.Length <= 2)
        {
            errorMessage = "Title must be longer than 2 characters";
            return false;
        }
        if (description.Length <= 9)
        {
            errorMessage = "Description must be longer than 10 characters";
            return false;
        }
        errorMessage = null;
        return true;
    }
    private static void SendReport(Transform parent, RequestInGameType requestInGameType, string description, string title)
    {
        bool isActive = true;
        string text = "データを取得中";
        LoadingUI.ShowLoadingUI(parent, () => text, () => isActive);
        switch (requestInGameType)
        {
            case RequestInGameType.Bug:
                AmongUsClient.Instance.StartCoroutine(RequestInGameManager.GetOrCreateToken(token =>
                {
                    if (token == null)
                    {
                        Logger.Error($"Failed to get token");
                        isActive = false;
                    }
                    else
                    {
                        text = "レポートを送信中";
                        Dictionary<string, string> additionalInfo = new();
                        additionalInfo["version"] = Application.version;
                        additionalInfo["mode"] = Categories.ModeOption.ToString();
                        additionalInfo["log"] = SNRLogListener.Instance.logBuilder.ToString();
                        AmongUsClient.Instance.StartCoroutine(RequestInGameManager.SendReport(description, title, RequestInGameType.Bug.ToString(), additionalInfo, success =>
                        {
                            if (!success)
                            {
                                Logger.Error($"Failed to send report");
                            }
                            else
                            {
                                Logger.Info($"Report sent: {title} - {description}");
                                isActive = false;
                                new LateTask(() =>
                                {
                                    CreateSuccessUI(parent.parent);
                                    GameObject.Destroy(parent.gameObject);
                                }, 0f);
                            }
                        }).WrapToIl2Cpp());
                    }
                }).WrapToIl2Cpp());
                break;
            default:
                AmongUsClient.Instance.StartCoroutine(RequestInGameManager.GetOrCreateToken(token =>
                {
                    if (token == null)
                    {
                        Logger.Error($"Failed to get token");
                        isActive = false;
                    }
                    else
                    {
                        text = "レポートを送信中";
                        Dictionary<string, string> additionalInfo = new();
                        AmongUsClient.Instance.StartCoroutine(RequestInGameManager.SendReport(description, title, requestInGameType.ToString(), additionalInfo, success =>
                        {
                            if (!success)
                            {
                                Logger.Error($"Failed to send report");
                            }
                            else
                            {
                                Logger.Info($"Report sent: {title} - {description}");
                                isActive = false;
                                new LateTask(() =>
                                {
                                    CreateSuccessUI(parent.parent);
                                    GameObject.Destroy(parent.gameObject);
                                }, 0f);
                            }
                        }).WrapToIl2Cpp());
                    }
                }).WrapToIl2Cpp());
                break;
        }
    }
    private static void CreateSuccessUI(Transform parent)
    {
        GameObject successUI = GameObject.Instantiate(AssetManager.GetAsset<GameObject>("SuccessReport"), parent);
        successUI.transform.localPosition = new(0f, 0f, -10f);
        GameObject returnButton = successUI.transform.Find("ReturnButton").gameObject;
        PassiveButton passiveButton = returnButton.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { passiveButton.GetComponent<Collider2D>() };
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            SelectButtonsMenu.ShowMainMenuUI(parent.gameObject);
            GameObject.Destroy(successUI);
        }));
        passiveButton.OnMouseOver = new();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            returnButton.transform.Find("Selected").gameObject.SetActive(true);
        }));
        passiveButton.OnMouseOut = new();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            returnButton.transform.Find("Selected").gameObject.SetActive(false);
        }));
    }
    public static void ConfigureTextBox(TextBoxTMP textBox)
    {
        PassiveButton passiveButton = textBox.gameObject.AddComponent<PassiveButton>();
        passiveButton.Colliders = new Collider2D[] { textBox.GetComponent<BoxCollider2D>() };
        string defaultText = textBox.outputText.text;

        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((UnityAction)(() =>
        {
            textBox.GiveFocus();
            if (string.IsNullOrEmpty(textBox.text))
                textBox.outputText.text = "";
        }));

        passiveButton.OnMouseOver = new UnityEvent();
        passiveButton.OnMouseOver.AddListener((UnityAction)(() =>
        {
            if (!textBox.hasFocus)
                textBox.Background.color = Color.green;
        }));

        passiveButton.OnMouseOut = new UnityEvent();
        passiveButton.OnMouseOut.AddListener((UnityAction)(() =>
        {
            if (!textBox.hasFocus)
                textBox.Background.color = Color.white;
        }));

        textBox.OnFocusLost = new();
        textBox.OnFocusLost.AddListener((UnityAction)(() =>
        {
            if (string.IsNullOrEmpty(textBox.text))
                textBox.outputText.text = defaultText;
        }));
    }
}
