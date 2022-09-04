using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using PulsarModLoader;
using System.Threading.Tasks;

namespace The_Flagship
{
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
        static void Postfix(PLOldWarsShip_Human __instance, ref string __result)
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled) __result = "Nnockback Resistance\n+100% Reactor Output\n+10x Oxygen Reffil\n<color=red>Cannot use repair stations or warp gates</color>";
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
                inStats.OxygenRefillRate *= 10;
            }
        }
    }
    [HarmonyPatch(typeof(PLMegaTurret), "ChargeComplete")]
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
            if (__instance.ShipStats.Ship.GetIsPlayerShip() && Command.shipAssembled)
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
            if (Command.shipAssembled && __instance.ShipInfo != null && __instance.ShipInfo.GetIsPlayerShip() && __instance.ShipInfo.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                __instance.ShipInfo.ExteriorRigidbody.AddTorque(collision.impulse * -1, ForceMode.Force);
                __instance.ShipInfo.ExteriorRigidbody.AddForce(collision.impulse * -1, ForceMode.Force);
            }
        }
    }
    [HarmonyPatch(typeof(PLFire), "Update")]
    class FireUpdate
    {
        static void Postfix(PLFire __instance)
        {
            if (__instance.MyShip == PLEncounterManager.Instance.PlayerShip && PLEncounterManager.Instance.PlayerShip && Command.shipAssembled)
            {
                __instance.MyRoomArea = PLEncounterManager.Instance.PlayerShip.AllRoomAreas[0];
                PLEncounterManager.Instance.PlayerShip.AllRoomAreas[0].IsHidden = false;
                __instance.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
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
            if (__instance.ShipInfo != null && __instance.ShipInfo.GetIsPlayerShip() && Command.shipAssembled)
            {
                __instance._rigidbody.AddTorque(__instance.InputTorque * __instance.RotationSpeed * (__instance.IsBoosting ? 1.32f : 1f) * -0.9f);
            }
        }
    }
    [HarmonyPatch(typeof(PLSpaceScrap), "Update")]
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
            if (!__instance.IsOpen && __instance.MyInterior == null && __instance.gameObject.layer == PLEncounterManager.Instance.PlayerShip.InteriorStatic.layer && PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.CurrentShip == PLEncounterManager.Instance.PlayerShip)
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
    [HarmonyPatch(typeof(PLServer), "CreateFireAtSystem")]
    class EngineeringFireFix
    {
        static void Postfix(PLServer __instance, PLMainSystem inSys, bool green = false)
        {
            if (inSys is PLEngineeringSystem && inSys.MyShipInfo.GetIsPlayerShip() && Command.shipAssembled)
            {
                string methodName = "ClientCreateFireAtSystem";
                if (green)
                {
                    methodName = "ClientCreateGREENFireAtSystem";
                }
                __instance.photonView.RPC(methodName, PhotonTargets.All, new object[]
                {
                    inSys.MyShipInfo.ShipID,
                    inSys.SystemID,
                    __instance.ServerFireIDCounter + 1,
                    new Vector3(357.2256f, -385.7804f, 1346.935f)
                });
                __instance.ServerFireIDCounter++;
            }
        }
    }
    [HarmonyPatch(typeof(PLShipInfoBase), "GetAllTeleporterLocationInstances")]
    class TeleporterToInsideTeleports
    {
        static void Postfix(PLShipInfoBase __instance, ref List<PLTeleportationLocationInstance> __result)
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled)
            {
                __result.Add(__instance.MyTLI);
            }
        }
    }
    [HarmonyPatch(typeof(PLTeleportationLocationInstance), "ShouldBeUsable")]
    class TeleporterInsideEnabled
    {
        static void Postfix(PLTeleportationLocationInstance __instance, ref bool __result)
        {
            if (__instance.MyShipInfo != null && __instance.MyShipInfo.GetIsPlayerShip() && Command.shipAssembled)
            {
                __result = true;
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
            if (Command.shipAssembled && __instance.GetIsPlayerShip() && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                foreach (Transform transform in __instance.RegularTurretPoints)
                {
                    foreach (Transform child in transform)
                    {
                        if (child.localScale.x < 1)
                        {
                            child.localScale = Vector3.one;
                        }
                    }
                }
                if (__instance.MainTurretPoint.childCount > 0 && __instance.MainTurretPoint.GetChild(0).localScale.x < 10)
                {
                    __instance.MainTurretPoint.GetChild(0).localScale = Vector3.one * 13;
                }
                for (int i = 0; i < 10; i++)
                {
                    if (Command.prisionCells[i] != null)
                    {
                        if (PLServer.Instance.GetPlayerFromPlayerID(Command.playersArrested[i]) == null) Command.playersArrested[i] = -1;
                        Command.prisionCells[i].SetActive(Command.playersArrested[i] != -1);
                    }
                }
                if (PhotonNetwork.isMasterClient) ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.prisionRPC", PhotonTargets.Others, new object[] { Command.playersArrested });
            }
        }
        [HarmonyPatch(typeof(PLPlayer), "Update")]
        class PlayerUpdate
        {
            static float LastWarning = Time.time;
            static void Postfix(PLPlayer __instance)
            {
                if (__instance == PLNetworkManager.Instance.LocalPlayer && Command.playersArrested.Contains(__instance.GetPlayerID()) && __instance.GetPawn() != null)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (Command.playersArrested[i] == __instance.GetPlayerID())
                        {
                            Vector3 prisioncell = Vector3.one;
                            switch (i)
                            {
                                default:
                                case 0:
                                    prisioncell = new Vector3(335.8f, -442.2f, 1744.3f);
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
                            if ((__instance.GetPawn().transform.position - prisioncell).sqrMagnitude > 5)
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
        [HarmonyPatch(typeof(PLBot), "Update")]
        class GiveControllerBot
        {
            static void Postfix(PLBot __instance)
            {
                /*
                if (__instance.MyBotController != null)
                {
                    __instance.MyBotController.MyBot = __instance;
                    if (PLEncounterManager.Instance.PlayerShip != null && __instance.AI_TargetTLI == PLEncounterManager.Instance.PlayerShip.MyTLI && Command.shipAssembled)
                    {

                        PLPathfinderGraphEntity targetInterior = PLPathfinder.GetInstance().GetPGEforTLIAndPosition(PLEncounterManager.Instance.PlayerShip.MyTLI, __instance.MyBotController.Assigned_AI_TargetPos);
                        PulsarModLoader.Utilities.Messaging.Notification("Current: " + __instance.PlayerOwner.MyPGE.ID + ", Target: " + targetInterior.ID);
                        PulsarModLoader.Utilities.Messaging.Notification("Target Position: " + __instance.MyBotController.Assigned_AI_TargetPos);
                        if (__instance.PlayerOwner.MyPGE != null && targetInterior.ID != __instance.PlayerOwner.MyPGE.ID)
                        {
                            int doorID = 0;

                            if (targetInterior.ID < __instance.PlayerOwner.MyPGE.ID) //Bot is in brige but wants to go to main area 
                            {
                                float distance = (new Vector3(434.2143f, -430.2697f, 1730.034f) - __instance.AI_TargetPos).magnitude;
                                if ((new Vector3(378.7f, -384.8338f, 1366.8f) - __instance.AI_TargetPos).magnitude < distance)
                                {
                                    distance = (new Vector3(378.7f, -384.8338f, 1366.8f) - __instance.AI_TargetPos).magnitude;
                                    doorID = 1;
                                }
                                if ((new Vector3(380.3f, -399.7f, 1723.8f) - __instance.AI_TargetPos).magnitude < distance)
                                {
                                    distance = (new Vector3(380.3f, -399.7f, 1723.8f) - __instance.AI_TargetPos).magnitude;
                                    doorID = 2;
                                }
                                if ((new Vector3(375.4f, -382.7f, 1575.7f) - __instance.AI_TargetPos).magnitude < distance)
                                {
                                    doorID = 3;
                                }
                                switch (doorID)
                                {
                                    default:
                                    case 0:
                                        __instance.AI_TargetPos = new Vector3(-28.0667f, -260.9866f, -395.6801f);
                                        __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                        if ((__instance.PlayerOwner.GetPawn().transform.position - __instance.AI_TargetPos).sqrMagnitude < 16)
                                        {
                                            __instance.MyBotController.Path = null;
                                            __instance.PlayerOwner.GetPawn().transform.position = new Vector3(434.2143f, -430.2697f, 1730.034f);
                                            __instance.PlayerOwner.GetPawn().OnTeleport();
                                            if (__instance.PlayerOwner.GetPawn() != null)
                                            {
                                                __instance.AI_TargetPos = __instance.PlayerOwner.GetPawn().transform.position;
                                                __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                                __instance.ResetTLI();
                                            }
                                        }
                                        break;
                                    case 1:
                                        __instance.AI_TargetPos = new Vector3(-0.6f, -261, -443.1f);
                                        __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                        __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                        if ((__instance.PlayerOwner.GetPawn().transform.position - __instance.AI_TargetPos).sqrMagnitude < 16)
                                        {
                                            __instance.MyBotController.Path = null;
                                            __instance.PlayerOwner.GetPawn().transform.position = new Vector3(378.7f, -384.8338f, 1366.8f);
                                            __instance.PlayerOwner.GetPawn().OnTeleport();
                                            if (__instance.PlayerOwner.GetPawn() != null)
                                            {
                                                __instance.AI_TargetPos = __instance.PlayerOwner.GetPawn().transform.position;
                                                __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                                __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                                __instance.ResetTLI();
                                            }
                                        }
                                        break;
                                    case 2:
                                        __instance.AI_TargetPos = new Vector3(27.6f, -261f, -395.3f);
                                        __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                        __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                        if ((__instance.PlayerOwner.GetPawn().transform.position - __instance.AI_TargetPos).sqrMagnitude < 16)
                                        {
                                            __instance.MyBotController.Path = null;
                                            __instance.PlayerOwner.GetPawn().transform.position = new Vector3(380.3f, -399.7f, 1723.8f);
                                            __instance.PlayerOwner.GetPawn().OnTeleport();
                                            if (__instance.PlayerOwner.GetPawn() != null)
                                            {
                                                __instance.AI_TargetPos = __instance.PlayerOwner.GetPawn().transform.position;
                                                __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                                __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                                __instance.ResetTLI();
                                            }
                                        }
                                        break;
                                    case 3:
                                        __instance.AI_TargetPos = new Vector3(-13.4f, -261, -364.3f);
                                        __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                        __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                        if (__instance.PlayerOwner.GetPawn() != null && (__instance.PlayerOwner.GetPawn().transform.position - __instance.AI_TargetPos).sqrMagnitude < 16)
                                        {
                                            __instance.PlayerOwner.GetPawn().transform.position = new Vector3(375.4f, -382.7f, 1575.7f);
                                            __instance.PlayerOwner.GetPawn().OnTeleport();
                                            __instance.AI_TargetPos = __instance.PlayerOwner.GetPawn().transform.position;
                                            __instance.AI_TargetPos_Raw = __instance.AI_TargetPos;
                                            __instance.MyBotController.Assigned_AI_TargetPos = __instance.AI_TargetPos;
                                        }
                                        break;
                                }
                            }
                            else //Bot is in main area but wants to go to bridge
                            {

                            }
                        }
                        PulsarModLoader.Utilities.Messaging.Notification("New Target Position: " + __instance.MyBotController.Assigned_AI_TargetPos);
                    }
                }
                */
            }
        }
    }
}
