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
        public static int PatrolBotsLevel = 0;
        public static int FighterCount = 10;
        public static uint BridgePathID = 0;
        public override string Version => "1.8";

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
        public static bool shipAssembled = false;
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
            return new string[][] { new string[] { "assemble", "autoassemble", "prison" } };
        }
        public override string Description()
        {
            return "Assembles the flagship and allows for prision control";
        }

        public override void Execute(string arguments)
        {
            if (!PhotonNetwork.isMasterClient) PulsarModLoader.Utilities.Messaging.Notification("Only the host can use the commands!");
            if (PLEncounterManager.Instance.PlayerShip != null)
            {
                string[] separatedArguments = arguments.Split(' ');
                if (PLEncounterManager.Instance.PlayerShip.ShipTypeID != EShipType.OLDWARS_HUMAN)
                {
                    PulsarModLoader.Utilities.Messaging.Notification("You must be playing with an Interceptor to use the flagship!");
                    return;
                }
                if (arguments == Arguments()[0][0])
                {
                    if (!shipAssembled)
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
                        }
                        else
                        {
                            PulsarModLoader.Utilities.Messaging.Notification("Player not found!");
                        }
                    }
                }
            }
        }

        public static void MoveObjAndChild(Transform transform, int layer)
        {
            foreach (Transform child in transform)
            {
                MoveObjAndChild(child, layer);
            }
            transform.gameObject.layer = layer;
            if (transform.gameObject.name.ToLower().Contains("ff_infected_mesh") || transform.gameObject.name.ToLower().Contains("if_stem") || transform.gameObject.name.ToLower().Contains("infected_bark") || transform.gameObject.tag == "lava" || transform.gameObject.name.ToLower().Contains("ff_infected_small")
                || transform.gameObject.name.ToLower().Contains("if_mound") || transform.gameObject.name.ToLower().Contains("infected_pool") || transform.gameObject.name.ToLower().Contains("terrain") || transform.gameObject.name.ToLower().Contains("infectedasset") || transform.gameObject.name.ToLower().Contains("infected_deco")
                || transform.gameObject.name.ToLower().Contains("infected_tunnel") || transform.gameObject.name.ToLower().Contains("infected_buddle") || transform.gameObject.name.ToLower().Contains("if_bark") || transform.gameObject.name.ToLower().Contains("missionnpc") || transform.gameObject.name.ToLower().Contains("captainchair")
                || transform.gameObject.name.ToLower().Contains("infectedsurface") || transform.gameObject.name.ToLower().Contains("missionvolume") || transform.gameObject.name.ToLower().Contains("if_splatter") || transform.gameObject.name.ToLower().Contains("if_bubble") || transform.gameObject.name.ToLower().Contains("fs_rot")
                || transform.gameObject.name.ToLower().Contains("spawns") || transform.gameObject.name.ToLower().Contains("snowps") || transform.gameObject.name.ToLower().Contains("particle system") || transform.gameObject.name.Split(' ')[0] == "Plane" || transform.gameObject.name.ToLower().Contains("rot_")
                || transform.gameObject.name.ToLower().Contains("humanoid_drone") || transform.gameObject.name.ToLower().Contains("generic_civ") || transform.gameObject.name.ToLower().Contains("deadbody") || transform.gameObject.name.ToLower().Contains("veins_") || transform.gameObject.name.ToLower().Contains("estate_4")
                || transform.gameObject.name.ToLower().Contains("exosuitasset") || transform.gameObject.name.ToLower().Contains("computer_good") || transform.gameObject.name.ToLower().Contains("forsakenflagship_shakingvolume") || transform.gameObject.name.ToLower().Contains("market_deco_02") || transform.gameObject.name.ToLower().Contains("aog_crate_01")
                || transform.gameObject.name.ToLower().Contains("cargocrate_02-2") || transform.gameObject.name.ToLower().Contains("cargo_048") || transform.gameObject.name.ToLower().Contains("cargo_base_01 (2)"))
            {
                transform.gameObject.tag = "Projectile";
            }
            if (transform.name.ToLower().Contains("light"))
            {
                transform.gameObject.SetActive(true);
            }
        }
        public static async void FabricateFlagship()
        {
            if (shipAssembled) return;
            shipAssembled = true;
            PulsarModLoader.Utilities.Messaging.Notification("Assembling the flagship, please stand by...", PLNetworkManager.Instance.LocalPlayer, default, 10000);
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            ship.IsGodModeActive = true;
            AsyncOperation op = SceneManager.LoadSceneAsync(67, LoadSceneMode.Additive);
            AsyncOperation op2 = SceneManager.LoadSceneAsync(10, LoadSceneMode.Additive);
            AsyncOperation op3 = SceneManager.LoadSceneAsync(105, LoadSceneMode.Additive);
            AsyncOperation op4 = SceneManager.LoadSceneAsync(98, LoadSceneMode.Additive);
            while (!op.isDone || !op2.isDone || !op3.isDone || !op4.isDone)
            {
                await Task.Yield();
            }
            Scene Estate = SceneManager.GetSceneByBuildIndex(67);
            Scene Flagship = SceneManager.GetSceneByBuildIndex(105);
            Scene WDHub = SceneManager.GetSceneByBuildIndex(10);
            Scene FluffyMansion = SceneManager.GetSceneByBuildIndex(98);
            PLPilotingSystem currentpisys = ship.PilotingSystem;
            GameObject currentexterior = ship.Exterior;
            List<MeshRenderer> camerasRenders = new List<MeshRenderer>();
            if (ship != null)
            {
                GameObject exterior = null;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                {
                    if (gameObject.name == "Test_Exterior")
                    {
                        exterior = gameObject;
                        break;
                    }
                }
                if (exterior != null)
                {
                    GameObject newexterior = Object.Instantiate(exterior, currentexterior.transform.position, exterior.transform.rotation);
                    newexterior.AddComponent<PLPilotingSystem>();
                    newexterior.GetComponent<PLPilotingSystem>().MyShipInfo = ship;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint = currentpisys.ActivationPoint;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint.position = new Vector3(-0.7f, -261f, -317.3f);
                    currentpisys.enabled = false;
                    currentpisys.ActivationPoint = null;
                    ship.PilotingSystem = newexterior.GetComponent<PLPilotingSystem>();
                    newexterior.AddComponent<PLPilotingHUD>();
                    newexterior.GetComponent<PLPilotingHUD>().MyShipInfo = ship;
                    ship.PilotingHUD = newexterior.GetComponent<PLPilotingHUD>();
                    newexterior.AddComponent<PLShipControl>();
                    newexterior.GetComponent<PLShipControl>().ShipInfo = ship;
                    newexterior.GetComponent<PLShipControl>().LatestSolution = new List<PathNode>();
                    ship.MyShipControl = newexterior.GetComponent<PLShipControl>();
                    newexterior.AddComponent<Rigidbody>();
                    newexterior.GetComponent<Rigidbody>().useGravity = false;
                    newexterior.GetComponent<Rigidbody>().angularDrag = 1.3f;
                    newexterior.GetComponent<Rigidbody>().drag = 0.37f;
                    newexterior.GetComponent<Rigidbody>().inertiaTensor = new Vector3(14139.08f, 22628.87f, 13388.7f);
                    newexterior.GetComponent<Rigidbody>().inertiaTensorRotation = new Quaternion(359.9627f, -0.0003f, -0.0001f, 0);
                    newexterior.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
                    newexterior.GetComponent<Rigidbody>().mass = 680;
                    newexterior.GetComponent<PLShipControl>()._rigidbody = newexterior.GetComponent<Rigidbody>();
                    newexterior.GetComponent<Rigidbody>().angularVelocity = currentexterior.GetComponent<Rigidbody>().angularVelocity;
                    newexterior.GetComponent<Rigidbody>().velocity = currentexterior.GetComponent<Rigidbody>().velocity;
                    newexterior.transform.SetParent(ship.ShipRoot.transform);
                    ship._rigidbody = newexterior.GetComponent<Rigidbody>();
                    newexterior.layer = currentexterior.layer;
                    newexterior.AddComponent<PLFlightAI>();
                    newexterior.GetComponent<PLFlightAI>().MyShipInfo = ship;
                    ship.MyFlightAI = newexterior.GetComponent<PLFlightAI>();
                    newexterior.tag = "Ship";
                    Object.DontDestroyOnLoad(newexterior);
                    ship.ExteriorRenderers.Add(newexterior.GetComponent<MeshRenderer>());
                    ship.ExteriorRigidbody = newexterior.GetComponent<Rigidbody>();
                    ship.ExteriorMeshRenderer = newexterior.GetComponent<MeshRenderer>();
                    ship.Exterior = newexterior;
                    ship.ExteriorTransformCached = newexterior.transform;
                    ship.ExteriorCollider = newexterior.GetComponent<MeshCollider>();
                    ship.ExteriorMeshCollider = newexterior.GetComponent<MeshCollider>();
                    ship.Hull_Virtual_MeshCollider = newexterior.GetComponent<MeshCollider>();
                    ship.OrbitCameraMinDistance = 920f;
                    ship.OrbitCameraMaxDistance = 1550f;
                    ship.WarpBlocker.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.localPosition = Vector3.zero;
                    ship.WarpBlocker.transform.localPosition = Vector3.zero;
                    ship.MainTurretPoint.transform.SetParent(newexterior.transform);
                    ship.MainTurretPoint.transform.localPosition = new Vector3(0, 171.563f, -478.4174f);
                    ship.MainTurretPoint.transform.localScale = Vector3.one * 0.2f;
                    ship.MainTurretPoint.transform.localRotation = new Quaternion(0, 0, 0, 0);
                    List<Transform> allturretpoints = new List<Transform>();
                    ship.RegularTurretPoints[0].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[0].transform.localPosition = new Vector3(-75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[0].transform.localScale = Vector3.one * 10f;
                    ship.RegularTurretPoints[0].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    allturretpoints.Add(ship.RegularTurretPoints[0]);
                    ship.RegularTurretPoints[1].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[1].transform.localPosition = new Vector3(75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[1].transform.localScale = Vector3.one * 10f;
                    ship.RegularTurretPoints[1].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    allturretpoints.Add(ship.RegularTurretPoints[1]);
                    GameObject newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(-0.3341f, 54.5146f, 237.6466f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    if (ship.GetTurretAtID(3) != null && ship.GetTurretAtID(3).TurretInstance != null)
                    {
                        ship.GetTurretAtID(3).TurretInstance.transform.position = newturretpoint.transform.position;
                        ship.GetTurretAtID(3).TurretInstance.transform.rotation = newturretpoint.transform.rotation;
                        if (newturretpoint.transform.childCount > 0) Object.Destroy(newturretpoint.transform.GetChild(0).gameObject);
                        ship.GetTurretAtID(3).TurretInstance.transform.SetParent(newturretpoint.transform);
                    }
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(0.9832f, 63.7453f, 12.822f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    if (ship.GetTurretAtID(4) != null && ship.GetTurretAtID(4).TurretInstance != null)
                    {
                        ship.GetTurretAtID(4).TurretInstance.transform.position = newturretpoint.transform.position;
                        ship.GetTurretAtID(4).TurretInstance.transform.rotation = newturretpoint.transform.rotation;
                        if (newturretpoint.transform.childCount > 0) Object.Destroy(newturretpoint.transform.GetChild(0).gameObject);
                        ship.GetTurretAtID(4).TurretInstance.transform.SetParent(newturretpoint.transform);
                    }
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(-193.6323f, -28.8545f, -608.6761f);
                    newturretpoint.transform.localRotation = new Quaternion(0, 0, 0.7071f, 0.7071f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    if (ship.GetTurretAtID(5) != null && ship.GetTurretAtID(5).TurretInstance != null)
                    {
                        ship.GetTurretAtID(5).TurretInstance.transform.position = newturretpoint.transform.position;
                        ship.GetTurretAtID(5).TurretInstance.transform.rotation = newturretpoint.transform.rotation;
                        if (newturretpoint.transform.childCount > 0) Object.Destroy(newturretpoint.transform.GetChild(0).gameObject);
                        ship.GetTurretAtID(5).TurretInstance.transform.SetParent(newturretpoint.transform);
                    }
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(193.2294f, -28.8545f, -608.6761f);
                    newturretpoint.transform.localRotation = new Quaternion(0, 0, 0.7071f, -0.7071f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    if (ship.GetTurretAtID(6) != null && ship.GetTurretAtID(6).TurretInstance != null)
                    {
                        ship.GetTurretAtID(6).TurretInstance.transform.position = newturretpoint.transform.position;
                        ship.GetTurretAtID(6).TurretInstance.transform.rotation = newturretpoint.transform.rotation;
                        if (newturretpoint.transform.childCount > 0) Object.Destroy(newturretpoint.transform.GetChild(0).gameObject);
                        ship.GetTurretAtID(6).TurretInstance.transform.SetParent(newturretpoint.transform);
                    }
                    ship.RegularTurretPoints = allturretpoints.ToArray();
                    ship.CurrentTurretControllerPlayerID = new int[7] { -1, -1, -1, -1, -1, -1, -1 };
                    GameObject clamps = null;
                    foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                    {
                        if (gameObject.name == "Exterior_FactoryClamps" && gameObject.transform.parent == newexterior.transform)
                        {
                            clamps = gameObject;
                            break;
                        }
                    }
                    if (clamps != null)
                    {
                        Object.Destroy(clamps);
                    }
                    foreach (MeshRenderer renderer in currentexterior.GetComponentsInChildren<MeshRenderer>())
                    {
                        renderer.enabled = false;
                    }
                    foreach (MeshCollider collider in currentexterior.GetComponentsInChildren<MeshCollider>())
                    {
                        collider.enabled = false;
                    }
                    currentexterior.transform.position = newexterior.transform.position;
                    currentexterior.transform.SetParent(newexterior.transform);
                    currentexterior.SetActive(false);
                }
                GameObject interior = null;
                GameObject bridge = null;
                GameObject rightwing = null;
                GameObject rightwingDeco = null;
                GameObject vault = null;
                GameObject vaultDeco = null;
                GameObject engineering = null;
                GameObject reactorroom = null;
                GameObject copydoor = null;
                GameObject smallturret1 = null;
                GameObject smallturret2 = null;
                GameObject mainturret = null;
                GameObject weaponssys = null;
                GameObject nukeswitch1 = null;
                GameObject nukeswitch2 = null;
                GameObject nukecore = null;
                GameObject lifesys = null;
                GameObject sciencesys = null;
                GameObject fuelboard = null;
                GameObject fueldecal = null;
                List<Light> allLights = new List<Light>();
                GameObject enginesys = null;
                GameObject switchboard = null;
                GameObject[] powerswitches = new GameObject[3] { null, null, null };
                GameObject hullheal = null;
                GameObject chair = null;
                GameObject ejectswitch = null;
                GameObject ejectlabel = null;
                GameObject safetyswitch = null;
                GameObject safetybox = null;
                GameObject safetylabel = null;
                GameObject teldoor = null;
                GameObject barber = null;
                GameObject neural = null;
                GameObject landdrone = null;
                GameObject blackbox = null;
                GameObject foxplush = null;
                GameObject walkway = null;
                GameObject fighterCargo = null;
                GameObject fighterCargoBase = null;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>(true))
                {
                    if (gameObject.name == "Planet" && gameObject.scene.buildIndex == 105)
                    {
                        interior = gameObject;
                    }
                    else if (gameObject.name == "BridgeBSO")
                    {
                        bridge = gameObject;
                    }
                    else if (gameObject.name == "FS_RightCrew_01")
                    {
                        rightwing = gameObject;
                    }
                    else if (gameObject.name == "RIGHT_CREW")
                    {
                        rightwingDeco = gameObject;
                    }
                    else if (gameObject.name == "FS_Vault")
                    {
                        vault = gameObject;
                    }
                    else if (gameObject.name == "VAULT")
                    {
                        vaultDeco = gameObject;
                    }
                    else if (gameObject.name == "FS_Engineering_Room")
                    {
                        engineering = gameObject.transform.parent.gameObject;
                    }
                    else if (gameObject.name == "REACTOR" && gameObject.scene.buildIndex == 67)
                    {
                        reactorroom = gameObject;
                    }
                    else if (gameObject.name == "WD_AutomatedDoors (3)")
                    {
                        copydoor = gameObject;
                    }
                    else if (gameObject.name == "Turrett_Control_01" && smallturret1 == null && gameObject != smallturret2)
                    {
                        smallturret1 = gameObject;
                    }
                    else if (gameObject.name == "Turrett_Control_01" && gameObject != smallturret1)
                    {
                        smallturret2 = gameObject;
                    }
                    else if (gameObject.name == "Turrett_Control_02")
                    {
                        mainturret = gameObject;
                    }
                    else if (gameObject.name == "SystemInstance_Weapons")
                    {
                        weaponssys = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).name == "pCube24" && nukeswitch1 == null && gameObject != nukeswitch2 && gameObject.transform.childCount == 3)
                    {
                        nukeswitch1 = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).name == "pCube24" && gameObject != nukeswitch1 && gameObject.transform.childCount == 3)
                    {
                        nukeswitch2 = gameObject;
                    }
                    else if (gameObject.name == "Nuke_Desk_02")
                    {
                        nukecore = gameObject;
                    }
                    else if (gameObject.name == "LifeSupportSysInstance")
                    {
                        lifesys = gameObject;
                    }
                    else if (gameObject.name == "ComputerSysInstance")
                    {
                        sciencesys = gameObject;
                    }
                    else if (gameObject.name == "FuelBoard")
                    {
                        fuelboard = gameObject;
                    }
                    else if (gameObject.name == "Decal_ManualProgramCharge")
                    {
                        fueldecal = gameObject;
                    }
                    else if (gameObject.name == "Planet" && gameObject.scene.buildIndex == 67)
                    {
                        foreach (Light light in gameObject.GetComponentsInChildren<Light>(true))
                        {
                            if (light != null)
                            {
                                allLights.Add(light);
                            }
                        }
                    }
                    else if (gameObject.name == "EngineeringSysInstance")
                    {
                        enginesys = gameObject;
                    }
                    else if (gameObject.name == "Switchboard_01")
                    {
                        switchboard = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount == 4 && powerswitches[0] == null)
                    {
                        powerswitches[0] = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount == 4 && powerswitches[1] == null && gameObject != powerswitches[0])
                    {
                        powerswitches[1] = gameObject;
                    }
                    else if (gameObject.name == "Switch_01" && gameObject.transform.childCount == 4 && powerswitches[2] == null && gameObject != powerswitches[1])
                    {
                        powerswitches[2] = gameObject;
                    }
                    else if (gameObject.name == "ScrapStation")
                    {
                        hullheal = gameObject;
                    }
                    else if (gameObject.name == "Chair_01 (15)" && gameObject.scene.buildIndex == 105)
                    {
                        chair = gameObject;
                    }
                    else if (gameObject.name == "Lever_01" && gameObject.GetComponent<PLReactorCoreEjectStation>() != null)
                    {
                        ejectswitch = gameObject;
                    }
                    else if (gameObject.name == "Quad" && gameObject.layer == ship.InteriorStatic.layer)
                    {
                        ejectlabel = gameObject;
                    }
                    else if (gameObject.name == "Lever_01" && gameObject.GetComponent<PLReactorSafetyPanel>() != null)
                    {
                        safetyswitch = gameObject;
                    }
                    else if (gameObject.name == "SafetySwitchboard_01")
                    {
                        safetybox = gameObject;
                    }
                    else if (gameObject.name == "Decal_CoreSafety")
                    {
                        safetylabel = gameObject;
                    }
                    else if (gameObject.name == "Door_01" && gameObject.transform.parent.name == "BridgePrefab")
                    {
                        teldoor = gameObject;
                    }
                    else if (gameObject.name == "TheBarber")
                    {
                        barber = gameObject;
                    }
                    else if (gameObject.name == "NeuralRewriter")
                    {
                        neural = gameObject;
                    }
                    else if (gameObject.name == "LandDrone_Idle")
                    {
                        landdrone = gameObject;
                    }
                    else if (gameObject.name == "Quad" && gameObject.scene == WDHub)
                    {
                        blackbox = gameObject;
                    }
                    else if (gameObject.name == "Toy_Fox")
                    {
                        foxplush = gameObject;
                    }
                    else if (gameObject.name == "Walkway")
                    {
                        walkway = gameObject;
                    }
                    else if(gameObject.name == "Cargo_048") 
                    {
                        fighterCargo = gameObject;
                    }
                    else if (gameObject.name == "Cargo_Base_01 (2)")
                    {
                        fighterCargoBase = gameObject;
                    }
                    if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null && reactorroom != null && copydoor != null
                        && smallturret1 != null && smallturret2 != null && mainturret != null && weaponssys != null && nukeswitch1 != null && nukeswitch2 != null && nukecore != null && lifesys != null
                        && sciencesys != null && fuelboard != null && fueldecal != null && allLights.Count > 0 && enginesys != null && switchboard != null && powerswitches[0] != null
                        && powerswitches[1] != null && powerswitches[2] != null && hullheal != null && chair != null && ejectswitch != null && ejectlabel != null && safetyswitch != null && safetybox != null
                        && safetylabel != null && teldoor != null && barber != null && neural != null && landdrone != null && blackbox != null && foxplush != null && walkway != null && fighterCargo != null && fighterCargoBase != null) break;
                }
                if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null)
                {
                    foreach (PLDoor door in ship.InteriorDynamic.GetComponentsInChildren<PLDoor>())
                    {
                        door.gameObject.transform.position += new Vector3(0, 0, 1500);
                    }
                    foreach (PLUIScreen screen in ship.MyScreenBase.AllScreens)
                    {
                        screen.gameObject.transform.position += new Vector3(0, 0, 1500);
                    }
                    GameObject newinterior = Object.Instantiate(interior, interior.transform.position + new Vector3(400, -400, 1500), interior.transform.rotation);
                    GameObject newbridge = Object.Instantiate(bridge, new Vector3(0, -258.2285f, -409.0034f), bridge.transform.rotation);
                    GameObject newrighwing = Object.Instantiate(rightwing, new Vector3(357.7801f, -367.354f, 1681.315f), rightwing.transform.rotation);
                    GameObject newrightwingdeco = Object.Instantiate(rightwingDeco, new Vector3(462.3824f, -431.5907f, 1618.373f), rightwingDeco.transform.rotation);
                    Vector3 offset = newrighwing.transform.position - rightwing.transform.position;
                    foreach (Light light in allLights)
                    {
                        GameObject newlight = Object.Instantiate(light.gameObject, light.gameObject.transform.position + offset, light.gameObject.transform.rotation);
                        Object.DontDestroyOnLoad(newlight);
                        newlight.transform.SetParent(newinterior.transform);
                    }
                    GameObject newvault = Object.Instantiate(vault, vault.transform.position + offset, new Quaternion(0, 0.0012f, 0, -1));
                    GameObject newvaultdeco = Object.Instantiate(vaultDeco, vaultDeco.transform.position + offset, new Quaternion(0, 0.0012f, 0, -1));
                    GameObject newengineering = Object.Instantiate(engineering, engineering.transform.position + offset + new Vector3(0, 0, 2), new Quaternion(0, 0.0029f, 0, 1));
                    GameObject newreactor = Object.Instantiate(reactorroom, reactorroom.transform.position + offset, new Quaternion(0, 0.0029f, 0, 1));
                    newbridge.transform.localRotation = new Quaternion(0.2202f, 0.0157f, 0.0263f, -0.975f);
                    GameObject newbarber = Object.Instantiate(barber, new Vector3(369f, -443.3361f, 1497), new Quaternion(0, -0.0376f, 0, 0.9993f));
                    Object.DontDestroyOnLoad(newbarber);
                    newbarber.transform.SetParent(newinterior.transform);
                    PLGameStatic.Instance.m_AppearanceStations.Add(newbarber.GetComponentInChildren<PLAppearanceStation>());
                    GameObject newneural = Object.Instantiate(neural, new Vector3(370, -443.3894f, 1551.893f), neural.transform.rotation);
                    Object.DontDestroyOnLoad(newneural);
                    newneural.transform.SetParent(newinterior.transform);
                    PLGameStatic.Instance.m_NeuralRewriters.Add(newneural.GetComponentInChildren<PLNeuralRewriter>());
                    GameObject newlanddrone = Object.Instantiate(landdrone, landdrone.transform.position + offset, landdrone.transform.rotation);
                    Object.DontDestroyOnLoad(newlanddrone);
                    newlanddrone.transform.SetParent(newinterior.transform);
                    GameObject newblackbox = Object.Instantiate(blackbox, new Vector3(282, -432.9246f, 1740), blackbox.transform.rotation);
                    newblackbox.transform.SetParent(newinterior.transform);
                    Object.DontDestroyOnLoad(newblackbox);
                    ship.InteriorRenderers.Add(newblackbox.GetComponent<MeshRenderer>());
                    GameObject StarPlataform1 = Object.Instantiate(blackbox, new Vector3(274.9763f, -424.1945f, 1737.242f), new Quaternion(0.6533f, -0.2706f, -0.2706f, -0.6553f));
                    StarPlataform1.transform.SetParent(newinterior.transform);
                    StarPlataform1.transform.localScale = new Vector3(15, 5, 1);
                    Object.DontDestroyOnLoad(StarPlataform1);
                    ship.InteriorRenderers.Add(StarPlataform1.GetComponent<MeshRenderer>());
                    GameObject StarPlataform2 = Object.Instantiate(blackbox, new Vector3(274.9763f, -424.1945f, 1737.242f), new Quaternion(0.6533f, 0.2706f, -0.2706f, 0.6553f));
                    StarPlataform2.transform.SetParent(newinterior.transform);
                    StarPlataform2.transform.localScale = new Vector3(15, 5, 1);
                    Object.DontDestroyOnLoad(StarPlataform2);
                    ship.InteriorRenderers.Add(StarPlataform2.GetComponent<MeshRenderer>());
                    Object.DontDestroyOnLoad(newinterior);
                    Object.DontDestroyOnLoad(newbridge);
                    Object.DontDestroyOnLoad(newrighwing);
                    Object.DontDestroyOnLoad(newrightwingdeco);
                    Object.DontDestroyOnLoad(newvault);
                    Object.DontDestroyOnLoad(newvaultdeco);
                    Object.DontDestroyOnLoad(newengineering);
                    Object.DontDestroyOnLoad(newreactor);
                    GameObject oldEngineDoor = null;
                    foreach (GameObject @object in Object.FindObjectsOfType<GameObject>(true))
                    {
                        if (@object.name == "WD_AutomatedDoors (14)" && @object.transform.parent.parent == newinterior.transform)
                        {
                            oldEngineDoor = @object;
                            break;
                        }
                    }
                    if (oldEngineDoor != null)
                    {
                        GameObject newEngineDoor = Object.Instantiate(copydoor, new Vector3(358.07f, - 383.02f, 1445.427f), oldEngineDoor.transform.rotation);
                        newEngineDoor.transform.SetParent(newinterior.transform);
                        GameObject newStarDoor = Object.Instantiate(copydoor, new Vector3(278.0616f, -428.5895f, 1715.356f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        newStarDoor.transform.SetParent(newinterior.transform);
                        oldEngineDoor.transform.position += new Vector3(0, 20, 0);
                        GameObject newCaptainDoor = Object.Instantiate(copydoor, new Vector3(372.6432f, -440.1014f, 1670.436f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        newCaptainDoor.transform.SetParent(newinterior.transform);

                    }
                    if (walkway != null)
                    {
                        GameObject newwalkway = Object.Instantiate(walkway, new Vector3(396.2569f, -383.5245f, 1519.001f), walkway.transform.rotation);
                        Object.DontDestroyOnLoad(newwalkway);
                        newwalkway.GetComponentInChildren<PLPushVolume>().MyTLI = ship.MyTLI;
                        newwalkway.transform.SetParent(newinterior.transform);
                        newwalkway = Object.Instantiate(walkway, new Vector3(320.078f, -383.5245f, 1519.001f), new Quaternion(0, 1, 0, 0));
                        Object.DontDestroyOnLoad(newwalkway);
                        newwalkway.GetComponentInChildren<PLPushVolume>().MyTLI = ship.MyTLI;
                        newwalkway.GetComponentInChildren<PLPushVolume>().WindForceGlobal.z *= -1;
                        newwalkway.transform.SetParent(newinterior.transform);
                    }
                    foreach (Transform transform in newinterior.transform)
                    {
                        if (transform.gameObject.name == "REACTOR")
                        {
                            transform.gameObject.tag = "Projectile";
                            break;
                        }
                    }
                    /*
                    foreach(PLUIScreen screen in ship.InteriorDynamic.GetComponentsInChildren<PLUIScreen>(true)) 
                    {
                        ship.MyScreenBase.AllScreens.Remove(screen);
                        Object.Destroy(screen.gameObject);
                    }
                    */
                    newreactor.transform.GetChild(7).gameObject.SetActive(true);
                    Mod.reactorEffect = newreactor.transform.GetComponentInChildren<ParticleSystem>(true);
                    Mod.reactorEffect.startColor = new Color(0.3835f, 0, 0.6f, 1);
                    Mod.reactorEffect.startSize = 1f;
                    Mod.reactorEffect.gameObject.SetActive(true);
                    Object.DontDestroyOnLoad(Mod.reactorEffect.gameObject);
                    foreach (Transform transform in newreactor.transform)
                    {
                        if (transform.gameObject.name == "Sphere")
                        {
                            transform.gameObject.SetActive(false);
                        }
                        if (transform.gameObject.name.Contains("Particle System"))
                        {
                            transform.gameObject.name = transform.gameObject.name.Replace("Particle System", "Reactor Particle");
                        }
                    }
                    MoveObjAndChild(newinterior.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newbridge.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newrighwing.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newrightwingdeco.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newvault.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newvaultdeco.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newengineering.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newreactor.transform, ship.InteriorStatic.layer);
                    ship.InteriorStatic.transform.localPosition = ship.InteriorStatic.transform.localPosition + new Vector3(0, 0, 1500);
                    newrightwingdeco.transform.SetParent(newrighwing.transform);
                    newrighwing.transform.SetParent(newinterior.transform);
                    newvault.transform.SetParent(newinterior.transform);
                    newvaultdeco.transform.SetParent(newvault.transform);
                    newengineering.transform.SetParent(newinterior.transform);
                    newreactor.transform.SetParent(newinterior.transform);
                    foreach (ParticleSystem particle in ship.ReactorInstance.gameObject.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        particle.startLifetime = 5;
                    }
                    GameObject[] infectedstuff = GameObject.FindGameObjectsWithTag("Projectile");
                    foreach (GameObject @object in infectedstuff)
                    {
                        Object.DestroyImmediate(@object);
                    }
                    foreach (MeshRenderer render in newinterior.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (render != null)
                        {
                            ship.InteriorRenderers.Add(render);
                        }
                    }
                    foreach (MeshRenderer render in newbridge.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (render != null)
                        {
                            ship.InteriorRenderers.Add(render);
                        }
                    }
                    ship.InteriorShipLights.Clear();
                    foreach (Light light in newbridge.GetComponentsInChildren<Light>(true))
                    {
                        if (light != null)
                        {
                            ship.InteriorShipLights.Add(light);
                            light.enabled = true;
                        }
                    }
                    foreach (Light light in newinterior.GetComponentsInChildren<Light>(true))
                    {
                        if (light != null)
                        {
                            ship.InteriorShipLights.Add(light);
                            light.enabled = true;
                        }
                    }
                    foreach (PLDoor door in newinterior.GetComponentsInChildren<PLDoor>(true))
                    {
                        if (door != null)
                        {
                            door.gameObject.SetActive(true);
                            door.MyShipInfo = ship;
                        }
                    }
                    foreach (PLLockedSeamlessDoor lockedoor in newinterior.GetComponentsInChildren<PLLockedSeamlessDoor>(true))
                    {
                        if (lockedoor != null)
                        {
                            lockedoor.RequiredItem = "Hands";
                        }
                    }
                    foreach (Transform transform in newinterior.transform.GetChild(5))
                    {
                        if (transform.gameObject.name.Contains("FS_Bar_Brig_Deco_02"))
                        {
                            if (!transform.gameObject.name.Contains("("))
                            {
                                prisionCells[0] = transform.gameObject;
                                continue;
                            }
                            for (int i = 1; i < 10; i++)
                            {
                                if (transform.gameObject.name.Contains("(" + i + ")"))
                                {
                                    prisionCells[i] = transform.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                    GameObject Abyssdeathobject = Object.Instantiate(blackbox);
                    PLKillVolume abyssdeath = Abyssdeathobject.AddComponent<PLKillVolume>();
                    Abyssdeathobject.transform.position = new Vector3(447, -501, 1514);
                    abyssdeath.Dimensions = new Vector3(200, 5, 200);
                    Object.DontDestroyOnLoad(Abyssdeathobject);
                    Abyssdeathobject.transform.SetParent(newinterior.transform);
                    foreach (PLAmbientSFXControl sfx in newinterior.GetComponentsInChildren<PLAmbientSFXControl>())
                    {
                        if (sfx.Event.ToLower().Contains("infected"))
                        {
                            sfx.enabled = false;
                        }
                    }
                    PLTeleportationScreen origianlTP = null;
                    foreach (PLUIScreen mesa in ship.MyScreenBase.AllScreens)
                    {
                        if (mesa is PLTeleportationScreen)
                        {
                            origianlTP = mesa as PLTeleportationScreen;
                            break;
                        }
                    }
                    ship.CaptainsChairPivot.position = new Vector3(-0.7818f, -261.7534f, -324.4219f);
                    if (origianlTP != null)
                    {
                        foreach (PLClonedScreen screen in newinterior.GetComponentsInChildren<PLClonedScreen>())
                        {
                            screen.MyTargetScreen = origianlTP;
                        }
                    }
                    PLInteriorDoor CapitanToBridge = newinterior.GetComponentInChildren<PLInteriorDoor>(true);
                    PLInteriorDoor BridgeToCaptain = newbridge.GetComponentInChildren<PLInteriorDoor>(true);
                    if (CapitanToBridge != null && BridgeToCaptain != null)
                    {
                        CapitanToBridge.OptionalTLI = ship.MyTLI;
                        CapitanToBridge.TargetDoor = BridgeToCaptain;
                        CapitanToBridge.transform.rotation = new Quaternion(0, 0.7124f, 0, -0.7018f);
                        CapitanToBridge.MyInterior = newbridge.GetComponent<PLInterior>();
                        CapitanToBridge.MyInterior.InteriorID = -69;
                        CapitanToBridge.IsEntrance = true;
                        BridgeToCaptain.TargetDoor = CapitanToBridge;
                        BridgeToCaptain.OptionalTLI.SubHubID = -69;
                        BridgeToCaptain.OptionalTLI = ship.MyTLI;
                        BridgeToCaptain.IsEntrance = false;
                        GameObject BridgeToEngineOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(-0.6f, -261, -443.1f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(BridgeToEngineOjb);
                        BridgeToEngineOjb.transform.SetParent(newbridge.transform);
                        PLInteriorDoor BridgeToEngine = BridgeToEngineOjb.GetComponent<PLInteriorDoor>();
                        GameObject EngineToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(378.7f, -384.8f, 1366.8f), new Quaternion(0, -0.6448f, 0, 0.7644f));
                        Object.DontDestroyOnLoad(EngineToBridgeOjb);
                        EngineToBridgeOjb.transform.SetParent(newinterior.transform);
                        PLInteriorDoor EngineToBridge = EngineToBridgeOjb.GetComponent<PLInteriorDoor>();
                        if (BridgeToEngine != null && EngineToBridge != null)
                        {
                            BridgeToEngine.TargetDoor = EngineToBridge;
                            BridgeToEngine.VisibleName = "Engineering";
                            EngineToBridge.TargetDoor = BridgeToEngine;
                            EngineToBridge.VisibleName = "Bridge";
                            EngineToBridge.MyInterior = CapitanToBridge.MyInterior;
                            EngineToBridge.IsEntrance = true;

                        }
                        GameObject BridgeToScienceOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(27.6f, -261, -395.3f), new Quaternion(0, -0.8471f, 0, 0.5314f));
                        Object.DontDestroyOnLoad(BridgeToScienceOjb);
                        PLInteriorDoor BridgeToScience = BridgeToScienceOjb.GetComponent<PLInteriorDoor>();
                        GameObject ScienceToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(393.8795f, -399.9159f, 1705.848f), Quaternion.Euler(0,0,0));
                        Object.DontDestroyOnLoad(ScienceToBridgeOjb);
                        PLInteriorDoor ScienceToBridge = ScienceToBridgeOjb.GetComponent<PLInteriorDoor>();
                        if (BridgeToScience != null && ScienceToBridge != null)
                        {
                            BridgeToScience.TargetDoor = ScienceToBridge;
                            BridgeToScience.VisibleName = "Atrium";
                            ScienceToBridge.TargetDoor = BridgeToScience;
                            ScienceToBridge.VisibleName = "Bridge";
                            ScienceToBridge.MyInterior = CapitanToBridge.MyInterior;
                            ScienceToBridge.IsEntrance = true;
                            //ScienceToBridge.MyInterior = BridgeToCaptain.MyInterior;
                        }
                        GameObject EngineToReactorOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(374.3f, -384.9f, 1387.7f), new Quaternion(0, -0.9406f, 0, 0.3364f));
                        Object.DontDestroyOnLoad(EngineToReactorOjb);
                        PLInteriorDoor EngineToReactor = EngineToReactorOjb.GetComponent<PLInteriorDoor>();
                        GameObject ReactorToEngineOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(367.4f, -442.5f, 1403.1f), new Quaternion(0, -0.9722f, 0, 0.2343f));
                        Object.DontDestroyOnLoad(ReactorToEngineOjb);
                        PLInteriorDoor ReactorToEngine = ReactorToEngineOjb.GetComponent<PLInteriorDoor>();
                        if (EngineToReactor != null && ReactorToEngine != null)
                        {
                            EngineToReactor.TargetDoor = ReactorToEngine;
                            EngineToReactor.VisibleName = "Reactor";
                            ReactorToEngine.TargetDoor = EngineToReactor;
                            ReactorToEngine.VisibleName = "Engineering";
                        }
                        GameObject EngineToScrapOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(378.5f, -384.9f, 1380.2f), new Quaternion(0, -0.8484f, 0, 0.5294f));
                        Object.DontDestroyOnLoad(EngineToScrapOjb);
                        PLInteriorDoor EngineToScrap = EngineToScrapOjb.GetComponent<PLInteriorDoor>();
                        GameObject ScrapToEngineOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(469.2f, -399.4f, 1485.9f), new Quaternion(0, 0.7061f, 0, 0.7081f));
                        Object.DontDestroyOnLoad(ScrapToEngineOjb);
                        PLInteriorDoor ScrapToEngine = ScrapToEngineOjb.GetComponent<PLInteriorDoor>();
                        if (EngineToScrap != null && ScrapToEngine != null)
                        {
                            EngineToScrap.TargetDoor = ScrapToEngine;
                            EngineToScrap.VisibleName = "Vault";
                            ScrapToEngine.TargetDoor = EngineToScrap;
                            ScrapToEngine.VisibleName = "Engineering";
                        }
                        GameObject WeaponsToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(-13.4f, -261f, -364.3f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(WeaponsToBridgeOjb);
                        PLInteriorDoor WeaponsToBridge = WeaponsToBridgeOjb.GetComponent<PLInteriorDoor>();
                        GameObject BridgeToWeaponsOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(375.4f, -382.7f, 1575.7f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(BridgeToWeaponsOjb);
                        PLInteriorDoor BridgeToWeapons = BridgeToWeaponsOjb.GetComponent<PLInteriorDoor>();
                        if (WeaponsToBridge != null && BridgeToWeapons != null)
                        {
                            WeaponsToBridge.TargetDoor = BridgeToWeapons;
                            WeaponsToBridge.VisibleName = "Weapons";
                            BridgeToWeapons.TargetDoor = WeaponsToBridge;
                            BridgeToWeapons.VisibleName = "Bridge";
                            BridgeToWeapons.MyInterior = CapitanToBridge.MyInterior;
                            BridgeToWeapons.IsEntrance = true;
                            //BridgeToWeapons.MyInterior = BridgeToCaptain.MyInterior;
                        }
                    }
                    ship.AllInteriorShipLightIntensities = new float[ship.InteriorShipLights.Count];
                    ship.AllInteriorShipLightColors = new Color[ship.InteriorShipLights.Count];
                    ship.AllInteriorShipLightModIntensities = new float[ship.InteriorShipLights.Count];
                    int num2 = 0;
                    foreach (Light light3 in ship.InteriorShipLights)
                    {
                        light3.cullingMask = -4722690;
                        ship.AllInteriorShipLightIntensities[num2] = ship.InteriorShipLights[num2].intensity;
                        ship.AllInteriorShipLightColors[num2] = ship.InteriorShipLights[num2].color;
                        num2++;
                        if (light3.gameObject.activeSelf && light3.gameObject.GetComponent<PLDisableLight>() == null)
                        {
                            PLGameStatic.Instance.AddDLI(new DisableLightInfo
                            {
                                light = light3,
                                minQualityLevel = -1,
                                isOnShip = true
                            });
                        }
                    }
                    for (int j = 0; j < ship.InteriorShipLights.Count; j++)
                    {
                        ship.AllInteriorShipLightModIntensities[j] = 0f;
                    }
                    if (newinterior.GetComponent<MeshRenderer>() != null) ship.InteriorRenderers.Add(newinterior.GetComponent<MeshRenderer>());
                    PLSpawner[] spawners = newinterior.GetComponentsInChildren<PLSpawner>();
                    foreach (PLSpawner spawner in newinterior.GetComponentsInChildren<PLSpawner>())
                    {
                        spawner.enabled = false;
                    }
                    for (int i = 0; i < spawners.Length; i++)
                    {
                        Object.Destroy(spawners[i]);
                    }
                    newinterior.AddComponent<PLShipConnection>();
                    newinterior.GetComponent<PLShipConnection>().MyShip = ship;
                    foreach (PLTeleportationLocationInstance tp in Object.FindObjectsOfType<PLTeleportationLocationInstance>())
                    {
                        if (tp.name == "PLGame" && tp.AllTTIs.Length > 1 && tp.AllTTIs[1].gameObject.transform.parent == newinterior.transform)
                        {
                            //tp.MyShipInfo = ship;
                            //ship.MyTLI = tp;
                            break;
                        }
                    }
                    //How me changing the starting position of the contraband station broke this part of the code???????
                    //PLPathfinder.GetInstance().AllPGEs.RemoveAll((PLPathfinderGraphEntity graph) => graph.ID == ship.InteriorStatic.transform.GetChild(1).GetComponent<PLShipAStarConnection>().navGraphIDs[0]);
                    ship.InteriorStatic.transform.GetChild(1).GetComponent<PLShipAStarConnection>().navGraphIDs.Clear();
                    Object.Destroy(ship.InteriorStatic.transform.GetChild(1));
                    ship.InteriorStatic.transform.position = new Vector3(367.3f, -382.3f, 1548);
                    newinterior.transform.SetParent(ship.InteriorStatic.transform);
                    newbridge.transform.SetParent(ship.InteriorStatic.transform);
                    newinterior.transform.GetChild(0).localPosition = new Vector3(-117, 22.6f, 7);
                    newinterior.GetComponentInChildren<PLPlanetAStarConnection>().TLI = ship.MyTLI;
                    newinterior.GetComponentInChildren<PLPlanetAStarConnection>().DataPath = "Assets/Resources/Navgraphs/AOG_HUB_NAVGRAPH.bytes";
                    //Matrix4x4 oldMat = Matrix4x4.TRS(newinterior.GetComponentInChildren<PLPlanetAStarConnection>().SavedLoc, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                    Matrix4x4 oldMat = Matrix4x4.TRS(newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position + new Vector3(400, -400, 1500), Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.localScale) * Matrix4x4.TRS(Vector3.zero, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.rotation, Vector3.one);
                    Matrix4x4 newMat = Matrix4x4.TRS(newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position + new Vector3(400, -400, 1500) + new Vector3(400, -400, 1500), Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.localScale) * Matrix4x4.TRS(Vector3.zero, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.rotation, Vector3.one);
                    foreach (uint inID2 in newinterior.GetComponentInChildren<PLPlanetAStarConnection>().navGraphIDs)
                    {
                        RecastGraph graph = PLPathfinder.GetInstance().GetPGEFromID(inID2).Graph;
                        graph.forcedBoundsCenter += newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position - newinterior.GetComponentInChildren<PLPlanetAStarConnection>().SavedLoc;
                        graph.RelocateNodes(newMat * oldMat.inverse);
                    }
                    //newinterior.GetComponentInChildren<PLPlanetAStarConnection>().SavedLoc = new Vector3(-43.8946f, 34.6f, 69.9739f);
                    //newinterior.GetComponentInChildren<PLPlanetAStarConnection>().DataPath = "Assets/Resources/Navgraphs/AOG_HUB_NAVGRAPH.bytes";
                    newbridge.GetComponentInChildren<PLCustomAStarConnection>().TLI = ship.MyTLI;
                    //newbridge.AddComponent<PLRoomArea>();
                    //newbridge.GetComponent<PLRoomArea>().Dimensions = new Vector3(1500, 800, 9999);
                    //List<PLRoomArea> areas = PLEncounterManager.Instance.PlayerShip.AllRoomAreas.ToList();
                    //areas.Add(newbridge.GetComponent<PLRoomArea>());
                    //PLEncounterManager.Instance.PlayerShip.AllRoomAreas = areas.ToArray();
                    //newbridge.GetComponent<PLRoomArea>().Show();
                    fighterCargo.transform.SetParent(fighterCargoBase.transform);
                    int Fightercounter = 0;
                    for(int i = 0; i < 5; i++) 
                    {
                        for(int j = 0; j < 2; j++) 
                        {
                            float Z = i * 7.32f;
                            if (i == 4) Z = 42.84f;
                            GameObject fighterCargoClone = Object.Instantiate(fighterCargoBase, new Vector3(307.4266f + (j * 7.7149f), -443.78f, 1518.769f - Z), fighterCargoBase.transform.rotation);
                            Object.DontDestroyOnLoad(fighterCargoClone);
                            fighterCargoClone.transform.SetParent(newinterior.transform);
                            fighterCargoClone.layer = newinterior.gameObject.layer;
                            fighterCargoClone.transform.GetChild(0).gameObject.layer = newinterior.gameObject.layer;
                            fighterCargoClone.name = $"FighterCargoBase ({i})";
                            fighterCargoClone.transform.GetChild(0).name = $"FighterCargo ({i})";
                            PLFighterScreen.fighterCargo[Fightercounter] = fighterCargoClone;
                            Fightercounter++;
                        }
                    }
                    List<Transform> allturrets = new List<Transform>();
                    if (smallturret1 != null)
                    {
                        smallturret1.transform.position = new Vector3(390.0232f, -383.5964f, 1517.274f);
                        smallturret1.transform.rotation = new Quaternion(0, 0, 0, 1);
                        allturrets.Add(smallturret1.transform);
                        ship.InteriorRenderers.Add(smallturret1.GetComponent<MeshRenderer>());
                        GameObject newturret = Object.Instantiate(smallturret1, smallturret1.transform.position - new Vector3(0, 0, 34), smallturret1.transform.rotation);
                        Object.DontDestroyOnLoad(newturret);
                        allturrets.Add(newturret.transform);
                        ship.InteriorRenderers.Add(newturret.GetComponent<MeshRenderer>());
                        newturret = Object.Instantiate(smallturret1, smallturret1.transform.position + new Vector3(0, 0, 34), smallturret1.transform.rotation);
                        Object.DontDestroyOnLoad(newturret);
                        allturrets.Add(newturret.transform);
                        ship.InteriorRenderers.Add(newturret.GetComponent<MeshRenderer>());
                    }
                    if (smallturret2 != null)
                    {
                        smallturret2.transform.position = new Vector3(326.2234f, -383.5964f, 1517.274f);
                        smallturret2.transform.rotation = new Quaternion(0, 1, 0, 0);
                        allturrets.Add(smallturret2.transform);
                        ship.InteriorRenderers.Add(smallturret2.GetComponent<MeshRenderer>());
                        GameObject newturret = Object.Instantiate(smallturret2, smallturret2.transform.position - new Vector3(0, 0, 34), smallturret2.transform.rotation);
                        Object.DontDestroyOnLoad(newturret);
                        allturrets.Add(newturret.transform);
                        ship.InteriorRenderers.Add(newturret.GetComponent<MeshRenderer>());
                        newturret = Object.Instantiate(smallturret2, smallturret2.transform.position + new Vector3(0, 0, 34), smallturret2.transform.rotation);
                        Object.DontDestroyOnLoad(newturret);
                        allturrets.Add(newturret.transform);
                        ship.InteriorRenderers.Add(newturret.GetComponent<MeshRenderer>());
                    }
                    ship.WeaponsSystem.RegularTurretStationTransforms = allturrets.ToArray();
                    if (mainturret != null)
                    {
                        mainturret.transform.position = new Vector3(358, -383.6652f, 1572.374f);
                        mainturret.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.InteriorRenderers.Add(mainturret.GetComponent<MeshRenderer>());
                    }
                    if (weaponssys != null)
                    {
                        weaponssys.transform.position = new Vector3(358.1818f, -383.6548f, 1588.564f);
                        weaponssys.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.SysInstUIRoots[1].transform.position = new Vector3(358.1926f, -382.2694f, 1587.644f);
                        ship.SysInstUIRoots[1].transform.rotation = new Quaternion(0, 0, 0, 1);
                    }
                    if (nukecore != null && nukeswitch1 != null && nukeswitch2 != null)
                    {
                        nukeswitch1.transform.SetParent(nukecore.transform);
                        nukeswitch2.transform.SetParent(nukecore.transform);
                        nukeswitch2.transform.localPosition = new Vector3(0.3883f, 1.5048f, -0.873f);
                        ship.NukeActivator.transform.SetParent(nukecore.transform);
                        nukecore.transform.position = new Vector3(371.977f, -383.6779f, 1609);
                        nukecore.transform.rotation = new Quaternion(0, 1, 0, 0);
                    }
                    if (lifesys != null)
                    {
                        lifesys.transform.position = new Vector3(380.4184f, -400.8968f, 1732.827f);
                        lifesys.transform.rotation = new Quaternion(0, 1, 0, 0);
                        ship.SysInstUIRoots[3].transform.position = new Vector3(381.32f, -399.8868f, 1732.855f);
                        ship.SysInstUIRoots[3].transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                    }
                    if (sciencesys != null)
                    {
                        sciencesys.transform.position = new Vector3(335.9909f, -400.8744f, 1732.785f);
                        sciencesys.transform.rotation = new Quaternion(-0.002f, 0, 0.0005f, -1);
                        ship.SysInstUIRoots[0].transform.position = new Vector3(335.433f, -399.7129f, 1732.852f);
                        ship.SysInstUIRoots[0].transform.rotation = new Quaternion(0.0014f, -0.7071f, -0.0043f, -0.7071f);
                        ship.ResearchWorldRootBGObj.transform.position = new Vector3(316.6057f, -399, 1735.064f);
                        ship.ResearchWorldRootBGObj.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.ResearchLockerWorldRootBGObj.transform.position = new Vector3(315.9459f, -398.9975f, 1732.498f);
                        ship.ResearchLockerWorldRootBGObj.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerFrame.transform.position = new Vector3(316.0803f, -399.738f, 1732.706f);
                        ship.ResearchLockerFrame.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerCollider.transform.position = new Vector3(316.0803f, -399.738f, 1732.706f);
                        ship.ResearchLockerCollider.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                        ship.ResearchLockerWorldRoot.transform.position = new Vector3(316.0803f, -399.738f, 1732.706f);
                        ship.ResearchLockerWorldRoot.transform.rotation = new Quaternion(0, 0.4468f, 0, -0.8946f);
                    }
                    if (enginesys != null)
                    {
                        enginesys.transform.position = new Vector3(357.2437f, -385.8001f, 1345.707f);
                        ship.SysInstUIRoots[2].transform.position = new Vector3(357.2109f, -384.793f, 1346.845f);
                    }
                    if (fuelboard != null)
                    {
                        ship.WarpFuelBoostLever.transform.position = new Vector3(369.8772f, -384.394f, 1390.938f);
                        ship.WarpFuelBoostLever.transform.rotation = new Quaternion(0, 0.541f, 0, -0.841f);
                        ship.WarpFuelBoostLever.LightModelRenderer.transform.position = new Vector3(369.8477f, -383.7385f, 1390.91f);
                        ship.WarpFuelBoostLever.LightModelRenderer.transform.rotation = new Quaternion(0, 0.541f, 0, -0.841f);
                        ship.WarpFuelBoostLever.CapsuleMeshRenderer.transform.position = new Vector3(369.1632f, -384.4381f, 1391.212f);
                        ship.WarpFuelBoostLever.CapsuleMeshRenderer.transform.rotation = new Quaternion(0, 0.541f, 0, -0.841f);
                        fuelboard.transform.position = new Vector3(369.5135f, -384.2985f, 1391.068f);
                        fuelboard.transform.rotation = new Quaternion(0, 0.2346f, 0, 0.9721f);
                        if (fueldecal != null)
                        {
                            fueldecal.transform.position = new Vector3(369.9951f, -383.163f, 1391.209f);
                            fueldecal.transform.rotation = new Quaternion(0, 0.2233f, 0, 0.9748f);
                        }
                    }
                    if (switchboard != null)
                    {
                        switchboard.transform.position = new Vector3(347.1287f, -384.3018f, 1391.802f);
                        switchboard.transform.rotation = new Quaternion(0, 0.8528f, 0, -0.5223f);
                        if (powerswitches[0] != null)
                        {
                            powerswitches[0].transform.position = new Vector3(346.249f, -384.2436f, 1391.329f);
                            powerswitches[0].transform.rotation = new Quaternion(0, 0.5157f, 0, 0.8568f);
                        }
                        if (powerswitches[1] != null)
                        {
                            powerswitches[1].transform.position = new Vector3(346.8275f, -384.2436f, 1391.644f);
                            powerswitches[1].transform.rotation = new Quaternion(0, 0.5157f, 0, 0.8568f);
                        }
                        if (powerswitches[2] != null)
                        {
                            powerswitches[2].transform.position = new Vector3(347.4274f, -384.2436f, 1391.958f);
                            powerswitches[2].transform.rotation = new Quaternion(0, 0.5157f, 0, 0.8568f);
                        }
                    }
                    if (safetybox != null)
                    {
                        if (safetyswitch != null)
                        {
                            safetyswitch.transform.SetParent(safetybox.transform);
                        }
                        if (safetylabel != null)
                        {
                            safetylabel.transform.SetParent(safetybox.transform);
                        }
                        safetybox.transform.position = new Vector3(345.8221f, -442, 1397.913f);
                        safetybox.transform.rotation = new Quaternion(0, 0.8348f, 0, -0.5506f);
                    }
                    if (hullheal != null)
                    {
                        ship.HullHealSwitch.transform.SetParent(hullheal.transform);
                        (ship as PLOldWarsShip_Human).HullHealUI.transform.SetParent(hullheal.transform);
                        hullheal.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                        hullheal.transform.position = new Vector3(496.0247f, -404.446f, 1503.041f);
                        hullheal.transform.SetParent(newvault.transform);
                        hullheal.transform.rotation = new Quaternion(0, 0.9952f, 0, 0.0974f);
                    }
                    if (chair != null)
                    {
                        GameObject cloneChair = Object.Instantiate(chair, new Vector3(327.5073f, -443.3779f, 1788.547f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(cloneChair.transform);
                        cloneChair.layer = ship.InteriorStatic.layer;
                        cloneChair = Object.Instantiate(chair, new Vector3(327.9145f, -443.3779f, 1792.25f), new Quaternion(0, 1, 0, 0));
                        Object.DontDestroyOnLoad(cloneChair.transform);
                        cloneChair.layer = ship.InteriorStatic.layer;
                        cloneChair = Object.Instantiate(chair, new Vector3(327.0746f, -443.3779f, 1792.25f), new Quaternion(0, 1, 0, 0));
                        Object.DontDestroyOnLoad(cloneChair.transform);
                        cloneChair.layer = ship.InteriorStatic.layer;
                        ship.GetLiarsDice().transform.position = new Vector3(327, -441, 1790);
                        ship.LiarsDice_SitPositions[0].position = new Vector3(327.9145f, -443.3582f, 1792.25f);
                        ship.LiarsDice_SitPositions[0].rotation = new Quaternion(0, 1, 0, 0);
                        ship.LiarsDice_SitPositions[1].position = new Vector3(329.45f, -443.3582f, 1790.41f);
                        ship.LiarsDice_SitPositions[1].rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        ship.LiarsDice_SitPositions[2].position = new Vector3(327.5073f, -443.3582f, 1788.547f);
                        ship.LiarsDice_SitPositions[2].rotation = new Quaternion(0, 0, 0, 1);
                        ship.LiarsDice_SitPositions[3].position = new Vector3(325.72f, -443.3582f, 1790.43f);
                        ship.LiarsDice_SitPositions[3].rotation = new Quaternion(0, 0.7071f, 0, 0.7071f);
                        ship.LiarsDice_SitPositions[4].position = new Vector3(327.0746f, -443.3582f, 1792.25f);
                        ship.LiarsDice_SitPositions[4].rotation = new Quaternion(0, 1, 0, 0);
                    }
                    if (ejectswitch != null)
                    {
                        if (ejectlabel != null)
                        {
                            ejectlabel.transform.SetParent(ejectswitch.transform);
                        }
                        ejectswitch.transform.position = new Vector3(370.173f, -442.6216f, 1397.611f);
                        ejectswitch.transform.rotation = new Quaternion(-0.0003f, -0.5454f, 0.0002f, 0.8382f);
                        ejectswitch.transform.localScale = Vector3.one * 2.5f;
                    }
                    if (teldoor != null)
                    {
                        GameObject newTelDoor = Object.Instantiate(teldoor, new Vector3(375.1424f, -385.9102f, 1387.404f), new Quaternion(0, 0.3806f, 0, -0.9248f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(369.0363f, -443.5204f, 1404.2f), new Quaternion(0, 0.5421f, 0, -0.8403f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(374.9182f, -383.6057f, 1574.436f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(468.4048f, -400.4253f, 1486.02f), new Quaternion(0, 1, 0, 0));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(379.7382f, -385.9098f, 1380.838f), new Quaternion(0, -0.2217f, 0, 0.9751f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(394.0073f, - 400.8728f, 1704.814f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                    }
                    ship.MyAmmoRefills[0].transform.position = new Vector3(344.9436f, -383.6307f, 1609.502f);
                    ship.MyAmmoRefills[0].transform.rotation = new Quaternion(0, 0.7168f, 0, 0.6972f);
                    (ship.Spawners[0] as GameObject).transform.position = new Vector3(-2, -260, -324);
                    (ship.Spawners[1] as GameObject).transform.position = new Vector3(0, -261, -318.7f);
                    (ship.Spawners[2] as GameObject).transform.position = new Vector3(-1, -260, -322);
                    (ship.Spawners[3] as GameObject).transform.position = new Vector3(-2, -260, -325);
                    (ship.Spawners[4] as GameObject).transform.position = new Vector3(0, -260, -326);
                    (ship.Spawners[4] as GameObject).transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.ShipLogStation.transform.position = new Vector3(445.2327f, -430f, 1753);
                    ship.ShipLogStation.transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.GetLockers()[0].transform.position = new Vector3(406.1302f, -430, 1749.819f);
                    ship.GetLockers()[0].transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.GetLockers()[1].transform.position = new Vector3(406.1302f, -430, 1750.819f);
                    ship.GetLockers()[1].transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.GetLockers()[2].transform.position = new Vector3(406.1302f, -430, 1751.819f);
                    ship.GetLockers()[2].transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.GetLockers()[3].transform.position = new Vector3(406.1302f, -430, 1752.819f);
                    ship.GetLockers()[3].transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.GetLockers()[4].transform.position = new Vector3(406.1302f, -430, 1753.819f);
                    ship.GetLockers()[4].transform.rotation = new Quaternion(0, 0, 0, 1);
                    for (int i = 0; i < 14; i++)
                    {
                        ship.CargoBases[i].transform.position = new Vector3(281.2728f, -443.3417f, 1472.178f + (i * 2));
                        ship.CargoBases[i].transform.rotation = new Quaternion(0, 0.7082f, 0, 0.706f);
                    }
                    List<GameObject> cargo = new List<GameObject>();
                    cargo.AddRange(ship.CargoBases);
                    int index = 14;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 24; j++)
                        {
                            if (i == 0 && j == 0) j = 14;
                            GameObject cargoslot = Object.Instantiate(ship.CargoBases[0], new Vector3(281.2728f + (i * 4), -443.3417f, 1472.178f + (j * 2)), new Quaternion(0, 0.7082f, 0, 0.706f));
                            Object.DontDestroyOnLoad(cargoslot);
                            cargo.Add(cargoslot);
                            CargoObjectDisplay cargoDisplay = new CargoObjectDisplay();
                            cargoDisplay.RootObj = cargoslot;
                            cargoDisplay.DisplayedItem = null;
                            cargoDisplay.DisplayObj = null;
                            cargoDisplay.Index = index;
                            cargoDisplay.Hidden = false;
                            ship.CargoObjectDisplays.Add(cargoDisplay);
                            index++;
                        }
                    }
                    ship.CargoBases = cargo.ToArray();
                    ship.EngUpgradeUIRoot.transform.position = new Vector3(340.7982f, -384.2946f, 1387.454f);
                    ship.EngUpgradeUIRoot.transform.rotation = new Quaternion(0, 0.3768f, 0, -0.9263f);
                    ship.EngUpgradeUIWorldRoot.position = new Vector3(340.7982f, -384.2946f, 1387.454f);
                    ship.EngUpgradeUIWorldRoot.rotation = new Quaternion(0, 0.3768f, 0, -0.9263f);
                    ship.WeapUpgradeUIRoot.transform.position = new Vector3(358.1492f, -383.6829f, 1507.708f);
                    ship.WeapUpgradeUIRoot.transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.WeapUpgradeUIWorldRoot.transform.position = new Vector3(358.1492f, -383.6829f, 1507.708f);
                    ship.WeapUpgradeUIWorldRoot.transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.SalvageUIRoot.transform.position = new Vector3(336.5367f, -384.5613f, 1381.647f);
                    ship.SalvageUIRoot.transform.rotation = new Quaternion(0, 0.5292f, 0, -0.8485f);
                    ship.SalvageShipUIRoot.transform.position = new Vector3(336.0931f, -384.0558f, 1380.696f);
                    ship.SalvageShipUIRoot.transform.rotation = new Quaternion(0, 0.5292f, 0, -0.8485f);
                    PLTeleportationScreen tpscreen = null;
                    PLScientistSensorScreen sensorscreen = null;
                    PLScientistComputerScreen computerscreen = null;
                    PLScientistVirusScreen virusscreen = null;
                    PLCommsScreen commsScreen = null;
                    PLCaptainScreen statusscreen = null;
                    PLEngineerReactorScreen reactorscreen = null;
                    PLWarpDriveScreen warpscreen = null;
                    PLEngineerCoolantScreen coolantscreen = null;
                    PLEngineerAuxReactorScreen auxscreen = null;
                    PLUIPilotingScreen pilotscren = null;
                    PLClonedScreen clonedScreen = null;
                    PLMissileLauncherScreen misslescreen = null;
                    PLWeaponsNukeScreen nukescreen = null;
                    PLStartupScreen powerscreen = null;
                    foreach (PLUIScreen screen in ship.MyScreenBase.AllScreens)
                    {
                        if (screen is PLTeleportationScreen)
                        {
                            tpscreen = screen as PLTeleportationScreen;
                        }
                        else if (screen is PLScientistSensorScreen)
                        {
                            sensorscreen = screen as PLScientistSensorScreen;
                        }
                        else if (screen is PLScientistComputerScreen)
                        {
                            computerscreen = screen as PLScientistComputerScreen;
                        }
                        else if (screen is PLScientistVirusScreen)
                        {
                            virusscreen = screen as PLScientistVirusScreen;
                        }
                        else if (screen is PLCommsScreen)
                        {
                            commsScreen = screen as PLCommsScreen;
                        }
                        else if (screen is PLCaptainScreen)
                        {
                            statusscreen = screen as PLCaptainScreen;
                        }
                        else if (screen is PLEngineerReactorScreen)
                        {
                            reactorscreen = screen as PLEngineerReactorScreen;
                        }
                        else if (screen is PLWarpDriveScreen)
                        {
                            warpscreen = screen as PLWarpDriveScreen;
                        }
                        else if (screen is PLEngineerCoolantScreen)
                        {
                            coolantscreen = screen as PLEngineerCoolantScreen;
                        }
                        else if (screen is PLEngineerAuxReactorScreen)
                        {
                            auxscreen = screen as PLEngineerAuxReactorScreen;
                        }
                        else if (screen is PLUIPilotingScreen)
                        {
                            pilotscren = screen as PLUIPilotingScreen;
                        }
                        else if (screen is PLClonedScreen && screen.name == "ClonedScreen_Status")
                        {
                            clonedScreen = screen as PLClonedScreen;
                        }
                        else if (screen is PLMissileLauncherScreen)
                        {
                            misslescreen = screen as PLMissileLauncherScreen;
                        }
                        else if (screen is PLWeaponsNukeScreen)
                        {
                            nukescreen = screen as PLWeaponsNukeScreen;
                        }
                        else if (screen is PLStartupScreen)
                        {
                            powerscreen = screen as PLStartupScreen;
                        }
                        if (screen.transform.childCount > 2)
                        {
                            Transform fisical = screen.transform.GetChild(0);
                            Object.Destroy(fisical.gameObject);
                        }
                    }
                    if (tpscreen != null)
                    {
                        tpscreen.gameObject.transform.position = new Vector3(427.7254f, -429.5034f, 1729.931f);
                        tpscreen.gameObject.transform.rotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.3508f, -441.1491f, 1481.12f), new Quaternion(0, 0.9239f, 0, -0.3827f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = tpscreen;
                            GameObject teleport2 = Object.Instantiate(clonedScreen.gameObject, new Vector3(349.7148f, -441.5712f, 1663.779f), new Quaternion(0, 0.8434f, 0, -0.5373f));
                            Object.DontDestroyOnLoad(teleport2);
                            teleport2.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport2.GetComponent<PLClonedScreen>());
                            teleport2.GetComponent<PLClonedScreen>().MyTargetScreen = tpscreen;
                        }
                    }
                    if (sensorscreen != null)
                    {
                        sensorscreen.transform.position = new Vector3(-1.1564f, -260.2763f, -321.4911f);
                        sensorscreen.transform.rotation = new Quaternion(0, 0.998f, 0, 0.0635f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, -399.4763f, 1730.905f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = sensorscreen;
                        }
                    }
                    if (computerscreen != null)
                    {
                        computerscreen.transform.position = new Vector3(-0.1764f, -260.2763f, -321.4911f);
                        computerscreen.transform.rotation = new Quaternion(0, 0.9971f, 0, -0.0755f);
                        computerscreen.gameObject.name = "cloned computer";
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, -399.4763f, 1729.965f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = computerscreen;
                        }
                    }
                    if (virusscreen != null)
                    {
                        virusscreen.transform.position = new Vector3(0.8726f, -260.2763f, -321.88f);
                        virusscreen.transform.rotation = new Quaternion(0, 0.9632f, 0, -0.2686f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(336.5857f, -399.4763f, 1729.01f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = virusscreen;
                        }
                    }
                    if (commsScreen != null)
                    {
                        commsScreen.transform.position = new Vector3(-2.1074f, -260.276f, -321.7779f);
                        commsScreen.transform.rotation = new Quaternion(0, 0.9728f, 0, 0.2318f);
                    }
                    if (statusscreen != null)
                    {
                        statusscreen.transform.position = new Vector3(-3.0238f, -260.276f, -322.3781f);
                        statusscreen.transform.rotation = new Quaternion(0, 0.9065f, 0, 0.4222f);
                    }
                    if (reactorscreen != null)
                    {
                        reactorscreen.transform.position = new Vector3(-0.3147f, -260.276f, -327.1871f);
                        reactorscreen.transform.rotation = new Quaternion(0, 0.0776f, 0, -0.997f);
                        reactorscreen.gameObject.name = "cloned reactor";
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(361.3329f, -383.2109f, 1373.86f), new Quaternion(0, 0.3868f, 0, 0.9222f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = reactorscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.9873f, -416.4473f, 1386.166f), new Quaternion(0, 0f, 0, 1f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = reactorscreen;
                        }
                    }
                    if (warpscreen != null)
                    {
                        warpscreen.transform.position = new Vector3(-1.3783f, -260.276f, -327.1472f);
                        warpscreen.transform.rotation = new Quaternion(0, 0.1093f, 0, 0.994f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(356.2941f, -383.2109f, 1374.809f), new Quaternion(0, 0.1337f, 0, -0.991f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = warpscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(375.6855f, -416.4473f, 1368.2f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = warpscreen;
                        }
                    }
                    if (coolantscreen != null) //Also Adding modded screens here, because why not
                    {
                        coolantscreen.transform.position = new Vector3(0.6817f, -260.276f, -326.9217f);
                        coolantscreen.transform.rotation = new Quaternion(0, 0.2374f, 0, -0.9714f);
                        GameObject temperature = Object.Instantiate(coolantscreen.gameObject, new Vector3(379.5182f, -399.564f, 1731.114f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(temperature);
                        foreach (Transform transform in temperature.transform)
                        {
                            Object.DestroyImmediate(transform.gameObject);
                        }
                        Object.Destroy(temperature.GetComponent<PLEngineerCoolantScreen>());
                        PLTemperatureScreen temperatureScreen = temperature.AddComponent<PLTemperatureScreen>();
                        temperatureScreen.Engage();
                        Mod.moddedScreens.Add(temperature);

                        temperature = Object.Instantiate(coolantscreen.gameObject, new Vector3(353.223f, -383.2109f, 1371.455f), new Quaternion(0, 0.5991f, 0, -0.8007f));
                        Object.DontDestroyOnLoad(temperature);
                        foreach (Transform transform in temperature.transform)
                        {
                            Object.DestroyImmediate(transform.gameObject);
                        }
                        Object.Destroy(temperature.GetComponent<PLEngineerCoolantScreen>());
                        PLArmorBonusScreen armorScreen = temperature.AddComponent<PLArmorBonusScreen>();
                        armorScreen.Engage();
                        Mod.moddedScreens.Add(temperature);
                        //Object.Destroy(temperature.GetComponent<PLEngineerCoolantScreen>());
                        /*
                        GameObject test = Object.Instantiate(coolantscreen.gameObject, new Vector3(-9, -261, -323), new Quaternion(0, 0.3755f, 0, -0.9268f));
                        await Task.Delay(1000);
                        PLEngineerCoolantScreen testScreen = test.GetComponent<PLEngineerCoolantScreen>();
                        testScreen.FuelPanel.transform.position = new Vector3(0, 500);
                        testScreen.DistressPanel.transform.position = new Vector3(0, 500);
                        testScreen.FuelCountLabel.transform.position = new Vector3(0, 500);
                        */
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(358.9745f, -383.2109f, 1375.063f), new Quaternion(0, 0.1361f, 0, 0.9907f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = coolantscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(339.9966f, -416.4473f, 1368.19f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = coolantscreen;
                        }
                    }
                    if (auxscreen != null)
                    {
                        auxscreen.transform.position = new Vector3(-2.2474f, -260.276f, -326.8127f);
                        auxscreen.transform.rotation = new Quaternion(0, 0.2286f, 0, 0.9735f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(362.6146f, -383.2109f, 1371.575f), new Quaternion(0, 0.5941f, 0, 0.8044f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = auxscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.8f, -416.4473f, 1350.418f), new Quaternion(0, 1f, 0, 0f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = auxscreen;
                        }
                    }
                    if (pilotscren != null)
                    {
                        pilotscren.transform.position = new Vector3(-1.5436f, -260.6036f, -316.256f);
                    }
                    if (clonedScreen != null)
                    {
                        clonedScreen.transform.position = new Vector3(0.0273f, -260.6036f, -316.256f);
                        clonedScreen.transform.rotation = new Quaternion(0, 0.9239f, 0, -0.3827f);
                        GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(354.2722f, -383.2109f, 1373.794f), new Quaternion(0, 0.3755f, 0, -0.9268f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(356.739f, -382.3126f, 1572.901f), new Quaternion(0, 0.3f, 0, 0.954f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(358.1434f, -383.88f, 1689.191f), new Quaternion(0, 1f, 0, 0));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(438.5164f, -398.909f, 1702.425f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(277.7163f, -398.909f, 1702.425f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(256.0582f, -430, 1701.648f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(256.0582f, -430, 1575.247f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(459.8836f, -430, 1701.74f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(459.8836f, -430, 1575.247f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(427.1271f, -429.3854f, 1603.108f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(289.654f, -429.3854f, 1603.014f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(365.7347f, -442, 1648.112f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(358.1891f, -442, 1517.672f), new Quaternion(0, 1f, 0, 0f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(350.5453f, -442, 1588.259f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                        teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(294.8038f, -442, 1471.506f), new Quaternion(0, 0, 0, 1f));
                        Object.DontDestroyOnLoad(teleport1);
                        teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                        ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                    }
                    if (misslescreen != null)
                    {
                        misslescreen.transform.position = new Vector3(359.1238f, -382.3235f, 1572.909f);
                        misslescreen.transform.rotation = new Quaternion(0, 0.3827f, 0, -0.9239f);
                    }
                    if (nukescreen != null)
                    {
                        nukescreen.transform.position = new Vector3(371.3765f, -382.3674f, 1610.65f);
                        nukescreen.transform.rotation = new Quaternion(0, 0.9239f, 0, -0.3827f);
                    }
                    if (powerscreen != null)
                    {
                        powerscreen.transform.position = new Vector3(347.9655f, -384.222f, 1392.117f);
                        powerscreen.transform.rotation = new Quaternion(0, 0.9732f, 0, 0.2298f);
                    }
                    GameObject starmap = Object.Instantiate(Object.FindObjectOfType<PLWDParticleSystem>().gameObject, new Vector3(287.5f, -430, 1749.5f), Object.FindObjectOfType<PLWDParticleSystem>().transform.rotation);
                    starmap.layer = newinterior.layer;
                    Object.DontDestroyOnLoad(starmap);
                    starmap.transform.SetParent(newinterior.transform);
                    ship.DialogueChoiceBG.transform.position = new Vector3(0.7823f, -259, -321.5387f);
                    ship.DialogueChoiceBG.transform.rotation = new Quaternion(-0.1016f, 0.1994f, 0.0202f, 0.9744f);
                    ship.DialogueTextBG.transform.position = new Vector3(-2.7849f, -259, -321.5387f);
                    ship.DialogueTextBG.transform.rotation = new Quaternion(0.0861f, 0.2357f, 0.0244f, -0.9672f);
                    ship.MyTLI.AllTTIs[0].transform.position = new Vector3(425.1f, -430, 1729.7f);
                    GameObject newtp = Object.Instantiate(ship.MyTLI.AllTTIs[0].gameObject, new Vector3(355.8f, -441.5f, 1479.2f), ship.MyTLI.AllTTIs[0].transform.rotation);
                    GameObject newtp2 = Object.Instantiate(ship.MyTLI.AllTTIs[0].gameObject, new Vector3(347.5f, -442f, 1662.4f), ship.MyTLI.AllTTIs[0].transform.rotation);
                    Object.DontDestroyOnLoad(newtp);
                    Object.DontDestroyOnLoad(newtp2);
                    PLTeleportationTargetInstance[] newtargets = new PLTeleportationTargetInstance[3];
                    newtargets[0] = ship.MyTLI.AllTTIs[0];
                    newtargets[1] = newtp.GetComponent<PLTeleportationTargetInstance>();
                    newtargets[2] = newtp2.GetComponent<PLTeleportationTargetInstance>();
                    newtargets[0].TeleporterTargetName = "Capitan Room";
                    newtargets[1].TeleporterTargetName = "Reactor";
                    newtargets[2].TeleporterTargetName = "Bar";
                    ship.MyTLI.AllTTIs = newtargets;
                    ship.ExosuitVisualAssets[0].transform.position = new Vector3(388.613f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[0].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[1].transform.position = new Vector3(389.013f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[1].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[2].transform.position = new Vector3(389.413f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[2].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[3].transform.position = new Vector3(389.813f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[3].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.ExosuitVisualAssets[4].transform.position = new Vector3(390.213f, -400.052f, 1724.208f);
                    ship.ExosuitVisualAssets[4].transform.rotation = new Quaternion(0, 0.7377f, 0, -0.6752f);
                    ship.MyAtrium.transform.position = new Vector3(380.1347f, -399.7f, 1723f);
                    GameObject newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1727f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);
                    newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1738f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);
                    newatrium = Object.Instantiate(ship.MyAtrium.gameObject, new Vector3(380.1347f, -399.7f, 1742f), ship.MyAtrium.transform.rotation);
                    Object.DontDestroyOnLoad(newatrium.transform);
                    newatrium.transform.SetParent(newinterior.transform);

                    PLPickupObject pickup = newinterior.GetComponentInChildren<PLPickupObject>();
                    if (pickup != null)
                    {
                        GameObject ResearchItem = Object.Instantiate(pickup.gameObject, newinterior.transform);
                        ResearchItem.transform.position = new Vector3(369.4055f, - 406.6891f, 1724.177f);
                        OnWarpBase.ResearchItemPickups[0] = ResearchItem.GetComponent<PLPickupObject>();
                        Object.DontDestroyOnLoad(ResearchItem);

                        ResearchItem = Object.Instantiate(pickup.gameObject, newinterior.transform);
                        ResearchItem.transform.position = new Vector3(371.5657f, - 403.9288f, 1735.507f);
                        OnWarpBase.ResearchItemPickups[1] = ResearchItem.GetComponent<PLPickupObject>();
                        Object.DontDestroyOnLoad(ResearchItem);

                        ResearchItem = Object.Instantiate(pickup.gameObject, newinterior.transform);
                        ResearchItem.transform.position = new Vector3(367.3712f, - 409.4961f, 1741.433f);
                        OnWarpBase.ResearchItemPickups[2] = ResearchItem.GetComponent<PLPickupObject>();
                        Object.DontDestroyOnLoad(ResearchItem);

                        ResearchItem = Object.Instantiate(pickup.gameObject, newinterior.transform);
                        ResearchItem.transform.position = new Vector3(348.8619f, - 409.4961f, 1731.426f);
                        OnWarpBase.ResearchItemPickups[3] = ResearchItem.GetComponent<PLPickupObject>();
                        Object.DontDestroyOnLoad(ResearchItem);

                        ResearchItem = Object.Instantiate(pickup.gameObject, newinterior.transform);
                        ResearchItem.transform.position = new Vector3(344.6439f, - 403.8234f, 1737.68f);
                        OnWarpBase.ResearchItemPickups[4] = ResearchItem.GetComponent<PLPickupObject>();
                        Object.DontDestroyOnLoad(ResearchItem);

                        for (int i = 0; i < OnWarpBase.ResearchItemPickups.Length; i++)
                        {
                            OnWarpBase.ResearchItemPickups[i].PickedUp = true;
                            OnWarpBase.ResearchItemPickups[i].PickupID = 10000 + i;
                            OnWarpBase.ResearchItemPickups[i].name = "Flagship Research " + i;
                        }
                    }
                    //Special manual screens
                    GameObject droneUpgradeRoot = Object.Instantiate(ship.EngUpgradeUIWorldRoot.gameObject, new Vector3(468.5817f, -399, 1477.902f), Quaternion.Euler(new Vector3(0, 270, 0)));
                    PLPatrolBotUpgradeScreen patrolUpgrade = droneUpgradeRoot.AddComponent<PLPatrolBotUpgradeScreen>();
                    patrolUpgrade.setValues(droneUpgradeRoot.transform, ship.worldUiCanvas, ship);
                    patrolUpgrade.Assemble();
                    Object.DontDestroyOnLoad(droneUpgradeRoot);

                    droneUpgradeRoot = Object.Instantiate(ship.EngUpgradeUIWorldRoot.gameObject, new Vector3(358.9247f, -436.23f, 1622.292f), Quaternion.Euler(new Vector3(0, 180, 0)));
                    PLAutoRepairScreen repairUpgrade = droneUpgradeRoot.AddComponent<PLAutoRepairScreen>();
                    repairUpgrade.setValues(repairUpgrade.transform, ship.worldUiCanvas, ship);
                    repairUpgrade.Assemble();
                    Object.DontDestroyOnLoad(droneUpgradeRoot);

                    droneUpgradeRoot = Object.Instantiate(ship.EngUpgradeUIWorldRoot.gameObject, new Vector3(-12.7774f, - 259.9289f, - 337.9999f), Quaternion.Euler(new Vector3(0, 180, 0)));
                    PLFighterScreen fighterControl = droneUpgradeRoot.AddComponent<PLFighterScreen>();
                    fighterControl.setValues(fighterControl.transform, ship.worldUiCanvas, ship);
                    fighterControl.Assemble();
                    Object.DontDestroyOnLoad(droneUpgradeRoot);
                    /*
                    droneUpgradeRoot = Object.Instantiate(ship.EngUpgradeUIWorldRoot.gameObject, new Vector3(0.7774f, -259.9289f, -337.9999f), Quaternion.Euler(new Vector3(0, 180, 0)));
                    PLCyberAttackScreen cyberScreen = droneUpgradeRoot.AddComponent<PLCyberAttackScreen>();
                    cyberScreen.setValues(cyberScreen.transform, ship.worldUiCanvas, ship);
                    cyberScreen.Assemble();
                    Object.DontDestroyOnLoad(droneUpgradeRoot);
                    */
                    //ship.InteriorStatic = interior;
                    if (foxplush != null)
                    {
                        GameObject newfox = Object.Instantiate(foxplush, new Vector3(330.5399f, -443.1522f, 1735.403f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one;
                        newfox = Object.Instantiate(foxplush, new Vector3(266.4287f, -431.316f, 1672.744f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.3f;
                        newfox = Object.Instantiate(foxplush, new Vector3(449.5674f, -430.5944f, 1666.002f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.3f;
                        newfox = Object.Instantiate(foxplush, new Vector3(319.6508f, -434.2854f, 1486.746f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.5f;
                    }
                    if (PLNetworkManager.Instance.MyLocalPawn != null) PLNetworkManager.Instance.MyLocalPawn.transform.position = (ship.Spawners[PLNetworkManager.Instance.LocalPlayer.GetClassID()] as GameObject).transform.position;
                    ship.ReactorInstance.transform.position = new Vector3(357.8f, -425.7683f, 1368.4f);
                    ship.ReactorInstance.LightMeltdownEnd = new Vector3(0, -12, 0);
                    //Create camera system
                    //Bridge teleporters
                    int cameraResolution = 344;
                    GameObject cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(-8.6182f, -255.2728f, - 437);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(40.1343f, 38.2f, 18.2477f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    Camera cam = cameraObj.AddComponent<Camera>();
                    GameObject cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(13.3449f, - 260.1196f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    RenderTexture texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        cam.fieldOfView = 120;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Captain teleporter
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(422.1003f, - 424.9404f, 1743.08f);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(347.1234f, 159.8055f, 12.9391f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(13.3449f, -259.1556f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Bridge kitchen
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(12f, - 258.7097f, - 349.2461f);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(344.4979f, 144.1035f, 10.4296f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(12.2467f, -260.1196f, -338.03f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Prision
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(332, - 440.9998f, 1745);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(349.5706f, 150.6783f, 7.1573f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(12.2466f, -259.1556f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Engineering Room
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(370, - 368, 1396);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(2.6799f, 202.9273f, 347.157f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(13.3449f, - 258.2174f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        cam.fieldOfView = 90;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Reactor room
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(384, - 414.9999f, 1397);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(2.68f, 217.4727f, 342.0661f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(12.2798f, - 258.2174f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        cam.fieldOfView = 90;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //atrium
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(397, - 389, 1705);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(44.8616f, 328.018f, 348.6111f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(11.1736f, - 259.1556f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //science lab
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(316.9096f, - 388.9924f, 1704.998f);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(42.3168f, 43.3274f, 22.4293f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(11.1736f, - 260.1196f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Weapons main bay
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(380, - 379.9999f, 1585);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(16.8621f, 237.2904f, 333.5201f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(11.1736f, - 258.1938f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        cam.fieldOfView = 90;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //left weapons wing
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(397.0001f, - 379, 1566);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(349.226f, 196.2365f, 350.611f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(10.1406f, -258.1938f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //right weapons wing
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(317.9306f, - 378.9911f, 1565.998f);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(349.226f, 168.2366f, 0.7929f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(9.0675f, -258.1938f, -338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //left crew wing
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(472.9999f, - 418, 1734);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(349.226f, 201.6911f, 350.6112f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(10.1073f, - 259.1466f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //right crew wing
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(242.5644f, - 417.9845f, 1733.999f);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(347.0442f, 173.6911f, 1.8838f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(9.0493f, - 259.1466f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //Cargo room
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(280.0001f, - 422, 1471);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(48.1356f, 61.3493f, 31.3384f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(10.1073f, - 260.1286f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                    //bar teleporter
                    cameraObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraObj.name = "cameraObj";
                    cameraObj.transform.SetParent(newbridge.transform);
                    cameraObj.transform.position = new Vector3(366, - 424.9998f, 1685);
                    cameraObj.transform.localRotation = Quaternion.Euler(new Vector3(9.9539f, 203.8947f, 350.2475f));
                    cameraObj.layer = newbridge.layer;
                    camerasRenders.Add(cameraObj.GetComponent<MeshRenderer>());
                    cameraObj.GetComponent<BoxCollider>().enabled = false;
                    cam = cameraObj.AddComponent<Camera>();
                    cameraView = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cameraView.name = "cameraView";
                    cameraView.transform.SetParent(newbridge.transform);
                    cameraView.transform.position = new Vector3(9.0529f, - 260.1286f, - 338.0208f);
                    cameraView.transform.localScale = new Vector3(0.01f, 0.175f, 0.1836f);
                    cameraView.transform.localRotation = Quaternion.Euler(new Vector3(3.3363f, 271.0923f, 334.5788f));
                    cameraView.layer = newbridge.layer;
                    texture = new RenderTexture(cameraResolution, cameraResolution, 16, RenderTextureFormat.ARGB32);
                    texture.name = "CameraText";
                    texture.Create();
                    if (texture.IsCreated())
                    {
                        cam.targetTexture = texture;
                        MeshRenderer camMesh = cameraView.GetComponent<MeshRenderer>();
                        camMesh.material.SetTexture("_MainTex", texture);
                    }
                }
            }
            ship.IsGodModeActive = false;
            ship.MyStats.Mass = 4620;
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, ship.CargoBases.Length);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 12);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 9);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 8);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 4);
            ship.MyStats.SetSlot_IsLocked(ESlotType.E_COMP_HULL, false);
            ship.FactionID = 2;
            ship.SensorDishCollectingScrapRange = 1800;
            ship.InteriorShipLights.RemoveAll((Light light) => light == null);
            float[] powerPercent = new float[17];
            powerPercent.AddRangeToArray(ship.PowerPercent_SysIntConduits);
            powerPercent[16] = 0;
            ship.PowerPercent_SysIntConduits = powerPercent;
            for (int i = 0; i < ship.PowerPercent_SysIntConduits.Length; i++)
            {
                ship.PowerPercent_SysIntConduits[i] = 1f;
            }
            powerPercent = new float[17];
            powerPercent.AddRangeToArray(ship.m_SysIntConduit_LocalChangeTime);
            powerPercent[16] = 0;
            ship.m_SysIntConduit_LocalChangeTime = powerPercent;
            powerPercent = new float[17];
            powerPercent.AddRangeToArray(ship.m_SysIntConduit_LocalChangeValue);
            powerPercent[16] = 0;
            ship.m_SysIntConduit_LocalChangeValue = powerPercent;
            if (ship.MyHull != null && ship.MyHull.Level < 9)
            {
                ship.MyHull.Level = 9;
                ship.MyHull.Current = 3920;
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_THRUSTER).Count == 2 && PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < 7; i++) ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(9, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_THRUSTER);
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_INERTIA_THRUSTER).Count == 1 && PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < 7; i++) ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(25, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_INERTIA_THRUSTER);
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_MANEUVER_THRUSTER).Count == 1 && PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < 5; i++) ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(26, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_MANEUVER_THRUSTER);
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_TURRET).Count == 2 && PhotonNetwork.isMasterClient)
            {
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(10, 9, 1, 0, 12), null), -1, ESlotType.E_COMP_TURRET);
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(10, 11, 1, 0, 12), null), -1, ESlotType.E_COMP_TURRET);
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(10, 13, 1, 0, 12), null), -1, ESlotType.E_COMP_TURRET);
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(10, 6, 1, 0, 12), null), -1, ESlotType.E_COMP_TURRET);
            }
            if(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MAINTURRET, false).Count > 0 && PhotonNetwork.isMasterClient) 
            {
                int mainTurretLevel = ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MAINTURRET, false)[0].Level;
                ship.MyStats.RemoveShipComponent(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MAINTURRET, false)[0]);
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(11, PulsarModLoader.Content.Components.MegaTurret.MegaTurretModManager.Instance.GetMegaTurretIDFromName("TheFlagship_FlagShipMainTurret"), mainTurretLevel, 0, 12), null), -1, ESlotType.E_COMP_MAINTURRET);
            }
            else if(PhotonNetwork.isMasterClient)
            {
                ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(11, PulsarModLoader.Content.Components.MegaTurret.MegaTurretModManager.Instance.GetMegaTurretIDFromName("TheFlagship_FlagShipMainTurret"), 0, 0, 12), null), -1, ESlotType.E_COMP_MAINTURRET);
            }
            ship.MyStats.SetSlot_IsLocked(ESlotType.E_COMP_MAINTURRET, true);
            if (PLServer.Instance.CrewFactionID == -1)
            {
                PLServer.Instance.RepLevels[2] = 5;
                PLServer.Instance.CrewFactionID = 2;
            }
            ship.EngineeringSystem.MaxHealth = 100;
            ship.EngineeringSystem.Health = 100;
            ship.WeaponsSystem.MaxHealth = 100;
            ship.WeaponsSystem.Health = 100;
            ship.ComputerSystem.MaxHealth = 100;
            ship.ComputerSystem.Health = 100;
            ship.LifeSupportSystem.MaxHealth = 100;
            ship.LifeSupportSystem.Health = 100;
            ship.BridgeCameraTransform.position = new Vector3(-6.5955f, -258.5034f, -328.7074f);
            ship.BridgeCameraTransform.rotation = Quaternion.Euler(new Vector3(22.026f, 34.7839f, -0.0017f));
            foreach (MeshRenderer render in ship.InteriorStatic.GetComponentsInChildren<MeshRenderer>(true))
            {
                render.enabled = true;
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
            
            AsyncOperation des = SceneManager.UnloadSceneAsync(Estate);
            AsyncOperation des1 = SceneManager.UnloadSceneAsync(Flagship);
            AsyncOperation des2 = SceneManager.UnloadSceneAsync(WDHub);
            AsyncOperation des3 = SceneManager.UnloadSceneAsync(FluffyMansion);
            while (!des.isDone || !des1.isDone || !des2.isDone || !des3.isDone)
            {
                await Task.Yield();
            }

            PLNetworkManager.Instance.CurrentGame = Object.FindObjectOfType<PLGame>();
            if (PLNetworkManager.Instance.CurrentGame == null) PLNetworkManager.Instance.CurrentGame = Object.FindObjectOfType<PLGamePlanet>();
            await Task.Delay(15 * 1000);
            foreach (MeshRenderer render in camerasRenders)
            {
                render.enabled = false;
            }
            if (PLEncounterManager.Instance.PlayerShip != null)
            {
                GameObject newinterior = PLEncounterManager.Instance.PlayerShip.InteriorStatic;
                Matrix4x4 oldMat = Matrix4x4.TRS(newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.localScale) * Matrix4x4.TRS(Vector3.zero, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.rotation, Vector3.one);
                Matrix4x4 newMat = Matrix4x4.TRS(newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position + new Vector3(400, -400, 1500), Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.localScale) * Matrix4x4.TRS(Vector3.zero, newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.rotation, Vector3.one);
                foreach (uint inID2 in newinterior.GetComponentInChildren<PLPlanetAStarConnection>().navGraphIDs)
                {
                    RecastGraph graph = PLPathfinder.GetInstance().GetPGEFromID(inID2).Graph;
                    graph.forcedBoundsCenter += newinterior.GetComponentInChildren<PLPlanetAStarConnection>().transform.position - newinterior.GetComponentInChildren<PLPlanetAStarConnection>().SavedLoc;
                    graph.RelocateNodes(newMat * oldMat.inverse);
                }

            }
            if (PhotonNetwork.isMasterClient)
            {
                Mod.BridgePathID = PLPathfinder.GetInstance().GetPGEforTLIAndPosition(ship.MyTLI, new Vector3(-14, -261, -362)).ID;
                List<PLCombatTarget> drones = new List<PLCombatTarget>();
                Vector3[] positions = new Vector3[]
                {
                new Vector3(0, -261, -411), 
                new Vector3(428, -430, 1751),
                new Vector3(321, -382, 1453),
                new Vector3(357, -442, 1541),
                new Vector3(395, -382, 1588),
                new Vector3(287, -430, 1732),
                new Vector3(357, -384, 1355),
                };
                foreach (Vector3 vector in positions)
                {
                    PLCombatTarget component = PhotonNetwork.Instantiate("NetworkPrefabs/BoardingBot", vector, Quaternion.identity, 0, null).GetComponent<PLCombatTarget>();
                    if (component != null)
                    {
                        drones.Add(component);
                    }
                }
                await Task.Delay(100 * drones.Count);
                foreach (PLCombatTarget target in drones)
                {
                    if (target != null && target.gameObject != null)
                    {
                        Object.DontDestroyOnLoad(target.gameObject);
                        target.MyCurrentTLI = PLEncounterManager.Instance.PlayerShip.MyTLI;
                        target.CurrentShip = PLEncounterManager.Instance.PlayerShip;
                        target.name += " (frienddrone)";
                        PLBoardingBot drone = (PLBoardingBot)target;
                        if (drone != null)
                        {
                            target.StopAllCoroutines();
                            target.StartCoroutine(PatrolBotUpdate.PathRoutine(drone));
                            foreach (Light light in drone.MyLights)
                            {
                                if (light != null)
                                {
                                    light.color = Color.green;
                                }
                            }
                        }
                        drone.Heal(-500);
                    }
                }
            }
            ship.InteriorRenderers.RemoveAll((MeshRenderer render) => render == null);
            foreach (Light light in ship.InteriorShipLights)
            {
                try
                {
                    if (light.gameObject != null)
                    {
                        light.gameObject.SetActive(true);
                    }
                }
                catch { }
            }
            PLPathfinderGraphEntity oldPath = PLPathfinder.GetInstance().GetPGEforShip(ship);
            if (oldPath != null) oldPath.TLI = null;
            List<PLUIScreen> screensfordeletion = new List<PLUIScreen>();
            foreach(PLUIScreen screen in UnityEngine.Object.FindObjectsOfType<PLUIScreen>(true)) 
            {
                if(screen.MyScreenHubBase != null && screen.MyRootPanel == null && !(screen is PLClonedScreen)) 
                {
                    screensfordeletion.Add(screen);
                }
                if(screen.MyScreenHubBase.OptionalShipInfo == PLEncounterManager.Instance.PlayerShip && screen.transform.position.x < 200 && screen.transform.position.y < -300) 
                {
                    screensfordeletion.Add(screen);
                }
            }
            for(int i = screensfordeletion.Count -1; i >= 0; i--) 
            {
                screensfordeletion[i].MyScreenHubBase.AllScreens.Remove(screensfordeletion[i]);
                Object.Destroy(screensfordeletion[i]);
            }
            
            PulsarModLoader.Utilities.Messaging.Notification("Assembly Complete!");
        }
    }
    [HarmonyPatch(typeof(PLWarpGuardian),"Update")]
    class GuardianBeamFix 
    {
        public static float BeamMultiplier = 8f;
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions)
        {
            List<CodeInstruction> instructionsList = Instructions.ToList();
            instructionsList[1038].operand = AccessTools.Field(typeof(GuardianBeamFix), "BeamMultiplier");
            instructionsList[1038].opcode = OpCodes.Ldsfld;
            return instructionsList.AsEnumerable();
        }

        static void Postfix() 
        {
            BeamMultiplier = (Command.shipAssembled ? 0f : 8f);
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
