using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
}