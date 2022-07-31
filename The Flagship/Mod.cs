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
namespace The_Flagship
{
    /*
    TODO
    fix AI pathfind(maybe)
     */
    public class Mod : PulsarMod
    {
        public static bool AutoAssemble = false;
        public static ParticleSystem reactorEffect = null;
        public override string Version => "Beta 4.2";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Adds the flagship as a playable ship.";

        public override string Name => "The Flagship";

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.theflagship";
        }
    }
    [HarmonyPatch(typeof(PLOldWarsShip_Human), "GetShipTypeName")]
    class ShipType 
    {
        static void Postfix(PLOldWarsShip_Human __instance, ref string __result) 
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled) __result = "Flagship";
        }
    }
    [HarmonyPatch(typeof(PLOldWarsShip_Human), "GetShipShortDesc")]
    class ShipDesc
    {
        static void Postfix(PLOldWarsShip_Human __instance, ref string __result)
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled) __result = "Restored Capital Class Vessel";
        }
    }
    [HarmonyPatch(typeof(PLOldWarsShip_Human), "GetShipAttributes")]
    class ShipAtributes
    {
        static void Postfix(PLOldWarsShip_Human __instance,ref string __result)
        {
            if(__instance.GetIsPlayerShip() && Command.shipAssembled)__result = "Nnockback Resistance\n+100% Reactor Output\n<color=red>Cannot use repair stations or warp gates</color>";
        }
    }
    [HarmonyPatch(typeof(PLShipInfo), "ShipFinalCalculateStats")]
    class ShipStats
    {
        static void Postfix(PLShipInfo __instance, ref PLShipStats inStats)
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN) 
            {
                inStats.ReactorOutputFactor *= 2;
            }
        }
    }
    [HarmonyPatch(typeof(PLMegaTurret),"ChargeComplete")]
    class Knockback 
    {
        /*
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions) //Makes the boss attack more fequently
        {
            List<CodeInstruction> instructionsList = Instructions.ToList();
            instructionsList[406].operand = 2f;
            instructionsList[423].operand = 4f;
            return instructionsList.AsEnumerable();
        }
        */
        static void Postfix(PLMegaTurret __instance, Vector3 dir) 
        {
            if(__instance.ShipStats.Ship.GetIsPlayerShip() && Command.shipAssembled) 
            {
                __instance.ShipStats.Ship.Exterior.GetComponent<Rigidbody>().AddForceAtPosition(1200f * dir * __instance.m_KickbackForceMultiplier, __instance.TurretInstance.transform.position, ForceMode.Impulse);
            }
        }
    }
    [HarmonyPatch(typeof(PLMegaTurret_Proj), "Fire")]
    class Knockback2
    {
        static void Postfix(PLMegaTurret __instance, Vector3 dir)
        {
            if (__instance.ShipStats.Ship.GetIsPlayerShip() && Command.shipAssembled)
            {
                __instance.ShipStats.Ship.Exterior.GetComponent<Rigidbody>().AddForceAtPosition(1200f * dir * __instance.m_KickbackForceMultiplier, __instance.TurretInstance.transform.position, ForceMode.Impulse);
            }
        }
    }
    [HarmonyPatch(typeof(PLMissle), "Explode")]
    class MissileKnockback
    {
        static void Postfix(PLMissle __instance, Vector3 pos)
        {
            if (Command.shipAssembled && PLEncounterManager.Instance.PlayerShip != null && PLEncounterManager.Instance.PlayerShip.ExteriorRigidbody != null) 
            {
                PLEncounterManager.Instance.PlayerShip.ExteriorRigidbody.AddExplosionForce(__instance.MaxDamage * -40f, pos, __instance.DmgRadius * 2f);
            } 
        }
    }
    [HarmonyPatch(typeof(PLShipControl), "OnCollisionEnter")]
    class Collisiokknockback 
    {
        static void Postfix(PLShipControl __instance, Collision collision) 
        {
            if(Command.shipAssembled && __instance.ShipInfo != null && __instance.ShipInfo.GetIsPlayerShip() && __instance.ShipInfo.ShipTypeID == EShipType.OLDWARS_HUMAN) 
            {
                __instance.ShipInfo.ExteriorRigidbody.AddTorque(collision.impulse *-1, ForceMode.Force);
                __instance.ShipInfo.ExteriorRigidbody.AddForce(collision.impulse * -1, ForceMode.Force);
            }
        }
    }
    [HarmonyPatch(typeof(PLShipControl), "FixedUpdate")]
    class Inertia
    {
        /*
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions) //Makes the boss attack more fequently
        {
            List<CodeInstruction> instructionsList = Instructions.ToList();
            instructionsList[406].operand = 2f;
            instructionsList[423].operand = 4f;
            return instructionsList.AsEnumerable();
        }
        */
        static void Postfix(PLShipControl __instance)
        {
            if(__instance.ShipInfo.GetIsPlayerShip() && Command.shipAssembled) 
            {
                __instance._rigidbody.AddTorque(__instance.InputTorque * __instance.RotationSpeed * (__instance.IsBoosting ? 1.32f : 1f) * -0.9f);
            }
        }
    }
    [HarmonyPatch(typeof(PLSpaceScrap),"Update")]
    class ScrapCollecting 
    {
        static void Postfix(PLSpaceScrap __instance) 
        {
            if (Command.shipAssembled && PLEncounterManager.Instance.PlayerShip != null && PLEncounterManager.Instance.PlayerShip.ExteriorTransformCached != null && PLEncounterManager.Instance.PlayerShip.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                float sqrMagnitude = (PLEncounterManager.Instance.PlayerShip.ExteriorTransformCached.position - __instance.transform.position).sqrMagnitude;
                if (PhotonNetwork.isMasterClient && sqrMagnitude < 45000f && !__instance.Collected)
                {
                    __instance.OnCollect();
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLUIPawnAppearanceMenu), "IsLocalPawnNearAppearanceStation")]
    class BarberInShip 
    {
        static void Postfix(PLUIPawnAppearanceMenu __instance, ref bool __result) 
        {
            if (PLNetworkManager.Instance.MyLocalPawn != null && PLNetworkManager.Instance.MyLocalPawn.CurrentShip == PLEncounterManager.Instance.PlayerShip && Command.shipAssembled)
            {
                foreach (PLAppearanceStation plappearanceStation in PLGameStatic.Instance.m_AppearanceStations)
                {
                    if (plappearanceStation != null && Vector3.SqrMagnitude(plappearanceStation.transform.position - PLNetworkManager.Instance.MyLocalPawn.transform.position) < 25f)
                    {
                        __result = true;
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLDoor), "Open")]
    class DoorSoundOpen 
    {
        static void Prefix(PLDoor __instance) 
        {
            if (PLEncounterManager.Instance.PlayerShip == null) return;
            if(!__instance.IsOpen && __instance.MyInterior == null && __instance.gameObject.layer == PLEncounterManager.Instance.PlayerShip.InteriorStatic.layer && PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.CurrentShip == PLEncounterManager.Instance.PlayerShip) 
            {
                if (__instance.TheEstateDoorSFX)
                {
                    PLMusic.PostEvent("play_sx_station_estate_door_open", __instance.gameObject);
                    return;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLDoor), "Close")]
    class DoorSoundClose
    {
        static void Prefix(PLDoor __instance)
        {
            if (PLEncounterManager.Instance.PlayerShip == null) return;
            if (__instance.IsOpen && __instance.MyInterior == null && __instance.gameObject.layer == PLEncounterManager.Instance.PlayerShip.InteriorStatic.layer && PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.CurrentShip == PLEncounterManager.Instance.PlayerShip)
            {
                if (__instance.TheEstateDoorSFX)
                {
                    PLMusic.PostEvent("play_sx_station_estate_door_close", __instance.gameObject);
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PLGlobal), "EnterNewGame")]
    class OnJoin
    {
        static void Prefix()
        {
            Command.shipAssembled = false;
            Command.playersArrested = new int[10] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            Command.prisionCells = new GameObject[10];
            if (PhotonNetwork.isMasterClient)
            {
                string savedoptions = PLXMLOptionsIO.Instance.CurrentOptions.GetStringValue("flagship");
                if (savedoptions != string.Empty)
                {
                    Mod.AutoAssemble = bool.Parse(savedoptions);
                }
                if (Mod.AutoAssemble)
                {
                    AutoAssemble();
                }
            }
            else
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.sendRPC", PhotonNetwork.masterClient, new object[0]);
            }
        }
        static async void AutoAssemble()
        {
            while (PLEncounterManager.Instance == null || PLEncounterManager.Instance.PlayerShip == null)
            {
                await Task.Yield();
            }
            if (PLEncounterManager.Instance.PlayerShip.ShipTypeID == EShipType.OLDWARS_HUMAN && !Command.shipAssembled)
            {
                Command.shipAssembled = true;
                Command.FabricateFlagship();
            }
        }
    }
    public class sendRPC : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (Command.shipAssembled) SendRPC("pokegustavo.theflagship", "The_Flagship.RPCReciever", sender.sender, new object[0]);
        }
    }
    public class RPCReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                Command.shipAssembled = true;
                Command.FabricateFlagship();
            }
        }
    }
    public class prisionRPC : ModMessage 
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
                Command.playersArrested = (int[])arguments[0];
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
            return new string[][] { new string[] { "assemble", "autoassemble","prision" } };
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
                        shipAssembled = true;
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
                if(separatedArguments[0] == Arguments()[0][2]) 
                {
                    if (!shipAssembled) 
                    {
                        PulsarModLoader.Utilities.Messaging.Notification("Ship needs to be assembled!");
                    }
                    else if(separatedArguments.Length > 1) 
                    {
                        PLPlayer targetPlayer = PulsarModLoader.Utilities.HelperMethods.GetPlayerFromPlayerName(separatedArguments[1]);
                        int targetPlayerID;
                        if(targetPlayer == null && int.TryParse(separatedArguments[1],out targetPlayerID)) 
                        {
                            targetPlayer = PulsarModLoader.Utilities.HelperMethods.GetPlayerFromPlayerID(targetPlayerID);
                        }
                        if(targetPlayer != null) 
                        {
                            if(targetPlayer.TeamID != 0) 
                            {
                                PulsarModLoader.Utilities.Messaging.Notification("Player is not part of your crew!");
                                return;
                            }
                            for(int i = 0; i < 10; i++) 
                            {
                                if(playersArrested[i] == -1) 
                                {
                                    playersArrested[i] = targetPlayer.GetPlayerID();
                                    break;
                                }
                                else if(playersArrested[i] == targetPlayer.GetPlayerID()) 
                                {
                                    playersArrested[i] = -1;
                                    break;
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
                || transform.gameObject.name.ToLower().Contains("cargocrate_02-2"))
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
                    newexterior.GetComponent<Rigidbody>().angularDrag = 1.1f;
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
                    ship.MainTurretPoint.transform.localScale = Vector3.one * 10f;
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
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(0.9832f, 63.7453f, 12.822f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(-193.6323f, - 28.8545f, - 608.6761f);
                    newturretpoint.transform.localRotation = new Quaternion(0, 0, 0.7071f, 0.7071f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    newturretpoint = Object.Instantiate(ship.RegularTurretPoints[0].gameObject, newexterior.transform);
                    newturretpoint.transform.localPosition = new Vector3(193.2294f, -28.8545f, -608.6761f);
                    newturretpoint.transform.localRotation = new Quaternion(0, 0, 0.7071f, -0.7071f);
                    Object.DontDestroyOnLoad(newturretpoint);
                    allturretpoints.Add(newturretpoint.transform);
                    ship.RegularTurretPoints = allturretpoints.ToArray();
                    ship.CurrentTurretControllerPlayerID = new int[7] { -1, -1, -1, -1, -1, -1,-1 };
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
                    else if(gameObject.name == "TheBarber")
                    {
                        barber = gameObject;
                    }
                    else if(gameObject.name == "NeuralRewriter") 
                    {
                        neural = gameObject;
                    }
                    else if(gameObject.name == "LandDrone_Idle") 
                    {
                        landdrone = gameObject;
                    }
                    else if(gameObject.name == "Quad" && gameObject.scene == WDHub) 
                    {
                        blackbox = gameObject;
                    }
                    else if(gameObject.name == "Toy_Fox") 
                    {
                        foxplush = gameObject;
                    }
                    else if(gameObject.name == "Walkway") 
                    {
                        walkway = gameObject;
                    }
                    if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null && reactorroom != null && copydoor != null
                        && smallturret1 != null && smallturret2 != null && mainturret != null && weaponssys != null && nukeswitch1 != null && nukeswitch2 != null && nukecore != null && lifesys != null
                        && sciencesys != null && fuelboard != null && fueldecal != null && allLights.Count > 0 && enginesys != null && switchboard != null && powerswitches[0] != null
                        && powerswitches[1] != null && powerswitches[2] != null && hullheal != null && chair != null && ejectswitch != null && ejectlabel != null && safetyswitch != null && safetybox != null
                        && safetylabel != null && teldoor != null && barber != null && neural != null && landdrone != null && blackbox !=null && foxplush != null && walkway != null) break;
                }
                if (interior != null && bridge != null && rightwing != null && rightwingDeco != null && vault != null && vaultDeco != null && engineering != null)
                {
                    foreach(PLDoor door in ship.InteriorDynamic.GetComponentsInChildren<PLDoor>()) 
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
                    GameObject newbarber = Object.Instantiate(barber, new Vector3(369f, - 443.3361f, 1497), new Quaternion(0, -0.0376f, 0, 0.9993f));
                    Object.DontDestroyOnLoad(newbarber);
                    newbarber.transform.SetParent(newinterior.transform);
                    PLGameStatic.Instance.m_AppearanceStations.Add(newbarber.GetComponentInChildren<PLAppearanceStation>());
                    GameObject newneural = Object.Instantiate(neural, new Vector3(370, - 443.3894f, 1551.893f), neural.transform.rotation);
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
                        GameObject newEngineDoor = Object.Instantiate(copydoor, oldEngineDoor.transform.position, oldEngineDoor.transform.rotation);
                        newEngineDoor.transform.SetParent(newinterior.transform);
                        oldEngineDoor.transform.position += new Vector3(0, 20, 0);
                        GameObject newCaptainDoor = Object.Instantiate(copydoor, new Vector3(372.6432f, -440.1014f, 1670.436f), new Quaternion(0, 0.7071f, 0, -0.7071f));
                        newCaptainDoor.transform.SetParent(newinterior.transform);

                    }
                    if(walkway != null) 
                    {
                        GameObject newwalkway = Object.Instantiate(walkway, new Vector3(396.2569f, -383.5245f, 1519.001f), walkway.transform.rotation);
                        Object.DontDestroyOnLoad(newwalkway);
                        newwalkway.GetComponentInChildren<PLPushVolume>().MyTLI = ship.MyTLI;
                        newwalkway.transform.SetParent(newinterior.transform);
                        newwalkway = Object.Instantiate(walkway, new Vector3(320.078f, -383.5245f, 1519.001f), new Quaternion(0,1,0,0));
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
                    newreactor.transform.GetChild(7).gameObject.SetActive(true);
                    Mod.reactorEffect = newreactor.transform.GetComponentInChildren<ParticleSystem>(true);
                    Mod.reactorEffect.startColor = new Color(0.3835f, 0, 0.6f, 1);
                    Mod.reactorEffect.startSize = 1f;
                    Mod.reactorEffect.gameObject.SetActive(true);
                    Object.DontDestroyOnLoad(Mod.reactorEffect.gameObject);
                    foreach (Transform transform in newreactor.transform)
                    {
                        PulsarModLoader.Utilities.Logger.Info("Child name: " + transform.gameObject.name);
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
                    foreach(Transform transform in newinterior.transform.GetChild(5)) 
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
                                if(transform.gameObject.name.Contains("(" + i + ")")) 
                                {
                                    prisionCells[i] = transform.gameObject;
                                    break;
                                }
                            }
                        }
                    }
                    GameObject Abyssdeathobject = Object.Instantiate(blackbox);
                    PLKillVolume abyssdeath = Abyssdeathobject.AddComponent<PLKillVolume>();
                    Abyssdeathobject.transform.position = new Vector3(447,-501,1514);
                    abyssdeath.Dimensions = new Vector3(200,5,200);
                    Object.DontDestroyOnLoad(Abyssdeathobject);
                    Abyssdeathobject.transform.SetParent(newinterior.transform);
                    foreach(PLAmbientSFXControl sfx in newinterior.GetComponentsInChildren<PLAmbientSFXControl>()) 
                    {
                        if(sfx.Event.ToLower().Contains("infected")) 
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
                        CapitanToBridge.transform.rotation = new Quaternion(0,0.7124f,0,-0.7018f);
                        BridgeToCaptain.TargetDoor = CapitanToBridge;
                        BridgeToCaptain.OptionalTLI = ship.MyTLI;
                        GameObject BridgeToEngineOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(-0.6f, -261, -443.1f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(BridgeToEngineOjb);
                        PLInteriorDoor BridgeToEngine = BridgeToEngineOjb.GetComponent<PLInteriorDoor>();
                        GameObject EngineToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(378.7f, -384.8f, 1366.8f), new Quaternion(0, -0.6448f, 0, 0.7644f));
                        Object.DontDestroyOnLoad(EngineToBridgeOjb);
                        PLInteriorDoor EngineToBridge = EngineToBridgeOjb.GetComponent<PLInteriorDoor>();
                        if (BridgeToEngine != null && EngineToBridge != null)
                        {
                            BridgeToEngine.TargetDoor = EngineToBridge;
                            BridgeToEngine.VisibleName = "Engineering";
                            EngineToBridge.TargetDoor = BridgeToEngine;
                            EngineToBridge.VisibleName = "Bridge";
                        }
                        GameObject BridgeToScienceOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(27.6f, -261, -395.3f), new Quaternion(0, -0.8471f, 0, 0.5314f));
                        Object.DontDestroyOnLoad(BridgeToScienceOjb);
                        PLInteriorDoor BridgeToScience = BridgeToScienceOjb.GetComponent<PLInteriorDoor>();
                        GameObject ScienceToBridgeOjb = Object.Instantiate(BridgeToCaptain.gameObject, new Vector3(380.3f, -399.7f, 1723.8f), new Quaternion(0, 0.7158f, 0, 0.6983f));
                        Object.DontDestroyOnLoad(ScienceToBridgeOjb);
                        PLInteriorDoor ScienceToBridge = ScienceToBridgeOjb.GetComponent<PLInteriorDoor>();
                        if (BridgeToScience != null && ScienceToBridge != null)
                        {
                            BridgeToScience.TargetDoor = ScienceToBridge;
                            BridgeToScience.VisibleName = "Atrium";
                            ScienceToBridge.TargetDoor = BridgeToScience;
                            ScienceToBridge.VisibleName = "Bridge";
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
                    PLPathfinder.GetInstance().AllPGEs.RemoveAll((PLPathfinderGraphEntity graph) => graph.ID == ship.InteriorStatic.transform.GetChild(1).GetComponent<PLShipAStarConnection>().navGraphIDs[0]);
                    ship.InteriorStatic.transform.GetChild(1).GetComponent<PLShipAStarConnection>().navGraphIDs.Clear();
                    //Object.Destroy(ship.InteriorStatic.transform.GetChild(1));
                    ship.InteriorStatic.transform.position = new Vector3(367.3f, -382.3f, 1548);
                    newinterior.transform.SetParent(ship.InteriorStatic.transform);
                    newbridge.transform.SetParent(ship.InteriorStatic.transform);
                    /*
                    newinterior.transform.GetChild(0).localPosition = new Vector3(-117, 22.6f, 7);
                    newinterior.transform.GetChild(0).gameObject.AddMissingComponent<PLCustomAStarConnection>();
                    newinterior.transform.GetChild(0).gameObject.GetComponent<PLCustomAStarConnection>().DataPath = "Assets/Resources/Navgraphs/AOG_HUB_NAVGRAPH.bytes";
                    newinterior.transform.GetChild(0).gameObject.GetComponent<PLCustomAStarConnection>().SavedLoc = new Vector3(-43.8946f, 34.6f, 69.9739f);
                    newinterior.transform.GetChild(0).gameObject.GetComponent<PLCustomAStarConnection>().Start();
                    newinterior.transform.GetChild(0).gameObject.GetComponent<PLCustomAStarConnection>().TLI = ship.MyTLI;
                    */
                    newinterior.transform.GetChild(0).localPosition = new Vector3(-117, 22.6f, 7);
                    newinterior.GetComponentInChildren<PLPlanetAStarConnection>().TLI = ship.MyTLI;
                    //newinterior.GetComponentInChildren<PLPlanetAStarConnection>().SavedLoc = new Vector3(-43.8946f, 34.6f, 69.9739f);
                    //newinterior.GetComponentInChildren<PLPlanetAStarConnection>().DataPath = "Assets/Resources/Navgraphs/AOG_HUB_NAVGRAPH.bytes";
                    newbridge.GetComponentInChildren<PLCustomAStarConnection>().TLI = ship.MyTLI;
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
                    if(teldoor != null) 
                    {
                        GameObject newTelDoor = Object.Instantiate(teldoor, new Vector3(375.1424f, -385.9102f, 1387.404f), new Quaternion(0, 0.3806f, 0, -0.9248f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(369.0363f, - 443.5204f, 1404.2f), new Quaternion(0, 0.5421f, 0, -0.8403f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(374.9182f, - 383.6057f, 1574.436f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(468.4048f, - 400.4253f, 1486.02f), new Quaternion(0,1,0,0));
                        Object.DontDestroyOnLoad(newTelDoor);
                        ship.InteriorRenderers.Add(newTelDoor.GetComponent<MeshRenderer>());
                        newTelDoor = Object.Instantiate(teldoor, new Vector3(379.7382f, - 385.9098f, 1380.838f), new Quaternion(0, -0.2217f, 0, 0.9751f));
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
                    for(int i = 0; i < 14; i++) 
                    {
                        ship.CargoBases[i].transform.position = new Vector3(281.2728f, -443.3417f, 1472.178f + (i* 2));
                        ship.CargoBases[i].transform.rotation = new Quaternion(0, 0.7082f, 0, 0.706f);
                    }
                    List<GameObject> cargo = new List<GameObject>();
                    cargo.AddRange(ship.CargoBases);
                    int index = 14;
                    for(int i = 0; i < 3; i++) 
                    {
                        for(int j = 0; j < 24; j++) 
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
                    ship.EngUpgradeUIRoot.transform.position = new Vector3(340.7982f, - 384.2946f, 1387.454f);
                    ship.EngUpgradeUIRoot.transform.rotation = new Quaternion(0,0.3768f,0,-0.9263f);
                    ship.EngUpgradeUIWorldRoot.position = new Vector3(340.7982f, -384.2946f, 1387.454f);
                    ship.EngUpgradeUIWorldRoot.rotation = new Quaternion(0, 0.3768f, 0, -0.9263f);
                    ship.WeapUpgradeUIRoot.transform.position = new Vector3(358.1492f, -383.6829f, 1507.708f);
                    ship.WeapUpgradeUIRoot.transform.rotation = new Quaternion(0, 0, 0, 1);
                    ship.WeapUpgradeUIWorldRoot.transform.position = new Vector3(358.1492f, -383.6829f, 1507.708f);
                    ship.WeapUpgradeUIWorldRoot.transform.rotation = new Quaternion(0, 0, 0, 1);
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
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(361.3329f, -383.2109f, 1373.86f), new Quaternion(0, 0.3868f, 0, 0.9222f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = reactorscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.9873f, - 416.4473f, 1386.166f), new Quaternion(0, 0f, 0, 1f));
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
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(375.6855f, - 416.4473f, 1368.2f), new Quaternion(0, 0.7071f, 0, 0.7071f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = warpscreen;
                        }
                    }
                    if (coolantscreen != null)
                    {
                        coolantscreen.transform.position = new Vector3(0.6817f, -260.276f, -326.9217f);
                        coolantscreen.transform.rotation = new Quaternion(0, 0.2374f, 0, -0.9714f);
                        if (clonedScreen != null)
                        {
                            GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(358.9745f, -383.2109f, 1375.063f), new Quaternion(0, 0.1361f, 0, 0.9907f));
                            Object.DontDestroyOnLoad(teleport1);
                            teleport1.transform.SetParent(ship.InteriorDynamic.transform);
                            ship.MyScreenBase.AllScreens.Add(teleport1.GetComponent<PLClonedScreen>());
                            teleport1.GetComponent<PLClonedScreen>().MyTargetScreen = coolantscreen;
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(339.9966f, - 416.4473f, 1368.19f), new Quaternion(0, 0.7071f, 0, -0.7071f));
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
                            teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(357.8f, - 416.4473f, 1350.418f), new Quaternion(0, 1f, 0, 0f));
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
                        GameObject teleport1 = Object.Instantiate(clonedScreen.gameObject, new Vector3(354.2722f, - 383.2109f, 1373.794f), new Quaternion(0, 0.3755f, 0, -0.9268f));
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
                    //ship.InteriorStatic = interior;
                    if(foxplush != null) 
                    {
                        GameObject newfox = Object.Instantiate(foxplush, new Vector3(330.5399f, -443.1522f, 1735.403f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one;
                        newfox = Object.Instantiate(foxplush, new Vector3(266.4287f, - 431.316f, 1672.744f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.3f;
                        newfox = Object.Instantiate(foxplush, new Vector3(449.5674f, - 430.5944f, 1666.002f), new Quaternion(0, 0.5688f, 0, -0.8225f));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.3f;
                        newfox = Object.Instantiate(foxplush, new Vector3(319.6508f, - 434.2854f, 1486.746f), new Quaternion(0, 0, 0, 1));
                        Object.DontDestroyOnLoad(newfox);
                        newfox.transform.SetParent(newinterior.transform);
                        newfox.transform.localScale = Vector3.one * 0.5f;
                    }
                    if (PLNetworkManager.Instance.MyLocalPawn != null) PLNetworkManager.Instance.MyLocalPawn.transform.position = (ship.Spawners[PLNetworkManager.Instance.LocalPlayer.GetClassID()] as GameObject).transform.position;
                    ship.ReactorInstance.transform.position = new Vector3(357.8f, -425.7683f, 1368.4f);
                    ship.ReactorInstance.LightMeltdownEnd = new Vector3(0, -12, 0);
                }
            }
            ship.IsGodModeActive = false;
            ship.MyStats.Mass = 4620;
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, ship.CargoBases.Length);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 12);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_SALVAGE_SYSTEM, 0);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 9);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 8);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 4);
            ship.MyStats.SetSlot_IsLocked(ESlotType.E_COMP_HULL, false);
            ship.FactionID = 2;
            ship.SensorDishCollectingScrapRange = 1800;
            if(ship.MyHull != null && ship.MyHull.Level < 9) 
            {
                ship.MyHull.Level = 9;
                ship.MyHull.Current = 3920;
            }
            if(ship.MyStats.GetSlot(ESlotType.E_COMP_THRUSTER).Count == 2 && PhotonNetwork.isMasterClient) 
            {
                for(int i = 0; i < 7;i++)ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(9, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_THRUSTER);
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_INERTIA_THRUSTER).Count  == 1 && PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < 7; i++) ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(25, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_INERTIA_THRUSTER);
            }
            if (ship.MyStats.GetSlot(ESlotType.E_COMP_MANEUVER_THRUSTER).Count == 1 && PhotonNetwork.isMasterClient)
            {
                for (int i = 0; i < 5; i++) ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(26, 0, 2, 0, 12), null), -1, ESlotType.E_COMP_MANEUVER_THRUSTER);
            }
            PLServer.Instance.RepLevels[2] = 5;
            PLServer.Instance.CrewFactionID = 2;
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
            
        }
    }

    [HarmonyPatch(typeof(PLShipInfo), "Update")]
    public class Update
    {
        static void Postfix(PLShipInfo __instance, Color ___WarpObjectCurColor)
        {
            if (__instance.WarpBlocker != null)
            {
                __instance.WarpBlocker.transform.localScale = Vector3.one * (12000f - ___WarpObjectCurColor.a * 6000f);
            }
            if (!__instance.ShowingExterior && __instance.GetIsPlayerShip() && (PLNetworkManager.Instance.MyLocalPawn == null || PLNetworkManager.Instance.MyLocalPawn.CurrentShip == __instance) && Command.shipAssembled && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                __instance.InteriorRenderers.RemoveAll((MeshRenderer render) => render == null);
                __instance.ExteriorMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                if (PLNetworkManager.Instance.LocalPlayer.GetPlayerID() == __instance.SensorDishControllerPlayerID) __instance.ExteriorMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                foreach (MeshRenderer render in __instance.InteriorRenderers)
                {
                    render.enabled = true;
                    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
                PLWDParticleSystem starmap = __instance.InteriorStatic.GetComponentInChildren<PLWDParticleSystem>();
                if (starmap != null)
                {
                    starmap.gameObject.GetComponent<ParticleSystemRenderer>().enabled = true;
                }
                foreach (Light light in __instance.InteriorShipLights)
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
                if (Mod.reactorEffect != null)
                {
                    Mod.reactorEffect.enableEmission = __instance.IsReactorInMeltdown() && __instance.MyReactor != null;
                }
            }
            if(Command.shipAssembled && __instance.GetIsPlayerShip() && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN) 
            {
                foreach(Transform transform in __instance.RegularTurretPoints) 
                {
                    foreach(Transform child in transform) 
                    {
                        if(child.localScale.x < 1) 
                        {
                            child.localScale = Vector3.one;
                        }
                    }
                }
                if(__instance.MainTurretPoint.childCount > 0 && __instance.MainTurretPoint.GetChild(0).localScale.x < 10) 
                {
                    __instance.MainTurretPoint.GetChild(0).localScale = Vector3.one * 13;
                }
                for(int i = 0; i < 10; i++) 
                {
                    if (Command.prisionCells[i] != null) 
                    {
                        if (PLServer.Instance.GetPlayerFromPlayerID(Command.playersArrested[i]) == null) Command.playersArrested[i] = -1;
                        Command.prisionCells[i].SetActive(Command.playersArrested[i] != -1);
                    }
                }
                if(PhotonNetwork.isMasterClient)ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.prisionRPC", PhotonTargets.Others, new object[] { Command.playersArrested });
            }
        }
        [HarmonyPatch(typeof(PLPlayer),"Update")]
        class PlayerUpdate 
        {
            static float LastWarning = Time.time;
            static void Postfix(PLPlayer __instance) 
            {
                if(__instance == PLNetworkManager.Instance.LocalPlayer && Command.playersArrested.Contains(__instance.GetPlayerID()) && __instance.GetPawn() != null) 
                {
                    for(int i = 0; i < 10; i++) 
                    {
                        if(Command.playersArrested[i] == __instance.GetPlayerID()) 
                        {
                            Vector3 prisioncell = Vector3.one;
                            switch (i) 
                            {
                                default:
                                case 0:
                                    prisioncell = new Vector3(335.8f,-442.2f,1744.3f);
                                    break;
                                case 1:
                                    prisioncell = new Vector3(335.8f, -442.2f, 1741.3f);
                                    break;
                                case 2:
                                    prisioncell = new Vector3(335.8f, -442.2f, 1738.3f);
                                    break;
                                case 3:
                                    prisioncell = new Vector3(335.8f, -442.2f, 1735.3f);
                                    break;
                                case 4:
                                    prisioncell = new Vector3(335.8f, -442.2f, 1732.3f);
                                    break;
                                case 5:
                                    prisioncell = new Vector3(331f, -442.2f, 1732.3f);
                                    break;
                                case 6:
                                    prisioncell = new Vector3(331f, -442.2f, 1735.3f);
                                    break;
                                case 7:
                                    prisioncell = new Vector3(331f, -442.2f, 1738.3f);
                                    break;
                                case 8:
                                    prisioncell = new Vector3(331f, -442.2f, 1741.3f);
                                    break;
                                case 9:
                                    prisioncell = new Vector3(331f, -442.2f, 1744.3f);
                                    break;
                            }
                            if((__instance.GetPawn().transform.position - prisioncell).sqrMagnitude > 5) 
                            {
                                __instance.photonView.RPC("NetworkTeleportToSubHub", PhotonTargets.All, new object[]
                                {
                                __instance.StartingShip.MyTLI.SubHubID,
                                0
                                });
                                __instance.RecallPawnToPos(prisioncell);
                                if (Time.time - LastWarning > 10)
                                {
                                    PLServer.Instance.AddCrewWarning("Your host arrested you!", Color.red, 1, "Prision");
                                    LastWarning = Time.time;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(PLWDParticleSystem), "Update")]
        internal class WD_Hub_Map
        {
            static bool Prefix(PLWDParticleSystem __instance)
            {
                if (__instance.gameObject.scene.buildIndex != 10)
                {
                    if (Time.time - __instance.LastSetupTime > 10f && PLGlobal.Instance.Galaxy != null)
                    {
                        __instance.LastSetupTime = Time.time;
                        __instance.System.maxParticles = PLGlobal.Instance.Galaxy.AllSectorInfos.Count;
                        List<ParticleSystem.Particle> list = new List<ParticleSystem.Particle>();
                        foreach (PLSectorInfo plsectorInfo in PLGlobal.Instance.Galaxy.AllSectorInfos.Values)
                        {
                            if (PLStarmap.ShouldShowSectorBG(plsectorInfo))
                            {
                                ParticleSystem.Particle item = default(ParticleSystem.Particle);
                                int faction = plsectorInfo.MySPI.Faction;
                                if (faction != 2)
                                {
                                    item.color = PLGlobal.Instance.Galaxy.GetFactionColorForID(faction);
                                    item.size = 0.25f;
                                }
                                else
                                {
                                    item.color = PLGlobal.Instance.Galaxy.GetFactionColorForID(2);
                                    item.size = 0.4f;
                                }
                                item.position = __instance.transform.position + new Vector3(plsectorInfo.Position.x, plsectorInfo.Position.z, plsectorInfo.Position.y) * __instance.Size;
                                item.remainingLifetime = 10f;
                                list.Add(item);
                            }
                        }
                        __instance.System.SetParticles(list.ToArray(), list.Count);
                    }
                    return false;
                }
                return true;
            }
        }
    }
}
