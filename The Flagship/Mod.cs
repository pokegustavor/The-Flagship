using System.Collections.Generic;
using PulsarModLoader;
using PulsarModLoader.Chat.Commands.CommandRouter;
using UnityEngine;
using HarmonyLib;
using System.Diagnostics;
namespace The_Flagship
{
    public class Mod : PulsarMod
    {
        public override string Version => "1.0";

        public override string Author => "pokegustavo";

        public override string ShortDescription => "Adds the flagship as a playable ship.";

        public override string Name => "The Flagship";

        public override string HarmonyIdentifier()
        {
            return "pokegustavo.theflagship";
        }
    }

    public class testCommand : ChatCommand
    {
        public override string[] CommandAliases()
        {
            return new string[]
            {
                "test"
            };
        }

        public override string Description()
        {
            return "Test command";
        }

        public override void Execute(string arguments)
        {
            PLShipInfo ship = PLEncounterManager.Instance.PlayerShip;
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
                    GameObject newexterior = Object.Instantiate(exterior, exterior.transform.position + new Vector3(0f, 5000f, 0f), exterior.transform.rotation);
                    newexterior.AddComponent<PLPilotingSystem>();
                    newexterior.GetComponent<PLPilotingSystem>().MyShipInfo = ship;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint = currentpisys.ActivationPoint;
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
                    newexterior.transform.SetParent(ship.ShipRoot.transform);
                    newexterior.layer = currentexterior.layer;
                    Object.DontDestroyOnLoad(newexterior);
                    ship.ExteriorRigidbody = newexterior.GetComponent<Rigidbody>();
                    ship.ExteriorMeshRenderer = newexterior.GetComponent<MeshRenderer>();
                    ship.Exterior = newexterior;
                    ship.OrbitCameraMinDistance = 920f;
                    ship.OrbitCameraMaxDistance = 1550f;
                    ship.WarpBlocker.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.SetParent(newexterior.transform);
                    ship.WarpObject.transform.localPosition = Vector3.zero;
                    ship.WarpBlocker.transform.localPosition = Vector3.zero;
                    ship.MainTurretPoint.transform.SetParent(newexterior.transform);
                    ship.MainTurretPoint.transform.localPosition = new Vector3(0, 171.563f, -478.4174f);
                    ship.MainTurretPoint.transform.localScale = Vector3.one * 2.5f;
                    ship.MainTurretPoint.transform.localRotation = new Quaternion(0, 0, 0, 0);
                    ship.RegularTurretPoints[0].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[0].transform.localPosition = new Vector3(-75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[0].transform.localScale = Vector3.one * 2.5f;
                    ship.RegularTurretPoints[0].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    ship.RegularTurretPoints[1].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[1].transform.localPosition = new Vector3(75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[1].transform.localScale = Vector3.one * 2.5f;
                    ship.RegularTurretPoints[1].transform.localRotation = new Quaternion(0, 0, 0, 0);
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
                    currentexterior.transform.SetParent(newexterior.transform,false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PLShipInfoBase), "SetShipControllerID")]
    class test
    {
        static void Postfix(int inID)
        {
            StackTrace trace = new StackTrace();
            foreach (StackFrame stackFrame in trace.GetFrames())
            {
                PulsarModLoader.Utilities.Messaging.Notification(stackFrame.GetMethod().DeclaringType + ": " + stackFrame.GetMethod().Name);
            }

        }
    }

    [HarmonyPatch(typeof(PLShipInfo),"Update")]
    class WarpBlockedFix 
    {
        static void Postfix(PLShipInfo __instance, Color ___WarpObjectCurColor) 
        {
            if(__instance.WarpBlocker != null) 
            {
                __instance.WarpBlocker.transform.localScale = Vector3.one * (12000f - ___WarpObjectCurColor.a * 6000f);
            }
        }
    }
}
