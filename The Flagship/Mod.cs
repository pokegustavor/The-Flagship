using System.Collections.Generic;
using PulsarModLoader;
using PulsarModLoader.Chat.Commands.CommandRouter;
using UnityEngine;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
            FabricateFlagship();
        }

        public void MoveObjAndChild(Transform transform,int layer) 
        {
            foreach(Transform child in transform) 
            {
                MoveObjAndChild(child, layer);
            }
            transform.gameObject.layer = layer;
            if (transform.gameObject.name.ToLower().Contains("ff_infected_mesh") || transform.gameObject.name.ToLower().Contains("if_stem") || transform.gameObject.name.ToLower().Contains("infected_bark") || transform.gameObject.tag == "lava" || transform.gameObject.name.ToLower().Contains("ff_infected_small")
                || transform.gameObject.name.ToLower().Contains("if_mound") || transform.gameObject.name.ToLower().Contains("infected_pool") || transform.gameObject.name.ToLower().Contains("terrain") || transform.gameObject.name.ToLower().Contains("infectedasset") || transform.gameObject.name.ToLower().Contains("infected_deco")
                || transform.gameObject.name.ToLower().Contains("infected_tunnel") || transform.gameObject.name.ToLower().Contains("infected_buddle") || transform.gameObject.name.ToLower().Contains("if_bark") || transform.gameObject.name.ToLower().Contains("missionnpc") || transform.gameObject.name.ToLower().Contains("captainchair")
                || transform.gameObject.name.ToLower().Contains("infectedsurface") || transform.gameObject.name.ToLower().Contains("missionvolume") || transform.gameObject.name.ToLower().Contains("if_splatter") || transform.gameObject.name.ToLower().Contains("if_bubble") || transform.gameObject.name.ToLower().Contains("fs_rot")
                || transform.gameObject.name.ToLower().Contains("spawns") || transform.gameObject.name.ToLower().Contains("snowps") || transform.gameObject.name.ToLower().Contains("particle system") || transform.gameObject.name.Split(' ')[0] == "Plane" || transform.gameObject.name.ToLower().Contains("rot_")) 
            {
                transform.gameObject.tag = "Projectile";
            }
            if (transform.name.ToLower().Contains("light")) 
            {
                transform.gameObject.SetActive(true);
            }
        }

        async void FabricateFlagship() 
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(67, LoadSceneMode.Additive);
            AsyncOperation op2 = SceneManager.LoadSceneAsync(105, LoadSceneMode.Additive);
            while(!op.isDone || !op2.isDone) 
            {
                await Task.Yield();
            }
            Scene Estate = SceneManager.GetSceneByBuildIndex(67);
            Scene Flagship = SceneManager.GetSceneByBuildIndex(105);
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
                    GameObject newexterior = Object.Instantiate(exterior, currentexterior.transform.position, exterior.transform.rotation);
                    newexterior.AddComponent<PLPilotingSystem>();
                    newexterior.GetComponent<PLPilotingSystem>().MyShipInfo = ship;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint = currentpisys.ActivationPoint;
                    newexterior.GetComponent<PLPilotingSystem>().ActivationPoint.position = new Vector3(-0.7f,-261f,-317.3f);
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
                    newexterior.transform.SetParent(ship.ShipRoot.transform);
                    newexterior.layer = currentexterior.layer;
                    newexterior.AddComponent<PLFlightAI>();
                    newexterior.GetComponent<PLFlightAI>().MyShipInfo = ship;
                    newexterior.tag = "Ship";
                    Object.DontDestroyOnLoad(newexterior);
                    ship.ExteriorRenderers.Add(newexterior.GetComponent<MeshRenderer>());
                    ship.ExteriorRigidbody = newexterior.GetComponent<Rigidbody>();
                    ship.ExteriorMeshRenderer = newexterior.GetComponent<MeshRenderer>();
                    ship.Exterior = newexterior;
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
                    ship.RegularTurretPoints[0].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[0].transform.localPosition = new Vector3(-75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[0].transform.localScale = Vector3.one * 10f;
                    ship.RegularTurretPoints[0].transform.localRotation = new Quaternion(0, 0, 0, 0);
                    ship.RegularTurretPoints[1].transform.SetParent(newexterior.transform);
                    ship.RegularTurretPoints[1].transform.localPosition = new Vector3(75, 40.4253f, 235.0909f);
                    ship.RegularTurretPoints[1].transform.localScale = Vector3.one * 10f;
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
                    currentexterior.transform.SetParent(newexterior.transform, false);
                }
                GameObject interior = null;
                GameObject bridge = null;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                {
                    if (gameObject.name == "Planet")
                    {
                        interior = gameObject;
                    }
                    else if (gameObject.name == "BridgeBSO")
                    {
                        bridge = gameObject;
                    }
                    if (interior != null && bridge != null) break;
                }
                if (interior != null && bridge != null)
                {
                    GameObject newinterior = Object.Instantiate(interior, interior.transform.position + new Vector3(400, -400, 1500), interior.transform.rotation);
                    GameObject newbridge = Object.Instantiate(bridge, new Vector3(0, -258.2285f, -409.0034f), bridge.transform.rotation);
                    newbridge.transform.localRotation = new Quaternion(0.2202f, 0.0157f, 0.0263f, -0.975f);
                    //334,525 358,797 357,1805
                    Object.DontDestroyOnLoad(newinterior);
                    Object.DontDestroyOnLoad(newbridge);
                    MoveObjAndChild(newinterior.transform, ship.InteriorStatic.layer);
                    MoveObjAndChild(newbridge.transform, ship.InteriorStatic.layer);
                    ship.InteriorStatic.transform.localPosition = ship.InteriorStatic.transform.localPosition + new Vector3(0, 0, 1500);
                    newbridge.transform.SetParent(ship.InteriorStatic.transform);
                    //newbridge.transform.localPosition = new Vector3(-1.5054f, 144.0001f, -1908.824f);
                    //newbridge.transform.localRotation = new Quaternion(319.2696f, 109.4546f, 23.5083f, 0f);
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
                    foreach (PLKillVolume volume in newinterior.GetComponentsInChildren<PLKillVolume>())
                    {
                        volume.enabled = false;
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
                        BridgeToCaptain.TargetDoor = CapitanToBridge;
                        BridgeToCaptain.OptionalTLI = ship.MyTLI;
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
                    newinterior.transform.SetParent(ship.InteriorStatic.transform);
                    //ship.InteriorStatic = interior;
                    PLNetworkManager.Instance.MyLocalPawn.transform.position = new Vector3(0, -258.2285f, -409.0034f);
                }
            }
            SceneManager.UnloadSceneAsync(Estate);
            SceneManager.UnloadSceneAsync(Flagship);
        }
    }

    [HarmonyPatch(typeof(PLShipInfo),"Update")]
    public class Update
    {
        static void Postfix(PLShipInfo __instance, Color ___WarpObjectCurColor) 
        {
            if(__instance.WarpBlocker != null) 
            {
                __instance.WarpBlocker.transform.localScale = Vector3.one * (12000f - ___WarpObjectCurColor.a * 6000f);
            }
            if (!__instance.ShowingExterior && __instance.GetIsPlayerShip() && (PLNetworkManager.Instance.MyLocalPawn == null || PLNetworkManager.Instance.MyLocalPawn.CurrentShip == __instance)) 
            {
                __instance.InteriorRenderers.RemoveAll((MeshRenderer render) => render == null);
                foreach(MeshRenderer render in __instance.InteriorRenderers) 
                {
                    render.enabled = true;
                    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                }
                foreach(Light light in __instance.InteriorShipLights) 
                {
                    try
                    {
                        if (light.gameObject != null)
                        {
                            light.gameObject.SetActive(true);
                        }
                    }
                    catch{ }
                }
            }
        }
    }
}
