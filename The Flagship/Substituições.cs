using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using PulsarModLoader;
using System.Threading.Tasks;
using static PLBurrowArena;
using Pathfinding;

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
    public class OnJoin
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
        public static async void AutoAssemble()
        {
            while (PLEncounterManager.Instance.PlayerShip == null) await Task.Yield();
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, ship.CargoBases.Length);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 12);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 9);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 8);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 6);
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 4);
            ship.MyStats.SetSlot_IsLocked(ESlotType.E_COMP_HULL, false);
            while (PLEncounterManager.Instance == null || !PLLoader.Instance.IsLoaded)
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
    [HarmonyPatch(typeof(PLServer), "NotifyPlayerStart")]
    class MeshEnable 
    {
        static void Postfix() 
        {
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            if(ship != null && ship.ShipTypeID == EShipType.OLDWARS_HUMAN) 
            {
                foreach (MeshRenderer render in ship.InteriorStatic.GetComponentsInChildren<MeshRenderer>(true))
                {
                    render.enabled = true;
                    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
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
    [HarmonyPatch(typeof(PLPersistantEncounterInstance), "GetPlayerStartLoc")]
    class InspectionSpawn
    {
        static void Postfix(PLPersistantEncounterInstance __instance, ref Vector3 __result)
        {
            if (__instance.GetType() == typeof(PLCUContrabandStationEncounter) && Command.shipAssembled)
            {
                __result = new Vector3(-8.8961f, 22.3574f, -258.0648f);
            }
        }
    }
    [HarmonyPatch(typeof(PLOldWarsShip_Human), "SetupShipStats")]
    class StartingSlots
    {
        static void Postfix(PLOldWarsShip_Human __instance)
        {
            if (__instance.GetIsPlayerShip() && Mod.AutoAssemble)
            {
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, 72);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 12);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 6);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 9);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 8);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 6);
                __instance.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 4);
            }
        }
    }
    [HarmonyPatch(typeof(PLServer), "SpawnPlayerShipFromSaveData")]
    class FinishSlots
    {
        static void Postfix()
        {
            if (PLEncounterManager.Instance.PlayerShip.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
                if (ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_CPU).Count <= 5 && ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_TURRET).Count <= 2 && ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_THRUSTER).Count <= 2
                    && ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_INERTIA_THRUSTER).Count <= 1 && ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MANEUVER_THRUSTER).Count <= 1 && ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_SENS).Count <= 1)
                {
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, 14);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 5);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 2);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 2);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 1);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 1);
                    ship.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 1);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLShipInfoBase), "Start")]
    class PowerSlide
    {
        static void Postfix(PLShipInfoBase __instance)
        {
            float[] powerPercent = new float[17];
            powerPercent.AddRangeToArray(__instance.PowerPercent_SysIntConduits);
            __instance.PowerPercent_SysIntConduits = powerPercent;
            for (int i = 0; i < __instance.PowerPercent_SysIntConduits.Length; i++)
            {
                __instance.PowerPercent_SysIntConduits[i] = 1f;
            }
            powerPercent = new float[17];
            powerPercent.AddRangeToArray(__instance.m_SysIntConduit_LocalChangeTime);
            powerPercent[16] = 0;
            __instance.m_SysIntConduit_LocalChangeTime = powerPercent;
            powerPercent = new float[17];
            powerPercent.AddRangeToArray(__instance.m_SysIntConduit_LocalChangeValue);
            powerPercent[16] = 0;
            __instance.m_SysIntConduit_LocalChangeValue = powerPercent;
        }
    }
    [HarmonyPatch(typeof(PLCombatTarget), "GetIsFriendly")]
    class PatrolBotsFriend 
    {
        static void Postfix(PLCombatTarget __instance,ref bool __result) 
        {
            if(__instance is PLBoardingBot && __instance.name.Contains("(frienddrone)")) 
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(PLBoardingBot), "CombatRoutine")]
    class PatrolBotsCombat 
    {
        static bool Prefix(PLBoardingBot __instance) 
        {
            if (!__instance.name.Contains("(frienddrone)")) return true;
            PLCombatTarget lCombatTarget = null;
            if (UnityEngine.Random.Range(0, 20) < 2)
            {
                __instance.TargetPawn = null;
                float num = float.MaxValue;
                foreach (PLPawn plpawn in PLGameStatic.Instance.AllPawns)
                {
                    if (plpawn != null && !plpawn.Cloaked && !plpawn.PreviewPawn && !plpawn.IsDead && !Physics.Linecast(__instance.transform.position, plpawn.transform.position + Vector3.up * 1.5f, out _) && plpawn.GetTeamID() != __instance.CurrentShip.TeamID)
                    {
                        float magnitude = (plpawn.transform.position - __instance.transform.position).magnitude;
                        float num2 = Vector3.Dot((plpawn.transform.position - __instance.transform.position).normalized, __instance.transform.forward);
                        float num3 = magnitude * (1f - num2);
                        if (num3 < num)
                        {
                            num = num3;
                            __instance.TargetPawn = plpawn;
                        }
                    }
                }
                foreach (PLCombatTarget combatTarget in PLGameStatic.Instance.AllCombatTargets) 
                {
                    if(combatTarget != null && !(combatTarget is PLPawn) && !combatTarget.IsDead && !Physics.Linecast(__instance.transform.position, combatTarget.transform.position + Vector3.up * 1.5f, out _) && !combatTarget.GetIsFriendly()) 
                    {
                        float magnitude = (combatTarget.transform.position - __instance.transform.position).magnitude;
                        float num2 = Vector3.Dot((combatTarget.transform.position - __instance.transform.position).normalized, __instance.transform.forward);
                        float num3 = magnitude * (1f - num2);
                        if (num3 < num)
                        {
                            num = num3;
                            lCombatTarget = combatTarget;
                        }
                    }
                }
                float num6 = 1.5f;
                if (PLServer.Instance != null)
                {
                    num6 -= PLServer.Instance.ChaosLevel * 0.1f;
                }
                if (Time.time - __instance.LastShotFiredTime > num6 && !__instance.IsWeaponStunned())
                {
                    if (__instance.TargetPawn != null)
                    {
                        if (Vector3.Dot((__instance.TargetPawn.transform.position - __instance.transform.position).normalized, __instance.transform.forward) > 0.9f && UnityEngine.Random.Range(0, 25) < 9)
                        {
                            __instance.Server_FireShot();
                        }
                    }
                    else if(lCombatTarget != null) 
                    {
                        if (Vector3.Dot((lCombatTarget.transform.position - __instance.transform.position).normalized, __instance.transform.forward) > 0.9f && UnityEngine.Random.Range(0, 25) < 9)
                        {
                            __instance.Server_FireShot();
                        }
                    }
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PLBoardingBot),"Update")]
    class PatrolBotUpdate 
    {
        static bool Prefix(PLBoardingBot __instance) 
        {
            if (!__instance.name.Contains("(frienddrone)")) return true;
            float expectedMaxH = 150 + Mod.PatrolBotsLevel * 25;
            if(__instance.MaxHealth != expectedMaxH) 
            {
                float Percent = __instance.Health / __instance.MaxHealth;
                __instance.MaxHealth = expectedMaxH;
                __instance.Health = __instance.MaxHealth * Percent;
            }
            PLCombatTarget lCombatTarget = null;
            float nums = float.MaxValue;
            foreach (PLCombatTarget combatTarget in PLGameStatic.Instance.AllCombatTargets)
            {
                if (combatTarget != null && !(combatTarget is PLPawn) && !combatTarget.IsDead && !Physics.Linecast(__instance.transform.position, combatTarget.transform.position + Vector3.up * 1.5f, out _) && !combatTarget.GetIsFriendly())
                {
                    float magnitude = (combatTarget.transform.position - __instance.transform.position).magnitude;
                    float num2 = Vector3.Dot((combatTarget.transform.position - __instance.transform.position).normalized, __instance.transform.forward);
                    float num3 = magnitude * (1f - num2);
                    if (num3 < nums)
                    {
                        nums = num3;
                        lCombatTarget = combatTarget;
                    }
                }
            }
            if (Time.time - __instance.LastDamageTakenTime > 0.6f)
            {
                __instance.SlowHealth = Mathf.Lerp(__instance.SlowHealth, __instance.Health, Mathf.Clamp01(Time.deltaTime * 8f));
            }
            if (Time.time - __instance.LastDamageTakenTime > 1f)
            {
                __instance.SlowHealth_NonSmoothed = __instance.Health;
            }
            __instance.UpdateGraphAndAreaIndex(false);
            __instance.StunnedAmount -= Time.deltaTime * 20f * __instance.StunnedRecoverySpeed;
            __instance.StunnedAmount -= Time.deltaTime * Mathf.Clamp01(__instance.MaxHealth * 5E-05f) * 400f * __instance.StunnedRecoverySpeed;
            __instance.StunnedAmount = Mathf.Clamp(__instance.StunnedAmount, 0f, 180f);
            __instance.SlowedAmount -= Time.deltaTime;
            __instance.SlowedAmount -= Time.deltaTime * Mathf.Clamp01(__instance.MaxHealth * 5E-05f) * 20f;
            __instance.SlowedAmount = Mathf.Clamp(__instance.SlowedAmount, 0f, 5f);
            __instance.SlowedAmount_Smoothed = Mathf.Lerp(__instance.SlowedAmount_Smoothed, __instance.SlowedAmount, Mathf.Clamp01(Time.deltaTime * 1.5f));
            __instance.Lifetime += Time.deltaTime;
            if (PLServer.Instance != null && !__instance.SetupCombatTargetID)
            {
                if (PhotonNetwork.isMasterClient)
                {
                    PLServer instance = PLServer.Instance;
                    int pawnBaseIDCounter = instance.PawnBaseIDCounter;
                    instance.PawnBaseIDCounter = pawnBaseIDCounter + 1;
                    __instance.CombatTargetID = pawnBaseIDCounter;
                }
                else if (__instance.photonView != null)
                {
                    __instance.photonView.RPC("RequestCombatTargetID", PhotonTargets.MasterClient, Array.Empty<object>());
                }
                __instance.SetupCombatTargetID = true;
            }
            __instance.currentVel = (__instance.transform.position - __instance.prevSpd).magnitude / Time.unscaledDeltaTime;
            __instance.prevSpd = __instance.transform.position;
            PLMusic.SetRTPCValue("investigator_speed", __instance.currentVel);
            if (__instance.CurrentShip == null && (__instance.visualLastSetLayer != 10 || __instance.firstVisualFrame))
            {
                __instance.visualLastSetLayer = 10;
                __instance.gameObject.layer = 10;
                foreach (Renderer renderer in __instance.MyRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.gameObject.layer = 10;
                        __instance.firstVisualFrame = false;
                    }
                }
            }
            else if (__instance.CurrentShip != null && (__instance.visualLastSetLayer != 11 || __instance.firstVisualFrame))
            {
                __instance.visualLastSetLayer = 11;
                __instance.gameObject.layer = 11;
                foreach (Renderer renderer2 in __instance.MyRenderers)
                {
                    if (renderer2 != null)
                    {
                        renderer2.gameObject.layer = 11;
                        __instance.firstVisualFrame = false;
                    }
                }
            }
            if (PhotonNetwork.isMasterClient)
            {
                __instance.CombatRoutine();
                if (__instance.Health <= 0f)
                {
                    if (!__instance.IsDead)
                    {
                        __instance.IsDead = true;
                        __instance.LastHeadDamageTakenTime = Time.time;
                        __instance.transform.Rotate(90, 0, 0);
                        foreach (Light light in __instance.MyLights)
                        {
                            if (light != null)
                            {
                                light.enabled = false;
                            }
                        }
                    }
                    __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.Euler(new Vector3(90,0,0)), Mathf.Clamp01(Time.deltaTime));
                    if (Time.time - __instance.LastHeadDamageTakenTime > 30) 
                    {
                        __instance.IsDead = false;
                        __instance.Health = __instance.MaxHealth;
                        foreach (Light light in __instance.MyLights)
                        {
                            if (light != null)
                            {
                                light.enabled = true;
                            }
                        }
                    }
                    else return false;
                }
                PLPathfinderGraphEntity pgeforTLIAndTransform = PLPathfinder.GetInstance().GetPGEforTLIAndTransform(__instance.MyCurrentTLI, __instance.transform);
                if (pgeforTLIAndTransform != null)
                {
                    if (__instance.nodesCountInArea_cache_IsFirstFrame || __instance.nodesCountInArea_cachedAreaIndexForSearching != __instance.AreaIndexForSearching || UnityEngine.Random.Range(0, 4000) < 2)
                    {
                        __instance.nodesCountInArea_cache_IsFirstFrame = false;
                        __instance.nodesCountInArea_cachedAreaIndexForSearching = __instance.AreaIndexForSearching;
                        __instance.nodesCountInArea = 0;
                        __instance.nodesInArea_Cached.Clear();
                        pgeforTLIAndTransform.Graph.GetNodes(delegate (GraphNode node)
                        {
                            if (node.Area == __instance.AreaIndexForSearching)
                            {
                                __instance.nodesInArea_Cached.Add(node);
                                __instance.nodesCountInArea++;
                            }
                            return true;
                        });
                    }
                    if (UnityEngine.Random.Range(0, 8) < 2)
                    {
                        foreach (GraphNode graphNode in __instance.nodesInArea_Cached)
                        {
                            if (Vector3.SqrMagnitude((Vector3)graphNode.position - __instance.transform.position) < 25f && !__instance.visitedNodes.Contains(graphNode.position))
                            {
                                __instance.visitedNodes.Add(graphNode.position);
                            }
                        }
                    }
                    if ((__instance.visitedNodes.Count >= Mathf.RoundToInt((float)__instance.nodesCountInArea * 0.8f) && __instance.Lifetime > 5f) || __instance.Lifetime > 120f)
                    {
                        __instance.visitedNodes.Clear();
                    }
                    bool flag = true;
                    float num = 25f;
                    __instance.currentInvestigationTarget = null;
                    if (__instance.currentInvestigationTarget == null)
                    {
                        foreach (Component component in __instance.ComponentsToCasuallyInvestigate)
                        {
                            if (component != null && !__instance.investigatedGOs.Contains(component.gameObject))
                            {
                                float num2 = Vector3.SqrMagnitude(component.transform.position - __instance.transform.position);
                                if (num2 < num)
                                {
                                    num = num2;
                                    __instance.currentInvestigationTarget = component.transform;
                                }
                            }
                        }
                        if (UnityEngine.Random.Range(0, 100) < 2 && __instance.currentInvestigationTarget == null)
                        {
                            __instance.investigatedGOs.Clear();
                        }
                    }
                    Vector3 vector = Vector3.zero;
                    if (flag && !__instance.IsStunned() && __instance.currentPath != null && __instance.currentPath.vectorPath.Count > __instance.currentPathIndex)
                    {
                        __instance.targetPos = __instance.currentPath.vectorPath[__instance.currentPathIndex] + Vector3.up * 1.5f;
                        if (Vector3.SqrMagnitude(__instance.targetPos - __instance.transform.position) < 2f)
                        {
                            __instance.currentPathIndex++;
                        }
                        vector = (__instance.targetPos - __instance.transform.position).normalized;
                        __instance.transform.position = Vector3.Lerp(__instance.transform.position, __instance.targetPos + new Vector3(0f, Mathf.Sin(Time.time) * 0.4f, 0f), Mathf.Clamp01(Time.deltaTime*2*(1f + 0.2f * Mod.PatrolBotsLevel)));
                    }
                    if (__instance.TargetPawn != null)
                    {
                        __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.LookRotation(__instance.TargetPawn.transform.position - __instance.transform.position), Mathf.Clamp01(Time.deltaTime * 9f));
                    }
                    else if (lCombatTarget != null)
                    {
                        __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.LookRotation(lCombatTarget.transform.position - __instance.transform.position), Mathf.Clamp01(Time.deltaTime * 9f));
                    }
                    else if (__instance.TargetSystemInstance != null)
                    {
                        Vector3 a = __instance.TargetSystemInstance.transform.position + Vector3.up;
                        __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.LookRotation(a - __instance.transform.position), Mathf.Clamp01(Time.deltaTime * 9f));
                    }
                    else if (__instance.currentInvestigationTarget != null)
                    {
                        if (UnityEngine.Random.Range(0, 1300) == 0 && !__instance.investigatedGOs.Contains(__instance.currentInvestigationTarget.gameObject))
                        {
                            __instance.investigatedGOs.Add(__instance.currentInvestigationTarget.gameObject);
                        }
                        __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.LookRotation(__instance.currentInvestigationTarget.position - __instance.transform.position), Mathf.Clamp01(Time.deltaTime * 9f));
                    }
                    else if (vector != Vector3.zero)
                    {
                        __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.LookRotation(vector), Mathf.Clamp01(Time.deltaTime * 2f));
                    }
                }
                if (__instance.currentInvestigationTarget != null)
                {
                    __instance.LastNetScanPos = __instance.currentInvestigationTarget.position;
                }
                else
                {
                    __instance.LastNetScanPos = Vector3.zero;
                }
                nums = float.MaxValue;
                try
                {
                    if (!__instance.pathRequestInProgress && __instance.CurrentShip != null && __instance.CurrentShip.IsAuxSystemActive(6) && __instance.TargetPawn == null && lCombatTarget == null)
                    {
                        PLPathfinderGraphEntity myGraphEntity = PLPathfinder.GetInstance().GetPGEforTLIAndTransform(__instance.MyCurrentTLI, __instance.transform);
                        PLCombatTarget combatTarget = null;
                        foreach (PLCombatTarget pLCombatTarget in PLGameStatic.Instance.AllCombatTargets)
                        {
                            if (pLCombatTarget.gameObject != null && pLCombatTarget.CurrentShip == __instance.CurrentShip && PLPathfinder.GetInstance().GetPGEforTLIAndTransform(pLCombatTarget.MyCurrentTLI, pLCombatTarget.transform) == myGraphEntity && !pLCombatTarget.IsDead && !pLCombatTarget.GetIsFriendly())
                            {
                                float magnitude = (pLCombatTarget.transform.position - __instance.transform.position).magnitude;
                                float num2 = Vector3.Dot((pLCombatTarget.transform.position - __instance.transform.position).normalized, __instance.transform.forward);
                                float num3 = magnitude * (1f - num2);
                                if (num3 < nums)
                                {
                                    nums = num3;
                                    combatTarget = pLCombatTarget;
                                }
                            }
                        }
                        if (combatTarget != null && (__instance.currentPath == null || ((__instance.currentPath.vectorPath[__instance.currentPath.vectorPath.Count - 1] + Vector3.up * 1.5f) - combatTarget.transform.position).magnitude > 25))
                        {
                            __instance.seeker.StartPath(__instance.transform.position, combatTarget.transform.position, new OnPathDelegate(__instance.OnPathComplete));
                            foreach (Light light in __instance.MyLights)
                            {
                                if (light != null)
                                {
                                    light.color = Color.magenta;
                                }
                            }
                            __instance.pathRequestInProgress = true;
                        }
                    }
                    else if (__instance.currentPath == null)
                    {
                        foreach (Light light in __instance.MyLights)
                        {
                            if (light != null)
                            {
                                light.color = Color.green;
                            }
                        }
                    }
                }
                catch{ }
            }
            else
            {
                __instance.transform.position = Vector3.Lerp(__instance.transform.position, __instance.LastNetPos, Mathf.Clamp01(Time.deltaTime * 5f *2* (1f + 0.2f * Mod.PatrolBotsLevel)));
                __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, __instance.LastNetRot, Mathf.Clamp01(Time.deltaTime * 8f));
            }
            if (PhotonNetwork.isMasterClient && Time.time - __instance.LastTimeSentNetUpdate > 0.1f)
            {
                __instance.LastTimeSentNetUpdate = Time.time;
                PLServer.Instance.SendUnreliableCombatTargetUpdateToOthers(__instance.CombatTargetID, __instance.transform.position, __instance.transform.rotation);
            }
            return false;
        }
        public static IEnumerator PathRoutine(PLBoardingBot __instance) 
        {
            bool setupAreaIndex = false;
            int endOfFrame = 0;
            while (Application.isPlaying)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1f));
                if (PhotonNetwork.isMasterClient)
                {
                    PLPathfinderGraphEntity pgeforTLIAndTransform = PLPathfinder.GetInstance().GetPGEforTLIAndTransform(__instance.MyCurrentTLI, __instance.transform);
                    if (pgeforTLIAndTransform != null)
                    {
                        if (!setupAreaIndex || UnityEngine.Random.Range(0, 10) == 0)
                        {
                            NNInfoInternal nearest = pgeforTLIAndTransform.Graph.GetNearest(__instance.transform.position - Vector3.up);
                            if (nearest.node != null)
                            {
                                __instance.AreaIndexForSearching = nearest.node.Area;
                                setupAreaIndex = true;
                            }
                        }
                        yield return endOfFrame;
                        if (setupAreaIndex)
                        {
                            if (__instance.NextMainPathPos == null)
                            {
                                GraphNode nextMainPathPos = null;
                                float num = float.MaxValue;
                                Vector3 randomTarget = new Vector3(UnityEngine.Random.Range(249,480), UnityEngine.Random.Range(-426,-345), UnityEngine.Random.Range(1337, 1795));
                                Vector3 randomTarget2 = new Vector3(UnityEngine.Random.Range(-33, 31), -256, UnityEngine.Random.Range(-447, -315));
                                if ((__instance.transform.position - randomTarget).magnitude > (__instance.transform.position - randomTarget2).magnitude) randomTarget = randomTarget2;
                                NNConstraint nnconstraint = new NNConstraint();
                                nnconstraint.area = (int)__instance.AreaIndex;
                                nnconstraint.constrainArea = true;
                                nnconstraint.constrainWalkability = true;
                                GraphNode node = pgeforTLIAndTransform.Graph.GetNearest(randomTarget, nnconstraint).node;
                                if (node != null)
                                {
                                    __instance.NextMainPathPos = node;
                                }
                                else
                                {
                                    foreach (GraphNode graphNode in __instance.nodesInArea_Cached)
                                    {
                                        if (__instance.NextMainPathPos == null && UnityEngine.Random.Range(0, 3) == 0 && !__instance.visitedNodes.Contains(graphNode.position))
                                        {
                                            float num2 = Vector3.SqrMagnitude((Vector3)graphNode.position - __instance.transform.position);
                                            if (num2 < num)
                                            {
                                                num = num2;
                                                nextMainPathPos = graphNode;
                                            }
                                        }
                                    }
                                    __instance.NextMainPathPos = nextMainPathPos;
                                }
                                if (__instance.NextMainPathPos != null && !__instance.pathRequestInProgress)
                                {
                                    foreach (Light light in __instance.MyLights)
                                    {
                                        if (light != null)
                                        {
                                            light.color = Color.green;
                                        }
                                    }
                                    __instance.pathRequestInProgress = true;
                                    __instance.seeker.StartPath(__instance.transform.position, (Vector3)__instance.NextMainPathPos.position, new OnPathDelegate(__instance.OnPathComplete));
                                }
                            }
                            else if (__instance.currentPath == null || __instance.currentPath.vectorPath == null || __instance.currentPath.vectorPath.Count == 0 || __instance.currentPathIndex >= __instance.currentPath.vectorPath.Count)
                            {
                                __instance.NextMainPathPos = null;
                            }
                        }
                    }
                }
            }
            yield break;
        }
    }
    [HarmonyPatch(typeof(PLBoardingBot), "FireShot")]
    class PatrolBotShot 
    {
        static bool Prefix(PLBoardingBot __instance, Vector3 aimAtPoint, Vector3 destNormal, int newBoltID, Collider hitCollider) 
        {
            if (!__instance.name.Contains("(frienddrone)")) return true;
            GameObject gameObject = __instance.CreateBoltGO();
            PLBolt component = gameObject.GetComponent<PLBolt>();
            component.DamageDone = 30f + Mod.PatrolBotsLevel *5;
            if (PLServer.Instance != null)
            {
                component.DamageDone += PLServer.Instance.ChaosLevel * 6f;
            }
            component.ProjSpeed = 50f;
            component.Setup(__instance.transform.position, aimAtPoint, destNormal, Vector3.zero, null, __instance, newBoltID, hitCollider);
            Transform[] componentsInChildren = gameObject.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = __instance.gameObject.layer;
            }
            PLMusic.PostEvent("play_sx_npc_investigator_shoot", __instance.gameObject);
            __instance.LastShotFiredTime = Time.time;
            return false;
        }
    }
    [HarmonyPatch(typeof(PLBot), "Update")]
    class GiveControllerBot
    {
        static void Postfix(PLBot __instance)
        {
            if (__instance.MyBotController != null)
            {
                __instance.MyBotController.MyBot = __instance;
                if (PLEncounterManager.Instance.PlayerShip != null && __instance.AI_TargetTLI == PLEncounterManager.Instance.PlayerShip.MyTLI && Command.shipAssembled)
                {

                    PLPathfinderGraphEntity targetInterior = PLPathfinder.GetInstance().GetPGEforTLIAndPosition(PLEncounterManager.Instance.PlayerShip.MyTLI, __instance.MyBotController.Assigned_AI_TargetPos);
                    //PulsarModLoader.Utilities.Messaging.Notification("Current: " + __instance.PlayerOwner.MyPGE.ID + ", Target: " + targetInterior.ID);
                    //PulsarModLoader.Utilities.Messaging.Notification("Target Position: " + __instance.MyBotController.Assigned_AI_TargetPos);
                    if (__instance.PlayerOwner.MyPGE != null && targetInterior.ID != __instance.PlayerOwner.MyPGE.ID)
                    {
                        if (targetInterior.ID < __instance.PlayerOwner.MyPGE.ID) //Bot is in brige but wants to go to main area 
                        {
                            float distance = (new Vector3(434.2143f, -430.2697f, 1730.034f) - __instance.AI_TargetPos).magnitude;
                            int doorID = 0;
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
                                        Physics.SyncTransforms();
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
                                        Physics.SyncTransforms();
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
                                        Physics.SyncTransforms();
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
                                        Physics.SyncTransforms();
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
                    //PulsarModLoader.Utilities.Messaging.Notification("New Target Position: " + __instance.MyBotController.Assigned_AI_TargetPos);
                }
            }
        }
    }
}
