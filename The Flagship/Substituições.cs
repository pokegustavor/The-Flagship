using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;
using PulsarModLoader;
using System.Threading.Tasks;
using static PLBurrowArena;
using Pathfinding;
using UnityEngine.UI;
using System.Linq;
using PulsarModLoader.Utilities;

namespace The_Flagship
{
    /*
    public class PLFlagship : PLOldWarsShip_Human
    {
        public override string GetShipTypeName()
        {
            return "Flagship";
        }
        public override string GetShipShortDesc()
        {
            return "Restored Capital Class Vessel";
        }
        public override string GetShipAttributes()
        {
            return "Nnockback Resistance\n+100% Reactor Output\n+10x Oxygen Reffil\n<color=red>Cannot use repair stations or warp gates</color>";
        }
        public override void ShipFinalCalculateStats(ref PLShipStats inStats)
        {
            base.ShipFinalCalculateStats(ref inStats);
            inStats.ReactorOutputFactor *= 2;
            inStats.OxygenRefillRate *= 10;
            inStats.HullArmor *= ShipStats.armorModifier;
            inStats.ReactorTotalOutput += PLAutoRepairScreen.powerusage;
        }
    }
    */
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
            if (__instance.GetIsPlayerShip() && Command.shipAssembled) __result = "Nnockback Resistance\n+100% Reactor Output\n10x Oxygen Reffil\n<color=red>Cannot use repair stations or warp gates</color>\n<color=red>100x EM Signature</color>";
        }
    }
    [HarmonyPatch(typeof(PLShipInfo), "ShipFinalCalculateStats")]
    public class ShipStats
    {
        public static float armorModifier = 1f;
        static void Postfix(PLShipInfo __instance, ref PLShipStats inStats)
        {
            if (__instance.GetIsPlayerShip() && Command.shipAssembled && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
                inStats.ReactorOutputFactor *= 2;
                inStats.OxygenRefillRate *= 10;
                inStats.HullArmor *= armorModifier;
                inStats.ReactorTotalOutput += PLAutoRepairScreen.powerusage;
                inStats.EMSignature *= 100;
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
                __instance.ShipInfo.ExteriorRigidbody.angularVelocity = Vector3.zero;
                __instance._rigidbody.AddTorque(__instance.InputTorque * __instance.rotationSpeed * (__instance.IsBoosting ? 1.32f : 1f));

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
    [HarmonyPatch(typeof(PLNetworkManager), "OnServerCreatedRoom")]
    class OnCreateGame 
    {
        static void Postfix() 
        {
            if (PhotonNetwork.isMasterClient)
            {
                string savedoptions = PLXMLOptionsIO.Instance.CurrentOptions.GetStringValue("flagship");
                if (savedoptions != string.Empty)
                {
                    Mod.AutoAssemble = bool.Parse(savedoptions);
                }
                if (Mod.AutoAssemble)
                {
                    OnJoin.AutoAssemble();
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLGlobal), "EnterNewGame")]
    public class OnJoin
    {
        static void Prefix()
        {
            if(!PhotonNetwork.isMasterClient)
            {
                ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.sendRPC", PhotonNetwork.masterClient, new object[0]);
            }
        }
        public static async void AutoAssemble(bool fromFile = false)
        {
            while (PLEncounterManager.Instance.PlayerShip == null) await Task.Yield();
            //if (PLServer.GetCurrentSector() != null && (!fromFile && (PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.COLONIAL_HUB || PLServer.Instance.CurrentCrewLevel > 1))) return;
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, 72);
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
                Command.FabricateFlagship();
            }
        }
    }
    public class sendRPC : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (Command.shipAssembled)
            {
                SendRPC("pokegustavo.theflagship", "The_Flagship.RPCReciever", sender.sender, new object[0]);
                SendRPC("pokegustavo.theflagship", "The_Flagship.UpgradePatrolCurrentReciever", sender.sender, new object[] { Mod.PatrolBotsLevel});
            }
        }
    }
    public class RPCReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            if (sender.sender == PhotonNetwork.masterClient)
            {
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
    public class AutoRepairReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            PLAutoRepairScreen.Online = (bool)arguments[0];
            PLAutoRepairScreen.CurrentMultiplier = (float)arguments[1];
        }
    }
    [HarmonyPatch(typeof(PLServer), "NotifyPlayerStart")]
    class MeshEnable
    {
        static void Postfix()
        {
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
            if (ship != null && ship.ShipTypeID == EShipType.OLDWARS_HUMAN)
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
        static float lastPrisionSync = Time.time;
        static void Prefix(PLShipInfo __instance) 
        {
            if (__instance.EndGameSequenceActive)
            {
                __instance.InteriorShipLights.RemoveAll((Light light) => light == null);
            }
        }
        static void Postfix(PLShipInfo __instance, Color ___WarpObjectCurColor)
        {
            if (__instance.WarpBlocker != null)
            {
                __instance.WarpBlocker.transform.localScale = Vector3.one * (12000f - ___WarpObjectCurColor.a * 6000f);
            }
            if (!__instance.ShowingExterior && __instance.GetIsPlayerShip() && (PLNetworkManager.Instance.MyLocalPawn == null || PLNetworkManager.Instance.MyLocalPawn.CurrentShip == __instance) && Command.shipAssembled && __instance.ShipTypeID == EShipType.OLDWARS_HUMAN)
            {
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
                if (PhotonNetwork.isMasterClient && Time.time - lastPrisionSync > 2)
                {
                    ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.prisionRPC", PhotonTargets.Others, new object[] { Command.playersArrested });
                    lastPrisionSync = Time.time;
                }
            }
        }
    }
    /*
    [HarmonyPatch(typeof(PLShipInfo), "OnSysInstValueChanged")]
    class SystemBar 
    {
        static void Postfix(int inID, PLSysIntUISlider slider, float value) 
        {
            Text description = slider.gameObject.GetComponent<Text>();
            if (description.text.Contains("\n")) description.text = description.text.Remove(description.text.IndexOf("\n"));
            description.text += $"\n({Mathf.FloorToInt(value * 100)}%)";
        }
    }
    */
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
    [HarmonyPatch(typeof(PLInfectedSporeProj), "Explode")]
    class InfectedSporeRework 
    {
        static void Postfix(PLInfectedSporeProj __instance) 
        {
            if(__instance.ShipAttachedTo == PLEncounterManager.Instance.PlayerShip && Command.shipAssembled) 
            {
                __instance.Speed = 0;
                __instance.MyRigidbody.velocity = Vector3.zero;
            }
        }
    }
    [HarmonyPatch(typeof(PLInfectedSporeProj),"FixedUpdate")]
    class SporeSpawnBuff 
    {
        static void Prefix(PLInfectedSporeProj __instance) 
        {
            if (Command.shipAssembled && __instance.ShipAttachedTo == PLEncounterManager.Instance.PlayerShip) 
            {
                if (__instance.ShipAttachedTo != null && __instance.Attached && UnityEngine.Random.Range(0, 100) == 0 && PhotonNetwork.isMasterClient && __instance.SpawnedSpiders < 30 && __instance.ShipAttachedTo.MyTLI != null)
                {
                    PLPathfinderGraphEntity pgeforShip = PLPathfinder.GetInstance().GetPGEforShip(__instance.ShipAttachedTo);
                    if (pgeforShip != null)
                    {
                        NNConstraint nnconstraint = new NNConstraint();
                        nnconstraint.constrainWalkability = true;
                        nnconstraint.walkable = true;
                        nnconstraint.graphMask = PLBot.GetContraintForPGE(ref nnconstraint, pgeforShip).graphMask;
                        nnconstraint.area = (int)pgeforShip.LargestAreaIndex;
                        nnconstraint.constrainArea = true;
                        Vector3 position = new Vector3(UnityEngine.Random.Range(pgeforShip.Graph.forcedBounds.min.x, pgeforShip.Graph.forcedBounds.max.x), UnityEngine.Random.Range(pgeforShip.Graph.forcedBounds.min.y, pgeforShip.Graph.forcedBounds.max.y), UnityEngine.Random.Range(pgeforShip.Graph.forcedBounds.min.z, pgeforShip.Graph.forcedBounds.max.z));
                        NNInfoInternal nearest = pgeforShip.Graph.GetNearest(position, nnconstraint);
                        if (nearest.node != null && nearest.node.Area == pgeforShip.LargestAreaIndex)
                        {
                            Ray ray = new Ray((Vector3)nearest.node.position, Vector3.up);
                            RaycastHit raycastHit = default(RaycastHit);
                            if (Physics.Raycast(ray, out raycastHit, 15f, 2048) && Vector3.Dot(raycastHit.normal, Vector3.up) < 0f)
                            {
                                string type = "NetworkPrefabs/Infected_Spider_01";
                                if (UnityEngine.Random.Range(0, 10) == 0) type = "NetworkPrefabs/Infected_Spider_02";
                                PLInfectedSpider component = PhotonNetwork.Instantiate(type, raycastHit.point + Vector3.up, Quaternion.identity, 0, null).GetComponent<PLInfectedSpider>();
                                PLInfectedSpider_Medium component2 = PhotonNetwork.Instantiate(type, raycastHit.point + Vector3.up, Quaternion.identity, 0, null).GetComponent<PLInfectedSpider_Medium>();
                                if (component != null)
                                {
                                    component.MyCurrentTLI = __instance.ShipAttachedTo.MyTLI;
                                }
                                if(component2 != null) 
                                {
                                    component2.MyCurrentTLI = __instance.ShipAttachedTo.MyTLI;
                                }
                                __instance.SpawnedSpiders++;
                            }
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLInfectedSpider),"Update")]
    class CrawlerOutOfBounds 
    {
        static void Postfix(PLInfectedSpider __instance) 
        {
            if(__instance.transform.position.y < -5000 && !__instance.IsDead) 
            {
                __instance.OnDeath();
            }
        }
    }
    [HarmonyPatch(typeof(PLInfectedSpider_Medium), "Update")]
    class BigCrawlerOutOfBounds
    {
        static void Postfix(PLInfectedSpider __instance)
        {
            if (__instance.transform.position.y < -5000 && !__instance.IsDead)
            {
                __instance.OnDeath();
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
        static void Postfix(PLCombatTarget __instance, ref bool __result)
        {
            if (__instance is PLBoardingBot && __instance.name.Contains("(frienddrone)"))
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
            if(__instance.CurrentShip == null) 
            {
                PhotonNetwork.Destroy(__instance.gameObject);
                return false;
            }
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
                    if (combatTarget != null && !(combatTarget is PLPawn) && !combatTarget.IsDead && !Physics.Linecast(__instance.transform.position, combatTarget.transform.position + Vector3.up * 1.5f, out _) && !combatTarget.GetIsFriendly())
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
                    else if (lCombatTarget != null)
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
    class DroneReciever : ModMessage
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            foreach (PLBoardingBot bot in UnityEngine.Object.FindObjectsOfType<PLBoardingBot>(true))
            {
                if (bot.photonView.instantiationId == (int)arguments[0])
                {
                    if (!bot.name.Contains("(frienddrone)")) bot.name += " (frienddrone)";
                    foreach (Light light in bot.MyLights)
                    {
                        if (light != null)
                        {
                            light.color = new Color((float)arguments[1], (float)arguments[2], (float)arguments[3], (float)arguments[4]);
                            light.enabled = (bool)arguments[5];
                        }
                    }
                    break;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLBoardingBot), "Update")]
    class PatrolBotUpdate
    {
        static float LastSync = 0;
        static void RequestSync()
        {
            foreach (PLBoardingBot bot in UnityEngine.Object.FindObjectsOfType<PLBoardingBot>(true))
            {
                if (bot.name.Contains("(frienddrone)"))
                {
                    if (PhotonNetwork.isMasterClient) ModMessage.SendRPC("pokegustavo.theflagship", "The_Flagship.DroneReciever", PhotonTargets.Others, new object[]
                    {
                    bot.photonView.instantiationId,
                    bot.MyLights[0].color.r,
                    bot.MyLights[0].color.g,
                    bot.MyLights[0].color.b,
                    bot.MyLights[0].color.a,
                    bot.MyLights[0].enabled
                    });
                }
            }

        }
        static bool Prefix(PLBoardingBot __instance)
        {
            if (!__instance.name.Contains("(frienddrone)")) return true;
            float expectedMaxH = 155 + Mod.PatrolBotsLevel * 25;
            if (PhotonNetwork.isMasterClient && Time.time - LastSync > 10)
            {
                LastSync = Time.time;
                RequestSync();
            }
            if (__instance.MaxHealth != expectedMaxH)
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
            //PLCombatTarget Update
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
            //Over
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
                    __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, Quaternion.Euler(new Vector3(90, 0, 0)), Mathf.Clamp01(Time.deltaTime));
                    if (Time.time - __instance.LastHeadDamageTakenTime > 30 && __instance.IsDead)
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
                        __instance.transform.position = Vector3.Lerp(__instance.transform.position, __instance.targetPos + new Vector3(0f, Mathf.Sin(Time.time) * 0.4f, 0f), Mathf.Clamp01(Time.deltaTime * 2 * (1f + 0.2f * Mod.PatrolBotsLevel)));
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
                        if (combatTarget != null && (__instance.currentPath == null || ((__instance.currentPath.vectorPath[__instance.currentPath.vectorPath.Count - 1] + Vector3.up * 1.5f) - combatTarget.transform.position).magnitude > 16))
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
                }
                catch { }
                List<PLMainSystem> damagedStuff = new List<PLMainSystem>();
                float distanceToSystem = float.MaxValue;
                Transform targetSystem = null;
                if (__instance.CurrentShip.EngineeringSystem.Health < __instance.CurrentShip.EngineeringSystem.MaxHealth)
                {
                    damagedStuff.Add(__instance.CurrentShip.EngineeringSystem);
                }
                if (__instance.CurrentShip.ComputerSystem.Health < __instance.CurrentShip.ComputerSystem.MaxHealth)
                {
                    damagedStuff.Add(__instance.CurrentShip.ComputerSystem);
                }
                if (__instance.CurrentShip.WeaponsSystem.Health < __instance.CurrentShip.WeaponsSystem.MaxHealth)
                {
                    damagedStuff.Add(__instance.CurrentShip.WeaponsSystem);
                }
                if (__instance.CurrentShip.LifeSupportSystem.Health < __instance.CurrentShip.LifeSupportSystem.MaxHealth)
                {
                    damagedStuff.Add(__instance.CurrentShip.LifeSupportSystem);
                }
                if ((__instance.MyLights[0].color == Color.green || __instance.MyLights[0].color == Color.white) && __instance.pathRequestInProgress == false && damagedStuff.Count > 0)
                {
                    foreach (PLMainSystem system in damagedStuff)
                    {
                        if ((system.MyInstance.transform.position - __instance.transform.position).magnitude < distanceToSystem)
                        {
                            distanceToSystem = (system.MyInstance.transform.position - __instance.transform.position).magnitude;
                            targetSystem = system.MyInstance.transform;
                        }
                    }
                    if (targetSystem != null && (__instance.MyLights[0].color == Color.green || (__instance.MyLights[0].color == Color.white && ((__instance.currentPath.vectorPath[__instance.currentPath.vectorPath.Count - 1] + Vector3.up * 1.5f) - targetSystem.position).magnitude > 16)))
                    {
                        NNConstraint nnconstraint = new NNConstraint();
                        nnconstraint.area = (int)__instance.AreaIndex;
                        nnconstraint.constrainArea = true;
                        nnconstraint.constrainWalkability = true;
                        Vector3 targetPos = (Vector3)pgeforTLIAndTransform.Graph.GetNearest(targetSystem.position, nnconstraint).node.position;
                        //PulsarModLoader.Utilities.Messaging.Notification("Target pos: " + targetPos);
                        __instance.seeker.StartPath(__instance.transform.position, targetPos, new OnPathDelegate(__instance.OnPathComplete));
                        foreach (Light light in __instance.MyLights)
                        {
                            if (light != null)
                            {
                                light.color = Color.white;
                            }
                        }
                        __instance.pathRequestInProgress = true;
                    }
                }
                foreach (PLMainSystem system in damagedStuff)
                {
                    if ((system.MyInstance.transform.position - __instance.transform.position).magnitude < 8)
                    {
                        system.Health += (30 + 5 * Mod.PatrolBotsLevel) * Time.deltaTime;
                        system.Health = Mathf.Clamp(system.Health, 0, system.MaxHealth);
                        system.MyInstance.LastRepairActiveTime = Time.time;
                        break;
                    }
                }
                if (__instance.currentPath == null)
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
            else
            {
                __instance.transform.position = Vector3.Lerp(__instance.transform.position, __instance.LastNetPos, Mathf.Clamp01(Time.deltaTime * 5f * 2 * (1f + 0.2f * Mod.PatrolBotsLevel)));
                __instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, __instance.LastNetRot, Mathf.Clamp01(Time.deltaTime * 8f));
            }
            foreach (PLFire fire in __instance.CurrentShip.AllFires.Values)
            {
                if ((fire.transform.position - __instance.transform.position).magnitude < 8)
                {
                    fire.Intensity -= Time.deltaTime * 2;
                }
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
                                Vector3 randomTarget = new Vector3(UnityEngine.Random.Range(249, 480), UnityEngine.Random.Range(-426, -345), UnityEngine.Random.Range(1337, 1795));
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
            component.DamageDone = 30f + Mod.PatrolBotsLevel * 5;
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
    [HarmonyPatch(typeof(PLReactor), "ShipUpdate")]
    class PowerUsage
    {
        static bool Prefix(PLShipInfoBase inShipInfo, PLReactor shipReactor)
        {
            if (inShipInfo != null && inShipInfo.ShipTypeID == EShipType.OLDWARS_HUMAN && inShipInfo == PLEncounterManager.Instance.PlayerShip && Command.shipAssembled)
            {
                PLPlayer cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(4, inShipInfo);
                if (cachedFriendlyPlayerOfClass == null || inShipInfo.GetIsPlayerShip())
                {
                    cachedFriendlyPlayerOfClass = PLServer.Instance.GetCachedFriendlyPlayerOfClass(4);
                }
                float num = 0f;
                if (inShipInfo.ReactorCoolantLevelPercent > 0f)
                {
                    num = Mathf.Pow((float)inShipInfo.ReactorCoolingPumpState, 2f) * 0.2f;
                    float num2 = 1f;
                    if (cachedFriendlyPlayerOfClass != null)
                    {
                        num2 = 1f - (float)cachedFriendlyPlayerOfClass.Talents[20] * 0.08f;
                    }
                    if (inShipInfo == PLAbyssShipInfo.Instance)
                    {
                        num2 *= 0.5f;
                    }
                    inShipInfo.ReactorCoolantLevelPercent -= num * 0.0075f * Time.deltaTime * num2;
                }
                float num3 = 1f;
                if (cachedFriendlyPlayerOfClass != null)
                {
                    num3 *= 1f - (float)cachedFriendlyPlayerOfClass.Talents[46] * 0.02f;
                }
                float num4 = 0.55f;
                if (inShipInfo.Reactor_OCActive)
                {
                    num4 *= 0.1f;
                    num3 *= 1.2f;
                }
                float num5;
                if (inShipInfo.MyStats.ReactorBoostedOutputMax > 250f && shipReactor != null)
                {
                    num5 = (Mathf.Clamp01(inShipInfo.MyStats.ReactorTotalUsagePercent) * shipReactor.HeatOutput * inShipInfo.MyStats.ReactorHeatOutputFactor * num3 - (num4 + num * 1.5f)) * 135f;
                }
                else
                {
                    num5 = -15f;
                }
                inShipInfo.MyStats.ReactorTempVel = Mathf.Clamp(inShipInfo.MyStats.ReactorTempVel, -30f, 75f);
                inShipInfo.MyStats.ReactorTempVel = Mathf.Lerp(inShipInfo.MyStats.ReactorTempVel, num5, Time.deltaTime * 0.2f);
                inShipInfo.MyStats.ReactorTempCurrent += (num5 + inShipInfo.MyStats.ReactorTempVel * 0.1f) * Time.deltaTime;
                inShipInfo.MyStats.ReactorTempCurrent = Mathf.Clamp(inShipInfo.MyStats.ReactorTempCurrent, inShipInfo.MyStats.ReactorTempMax * 0.07f, inShipInfo.MyStats.ReactorTempMax * 1.025f);
                if (inShipInfo.MyStats.ReactorTempCurrent >= inShipInfo.MyStats.ReactorTempMax && inShipInfo.CoreInstability > 0.05f && shipReactor != null && inShipInfo.ReactorCoolingEnabled && !inShipInfo.IsReactorOverheated() && PhotonNetwork.isMasterClient)
                {
                    PLServer.Instance.ServerShipOverheat(inShipInfo.ShipID);
                }
                int num6 = 0;
                List<PLPoweredShipComponent> allPoweredComponents = PLReactor.GetAllPoweredComponents(inShipInfo.MyStats);
                float num7 = inShipInfo.AuxOutputPowerAmount;
                if (shipReactor != null)
                {
                    num7 += shipReactor.CalcEnergyOutputMax();
                }
                if (cachedFriendlyPlayerOfClass != null)
                {
                    num7 *= 1f + (float)cachedFriendlyPlayerOfClass.Talents[45] * 0.02f;
                }
                num7 += Mathf.Pow(inShipInfo.DischargeAmount, 1.35f) * 40000f;
                inShipInfo.MyStats.ReactorBoostedOutputMax = (0.01f + num7) * inShipInfo.MyStats.ReactorOutputFactor;
                if (inShipInfo.Reactor_OCActive)
                {
                    if (inShipInfo.MyReactor != null)
                    {
                        inShipInfo.MyStats.ReactorBoostedOutputMax *= 1.5f;
                    }
                    else
                    {
                        inShipInfo.MyStats.ReactorBoostedOutputMax *= 1.2f;
                    }
                }
                foreach (PLPoweredShipComponent plpoweredShipComponent in allPoweredComponents)
                {
                    if (plpoweredShipComponent != null && plpoweredShipComponent.IsEquipped && plpoweredShipComponent.ActualSlotType == ESlotType.E_COMP_CPU && plpoweredShipComponent.SubType == 25)
                    {
                        inShipInfo.MyStats.ReactorBoostedOutputMax *= 1f + 0.03f * plpoweredShipComponent.LevelMultiplier(0.25f, 1f);
                    }
                }
                if (Time.time - inShipInfo.MyStats.LastReactorSetPowerLevelsTime > 0.5f)
                {
                    inShipInfo.MyStats.LastReactorSetPowerLevelsTime = Time.time;
                    for (int i = 0; i < 5; i++)
                    {
                        if (i != 4)
                        {
                            PLReactor.SetPowerLevels(inShipInfo.MyStats, i, inShipInfo.SystemPowerLevels[i]);
                        }
                        else
                        {
                            PLReactor.SetPowerLevels(inShipInfo.MyStats, 4, inShipInfo.ReactorTotalPowerLimitPercent);
                        }
                    }
                }
                PLShipInfo plshipInfo = inShipInfo as PLShipInfo;
                if (inShipInfo.IsReactorOverheated())
                {
                    inShipInfo.IsEmergencyCooling = true;
                    inShipInfo.MyStats.ReactorTotalUsagePercent = 0f;
                    inShipInfo.MyStats.ReactorTotalOutput = 0f;
                    using (List<PLPoweredShipComponent>.Enumerator enumerator = allPoweredComponents.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            PLPoweredShipComponent plpoweredShipComponent2 = enumerator.Current;
                            plpoweredShipComponent2.InputPower_Watts = 0f;
                            plpoweredShipComponent2.RequestPowerUsage_Limit = 0f;
                        }
                        return false;
                    }
                }
                if (plshipInfo != null && plshipInfo.StartupStepIndex < 1)
                {
                    inShipInfo.MyStats.ReactorTotalUsagePercent = 0f;
                    inShipInfo.MyStats.ReactorTotalOutput = 0f;
                    using (List<PLPoweredShipComponent>.Enumerator enumerator = allPoweredComponents.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            PLPoweredShipComponent plpoweredShipComponent3 = enumerator.Current;
                            plpoweredShipComponent3.InputPower_Watts = 0f;
                            plpoweredShipComponent3.RequestPowerUsage_Limit = 0f;
                        }
                        return false;
                    }
                }
                if (plshipInfo != null && plshipInfo.StartupSwitchBoard != null && !plshipInfo.StartupSwitchBoard.GetLateStatus(0))
                {
                    inShipInfo.MyStats.ReactorTotalUsagePercent = 0f;
                    inShipInfo.MyStats.ReactorTotalOutput = 0f;
                    using (List<PLPoweredShipComponent>.Enumerator enumerator = allPoweredComponents.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            PLPoweredShipComponent plpoweredShipComponent4 = enumerator.Current;
                            plpoweredShipComponent4.InputPower_Watts = 0f;
                            plpoweredShipComponent4.RequestPowerUsage_Limit = 0f;
                        }
                        return false;
                    }
                }
                inShipInfo.MyStats.ReactorTotalOutput += PLAutoRepairScreen.powerusage;
                inShipInfo.IsEmergencyCooling = false;
                if (inShipInfo.MyStats.ReactorBoostedOutputMax > 0.01f)
                {
                    inShipInfo.MyStats.ReactorTotalUsagePercent = inShipInfo.MyStats.ReactorTotalOutput / inShipInfo.MyStats.ReactorBoostedOutputMax;
                }
                else
                {
                    inShipInfo.MyStats.ReactorTotalUsagePercent = 0f;
                }
                foreach (PLShipComponent plshipComponent in inShipInfo.MyStats.GetComponentsOfType(ESlotType.E_COMP_REACTOR, false))
                {
                    if (typeof(PLReactor).IsAssignableFrom(plshipComponent.GetType()))
                    {
                        PLReactor plreactor = (PLReactor)plshipComponent;
                        if (plreactor != null && plreactor.isActive)
                        {
                            num6++;
                        }
                    }
                }
                float num8 = 0f;
                foreach (PLPoweredShipComponent plpoweredShipComponent5 in allPoweredComponents)
                {
                    if (plpoweredShipComponent5 != null && !plpoweredShipComponent5.InCargoSlot())
                    {
                        plpoweredShipComponent5.InternalSysInst_PowerLimitPercent = PLReactor.GetInternalSysInstPowerUsagePercentForComponent(plpoweredShipComponent5);
                    }
                }
                foreach (PLPoweredShipComponent plpoweredShipComponent6 in allPoweredComponents)
                {
                    if (plpoweredShipComponent6.IsPowerActive && !plpoweredShipComponent6.InCargoSlot())
                    {
                        float num9 = plpoweredShipComponent6.CalculatedMaxPowerUsage_Watts * Mathf.Min(new float[]
                        {
                    plpoweredShipComponent6.RequestPowerUsage_Limit,
                    plpoweredShipComponent6.RequestPowerUsage_Percent,
                    plpoweredShipComponent6.InternalSysInst_PowerLimitPercent
                        });
                        num8 += num9;
                    }
                }
                num8 += PLAutoRepairScreen.powerusage;
                float num10 = 1f;
                if (num8 > num7 * inShipInfo.ReactorTotalPowerLimitPercent)
                {
                    num10 = Mathf.Clamp01(inShipInfo.MyStats.ReactorBoostedOutputMax * inShipInfo.ReactorTotalPowerLimitPercent / num8);
                }
                inShipInfo.MyStats.ReactorTotalOutput = 0f;
                foreach (PLPoweredShipComponent plpoweredShipComponent7 in allPoweredComponents)
                {
                    if (plpoweredShipComponent7.IsPowerActive && !plpoweredShipComponent7.InCargoSlot())
                    {
                        float num11 = plpoweredShipComponent7.CalculatedMaxPowerUsage_Watts * Mathf.Min(new float[]
                        {
                    plpoweredShipComponent7.RequestPowerUsage_Limit,
                    plpoweredShipComponent7.RequestPowerUsage_Percent,
                    plpoweredShipComponent7.InternalSysInst_PowerLimitPercent
                        }) * num10;
                        plpoweredShipComponent7.InputPower_Watts = num11;
                        inShipInfo.MyStats.ReactorTotalOutput += num11;
                    }
                    else
                    {
                        plpoweredShipComponent7.InputPower_Watts = 0f;
                    }
                }
                inShipInfo.MyStats.ReactorTotalOutput += PLAutoRepairScreen.powerusage;
                inShipInfo.MyStats.ReactorTotalOutput = Mathf.Clamp(inShipInfo.MyStats.ReactorTotalOutput, 0f, inShipInfo.MyStats.ReactorBoostedOutputMax);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PLBot), "GetNearestEnemyTargetTransform")]
    class BotTarget
    {
        static void Postfix(PLBot __instance, ref PLCombatTarget outTarget, ref Transform __result, float rangeMultiplier = 1f)
        {
            float num = Mathf.Pow(140f * rangeMultiplier, 2f);
            Transform transform = null;
            foreach (PLCombatTarget plcombatTarget in PLGameStatic.Instance.AllCombatTargets)
            {
                if (plcombatTarget != null && __instance.PlayerOwner != null && __instance.PlayerOwner.GetPawn() != null && !plcombatTarget.IsDead && plcombatTarget.ShouldShowInHUD() && plcombatTarget.gameObject.activeInHierarchy)
                {
                    PLPawn plpawn = plcombatTarget as PLPawn;
                    UnityEngine.Object x = plcombatTarget as PLGroundTurret;
                    bool flag = false;
                    if (x != null)
                    {
                        flag = false;
                    }
                    else if (plpawn == null || (!plpawn.PreviewPawn && !plpawn.Cloaked))
                    {
                        flag = true;
                    }
                    if (flag && plcombatTarget.CurrentShip == __instance.PlayerOwner.GetPawn().CurrentShip && plcombatTarget.MyInterior == __instance.PlayerOwner.GetPawn().MyInterior && plcombatTarget.MyCurrentTLI == __instance.PlayerOwner.MyCurrentTLI && ((plcombatTarget.GetPlayer() == null && __instance.PlayerOwner.GetClassID() != -1 && __instance.PlayerOwner.TeamID == 0 && !plcombatTarget.GetIsFriendly()) || ((__instance.PlayerOwner.TeamID != 0 && plcombatTarget.GetIsFriendly()) || (plcombatTarget.GetPlayer() != null && plcombatTarget.GetPlayer() != __instance.PlayerOwner && plcombatTarget.GetPlayer().TeamID != __instance.PlayerOwner.TeamID))))
                    {
                        float num2 = (plcombatTarget.transform.position - __instance.PlayerOwner.GetPawn().transform.position).sqrMagnitude;
                        if (plcombatTarget == __instance.HighPriorityTarget)
                        {
                            num2 *= 0.2f;
                        }
                        if (num2 < num && (plcombatTarget == __instance.HighPriorityTarget || __instance.PlayerOwner.GetPawn().HadRecentLOSSuccessToTarget(plcombatTarget)))
                        {
                            num = num2;
                            transform = plcombatTarget.transform;
                            outTarget = plcombatTarget;
                        }
                    }
                }
            }
            if (__instance.PlayerOwner.TeamID == 0)
            {
                foreach (PLGroundTurret plgroundTurret in PLGameStatic.Instance.AllGroundTurrets)
                {
                    if (plgroundTurret != null && __instance.PlayerOwner != null && __instance.PlayerOwner.GetPawn() != null && !plgroundTurret.IsDead && plgroundTurret.ShouldShowInHUD() && plgroundTurret.Target != null && plgroundTurret.Target.GetIsFriendly() && ((plgroundTurret.GetPlayer() == null && plgroundTurret.CurrentShip == __instance.PlayerOwner.GetPawn().CurrentShip && plgroundTurret.MyInterior == __instance.PlayerOwner.GetPawn().MyInterior) || (plgroundTurret.GetPlayer() != null && plgroundTurret.GetPlayer() != __instance.PlayerOwner && plgroundTurret.GetPlayer().TeamID != __instance.PlayerOwner.TeamID && plgroundTurret.GetPlayer().SubHubID == __instance.PlayerOwner.SubHubID)))
                    {
                        float sqrMagnitude = (plgroundTurret.transform.position - __instance.PlayerOwner.GetPawn().transform.position).sqrMagnitude;
                        if (sqrMagnitude < num && sqrMagnitude > 1f && __instance.PlayerOwner.GetPawn().HadRecentLOSSuccessToTarget(plgroundTurret))
                        {
                            num = sqrMagnitude;
                            transform = plgroundTurret.transform;
                            outTarget = plgroundTurret;
                        }
                    }
                }
                foreach (PLFBVent plfbvent in PLGameStatic.Instance.AllFBVents)
                {
                    if (plfbvent != null && __instance.PlayerOwner != null && __instance.PlayerOwner.GetPawn() != null && !plfbvent.IsDead && plfbvent.ShouldTakeDamage() && ((plfbvent.GetPlayer() == null && plfbvent.CurrentShip == __instance.PlayerOwner.GetPawn().CurrentShip && plfbvent.MyInterior == __instance.PlayerOwner.GetPawn().MyInterior) || (plfbvent.GetPlayer() != null && plfbvent.GetPlayer() != __instance.PlayerOwner && plfbvent.GetPlayer().TeamID != __instance.PlayerOwner.TeamID && plfbvent.GetPlayer().SubHubID == __instance.PlayerOwner.SubHubID)))
                    {
                        float sqrMagnitude2 = (plfbvent.transform.position - __instance.PlayerOwner.GetPawn().transform.position).sqrMagnitude;
                        if (sqrMagnitude2 < num && sqrMagnitude2 > 1f && __instance.PlayerOwner.GetPawn().HadRecentLOSSuccessToTarget(plfbvent))
                        {
                            num = sqrMagnitude2;
                            transform = plfbvent.transform;
                            outTarget = plfbvent;
                        }
                    }
                }
            }
            if (transform == null && __instance.MyBotController != null && __instance.MyBotController.MyPawn != null && __instance.PlayerOwner != null && __instance.MyBotController.MyPawn.CurrentShip != null && __instance.MyBotController.MyPawn.CurrentShip.TeamID != __instance.PlayerOwner.TeamID)
            {
                foreach (PLSystemInstance plsystemInstance in __instance.MyBotController.MyPawn.CurrentShip.RepairableSystemInstances)
                {
                    if (plsystemInstance != null)
                    {
                        float sqrMagnitude3 = (plsystemInstance.transform.position - __instance.MyBotController.MyPawn.transform.position).sqrMagnitude;
                        if (sqrMagnitude3 < num)
                        {
                            num = sqrMagnitude3;
                            transform = plsystemInstance.transform;
                            outTarget = null;
                        }
                    }
                }
            }
            if (transform == null && __instance.HighPriorityTarget != null)
            {
                transform = __instance.HighPriorityTarget.transform;
                outTarget = __instance.HighPriorityTarget;
            }
            __result = transform;
        }
    }
    [HarmonyPatch(typeof(PLShipInfoBase),"Update")]
    internal class FighterAggro 
    {
        static List<PLShipInfoBase> warpingShips = new List<PLShipInfoBase>();
        static void Prefix(PLShipInfoBase __instance) 
        {
            if (__instance.name.Contains("(fighterBot)") && PLEncounterManager.Instance.PlayerShip != null)
            {
                if (__instance.HostileShips != null && __instance.HostileShips.Contains(PLEncounterManager.Instance.PlayerShip.ShipID))
                {
                    __instance.HostileShips.Remove(PLEncounterManager.Instance.PlayerShip.ShipID);
                }
                if (__instance.TargetShip == null) __instance.TargetShip = PLEncounterManager.Instance.PlayerShip.TargetShip;
                if (__instance.TargetShip == PLEncounterManager.Instance.PlayerShip || (__instance.TargetShip != null && __instance.TargetShip.name.Contains("(fighterBot)"))) __instance.TargetShip = null;
                if (__instance.TargetSpaceTarget == PLEncounterManager.Instance.PlayerShip || (__instance.TargetSpaceTarget != null && __instance.TargetSpaceTarget.name.Contains("(fighterBot)"))) __instance.TargetSpaceTarget = null;
                if (__instance.SpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.SpaceTargetID = -1;
                if (__instance.CaptainTargetedSpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.CaptainTargetedSpaceTargetID = -1;
            }
        }
        static void Postfix(PLShipInfoBase __instance) 
        {
            if (__instance.name.Contains("(fighterBot)") && PLEncounterManager.Instance.PlayerShip != null) 
            {
                if(__instance.GetLifetime() <= 1f && !warpingShips.Contains(__instance)) 
                {
                    PhaseAway(__instance);
                }
                if (__instance.HostileShips != null && __instance.HostileShips.Contains(PLEncounterManager.Instance.PlayerShip.ShipID))
                {
                    __instance.HostileShips.Remove(PLEncounterManager.Instance.PlayerShip.ShipID);
                }
                if (__instance.TargetShip == null) __instance.TargetShip = PLEncounterManager.Instance.PlayerShip.TargetShip;
                if (__instance.TargetShip == PLEncounterManager.Instance.PlayerShip || (__instance.TargetShip != null && __instance.TargetShip.name.Contains("(fighterBot)"))) __instance.TargetShip = null;
                if (__instance.TargetSpaceTarget == PLEncounterManager.Instance.PlayerShip || (__instance.TargetSpaceTarget != null && __instance.TargetSpaceTarget.name.Contains("(fighterBot)"))) __instance.TargetSpaceTarget = null;
                if (__instance.SpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.SpaceTargetID = -1;
                if (__instance.CaptainTargetedSpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.CaptainTargetedSpaceTargetID = -1;
            }
        }
        public static async void PhaseAway(PLShipInfoBase ship)
        {
            if (ship != null)
            {
                warpingShips.Add(ship);
                float StartedPhasing = Time.time;
                //GameObject.Instantiate(PLGlobal.Instance.PhasePS, ship.Exterior.transform.position, Quaternion.identity);
                GameObject gameObject = GameObject.Instantiate(PLGlobal.Instance.PhaseTrailPS, ship.Exterior.transform.position, Quaternion.identity);
                if (gameObject != null)
                {
                    PLPhaseTrail component = gameObject.GetComponent<PLPhaseTrail>();
                    if (component != null)
                    {
                        component.StartPos = PLEncounterManager.Instance.PlayerShip.Exterior.transform.position;
                        component.End = ship.Exterior.transform;
                    }
                }
                foreach (PLShipInfoBase plshipInfoBase in PLEncounterManager.Instance.AllShips.Values)
                {
                    if (plshipInfoBase != null && plshipInfoBase.MySensorObjectShip != null)
                    {
                        PLSensorObjectCacheData plsensorObjectCacheData = plshipInfoBase.MySensorObjectShip.IsDetectedBy_CachedInfo(ship, true);
                        if (plsensorObjectCacheData != null)
                        {
                            plsensorObjectCacheData.LastDetectedCheckTime = 0f;
                            plsensorObjectCacheData.IsDetected = false;
                        }
                    }
                }
                DelayedEndPhasePS(ship);
                MeshRenderer[] hullplanting = ship.HullPlatingRenderers;
                List<PLShipComponent> componentsOfType = ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_TURRET, false);
                componentsOfType.AddRange(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MAINTURRET, false));
                componentsOfType.AddRange(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_AUTO_TURRET, false));
                PLMusic.PostEvent("play_sx_ship_enemy_phasedrone_warp", ship.Exterior);
                while (Time.time - StartedPhasing < 1f)
                {
                    ship.MyStats.EMSignature = 0f;
                    ship.MyStats.CanBeDetected = false;
                    ship.Exterior.GetComponent<MeshRenderer>().enabled = false;
                    foreach (Renderer rend in hullplanting)
                    {
                        if (rend != null)
                        {
                            rend.enabled = false;
                        }
                    }
                    foreach (PLShipComponent comp in componentsOfType)
                    {
                        PLTurret turret = comp as PLTurret;
                        if (turret != null && turret.TurretInstance != null)
                        {
                            foreach (Renderer rend in turret.TurretInstance.MyMainRenderers)
                            {
                                rend.enabled = false;
                            }
                        }
                    }
                    if (ship is PLFluffyShipInfo || ship is PLFluffyShipInfo2)
                    {
                        if ((ship as PLFluffyShipInfo).MyVisibleBomb != null)
                        {
                            (ship as PLFluffyShipInfo).MyVisibleBomb.gameObject.SetActive(false);
                        }
                    }
                    ship.MyStats.ThrustOutputCurrent = 0f;
                    ship.MyStats.ManeuverThrustOutputCurrent = 0f;
                    ship.MyStats.InertiaThrustOutputCurrent = 0f;
                    if (ship.ExteriorMeshCollider != null)
                    {
                        ship.ExteriorMeshCollider.enabled = false;
                    }
                    await Task.Yield();
                }
                warpingShips.Remove(ship);
                ship.Exterior.GetComponent<MeshRenderer>().enabled = true;
                foreach (Renderer rend in hullplanting)
                {
                    if (rend != null)
                    {
                        rend.enabled = true;
                    }
                }
                componentsOfType = ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_TURRET, false);
                componentsOfType.AddRange(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_MAINTURRET, false));
                componentsOfType.AddRange(ship.MyStats.GetComponentsOfType(ESlotType.E_COMP_AUTO_TURRET, false));
                foreach (PLShipComponent comp in componentsOfType)
                {
                    PLTurret turret = comp as PLTurret;
                    if (turret != null && turret.TurretInstance != null)
                    {
                        foreach (Renderer rend in turret.TurretInstance.MyMainRenderers)
                        {
                            rend.enabled = true;
                        }
                    }
                }
                if (ship is PLFluffyShipInfo || ship is PLFluffyShipInfo2)
                {
                    if ((ship as PLFluffyShipInfo).MyVisibleBomb != null)
                    {
                        (ship as PLFluffyShipInfo).MyVisibleBomb.gameObject.SetActive(true);
                    }
                }
                if (ship.ExteriorMeshCollider != null)
                {
                    ship.ExteriorMeshCollider.enabled = true;
                }
                ship.MyStats.CanBeDetected = true;
                PLMusic.PostEvent("stop_sx_ship_enemy_phasedrone_warp", ship.Exterior);
            }
        }
        private static async void DelayedEndPhasePS(PLShipInfoBase ship)
        {
            await Task.Delay(1000);
            GameObject.Instantiate(PLGlobal.Instance.PhasePS, ship.Exterior.transform.position, Quaternion.identity);
        }
    }
    [HarmonyPatch(typeof(PLInGameUI), "SetShipAsTarget")]
    class SetFighterTarget 
    {
        static void Postfix(PLSpaceTarget target)
        {
            foreach(PLShipInfoBase ship in PLFighterScreen.fighterInstances) 
            {
                if(ship != null && ship.GetCurrentShipControllerPlayerID() == PLNetworkManager.Instance.LocalPlayerID) 
                {
                    ship.photonView.RPC("Captain_SetTargetShip", PhotonTargets.All, new object[]
                    {
                    target.SpaceTargetID
                    });
                    break;
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLShipInfoBase), "Captain_SetTargetShip")]
    class ConfirmFighterTarget 
    {
        static void Postfix(PLShipInfoBase __instance, int inShipID) 
        {
            if (__instance.name.Contains("(fighterBot)")) 
            {
                __instance.HostileShips.Add(inShipID);
                __instance.TargetShip = PLEncounterManager.Instance.GetShipFromID(inShipID);
            }
        }
    }
    [HarmonyPatch(typeof(PLTurret),"Tick")]
    class FighterTurretTarget 
    {
        static void Prefix(PLTurret __instance) 
        {
            if (__instance.ShipStats != null && __instance.ShipStats.Ship != null && __instance.ShipStats.Ship.name.Contains("(fighterBot)"))
            {
                if (__instance.ShipStats.Ship.HostileShips != null && __instance.ShipStats.Ship.HostileShips.Contains(PLEncounterManager.Instance.PlayerShip.ShipID))
                {
                    __instance.ShipStats.Ship.HostileShips.Remove(PLEncounterManager.Instance.PlayerShip.ShipID);
                }
                if (__instance.ShipStats.Ship.TargetShip == null) __instance.ShipStats.Ship.TargetShip = PLEncounterManager.Instance.PlayerShip.TargetShip;
                if (__instance.ShipStats.Ship.TargetShip == PLEncounterManager.Instance.PlayerShip || (__instance.ShipStats.Ship.TargetShip != null && __instance.ShipStats.Ship.TargetShip.name.Contains("(fighterBot)"))) __instance.ShipStats.Ship.TargetShip = null;
                if (__instance.ShipStats.Ship.SpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.ShipStats.Ship.SpaceTargetID = -1;
                if (__instance.ShipStats.Ship.CaptainTargetedSpaceTargetID == PLEncounterManager.Instance.PlayerShip.SpaceTargetID) __instance.ShipStats.Ship.CaptainTargetedSpaceTargetID = -1;
                if (__instance.ShipStats.Ship.TargetSpaceTarget == PLEncounterManager.Instance.PlayerShip || (__instance.ShipStats.Ship.TargetSpaceTarget != null && __instance.ShipStats.Ship.TargetSpaceTarget.name.Contains("(fighterBot)"))) __instance.ShipStats.Ship.TargetSpaceTarget = null;
                if (__instance.Targeted_SpaceTarget != null && (__instance.Targeted_SpaceTarget == PLEncounterManager.Instance.PlayerShip || __instance.Targeted_SpaceTarget.name.Contains("(fighterBot)")))
                {
                    int ID = __instance.Targeted_SpaceTarget.SpaceTargetID;
                    if (__instance.SpaceTargetIDsTargeted.Contains(ID))
                    {
                        __instance.SpaceTargetIDsTargeted.Remove(ID);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(PLServer), "ClientGetUpdatedComponent")]
    class ClientInventorySize 
    {
        static void Prefix(int inShipID) 
        {
            PLShipInfoBase ship = PLEncounterManager.Instance.GetShipFromID(inShipID);
            if(ship != null && ship == PLEncounterManager.Instance.PlayerShip && ship.ShipTypeID == EShipType.OLDWARS_HUMAN) 
            {
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, 72);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 12);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_TURRET, 6);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_THRUSTER, 9);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_INERTIA_THRUSTER, 8);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_MANEUVER_THRUSTER, 6);
                PLEncounterManager.Instance.PlayerShip.MyStats.SetSlotLimit(ESlotType.E_COMP_SENS, 4);
            }
        }
    }
    [HarmonyPatch(typeof(PLOldWarsShip_Human), "SetupShipStats")]
    class InitialSpaceOnLoad 
    {
        static void Postfix(PLOldWarsShip_Human __instance) 
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
    [HarmonyPatch(typeof(PLShipInfoBase),"OnWarp")]
    class OnWarp 
    {
        static void Postfix(PLShipInfoBase __instance) 
        {
            if (__instance.GetIsPlayerShip() && PhotonNetwork.isMasterClient) 
            {
                foreach(PLShipInfoBase ship in PLEncounterManager.Instance.AllShips.Values) 
                {
                    if (ship != null && ship.name.Contains("(fighterBot)") && ship.MyStats != null) 
                    {
                        PLServer.Instance.CurrentUpgradeMats += Mathf.CeilToInt(20 * (ship.MyStats.HullCurrent / ship.MyStats.HullMax));
                        ship.PersistantShipInfo.IsShipDestroyed = true;
                    }
                }
            }
        }
    }

}
