using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace The_Flagship
{
    public class PLTemperatureScreen : PLCaptainScreen
    {
        UISprite panel;
        UILabel panelLabel;
        UILabel[] controlButtonsLabel;
        UISprite[] controlButtons;
        UILabel currentTemp;
        protected override void Start()
        {
            base.Start();
            ScreenID = 16;
        }
        protected override void SetupUI()
        {
            base.SetupUI();

            panel = CreatePanelEditable("TEMPERATURE CONTROL", Vector3.zero, new Vector2(512f, 256f), UI_White, out panelLabel, null, UIWidget.Pivot.TopLeft);
            controlButtonsLabel = new UILabel[2];
            controlButtons = new UISprite[2];
            controlButtons[0] = CreateButtonEditable("Dec", "-", new Vector3(256f, 60f), new Vector2(5f, 10f), UI_White, out controlButtonsLabel[0], panel.cachedTransform, UIWidget.Pivot.TopLeft);
            controlButtons[1] = CreateButtonEditable("Inc", "+", new Vector3(256f, -60f), new Vector2(5f, 10f), UI_White, out controlButtonsLabel[1], panel.cachedTransform, UIWidget.Pivot.TopRight);
            currentTemp = CreateLabel("25°C", new Vector3(256f, 120f), 20, UI_White, panel.cachedTransform);
        }
        public override void Update()
        {
            base.Update();
            if (MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null)
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
            if (inButton.name == "Dec" && MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null)
            {
                MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature -= 0.05f;
                PlaySoundUI(UIHoverSound);
            }
            else if (inButton.name == "Inc" && MyScreenHubBase != null && MyScreenHubBase.OptionalShipInfo != null)
            {
                MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature += 0.05f;
                PlaySoundUI(UIHoverSound);
            }
            MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature = Mathf.Clamp(MyScreenHubBase.OptionalShipInfo.MyTLI.AtmoSettings.Temperature, -2, 3);
        }
    }
}
