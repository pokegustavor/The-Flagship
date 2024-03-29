﻿using PulsarModLoader;
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
using Pathfinding;
using System.Threading.Tasks;
using TeamUtility.IO;
using UnityEngine.EventSystems;
using Pathfinding.RVO;

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
        public override void Update()
        {
            base.Update();
            if (MyScreenHubBase == null)
            {
                Destroy(this);
            }
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
        public float duration = 30f;
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
                ShipStats.armorModifier,
                duration
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
                            Play(gameLabels, "easy");
                            break;
                        case 2:
                            Play(gameLabels, "medium");
                            break;
                        case 3:
                            Play(gameLabels, "hard");
                            break;
                    }
                    XTurn = true;
                    AITurn++;
                    CheckGame(gameLabels);
                }
            }
            else
            {
                if (VictoryTimer > 0)
                {
                    VictoryTimer -= Time.deltaTime;
                    description.text = "Time remaining: " + VictoryTimer.ToString("0.00");
                    bar.color = Color.red;
                    barMask.clipOffset = new Vector2(((VictoryTimer / duration) - 1) * 510, 0);

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
                        duration = 300f;
                        break;
                    case 2:
                        ShipStats.armorModifier = 2f;
                        duration = 180f;
                        break;
                    case 3:
                        ShipStats.armorModifier = 3f;
                        duration = 120f;
                        break;
                }
                VictoryTimer = duration;
                CooldownTimer = 240f;
                defeatCount = 0;
            }
            else
            {
                MyScreenHubBase.OptionalShipInfo.MyStats.ReactorTempCurrent += MyScreenHubBase.OptionalShipInfo.MyStats.ReactorTempMax * 0.25f;
                defeatCount++;
            }
        }
        void CheckGame(UILabel[,] board)
        {
            bool gameFinished = false;
            bool playerWon = false;
            if (IsWinner(board, "X"))
            {
                gameFinished = true;
                playerWon = true;
            }
            else if (IsWinner(board, "O") || IsGameOver(board))
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
        int defeatCount = 0;
        // Play method chooses the best move and marks it with "O"
        public void Play(UILabel[,] board, string difficulty)
        {
            if (difficulty == "easy")
            {
                RandomPlay(board);
            }
            else if (difficulty == "medium")
            {
                int[] bestMove = MiniMax(board, 6, true, defeatCount >= 7 ? 0.7f : 0.20f);
                if (bestMove[0] != -1 && bestMove[1] != -1)
                {
                    board[bestMove[0], bestMove[1]].text = "O";
                }
            }
            else if (difficulty == "hard")
            {
                int[] bestMove = MiniMax(board, 6, true, defeatCount >= 7 ? 0.10f : 0.03f);
                if (bestMove[0] != -1 && bestMove[1] != -1)
                {
                    board[bestMove[0], bestMove[1]].text = "O";
                }
            }
        }

        // RandomPlay chooses a random empty cell to mark with "O"
        private void RandomPlay(UILabel[,] board)
        {
            List<int[]> emptyCells = GetEmptyCells(board);

            if (emptyCells.Count > 0)
            {
                int[] randomCell = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
                board[randomCell[0], randomCell[1]].text = "O";
            }
        }

        // Minimax evaluates the value of each move based on its position in the possibility tree
        private int Minimax(UILabel[,] board, int depth, string player)
        {
            if (IsGameOver(board))
            {
                return GetScore(board);
            }

            List<int[]> emptyCells = GetEmptyCells(board);

            if (emptyCells.Count == 0)
            {
                return 0;
            }

            int bestScore;

            if (player == "O")
            {
                bestScore = int.MinValue;

                foreach (int[] cell in emptyCells)
                {
                    board[cell[0], cell[1]].text = "O";
                    int score = Minimax(board, depth - 1, "X");
                    board[cell[0], cell[1]].text = "";

                    bestScore = Mathf.Max(bestScore, score);
                }
            }
            else
            {
                bestScore = int.MaxValue;

                foreach (int[] cell in emptyCells)
                {
                    board[cell[0], cell[1]].text = "X";
                    int score = Minimax(board, depth - 1, "O");
                    board[cell[0], cell[1]].text = "";

                    bestScore = Mathf.Min(bestScore, score);
                }
            }

            return bestScore;
        }

        // IsGameOver checks if the game has ended and who won
        private bool IsGameOver(UILabel[,] board)
        {
            // check rows
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0].text == board[i, 1].text && board[i, 1].text == board[i, 2].text && board[i, 0].text != "")
                {
                    return true;
                }
            }

            // check columns
            for (int j = 0; j < 3; j++)
            {
                if (board[0, j].text == board[1, j].text && board[1, j].text == board[2, j].text && board[0, j].text != "")
                {
                    return true;
                }
            }

            // check diagonals
            if (board[0, 0].text == board[1, 1].text && board[1, 1].text == board[2, 2].text && board[0, 0].text != "")
            {
                return true;
            }

            if (board[0, 2].text == board[1, 1].text && board[1, 1].text == board[2, 0].text && board[0, 2].text != "")
            {
                return true;
            }

            // check for tie
            List<int[]> emptyCells = GetEmptyCells(board);

            if (emptyCells.Count == 0)
            {
                return true;
            }

            return false;
        }

        // GetEmptyCells returns a list of empty cells on the board
        private List<int[]> GetEmptyCells(UILabel[,] board)
        {
            List<int[]> emptyCells = new List<int[]>();

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].text == "")
                    {
                        emptyCells.Add(new int[] { i, j });
                    }
                }
            }

            return emptyCells;
        }

        // GetScore returns the score for the current state of the board
        private int GetScore(UILabel[,] board)
        {
            if (IsWinner(board, "O"))
            {
                return 1;
            }
            else if (IsWinner(board, "X"))
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        // IsWinner checks if a player has won the game
        private bool IsWinner(UILabel[,] board, string player)
        {
            // check rows
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0].text == player && board[i, 1].text == player && board[i, 2].text == player)
                {
                    return true;
                }
            }

            // check columns
            for (int j = 0; j < 3; j++)
            {
                if (board[0, j].text == player && board[1, j].text == player && board[2, j].text == player)
                {
                    return true;
                }
            }

            // check diagonals
            if (board[0, 0].text == player && board[1, 1].text == player && board[2, 2].text == player)
            {
                return true;
            }

            if (board[0, 2].text == player && board[1, 1].text == player && board[2, 0].text == player)
            {
                return true;
            }

            return false;
        }

        // método MiniMax
        private int[] MiniMax(UILabel[,] board, int depth, bool maximizingPlayer, float fluke)
        {
            // verifica se o jogo acabou ou se atingiu a profundidade máxima
            if (IsGameOver(board) || depth == 0)
            {
                int score = GetScore(board);
                return new int[] { -1, -1, score };
            }

            int bestScore = maximizingPlayer ? int.MinValue : int.MaxValue;
            List<int[]> bestMoves = new List<int[]>();
            List<int[]> secondBestMoves = new List<int[]>();
            // para cada célula vazia, simula o movimento e chama o método MiniMax recursivamente
            foreach (int[] emptyCell in GetEmptyCells(board))
            {
                board[emptyCell[0], emptyCell[1]].text = maximizingPlayer ? "O" : "X";
                int[] currentMove = MiniMax(board, depth - 1, !maximizingPlayer, fluke);
                board[emptyCell[0], emptyCell[1]].text = "";
                currentMove[0] = emptyCell[0];
                currentMove[1] = emptyCell[1];
                // verifica se o valor do movimento atual é melhor que o melhor valor até agora
                if (maximizingPlayer && currentMove[2] > bestScore)
                {
                    secondBestMoves = bestMoves.ToList();
                    bestMoves.Clear();
                    bestMoves.Add(currentMove);
                    bestScore = currentMove[2];
                }
                else if (!maximizingPlayer && currentMove[2] < bestScore)
                {
                    secondBestMoves = bestMoves.ToList();
                    bestMoves.Clear();
                    bestMoves.Add(currentMove);
                    bestScore = currentMove[2];
                }
                else if (currentMove[2] == bestScore)
                {
                    bestMoves.Add(currentMove);
                }
            }
            //PulsarModLoader.Utilities.Messaging.Notification("Total: " + bestMoves.Count);
            int[] result = bestMoves[UnityEngine.Random.Range(0, bestMoves.Count - 1)];
            if (UnityEngine.Random.value < fluke && secondBestMoves.Count > 0 && maximizingPlayer) result = secondBestMoves[UnityEngine.Random.Range(0, secondBestMoves.Count - 1)];
            return result;
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
    public class PLModdedSpecialScreen : MonoBehaviour
    {
        public void setValues(Transform root, Canvas canvas, PLShipInfo ship)
        {
            UIWorldRoot = root;
            worldUiCanvas = canvas;
            myShip = ship;

        }
        virtual protected void Update()
        {
            if (myShip == null)
            {
                Destroy(this); return;
            }
            if (PLServer.Instance.IsReflection_FlipIsActiveLocal != Reflected && Assembled)
            {
                Reflected = !Reflected;
                Vector3 Scale = UIRoot.transform.localScale;
                Scale.Scale(new Vector3(-1, 1, 1));
                UIRoot.transform.localScale = Scale;
            }
        }
        public virtual void Assemble() { }

        protected PLShipInfo myShip;
        protected Transform UIWorldRoot;
        protected GameObject UIRoot;
        protected Canvas worldUiCanvas;
        protected bool Assembled = false;
        bool Reflected = false;
    }
    public class PLPatrolBotUpgradeScreen : PLModdedSpecialScreen
    {
        public override void Assemble()
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
        protected override void Update()
        {
            base.Update();
            if (Assembled)
            {
                CurrentScrap.text = PLServer.Instance.CurrentUpgradeMats.ToString();
                Descriptions[1].text = $"Level {Mod.PatrolBotsLevel + 1}\r\n{155 + 25 * Mod.PatrolBotsLevel}\r\n\r\n{30 + 5 * Mod.PatrolBotsLevel}\r\n\r\n{1f + 0.2f * Mod.PatrolBotsLevel}";
                if (Mod.PatrolBotsLevel < 9) Descriptions[2].text = $"Level {Mod.PatrolBotsLevel + 2}\r\n{155 + 25 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{30 + 5 * (Mod.PatrolBotsLevel + 1)}\r\n\r\n{1f + 0.2f * (Mod.PatrolBotsLevel + 1)}";
                else Descriptions[2].text = string.Empty;
                NextUpgradePrice = Mathf.FloorToInt(10 + 10 * 0.1f * Mod.PatrolBotsLevel);
                CostLabel.text = NextUpgradePrice.ToString();
            }
        }

        int NextUpgradePrice = 0;
        Text CostLabel;
        Text CurrentScrap;
        float LastUpgradeAttempt = Time.time;
        Text[] Descriptions = new Text[3];
        RawImage IconImage;
    }
    public class PLAutoRepairScreen : PLModdedSpecialScreen
    {
        public override void Assemble()
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
        override protected void Update()
        {
            base.Update();
            if (Assembled)
            {
                ButtonLabel.text = (Online ? "Deactivate" : "Activate") + " Auto Repair";
                CurrentMultiplier = Mathf.Clamp(CurrentMultiplier + Time.deltaTime * (Online ? 1 : -1), 0.1f, 70);
                float valueMultier = Mathf.Log(15 * CurrentMultiplier, 2);
                powerusage = 4000 * valueMultier;
                float repairvalue = 2.5f * valueMultier;
                PowerUsageLabel.text = $"Current Power Usage: {Mathf.FloorToInt(powerusage)}MW\r\n\r\nCurrent Repair Power: {repairvalue.ToString("0")}/s";
                if (Online && myShip.MyHull != null && !myShip.IsReactorOverheated() && myShip.StartupStepIndex >= 1 && myShip.StartupSwitchBoard != null && myShip.StartupSwitchBoard.GetLateStatus(0))
                {
                    if (myShip.MyHull != null)
                    {
                        myShip.MyHull.Current += repairvalue * Time.deltaTime;
                        myShip.MyHull.Current = Mathf.Clamp(myShip.MyHull.Current, 0, myShip.MyStats.HullMax);
                    }

                }
                else
                {
                    powerusage = 0f;
                }
                if (PhotonNetwork.isMasterClient && Time.time - lastSync > 5)
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
        public static float CurrentMultiplier = 1;
        Text PowerUsageLabel;
        RawImage IconImage;
    }
    public class PLFighterScreen : PLModdedSpecialScreen
    {
        public override void Assemble()
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
            IconImage.texture = (Texture2D)Resources.Load("Icons/78_Thrusters");
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
            UpgradeName.text = "Select your fighter";
            UpgradeName.color = Color.white;
            GameObject gameObject5 = new GameObject("Title", new Type[]
            {
            typeof(Text)
            });
            gameObject5.transform.SetParent(gameObject.transform);
            gameObject5.transform.localPosition = new Vector3(-20f, 175f, 0f);
            gameObject5.transform.localRotation = Quaternion.identity;
            gameObject5.transform.localScale = Vector3.one * 0.25f;
            gameObject5.GetComponent<RectTransform>().anchoredPosition3D = gameObject5.transform.localPosition;
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1450f);
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text ScreenTitle = gameObject5.GetComponent<Text>();
            ScreenTitle.font = PLGlobal.Instance.MainFont;
            ScreenTitle.resizeTextMaxSize = 70;
            ScreenTitle.resizeTextMinSize = 10;
            ScreenTitle.resizeTextForBestFit = true;
            ScreenTitle.alignment = TextAnchor.UpperLeft;
            ScreenTitle.raycastTarget = false;
            ScreenTitle.text = PLLocalize.Localize("FIGHTERS CONTROL", false);
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
            Descriptions[0].text = "\r\nLight Fighter\r\n\r\n\r\nHeavy Fighter\r\n\r\n\r\nSupport Fighter";
            Descriptions[0].color = Color.gray;
            Descriptions[0].transform.localPosition -= new Vector3(0, 700, 0);
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
            component.text = PLLocalize.Localize("CRAFT FIGHTER", false);
            component.font = PLGlobal.Instance.MainFont;
            component.GetComponent<RectTransform>().anchoredPosition3D = component.transform.localPosition;
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);

            for (int i = 0; i < 3; i++)
            {
                int valueofI = i;
                GameObject fighterButton = new GameObject("EXTRACT_BTN", new Type[]
                {
                typeof(Image),
                typeof(Button)
                });
                Button fighterButtonBtn = fighterButton.GetComponent<Button>();
                Image fighterButtonImage = fighterButton.GetComponent<Image>();
                fighterButtonImage.sprite = PLGlobal.Instance.TabFillSprite;
                fighterButtonImage.type = Image.Type.Sliced;
                fighterButtonImage.transform.SetParent(gameObject.transform);
                fighterButtonBtn.transform.localPosition = new Vector3(70f, -25f - (60 * i), 0f);
                fighterButtonBtn.transform.localRotation = Quaternion.identity;
                fighterButtonBtn.transform.localScale = Vector3.one;
                fighterButtonBtn.gameObject.layer = 3;
                fighterButtonBtn.GetComponent<RectTransform>().anchoredPosition3D = fighterButtonBtn.transform.localPosition;
                fighterButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 250f);
                fighterButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                ColorBlock colors1 = fighterButtonBtn.colors;
                colors1.normalColor = Color.gray;
                fighterButtonBtn.colors = colors1;
                fighterButtonBtn.onClick.AddListener(delegate ()
                {
                    SpawnFighter(valueofI);
                });
                GameObject fighterButton10 = new GameObject("ExtractBtnLabel", new Type[]
                {
                typeof(Text)
                });
                fighterButton10.transform.SetParent(fighterButton.transform);
                fighterButton10.transform.localPosition = new Vector3(0f, 0f, 0f);
                fighterButton10.transform.localRotation = Quaternion.identity;
                fighterButton10.transform.localScale = Vector3.one;
                Text fighterButtoncomponent = fighterButton10.GetComponent<Text>();
                fighterButtoncomponent.alignment = TextAnchor.MiddleCenter;
                fighterButtoncomponent.resizeTextForBestFit = true;
                fighterButtoncomponent.resizeTextMinSize = 8;
                fighterButtoncomponent.resizeTextMaxSize = 18;
                fighterButtoncomponent.color = Color.black;
                fighterButtoncomponent.raycastTarget = false;
                fighterButtoncomponent.text = PLLocalize.Localize("SPAWN FIGHTER", false);
                fighterButtoncomponent.font = PLGlobal.Instance.MainFont;
                fighterButtoncomponent.GetComponent<RectTransform>().anchoredPosition3D = fighterButtoncomponent.transform.localPosition;
                fighterButtoncomponent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                fighterButtoncomponent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                Descriptions[i] = fighterButtoncomponent;
            }
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
            component2.font = PLGlobal.Instance.MainFont;
            component2.resizeTextMaxSize = 150;
            component2.resizeTextMinSize = 10;
            component2.resizeTextForBestFit = true;
            component2.alignment = TextAnchor.MiddleCenter;
            component2.raycastTarget = false;
            component2.text = "20";
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

            GameObject FighterLabel = new GameObject("FighterLabel", new Type[]
        {
            typeof(Text)
        });
            FighterLabel.transform.SetParent(gameObject.transform);
            FighterLabel.transform.localPosition = new Vector3(60f, 262f, 0f);
            FighterLabel.transform.localRotation = Quaternion.identity;
            FighterLabel.transform.localScale = Vector3.one * 0.25f;
            FighterLabel.GetComponent<RectTransform>().anchoredPosition3D = FighterLabel.transform.localPosition;
            FighterLabel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1300f);
            FighterLabel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            CurrentFighterSupply = FighterLabel.GetComponent<Text>();
            CurrentFighterSupply.font = PLGlobal.Instance.MainFont;
            CurrentFighterSupply.resizeTextMaxSize = 150;
            CurrentFighterSupply.resizeTextMinSize = 5;
            CurrentFighterSupply.resizeTextForBestFit = true;
            CurrentFighterSupply.alignment = TextAnchor.MiddleCenter;
            CurrentFighterSupply.raycastTarget = false;
            CurrentFighterSupply.text = "10/10";
            CurrentFighterSupply.color = Color.white;
            GameObject FightIcon = new GameObject("FighIcon", new Type[]
            {
            typeof(RawImage)
            });
            FightIcon.transform.SetParent(FighterLabel.transform);
            FightIcon.transform.localPosition = new Vector3(-270f, 0f, 0f);
            FightIcon.transform.localRotation = Quaternion.identity;
            FightIcon.transform.localScale = Vector3.one;
            FightIcon.layer = 3;
            RawImage FightImage = FightIcon.GetComponent<RawImage>();
            FightImage.texture = (Texture2D)Resources.Load("Icons/82_Thrusters");
            FightImage.GetComponent<RectTransform>().anchoredPosition3D = FightIcon.transform.localPosition;
            FightImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 128f);
            FightImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 128f);

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
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Can't craft more fighters without Component Upgrader talent", false);
                return;
            }
            if (Mod.FighterCount >= 10)
            {
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Fighter supply already full", false);
                return;
            }
            if (PLServer.Instance.CurrentUpgradeMats < 20)
            {
                PLTabMenu.Instance.TimedErrorMsg = PLLocalize.Localize("Can't afford more fighters. Process more scrap!", false);
                return;
            }
            if (Time.time - LastUpgradeAttempt > 2f)
            {
                LastUpgradeAttempt = Time.time;
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.FighterCraftReciever", PhotonTargets.All, new object[0]);
                PLMusic.PostEvent("play_sx_ui_ship_upgradecomponent", base.gameObject);
            }
        }
        public static void SpawnFighter(int type, int controller = -1, PhotonPlayer sender = null)
        {
            if (PLEncounterManager.Instance.PlayerShip != null && PLEncounterManager.Instance.PlayerShip.InWarp || Time.time - lastClick < 3) return;
            lastClick = Time.time;
            if (!PhotonNetwork.isMasterClient)
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.FighterRequestReciever", PhotonTargets.MasterClient, new object[] { type, (int)PLNetworkManager.Instance.LocalPlayerID });
                return;
            }
            if (controller == -1) controller = PLNetworkManager.Instance.LocalPlayerID;
            //If Data is null create
            if (figtherData[type] == null)
            {
                EShipType droneType = EShipType.E_WDDRONE1;
                if (type == 1)
                {
                    droneType = EShipType.E_WDDRONE2;
                }
                else if (type == 2)
                {
                    droneType = EShipType.E_WDDRONE3;
                }
                PLPersistantShipInfo shipdata = new PLPersistantShipInfo(droneType, -1, PLServer.GetCurrentSector());
                figtherData[type] = shipdata;
            }
            PLPersistantShipInfo Persistantshipdata = figtherData[type];
            Persistantshipdata.IsShipDestroyed = false;
            Persistantshipdata.HullPercent = 1f;
            Persistantshipdata.ShldPercent = 1f;
            //If ship doesn't exist create
            if (Persistantshipdata.ShipInstance == null && Mod.FighterCount > 0)
            {
                Persistantshipdata.CompOverrides.Clear();
                List<ComponentOverrideData> overrides = new List<ComponentOverrideData>();
                PLRand shipDeterministicRand = PLShipInfoBase.GetShipDeterministicRand(Persistantshipdata, 300);
                switch (type)
                {
                    case 0:
                        Persistantshipdata.ShipName = "Light Fighter";
                        overrides.Add(new ComponentOverrideData() { CompType = 1, CompSubType = 3, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 1, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 3, CompSubType = 1, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 3, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 6, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 6, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 3, ReplaceExistingComp = true, CompLevel = 1 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 3, ReplaceExistingComp = true, CompLevel = 1 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 1, ReplaceExistingComp = true, CompLevel = 3 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 12, ReplaceExistingComp = true, CompLevel = 3 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 25, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 25, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 26, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 26, SlotNumberToReplace = 0 });
                        Persistantshipdata.CompOverrides.AddRange(overrides);
                        break;
                    case 1:
                        Persistantshipdata.ShipName = "Heavy Fighter";
                        overrides.Add(new ComponentOverrideData() { CompType = 1, CompSubType = 5, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 1, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 3, CompSubType = 10, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 3, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 6, CompSubType = 2, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 6, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 2, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 2, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 4, ReplaceExistingComp = true, CompLevel = 3 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 6, ReplaceExistingComp = true, CompLevel = 3 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 11, CompSubType = 1, ReplaceExistingComp = true, CompLevel = 0 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 11, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 25, CompSubType = 4, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 25, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 26, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 26, SlotNumberToReplace = 0 });
                        Persistantshipdata.CompOverrides.AddRange(overrides);
                        break;
                    case 2:
                        Persistantshipdata.ShipName = "Support Fighter";
                        overrides.Add(new ComponentOverrideData() { CompType = 1, CompSubType = 4, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 1, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 3, CompSubType = 6, ReplaceExistingComp = true, CompLevel = 4 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 3, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 6, CompSubType = 1, ReplaceExistingComp = true, CompLevel = 2 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 6, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 1 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 9, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 1 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 9, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 9, ReplaceExistingComp = true, CompLevel = 0 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 10, CompSubType = 13, ReplaceExistingComp = true, CompLevel = 1 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 10, SlotNumberToReplace = 1 });
                        overrides.Add(new ComponentOverrideData() { CompType = 25, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 0 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 25, SlotNumberToReplace = 0 });
                        overrides.Add(new ComponentOverrideData() { CompType = 26, CompSubType = 0, ReplaceExistingComp = true, CompLevel = 0 - PLShipInfoBase.GetChaosBoost(Persistantshipdata, shipDeterministicRand.Next() % 50), IsCargo = false, CompTypeToReplace = 26, SlotNumberToReplace = 0 });
                        Persistantshipdata.CompOverrides.AddRange(overrides);
                        break;
                }
                Persistantshipdata.MyCurrentSector = PLServer.GetCurrentSector();
                PLShipInfoBase shipInstance = PLEncounterManager.Instance.GetCPEI().SpawnEnemyShip(Persistantshipdata.Type, Persistantshipdata, PLEncounterManager.Instance.PlayerShip.Exterior.transform.position + new Vector3(UnityEngine.Random.Range(100, 300), UnityEngine.Random.Range(100, 300), UnityEngine.Random.Range(100, 300)), PLEncounterManager.Instance.PlayerShip.Exterior.transform.rotation);
                shipInstance.NoRepLossOnKilled = true;
                shipInstance.DropScrap = false;
                shipInstance.name += " (fighterBot)";
                fighterInstances[type] = shipInstance;
                switch (type)
                {
                    case 0:
                        shipInstance.photonView.RPC("Captain_NameShip", PhotonTargets.All, new object[] { "Light Fighter" });
                        break;
                    case 1:
                        shipInstance.photonView.RPC("Captain_NameShip", PhotonTargets.All, new object[] { "Heavy Fighter" });
                        break;
                    case 2:
                        shipInstance.photonView.RPC("Captain_NameShip", PhotonTargets.All, new object[] { "Support Fighter" });
                        break;
                }
                Mod.FighterCount--;
                if (shipInstance.PilotingSystem == null)
                {
                    shipInstance.PilotingSystem = shipInstance.gameObject.AddComponent<PLPilotingSystem>();
                    shipInstance.PilotingSystem.MyShipInfo = shipInstance;
                }
                if (shipInstance.PilotingHUD == null)
                {
                    shipInstance.PilotingHUD = shipInstance.gameObject.AddComponent<PLPilotingHUD>();
                    shipInstance.PilotingHUD.MyShipInfo = shipInstance;
                }
                shipInstance.OrbitCameraMaxDistance = 40;
                shipInstance.OrbitCameraMinDistance = 10;
                //sends warning about building a new ship to the captain
                PLPlayer cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(0);
                if (cachedFriendlyPlayerOfClass != null && cachedFriendlyPlayerOfClass.GetPhotonPlayer() != sender)
                {
                    PLPlayer failuer = FlagshipHelperMethods.GetPlayerFromPhotonPlayer(sender);
                    if (failuer != null)
                    {
                        PLServer.Instance.photonView.RPC("AddNotificationLocalize", cachedFriendlyPlayerOfClass.GetPhotonPlayer(), new object[]
                            {
                                "[PL] has built a new fighter",
                                failuer.GetPlayerID(),
                                PLServer.Instance.GetEstimatedServerMs() + 6000,
                                true
                            });
                    }
                }

                if (sender != null)
                {
                    ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.PilotFighter", sender, new object[]
                    {
                        shipInstance.ShipID
                    });
                    return;
                }
                shipInstance.photonView.RPC("NewShipController", PhotonTargets.All, new object[]
                    {
                        controller
                    });

            }
            //Gives control if ship exists
            else if (Persistantshipdata.ShipInstance != null && Persistantshipdata.ShipInstance.GetCurrentShipControllerPlayerID() == -1)
            {
                if (sender != null)
                {
                    ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.PilotFighter", sender, new object[]
                    {
                        Persistantshipdata.ShipInstance.ShipID,
                    });
                    return;
                }
                Persistantshipdata.ShipInstance.photonView.RPC("NewShipController", PhotonTargets.All, new object[]
                    {
                        controller
                    });
            }
            //Sends warning if ship exists but is already been piloted
            else if (Persistantshipdata.ShipInstance != null && Persistantshipdata.ShipInstance.GetCurrentShipControllerPlayerID() != -1 && sender != null)
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.SendWarning", sender, new object[] { "Fighter already been piloted" });
            }
            //Sends warning if there is not enough supplies to build another figther
            else if (Mod.FighterCount <= 0 && sender != null)
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.SendWarning", sender, new object[] { "No Fighter supply remmaning. Craft some more!" });
            }
        }
        override protected void Update()
        {
            base.Update();
            if (Assembled)
            {
                CurrentScrap.text = PLServer.Instance.CurrentUpgradeMats.ToString();
                CurrentFighterSupply.text = Mod.FighterCount + "/10";
                for (int i = 0; i < 3; i++)
                {
                    if (fighterInstances[i] != null && fighterInstances[i].GetCurrentShipControllerPlayerID() == -1)
                    {
                        Descriptions[i].text = "Pilot Fighter";
                    }
                    else if (fighterInstances[i] != null && fighterInstances[i].GetCurrentShipControllerPlayerID() != -1)
                    {
                        Descriptions[i].text = "Fighter Under Use";
                    }
                    else
                    {
                        Descriptions[i].text = "Construct Fighter";
                    }
                    /*
                    if (fighterInstances[i] != null) 
                    {
                        if (fighterInstances[i].PilotingSystem == null)
                        {
                            fighterInstances[i].PilotingSystem = fighterInstances[i].gameObject.AddComponent<PLPilotingSystem>();
                            fighterInstances[i].PilotingSystem.MyShipInfo = fighterInstances[i];
                        }
                        if (fighterInstances[i].PilotingHUD == null)
                        {
                            fighterInstances[i].PilotingHUD = fighterInstances[i].gameObject.AddComponent<PLPilotingHUD>();
                            fighterInstances[i].PilotingHUD.MyShipInfo = fighterInstances[i];
                        }
                        fighterInstances[i].OrbitCameraMaxDistance = 40;
                        fighterInstances[i].OrbitCameraMinDistance = 10;
                    }
                    */
                }
                UIRoot.SetActive(PLCameraSystem.Instance.CurrentCameraMode.GetType() != typeof(PLCameraMode_Pilot));
                if (PhotonNetwork.isMasterClient && Time.time - lastSync > 2)
                {
                    ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.FighterSyncReciever", PhotonTargets.Others, new object[]
                    {
                        Mod.FighterCount,
                        (fighterInstances[0] != null) ? fighterInstances[0].ShipID : -1,
                        (fighterInstances[1] != null) ? fighterInstances[1].ShipID : -1,
                        (fighterInstances[2] != null) ? fighterInstances[2].ShipID : -1,
                    });
                    lastSync = Time.time;
                }
                for (int i = 0; i < 10; i++)
                {
                    if (fighterCargo[i] != null) fighterCargo[i].transform.GetChild(0).gameObject.SetActive(i <= Mod.FighterCount - 1);
                }
            }
        }
        float lastSync = Time.time;
        static float lastClick = Time.time;
        Text ButtonLabel;
        Text PowerUsageLabel;
        RawImage IconImage;
        int NextUpgradePrice = 0;
        Text CostLabel;
        Text CurrentScrap;
        float LastUpgradeAttempt = Time.time;
        Text[] Descriptions = new Text[3];
        Text CurrentFighterSupply;
        public static PLPersistantShipInfo[] figtherData = new PLPersistantShipInfo[3];
        public static PLShipInfoBase[] fighterInstances = new PLShipInfoBase[3];
        public static GameObject[] fighterCargo = new GameObject[10];
    }
    public class PLCyberAttackScreen : PLModdedSpecialScreen
    {
        Text Title;
        GameObject selecttargettext;
        Text[] Descriptions = new Text[5];
        GameObject[] Buttons = new GameObject[5];
        Tile[,] gameBoard = new Tile[8, 9];
        List<PLShipInfo> targetsList = new List<PLShipInfo>();
        static bool ingame = false;
        bool syncendInGame = true;
        static int mines = 0;
        static int flags = 0;
        public static bool failed = false;
        float lastListUpdate = Time.time;
        PLShipInfo targetship;
        public static List<PLShipInfo> shutdownList = new List<PLShipInfo>();
        public override void Assemble()
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
            GameObject gameObject4 = new GameObject("DroneText", new Type[]
            {
            typeof(Text)
            });
            gameObject4.transform.SetParent(gameObject.transform);
            gameObject4.transform.localPosition = new Vector3(0f, 55f, 0f);
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
            UpgradeName.text = "Select your target:\nShift + LeftClick to flag/unflag";
            UpgradeName.color = Color.white;
            selecttargettext = gameObject4;
            GameObject gameObject5 = new GameObject("Title", new Type[]
            {
            typeof(Text)
            });
            gameObject5.transform.SetParent(gameObject.transform);
            gameObject5.transform.localPosition = new Vector3(-20f, 175f, 0f);
            gameObject5.transform.localRotation = Quaternion.identity;
            gameObject5.transform.localScale = Vector3.one * 0.25f;
            gameObject5.GetComponent<RectTransform>().anchoredPosition3D = gameObject5.transform.localPosition;
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1450f);
            gameObject5.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800f);
            Text ScreenTitle = gameObject5.GetComponent<Text>();
            ScreenTitle.font = PLGlobal.Instance.MainFont;
            ScreenTitle.resizeTextMaxSize = 70;
            ScreenTitle.resizeTextMinSize = 10;
            ScreenTitle.resizeTextForBestFit = true;
            ScreenTitle.alignment = TextAnchor.UpperLeft;
            ScreenTitle.raycastTarget = false;
            ScreenTitle.text = PLLocalize.Localize("VIRTUAL CYBERATTACK", false);
            ScreenTitle.color = Color.white;
            Title = ScreenTitle;
            for (int i = 0; i < 5; i++)
            {
                int valueofI = i;
                GameObject MainButton = new GameObject("EXTRACT_BTN", new Type[]
                {
                typeof(Image),
                typeof(Button)
                });
                Button MainButtonBtn = MainButton.GetComponent<Button>();
                Image MainButtonImage = MainButton.GetComponent<Image>();
                MainButtonImage.sprite = PLGlobal.Instance.TabFillSprite;
                MainButtonImage.type = Image.Type.Sliced;
                MainButtonImage.transform.SetParent(gameObject.transform);
                MainButtonBtn.transform.localPosition = new Vector3(0f, -25f - (60 * i), 0f);
                MainButtonBtn.transform.localRotation = Quaternion.identity;
                MainButtonBtn.transform.localScale = new Vector3(1.8f, 1, 1);
                MainButtonBtn.gameObject.layer = 3;
                MainButtonBtn.GetComponent<RectTransform>().anchoredPosition3D = MainButtonBtn.transform.localPosition;
                MainButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 250f);
                MainButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                ColorBlock colors1 = MainButtonBtn.colors;
                colors1.normalColor = Color.gray;
                MainButtonBtn.colors = colors1;
                MainButtonBtn.onClick.AddListener(delegate ()
                {
                    StartMine(valueofI);
                });
                GameObject ButtonTextobj = new GameObject("ExtractBtnLabel", new Type[]
                {
                typeof(Text)
                });
                ButtonTextobj.transform.SetParent(MainButton.transform);
                ButtonTextobj.transform.localPosition = new Vector3(0f, 0f, 0f);
                ButtonTextobj.transform.localRotation = Quaternion.identity;
                ButtonTextobj.transform.localScale = new Vector3(0.55f, 1, 1);
                Text ButtonText = ButtonTextobj.GetComponent<Text>();
                ButtonText.alignment = TextAnchor.MiddleCenter;
                ButtonText.resizeTextForBestFit = true;
                ButtonText.resizeTextMinSize = 8;
                ButtonText.resizeTextMaxSize = 18;
                ButtonText.color = Color.black;
                ButtonText.raycastTarget = false;
                ButtonText.text = PLLocalize.Localize("U.S.S Money <color=red>HARD</color>", false);
                ButtonText.font = PLGlobal.Instance.MainFont;
                ButtonText.GetComponent<RectTransform>().anchoredPosition3D = ButtonText.transform.localPosition;
                ButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                ButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                Descriptions[i] = ButtonText;
                Buttons[i] = MainButton;
            }
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int valueofI = i;
                    int valueofJ = j;
                    GameObject MainButton = new GameObject("EXTRACT_BTN", new Type[]
                    {
                typeof(Image),
                typeof(Button)
                    });
                    Button MainButtonBtn = MainButton.GetComponent<Button>();
                    Image MainButtonImage = MainButton.GetComponent<Image>();
                    MainButtonImage.sprite = PLGlobal.Instance.TabFillSprite;
                    MainButtonImage.type = Image.Type.Sliced;
                    MainButtonImage.transform.SetParent(gameObject.transform);
                    MainButtonBtn.transform.localPosition = new Vector3(-185f + (50 * i), 215 - (60 * j), 0f);
                    MainButtonBtn.transform.localRotation = Quaternion.identity;
                    MainButtonBtn.transform.localScale = new Vector3(0.15f, 0.9f, 1);
                    MainButtonBtn.gameObject.layer = 3;
                    MainButtonBtn.GetComponent<RectTransform>().anchoredPosition3D = MainButtonBtn.transform.localPosition;
                    MainButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 250f);
                    MainButtonBtn.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                    ColorBlock colors1 = MainButtonBtn.colors;
                    colors1.normalColor = Color.gray;
                    MainButtonBtn.colors = colors1;
                    GameObject ButtonTextobj = new GameObject("ExtractBtnLabel", new Type[]
                    {
                typeof(Text)
                    });
                    ButtonTextobj.transform.SetParent(MainButton.transform);
                    ButtonTextobj.transform.localPosition = new Vector3(0f, 0f, 0f);
                    ButtonTextobj.transform.localRotation = Quaternion.identity;
                    ButtonTextobj.transform.localScale = new Vector3(6f, 1.2f, 1);
                    Text ButtonText = ButtonTextobj.GetComponent<Text>();
                    ButtonText.alignment = TextAnchor.MiddleCenter;
                    ButtonText.resizeTextForBestFit = true;
                    ButtonText.resizeTextMinSize = 8;
                    ButtonText.resizeTextMaxSize = 18;
                    ButtonText.color = Color.black;
                    ButtonText.raycastTarget = false;
                    ButtonText.text = PLLocalize.Localize("<color=#36154D>1</color>", false);
                    ButtonText.font = PLGlobal.Instance.MainFont;
                    ButtonText.GetComponent<RectTransform>().anchoredPosition3D = ButtonText.transform.localPosition;
                    ButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
                    ButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
                    Tile tile = new Tile(MainButton, ButtonText, i, j);
                    gameBoard[i, j] = tile;
                    MainButtonBtn.onClick.AddListener(delegate ()
                    {
                        PressedTile(valueofI, valueofJ);
                    });
                }
            }
            Assembled = true;
        }
        void StartMine(int target)
        {
            //Resets the board
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    gameBoard[i, j].Reset();
                }
            }
            flags = 0;
            mines = 0;
            if (target >= targetsList.Count()) return;
            PLShipInfo ship = targetsList[target];
            if (ship != null)
            {
                int difficulty = 5;
                if (ship.MyStats.CyberDefenseRating >= 2)
                {
                    difficulty = 13;
                }
                else if (ship.MyStats.CyberDefenseRating >= 1f)
                {
                    difficulty = 7;
                }
                mines = difficulty;
                for (int i = 0; i < difficulty; i++)
                {
                    int x;
                    int y;
                    do
                    {
                        x = UnityEngine.Random.Range(0, 8);
                        y = UnityEngine.Random.Range(0, 9);
                    } while (gameBoard[x, y].hasMine);
                    gameBoard[x, y].hasMine = true;
                }
                //updates the value for all mines
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        gameBoard[i, j].GenerateValue(gameBoard);
                    }
                }
                ingame = true;
                targetship = ship;
                Title.text = "VIRTUAL CYBERATTACK      " + (mines - flags) + " Mines";
            }
        }
        void PressedTile(int x, int y)
        {
            if (ingame && !failed)
            {
                if (!gameBoard[x, y].revealed)
                {
                    gameBoard[x, y].Pressed(gameBoard, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                    if (!failed && GameIsComplete())
                    {
                        ingame = false;
                        if (targetship != null)
                        {
                            ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.CyberAttackReciever", PhotonTargets.MasterClient, new object[] { true, targetship.ShipID, PLNetworkManager.Instance.LocalPlayer.GetPlayerID() });
                        }
                    }
                }
                Title.text = "VIRTUAL CYBERATTACK      " + (mines - flags) + " Mines";
            }
        }
        protected override void Update()
        {
            base.Update();
            if (Assembled && PLEncounterManager.Instance.PlayerShip != null)
            {
                if (syncendInGame != ingame)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 9; j++)
                        {
                            gameBoard[i, j].button.SetActive(ingame);
                        }
                    }
                    selecttargettext.SetActive(!ingame);
                    for (int i = 0; i < 5; i++)
                    {
                        Buttons[i].SetActive(!ingame);
                    }
                    if (!ingame) Title.text = "VIRTUAL CYBERATTACK";
                    syncendInGame = ingame;
                }
                if (failed)
                {
                    PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
                    if (ship != null)
                    {
                        if (ship.StartupSwitchBoard != null && !ship.StartupSwitchBoard.GetStatus(0))
                        {
                            failed = false;
                            ingame = false;
                        }
                        else if (PhotonNetwork.isMasterClient)
                        {
                            ship.DischargeAmount += 0.2f * Time.deltaTime;
                        }
                    }
                    else
                    {
                        failed = false;
                    }
                }
                if (PhotonNetwork.isMasterClient)
                {
                    shutdownList.RemoveAll((PLShipInfo ship) => ship == null || ship.StartupSwitchBoard == null || !ship.StartupSwitchBoard.GetStatus(0));
                    foreach (PLShipInfo ship in shutdownList)
                    {
                        ship.DischargeAmount += 0.2f * Time.deltaTime;
                        ship.AddHostileShip(PLEncounterManager.Instance.PlayerShip);
                    }
                }
                if (Time.time - lastListUpdate > 1f && !ingame)
                {
                    targetsList.Clear();
                    foreach (PLShipInfoBase ship in PLEncounterManager.Instance.AllShips.Values)
                    {
                        if (targetsList.Count >= 5) break;
                        if (ship != null && !ship.GetIsPlayerShip() && ship is PLShipInfo && ship.MySensorObjectShip.IsDetectedBy(PLEncounterManager.Instance.PlayerShip))
                        {
                            targetsList.Add(ship as PLShipInfo);
                        }
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        if (i + 1 <= targetsList.Count() && targetsList[i] != null)
                        {
                            Buttons[i].SetActive(true);
                            Descriptions[i].text = targetsList[i].ShipName;
                            if (targetsList[i].MyStats.CyberDefenseRating >= 2)
                            {
                                Descriptions[i].text += " <color=red>HARD</color>";
                            }
                            else if (targetsList[i].MyStats.CyberDefenseRating >= 1f)
                            {
                                Descriptions[i].text += " <color=yellow>MEDIUM</color>";
                            }
                            else
                            {
                                Descriptions[i].text += " <color=green>EASY</color>";
                            }
                        }
                        else
                        {
                            Buttons[i].SetActive(false);
                        }
                    }
                    lastListUpdate = Time.time;
                }
            }
        }
        bool GameIsComplete()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (!gameBoard[i, j].revealed && !gameBoard[i, j].hasMine) return false;
                }
            }
            return true;
        }
        class Tile
        {
            private readonly int x, y;
            public bool hasMine = false;
            public bool revealed = false;
            public bool flagged = false;
            int nearMines = 0;
            public GameObject button;
            Text text;

            public Tile(GameObject button, Text text, int x, int y)
            {
                this.button = button;
                this.text = text;
                this.x = x;
                this.y = y;
            }
            public void GenerateValue(Tile[,] board)
            {
                int mineCount = 0;
                try
                {
                    if (!hasMine)
                    {
                        //Count top line mines
                        if (y > 0)
                        {
                            for (int i = x - 1; i <= x + 1; i++)
                            {
                                if (i >= 0 && i < 8 && board[i, y - 1].hasMine)
                                {
                                    mineCount++;
                                }
                            }
                        }
                        //Count side mines
                        for (int i = x - 1; i <= x + 1; i++)
                        {
                            if (i >= 0 && i < 8 && x != i && board[i, y].hasMine)
                            {
                                mineCount++;
                            }
                        }
                        //Count bottom mines
                        if (y < 8)
                        {
                            for (int i = x - 1; i <= x + 1; i++)
                            {
                                if (i >= 0 && i < 8 && board[i, y + 1].hasMine)
                                {
                                    mineCount++;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    PulsarModLoader.Utilities.Logger.Info("ERROR AT COORDINATES: " + x + ", " + y);
                }
                switch (mineCount)
                {
                    case 1:
                        text.text = "<color=#17B2FA>1</color>";
                        break;
                    case 2:
                        text.text = "<color=#66FA17>2</color>";
                        break;
                    case 3:
                        text.text = "<color=#FA1717>3</color>";
                        break;
                    case 4:
                        text.text = "<color=#0F00FF>4</color>";
                        break;
                    case 5:
                        text.text = "<color=#F5D20A>5</color>";
                        break;
                    case 6:
                        text.text = "<color=#8007F9>6</color>";
                        break;
                    case 7:
                        text.text = "<color=#F907C6>7</color>";
                        break;
                    case 8:
                        text.text = "<color=#E8FF00>8</color>";
                        break;
                    default:
                        text.text = string.Empty;
                        break;
                }
                nearMines = mineCount;
                text.enabled = false;
            }
            public void Pressed(Tile[,] board, bool shouldFlag)
            {
                if (!revealed && !flagged && !shouldFlag)
                {
                    revealed = true;
                    text.enabled = true;
                    if (nearMines == 0)
                    {
                        if (hasMine)
                        {
                            Button but = button.gameObject.GetComponent<Button>();
                            ColorBlock colors1 = but.colors;
                            colors1.normalColor = Color.red;
                            colors1.highlightedColor = Color.red;
                            colors1.pressedColor = Color.red;
                            colors1.selectedColor = Color.red;
                            but.colors = colors1;
                        }
                        else
                        {
                            Button but = button.gameObject.GetComponent<Button>();
                            ColorBlock colors1 = but.colors;
                            colors1.normalColor = Color.green;
                            colors1.highlightedColor = Color.green;
                            colors1.pressedColor = Color.green;
                            colors1.selectedColor = Color.green;
                            but.colors = colors1;
                        }
                    }
                    else
                    {
                        Button but = button.gameObject.GetComponent<Button>();
                        ColorBlock colors1 = but.colors;
                        colors1.normalColor = Color.gray;
                        colors1.highlightedColor = Color.white;
                        colors1.pressedColor = Color.white;
                        colors1.selectedColor = Color.gray;
                        but.colors = colors1;
                    }
                    if (hasMine)
                    {
                        failed = true;
                        ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.CyberAttackReciever", PhotonTargets.MasterClient, new object[] { false, -1, PLNetworkManager.Instance.LocalPlayer.GetPlayerID() });
                        return;
                    }
                    if (nearMines == 0)
                    {
                        for (int i = x - 1; i <= x + 1; i++)
                        {
                            for (int j = y - 1; j <= y + 1; j++)
                            {
                                if (i >= 0 && j >= 0 && (i != x || j != y) && i < 8 && j < 9 && !board[i, j].revealed)
                                {
                                    if (board[i, j].flagged)
                                    {
                                        board[i, j].flagged = false;
                                        flags--;
                                    }
                                    board[i, j].Pressed(board, false);
                                }
                            }
                        }
                    }
                }
                else if (!revealed && shouldFlag)
                {
                    if (flagged)
                    {
                        Button but = button.gameObject.GetComponent<Button>();
                        ColorBlock colors1 = but.colors;
                        colors1.normalColor = Color.gray;
                        colors1.highlightedColor = Color.white;
                        colors1.pressedColor = Color.white;
                        colors1.selectedColor = Color.gray;
                        but.colors = colors1;
                        flagged = false;
                        flags--;
                    }
                    else if (mines > flags)
                    {
                        Button but = button.gameObject.GetComponent<Button>();
                        ColorBlock colors1 = but.colors;
                        colors1.normalColor = Color.cyan;
                        colors1.highlightedColor = Color.cyan;
                        colors1.pressedColor = Color.cyan;
                        colors1.selectedColor = Color.cyan;
                        but.colors = colors1;
                        flagged = true;
                        flags++;
                    }
                }
            }
            public void Reset()
            {
                hasMine = false;
                nearMines = 0;
                revealed = false;
                flagged = false;
                Button but = button.gameObject.GetComponent<Button>();
                ColorBlock colors1 = but.colors;
                colors1.normalColor = Color.gray;
                colors1.highlightedColor = Color.white;
                colors1.pressedColor = Color.white;
                colors1.selectedColor = Color.gray;
                but.colors = colors1;
            }
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
                        if (!(bool)arguments[2])
                        {
                            PLPlayer cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(0);
                            if (cachedFriendlyPlayerOfClass != null && cachedFriendlyPlayerOfClass.GetPhotonPlayer() != sender.sender)
                            {
                                PLPlayer failuer = FlagshipHelperMethods.GetPlayerFromPhotonPlayer(sender.sender);
                                if (failuer != null)
                                {
                                    PLServer.Instance.photonView.RPC("AddNotificationLocalize", cachedFriendlyPlayerOfClass.GetPhotonPlayer(), new object[]
                                    {
                                        "[PL] failed the armor boost",
                                        failuer.GetPlayerID(),
                                        PLServer.Instance.GetEstimatedServerMs() + 6000,
                                        true
                                    });
                                }
                            }
                        }
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
                            armor.duration = (float)arguments[4];
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
                    SendRPC("pokegustavo.theflagship", "The_Flagship.UpgradePatrolCurrentReciever", PhotonTargets.Others, new object[] { Mod.PatrolBotsLevel });
                    PLServer.Instance.photonView.RPC("AddCrewWarning_OneString_Localized", PhotonTargets.All, new object[]
                    {
                        "[STR0] UPGRADED!",
                        Color.blue,
                        0,
                        "SHIP",
                        "PATROL BOTS"
                    });
                    PLServer.Instance.CurrentUpgradeMats -= (int)arguments[0];
                    PLPlayer cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(0);
                    if (cachedFriendlyPlayerOfClass != null && cachedFriendlyPlayerOfClass.GetPhotonPlayer() != sender.sender)
                    {
                        PLPlayer failuer = FlagshipHelperMethods.GetPlayerFromPhotonPlayer(sender.sender);
                        if (failuer != null)
                        {
                            PLServer.Instance.photonView.RPC("AddNotificationLocalize", cachedFriendlyPlayerOfClass.GetPhotonPlayer(), new object[]
                            {
                                        "[PL] upgrade the patrol bots",
                                        failuer.GetPlayerID(),
                                        PLServer.Instance.GetEstimatedServerMs() + 6000,
                                        true
                            });
                        }
                    }
                }
            }
        }
    }
    class UpgradePatrolCurrentReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                Mod.PatrolBotsLevel = (int)arguments[0];
            }
        }
    }
    class FighterCraftReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (20 <= PLServer.Instance.CurrentUpgradeMats && Mod.FighterCount < 10)
            {
                Mod.FighterCount = Mathf.Clamp(Mod.FighterCount + 1, 0, 10);
                PLServer.Instance.CurrentUpgradeMats -= 20;
            }
        }
    }
    class FighterRequestReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            PLFighterScreen.SpawnFighter((int)arguments[0], (int)arguments[1], sender.sender);
        }
    }
    class SendWarning : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                PLTabMenu.Instance.TimedErrorMsg = (string)arguments[0];
            }
        }
    }
    class FighterSyncReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                Mod.FighterCount = (int)arguments[0];
                for (int i = 0; i < 3; i++)
                {
                    int ID = (int)arguments[i + 1];
                    if (ID != -1)
                    {
                        PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(ID);
                        if (ship != null)
                        {
                            PLFighterScreen.fighterInstances[i] = ship;
                        }
                    }
                }
            }
        }
    }
    class PilotFighter : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                DelayedControl((int)arguments[0]);
            }
        }
        static async void DelayedControl(int shipID)
        {
            float Timer = Time.time;
            PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(shipID);
            while (ship == null && Time.time - Timer < 15)
            {
                ship = PLEncounterManager.Instance.GetShipFromID(shipID);
                await Task.Yield();
            }
            if (Time.time - Timer >= 15 && ship == null)
            {
                return;
            }
            if (ship.PilotingSystem == null)
            {
                ship.PilotingSystem = ship.gameObject.AddComponent<PLPilotingSystem>();
                ship.PilotingSystem.MyShipInfo = ship;
            }
            if (ship.PilotingHUD == null)
            {
                ship.PilotingHUD = ship.gameObject.AddComponent<PLPilotingHUD>();
                ship.PilotingHUD.MyShipInfo = ship;
            }
            ship.OrbitCameraMaxDistance = 40;
            ship.OrbitCameraMinDistance = 10;
            await Task.Yield();
            ship.photonView.RPC("NewShipController", PhotonTargets.All, new object[]
                {
                        (int)PLNetworkManager.Instance.LocalPlayerID
                });
            if (ship.MyStats.HullCurrent >= ship.MyStats.HullMax && ship.MyStats.ShieldsCurrent >= ship.MyStats.ShieldsMax) FighterAggroToFlagship.PhaseAway(ship);
        }
    }
    class CyberAttackReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            PLPlayer cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(0);
            bool victory = (bool)arguments[0];
            if (victory)
            {
                PLShipInfo ship = PLEncounterManager.Instance.GetShipFromID((int)arguments[1]) as PLShipInfo;
                if (ship != null) PLCyberAttackScreen.shutdownList.Add(ship);
            }
            else
            {
                PLCyberAttackScreen.failed = true;
                if (cachedFriendlyPlayerOfClass.GetPlayerID() != (int)arguments[2])
                {
                    PLPlayer failuer = PLServer.Instance.GetPlayerFromPlayerID((int)arguments[2]);
                    if (failuer != null)
                    {
                        PLServer.Instance.photonView.RPC("AddNotificationLocalize", cachedFriendlyPlayerOfClass.GetPhotonPlayer(), new object[]
                            {
                                "[PL] failed the cyberattack",
                                failuer.GetPlayerID(),
                                PLServer.Instance.GetEstimatedServerMs() + 6000,
                                true
                            });
                    }
                }
            }
        }
    }
}