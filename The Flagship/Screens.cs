using PulsarModLoader;
using PulsarModLoader.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using HarmonyLib;
using System.Linq;

namespace The_Flagship
{
    public class PLModdedScreenBase : PLUIScreen
    {
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
        }
        protected override void SetupUI()
        {
            base.SetupUI();
            setedup = true;
        }
        public override bool UIIsSetup()
        {
            return base.UIIsSetup() && setedup;
        }
    }
    public class PLTemperatureScreen : PLModdedScreenBase
    {
        UIWidget panel;
        UISprite[] controlButtons;
        UILabel currentTemp;
        protected override void Start()
        {
            base.Start();
            ScreenID = 13;
        }
        protected override void SetupUI()
        {
            base.SetupUI();

            panel = CreatePanel("TEMPERATURE CONTROL", Vector3.zero, new Vector2(512f, 512f), UI_DarkGrey);
            controlButtons = new UISprite[2];
            controlButtons[0] = CreateButton("Dec", "-", new Vector3(142f, 221f), new Vector2(75f, 75f), UI_White);
            controlButtons[1] = CreateButton("Inc", "+", new Vector3(337f, 221f), new Vector2(75f, 75f), UI_White);
            currentTemp = CreateLabel("25°C", new Vector3(238f, 221f), 25, UI_White);
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
            ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.TemperatureReciever", PhotonTargets.All, new object[]
            {
                MyScreenHubBase.OptionalShipInfo.ShipID,
                Mathf.Clamp(MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature, -2, 3)
            });
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
            Descriptions[0].text = "-\r\nHealth\r\n\r\nDamage/Repair\r\n\r\nSpeed";
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
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.UpgradePatrolReciever", PhotonTargets.MasterClient, new object[] { NextUpgradePrice });
                PLMusic.PostEvent("play_sx_ui_ship_upgradecomponent", base.gameObject);
            }
        }
        void Update()
        {
            if (myShip == null)
            {
                Destroy(this); return;
            }
            if (Assembled)
            {
                CurrentScrap.text = PLServer.Instance.CurrentUpgradeMats.ToString();
                Descriptions[1].text = $"Level {Mod.PatrolBotsLevel + 1}\r\n{155 + 25 * Mod.PatrolBotsLevel}\r\n\r\n{30 + 5 * Mod.PatrolBotsLevel}\r\n\r\n{1f + 0.2f * Mod.PatrolBotsLevel}";
                if (Mod.PatrolBotsLevel < 9) Descriptions[2].text = $"Level {Mod.PatrolBotsLevel + 2}\r\n{155 + 25 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{30 + 5 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{1f + 0.2f * (Mod.PatrolBotsLevel + 1)}";
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
    public class PLAutoRepairScreen : MonoBehaviour
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
            IconImage.texture = (Texture2D)Resources.Load("Icons/89");
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
            UpgradeName.text = "Hull Auto Repair";
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
            ScreenTitle.text = PLLocalize.Localize("Auto Repair System Control", false);
            ScreenTitle.color = Color.white;
            GameObject gameObject8 = new GameObject("SHIPCOMP_STATRIGHT", new Type[]
            {
            typeof(Text)
            });
            gameObject8.transform.SetParent(gameObject.transform);
            gameObject8.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            gameObject8.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            gameObject8.transform.localRotation = Quaternion.identity;
            gameObject8.transform.localScale = Vector3.one;
            gameObject8.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(155f, 0f, 0f);
            gameObject8.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            gameObject8.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            PowerUsageLabel = gameObject8.GetComponent<Text>();
            PowerUsageLabel.font = PLGlobal.Instance.MainFont;
            PowerUsageLabel.resizeTextMaxSize = 18;
            PowerUsageLabel.resizeTextMinSize = 8;
            PowerUsageLabel.resizeTextForBestFit = true;
            PowerUsageLabel.alignment = TextAnchor.UpperLeft;
            PowerUsageLabel.raycastTarget = false;
            PowerUsageLabel.text = "Current Power Usage: \r\n\r\nCurrent Repair Power: ";
            PowerUsageLabel.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            PowerUsageLabel.transform.localPosition = new Vector3(524f, -451f, 0f);
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
            UpgradeBtn.transform.localPosition = new Vector3(-5f, -250f, 0f);
            UpgradeBtn.transform.localRotation = Quaternion.identity;
            UpgradeBtn.GetComponent<Image>().SetNativeSize();
            UpgradeBtn.transform.localScale = new Vector3(1.9f, 2, 1);
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
            component.text = PLLocalize.Localize("Activate Auto Repair", false);
            component.font = PLGlobal.Instance.MainFont;
            component.GetComponent<RectTransform>().anchoredPosition3D = component.transform.localPosition;
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            ButtonLabel = component;
            Assembled = true;
        }
        void UpgradeClick()
        {
            Online = !Online;
            ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.AutoRepairReciever", PhotonTargets.Others, new object[] { Online, CurrentMultiplier });
            PLMusic.PostEvent("play_sx_ui_ship_upgradecomponent", base.gameObject);
        }
        void Update()
        {
            if (myShip == null)
            {
                Destroy(this); return;
            }
            if (Assembled)
            {
                ButtonLabel.text = (Online ? "Deactivate" : "Activate") + " Auto Repair";
                CurrentMultiplier = Mathf.Clamp(CurrentMultiplier + Time.deltaTime * (Online ? 1 : -1),0.1f,70);
                float valueMultier = Mathf.Log(15 * CurrentMultiplier, 2);
                powerusage = 4000 * valueMultier;
                float repairvalue = 5 * valueMultier;
                PowerUsageLabel.text = $"Current Power Usage: {Mathf.FloorToInt(powerusage)}MW\r\n\r\nCurrent Repair Power: {repairvalue.ToString("0")}/s";
                if (Online && myShip.MyHull != null && !myShip.IsReactorOverheated() && myShip.StartupStepIndex >= 1 && myShip.StartupSwitchBoard != null && myShip.StartupSwitchBoard.GetLateStatus(0)) 
                {
                    if(myShip.MyHull != null) 
                    {
                        myShip.MyHull.Current += repairvalue * Time.deltaTime;
                        myShip.MyHull.Current = Mathf.Clamp(myShip.MyHull.Current, 0, myShip.MyStats.HullMax);
                    }

                }
                else 
                {
                    powerusage = 0f;
                }
                if(PhotonNetwork.isMasterClient && Time.time - lastSync > 5) 
                {
                    lastSync = Time.time;
                    ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.AutoRepairReciever", PhotonTargets.Others, new object[] { Online, CurrentMultiplier });
                }
            }
        }
        float lastSync = Time.time;
        public static float powerusage = 0f;
        Text ButtonLabel;
        public static bool Online = false;
        bool Assembled = false;
        public static float CurrentMultiplier = 1;
        Text PowerUsageLabel;
        Transform UIWorldRoot;
        GameObject UIRoot;
        Canvas worldUiCanvas;
        PLShipInfo myShip;
        RawImage IconImage;
    }
    public class PLArmorBonusScreen : PLModdedScreenBase
    {
        UIWidget panel;
        UISprite[] difficultyButtons;
        UISprite[] gameButtons;
        UILabel[,] gameLabels;
        UILabel description;
        UIPanel barMask;
        UISprite barOutline;
        UISprite bar;
        int difficulty = 0;
        bool onGame = false;
        bool XTurn = true;
        int AITurn = 0;
        public float VictoryTimer = 0;
        public float CooldownTimer = 0;
        float lastSync = 0;
        protected override void Start()
        {
            base.Start();
            ScreenID = 14;
        }
        protected override void SetupUI()
        {
            base.SetupUI();

            panel = CreatePanel("ARMOR HARDENING", Vector3.zero, new Vector2(512f, 512f), UI_DarkGrey);
            difficultyButtons = new UISprite[3];
            gameButtons = new UISprite[9];
            gameLabels = new UILabel[3, 3];
            description = CreateLabel("Select your difficulty:", new Vector3(80, 140), 20, UI_White);
            bar = CreateHorizontalBar(new Vector3(2, 200), new Vector2(510f, 75f), 1f, UI_Red, null, out barMask, out barOutline);
            difficultyButtons[0] = CreateButton("Easy", "Easy 1.5x extra armor", new Vector3(2f, 210f), new Vector2(510f, 75f), Color.green);
            difficultyButtons[1] = CreateButton("Med", "Medium 2x extra armor", new Vector3(2f, 290f), new Vector2(510f, 75f), UI_PoweredBlue);
            difficultyButtons[2] = CreateButton("Hard", "Hard 3x extra armor", new Vector3(2f, 370f), new Vector2(510f, 75f), UI_Red);
            for (int i = 0; i < 9; i++)
            {
                gameButtons[i] = CreateButtonEditable(i.ToString(), "", new Vector3(50 + 137 * (i % 3), 50 + 137 * (i / 3)), new Vector3(137, 137), UI_White, out gameLabels[i % 3, i / 3]);
            }

        }
        public override void OnButtonClick(UIWidget inButton)
        {
            base.OnButtonClick(inButton);
            if (!onGame)
            {
                if (inButton == difficultyButtons[0])
                {
                    difficulty = 1;
                    onGame = true;
                    XTurn = true;
                }
                if (inButton == difficultyButtons[1])
                {
                    difficulty = 2;
                    onGame = true;
                    XTurn = true;
                }
                if (inButton == difficultyButtons[2])
                {
                    difficulty = 3;
                    onGame = true;
                    XTurn = true;
                }
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    if (inButton == gameButtons[i] && gameLabels[i % 3, i / 3].text == "" && XTurn)
                    {
                        gameLabels[i % 3, i / 3].text = "X";
                        XTurn = false;
                        CheckGame(gameLabels);
                        break;
                    }
                }
            }
        }
        public override void Update()
        {
            base.Update();
            if (onGame && (VictoryTimer > 0 || CooldownTimer > 0))
            {
                difficulty = 0;
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        gameLabels[row, col].text = "";
                    }
                }
                XTurn = true;
                AITurn = 0;
                onGame = false;
            }
            for (int i = 0; i < 3; i++)
            {
                difficultyButtons[i].alpha = (onGame || VictoryTimer > 0 || CooldownTimer > 0 ? 0 : 1);
            }
            for (int i = 0; i < 9; i++)
            {
                gameButtons[i].alpha = (onGame ? 1 : 0);
                gameLabels[i % 3, i / 3].alpha = (onGame ? 1 : 0);
            }
            description.alpha = (onGame ? 0 : 1);
            bar.alpha = (onGame || (VictoryTimer <= 0 && CooldownTimer <= 0) ? 0 : 1);
            barMask.alpha = (onGame || (VictoryTimer <= 0 && CooldownTimer <= 0) ? 0 : 1);
            barOutline.alpha = (onGame || (VictoryTimer <= 0 && CooldownTimer <= 0) ? 0 : 1);
            if (PhotonNetwork.isMasterClient && Time.time - lastSync > 5)
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.ArmorDataReciever", PhotonTargets.Others, new object[]
                {
                MyScreenHubBase.OptionalShipInfo.ShipID,
                VictoryTimer,
                CooldownTimer,
                ShipStats.armorModifier
                });
                lastSync = Time.time;
            }
            if (onGame)
            {
                if (!XTurn)
                {
                    switch (difficulty)
                    {
                        case 1:
                            Easy(gameLabels, "O");
                            XTurn = true;
                            AITurn++;
                            CheckGame(gameLabels);
                            break;
                        case 2:
                            Medium(gameLabels, "O");
                            XTurn = true;
                            AITurn++;
                            CheckGame(gameLabels);
                            break;
                        case 3:
                            Hard(gameLabels, "O");
                            XTurn = true;
                            AITurn++;
                            CheckGame(gameLabels);
                            break;
                    }
                }
            }
            else
            {
                if (VictoryTimer > 0)
                {
                    VictoryTimer -= Time.deltaTime;
                    description.text = "Time remaining: " + VictoryTimer.ToString("0.00");
                    bar.color = Color.red;
                    barMask.clipOffset = new Vector2(((VictoryTimer / 120) - 1) * 510, 0);

                }
                else if (CooldownTimer > 0)
                {
                    CooldownTimer -= Time.deltaTime;
                    description.text = "Recharging buff: " + ((1 - (CooldownTimer / 240)) * 100).ToString("0.00") + "%";
                    bar.color = Color.red;
                    barMask.clipOffset = new Vector2(-((CooldownTimer / 240)) * 510, 0);
                    ShipStats.armorModifier = 1f;
                }
                else
                {
                    description.text = "Select your difficulty:";
                }
            }
        }
        public void OnCompletion(int difficulty, bool playerWon)
        {
            if (playerWon)
            {
                switch (difficulty)
                {
                    case 1:
                        ShipStats.armorModifier = 1.5f;
                        break;
                    case 2:
                        ShipStats.armorModifier = 2f;
                        break;
                    case 3:
                        ShipStats.armorModifier = 3f;
                        break;
                }
                VictoryTimer = 120f;
                CooldownTimer = 240f;
            }
            else
            {
                MyScreenHubBase.OptionalShipInfo.MyStats.ReactorTempCurrent += MyScreenHubBase.OptionalShipInfo.MyStats.ReactorTempMax * 0.25f;
            }
        }
        void CheckGame(UILabel[,] board)
        {
            bool gameFinished = false;
            bool playerWon = false;
            if (CheckWin(board, "X"))
            {
                gameFinished = true;
                playerWon = true;
            }
            else if (CheckWin(board, "O") || CheckDraw(board))
            {
                gameFinished = true;
            }
            if (gameFinished)
            {
                onGame = false;
                OnCompletion(difficulty, playerWon);
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.ArmorCompletionReciever", PhotonTargets.Others, new object[]
                {
                MyScreenHubBase.OptionalShipInfo.ShipID,
                difficulty,
                playerWon,
                });
                difficulty = 0;
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        gameLabels[row, col].text = "";
                    }
                }
                XTurn = true;
                AITurn = 0;
            }

        }
        void Easy(UILabel[,] board, string player)
        {
            System.Random random = new System.Random();
            int row = random.Next(0, 3);
            int col = random.Next(0, 3);
            int attempt = 0;
            while (board[row, col].text != "" && attempt < 1000)
            {
                row = random.Next(0, 3);
                col = random.Next(0, 3);
                attempt++;
            }
            if (attempt >= 1000)
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (board[i, j].text == "")
                        {
                            board[i, j].text = player;
                            return;
                        }
                    }
                }
                return;
            }
            board[row, col].text = player;
        }
        void Medium(UILabel[,] board, string player)
        {

            // Check for a winning move
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].text == "")
                    {
                        board[i, j].text = player;
                        if (CheckWin(board, player))
                        {
                            return;
                        }
                        board[i, j].text = "";
                    }
                }
            }

            // Check for a blocking move
            string opponentSymbol = player == "X" ? "O" : "X";
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].text == "")
                    {
                        board[i, j].text = opponentSymbol;
                        if (CheckWin(board, opponentSymbol))
                        {
                            board[i, j].text = player;
                            return;
                        }
                        board[i, j].text = "";
                    }
                }
            }

            // Try to take the center
            if (board[1, 1].text == "")
            {
                board[1, 1].text = player;
                return;
            }

            // Try to take a corner
            int[] corners = { 0, 2 };
            System.Random random = new System.Random();
            foreach (int i in corners)
            {
                foreach (int j in corners)
                {
                    if (board[i, j].text == "")
                    {
                        board[i, j].text = player;
                        return;
                    }
                }
            }

            // Take any available spot
            Easy(board, player);
        }
        void Hard(UILabel[,] board, string symbol)
        {
            if (AITurn == 1 && board[1, 1].text == symbol)
            {
                if ((board[0, 0].text != "" && board[2, 2].text != "") || (board[2, 0].text != "" && board[0, 2].text != ""))
                {
                    board[0, 1].text = symbol;
                    return;
                }
            }
            // Check if we can win in the next move
            int[] winMove = FindWinningMove(board, symbol);
            if (winMove != null)
            {
                board[winMove[0], winMove[1]].text = symbol;
                return;
            }
            // Check if we need to block the opponent's winning move
            string opponentSymbol = symbol == "X" ? "O" : "X";
            int[] blockMove = FindWinningMove(board, opponentSymbol);
            if (blockMove != null)
            {
                board[blockMove[0], blockMove[1]].text = symbol;
                return;
            }
            // Check if we can take the center
            if (board[1, 1].text == "")
            {
                board[1, 1].text = symbol;
                return;
            }
            // Check if we can take a corner opposite to opponent's last move
            int[] oppositeCornerMove = FindOppositeCornerMove(board, opponentSymbol);
            if (oppositeCornerMove != null && board[1, 1].text == "")
            {
                board[oppositeCornerMove[0], oppositeCornerMove[1]].text = symbol;
                return;
            }
            // Check if we can take a corner
            int[] cornerMove = FindCornerMove(board);
            if (cornerMove != null)
            {
                board[cornerMove[0], cornerMove[1]].text = symbol;
                return;
            }
            // Check if we can block two simultaneous winning moves of the opponent
            int[] simultaneousWinningMove = FindSimultaneousWinningMove(board, opponentSymbol);
            if (simultaneousWinningMove != null)
            {
                board[simultaneousWinningMove[0], simultaneousWinningMove[1]].text = symbol;
                return;
            }
            Easy(board, symbol);
        }
        bool CheckWin(UILabel[,] board, string player)
        {
            // Verifica se o jogador ganhou na horizontal
            for (int row = 0; row < 3; row++)
            {
                if (board[row, 0].text == player && board[row, 1].text == player && board[row, 2].text == player)
                {
                    return true;
                }
            }
            // Verifica se o jogador ganhou na vertical
            for (int col = 0; col < 3; col++)
            {
                if (board[0, col].text == player && board[1, col].text == player && board[2, col].text == player)
                {
                    return true;
                }
            }
            // Verifica se o jogador ganhou na diagonal
            if (board[0, 0].text == player && board[1, 1].text == player && board[2, 2].text == player)
            {
                return true;
            }
            if (board[0, 2].text == player && board[1, 1].text == player && board[2, 0].text == player)
            {
                return true;
            }
            // Se nenhum dos casos anteriores for verdadeiro, retorna falso
            return false;
        }
        bool CheckDraw(UILabel[,] board)
        {
            // Verifica se todas as casas estão preenchidas
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (board[row, col].text == "")
                    {
                        return false;
                    }
                }
            }
            // Se todas as casas estiverem preenchidas e ninguém ganhou, é um empate
            return true;
        }
        private int[] FindWinningMove(UILabel[,] board, string symbol)
        {
            // Check rows
            for (int i = 0; i < 3; i++)
            {
                int count = 0;
                int emptyIndex = -1;
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].text == symbol)
                    {
                        count++;
                    }
                    else if (board[i, j].text == "")
                    {
                        emptyIndex = j;
                    }
                }
                if (count == 2 && emptyIndex != -1)
                {
                    return new int[] { i, emptyIndex };
                }
            }

            // Check columns
            for (int j = 0; j < 3; j++)
            {
                int count = 0;
                int emptyIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (board[i, j].text == symbol)
                    {
                        count++;
                    }
                    else if (board[i, j].text == "")
                    {
                        emptyIndex = i;
                    }
                }
                if (count == 2 && emptyIndex != -1)
                {
                    return new int[] { emptyIndex, j };
                }
            }

            // Check diagonals
            if (board[0, 0].text == symbol && board[1, 1].text == symbol && board[2, 2].text == "")
            {
                return new int[] { 2, 2 };
            }
            if (board[0, 0].text == symbol && board[2, 2].text == symbol && board[1, 1].text == "")
            {
                return new int[] { 1, 1 };
            }
            if (board[1, 1].text == symbol && board[2, 2].text == symbol && board[0, 0].text == "")
            {
                return new int[] { 0, 0 };
            }
            if (board[0, 2].text == symbol && board[1, 1].text == symbol && board[2, 0].text == "")
            {
                return new int[] { 2, 0 };
            }
            if (board[0, 2].text == symbol && board[2, 0].text == symbol && board[1, 1].text == "")
            {
                return new int[] { 1, 1 };
            }
            if (board[1, 1].text == symbol && board[2, 0].text == symbol && board[0, 2].text == "")
            {
                return new int[] { 0, 2 };
            }

            return null;
        }
        private int[] FindOppositeCornerMove(UILabel[,] board, string symbol)
        {
            if (board[0, 0].text == symbol && board[2, 2].text == "" ||
                board[0, 2].text == symbol && board[2, 0].text == "" ||
                board[2, 0].text == symbol && board[0, 2].text == "" ||
                board[2, 2].text == symbol && board[0, 0].text == "")
            {
                return new int[] { 1, 1 };
            }

            return null;
        }
        private int[] FindCornerMove(UILabel[,] board)
        {
            if (board[0, 0].text == "")
            {
                return new int[] { 0, 0 };
            }
            if (board[0, 2].text == "")
            {
                return new int[] { 0, 2 };
            }
            if (board[2, 0].text == "")
            {
                return new int[] { 2, 0 };
            }
            if (board[2, 2].text == "")
            {
                return new int[] { 2, 2 };
            }

            return null;
        }
        private int[] FindSimultaneousWinningMove(UILabel[,] board, string symbol)
        {
            // Check rows
            for (int i = 0; i < 3; i++)
            {
                int count = 0;
                int emptyIndex = -1;
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].text == symbol)
                    {
                        count++;
                    }
                    else if (board[i, j].text == "")
                    {
                        emptyIndex = j;
                    }
                }
                if (count == 1 && emptyIndex != -1)
                {
                    // Check if placing the symbol in this position will allow the opponent to win
                    UILabel[,] boardCopy = (UILabel[,])board.Clone();
                    boardCopy[i, emptyIndex].text = symbol;
                    if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                    {
                        continue;
                    }

                    // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                    boardCopy[i, emptyIndex].text = (symbol == "X" ? "O" : "X");
                    if (FindWinningMove(boardCopy, symbol) != null)
                    {
                        return new int[] { i, emptyIndex };
                    }
                }
            }

            // Check columns
            for (int j = 0; j < 3; j++)
            {
                int count = 0;
                int emptyIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (board[i, j].text == symbol)
                    {
                        count++;
                    }
                    else if (board[i, j].text == "")
                    {
                        emptyIndex = i;
                    }
                }
                if (count == 1 && emptyIndex != -1)
                {
                    // Check if placing the symbol in this position will allow the opponent to win
                    UILabel[,] boardCopy = (UILabel[,])board.Clone();
                    boardCopy[emptyIndex, j].text = symbol;
                    if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                    {
                        continue;
                    }

                    // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                    boardCopy[emptyIndex, j].text = (symbol == "X" ? "O" : "X");
                    if (FindWinningMove(boardCopy, symbol) != null)
                    {
                        return new int[] { emptyIndex, j };
                    }
                }
            }

            // Check diagonals
            if (board[0, 0].text == symbol && board[1, 1].text == "" && board[2, 2].text == symbol)
            {
                // Check if placing the symbol in this position will allow the opponent to win
                UILabel[,] boardCopy = (UILabel[,])board.Clone();
                boardCopy[1, 1].text = symbol;
                if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                {
                    return null;
                }

                // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                boardCopy[1, 1].text = (symbol == "X" ? "O" : "X");
                if (FindWinningMove(boardCopy, symbol) != null)
                {
                    return new int[] { 1, 1 };
                }
            }
            if (board[0, 0].text == symbol && board[1, 1].text == symbol && board[2, 2].text == "")
            {
                // Check if placing the symbol in this position will allow the opponent to
                // Check if placing the symbol in this position will allow the opponent to win
                UILabel[,] boardCopy = (UILabel[,])board.Clone();
                boardCopy[2, 2].text = symbol;
                if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                {
                    return null;
                }

                // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                boardCopy[2, 2].text = (symbol == "X" ? "O" : "X");
                if (FindWinningMove(boardCopy, symbol) != null)
                {
                    return new int[] { 2, 2 };
                }
            }
            if (board[0, 2].text == symbol && board[1, 1].text == "" && board[2, 0].text == symbol)
            {
                // Check if placing the symbol in this position will allow the opponent to win
                UILabel[,] boardCopy = (UILabel[,])board.Clone();
                boardCopy[1, 1].text = symbol;
                if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                {
                    return null;
                }

                // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                boardCopy[1, 1].text = (symbol == "X" ? "O" : "X");
                if (FindWinningMove(boardCopy, symbol) != null)
                {
                    return new int[] { 1, 1 };
                }
            }
            if (board[0, 2].text == symbol && board[1, 1].text == symbol && board[2, 0].text == "")
            {
                // Check if placing the symbol in this position will allow the opponent to win
                UILabel[,] boardCopy = (UILabel[,])board.Clone();
                boardCopy[2, 0].text = symbol;
                if (FindWinningMove(boardCopy, (symbol == "X" ? "O" : "X")) != null)
                {
                    return null;
                }

                // Check if placing the symbol in this position will allow the opponent to have two simultaneous winning moves
                boardCopy[2, 0].text = (symbol == "X" ? "O" : "X");
                if (FindWinningMove(boardCopy, symbol) != null)
                {
                    return new int[] { 2, 0 };
                }
            }

            // If no simultaneous winning move is found, return null
            return null;
        }

        UISprite CreateHorizontalBar(Vector3 inPosition, Vector2 inSize, float inValue, Color inInteriorColor, Transform inParent, out UIPanel maskPanel, out UISprite barOutline)
        {
            UISprite uisprite = base.CreateSprite(this.MyScreenHubBase.ScreenThemeAtlas, "small_button", inPosition, inSize, inInteriorColor, inParent, UIWidget.Pivot.TopLeft);
            UIPanel uipanel = base.CreateClippingPanel("BarMask", Vector3.zero, inSize, uisprite.cachedTransform, UIWidget.Pivot.TopLeft);
            UISprite uisprite2 = base.CreateSprite(this.MyScreenHubBase.ScreenThemeAtlas, "small_button_fill", Vector3.zero, inSize, inInteriorColor, uipanel.cachedTransform, UIWidget.Pivot.Center);
            uisprite2.type = UIBasicSprite.Type.Sliced;
            uisprite2.depth = uisprite.depth + 1;
            uisprite.depth = uisprite.depth;
            uisprite.name = "Bar_Horizontal";
            maskPanel = uipanel;
            barOutline = uisprite;
            maskPanel.depth = PLUIScreen.internalDepthCounterPanels;
            PLUIScreen.internalDepthCounterPanels++;
            uipanel.cachedTransform.localPosition = new Vector3(inSize.x * 0.5f, inSize.y * -0.5f, 0f);
            this.AllStylizedElements.Add(uisprite);
            return uisprite2;
        }
    }
    class ArmorCompletionReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            int shipID = (int)arguments[0];
            PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(shipID);
            if (ship != null && ship is PLShipInfo)
            {
                foreach (PLUIScreen screen in (ship as PLShipInfo).MyScreenBase.AllScreens)
                {
                    if (screen is PLArmorBonusScreen)
                    {
                        PLArmorBonusScreen armor = screen as PLArmorBonusScreen;
                        armor.OnCompletion((int)arguments[1], (bool)arguments[2]);
                        break;
                    }
                }
            }
        }
    }
    class ArmorDataReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                int shipID = (int)arguments[0];
                PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(shipID);
                if (ship != null && ship is PLShipInfo)
                {
                    foreach (PLUIScreen screen in (ship as PLShipInfo).MyScreenBase.AllScreens)
                    {
                        if (screen is PLArmorBonusScreen)
                        {
                            PLArmorBonusScreen armor = screen as PLArmorBonusScreen;
                            armor.VictoryTimer = (float)arguments[1];
                            armor.CooldownTimer = (float)arguments[2];
                            ShipStats.armorModifier = (float)arguments[3];
                            break;
                        }
                    }
                }
            }
        }
    }
    class TemperatureReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            int shipID = (int)arguments[0];
            PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(shipID);
            if (ship != null && ship is PLShipInfo)
            {
                ship.MyTLI.AtmoSettings.Temperature = (float)arguments[1];
            }
        }
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
                    SendRPC("pokegustavo.theflagship", "The_Flagship.UpgradePatrolReciever", PhotonTargets.Others, new object[] { 0 });
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