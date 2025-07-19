using System.Collections.Generic;
using PulsarModLoader;
using PulsarModLoader.Chat.Commands.CommandRouter;
using UnityEngine;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Pathfinding;
using PulsarModLoader.MPModChecks;
using PulsarModLoader.SaveData;
using System.IO;
using static UIPopupList;
using System.Reflection.Emit;
using static The_Flagship.Assembler;

namespace The_Flagship
{
    /*
    TODO
     */
    public class Mod : PulsarMod
    {
        public static bool AutoAssemble = false;
        public static ParticleSystem reactorEffect = null;
        public static List<GameObject> moddedScreens = new List<GameObject>();
        public static List<Camera> cameras = new List<Camera>();
        public static int PatrolBotsLevel = 0;
        public static int FighterCount = 10;
        public static uint BridgePathID = 0;
        public override string Version => "1.9.1";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Adds the flagship as a playable ship.";

        public override string Name => "The Flagship";

        public override int MPRequirements => (int)MPRequirement.All;

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.theflagship";
        }
    }
    class MySaveData : PMLSaveData
    {

        public override string Identifier()
        {
            return "pokegustavo.theflagship";
        }

        public override void LoadData(byte[] Data, uint VersionID)
        {
            OnExit.Postfix();
            using (MemoryStream dataStream = new MemoryStream(Data))
            {
                using (BinaryReader binaryReader = new BinaryReader(dataStream))
                {
                    bool assembled = binaryReader.ReadBoolean();
                    if (assembled) OnJoin.AutoAssemble();
                    Mod.PatrolBotsLevel = binaryReader.ReadInt32();
                    Mod.FighterCount = binaryReader.ReadInt32();
                }
            }
        }

        public override byte[] SaveData()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(stream))
                {
                    binaryWriter.Write(Command.shipAssembled);
                    binaryWriter.Write(Mod.PatrolBotsLevel);
                    binaryWriter.Write(Mod.FighterCount);
                }
                return stream.ToArray();
            }
        }
    }
    public class Command : ChatCommand
    {
        public static bool cameraenabled = true;
        public static bool shipAssembled = false;
        public static bool realtimecams = false;
        public static int[] playersArrested = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public static GameObject[] prisionCells = new GameObject[10];
        public override string[] CommandAliases()
        {
            return new string[]
            {
                "flagship"
            };
        }
        public override string[][] Arguments()
        {
            return new string[][] { new string[] { "assemble", "autoassemble", "prison", "realtimecam", "cameras" } };
        }
        public override string Description()
        {
            return "Assembles the flagship and allows for prison control";
        }

        public override void Execute(string arguments)
        {
            string[] separatedArguments = arguments.Split(' ');
            if (arguments == Arguments()[0][3])
            {
                realtimecams = !realtimecams;
                if (realtimecams)
                {
                    PulsarModLoader.Utilities.Messaging.Notification("This may cause FPS drops on the bridge!");
                }
                foreach (Camera cam in Mod.cameras)
                {
                    cam.enabled = realtimecams;
                }
                return;
            }
            if (arguments == Arguments()[0][4])
            {
                cameraenabled = !cameraenabled;
                PulsarModLoader.Utilities.Messaging.Notification("The camera system has been " + (cameraenabled ? "Enabled" : "Disabled"));
                return;
            }
            if (PLEncounterManager.Instance.PlayerShip != null)
            {
                if (arguments == Arguments()[0][0])
                {
                    if (!PhotonNetwork.isMasterClient) PulsarModLoader.Utilities.Messaging.Notification("Only the host can use the commands!");
                    else if (!shipAssembled)
                    {
                        FabricateFlagship();
                        ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.RPCReciever", PhotonTargets.Others, new object[0]);
                    }
                    else
                    {
                        PulsarModLoader.Utilities.Messaging.Notification("Ship already assembled!");
                    }
                }
                if (arguments == Arguments()[0][1])
                {
                    Mod.AutoAssemble = !Mod.AutoAssemble;
                    PLXMLOptionsIO.Instance.CurrentOptions.SetStringValue("flagship", string.Format("{0}", new object[]
                    {
                        Mod.AutoAssemble
                    }));
                    PulsarModLoader.Utilities.Messaging.Notification("Auto assemble " + (Mod.AutoAssemble ? "Enabled" : "Disabled"));
                }
                if (separatedArguments[0] == Arguments()[0][2])
                {
                    if (!shipAssembled)
                    {
                        PulsarModLoader.Utilities.Messaging.Notification("Ship needs to be assembled!");
                    }
                    if (PLNetworkManager.Instance.LocalPlayer != null && PLNetworkManager.Instance.LocalPlayer.GetClassID() != 0) 
                    {
                        PulsarModLoader.Utilities.Messaging.Notification("Only the captain can use this command");
                    }
                    else if (separatedArguments.Length > 1)
                    {
                        PLPlayer targetPlayer = PulsarModLoader.Utilities.HelperMethods.GetPlayerFromPlayerName(separatedArguments[1]);
                        int targetPlayerID;
                        if (targetPlayer == null && int.TryParse(separatedArguments[1], out targetPlayerID))
                        {
                            targetPlayer = PulsarModLoader.Utilities.HelperMethods.GetPlayerFromPlayerID(targetPlayerID);
                        }
                        if (targetPlayer != null)
                        {
                            if (targetPlayer.TeamID != 0)
                            {
                                PulsarModLoader.Utilities.Messaging.Notification("Player is not part of your crew!");
                                return;
                            }
                            if (!playersArrested.Contains(targetPlayer.GetPlayerID()))
                            {

                                for (int i = 0; i < 10; i++)
                                {
                                    if (playersArrested[i] == -1)
                                    {
                                        playersArrested[i] = targetPlayer.GetPlayerID();
                                        PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
                                        if (ship != null)
                                        {
                                            if (ship.GetCurrentShipControllerPlayerID() == targetPlayer.GetPlayerID())
                                            {
                                                ship.photonView.RPC("NewShipController", PhotonTargets.All, new object[] { -1 });
                                            }
                                            for (int j = 0; j < ship.GetCurrentTurretControllerMaxTurretIndex(); j++)
                                            {
                                                if (ship.GetCurrentTurretControllerPlayerID(j) == targetPlayer.GetPlayerID())
                                                {
                                                    ship.photonView.RPC("NewTurretController", PhotonTargets.All, new object[] { j, -1 });
                                                }
                                            }
                                            if (ship.SensorDishControllerPlayerID == targetPlayer.GetPlayerID())
                                            {
                                                ship.photonView.RPC("RequestSensorDishController", PhotonTargets.All, new object[] { -1 });
                                            }
                                            if (ship.CaptainsChairPlayerID == targetPlayer.GetPlayerID())
                                            {
                                                ship.photonView.RPC("AttemptToSitInCaptainsChair", PhotonTargets.All, new object[] { -1 });
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    if (playersArrested[i] == targetPlayer.GetPlayerID())
                                    {
                                        playersArrested[i] = -1;
                                        break;
                                    }
                                }
                            }
                            if (!PhotonNetwork.isMasterClient) 
                            {
                                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.prisionRPC", PhotonTargets.MasterClient, new object[] { Command.playersArrested });
                            }
                        }
                        else
                        {
                            PulsarModLoader.Utilities.Messaging.Notification("Player not found!");
                        }
                    }
                }
            }
        }

        
    }
    [HarmonyPatch(typeof(PLNetworkManager), "OnLeaveGame")]
    internal class OnExit
    {
        internal static void Postfix()
        {
            foreach (GameObject gameObject in Mod.moddedScreens)
            {
                Object.Destroy(gameObject);
            }
            Command.shipAssembled = false;
            Mod.moddedScreens.Clear();
            Mod.FighterCount = 10;
            Mod.PatrolBotsLevel = 0;
            PLAutoRepairScreen.CurrentMultiplier = 1f;
            Command.playersArrested = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            Command.prisionCells = new GameObject[10];
        }
    }
}
