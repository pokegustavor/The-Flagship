using PulsarModLoader;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace The_Flagship
{
    public class PLTemperatureScreen : PLUIScreen
    {
        UIWidget panel;
        UISprite[] controlButtons;
        UILabel currentTemp;
        bool setedup = false;
        public void Engage() 
        {
            Start();
        }
        protected override void Start()
        {
            if (MyScreenHubBase == null) MyScreenHubBase = PLEncounterManager.Instance.PlayerShip.MyScreenBase;
            this.LastSparkTime = float.MinValue;
            if (this.MyScreenHubBase != null && this.MyScreenHubBase.LoadedScreenRenderTexture == null)
            {
                this.InitScreen();
            }
            if (base.GetComponent<Collider>() != null)
            {
                base.GetComponent<Collider>().isTrigger = false;
                base.GetComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
            }
            this.ResetCachedValues();
            this.ScreenSize = new Vector2(512f, 512f);
            this.MyLight = base.gameObject.GetComponent<Light>();
            this.MyLight.type = LightType.Point;
            this.MyLight.range = 3f;
            this.MyLight.intensity = 0.5f;
            this.MyLight.cullingMask = -4718594;
            this.MyRootPanel = this.CreateBlankPanel(base.gameObject.name + "_Panel", Vector3.zero, new Vector2(512f, 512f), this.MyScreenHubBase.ScreenRootPanel.transform, UIWidget.Pivot.TopLeft);
            base.GetComponent<Renderer>().material.mainTexture = null;
            ScreenID = 13;
        }
        protected override void SetupUI()
        {
            base.SetupUI();

            panel = CreatePanel("TEMPERATURE CONTROL", Vector3.zero, new Vector2(512f, 512f),UI_DarkGrey);
            controlButtons = new UISprite[2];
            controlButtons[0] = CreateButton("Dec", "-", new Vector3(142f, 221f), new Vector2(75f, 75f), UI_White);
            controlButtons[1] = CreateButton("Inc", "+", new Vector3(337f, 221f), new Vector2(75f, 75f), UI_White);
            currentTemp = CreateLabel("25°C", new Vector3(238f, 221f), 25, UI_White);
            setedup = true;
        }
        public override void OnButtonHover(UIWidget inButton)
        {
            base.OnButtonHover(inButton);
            if (this.ShouldProcessButton(inButton) && this.LastHoverButton != inButton)
            {
                this.LastHoverButton = inButton;
                base.PlaySoundEventOnAllClonedScreens("play_ship_generic_internal_computer_ui_hover");
            }
            inButton.alpha = 1f;
        }
        public override void Update()
        {
            base.Update();
            if (MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null && MyScreenHubBase.OptionalShipInfo.MyTLI != null && currentTemp != null)
            {
                string tempType;
                switch (PLXMLOptionsIO.Instance.CurrentOptions.GetStringValueAsInt("TempUnits"))
                {
                    default:
                        tempType = "°F";
                        break;
                    case 1:
                        tempType = "°C";
                        break;
                    case 2:
                        tempType = "°K";
                        break;
                }
                PLGlobal.SafeLabelSetText(currentTemp, PLGlobal.GetTempStringFromTemp(MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature) + tempType);
            }
        }
        public override bool UIIsSetup()
        {
            return base.UIIsSetup() && setedup;
        }
        public override void OnButtonClick(UIWidget inButton)
        {
            base.OnButtonClick(inButton);
            if (inButton == controlButtons[0] && MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null)
            {
                MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature -= 0.05f;
            }
            else if (inButton == controlButtons[1] && MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null)
            {
                MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature += 0.05f;
            }
            MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature = Mathf.Clamp(MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature, -2, 3);
        }
    }

    public class PLPatrolBotUpgradeScreen : MonoBehaviour 
    {
        public void setValues(Transform root, Canvas canvas, PLShipInfo ship) 
        {
            UIWorldRoot = root;
            worldUiCanvas = canvas;
            myShip = ship;
        }
        public void Assemble() 
        {
            if (UIWorldRoot == null) return;
            GameObject gameObject = new GameObject("PatrolUpgradeUI", new Type[]
            {
            typeof(Image)
            });
            gameObject.transform.SetParent(this.worldUiCanvas.transform);
            gameObject.transform.position = UIWorldRoot.transform.position;
            gameObject.transform.localRotation = this.UIWorldRoot.localRotation;
            gameObject.transform.localScale = this.UIWorldRoot.localScale * 50f;
            gameObject.layer = 3;
            this.UIRoot = gameObject;
            gameObject.GetComponent<Image>().color = Color.white * 0.4f;
            gameObject.GetComponent<RectTransform>().anchoredPosition3D = gameObject.transform.localPosition;
            gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 450f);
            gameObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 600f);
            GameObject gameObject2 = new GameObject("bg", new Type[]
            {
            typeof(Image)
            });
            gameObject2.transform.SetParent(gameObject.transform);
            gameObject2.transform.localPosition = new Vector3(0f, 120f, 0f);
            gameObject2.transform.localRotation = Quaternion.identity;
            gameObject2.transform.localScale = Vector3.one;
            gameObject2.layer = 3;
            gameObject2.GetComponent<Image>().color = Color.black * 0.4f;
            gameObject2.GetComponent<RectTransform>().anchoredPosition3D = gameObject2.transform.localPosition;
            gameObject2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 450f);
            gameObject2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 180f);
            GameObject gameObject3 = new GameObject("compicon", new Type[]
            {
            typeof(RawImage)
            });
            this.IconImage = gameObject3.GetComponent<RawImage>();
            this.IconImage.transform.SetParent(gameObject.transform);
            this.IconImage.transform.localPosition = new Vector3(0f, 140f, 0f);
            this.IconImage.transform.localRotation = Quaternion.identity;
            this.IconImage.transform.localScale = Vector3.one;
            this.IconImage.gameObject.layer = 3;
            IconImage.texture = (Texture2D)Resources.Load("Icons/95");
            this.IconImage.GetComponent<RectTransform>().anchoredPosition3D = this.IconImage.transform.localPosition;
            this.IconImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64f);
            this.IconImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64f);
            GameObject gameObject4 = new GameObject("DroneText", new Type[]
            {
            typeof(Text)
            });
            gameObject4.transform.SetParent(gameObject.transform);
            gameObject4.transform.localPosition = new Vector3(0f, -20f, 0f);
            gameObject4.transform.localRotation = Quaternion.identity;
            gameObject4.transform.localScale = Vector3.one * 0.25f;
            gameObject4.GetComponent<RectTransform>().anchoredPosition3D = gameObject4.transform.localPosition;
            gameObject4.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject4.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text UpgradeName = gameObject4.GetComponent<Text>();
            UpgradeName.font = PLGlobal.Instance.MainFont;
            UpgradeName.resizeTextMaxSize = 85;
            UpgradeName.resizeTextMinSize = 10;
            UpgradeName.resizeTextForBestFit = true;
            UpgradeName.alignment = TextAnchor.UpperCenter;
            UpgradeName.raycastTarget = false;
            UpgradeName.text = "Upgrade Drones";
            UpgradeName.color = Color.white;
            GameObject gameObject5 = new GameObject("Title", new Type[]
            {
            typeof(Text)
            });
            gameObject5.transform.SetParent(gameObject.transform);
            gameObject5.transform.localPosition = new Vector3(0f, 175f, 0f);
            gameObject5.transform.localRotation = Quaternion.identity;
            gameObject5.transform.localScale = Vector3.one * 0.25f;
            gameObject5.GetComponent<RectTransform>().anchoredPosition3D = gameObject5.transform.localPosition;
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1450f);
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text ScreenTitle = gameObject5.GetComponent<Text>();
            ScreenTitle.font = PLGlobal.Instance.MainFont;
            ScreenTitle.resizeTextMaxSize = 85;
            ScreenTitle.resizeTextMinSize = 10;
            ScreenTitle.resizeTextForBestFit = true;
            ScreenTitle.alignment = TextAnchor.UpperLeft;
            ScreenTitle.raycastTarget = false;
            ScreenTitle.text = PLLocalize.Localize("PATROL BOTS UPGRADER", false);
            ScreenTitle.color = Color.white;
            GameObject gameObject6 = new GameObject("SHIPCOMP_STATTYPE", new Type[]
        {
            typeof(Text)
        });
            gameObject6.transform.SetParent(gameObject5.transform);
            gameObject6.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            gameObject6.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            gameObject6.transform.localRotation = Quaternion.identity;
            gameObject6.transform.localScale = Vector3.one;
            gameObject6.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-100f, 0f, 0f);
            gameObject6.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject6.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Descriptions[0] = gameObject6.GetComponent<Text>();
            Descriptions[0].font = PLGlobal.Instance.MainFont;
            Descriptions[0].resizeTextMaxSize = 64;
            Descriptions[0].resizeTextMinSize = 10;
            Descriptions[0].resizeTextForBestFit = true;
            Descriptions[0].alignment = TextAnchor.UpperLeft;
            Descriptions[0].raycastTarget = false;
            Descriptions[0].text = "-\r\nHealth\r\n\r\nDamage\r\n\r\nSpeed";
            Descriptions[0].color = Color.gray;
            Descriptions[0].transform.localPosition -= new Vector3(0, 700, 0);
            GameObject gameObject7 = new GameObject("SHIPCOMP_STATLEFT", new Type[]
            {
            typeof(Text)
            });
            gameObject7.transform.SetParent(gameObject5.transform);
            gameObject7.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            gameObject7.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            gameObject7.transform.localRotation = Quaternion.identity;
            gameObject7.transform.localScale = Vector3.one;
            gameObject7.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-400f, 0f, 0f);
            gameObject7.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject7.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Descriptions[1] = gameObject7.GetComponent<Text>();
            Descriptions[1].font = PLGlobal.Instance.MainFont;
            Descriptions[1].resizeTextMaxSize = 64;
            Descriptions[1].resizeTextMinSize = 10;
            Descriptions[1].resizeTextForBestFit = true;
            this.Descriptions[1].alignment = TextAnchor.UpperRight;
            this.Descriptions[1].raycastTarget = false;
            this.Descriptions[1].text = "SHIPCOMP_STATLEFT";
            this.Descriptions[1].color = Color.gray;
            Descriptions[1].transform.localPosition -= new Vector3(0, 700, 0);
            GameObject gameObject8 = new GameObject("SHIPCOMP_STATRIGHT", new Type[]
            {
            typeof(Text)
            });
            gameObject8.transform.SetParent(gameObject5.transform);
            gameObject8.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            gameObject8.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            gameObject8.transform.localRotation = Quaternion.identity;
            gameObject8.transform.localScale = Vector3.one;
            gameObject8.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(155f, 0f, 0f);
            gameObject8.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject8.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            this.Descriptions[2] = gameObject8.GetComponent<Text>();
            this.Descriptions[2].font = PLGlobal.Instance.MainFont;
            this.Descriptions[2].resizeTextMaxSize = 64;
            this.Descriptions[2].resizeTextMinSize = 10;
            this.Descriptions[2].resizeTextForBestFit = true;
            this.Descriptions[2].alignment = TextAnchor.UpperRight;
            this.Descriptions[2].raycastTarget = false;
            this.Descriptions[2].text = "SHIPCOMP_STATRIGHT";
            this.Descriptions[2].color = new Color(0.75f, 0.75f, 0.75f, 1f);
            Descriptions[2].transform.localPosition -= new Vector3(0, 700, 0);
            GameObject gameObject9 = new GameObject("EXTRACT_BTN", new Type[]
        {
            typeof(Image),
            typeof(Button)
        });
            Button UpgradeBtn = gameObject9.GetComponent<Button>();
            Image BtnImage = gameObject9.GetComponent<Image>();
            BtnImage.sprite = PLGlobal.Instance.TabFillSprite;
            BtnImage.type = Image.Type.Sliced;
            BtnImage.transform.SetParent(gameObject.transform);
            UpgradeBtn.transform.localPosition = new Vector3(70f, -250f, 0f);
            UpgradeBtn.transform.localRotation = Quaternion.identity;
            UpgradeBtn.transform.localScale = Vector3.one;
            UpgradeBtn.gameObject.layer = 3;
            UpgradeBtn.GetComponent<RectTransform>().anchoredPosition3D = UpgradeBtn.transform.localPosition;
            UpgradeBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 250f);
            UpgradeBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            ColorBlock colors = UpgradeBtn.colors;
            colors.normalColor = Color.gray;
            UpgradeBtn.colors = colors;
            UpgradeBtn.onClick.AddListener(delegate ()
            {
                UpgradeClick();
            });
            GameObject gameObject10 = new GameObject("ExtractBtnLabel", new Type[]
            {
            typeof(Text)
            });
            gameObject10.transform.SetParent(gameObject9.transform);
            gameObject10.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject10.transform.localRotation = Quaternion.identity;
            gameObject10.transform.localScale = Vector3.one;
            Text component = gameObject10.GetComponent<Text>();
            component.alignment = TextAnchor.MiddleCenter;
            component.resizeTextForBestFit = true;
            component.resizeTextMinSize = 8;
            component.resizeTextMaxSize = 18;
            component.color = Color.black;
            component.raycastTarget = false;
            component.text = PLLocalize.Localize("UPGRADE", false);
            component.font = PLGlobal.Instance.MainFont;
            component.GetComponent<RectTransform>().anchoredPosition3D = component.transform.localPosition;
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            GameObject gameObject11 = new GameObject("Label", new Type[]
        {
            typeof(Text)
        });
            gameObject11.transform.SetParent(gameObject.transform);
            gameObject11.transform.localPosition = new Vector3(-130f, -250f, 0f);
            gameObject11.transform.localRotation = Quaternion.identity;
            gameObject11.transform.localScale = Vector3.one * 0.25f;
            gameObject11.GetComponent<RectTransform>().anchoredPosition3D = gameObject11.transform.localPosition;
            gameObject11.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject11.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text component2 = gameObject11.GetComponent<Text>();
            CostLabel = component2;
            component2.font = PLGlobal.Instance.MainFont;
            component2.resizeTextMaxSize = 150;
            component2.resizeTextMinSize = 10;
            component2.resizeTextForBestFit = true;
            component2.alignment = TextAnchor.MiddleCenter;
            component2.raycastTarget = false;
            component2.text = "60";
            component2.color = Color.white;
            GameObject gameObject12 = new GameObject("UpgIcon", new Type[]
            {
            typeof(Image)
            });
            gameObject12.transform.SetParent(gameObject11.transform);
            gameObject12.transform.localPosition = new Vector3(-175f, 0f, 0f);
            gameObject12.transform.localRotation = Quaternion.identity;
            gameObject12.transform.localScale = Vector3.one;
            gameObject12.layer = 3;
            Image component3 = gameObject12.GetComponent<Image>();
            component3.sprite = PLGlobal.Instance.UpgradeMaterialIconSprite;
            component3.GetComponent<RectTransform>().anchoredPosition3D = gameObject12.transform.localPosition;
            component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 128f);
            component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 128f);
            GameObject gameObject13 = new GameObject("Label", new Type[]
            {
            typeof(Text)
            });
            gameObject13.transform.SetParent(gameObject.transform);
            gameObject13.transform.localPosition = new Vector3(175f, 262f, 0f);
            gameObject13.transform.localRotation = Quaternion.identity;
            gameObject13.transform.localScale = Vector3.one * 0.25f;
            gameObject13.GetComponent<RectTransform>().anchoredPosition3D = gameObject13.transform.localPosition;
            gameObject13.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject13.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text component4 = gameObject13.GetComponent<Text>();
            CurrentScrap = component4;
            component4.font = PLGlobal.Instance.MainFont;
            component4.resizeTextMaxSize = 150;
            component4.resizeTextMinSize = 10;
            component4.resizeTextForBestFit = true;
            component4.alignment = TextAnchor.MiddleCenter;
            component4.raycastTarget = false;
            component4.text = "60";
            component4.color = Color.white;
            GameObject gameObject14 = new GameObject("UpgIcon", new Type[]
            {
            typeof(Image)
            });
            gameObject14.transform.SetParent(gameObject13.transform);
            gameObject14.transform.localPosition = new Vector3(-175f, 0f, 0f);
            gameObject14.transform.localRotation = Quaternion.identity;
            gameObject14.transform.localScale = Vector3.one;
            gameObject14.layer = 3;
            Image component5 = gameObject14.GetComponent<Image>();
            component5.sprite = PLGlobal.Instance.UpgradeMaterialIconSprite;
            component5.GetComponent<RectTransform>().anchoredPosition3D = gameObject14.transform.localPosition;
            component5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 128f);
            component5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 128f);
            Assembled = true;
        }
        void UpgradeClick() 
        {
            if (PLNetworkManager.Instance.LocalPlayer == null || PLNetworkManager.Instance.LocalPlayer.Talents[55] == 0)
            {
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Can't upgrade the bots without Component Upgrader talent", false);
                return;
            }
            if (Mod.PatrolBotsLevel >= 9)
            {
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Patrol bots already at max level", false);
                return;
            }
            if (PLServer.Instance.CurrentUpgradeMats < NextUpgradePrice)
            {
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Can't afford patrol bot upgrade. Process more scrap!", false);
                return;
            }
            if (Time.time - LastUpgradeAttempt > 2f)
            {
                LastUpgradeAttempt = Time.time;
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.UpgradePatrolReciever", PhotonTargets.MasterClient, new object[] { NextUpgradePrice});
                PLMusic.PostEvent("play_sx_ui_ship_upgradecomponent", base.gameObject);
            }
        }
        void Update() 
        {
            if(myShip == null) 
            {
                Destroy(this); return;
            }
            if (Assembled) 
            {
                CurrentScrap.text = PLServer.Instance.CurrentUpgradeMats.ToString();
                Descriptions[1].text = $"Level {Mod.PatrolBotsLevel + 1}\r\n{150 + 25 * Mod.PatrolBotsLevel}\r\n\r\n{30 + 5 * Mod.PatrolBotsLevel}\r\n\r\n{1f + 0.2f * Mod.PatrolBotsLevel}";
                if (Mod.PatrolBotsLevel < 9) Descriptions[2].text = $"Level {Mod.PatrolBotsLevel + 2}\r\n{150 + 25 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{30 + 5 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{1f + 0.2f * (Mod.PatrolBotsLevel + 1)}";
                else Descriptions[2].text = string.Empty;
                NextUpgradePrice = Mathf.FloorToInt(30 + 30 * 0.1f * Mod.PatrolBotsLevel); 
                CostLabel.text = NextUpgradePrice.ToString();
            }
        }

        bool Assembled = false;
        int NextUpgradePrice = 0;
        Text CostLabel;
        Text CurrentScrap;
        float LastUpgradeAttempt = Time.time;
        Text[] Descriptions = new Text[3];
        Transform UIWorldRoot;
        GameObject UIRoot;
        Canvas worldUiCanvas;
        PLShipInfo myShip;
        RawImage IconImage;
    }
    class UpgradePatrolReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if ((int)arguments[0] <= PLServer.Instance.CurrentUpgradeMats)
            {
                Mod.PatrolBotsLevel = Mathf.Clamp(Mod.PatrolBotsLevel + 1, 0, 9);
                if (PhotonNetwork.isMasterClient)
                {
                    SendRPC("pokegustavo.theflagship", "The_Flagship.PLPatrolBotUpgradeScreen.UpgradePatrolReciever", PhotonTargets.Others, new object[] { 0 });
                    PLServer.Instance.photonView.RPC("AddCrewWarning_OneString_Localized", PhotonTargets.All, new object[]
                    {
                        "[STR0] UPGRADED!",
                        Color.blue,
                        0,
                        "SHIP",
                        "PATROL BOTS"
                    });
                    PLServer.Instance.CurrentUpgradeMats -= (int)arguments[0];
                }
            }
        }
    }
}